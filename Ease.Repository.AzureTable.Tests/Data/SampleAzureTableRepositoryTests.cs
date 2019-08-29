//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Threading.Tasks;
using AutoFixture;
using FakeItEasy;
using FluentAssertions;
using HelperAPIs.Impl.Test.Data;
using Microsoft.Azure.Cosmos.Table;
using NUnit.Framework;

namespace Ease.Repository.AzureTable.Tests.Data
{
    public class SampleAzureTableRepositoryTests
        : AzureTableRepositoryTests<AzureTableRepositoryContext, SampleAzureTableEntity, SampleAzureTableRepository>
    {
        protected override void PrepareDependenciesForContext(IFixture fixture)
        {
            base.PrepareDependenciesForContext(fixture);

            var config = fixture.Freeze<IAzureTableRepositoryConfig>();
            A.CallTo(() => config.ConnectionString).Returns("UseDevelopmentStorage=true");
            A.CallTo(() => config.TableNamePrefix).Returns(TestTableNamePrefix);
        }

        protected override void ModifyEntity(SampleAzureTableEntity entityToModify)
        {
            var newSuffix = Guid.NewGuid().ToString();
            entityToModify.FirstName.Should().NotEndWith(newSuffix);
            entityToModify.FirstName += newSuffix;
        }

        protected override void AssertEntitiesAreEquivalent(SampleAzureTableEntity result, SampleAzureTableEntity reference)
        {
            result.CurrentState().Should().BeEquivalentTo(reference.CurrentState(), options => options
                .Excluding(x => x.Timestamp));
        }

        protected override ITableEntity NewSimpleKeyFromEntity(SampleAzureTableEntity entity)
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

        [Test]
        public override void List_Returns_Empty_For_No_Data()
        {
            List_Returns_Empty_For_No_Data_Impl();
        }

        [Test]
        public override Task Add_New_Entity_And_List_RoundTrip()
        {
            return Add_New_Entity_And_Get_By_Key_RoundTrip_Impl();
        }

        [Test]
        public override void Add_Sets_Keys()
        {
            Add_Sets_Keys_Impl();
        }

        [Test]
        public override Task Add_New_Entity_And_Get_RoundTrip()
        {
            return Add_New_Entity_And_Get_RoundTrip_Impl();
        }

        [Test]
        public override Task Add_New_Entity_And_Get_By_Key_RoundTrip()
        {
            return Add_New_Entity_And_Get_By_Key_RoundTrip_Impl();
        }

        [Test, Ignore("TODO: Need to make this work in light of the transactional unit of work implementation.")]
        public override Task Add_Existing_Entity_And_Get_RoundTrip()
        {
            return Add_Existing_Entity_And_Get_RoundTrip_Impl();
        }

        [Test]
        public override Task Delete_And_Get_RoundTrip()
        {
            return Delete_And_Get_RoundTrip_Impl();
        }

        [Test, Ignore("TODO: Need to make this work in light of the transactional unit of work implementation.")]
        public override Task Delete_By_Key_And_Get_RoundTrip()
        {
            return Delete_By_Key_And_Get_RoundTrip_Impl();
        }
    }
}