//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using ChangeTracking;
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

        public interface ICanDoAndUndo
        {
            void Do(TestTableEntity entity);
            void Undo(TestTableEntity entity);
        }

        private enum Operation
        {
            Add,
            UndoAdd,
            UpdateOrUndoUpdate,
            Delete,
            UndoDelete
        }
        
        private IFixture _fixture;
        private AzureTableUnitOfWork<IAzureTableRepositoryContext> _sut;
        private ICanDoAndUndo _mockAddHandler;
        private ICanDoAndUndo _mockUpdateHandler;
        private ICanDoAndUndo _mockDeleteHandler;
        private TestTableEntity _rawEntity;

        private List<KeyValuePair<TestTableEntity, Operation>> _orderedOperations;
        
        [SetUp]
        public void SetUp()
        {
            _orderedOperations = new List<KeyValuePair<TestTableEntity, Operation>>();

            _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            _fixture.Freeze<IAzureTableRepositoryContext>();

            _mockAddHandler = MockHandler(Operation.Add, Operation.UndoAdd);
            _mockUpdateHandler = MockHandler(Operation.UpdateOrUndoUpdate, Operation.UpdateOrUndoUpdate);
            _mockDeleteHandler = MockHandler(Operation.Delete, Operation.UndoDelete);
            
            _sut = _fixture.Freeze<AzureTableUnitOfWork<IAzureTableRepositoryContext>>();
            
            _rawEntity = new TestTableEntity();
        }

        private ICanDoAndUndo MockHandler(Operation doOperation, Operation undoOperation)
        {
            var handler = A.Fake<ICanDoAndUndo>();
            A.CallTo(() => handler.Do(A<TestTableEntity>._))
                .Invokes((TestTableEntity e) => _orderedOperations.Add(
                    new KeyValuePair<TestTableEntity, Operation>(e, doOperation)));
            
            A.CallTo(() => handler.Undo(A<TestTableEntity>._))
                .Invokes((TestTableEntity e) => _orderedOperations.Add(
                    new KeyValuePair<TestTableEntity, Operation>(e, undoOperation)));
            
            return handler;
        }

        private TestTableEntity RegisterAdd(TestTableEntity entity)
        {
            return _sut.RegisterAdd(entity, _mockAddHandler.Do, _mockAddHandler.Undo);
        }

        private TestTableEntity RegisterForUpdates(TestTableEntity entity)
        {
            return _sut.RegisterForUpdates(new[]{entity}, _mockUpdateHandler.Do).SingleOrDefault();
        }

        private IEnumerable<TestTableEntity> RegisterForUpdates(params TestTableEntity[] entities)
        {
            return _sut.RegisterForUpdates(entities, _mockUpdateHandler.Do);
        }

        private TestTableEntity RegisterDelete(TestTableEntity entity)
        {
            _sut.RegisterDelete(entity, _mockDeleteHandler.Do, _mockDeleteHandler.Undo);
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
            RegisterDelete(_rawEntity);
            
            // Act
            await _sut.CompleteAsync();
            
            // Assert
            A.CallTo(() => _mockDeleteHandler.Do(_rawEntity)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockDeleteHandler.Undo(_rawEntity)).MustNotHaveHappened();
        }
        
        [Test]
        public async Task Complete_When_Object_Added_Calls_PersistAction()
        {
            // Arrange
            RegisterAdd(_rawEntity);
            
            // Act
            await _sut.CompleteAsync();
            
            // Assert
            A.CallTo(() => _mockAddHandler.Do(_rawEntity)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _mockAddHandler.Undo(_rawEntity)).MustNotHaveHappened();
        }

        [Test]
        public async Task Complete_When_Object_Update_Registered_With_No_Changes_Does_Nothing()
        {
            // Arrange
            _rawEntity.PartitionKey = "jiggy";
            _rawEntity.RowKey = "wiggy";
            
            RegisterForUpdates(_rawEntity);
            
            // Act
            await _sut.CompleteAsync();
            
            // Assert
            A.CallTo(() => _sut.Context.Client).MustNotHaveHappened();
            A.CallTo(() => _mockUpdateHandler.Do(A<TestTableEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _mockUpdateHandler.Undo(A<TestTableEntity>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task Complete_When_Object_Update_Registered_With_Changes_Calls_UpdateAction_With_Changes()
        {
            // Arrange
            _rawEntity.PartitionKey = "jiggy";
            _rawEntity.RowKey = "wiggy";
            
            var trackedEntity = RegisterForUpdates(_rawEntity);

            trackedEntity.SomeString = "Wagga";
            trackedEntity.SomeInt = 12;
            
            // Act
            await _sut.CompleteAsync();
            
            // Assert
            A.CallTo(() => _mockUpdateHandler.Do(A<TestTableEntity>.That.Matches(x => 
                    x.PartitionKey == _rawEntity.PartitionKey
                    && x.RowKey == _rawEntity.RowKey
                    && x.SomeString == "Wagga"
                    && x.SomeInt == 12
                    && !(x is IChangeTrackable<TestTableEntity>)
                    )))
                .MustHaveHappenedOnceExactly();
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
            var firstEntityToUpdate = RegisterForUpdates(new TestTableEntity {RowKey = idOfFirstEntityToUpdate});
            var secondEntityToUpdate = RegisterForUpdates(new TestTableEntity {RowKey = idOfSecondEntityToUpdate});
            
            // Act
            var expectedFirstDelete = RegisterDelete(new TestTableEntity {RowKey = idOfFirstEntityToDelete});
            
            firstEntityToUpdate.SomeString = "Hello world!";
            var expectedFirstUpdate = firstEntityToUpdate.GetCurrentSnapshot();
            
            var expectedFirstAdd = RegisterAdd(new TestTableEntity {RowKey = idOfFirstEntityToAdd}).GetCurrentSnapshot();

            secondEntityToUpdate.SomeInt = 1234;
            var expectedSecondUpdate = secondEntityToUpdate.GetCurrentSnapshot();
            
            var expectedSecondAdd = RegisterAdd(new TestTableEntity {RowKey = idOfSecondEntityToAdd}).GetCurrentSnapshot();
            
            var expectedSecondDelete = RegisterDelete(new TestTableEntity {RowKey = idOfSecondEntityToDelete});
            
            await _sut.CompleteAsync();
            
            // Assert
            _orderedOperations.Count.Should().Be(6);

            AssertOperation(0, Operation.Delete, expectedFirstDelete);
            AssertOperation(1, Operation.UpdateOrUndoUpdate, expectedFirstUpdate);
            AssertOperation(2, Operation.Add, expectedFirstAdd);
            AssertOperation(3, Operation.UpdateOrUndoUpdate, expectedSecondUpdate);
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