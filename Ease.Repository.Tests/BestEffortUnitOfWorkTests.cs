//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using ChangeTracking;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;

namespace Ease.Repository.Tests
{
    public class BestEffortUnitOfWorkTests
    {
        public class TestEntity
        {
            public virtual string Id { get; set; }
            public virtual string SomeString { get; set; }
            public virtual int SomeInt { get; set; }

            public TestEntity GetCurrentSnapshot()
            {
                var trackable = this as IChangeTrackable<TestEntity>;
                return null != trackable ? trackable.GetCurrent() : this;
            }
        }

        private enum Operation
        {
            Add,
            Update,
            Delete
        }

        private IFixture _fixture;
        private BestEffortUnitOfWork<object> _sut;
        private IStoreWriter _storeWriter;
        private TestEntity _rawEntity;

        private List<KeyValuePair<TestEntity, Operation>> _orderedOperations;

        [SetUp]
        public void SetUp()
        {
            _orderedOperations = new List<KeyValuePair<TestEntity, Operation>>();

            _fixture = new Fixture().Customize(new AutoFakeItEasyCustomization());
            _storeWriter = MockStoreWriter();
            _sut = _fixture.Freeze<BestEffortUnitOfWork<object>>();

            _sut.RegisterStoreFor<TestEntity>(_storeWriter);

            _rawEntity = new TestEntity();
        }

        private IStoreWriter MockStoreWriter()
        {
            var storeWriter = A.Fake<IStoreWriter>();

            A.CallTo(() => storeWriter.Add(A<TestEntity>._))
                .Invokes((TestEntity e) => _orderedOperations.Add(
                    new KeyValuePair<TestEntity, Operation>(e, Operation.Add)));

            A.CallTo(() => storeWriter.Update(A<TestEntity>._))
                .Invokes((TestEntity e) => _orderedOperations.Add(
                    new KeyValuePair<TestEntity, Operation>(e, Operation.Update)));

            A.CallTo(() => storeWriter.Delete(A<TestEntity>._))
                .Invokes((TestEntity e) => _orderedOperations.Add(
                    new KeyValuePair<TestEntity, Operation>(e, Operation.Delete)));

            return storeWriter;
        }

        private TestEntity RegisterSingleForUpdates(TestEntity entity)
        {
            return _sut.RegisterForUpdates(new[] { entity }).First();
        }

        private TestEntity RegisterDelete(TestEntity entity)
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
            A.CallTo(() => _storeWriter.Add(A<TestEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Update(A<TestEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Delete(A<TestEntity>._)).MustNotHaveHappened();
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
            A.CallTo(() => _storeWriter.Add(A<TestEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Update(A<TestEntity>._)).MustNotHaveHappened();
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
            A.CallTo(() => _storeWriter.Delete(A<TestEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Update(A<TestEntity>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task Complete_When_Object_Update_Registered_With_No_Changes_Does_Nothing()
        {
            // Arrange
            _sut.RegisterForUpdates(new[] { _rawEntity });

            // Act
            await _sut.CompleteAsync();

            // Assert
            A.CallTo(() => _storeWriter.Add(A<TestEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Update(A<TestEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Delete(A<TestEntity>._)).MustNotHaveHappened();
        }

        [Test]
        public async Task Complete_When_Object_Update_Registered_With_Changes_Calls_UpdateAction_With_Changes()
        {
            // Arrange
            var trackedEntity = RegisterSingleForUpdates(_rawEntity);

            trackedEntity.SomeString = "Wagga";
            trackedEntity.SomeInt = 12;

            // Act
            await _sut.CompleteAsync();

            // Assert
            A.CallTo(() => _storeWriter.Update(A<TestEntity>.That.Matches(x =>
                    x.SomeString == "Wagga"
                    && x.SomeInt == 12
                    && !(x is IChangeTrackable<TestEntity>)
                    )))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _storeWriter.Add(A<TestEntity>._)).MustNotHaveHappened();
            A.CallTo(() => _storeWriter.Delete(A<TestEntity>._)).MustNotHaveHappened();
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
            var firstEntityToUpdate = RegisterSingleForUpdates(new TestEntity { Id = idOfFirstEntityToUpdate });
            var secondEntityToUpdate = RegisterSingleForUpdates(new TestEntity { Id = idOfSecondEntityToUpdate });

            // Act
            var expectedFirstDelete = RegisterDelete(new TestEntity { Id = idOfFirstEntityToDelete });

            firstEntityToUpdate.SomeString = "Hello world!";
            var expectedFirstUpdate = firstEntityToUpdate.GetCurrentSnapshot();

            var expectedFirstAdd = _sut.RegisterAdd(new TestEntity { Id = idOfFirstEntityToAdd }).GetCurrentSnapshot();

            secondEntityToUpdate.SomeInt = 1234;
            var expectedSecondUpdate = secondEntityToUpdate.GetCurrentSnapshot();

            var expectedSecondAdd = _sut.RegisterAdd(new TestEntity { Id = idOfSecondEntityToAdd }).GetCurrentSnapshot();

            var expectedSecondDelete = RegisterDelete(new TestEntity { Id = idOfSecondEntityToDelete });

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

        private void AssertOperation(int index, Operation expectedOperation, TestEntity expectedEquivalentEntity)
        {
            _orderedOperations[index].Value.Should().Be(expectedOperation, $"[{index}] was wrong operation");
            _orderedOperations[index].Key.Should().BeEquivalentTo(expectedEquivalentEntity, $"[{index}] didn't match");
        }
    }
}