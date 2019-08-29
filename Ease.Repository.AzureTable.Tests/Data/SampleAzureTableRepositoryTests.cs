//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using AutoFixture;
using FakeItEasy;
using FluentAssertions;
using HelperAPIs.Impl.Test.Data;
using Microsoft.Azure.Cosmos.Table;

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
    }
}