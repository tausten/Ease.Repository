//
// Copyright (c) 2019 Tyler Austen. See LICENSE file at top of repository for details.
//

using System.Collections.Generic;

namespace Ease.Repository.AzureTable.Collections.Generic
{
    /// <summary>
    /// TODO: Move this to Ease.Util  (keep `internal` until then)
    /// 
    /// A mapping of keys to a collection of values. Takes care of ensuring that an empty collection is present,
    /// avoiding need for null checks and collection initialization per key prior to use. Eg:
    ///
    /// <code>
    /// var families = new MapOfCollections&lt;string, HashSet&lt;string&gt;, string&gt;();
    /// families["Doe"].Add("Jane");
    /// families["Doe"].Add("John");
    /// var theDoes = string.Join(", ", families["Doe"]);
    /// </code>
    ///
    /// See <seealso cref="MapOfHashSets"/> and <seealso cref="MapOfLists"/> for convenient simplification for
    /// common some cases.
    /// </summary>
    /// <typeparam name="TKey">The lookup key type.</typeparam>
    /// <typeparam name="TCollection">The collection type to store the values in per key.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    internal class MapOfCollections<TKey, TCollection, TValue>
        where TCollection : ICollection<TValue>, new()
    {
        private readonly Dictionary<TKey, TCollection> _inner = new Dictionary<TKey, TCollection>();

        private TCollection EnsureCollection(TKey key)
        {
            if (!_inner.TryGetValue(key, out var coll))
            {
                coll = new TCollection();
                _inner[key] = coll;
            }
            return coll;
        }

        public void Add(TKey key, TValue value)
        {
            EnsureCollection(key).Add(value);
        }

        public void Remove(TKey key, TValue value)
        {
            if (_inner.TryGetValue(key, out var coll))
            {
                coll.Remove(value);
            }
        }

        public TCollection this[TKey key]
        {
            get => EnsureCollection(key);
            set => _inner[key] = value;
        }

        public void Clear()
        {
            _inner.Clear();
        }
    }

    internal class MapOfHashSets<TKey, TValue> : MapOfCollections<TKey, HashSet<TValue>, TValue> { }

    internal class MapOfLists<TKey, TValue> : MapOfCollections<TKey, List<TValue>, TValue> { }
}