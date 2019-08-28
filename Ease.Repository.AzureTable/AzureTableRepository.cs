//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Collections.Generic;
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
        protected AzureTableUnitOfWork<TContext> UnitOfWork { get; private set; }

        protected readonly Lazy<CloudTable> Table;

        protected AzureTableRepository(AzureTableUnitOfWork<TContext> unitOfWork)
        {
            UnitOfWork = unitOfWork;
            Table = UnitOfWork.Context.PrepareTable(() => TableName);
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

        protected virtual void PersistToStore(TEntity entity)
        {
            var op = TableOperation.Insert(entity);
            var result = Table.Value.Execute(op);
            // TODO: What to do on failure to insert?
        }

        protected virtual TEntity GetFromStore(ITableEntity key)
        {
            var op = TableOperation.Retrieve<TEntity>(key.PartitionKey, key.RowKey);
            var result = Table.Value.Execute(op);
            return result.Result as TEntity;
        }

        protected virtual void UpdateInStore(TEntity entity)
        {
            // TODO: Look into versioned entities, and detecting concurrent editing.
            var op = TableOperation.Replace(entity);
            var result = Table.Value.Execute(op);
            // TODO: What to do on failure to update?
        }

        protected virtual void DeleteFromStore(TEntity entity)
        {
            var op = TableOperation.Delete(entity);
            var result = Table.Value.Execute(op);
            // TODO: What to do on failure to delete?
        }

        public IEnumerable<TEntity> List()
        {
            var query = new TableQuery<TEntity>();
            var entities = Table.Value.ExecuteQuery(query);
            return entities;
        }

        public TEntity Get(ITableEntity key)
        {
            // TODO: Register with UnitOfWork
            return GetFromStore(key);
        }

        public TEntity Add(TEntity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.RowKey))
            {
                entity.RowKey = NewUniqueId().ToString().ToUpperInvariant();
            }

            if (string.IsNullOrWhiteSpace(entity.PartitionKey))
            {
                entity.PartitionKey = CalculatePartitionKeyFor(entity);
            }

            // TODO: Register with UnitOfWork
            PersistToStore(entity);

            return entity;
        }

        public void Delete(ITableEntity key)
        {
            var entity = key as TEntity ?? Get(key);

            if (null != entity)
            {
                // TODO: Register with UnitOfWork
                DeleteFromStore(entity);
            }
        }
    }
}