//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System;
using Microsoft.Azure.Cosmos.Table;

namespace Ease.Repository.AzureTable
{
    public interface IAzureTableRepositoryContext
    {
        CloudTableClient Client { get; }
        Lazy<CloudTable> PrepareTable(Func<string> tableNameFunc);
    }

    public class AzureTableRepositoryContext : IAzureTableRepositoryContext
    {
        public AzureTableRepositoryContext(IAzureTableRepositoryConfig config)
        {
            Config = config;
            var storageAccount = CloudStorageAccount.Parse(Config.ConnectionString);
            Client = storageAccount.CreateCloudTableClient();
        }

        public IAzureTableRepositoryConfig Config { get; private set; }
        public CloudTableClient Client { get; private set; }

        /// <summary>
        /// Return a Lazy of CloudTable that will fetch the table reference, honoring any configured TableNamePrefix
        /// and auto-create if not exist.
        /// </summary>
        /// <param name="tableNameFunc"></param>
        /// <returns></returns>
        public Lazy<CloudTable> PrepareTable(Func<string> tableNameFunc)
        {
            return new Lazy<CloudTable>(() =>
            {
                var table = Client.GetTableReference(ApplyTableNamePrefix(tableNameFunc()));
                table.CreateIfNotExists();
                return table;
            });
        }

        private string ApplyTableNamePrefix(string tableName)
        {
            return string.IsNullOrWhiteSpace(Config.TableNamePrefix)
                ? tableName : $"{Config.TableNamePrefix}{tableName}";
        }
    }
}