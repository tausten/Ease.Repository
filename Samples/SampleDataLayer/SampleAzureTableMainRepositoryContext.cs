//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using Ease.Repository;
using Ease.Repository.AzureTable;
using Microsoft.Extensions.Configuration;

namespace SampleDataLayer
{
    /// <summary>
    /// Technically, you could manage more than one AzureTable-related repository context (i.e. driven by different configs
    /// potentially aimed at different storage accounts). The nested <see cref="StorageConfig"/> 
    /// class here demponstrates one way to achieve this is by creating a child class of <see cref="AzureTableRepositoryConfig"/>, 
    /// and having your own `*Context` class's constructor take this concrete type (or a tag interface variant of 
    /// <see cref="IAzureTableRepositoryConfig"/> if you wish) and then pass this to the base constructor. In this way, multiple such 
    /// configs can be registered with an IoC container and be injected into your repositories without ambiguity.
    /// 
    /// An alternative approach would be to implement some kind of factory pattern for your repositories that knows which config
    /// to provide to which repository, etc...
    /// </summary>
    public class SampleAzureTableMainRepositoryContext : AzureTableRepositoryContext
    {
        public class StorageConfig : AzureTableRepositoryConfig
        {
            public StorageConfig(IConfiguration config) : base(config, "Main") { }
        }

        public SampleAzureTableMainRepositoryContext(StorageConfig config, IBestEffortUnitOfWork unitOfWork, IAzureTableStoreFactory storeFactory) 
            : base(config, unitOfWork, storeFactory)
        { }
    }
}