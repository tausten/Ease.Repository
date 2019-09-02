//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;

namespace Ease.Repository.AzureTable
{
    public interface IAzureTableRepositoryContext : IRegisterEntities
    {
        CloudTableClient Client { get; }
        Lazy<CloudTable> RegisterTableForEntityType<TEntity>(Func<string> tableNameFunc);
    }

    public class AzureTableRepositoryContext : IAzureTableRepositoryContext
    {
        private readonly IBestEffortUnitOfWork _unitOfWork;
        private readonly IAzureTableStoreFactory _storeFactory;

        private readonly object _guard = new object();
        private readonly ConcurrentDictionary<Type, Lazy<CloudTable>> _tableByEntityType = new ConcurrentDictionary<Type, Lazy<CloudTable>>();
        private readonly ConcurrentDictionary<string, IStoreWriter> _storeWriterByTableName = new ConcurrentDictionary<string, IStoreWriter>();

        public AzureTableRepositoryContext(
            IAzureTableRepositoryConfig config, 
            IBestEffortUnitOfWork unitOfWork,
            IAzureTableStoreFactory storeFactory)
        {
            Config = config;
            _unitOfWork = unitOfWork;
            _storeFactory = storeFactory;
            var storageAccount = CloudStorageAccount.Parse(Config.ConnectionString);
            Client = storageAccount.CreateCloudTableClient();
        }

        public IAzureTableRepositoryConfig Config { get; private set; }
        public CloudTableClient Client { get; private set; }

        // By doing this delegation rather than directly exposing the UnitOfWork, we enable the scenario of changing UnitOfWork out from 
        // under the Repository instances without them knowing or needing to care.
        #region IRegisterEntites
        public TEntity RegisterAdd<TEntity>(TEntity entity) where TEntity : class, new()
        {
            EnsureTableAndWriterForEntityType<TEntity>();
            return _unitOfWork.RegisterAdd(entity);
        }

        public void RegisterDelete<TEntity>(TEntity entity) where TEntity : class, new()
        {
            EnsureTableAndWriterForEntityType<TEntity>();
            _unitOfWork.RegisterDelete(entity);
        }

        public IEnumerable<TEntity> RegisterForUpdates<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new()
        {
            EnsureTableAndWriterForEntityType<TEntity>();
            return _unitOfWork.RegisterForUpdates(entities);
        }
        #endregion // IRegisterEntites

        /// <summary>
        /// Return a Lazy of CloudTable that will fetch the table reference, honoring any configured TableNamePrefix
        /// and auto-create if not exist.
        /// </summary>
        /// <param name="tableNameFunc">Needs to be a func instead of direct access to virtual because this is wired up (into Lazy objects)
        /// in the constructor where virtual calls are troublesome.</param>
        /// <returns></returns>
        public Lazy<CloudTable> RegisterTableForEntityType<TEntity>(Func<string> tableNameFunc)
        {
            if (!_tableByEntityType.TryGetValue(typeof(TEntity), out Lazy<CloudTable> result))
            {
                lock (_guard)
                {
                    if (!_tableByEntityType.TryGetValue(typeof(TEntity), out result))
                    {
                        result = new Lazy<CloudTable>(() =>
                        {
                            var tableName = ApplyTableNamePrefix(tableNameFunc());
                            var table = Client.GetTableReference(tableName);
                            table.CreateIfNotExists();

                            if (!_storeWriterByTableName.TryGetValue(tableName, out IStoreWriter storeWriter))
                            {
                                lock (_guard)
                                {
                                    if (!_storeWriterByTableName.TryGetValue(tableName, out storeWriter))
                                    {
                                        storeWriter = _storeFactory.GetStoreWriter(table);
                                        _storeWriterByTableName[tableName] = storeWriter;
                                        _unitOfWork.RegisterStoreFor<TEntity>(storeWriter);
                                    }
                                }
                            }
                            return table;
                        });

                        _tableByEntityType[typeof(TEntity)] = result;
                    }
                }
            }
            return result;
        }

        private void EnsureTableAndWriterForEntityType<TEntity>()
        {
            if (!_tableByEntityType.TryGetValue(typeof(TEntity), out Lazy<CloudTable> lazyTable))
            {
                lock (_guard)
                {
                    if (!_tableByEntityType.TryGetValue(typeof(TEntity), out lazyTable))
                    {
                        throw new InvalidOperationException($"{nameof(RegisterTableForEntityType)} has not been called for entity type [{typeof(TEntity).Name}]");
                    }
                }
            }

            // force the lazy to evaluate
            var table = lazyTable.Value;
        }

        private string ApplyTableNamePrefix(string tableName)
        {
            return string.IsNullOrWhiteSpace(Config.TableNamePrefix)
                ? tableName : $"{Config.TableNamePrefix}{tableName}";
        }
    }
}