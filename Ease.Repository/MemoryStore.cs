//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ease.Repository
{
    /// <summary>
    /// A simple in-memory "Store" implementation for experimentation and testing.
    /// </summary>
    public class MemoryStore : IStoreBatchWriter
    {
        private readonly ConcurrentDictionary<Type, object> _entitiesByType = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Register an entity Type with an appropriate <paramref name="entityKeyComparer"/> (needed to detect if
        /// entity is already present)
        /// </summary>
        /// <param name="entityKeyComparer">Use to compare the key portion of the entity, ignoring the rest of non-key
        /// properties</param>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public HashSet<TEntity> Register<TEntity>(IEqualityComparer<TEntity> entityKeyComparer)
        {
            var result = new HashSet<TEntity>(entityKeyComparer);
            if (_entitiesByType.TryGetValue(typeof(TEntity), out var existingObject))
            {
                var existingEntities = (HashSet<TEntity>)existingObject;
                foreach (var entity in existingEntities)
                {
                    result.Add(entity);
                }
                existingEntities.Clear();
            }
            _entitiesByType[typeof(TEntity)] = result;
            return result;
        }

        /// <summary>
        /// Returns the managed set of entities of the specified Type.
        /// </summary>
        /// <typeparam name="TEntity">The entity Type</typeparam>
        /// <returns>The managed set of entities</returns>
        public HashSet<TEntity> Entities<TEntity>()
        {
            HashSet<TEntity> result;
            if (!_entitiesByType.TryGetValue(typeof(TEntity), out var entitiesObject))
            {
                result = Register(EqualityComparer<TEntity>.Default);
            }
            else
            {
                result = (HashSet<TEntity>)entitiesObject;
            }

            return result;
        }

        public void Add<TEntity>(TEntity entity) where TEntity : class, new()
        {
            if (!Entities<TEntity>().Add(entity))
            {
                throw new ArgumentException("Entity is already present.", nameof(entity));
            }
        }

        public void Update<TEntity>(TEntity entity) where TEntity : class, new()
        {
            Delete(entity);
            Add(entity);
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : class, new()
        {
            Entities<TEntity>().Remove(entity);
        }

        public void Add<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new()
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }
        }

        public void Update<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new()
        {
            foreach (var entity in entities)
            {
                Update(entity);
            }
        }

        public void Delete<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new()
        {
            foreach (var entity in entities)
            {
                Delete(entity);
            }
        }
    }
}
