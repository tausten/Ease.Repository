//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using ChangeTracking;
using Ease.Repository;
using Ease.Repository.AzureTable;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

namespace HelperAPIs.Impl.Test.Data
{
    public class AzureTableUnitOfWorkTests
    {
        public class TestTableEntity : AzureTrackableTableEntity
        {
            public virtual string SomeString { get; set; }
            public virtual int SomeInt { get; set; }

            public TestTableEntity GetCurrentSnapshot()
            {
                var trackable = this as IChangeTrackable<TestTableEntity>;
                return (null != trackable) ? trackable.GetCurrent() : this;
            }
        }

        private enum Operation
        {
            Add,
            Update,
            Delete
        }
        
        private IFixture _fixture;
        private AzureTableUnitOfWork<IAzureTableRepositoryContext> _sut;
        private IStoreWriter _storeWriter;
        private TestTableEntity _rawEntity;

        private List<KeyValuePair<TestTableEntity, Operation>> _orderedOperations;
        
        [SetUp]
        public void SetUp()
        {
            _orderedOperations = new List<KeyValuePair<TestTableEntity, Operation>>();

            _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            _fixture.Freeze<IAzureTableRepositoryContext>();
            _storeWriter = MockStoreWriter();
            _sut = _fixture.Freeze<AzureTableUnitOfWork<IAzureTableRepositoryContext>>();

            _sut.RegisterStoreFor<TestTableEntity>(_storeWriter);

            _rawEntity = new TestTableEntity();
        }

        private IStoreWriter MockStoreWriter()
        {
            var storeWriter = A.Fake<IStoreWriter>();

            A.CallTo(() => storeWriter.Add(A<TestTableEntity>._))
                .Invokes((TestTableEntity e) => _orderedOperations.Add(
                    new KeyValuePair<TestTableEntity, Operation>(e, Operation.Add)));

            A.CallTo(() => storeWriter.Update(A<TestTableEntity>._))
                .Invokes((TestTableEntity e) => _orderedOperations.Add(
                    new KeyValuePair<TestTableEntity, Operation>(e, Operation.Update)));

            A.CallTo(() => storeWriter.Delete(A<TestTableEntity>._))
                .Invokes((TestTableEntity e) => _orderedOperations.Add(
                    new KeyValuePair<TestTableEntity, Operation>(e, Operation.Delete)));
            
            return storeWriter;
        }

        private TestTableEntity RegisterSingleForUpdates(TestTableEntity entity)
        {
            return _sut.RegisterForUpdates(new[] { entity }).First();
        }

        private TestTableEntity RegisterDelete(TestTableEntity entity)
        {
            _sut.RegisterDelete(entity);
            return entity;
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public async Task Complete_When_No_Registrations_Does_Nothing()
        {
            // Arrange
            // Act
            await _sut.CompleteAsync();
            
            // Assert
            A.CallTo(() => _sut.Context.Client).MustNotHaveHappened();
        }
        
        [Test]
        public async Task Complete_When_Object_Deleted_Calls_DeleteAction()
        {
            // Arrange
            _sut.RegisterDelete(_rawEntity);
            
            // Act
            await _sut.CompleteAsync();
            
            // Assert
            A.CallTo(() => _storeWriter.Delete(_rawEntity)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _storeWriter.Add(A<TestTableEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Update(A<TestTableEntity>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task Complete_When_Object_Added_Calls_PersistAction()
        {
            // Arrange
            _sut.RegisterAdd(_rawEntity);
            
            // Act
            await _sut.CompleteAsync();
            
            // Assert
            A.CallTo(() => _storeWriter.Add(_rawEntity)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _storeWriter.Delete(A<TestTableEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Update(A<TestTableEntity>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task Complete_When_Object_Update_Registered_With_No_Changes_Does_Nothing()
        {
            // Arrange
            _rawEntity.PartitionKey = "jiggy";
            _rawEntity.RowKey = "wiggy";

            _sut.RegisterForUpdates(new[] { _rawEntity });
            
            // Act
            await _sut.CompleteAsync();
            
            // Assert
            A.CallTo(() => _sut.Context.Client).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Add(A<TestTableEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Update(A<TestTableEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Delete(A<TestTableEntity>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task Complete_When_Object_Update_Registered_With_Changes_Calls_UpdateAction_With_Changes()
        {
            // Arrange
            _rawEntity.PartitionKey = "jiggy";
            _rawEntity.RowKey = "wiggy";
            
            var trackedEntity = RegisterSingleForUpdates(_rawEntity);

            trackedEntity.SomeString = "Wagga";
            trackedEntity.SomeInt = 12;
            
            // Act
            await _sut.CompleteAsync();
            
            // Assert
            A.CallTo(() => _storeWriter.Update(A<TestTableEntity>.That.Matches(x => 
                    x.PartitionKey == _rawEntity.PartitionKey
                    && x.RowKey == _rawEntity.RowKey
                    && x.SomeString == "Wagga"
                    && x.SomeInt == 12
                    && !(x is IChangeTrackable<TestTableEntity>)
                    )))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _storeWriter.Add(A<TestTableEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Delete(A<TestTableEntity>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task Complete_Honors_Order_When_Multiple_Operations()
        {
            // Arrange
            const string idOfFirstEntityToAdd = "first add";
            const string idOfSecondEntityToAdd = "second add";

            const string idOfFirstEntityToUpdate = "first update";
            const string idOfSecondEntityToUpdate = "second update";

            const string idOfFirstEntityToDelete = "first delete";
            const string idOfSecondEntityToDelete = "second delete";

            // NOTE: The update registrations don't immediately - they get applied on actual edits of the returned entities
            var firstEntityToUpdate = RegisterSingleForUpdates(new TestTableEntity {RowKey = idOfFirstEntityToUpdate});
            var secondEntityToUpdate = RegisterSingleForUpdates(new TestTableEntity {RowKey = idOfSecondEntityToUpdate});
            
            // Act
            var expectedFirstDelete = RegisterDelete(new TestTableEntity {RowKey = idOfFirstEntityToDelete});
            
            firstEntityToUpdate.SomeString = "Hello world!";
            var expectedFirstUpdate = firstEntityToUpdate.GetCurrentSnapshot();
            
            var expectedFirstAdd = _sut.RegisterAdd(new TestTableEntity {RowKey = idOfFirstEntityToAdd}).GetCurrentSnapshot();

            secondEntityToUpdate.SomeInt = 1234;
            var expectedSecondUpdate = secondEntityToUpdate.GetCurrentSnapshot();
            
            var expectedSecondAdd = _sut.RegisterAdd(new TestTableEntity {RowKey = idOfSecondEntityToAdd}).GetCurrentSnapshot();
            
            var expectedSecondDelete = RegisterDelete(new TestTableEntity {RowKey = idOfSecondEntityToDelete});
            
            await _sut.CompleteAsync();
            
            // Assert
            _orderedOperations.Count.Should().Be(6);

            AssertOperation(0, Operation.Delete, expectedFirstDelete);
            AssertOperation(1, Operation.Update, expectedFirstUpdate);
            AssertOperation(2, Operation.Add, expectedFirstAdd);
            AssertOperation(3, Operation.Update, expectedSecondUpdate);
            AssertOperation(4, Operation.Add, expectedSecondAdd);
            AssertOperation(5, Operation.Delete, expectedSecondDelete);
        }

        private void AssertOperation(int index, Operation expectedOperation, TestTableEntity expectedEquivalentEntity)
        {
            _orderedOperations[index].Value.Should().Be(expectedOperation, $"[{index}] was wrong operation");
            _orderedOperations[index].Key.Should().BeEquivalentTo(expectedEquivalentEntity, $"[{index}] didn't match");
        }
    }
}