//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using NUnit.Framework;
using FluentAssertions;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Ease.Repository.Tests
{
    public class MemoryStoreTests
    {
        public class SomeEntityType
        {
            public static readonly IEqualityComparer<SomeEntityType> KeyComparer = new SomeEntityTypeKeyComparer();

            public virtual string GroupKey { get; set; }
            public virtual Guid Id { get; set; }
            public virtual string SomeString { get; set; }
            public virtual int SomeInt { get; set; }

            private class SomeEntityTypeKeyComparer : IEqualityComparer<SomeEntityType>
            {
                public bool Equals(SomeEntityType x, SomeEntityType y) { return x.GroupKey == y.GroupKey && x.Id == y.Id; }
                public int GetHashCode(SomeEntityType obj) { return HashCode.Combine(obj.GroupKey, obj.Id); }
            }
        }

        public class DifferentEntityType
        {
            public static readonly IEqualityComparer<DifferentEntityType> KeyComparer = new DifferentEntityTypeKeyComparer();

            public virtual string Identifier { get; set; }
            public virtual string Name { get; set; }
            public virtual int FavoriteNumber { get; set; }

            private class DifferentEntityTypeKeyComparer : IEqualityComparer<DifferentEntityType>
            {
                public bool Equals(DifferentEntityType x, DifferentEntityType y) { return x.Identifier == y.Identifier; }
                public int GetHashCode(DifferentEntityType obj) { return obj.Identifier.GetHashCode(); }
            }
        }

        private MemoryStore _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new MemoryStore();
        }

        [Test]
        public void Add_Before_Registration_Uses_Default_Comparer_And_Succeeds_When_Keys_Match_But_Different_Objects()
        {
            // Arrange
            var entity = new SomeEntityType { GroupKey = "1", Id = Guid.NewGuid(), SomeString = "Hello", SomeInt = 1 };
            var sameIdsEntity = new SomeEntityType { GroupKey = entity.GroupKey, Id = entity.Id, SomeString = "World", SomeInt = 2 };

            // Act
            _sut.Add(entity);
            _sut.Add(sameIdsEntity);

            // Assert
            _sut.Entities<SomeEntityType>().Count.Should().Be(2);
            var entityFromStore = _sut.Entities<SomeEntityType>().First(x => x.Equals(entity));
            var sameIdsEntityFromStore = _sut.Entities<SomeEntityType>().First(x => x.Equals(sameIdsEntity));

            entityFromStore.Should().NotBeSameAs(sameIdsEntityFromStore);
        }

        [Test]
        public void Add_After_Registration_With_KeyComparer_Fails_When_Keys_Match()
        {
            // Arrange
            var entity = new SomeEntityType { GroupKey = "1", Id = Guid.NewGuid(), SomeString = "Hello", SomeInt = 1 };
            var sameIdsEntity = new SomeEntityType { GroupKey = entity.GroupKey, Id = entity.Id, SomeString = "World", SomeInt = 2 };

            _sut.Register(SomeEntityType.KeyComparer);
            _sut.Add(entity);

            // Act
            // Assert
            _sut.Invoking(x => x.Add(sameIdsEntity))
                .Should().Throw<ArgumentException>().Where(e => e.ParamName == "entity");
        }

        [Test]
        public void Update_After_Registration_With_KeyComparer_Overwrites_When_Keys_Match()
        {
            // Arrange
            var entity = new SomeEntityType { GroupKey = "1", Id = Guid.NewGuid(), SomeString = "Hello", SomeInt = 1 };
            var sameIdsEntity = new SomeEntityType { GroupKey = entity.GroupKey, Id = entity.Id, SomeString = "World", SomeInt = 2 };

            _sut.Register(SomeEntityType.KeyComparer);

            // Act
            _sut.Add(entity);
            _sut.Update(sameIdsEntity);

            // Assert
            _sut.Entities<SomeEntityType>().Count.Should().Be(1);
            _sut.Entities<SomeEntityType>().First().Should().BeSameAs(sameIdsEntity);
            _sut.Entities<SomeEntityType>().Contains(entity).Should().BeTrue();
            _sut.Entities<SomeEntityType>().Contains(sameIdsEntity).Should().BeTrue();
        }

        [Test]
        public void Add_Different_Entity_Types()
        {
            // Arrange
            var someEntityTypeInstance = new SomeEntityType { GroupKey = "1", Id = Guid.NewGuid(), SomeString = "Hello", SomeInt = 1 };
            var differentEntityTypeInstance = new DifferentEntityType { Identifier = "111", Name = "Jiggy Wiggy", FavoriteNumber = 3 };

            // Act
            _sut.Add(someEntityTypeInstance);
            _sut.Add(differentEntityTypeInstance);

            // Assert
            _sut.Entities<SomeEntityType>().Single().Should().BeSameAs(someEntityTypeInstance);
            _sut.Entities<DifferentEntityType>().Single().Should().BeSameAs(differentEntityTypeInstance);
        }
    }
}