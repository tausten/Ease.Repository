//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Ease.Repository.AzureTable;

namespace SampleDataLayer
{
    public class CustomerAzureTableRepository : AzureTableRepository<SampleAzureTableMainRepositoryContext, CustomerAzureTableEntity>
    {
        public CustomerAzureTableRepository(SampleAzureTableMainRepositoryContext context) : base(context) { }

        private const string DefaultPartitionKey = "DEFAULT";
        protected override string CalculatePartitionKeyFor(CustomerAzureTableEntity entity)
        {
            return string.IsNullOrWhiteSpace(entity.LastName)
                ? DefaultPartitionKey
                // We're going to partition customers by the uppercase first letter of their last name 
                : entity.LastName.TrimStart().Substring(0, 1).ToUpperInvariant();
        }
    }
}
