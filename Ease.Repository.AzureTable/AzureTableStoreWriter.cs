//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Microsoft.Azure.Cosmos.Table;
using System;
using System.Threading.Tasks;

namespace Ease.Repository.AzureTable
{
    public class AzureTableStoreWriter : IStoreWriterAsync
    {
        private readonly CloudTable _table;

        public AzureTableStoreWriter(CloudTable table)
        {
            _table = table;
        }

        private ITableEntity EnsureIsTableEntity(object entity)
        {
            return entity as ITableEntity ?? throw new ArgumentException($"The entity must implement [{nameof(ITableEntity)}]", nameof(entity));
        }

        public void Add<TEntity>(TEntity entity) where TEntity : class, new()
        {
            var op = TableOperation.Insert(EnsureIsTableEntity(entity));
            var result = _table.Execute(op);
            // TODO: What to do on failure to insert?
        }

        public Task AddAsync<TEntity>(TEntity entity) where TEntity : class, new()
        {
            throw new NotImplementedException();
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : class, new()
        {
            var op = TableOperation.Delete(EnsureIsTableEntity(entity));
            var result = _table.Execute(op);
            // TODO: What to do on failure to delete?
        }

        public Task DeleteAsync<TEntity>(TEntity entity) where TEntity : class, new()
        {
            throw new NotImplementedException();
        }

        public void Update<TEntity>(TEntity entity) where TEntity : class, new()
        {
            // TODO: Look into versioned entities, and detecting concurrent editing.
            var op = TableOperation.Replace(EnsureIsTableEntity(entity));
            var result = _table.Execute(op);
            // TODO: What to do on failure to update?
        }

        public Task UpdateAsync<TEntity>(TEntity entity) where TEntity : class, new()
        {
            throw new NotImplementedException();
        }
    }
}
