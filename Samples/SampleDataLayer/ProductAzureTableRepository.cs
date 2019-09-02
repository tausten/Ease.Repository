﻿//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Ease.Repository.AzureTable;

namespace SampleDataLayer
{
    public class ProductAzureTableRepository : AzureTableRepository<SampleAzureTableMainRepositoryContext, ProductAzureTableEntity>
    {
        public ProductAzureTableRepository(SampleAzureTableMainRepositoryContext context) : base(context) { }

        /// <summary>
        /// For this sample, we're pretending that we're confident we won't have so many products that partitioning 
        /// would be better than just storing them all in same partition together.
        /// NOTE: This differs from the computed partition key for <see cref="CustomerAzureTableEntity"/>.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override string CalculatePartitionKeyFor(ProductAzureTableEntity entity)
        {
            return "Products";
        }
    }
}
