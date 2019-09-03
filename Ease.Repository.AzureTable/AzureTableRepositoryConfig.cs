//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Ease.Util.Extensions;
using Microsoft.Extensions.Configuration;

namespace Ease.Repository.AzureTable
{
    /// <summary>
    /// Interface for configuration for the underlying Azure Table store to be used by repositories.
    /// </summary>
    public interface IAzureTableRepositoryConfig
    {
        string ConnectionString { get; }
        string TableNamePrefix { get; }
    }

    /// <summary>
    /// May use directly, or use as a convenient base class for capturing config of a data model that spans multiple
    /// Azure Storage accounts. For example:
    /// 
    /// <code>
    /// public class MainStorageRepositoryConfig : AzureTableRepositoryConfig
    /// {
    ///     MainStorageRepositoryConfig(IConfiguration config) : base(config, "Main") { }
    /// }
    /// 
    /// public class SecondaryStorageRepositoryConfig : AzureTableRepositoryConfig
    /// {
    ///     MainStorageRepositoryConfig(IConfiguration config) : base(config, "Secondary") { }
    /// }
    /// </code>
    /// 
    /// Then your repositories can be differentiated by repository config, and the configuration properties themselves
    /// will be in separate sections, one under "Main:Azure:{stuff}" and one under "Secondary:Azure:{stuff}".
    /// </summary>
    public class AzureTableRepositoryConfig : IAzureTableRepositoryConfig
    {
        private readonly IConfiguration _config;
        private readonly string _configSectionPrefix;

        /// <summary>
        /// Initialize the config.
        /// </summary>
        /// <param name="config">The underlying config abstraction to fetch from.</param>
        /// <param name="configSectionPrefix">[optional] The prefix to add to the config keys during lookup (permits multiple config sections to coexist).</param>
        public AzureTableRepositoryConfig(IConfiguration config, string configSectionPrefix = null)
        {
            _config = config;
            _configSectionPrefix = $"{configSectionPrefix?.Trim().TrimEnd(':')}:" ?? string.Empty;
        }

        public virtual string ConnectionString => _config[$"{_configSectionPrefix}Azure:StorageConnectionString"].ToValueOr("UseDevelopmentStorage=true");
        public virtual string TableNamePrefix => _config[$"{_configSectionPrefix}Azure:TableNamePrefix"].ToValueOr("Dev");
    }
}