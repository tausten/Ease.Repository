//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ease.Repository
{
    /// <summary>
    /// Synchronous write abstraction for Stores.
    /// </summary>
    public interface IStoreWriter
    {
        /// <summary>
        /// Add the new entity to the store.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        void Add<TEntity>(TEntity entity) where TEntity : class, new();

        /// <summary>
        /// Update the entity in the store.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <typeparam name="TEntity">Tye entity Type</typeparam>
        void Update<TEntity>(TEntity entity) where TEntity : class, new();

        /// <summary>
        /// Delete the entity from the store.
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        void Delete<TEntity>(TEntity entity) where TEntity : class, new();
    }

    /// <summary>
    /// Synchronous batch write abstraction for Stores.
    /// </summary>
    public interface IStoreBatchWriter : IStoreWriter
    {
        /// <summary>
        /// Add the new entities to the store.
        /// </summary>
        /// <param name="entities">The entities to add</param>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        void Add<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new();

        /// <summary>
        /// Update the entities in the store.
        /// </summary>
        /// <param name="entities">The entities to update</param>
        /// <typeparam name="TEntity">Tye entity Type</typeparam>
        void Update<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new();

        /// <summary>
        /// Delete the entities from the store.
        /// </summary>
        /// <param name="entities">The entities to delete</param>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        void Delete<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new();
    }

    /// <summary>
    /// Async write abstraction for Stores.
    /// </summary>
    public interface IStoreWriterAsync : IStoreWriter
    {
        /// <summary>
        /// Add the new entity to the store.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        /// <returns>The task wait on to perform the operation.</returns>
        Task AddAsync<TEntity>(TEntity entity) where TEntity : class, new();

        /// <summary>
        /// Update the entity in the store.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <typeparam name="TEntity">Tye entity Type</typeparam>
        /// <returns>The task wait on to perform the operation.</returns>
        Task UpdateAsync<TEntity>(TEntity entity) where TEntity : class, new();

        /// <summary>
        /// Delete the entity from the store.
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        /// <returns>The task wait on to perform the operation.</returns>
        Task DeleteAsync<TEntity>(TEntity entity) where TEntity : class, new();
    }

    /// <summary>
    /// Asynchronous batch write abstraction for Stores.
    /// </summary>
    public interface IStoreBatchWriterAsync : IStoreWriterAsync
    {
        /// <summary>
        /// Add the new entities to the store.
        /// </summary>
        /// <param name="entities">The entities to add</param>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        Task AddAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new();

        /// <summary>
        /// Update the entities in the store.
        /// </summary>
        /// <param name="entities">The entities to update</param>
        /// <typeparam name="TEntity">Tye entity Type</typeparam>
        Task UpdateAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new();

        /// <summary>
        /// Delete the entities from the store.
        /// </summary>
        /// <param name="entities">The entities to delete</param>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        Task DeleteAsync<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new();
    }
}
