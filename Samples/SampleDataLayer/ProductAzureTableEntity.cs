//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Ease.Repository.AzureTable;

namespace SampleDataLayer
{
    public class ProductAzureTableEntity : AzureTableTrackableEntity
    {
        public virtual string ProductName { get; set; }
        public virtual string ProductDescription { get; set; }
    }
}
