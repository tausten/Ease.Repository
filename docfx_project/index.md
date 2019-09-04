# Ease.Repository

C#/.Net building blocks and concrete implementations of the repository and unit of work patterns compatible with both NoSQL and SQL / session-aware stores. Also includes base classes to aid in integration testing concrete repository classes, and provides default CRUD tests out-of-the-box when your concrete repository implements the required interfaces and your corresonding unit test class inherits from one of the available abstract bases. 

Concrete implementations may be expanded over time, but will start with AzureTable, Redis, EntityFramework, and NHibernate (in that order).

Ease.Repository targets .NET Standard 2.0+ ([coverage](https://docs.microsoft.com/en-us/dotnet/standard/net-standard#net-implementation-support): .NET Framework 4.6.1, .NET Core 2.0+, and later Mono, Xamarin and UWP targets).

# Installing via NuGet
The baseline abstractions - if you need or want to develop your own concrete implementations:

`Install-Package Ease.Repository`

[![Nuget](https://img.shields.io/nuget/v/Ease.Repository.svg)](https://www.nuget.org/packages/Ease.Repository/)

Off-the-shelf concrete implementations (these would be what you'd typically integrate with, rather than the baseline):

[`Install-Package Ease.Repository.AzureTable`](https://www.nuget.org/packages/Ease.Repository.AzureTable/)

[`[Coming Soon] Install-Package Ease.Repository.Redis [Coming Soon]`](https://www.nuget.org/packages/Ease.Repository.Redis/)

[`[Coming Soon] Install-Package Ease.Repository.EntityFramework [Coming Soon]`](https://www.nuget.org/packages/Ease.Repository.EntityFramework/)

[`[Coming Soon] Install-Package Ease.Repository.NHibernate [Coming Soon]`](https://www.nuget.org/packages/Ease.Repository.NHibernate/)

And integration test base class support providing default sets of CRUD verification:


[`Install-Package Ease.Repository.Test`](https://www.nuget.org/packages/Ease.Repository.Test/)

[`Install-Package Ease.Repository.AzureTable.Test`](https://www.nuget.org/packages/Ease.Repository.AzureTable.Test/)

[`[Coming Soon] Install-Package Ease.Repository.Redis.Test [Coming Soon]`](https://www.nuget.org/packages/Ease.Repository.Redis.Test/)

[`[Coming Soon] Install-Package Ease.Repository.EntityFramework.Test [Coming Soon]`](https://www.nuget.org/packages/Ease.Repository.EntityFramework.Test/)

[`[Coming Soon] Install-Package Ease.Repository.NHibernate.Test [Coming Soon]`](https://www.nuget.org/packages/Ease.Repository.NHibernate.Test/)


## Release notes

| **Latest Versioned Build Tests** | **Latest PR Build Tests** |
| --- | --- |
| [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/easeoss/Ease.Repository/10.svg)](https://dev.azure.com/easeoss/Ease.Repository/_build?definitionId=10) | [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/easeoss/Ease.Repository/9.svg)](https://dev.azure.com/easeoss/Ease.Repository/_build?definitionId=9) |

+ The [change log](https://github.com/tausten/Ease.Repository/blob/master/CHANGELOG.md) summarizes changes by release.
+ We tag Pull Requests and Issues with [milestones](https://github.com/tausten/Ease.Repository/milestones) which match to nuget package release numbers.
+ Breaking changes will be called out in the wiki with simple notes on any necessary steps to upgrade.

# Using Ease.Repository

Quick-start information will be in the [Articles](articles/intro.md), and full [API documentation is found here](api/index.md).

# 3rd Party Libraries and Contributions
A special thank you to the authors and maintainers of these libraries that helped to make Ease.Repository* possible:
* [ChangeTracking](https://www.nuget.org/packages/ChangeTracking/) - for the BestEffortUnitOfWork entity update tracking
* [AutoFixture](https://www.nuget.org/packages/AutoFixture) - excellent test fixture management
* [FakeItEasy](https://www.nuget.org/packages/FakeItEasy/) - wonderful mocking framework 
* [AutoFixture.FakeItEasy](https://www.nuget.org/packages/AutoFixture.AutoFakeItEasy/) - a perfect marriage, providing a very elegant auto-mocking pattern implementation

# Acknowledgements

* TODO - add helper acknowledgements here

# License

Licensed under the terms of the [MIT License](https://github.com/tausten/Ease.Repository/blob/master/LICENSE)
