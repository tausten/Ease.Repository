//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using AutoFixture;
using Ease.Repository;
using Ease.Repository.AzureTable;
using Ease.Repository.AzureTable.Test;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SampleDataLayer;
using System;
using System.Threading.Tasks;

namespace SampleDataLayerIntegrationTests
{
    public class ProductAzureTableRepositoryTests
        : AzureTableRepositoryTests<IAzureTableRepositoryContext, ProductAzureTableEntity, ProductAzureTableRepository>
    {
        protected override void PrepareDependenciesForContext(IFixture fixture)
        {
            base.PrepareDependenciesForContext(fixture);

            var config = fixture.Freeze<IConfiguration>();
            A.CallTo(() => config["Main:Azure:StorageConnectionString"]).Returns("UseDevelopmentStorage=true");
            A.CallTo(() => config["Main:Azure:TableNamePrefix"]).Returns(TestTableNamePrefix);

            // We dance this little jig for the case where we'd be registering a particular concrete implementation of 
            // a service with the DI container.
            var context = fixture.Freeze<SampleAzureTableMainRepositoryContext>();
            fixture.Inject<IAzureTableRepositoryContext>(context);
        }

        protected override void ModifyEntity(ProductAzureTableEntity entityToModify)
        {
            var newSuffix = Guid.NewGuid().ToString();
            entityToModify.ProductDescription.Should().NotEndWith(newSuffix);
            entityToModify.ProductDescription += newSuffix;
        }

        protected override void AssertEntitiesAreEquivalent(ProductAzureTableEntity result, ProductAzureTableEntity reference)
        {
            result.CurrentState().Should().BeEquivalentTo(reference.CurrentState(), options => options
                .Excluding(x => x.Timestamp)
                );
        }

        protected override ITableEntity NewSimpleKeyFromEntity(ProductAzureTableEntity entity)
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

        // TODO: Add your repository-specific tests here (eg. for extra query-related functionality, etc...)
    }
}
