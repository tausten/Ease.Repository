//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Ease.Util.Extensions;
using Microsoft.Extensions.Configuration;

namespace Ease.Repository.AzureTable
{
    public interface IAzureTableRepositoryConfig
    {
        string ConnectionString { get; }
        string TableNamePrefix { get; }
    }

    public class AzureTableRepositoryConfig : IAzureTableRepositoryConfig
    {
        private readonly IConfiguration _config;

        public AzureTableRepositoryConfig(IConfiguration config)
        {
            _config = config;
        }

        public string ConnectionString => _config["StorageConnectionString"].ToValueOr("UseDevelopmentStorage=true");
        public string TableNamePrefix => _config["Table:NamePrefix"].ToValueOr("Dev");
    }
}