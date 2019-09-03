//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Ease.Repository.Test;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;

namespace Ease.Repository.AzureTable.Test
{
    public abstract class AzureTableRepositoryTests<TContext, TEntity, TRepository>
        : RepositoryTests<ITableEntity, TEntity, TRepository>
        where TContext : class, IAzureTableRepositoryContext
        where TEntity : class, ITableEntity, new()
        where TRepository : AzureTableRepository<TContext, TEntity>
    {
        protected string TestTableNamePrefix { get; private set; }

        protected BestEffortUnitOfWork UnitOfWork { get; private set; }

        protected TContext Context { get; private set; }

        protected virtual void PrepareDependenciesForContext(IFixture fixture) { }

        protected override async Task CompleteUnitOfWorkAsync()
        {
            await UnitOfWork.CompleteAsync();
        }

        /// <summary>
        /// Override to explicitly `Freeze()` any necessary dependencies prior to the `Sut` being allocated.
        /// </summary>
        /// <param name="fixture"></param>
        protected override void FreezeDependencies(IFixture fixture)
        {
            TestTableNamePrefix = $"TEST{Guid.NewGuid().ToString().Replace("-", string.Empty)}"
                .ToUpperInvariant();

            fixture.Inject<IAzureTableStoreFactory>(new AzureTableStoreFactory());
            UnitOfWork = new BestEffortUnitOfWork();
            fixture.Inject<IBestEffortUnitOfWork>(UnitOfWork);

            PrepareDependenciesForContext(TheFixture);

            Context = fixture.Freeze<TContext>();
        }

        protected override void NullifyKeyFields(TEntity entity)
        {
            entity.PartitionKey = null;
            entity.RowKey = null;
        }

        protected override void AssertKeyFieldsNotNull(TEntity entity)
        {
            entity.PartitionKey.Should().NotBeNullOrWhiteSpace();
            entity.RowKey.Should().NotBeNullOrWhiteSpace();
        }

        protected override void AssertRepositoryHasNumEntities(int num = 0)
        {
            Sut.List().Count().Should().Be(num);
        }

        /// <summary>
        /// A tear-down step is necessary.
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating TearDown methods.
        /// </summary>
        public abstract void TearDown();

        protected virtual void TearDown_Impl()
        {
            var tables = Context.Client.ListTables(TestTableNamePrefix);
            foreach (var table in tables)
            {
                table.Delete();
            }
        }

        /// <summary>
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating test methods.
        /// </summary>
        public abstract void List_Returns_Empty_For_No_Data();

        protected virtual void List_Returns_Empty_For_No_Data_Impl()
        {
            // Arrange
            // Act
            var result = Sut.List();

            // Assert
            result.Should().BeEmpty();
        }

        /// <summary>
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating test methods.
        /// </summary>
        public abstract Task Add_New_Entity_And_List_RoundTrip();

        protected virtual async Task Add_New_Entity_And_List_RoundTrip_Impl()
        {
            // Arrange
            var newThing = NewEntity();
            Sut.List().Should().BeEmpty();

            // Act
            Sut.Add(newThing);
            await UnitOfWork.CompleteAsync();
            var result = Sut.List().ToList();

            // Assert
            result.Count.Should().Be(1);
            var fetchedThing = result.First();

            AssertEntitiesAreEquivalent(fetchedThing, newThing);
        }
    }
}