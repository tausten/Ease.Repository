# Ease.Repository.AzureTable
Concrete implementation of `Ease.Repository` for AzureTable as the underlying *Store*. Below is a discussion of how to leverage the package.

This implementation relies upon the [Microsoft.Azure.Cosmos.Table](https://www.nuget.org/packages/Microsoft.Azure.Cosmos.Table) NuGet package, and as such, your concrete implementations of `Repositories` will be working with things like `CloudTable`, and your `Entity` classes must implement `ITableEntity` (base class provided to make this easier).

## The Building Blocks
The following sections describe the AzureTable-specific implementations of the building blocks for the patterns.

### `Store` + `StoreFactory`
* The `Store` is implemented by [`AzureTableStoreWriter`](xref:Ease.Repository.AzureTable.AzureTableStoreWriter)
* The `StoreFactory` is implemented by [`AzureTableStoreFactory`](xref:Ease.Repository.AzureTable.AzureTableStoreFactory)

### `RepositoryContext`
* Interface is [`IAzureTableRepositoryContext`](xref:Ease.Repository.AzureTable.IAzureTableRepositoryContext)
* Concrete implementation / base class is [`AzureTableRepositoryContext`](xref:Ease.Repository.AzureTable.AzureTableRepositoryContext)
* In the trivial single underlying store configuration case, you can use the concrete implementation as-is
* In a configuration where you may wish to work with multiple underlying AzureTable storage accounts, you can create multiple child classes that provide the appropriate different [`IAzureTableRepositoryConfig`](xref:Ease.Repository.AzureTable.IAzureTableRepositoryConfig) implementations

### `Entity`
* All entities should inherit from [`AzureTableTrackableEntity`](xref:Ease.Repository.AzureTable.AzureTableTrackableEntity)
* This base class implements the requirements of the  `ITableEntity`
* All public properties must be virtual to permit the dynamic proxy-based change tracking
* Keep these as brain-dead simple POCOs

### `Repository`
* Concrete repository classes should inherit from [`AzureTableRepository`](xref:Ease.Repository.AzureTable.AzureTableRepository`2)

### `UnitOfWork`
* Must be the [`IBestEffortUnitOfWork`](xref:Ease.Repository.IBestEffortUnitOfWork) (implemented by [`BestEffortUnitOfWork`](xref:Ease.Repository.BestEffortUnitOfWork))
* Relies upon a dynamic proxy implementation, and therefor the concrete `Repository` implementations must remember to call [`IAzureTableRepositoryContext.RegisterForUpdates(...)`](xref:Ease.Repository.AzureTable.AzureTableRepositoryContext.RegisterForUpdates``1(System.Collections.Generic.IEnumerable{``0})) to wrap any retrieved entities in dynamic proxy and return those instead of the entities directly

## Integrating With Your Application

### Dependency Injection framework registration

* Identify the appropriate `Scope` / life cycle of the `UnitOfWork`
* All of the following services should be tied to this `Scoped` life cycle:
    * [`IBestEffortUnitOfWork`](xref:Ease.Repository.IBestEffortUnitOfWork) (the same registration should also be a provider of [`IUnitOfWork`](xref:Ease.Repository.IUnitOfWork))
    * [`IAzureTableRepositoryContext`](xref:Ease.Repository.AzureTable.IAzureTableRepositoryContext)
    * All of your concrete `Repository` implementations
* The following services can have a longer life cycle (including *Singleton*):
    * Implementations of [`IAzureTableRepositoryConfig`](xref:Ease.Repository.AzureTable.IAzureTableRepositoryConfig)
    * [`IAzureTableStoreFactory`](xref:Ease.Repository.AzureTable.AzureTableStoreFactory)

### Data Model

* Can (and should) be something as simple as:

```csharp
    public interface MyEntity : AzureTableTrackableEntity
    {
        public virtual int SomeInt { get; set; }
        public virtual string SomeString { get; set; }
    }
```

### Concrete `Repository` Implementations

* Should define their own interface, and then implement that (allows unit-testing of domain classes that depend on the repositories)... eg:

```csharp
    public interface IMyRepo : IAzureTableRepository<MyEntity> 
    {
        // TODO: Declare any extra methods beyond basic CRUD here
    }
```

* Should inherit from the base class to automatically implement CRUD including entity registration for change tracking of those operations... eg:

```csharp
    public class MyRepo : 
        AzureTableRepository<AzureTableRepositoryContext, MyEntity>,
        IMyRepo
    {
        public MyRepo(AzureTableRepositoryContext context) : base(context) { }

        protected override string CalculatePartitionKeyFor(MyEntity entity)
        {
            return ??? // TODO: Provide some kind of computed PartitionKey string
        }
    }
```

* If `IMyRepo` includes additional methods, then implement those, and make use of the base class's [`.Table`](xref:Ease.Repository.AzureTable.AzureTableRepository`2.Table) property as needed...  to perform queries / operations for the entity
     * You  _**must**_  call [`.Context.RegisterForUpdates(...)`](xref:Ease.Repository.AzureTable.AzureTableRepositoryContext.RegisterForUpdates``1(System.Collections.Generic.IEnumerable{``0})) to wrap any `MyEntity` instances obtained from the queries, and only return the wrapped entities in order for change tracking to work

```csharp
    public class MyRepo : // the inheritence bits
    {
        // The ctor and CalculatePartitionKeyFor(...) implementation

        public IEnumerable<MyEntity> MyGreatQuery(/* some query parameters */)
        {
            var query = new TableQuery<MyEntity>();
            // TODO: parameterize the query however you need...
            var entities = Table.Value.ExecuteQuery(query);
            return Context.RegisterForUpdates(entities);
        }
    }
```

* If you don't want the table name to be the entity Type name, then override the [`.TableName`](xref:Ease.Repository.AzureTable.AzureTableRepository`2.TableName) property
    * NOTE: The [`IAzureTableRepositoryConfig.TableNamePrefix`](xref:Ease.Repository.AzureTable.IAzureTableRepositoryConfig.TableNamePrefix) will be prepended to your `TableName` 

### AzureTable Storage Configuration

The default implementation of [IAzureTableRepositoryConfig](xref:Ease.Repository.AzureTable.AzureTableRepositoryConfig) builds on (and depends upon) the `Microsoft.Extensions.Configuration.IConfiguration` and expects to find the following config parameters:

* `{configSectionPrefix}Azure:StorageConnectionString`
    * default: `"UseDevelopmentStorage=true"`
* `{configSectionPrefix}Azure:TableNamePrefix`
    * default: `"Dev"`

By default, there is no `{configSectionPrefix}`, but the extra constructor parameter may be used to provide one such that multiple storage configs can coexist, and be used to 

### Using the `Repositories` and `IUnitOfWork`

Now that you've gotten all the players implemented and registered with your DI framework, it's time to actually use them.

1. Any domain classes can simply depend on your `Repository` interfaces (eg. `IMyRepo`)

1. Whatever component is managing the completion of the `UnitOfWork` should depend on [`IUnitOfWork`](xref:Ease.Repository.IUnitOfWork)
    * if / when all has gone well and the component wants the operations to be executed (eg. made persistent against the underlying `Store`), then call [`await IUnitOfWork.CompleteAsync()`](xref:Ease.Repository.IUnitOfWork.CompleteAsync)
    * regardless of success or failure, the `IUnitOfWork` must be `Disposed` when done with it...
        * if `CompleteAsync` was not called before `Dispose`, then this amounts to a *rollback*-like operation on the business transaction (eg. all pending updates tracked by the `IUnitOfWork` will be discarded)
        * if `CompleteAsync` was called successfully, then `Dispose` just releases any cached objects (the changes have already been persisted to the `Store`)

In its simplest (though not necessarily prettiest) form, this orchestration could amount to something like:

```csharp
    public class MyDomainService 
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMyRepo _repo;

        public MyDomainService(IUnitOfWork unitOfWork, IMyRepo repo)
        { 
            _unitOfWork = unitOfWork;
            _repo = repo;
        }

        public async Task DoSomethingTo(ITableEntity compositeKey)
        {
            // NOTE: compositeKey could be provided as an instance of `AzureTableEntityKey`
            var entity = _repo.Get(compositeKey);
            entity.SomeString = someNewValue;
            // TODO: other operations that may affect the entity

            // When we're finally done and ready to complete the unit of work...
            await _unitOfWork.CompleteAsync();
        }
    }
```

Often times, you'll have created some infrastructural helper components for managing exceptions, retries, etc...  and that component may be the one to decide if / when to call `CompleteAsync`. In still more sophisticated infrastructure scenarios, such a manager component could be the thing determining the scope of the IUnitOfWork and related components, enabling retries and other fault tolerant mechanisms. Such infrastructure is not provided directly by `Ease.Repository.*`, but should be possible.

### Integration Testing the Data Layer

Even basic CRUD operations can be troublesome, and it is useful to make sure they actually work against a real underlying `Store` before relying upon the `Repositories` in your domain logic. As the queries and operations against the store increase in complexity beyond basic CRUD, this becomes even more critical.

To support integration testing of your repositories, several base classes and infrastructure are available in the `Ease.Repository*.Test` packages. To remain test framework-agnostic, some gymnastics are required, but it should prove to be a low-cost tradeoff for the benefits.

The main requirement for these integration tests is that an AzureStorage account be available. By default, the local dev AzureStorage emulator connection string is used, so the tests would require the emulator to be running.

Here is a sample of a simple `Repository` integration test:

```csharp
    public class MyRepoTests
        : AzureTableRepositoryTests<IAzureTableRepositoryContext, MyEntity, MyRepo>
    {
        protected override void PrepareDependenciesForContext(IFixture fixture)
        {
            base.PrepareDependenciesForContext(fixture);

            var config = fixture.Freeze<IConfiguration>();
            A.CallTo(() => config["Main:Azure:StorageConnectionString"])
                .Returns("UseDevelopmentStorage=true");
            
            A.CallTo(() => config["Main:Azure:TableNamePrefix"])
                .Returns(TestTableNamePrefix);

            // We dance this little jig for the case where we'd be registering 
            // a particular concrete implementation of a service with the DI container.
            var context = fixture.Freeze<AzureTableRepositoryContext>();
            fixture.Inject<IAzureTableRepositoryContext>(context);
        }

        protected override void ModifyEntity(MyEntity entityToModify)
        {
            var newSuffix = Guid.NewGuid().ToString();
            entityToModify.SomeString.Should().NotEndWith(newSuffix);
            entityToModify.SomeString += newSuffix;
        }

        protected override void AssertEntitiesAreEquivalent(MyEntity result, MyEntity reference)
        {
            result.CurrentState().Should().BeEquivalentTo(
                reference.CurrentState(), options => options
                    .Excluding(x => x.Timestamp)
                );
        }

        protected override ITableEntity NewSimpleKeyFromEntity(MyEntity entity)
        {
            return new AzureTableEntityKey 
            { 
                PartitionKey = entity.PartitionKey, 
                RowKey = entity.RowKey 
            };
        }

        [SetUp]
        public override void SetUp()
        {
            SetUp_Impl();
        }

        [TearDown]
        public override void TearDown()
        {
            TearDown_Impl();
        }

        #region Base Tests

        [Test]
        public override void List_Returns_Empty_For_No_Data()
        {
            List_Returns_Empty_For_No_Data_Impl();
        }

        [Test]
        public override async Task Add_New_Entity_And_List_RoundTrip()
        {
            await Add_New_Entity_And_Get_By_Key_RoundTrip_Impl();
        }

        [Test]
        public override void Add_Sets_Keys()
        {
            Add_Sets_Keys_Impl();
        }

        [Test]
        public override async Task Add_New_Entity_And_Get_RoundTrip()
        {
            await Add_New_Entity_And_Get_RoundTrip_Impl();
        }

        [Test]
        public override async Task Add_New_Entity_And_Get_By_Key_RoundTrip()
        {
            await Add_New_Entity_And_Get_By_Key_RoundTrip_Impl();
        }

        [Test]
        public override async Task Delete_And_Get_RoundTrip()
        {
            await Delete_And_Get_RoundTrip_Impl();
        }

        // ... more tests may be required and provided by the base class ...

        #endregion Base Tests

        // TODO: Add your repository-specific tests here (eg. for extra query-related functionality, etc...)
    }
```

Some observations:

* The [`AzureTableRepositoryTests`](xref:Ease.Repository.AzureTable.Test.AzureTableRepositoryTests`3) base class uses [AutoFixture](https://www.nuget.org/packages/AutoFixture) and [FakeItEasy](https://www.nuget.org/packages/FakeItEasy/) for mocking and an "auto-mock" pattern implementation
* There are a set of tests that are _required_ to be implemented (by the `abstract`) keyword, but default implementations are provided in the base class by `{theRequiredTest_Impl}`
    * this is done instead of just using `virtual` in order to ensure that test runners can actually find the tests by forcing you to provide a method with appropriate attribute or other such runner registration for each
* The set of required tests may grow over time (i.e. be warned when upgrading to newer versions of the package), though the needed changes will be similarly trivial
