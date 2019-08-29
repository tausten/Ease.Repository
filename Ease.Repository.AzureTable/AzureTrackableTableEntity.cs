//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.Cosmos.Table;

namespace Ease.Repository.AzureTable
{
    /// <summary>
    /// Entity base class required for use with AzureTableUnitOfWork... unfortunately the existing TableEntity
    /// class's properties are not virtual, and therefore can not be used with dynamic proxy.
    /// </summary>
    public abstract class AzureTrackableTableEntity : ITableEntity
    {
        public virtual string PartitionKey { get; set; }
        public virtual string RowKey { get; set; }

        /// <summary>
        /// NOTE: According to the docs, this value is managed by the server, and effectively read-only.
        /// </summary>
        public virtual DateTimeOffset Timestamp { get; set; }

        public virtual string ETag { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            PrivateTableEntityReflectionReadMethod.Invoke(
                null, new object[] { this, properties, operationContext });
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var result = PrivateTableEntityReflectionWriteMethod.Invoke(
                null, new object[] { this, operationContext });
            return result as IDictionary<string, EntityProperty>;
        }

        static AzureTrackableTableEntity()
        {
            PrivateTableEntityReflectionReadMethod = typeof(TableEntity).GetMethod("ReflectionRead", BindingFlags.NonPublic | BindingFlags.Static);
            PrivateTableEntityReflectionWriteMethod = typeof(TableEntity).GetMethod("ReflectionWrite", BindingFlags.NonPublic | BindingFlags.Static);
        }

        /// <summary>
        /// Thanks to the library's excessive use of `internal` and `private`, we have to leap through hoops like this
        /// to reuse some utilities. :facepalm:
        /// </summary>
        private static readonly MethodInfo PrivateTableEntityReflectionReadMethod;
        private static readonly MethodInfo PrivateTableEntityReflectionWriteMethod;
    }
}