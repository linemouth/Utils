using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    // A read-only map that allows bi-directional dictionary-based lookup.
    // Map.Forward and Map.Reverse allow access to Dictionary functions.
    public class Map<A, B> : IEnumerable<KeyValuePair<A, B>>
    {
        public B this[A a]
        {
            get => forward[a];
            set
            {
                if (!IsReadOnly)
                {
                    forward[a] = value;
                }
            }
        }
        public A this[B b]
        {
            get => reverse[b];
            set
            {
                if (!IsReadOnly)
                {
                    reverse[b] = value;
                }
            }
        }
        public ICollection<A> Keys => forward.Keys;
        public ICollection<B> Values => reverse.Keys;
        public int Count => forward.Count;
        public bool IsReadOnly { get; set; } = false;

        private readonly Dictionary<A, B> forward = new Dictionary<A, B>();
        private readonly Dictionary<B, A> reverse = new Dictionary<B, A>();

        public Map()
        {
            Forward = new Indexer<A, B>(forward);
            Reverse = new Indexer<B, A>(reverse);
        }
        public class Indexer<TKey, TValue>
        {
            private readonly Dictionary<TKey, TValue> dictRef;
            public Indexer(Dictionary<TKey, TValue> dictionary)
            {
                dictRef = dictionary;
            }
            public IEqualityComparer<TKey> Comparer => dictRef.Comparer;
            public int Count { get => dictRef.Count; }
            public TValue this[TKey index] => dictRef[index];
            public Dictionary<TKey, TValue>.KeyCollection Keys => dictRef.Keys;
            public Dictionary<TKey, TValue>.ValueCollection Values => dictRef.Values;
            public bool ContainsKey(TKey key) => dictRef.ContainsKey(key);
            public bool ContainsValue(TValue value) => dictRef.ContainsValue(value);
            public Dictionary<TKey, TValue>.Enumerator? GetEnumerator() => dictRef.GetEnumerator();
            public bool TryGetValue(TKey key, out TValue value) => dictRef.TryGetValue(key, out value);
        }
        public void Add(A a, B b)
        {
            if (!IsReadOnly)
            {
                forward.Add(a, b);
                reverse.Add(b, a);
            }
        }
        public void Add(KeyValuePair<A, B> pair) => Add(pair.Key, pair.Value);
        public void Add(B b, A a) => Add(a, b);
        public void Add(KeyValuePair<B, A> pair) => Add(pair.Value, pair.Key);
        public void Clear()
        {
            if (!IsReadOnly)
            {
                forward.Clear();
                reverse.Clear();
            }
        }
        public IEnumerator<KeyValuePair<A, B>> GetEnumerator() => forward.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool TryGetValue(A a, out B b) => forward.TryGetValue(a, out b);
        public bool TryGetValue(B b, out A a) => reverse.TryGetValue(b, out a);
        public bool ContainsKey(A a) => forward.ContainsKey(a);
        public bool ContainsKey(B b) => reverse.ContainsKey(b);
        public bool Contains(KeyValuePair<A, B> pair) => forward.Contains(pair);
        public bool Contains(KeyValuePair<B, A> pair) => reverse.Contains(pair);
        public bool Remove(A a)
        {
            if (!IsReadOnly)
            {
                return forward.Remove(a);
            }
            return false;
        }
        public bool Remove(B b)
        {
            if (!IsReadOnly)
            {
                return reverse.Remove(b);
            }
            return false;
        }

        public Indexer<A, B> Forward { get; private set; }
        public Indexer<B, A> Reverse { get; private set; }
    }
}