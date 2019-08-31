//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Threading.Tasks;
using AutoFixture;
using Ease.Repository.AzureTable.Test;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using NUnit.Framework;

namespace Ease.Repository.AzureTable.Tests
{
    public class SomeAzureTableRepositoryTests
        : AzureTableRepositoryTests<AzureTableRepositoryContext, SomeAzureTableEntity, SomeAzureTableRepository>
    {
        protected override void PrepareDependenciesForContext(IFixture fixture)
        {
            base.PrepareDependenciesForContext(fixture);

            var config = fixture.Freeze<IAzureTableRepositoryConfig>();
            A.CallTo(() => config.ConnectionString).Returns("UseDevelopmentStorage=true");
            A.CallTo(() => config.TableNamePrefix).Returns(TestTableNamePrefix);
        }

        protected override void ModifyEntity(SomeAzureTableEntity entityToModify)
        {
            var newSuffix = Guid.NewGuid().ToString();
            entityToModify.FirstName.Should().NotEndWith(newSuffix);
            entityToModify.FirstName += newSuffix;
        }

        protected override void AssertEntitiesAreEquivalent(SomeAzureTableEntity result, SomeAzureTableEntity reference)
        {
            result.CurrentState().Should().BeEquivalentTo(reference.CurrentState(), options => options
                .Excluding(x => x.Timestamp));
        }

        protected override ITableEntity NewSimpleKeyFromEntity(SomeAzureTableEntity entity)
        {
            return new AzureTableEntityKey { PartitionKey = entity.PartitionKey, RowKey = entity.RowKey };
        }

        [SetUp]
        public override void SetUp()
        {
            SetUp_Impl();
        }

        [TearDown]
        public override void TearDown()
        {
            TearDown_Impl();
        }

        #region Base Tests
        [Test]
        public override void List_Returns_Empty_For_No_Data()
        {
            List_Returns_Empty_For_No_Data_Impl();
        }

        [Test]
        public override async Task Add_New_Entity_And_List_RoundTrip()
        {
            await Add_New_Entity_And_Get_By_Key_RoundTrip_Impl();
        }

        [Test]
        public override void Add_Sets_Keys()
        {
            Add_Sets_Keys_Impl();
        }

        [Test]
        public override async Task Add_New_Entity_And_Get_RoundTrip()
        {
            await Add_New_Entity_And_Get_RoundTrip_Impl();
        }

        [Test]
        public override async Task Add_New_Entity_And_Get_By_Key_RoundTrip()
        {
            await Add_New_Entity_And_Get_By_Key_RoundTrip_Impl();
        }

        [Test, Ignore("TODO: Need to make this work in light of the transactional unit of work implementation.")]
        public override async Task Add_Existing_Entity_And_Get_RoundTrip()
        {
            await Add_Existing_Entity_And_Get_RoundTrip_Impl();
        }

        [Test]
        public override async Task Delete_And_Get_RoundTrip()
        {
            await Delete_And_Get_RoundTrip_Impl();
        }

        [Test, Ignore("TODO: Need to make this work in light of the transactional unit of work implementation.")]
        public override async Task Delete_By_Key_And_Get_RoundTrip()
        {
            await Delete_By_Key_And_Get_RoundTrip_Impl();
        }

        #endregion // Base Tests

        private class Bug_12_Repository : AzureTableRepository<AzureTableRepositoryContext, SomeAzureTableEntity>
        {
            public string TheTableName => TableName;

            public Bug_12_Repository(BestEffortUnitOfWork<AzureTableRepositoryContext> unitOfWork) : base(unitOfWork) { }

            protected override string CalculatePartitionKeyFor(SomeAzureTableEntity entity)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void Bug_12_Default_Table_Name_Should_Be_TypeOf_Entity_Name()
        {
            // Arrange
            var sut = TheFixture.Freeze<Bug_12_Repository>();

            // Act
            // Assert
            sut.TheTableName.Should().Be(typeof(SomeAzureTableEntity).Name);
        }
    }
}