// 
// Copyright (c) 2019 Austen-Steele Enterprises, LLC. All rights reserved.
// 

using Microsoft.Azure.Cosmos.Table;

namespace Ease.Repository.AzureTable.Tests.Data
{
    public class SampleAzureTableEntity : AzureTrackableTableEntity
    {
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
    }
}