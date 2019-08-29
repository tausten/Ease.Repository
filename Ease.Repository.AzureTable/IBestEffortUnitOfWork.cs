//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;

namespace Ease.Repository.AzureTable
{
    /// <summary>
    /// Interface for unit of work pattern where best-effort transaction is to be managed by hand rather than by
    /// underlying store.
    /// 
    /// TODO: Make this not Azure-specific.. (i.e. rip out requirement for ITableEntity)
    /// </summary>
    public interface IBestEffortUnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Respositories must register the IStoreWriter to use for entity types that they wish the unit of work to manage.
        /// </summary>
        /// <typeparam name="TEntity">The entity Type the <paramref name="storeWriter"/> is for.</typeparam>
        /// <param name="storeWriter">The `IStoreWriter` to use for updating the store.</param>
        void RegisterStoreFor<TEntity>(IStoreWriter storeWriter);

        /// <summary>
        /// Registers an Add of a new entity with the unit of work.
        /// </summary>
        /// <param name="entity">The new entity being added</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <returns>The unit of work-tracked entity to return from repository.</returns>
        TEntity RegisterAdd<TEntity>(TEntity entity) where TEntity : class, new();

        /// <summary>
        /// Registers a set of entities for update handling with the unit of work. Typically, you should call this
        /// in the repository's Add handler, and in the repository's read-related handlers.
        /// </summary>
        /// <param name="entities">The entities that have been fetched from the store.</param>
        /// <param name="updateAction">The action to perform on a <typeparamref name="TEntity"/> to persist any
        /// changes made to the store.</param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <returns>The unit of work-tracked entities to return from repository.</returns>
        IEnumerable<TEntity> RegisterForUpdates<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new();

        /// <summary>
        /// Registers a Delete of an entity with the unit of work.
        /// </summary>
        /// <param name="entity">The entity being deleted.</param>
        /// <param name="deleteAction">The action to perform on a <typeparamref name="TEntity"/> to delete it from
        /// the store.</param>
        /// <param name="undoDeleteAction">The best-effort action to perform to undo
        /// a successful <paramref name="deleteAction"/></param>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        void RegisterDelete<TEntity>(TEntity entity) where TEntity : class, new();
    }
}
