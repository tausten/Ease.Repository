//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Linq;
using AutoFixture;
using Ease.Repository.AzureTable;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using NUnit.Framework;

namespace HelperAPIs.Impl.Test.Data
{
    public abstract class AzureTableRepositoryTests<TContext, TEntity, TRepository>
        : RepositoryTests<ITableEntity, TEntity, TRepository>
        where TContext : class, IAzureTableRepositoryContext
        where TEntity : class, ITableEntity, new()
        where TRepository : AzureTableRepository<TContext, TEntity>
    {
        protected string TestTableNamePrefix { get; private set; }
        
        protected AzureTableUnitOfWork<TContext> UnitOfWork { get; private set; }

        protected virtual void PrepareDependenciesForContext(IFixture fixture) { }

        /// <summary>
        /// Override to explicitly `Freeze()` any necessary dependencies prior to the `Sut` being allocated.
        /// </summary>
        /// <param name="fixture"></param>
        protected override void FreezeDependencies(IFixture fixture)
        {
            TestTableNamePrefix = $"TEST{Guid.NewGuid().ToString().Replace("-", string.Empty)}"
                .ToUpperInvariant();

            PrepareDependenciesForContext(TheFixture);

            UnitOfWork = fixture.Freeze<AzureTableUnitOfWork<TContext>>();
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
        
        [TearDown]
        public virtual void TearDown()
        {
            var tables = UnitOfWork.Context.Client.ListTables(TestTableNamePrefix);
            foreach (var table in tables)
            {
                table.Delete();
            }
        }

        [Test]
        public void List_Returns_Empty_For_No_Data()
        {
            // Arrange
            // Act
            var result = Sut.List();

            // Assert
            result.Should().BeEmpty();
        }
        
        [Test]
        public void Add_New_Entity_And_List_RoundTrip()
        {
            // Arrange
            var newThing = NewEntity();
            Sut.List().Should().BeEmpty();

            // Act
            Sut.Add(newThing);
            var result = Sut.List().ToList();

            // Assert
            result.Count.Should().Be(1);
            var fetchedThing = result.First();

            AssertEntitiesAreEquivalent(fetchedThing, newThing);
        }
    }
}