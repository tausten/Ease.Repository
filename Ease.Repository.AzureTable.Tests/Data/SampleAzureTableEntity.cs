//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Microsoft.Azure.Cosmos.Table;

namespace Ease.Repository.AzureTable.Tests.Data
{
    public class SampleAzureTableEntity : AzureTableTrackableEntity
    {
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
    }
}