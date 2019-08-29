//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using AutoFixture;
using AutoFixture.AutoFakeItEasy;
using FluentAssertions;
using System.Threading.Tasks;

namespace Ease.Repository.Test
{
    /// <summary>
    /// Baseline set of tests for any <see cref="Impl.Data.IRepository"/>. This test base class does not impost a particular test 
    /// framework (eg. MSTest, NUnit, etc..). Instead, you inherit from this class, then implement the abstract methods as just 
    /// delegate calls to the corresponding `base.*_Impl()` methods (unless you wish to provide your own alternative implementation
    /// for the corresponding baseline test). Don't forget to decorate the implemented test methods with your framework's 
    /// test attributes to make them discoverable by the test runner.
    /// </summary>
    /// <typeparam name="TKey">They type of the key.</typeparam>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TRepository">The type of the repository.</typeparam>
    public abstract class RepositoryTests<TKey, TEntity, TRepository>
        where TRepository : IRepository<TKey, TEntity>
        where TEntity : TKey, new()
    {
        /// <summary>
        /// The AutoFixture for the tests.
        /// </summary>
        protected IFixture TheFixture { get; private set; }

        /// <summary>
        /// The system under test (i.e. the repository) with its dependencies injected via <see cref="TheFixture"/>
        /// as `FakeItEasy` fakes.
        /// </summary>
        protected TRepository Sut { get; private set; }

        /// <summary>
        /// A setup step is necessary.
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating SetUp methods.
        /// </summary>
        public abstract void SetUp();

        protected virtual void SetUp_Impl()
        {
            TheFixture = new Fixture().Customize(new AutoFakeItEasyCustomization());

            FreezeDependencies(TheFixture);

            // Do this absolutely last (after "fixing" anything that may need to be mocked/faked)
            Sut = TheFixture.Create<TRepository>();
        }

        /// <summary>
        /// Override and complete the test unit of work.
        /// </summary>
        protected abstract Task CompleteUnitOfWorkAsync();

        /// <summary>
        /// Freeze any constructor-injected dependencies here so that they may be safely obtained later.
        /// </summary>
        /// <param name="fixture"></param>
        protected virtual void FreezeDependencies(IFixture fixture) { }

        /// <summary>
        /// Allocate a new entity with its properties set to non-null non-empty data.
        /// </summary>
        /// <returns></returns>
        protected virtual TEntity NewEntity()
        {
            return TheFixture.Create<TEntity>();
        }

        /// <summary>
        /// Nullify the <typeparamref name="TKey"/> fields of the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity"></param>
        protected abstract void NullifyKeyFields(TEntity entity);

        /// <summary>
        /// Make a detectable change to the entity, suitable for round-trip persistence testing.
        /// </summary>
        /// <param name="entityToModify">The entity to be modified in-place.</param>
        protected abstract void ModifyEntity(TEntity entityToModify);

        /// <summary>
        /// Assert that the result entity is "equivalent" to the reference entity (i.e. ignore non-essential properties
        /// during comparison, etc... so that just the "important" properties for equivalence are tested).
        /// </summary>
        /// <param name="result"></param>
        /// <param name="reference"></param>
        protected abstract void AssertEntitiesAreEquivalent(TEntity result, TEntity reference);

        /// <summary>
        /// Assert that the key fields in the entity aren't null or in "not set" state.
        /// </summary>
        /// <param name="entity"></param>
        protected abstract void AssertKeyFieldsNotNull(TEntity entity);

        /// <summary>
        /// Assert that there are <paramref name="num"/> entities stored in the repository.
        /// </summary>
        protected abstract void AssertRepositoryHasNumEntities(int num = 0);

        /// <summary>
        /// Return a simple (i.e. not full-entity type) key given an entity to start with.
        /// </summary>
        /// <param name="entity">The entity from which to allocate a new simple key instance.</param>
        /// <returns>The new simple key - NOT just the entity returned.</returns>
        protected abstract TKey NewSimpleKeyFromEntity(TEntity entity);

        /// <summary>
        /// Tests that the repository's Upsert method sets the key fields to something non-null non-empty.
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating test methods.
        /// </summary>
        public abstract void Add_Sets_Keys();

        protected void Add_Sets_Keys_Impl()
        {
            // Arrange
            var newThing = NewEntity();
            NullifyKeyFields(newThing);

            // Act
            var result = Sut.Add(newThing);

            // Assert
            AssertKeyFieldsNotNull(result);
        }

        /// <summary>
        /// Tests a round-trip of persisting a new entity and then fetching it by its key.
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating test methods.
        /// </summary>
        public abstract Task Add_New_Entity_And_Get_RoundTrip();

        protected async Task Add_New_Entity_And_Get_RoundTrip_Impl()
        {
            // Arrange
            var newThing = NewEntity();
            AssertRepositoryHasNumEntities(0);

            // Act
            Sut.Add(newThing);
            await CompleteUnitOfWorkAsync();
            var result = Sut.Get(newThing);

            // Assert
            AssertEntitiesAreEquivalent(result, newThing);
        }

        /// <summary>
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating test methods.
        /// </summary>
        public abstract Task Add_New_Entity_And_Get_By_Key_RoundTrip();

        protected virtual async Task Add_New_Entity_And_Get_By_Key_RoundTrip_Impl()
        {
            // Arrange
            var newThing = NewEntity();
            AssertRepositoryHasNumEntities(0);

            // Act
            Sut.Add(newThing);
            var key = NewSimpleKeyFromEntity(newThing);
            await CompleteUnitOfWorkAsync();
            var result = Sut.Get(key);

            // Assert
            AssertEntitiesAreEquivalent(result, newThing);
        }

        /// <summary>
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating test methods.
        /// 
        /// TODO: Solve the round-trip thing and then re-enable this... but don't force its implementation until then.
        /// </summary>
        public virtual Task Add_Existing_Entity_And_Get_RoundTrip() { return Task.CompletedTask; }

        protected virtual async Task Add_Existing_Entity_And_Get_RoundTrip_Impl()
        {
            // Arrange
            var newThing = NewEntity();
            AssertRepositoryHasNumEntities(0);
            Sut.Add(newThing);
            var existing = Sut.Get(newThing);
            ModifyEntity(existing);

            // Act
            Sut.Add(existing);
            await CompleteUnitOfWorkAsync();
            var result = Sut.Get(newThing);

            // Assert
            AssertEntitiesAreEquivalent(result, existing);
        }

        /// <summary>
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating test methods.
        /// </summary>
        public abstract Task Delete_And_Get_RoundTrip();

        protected virtual async Task Delete_And_Get_RoundTrip_Impl()
        {
            // Arrange
            var newThing = NewEntity();
            Sut.Add(newThing);

            // Act
            Sut.Delete(newThing);
            await CompleteUnitOfWorkAsync();
            var result = Sut.Get(newThing);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// NOTE: For most cases, just implement this as a call to the corresponding `base.*_Impl()`, and don't forget
        /// to apply your test framework's appropriate attributes or other mechanism for decorating test methods.
        /// 
        /// TODO: Solve the round-trip thing and then re-enable this... but don't force its implementation until then.
        /// </summary>
        public virtual Task Delete_By_Key_And_Get_RoundTrip() { return Task.CompletedTask; }

        protected virtual async Task Delete_By_Key_And_Get_RoundTrip_Impl()
        {
            // Arrange
            var newThing = NewEntity();
            Sut.Add(newThing);
            var key = NewSimpleKeyFromEntity(newThing);

            // Act
            Sut.Delete(key);
            await CompleteUnitOfWorkAsync();
            var result = Sut.Get(key);

            // Assert
            result.Should().BeNull();
        }
    }
}
