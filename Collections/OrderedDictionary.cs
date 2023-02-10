using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    /// <summary>
    /// Represents a dictionary where the insertion order is maintained.
    /// </summary>
    /// <typeparam name="TKey">The type of the lookup values.</typeparam>
    /// <typeparam name="TValue">The type of the data storage values.</typeparam>
    [Serializable]
    public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection
    {
        public TValue this[TKey key]
        {
            get => items[dict[key]].Value;
            set
            {
                if(!dict.ContainsKey(key))
                {
                    Add(key, value);
                }
                else
                {
                    int index = dict[key];
                    items[index] = new KeyValuePair<TKey, TValue>(items[index].Key, value);
                }
            }
        }
        public TValue this[int index]
        {
            get => items[index].Value;
            set => items[index] = new KeyValuePair<TKey, TValue>(items[index].Key, value);
        }
        public List<KeyValuePair<TKey, TValue>> Items => items.ToList();
        public ICollection<TKey> Keys => items.Select(item => item.Key).ToList();
        public ICollection<TValue> Values => items.Select(item => item.Value).ToList();
        public int Count => items.Count;
        public bool IsReadOnly => false;
        public object SyncRoot => items;
        public bool IsSynchronized => false;

        private readonly List<KeyValuePair<TKey, TValue>> items;
        private readonly Dictionary<TKey, int> dict;

        public OrderedDictionary(IEqualityComparer<TKey> comparer = null) : this(0, comparer) { }
        public OrderedDictionary(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            items = new List<KeyValuePair<TKey, TValue>>(capacity);
            dict = new Dictionary<TKey, int>(capacity, comparer ?? EqualityComparer<TKey>.Default);
        }
        public void Add(TKey key, TValue value)
        {
            if(!dict.ContainsKey(key))
            {
                dict.Add(key, Count);
                items.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
        }
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        public void Clear()
        {
            items.Clear();
            dict.Clear();
        }
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if(item.Key != null && dict.TryGetValue(item.Key, out int index))
            {
                return EqualityComparer<TValue>.Default.Equals(items[index].Value, item.Value);
            }
            return false;
        }
        public bool ContainsKey(TKey key) => dict.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => items.CopyTo(array, index);
        public void CopyTo(Array array, int index)
        {
            if(array == null)
            {
                throw new ArgumentNullException("array");
            }
            else if(index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            else if(array.Length - index < Count)
            {
                throw new ArgumentException("Not enough elements after index in the destination array.", "array");
            }
            else if(array.Rank != 1)
            {
                throw new ArgumentException("Only 1D arrays are supported.", "array");
            }

            Type elementType = array.GetType().GetElementType();
            if(typeof(TValue) == elementType)
            {
                Values.CopyTo((TValue[])array, index);
            }
            else if(typeof(TKey) == elementType)
            {
                Keys.CopyTo((TKey[])array, index);
            }
            else if(typeof(KeyValuePair<TKey, TValue>) == elementType)
            {
                items.CopyTo((KeyValuePair<TKey, TValue>[])array, index);
            }
            else
            {
                throw new ArgumentException($"Array element type of {elementType} is unsupported in OrderedDictionary<{typeof(TKey)}, {typeof(TValue)}>", "array");
            }
        }
        public int IndexOf(TKey key) => dict.TryGetValue(key, out int index) ? index : -1;
        public int IndexOf(TValue value) => items.FindIndex(item => EqualityComparer<TValue>.Default.Equals(item.Value, value));
        public int IndexOf(KeyValuePair<TKey, TValue> item) => items.IndexOf(item);
        public bool Insert(int index, TKey key, TValue value)
        {
            if(!dict.ContainsKey(key) && index >= 0 && index < Count)
            {
                // Insert
                items.Insert(index, new KeyValuePair<TKey, TValue>(key, value));
                dict.Add(key, index);

                // Shift all later values up
                for(++index; index < Count; ++index)
                {
                    ++dict[items[index].Key];
                }
                return true;
            }
            return false;
        }
        public bool Insert(int index, KeyValuePair<TKey, TValue> item) => Insert(index, item.Key, item.Value);
        public bool Insert(TKey insertBefore, TKey key, TValue value) => Insert(IndexOf(insertBefore), key, value);
        public bool Insert(TKey insertBefore, KeyValuePair<TKey, TValue> item) => Insert(IndexOf(insertBefore), item);
        public TValue Pop(int index)
        {
            TValue result = items[index].Value;
            RemoveAt(index);
            return result;
        }
        public TValue Pop(TKey key) => Pop(IndexOf(key));
        public bool Remove(TKey key) => RemoveAt(IndexOf(key));
        public bool Remove(KeyValuePair<TKey, TValue> item) => RemoveAt(IndexOf(item));
        public bool RemoveAt(int index)
        {
            if(index >= 0 && index < Count)
            {
                // Remove item
                dict.Remove(items[index].Key);
                items.RemoveAt(index);

                // Shift all later values down
                for(; index < Count; ++index)
                {
                    --dict[items[index].Key];
                }
                return true;
            }
            return false;
        }
        public bool TryGetValue(TKey key, out TValue value)
        {
            if(key != null && dict.TryGetValue(key, out int index))
            {
                value = items[index].Value;
                return true;
            }
            value = default;
            return false;
        }
        public bool TryGetValue(int index, out TValue value)
        {
            if(index >= 0 && index < Count)
            {
                value = items[index].Value;
                return true;
            }
            value = default;
            return false;
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
