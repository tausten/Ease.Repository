//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

namespace Ease.Repository
{
    /// <summary>
    /// Base interface for general entity repositories.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEntity"></typeparam>
    public interface IRepository<in TKey, TEntity>
        where TEntity : TKey, new()
    {
        /// <summary>
        /// Get an entity by its key.
        /// </summary>
        /// <param name="key">The key (may be compound) to use to look up the entity.</param>
        /// <returns>The matching entity, or `null` if not found. CAUTION: Do not attempt to serialize returned
        /// repository entities. They are not guaranteed to be <typeparamref name="TEntity"/> alone. They are
        /// more likely to be a dynamic proxy class inheriting from `TEntity` and extending it with additional
        /// properties you wouldn't want serialized. For serialization, you should map the entity to a suitably
        /// controlled DTO / view model, etc..</returns>
        TEntity Get(TKey key);

        /// <summary>
        /// Adds the entity to the repository.
        /// NOTE: Caller should proceed with the returned entity rather than the passed entity to guarantee proper unit
        /// of work tracking to be managed (i.e. the returned entity is not guaranteed to be reference equal to the
        /// passed entity).
        /// </summary>
        /// <param name="entity">The entity to create or update.</param>
        /// <returns>The resultant repository-persistent entity. CAUTION: Do not attempt to serialize returned
        /// repository entities. They are not guaranteed to be <typeparamref name="TEntity"/> alone. They are
        /// more likely to be a dynamic proxy class inheriting from `TEntity` and extending it with additional
        /// properties you wouldn't want serialized. For serialization, you should map the entity to a suitably
        /// controlled DTO / view model, etc..</returns>
        TEntity Add(TEntity entity);

        /// <summary>
        /// Deletes an entity by its key. If the entity is already deleted, this does not generate an exception.
        /// </summary>
        /// <param name="key">The key (may be compound) to use to find the entity to delete.</param>
        void Delete(TKey key);
    }
}