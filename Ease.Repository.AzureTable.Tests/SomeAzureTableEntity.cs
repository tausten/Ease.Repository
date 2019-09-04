//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

namespace Ease.Repository.AzureTable.Tests
{
    public class SomeAzureTableEntity : AzureTableTrackableEntity
    {
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
    }
}