# Repository + Unit of Work Pattern Implementation

The *Repository Pattern* provides an abstraction of operations against an underlying data store. General descriptions, benefits, disadvantages, and religious wars can easily be found online: 

* https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design
* https://programmingwithmosh.com/net/common-mistakes-with-the-repository-pattern/
* https://dev.to/kylegalbraith/getting-familiar-with-the-awesome-repository-pattern--1ao3
* https://www.infoworld.com/article/3117713/design-patterns-that-i-often-avoid-repository-pattern.html

And the *Unit of Work Pattern* provides an abstraction around gathering multiple operations into a logical business transaction:

* https://martinfowler.com/eaaCatalog/unitOfWork.html

The combination of these two patterns can limit the proliferation of data-store / persistence-specific code across domain logic. Instead, these details can be constrained to a small family of classes.

## Ease.Repository Goals
The following are the set of goals / pressures being balanced in this particular incarnation and orchestration of these patterns:

* boilerplate CRUD operations should be taken care of by infrastructure
    * this includes support for automated integration testing of concrete Repository class implementations
* no explicit `Update` method is required or exposed
    * the infrastructure should automatically know if/when entities returned by the repository have been modified, and subsequently orchestrate the ultimate writing back to the underlying store
* the *UnitOfWork* should be able to manage a transaction that consists of a mix of entites spanning multiple repositories, repository types, and underlying store types
    * eg. my business transaction should be able to combine operations against an EntityFramework-backed store as well as updates to a Redis store, AzureTable, etc..
* the framework should be extensible so that additional underlying stores can be supported easily by end-users as well as library maintainers

## The Building Blocks
In the `Ease.Repository` implementation of these patterns, the primary conceptual building blocks are:

### `Store` + `StoreFactory`
* a thin facade over an actual underlying data store, ORM, etc... (eg. AzureTable, Redis, EntityFramework, NHibernate, in-memory dictionaries...)
* should be paired with a *StoreFactory* that the *RepositoryContext* can use to obtain a store initialized with the appropriate connection / session for it to do its work

### `RepositoryContext`
* tied directly to a specific set of configuration parameters 
* manages a connection to a specific instance of the underlying store.
* depends on a *UnitOfWork* (i.e. has one injected, and can register operations with it)
* ~~may act as a factory for *Repositories*~~  DO NOT make the context a factory for *Repositories*
* should act as a factory for the corresponding *Store* (because it'll need to inject the managed connection / client abstraction into the *Store*)
* for transaction-aware *Stores*:
    * begin the underlying store transaction 
    * configure the *UnitOfWork*'s corresponding operations for completion / rollback of underlying store transaction

### `Entity`
* the *Store*'s representation of a domain class
* eg. a "CustomerEntity", "ProductEntity", etc...
* the thing managed by a particular *Repository* class

### `Repository`
* one per *Entity* type (perhaps including extra "book-keeping"-like adjunct entities) to be managed within a particular type of underlying store
* knows how to use an underlying *Store* to perform the CRUD operations for the managed *Entity* type
* registers these CRUD-related handlers with the *UnitOfWork* so that the UoW can know what to do when the time comes to either commit the business transaction, or roll it back

### [Optional] `RepositoryFactory`
* in scenario where the same *Repository* type may need to be able to talk to different underlying *Store*s, the application may choose to use a factory pattern to accomplish this
* alternatively, concrete repositories could be made generically templatized on specially-typed *RepositoryContext* or tag interfaces if the differentiation is compile-time resolvable

### `UnitOfWork`
* general-purpose business transaction not tied to any particular fixed set of *Repositories*, underlying *Stores*, etc...
* it does not know how to use a *Store* to perform an operation... instead, it knows how to track fundamental operations (*Add*, *Update*, *Remove*) and then perform the appropriate operations on affected *Entity* instances once the *UnitOfWork* is being *Completed*
* it knows how to orchestrate undo of updates if the *UnitOfWork* is being discarded without completion (i.e. an effective *Rollback* of the business transaction)

## Orchestration (Direct Repository Injection)
1. Begin Scope (i.e. the thing governing lifetime of the *UnitOfWork*)
1. *UnitOfWork* is allocated (implicitly beginning it)
1. For each kind of underlying store that would be involved (driven by DI resolution):
    1. Allocate corresponding *RepositoryContext*, injecting the *UnitOfWork*, underlying *Store*
    1. [optional] If underlying *Store* is transactional:
        1. Begin underlying *Store* transaction
        1. Register *Commit* and *Rollback* actions with *UnitOfWork*
1. On resolving a *Repository*
    1. the *Repository* has the *RepositoryContext* injected
    1. the *Repository* registers its managed *Entity* type(s) with the *RepositoryContext*
    1. the *RepositoryContext* registers the appropriate *Store* implementations of *Add*, *Update*, and *Remove* with the *UnitOfWork*
1. In the *Repository*'s CRUD implementations
    1. the *Repository* accesses the *current* session / client abstraction via the *RepositoryContext* in order to implement fetch / query operations
    1. the *Repository* registers any newly *Add*ed, *Retrieve*d, or *Remove*d entity with the *RepositoryContext*
    1. the *RepositoryContext* delegates such registrations to its *UnitOfWork*
        * this means that technically, the context could operate across multiple consecutive *UnitsOfWork* if so desired
1. On Scope ending, consider the state of the *UnitOfWork* 
    1. if it was a success, this Scope-managing component calls the *UnitOfWork*'s Complete method
        * *UnitOfWork* walks through the tracked operations in sequence, using the registered handlers to perform each
        * if all operations completed successfully consider completion finished
        * if an operation fails, then work backward through the appropriate undo actions, including any transactional rollbacks possible
    1. if it was a failure, then the component calls *Dispose* on the *UnitOfWork* without calling the Complete method
        * *UnitOfWork* rolls back any registered transactions

## Orchestration (Adjusted for RepositoryFactory)
The same orchestration workflow as before applies with the following minor adjustments:
* instead of domain class directly depending on *Repository*, have a *RepositoryFactory* injected
* the domain knows how to select among multiple *RepositoryContext*s for a given underlying *Store* type
* when the domain behavior needs a particular *Repository*:
    * *RepositoryFactory.Get{TRepository}(RepositoryContext)*
* when done with the *Repository*:
    * *RepositoryFactory.Release(Repository)*

## Concrete Implementations
Most, if not all, of the orchestration above is taken care of by the classes provided in the `Ease.Repository.*` packages for the corresponding underlying *Store*. The bootstrapping articles for each is listed below:
* Ease.Repository.AzureTable
* [planned] Ease.Repository.EntityFramework
* [planned] Ease.Repository.Redis
* [planned] Ease.Repository.NHibernate