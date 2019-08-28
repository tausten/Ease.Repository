# Ease.Repository

C#/.Net building blocks and concrete implementations of the repository and unit of work patterns compatible with both NoSQL and SQL / session-aware stores. Also includes base classes to aid in integration testing concrete repository classes, and provides default CRUD tests out-of-the-box when your concrete repository implements the required interfaces and your corresonding unit test class inherits from one of the available abstract bases. 

Concrete implementations may be expanded over time, but will start with AzureTable, Redis, EntityFramework, and NHibernate (in that order).

Ease.Repository targets .NET Standard 2.0+ ([coverage](https://docs.microsoft.com/en-us/dotnet/standard/net-standard#net-implementation-support): .NET Framework 4.6.1, .NET Core 2.0+, and later Mono, Xamarin and UWP targets).

For detailed [documentation, please go here...](https://tausten.github.io/Ease.Repository/)

## Installing via NuGet

`Install-Package Ease.Repository`

[![Nuget](https://img.shields.io/nuget/v/Ease.Repository.svg)](https://www.nuget.org/packages/Ease.Repository/)

## Build Status

| **Latest Tagged Build Tests** | **Latest PR Build Tests** |
| --- | --- |
| [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/easeoss/Ease.Repository/7.svg)](https://dev.azure.com/easeoss/Ease.Repository/_build?definitionId=7) | [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/easeoss/Ease.Repository/2.svg)](https://dev.azure.com/easeoss/Ease.Repository/_build?definitionId=2) |


## License

Licensed under the terms of the [MIT License](LICENSE)
