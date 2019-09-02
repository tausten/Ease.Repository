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

    /// <summary>
    /// Base class for AzureTable-backed repositories.
    /// </summary>
    /// <typeparam name="TContext">The repository context Type this repository will operate on.</typeparam>
    /// <typeparam name="TEntity">The entity Type managed by the repository.</typeparam>
    public abstract class AzureTableRepository<TContext, TEntity>
        : IRepository<ITableEntity, TEntity>
        where TContext : class, IAzureTableRepositoryContext
        where TEntity : class, ITableEntity, new()
    {
        /// <summary>
        /// Construct the repository instance to operate with the passed <paramref name="unitOfWork"/>.
        /// </summary>
        /// <param name="unitOfWork">The `UnitOfWork` this repository's operations will fall within.</param>
        protected AzureTableRepository(TContext context)
        {
            Table = context.RegisterTableForEntityType<TEntity>(() => TableName);
            Context = context;
        }

        /// <summary>
        /// The Azure `CloudTable` in which the entities will be stored. Repositories can use this property to implement
        /// the read operations beyond the simple <see cref="Get(ITableEntity)"/> and <see cref="List"/> methods.
        /// </summary>
        protected readonly Lazy<CloudTable> Table;

        /// <summary>
        /// By default, TableName will be the `typeof(TEntity).Name` (which means the table name will change if you rename
        /// the entity class). If you wish to have a stable name, then override and return the desired name.
        /// Either way, the actual table name may include a prefix as governed by the `TContext` when it prepares the
        /// table.
        /// </summary>
        protected virtual string TableName => typeof(TEntity).Name;

        /// <summary>
        /// The RepositoryContext.
        /// </summary>
        protected TContext Context { get; }

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
        /// Do be sure this will generate a unique identifier within the scope of this <typeparamref name="TEntity"/>'s
        /// persistent store.
        /// </summary>
        /// <returns></returns>
        protected virtual string NewUniqueId()
        {
            return Guid.NewGuid().ToString().ToUpperInvariant();
        }

        public virtual IEnumerable<TEntity> List()
        {
            var query = new TableQuery<TEntity>();
            var entities = Table.Value.ExecuteQuery(query);
            return Context.RegisterForUpdates(entities);
        }

        public virtual TEntity Get(ITableEntity key)
        {
            var op = TableOperation.Retrieve<TEntity>(key.PartitionKey, key.RowKey);
            var result = Table.Value.Execute(op);

            return result.Result is TEntity resultEntity
                ? Context.RegisterForUpdates(new[] { resultEntity }).FirstOrDefault()
                : null;
        }

        public virtual TEntity Add(TEntity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.RowKey))
            {
                entity.RowKey = NewUniqueId();
            }

            if (string.IsNullOrWhiteSpace(entity.PartitionKey))
            {
                entity.PartitionKey = CalculatePartitionKeyFor(entity);
            }

            return Context.RegisterAdd(entity);
        }

        public virtual void Delete(ITableEntity key)
        {
            var entity = key as TEntity ?? Get(key);

            if (null != entity)
            {
                Context.RegisterDelete(entity);
            }
        }
    }
}