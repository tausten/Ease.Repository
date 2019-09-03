//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Microsoft.Azure.Cosmos.Table;

namespace Ease.Repository.AzureTable
{
    public interface IAzureTableStoreFactory
    {
        IStoreWriter GetStoreWriter(CloudTable table);
    }

    public class AzureTableStoreFactory : IAzureTableStoreFactory
    {
        public IStoreWriter GetStoreWriter(CloudTable table)
        {
            return new AzureTableStoreWriter(table);
        }
    }
}