//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos.Table;

namespace Ease.Repository.AzureTable
{
    /// <summary>
    /// Use this if/when you don't have the full strongly-typed entity, but you do have the PartitionKey and RowKey
    /// for operations that just need the key values.
    /// </summary>
    public class AzureTableEntityKey : ITableEntity
    {
        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            throw new NotImplementedException(
                "This is intentionally not implemented because this class is just a thin key-holder.");
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            throw new NotImplementedException(
                "This is intentionally not implemented because this class is just a thin key-holder.");
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        #region Ignore these
        public DateTimeOffset Timestamp { get; set; }
        public string ETag { get; set; }
        #endregion
    }

    public abstract class AzureTableRepository<TContext, TEntity>
        : IRepository<ITableEntity, TEntity>
        where TContext : class, IAzureTableRepositoryContext
        where TEntity : class, ITableEntity, new()
    {
        protected BestEffortUnitOfWork<TContext> UnitOfWork { get; private set; }

        protected readonly Lazy<CloudTable> Table;

        protected readonly AzureTableStoreWriter StoreWriter;

        protected AzureTableRepository(BestEffortUnitOfWork<TContext> unitOfWork)
        {
            UnitOfWork = unitOfWork;
            Table = UnitOfWork.Context.PrepareTable(() => TableName);
            StoreWriter = new AzureTableStoreWriter(Table);

            UnitOfWork.RegisterStoreFor<TEntity>(StoreWriter);
        }

        /// <summary>
        /// By default, TableName will be the `nameof(TEntity)` (which means the table name will change if you rename
        /// the entity class). If you wish to have a stable name, then override and return the desired name.
        /// Either way, the actual table name may include a prefix as governed by the `TContext` when it prepares the
        /// table.
        /// </summary>
        protected virtual string TableName => nameof(TEntity);

        /// <summary>
        /// Override to return a suitable `PartitionKey` derived from the entity. This should be a function of
        /// the other properties in the entity so that a fundamental change to the partition can be detected and
        /// handled allowing for an entity to be moved properly (i.e. the entity's actual PartitionKey value should
        /// be treated as Read-Only).
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected abstract string CalculatePartitionKeyFor(TEntity entity);

        /// <summary>
        /// Override to provide some other means of generating unique IDs. Default is using Guid.NewGuid() to 
        /// reduce 3rd-party dependencies, but consider overriding and using a CombGuid algorithm or something similar.
        /// </summary>
        /// <returns></returns>
        protected virtual Guid NewUniqueId()
        {
            return Guid.NewGuid();
        }

        public virtual IEnumerable<TEntity> List()
        {
            var query = new TableQuery<TEntity>();
            var entities = Table.Value.ExecuteQuery(query);
            return UnitOfWork.RegisterForUpdates(entities);
        }

        public virtual TEntity Get(ITableEntity key)
        {
            var op = TableOperation.Retrieve<TEntity>(key.PartitionKey, key.RowKey);
            var result = Table.Value.Execute(op);

            return result.Result is TEntity resultEntity
                ? UnitOfWork.RegisterForUpdates(new[] { resultEntity }).FirstOrDefault()
                : null;
        }

        public virtual TEntity Add(TEntity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.RowKey))
            {
                entity.RowKey = NewUniqueId().ToString().ToUpperInvariant();
            }

            if (string.IsNullOrWhiteSpace(entity.PartitionKey))
            {
                entity.PartitionKey = CalculatePartitionKeyFor(entity);
            }

            return UnitOfWork.RegisterAdd(entity);
        }

        public virtual void Delete(ITableEntity key)
        {
            var entity = key as TEntity ?? Get(key);

            if (null != entity)
            {
                UnitOfWork.RegisterDelete(entity);
            }
        }
    }
}