using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    /// <summary>
    /// Represents a list where the items can be referenced by a key generated from the item itself.
    /// </summary>
    /// <typeparam name="TKey">The type of the lookup values.</typeparam>
    /// <typeparam name="TValue">The type of the data storage values.</typeparam>
    public class KeyedList<TKey, TValue> : IList<TValue> where TKey : IEquatable<TKey>
    {
        public TValue this[TKey key]
        {
            get => list[IndexOf(key)];
            set => list[IndexOf(key)] = value;
        }
        public TValue this[int index]
        {
            get => list[index];
            set
            {
                if(!IsReadOnly)
                {
                    list[index] = value;
                }
            }
        }
        public ICollection<TKey> Keys => list.Select(i => keyGenerator(i)).ToArray();
        public ICollection<TValue> Values => list.ToArray();
        public int Count => list.Count;
        public bool IsReadOnly { get; set; } = false;
        public delegate TKey KeyGenerator(TValue item);

        private readonly List<TValue> list = new List<TValue>();
        private KeyGenerator keyGenerator;

        public KeyedList(KeyGenerator keyGenerator, IEnumerable<TValue> items = null)
        {
            this.keyGenerator = keyGenerator;
            list = items != null ? items.ToList() : new List<TValue>();
        }
        public void Add(TValue item) { if(!IsReadOnly) { list.Add(item); } }
        public void Clear() { if(!IsReadOnly) { list.Clear(); } }
        public bool Contains(TValue item) => list.Contains(item);
        public bool ContainsKey(TKey key) => list.Any(item => keyGenerator(item).Equals(key));
        public void CopyTo(TValue[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => list.Select(item => new KeyValuePair<TKey, TValue>(keyGenerator(item), item)).ToArray().CopyTo(array, arrayIndex);
        public int IndexOf(TKey key) => list.FindIndex(item => keyGenerator(item).Equals(key));
        public int IndexOf(TValue value) => list.FindIndex(item => item.Equals(value));
        public int IndexOf(KeyValuePair<TKey, TValue> pair) => list.FindIndex(item => item.Equals(pair.Value) && keyGenerator(item).Equals(pair.Key));
        public void Insert(int index, TValue item) { if(!IsReadOnly) { list.Insert(index, item); } }
        public bool Remove(TKey key)
        {
            if(ContainsKey(key))
            {
                RemoveAt(IndexOf(key));
                return true;
            }
            return false;
        }
        public bool Remove(TValue value)
        {
            if(Contains(value))
            {
                RemoveAt(IndexOf(value));
                return true;
            }
            return false;
        }
        public void RemoveAt(int index)
        {
            if(!IsReadOnly && index >= 0 && index < list.Count)
            {
                list.RemoveAt(index);
            }
        }
        public bool TryGetValue(TKey key, out TValue value) => TryGetValue(list.FindIndex(item => keyGenerator(item).Equals(key)), out value);
        public bool TryGetValue(int index, out TValue value)
        {
            if(index >= 0 && index < list.Count)
            {
                value = list[index];
                return true;
            }
            value = default;
            return false;
        }
        public IEnumerator<TValue> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    }
}
