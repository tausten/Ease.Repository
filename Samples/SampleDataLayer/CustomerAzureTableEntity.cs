//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Ease.Repository.AzureTable;
using System;

namespace SampleDataLayer
{
    /// <summary>
    /// NOTE: Normally, a name of `CustomerEntity` would be better.. 
    /// but because this sample data model includes entities for multiple different stores, it's useful to 
    /// differentiate which entity goes with which store by name.
    /// </summary>
    public class CustomerAzureTableEntity : AzureTableTrackableEntity
    {
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual DateTime Birthday { get; set; }
        public virtual string FavoriteProduct { get; set; }

        // TODO: Once AzureTableTrackableEntity has support for Json serialization of non-natively supported types, 
        // let's add a collection property to the sample.
//        public virtual List<string> ProductIds { get; set; }
    }
}
