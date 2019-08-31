//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

namespace Ease.Repository.AzureTable.Tests
{
    public class SomeAzureTableRepository : AzureTableRepository<AzureTableRepositoryContext, SomeAzureTableEntity>
    {
        public SomeAzureTableRepository(BestEffortUnitOfWork<AzureTableRepositoryContext> unitOfWork) : base(unitOfWork) { }

        private const string DefaultPartitionKey = "DEFAULT";
        protected override string CalculatePartitionKeyFor(SomeAzureTableEntity entity)
        {
            return string.IsNullOrWhiteSpace(entity.LastName)
                ? DefaultPartitionKey
                // We're going to partition customers by the uppercase first letter of their last name 
                : entity.LastName.TrimStart().Substring(0, 1).ToUpperInvariant();
        }
    }
}