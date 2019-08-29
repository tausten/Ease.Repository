// 
// Copyright (c) 2019 Austen-Steele Enterprises, LLC. All rights reserved.
// 

namespace Ease.Repository.AzureTable.Tests.Data
{
    public class SampleAzureTableRepository : AzureTableRepository<AzureTableRepositoryContext, SampleAzureTableEntity>
    {
        public SampleAzureTableRepository(AzureTableUnitOfWork<AzureTableRepositoryContext> unitOfWork) : base(unitOfWork) { }

        private const string DefaultPartitionKey = "DEFAULT";
        protected override string CalculatePartitionKeyFor(SampleAzureTableEntity entity)
        {
            return string.IsNullOrWhiteSpace(entity.LastName)
                ? DefaultPartitionKey
                // We're going to partition customers by the uppercase first letter of their last name 
                : entity.LastName.TrimStart().Substring(0, 1).ToUpperInvariant();
        }
    }
}