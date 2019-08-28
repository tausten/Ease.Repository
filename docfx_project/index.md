# Ease.Repository

C#/.Net building blocks and concrete implementations of the repository and unit of work patterns compatible with both NoSQL and SQL / session-aware stores. Also includes base classes to aid in integration testing concrete repository classes, and provides default CRUD tests out-of-the-box when your concrete repository implements the required interfaces and your corresonding unit test class inherits from one of the available abstract bases. 

Concrete implementations may be expanded over time, but will start with AzureTable, Redis, EntityFramework, and NHibernate (in that order).

Ease.Repository targets .NET Standard 2.0+ ([coverage](https://docs.microsoft.com/en-us/dotnet/standard/net-standard#net-implementation-support): .NET Framework 4.6.1, .NET Core 2.0+, and later Mono, Xamarin and UWP targets).

# Installing via NuGet

`Install-Package Ease.Repository`

[![Nuget](https://img.shields.io/nuget/v/Ease.Repository.svg)](https://www.nuget.org/packages/Ease.Repository/)

## Release notes

| **Latest Tagged Build Tests** | **Latest PR Build Tests** |
| --- | --- |
| [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/easeoss/Ease.Repository/10.svg)](https://dev.azure.com/easeoss/Ease.Repository/_build?definitionId=10) | [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/easeoss/Ease.Repository/9.svg)](https://dev.azure.com/easeoss/Ease.Repository/_build?definitionId=9) |

+ The [change log](https://github.com/tausten/Ease.Repository/blob/master/CHANGELOG.md) summarizes changes by release.
+ We tag Pull Requests and Issues with [milestones](https://github.com/tausten/Ease.Repository/milestones) which match to nuget package release numbers.
+ Breaking changes will be called out in the wiki with simple notes on any necessary steps to upgrade.

# Using Ease.Repository

Quick-start information will be in the [Articles](articles/intro.md), and full [API documentation is found here](api/index.md).

# 3rd Party Libraries and Contributions

* TODO - reference the various mock framework projects here (and any other libs, but aim to keep lib deps light)

# Acknowledgements

* TODO - add acknowledgements here

# License

Licensed under the terms of the [MIT License](https://github.com/tausten/Ease.Repository/blob/master/LICENSE)
