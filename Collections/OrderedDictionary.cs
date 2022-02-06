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
            get => values[dict[key]];
            set
            {
                if(!dict.ContainsKey(key))
                {
                    Add(key, value);
                }
                else
                {
                    values[dict[key]] = value;
                }
            }
        }
        public TValue this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }
        public ICollection<TKey> Keys => keys.ToList();
        public ICollection<TValue> Values => values.ToList();
        public int Count => values.Count;
        public bool IsReadOnly => false;
        public object SyncRoot => values;
        public bool IsSynchronized => false;

        private readonly List<TKey> keys;
        private readonly List<TValue> values;
        private readonly Dictionary<TKey, int> dict;

        public OrderedDictionary(IEqualityComparer<TKey> comparer = null) : this(0, comparer) { }
        public OrderedDictionary(int capacity, IEqualityComparer<TKey> comparer = null)
        {
            keys = new List<TKey>(capacity);
            values = new List<TValue>(capacity);
            dict = new Dictionary<TKey, int>(capacity, comparer ?? EqualityComparer<TKey>.Default);
        }
        public void Add(TKey key, TValue value)
        {
            if(!dict.ContainsKey(key))
            {
                dict.Add(key, Count);
                keys.Add(key);
                values.Add(value);
            }
        }
        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        public void Clear()
        {
            keys.Clear();
            values.Clear();
            dict.Clear();
        }
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if(item.Key != null && dict.TryGetValue(item.Key, out int index))
            {
                return EqualityComparer<TValue>.Default.Equals(values[index], item.Value);
            }
            return false;
        }
        public bool ContainsKey(TKey key) => dict.ContainsKey(key);
        public void CopyTo(TValue[] array, int arrayIndex) => values.CopyTo(array, arrayIndex);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => CopyTo((Array)array, index);
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
                TValue[] data = (TValue[])array;
                for(int i = 0; i < Count; ++i)
                {
                    data[index + i] = values[i];
                }
            }
            else if(typeof(KeyValuePair<TKey, TValue>) == elementType)
            {
                KeyValuePair<TKey, TValue>[] data = (KeyValuePair<TKey, TValue>[])array;
                for(int i = 0; i < Count; ++i)
                {
                    data[index + i] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
                }
            }
            else
            {
                throw new ArgumentException($"Array element type of {elementType} is unsupported in OrderedDictionary<{typeof(TKey)}, {typeof(TValue)}>", "array");
            }
        }
        public int IndexOf(TKey key) => dict.TryGetValue(key, out int index) ? index : -1;
        public int IndexOf(TValue value) => values.FindIndex(v => EqualityComparer<TValue>.Default.Equals(v, value));
        public int IndexOf(KeyValuePair<TKey, TValue> item)
        {
            if(item.Key != null && dict.TryGetValue(item.Key, out int index))
            {
                if(EqualityComparer<TValue>.Default.Equals(values[index], item.Value))
                {
                    return index;
                }
            }
            return -1;
        }
        public bool Insert(int index, TKey key, TValue value)
        {
            if(!dict.ContainsKey(key) && index >= 0 && index < Count)
            {
                // Insert
                keys.Insert(index, key);
                values.Insert(index, value);
                dict.Add(key, index);

                // Shift all later values up
                for(++index; index < Count; ++index)
                {
                    ++dict[keys[index]];
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
            TValue result = values[index];
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
                dict.Remove(keys[index]);
                keys.RemoveAt(index);
                values.RemoveAt(index);

                // Shift all later values down
                for(; index < Count; ++index)
                {
                    --dict[keys[index]];
                }
                return true;
            }
            return false;
        }
        public bool TryGetValue(TKey key, out TValue value)
        {
            if(key != null && dict.TryGetValue(key, out int index))
            {
                value = values[index];
                return true;
            }
            value = default;
            return false;
        }
        public bool TryGetValue(int index, out TValue value)
        {
            if(index >= 0 && index < Count)
            {
                value = values[index];
                return true;
            }
            value = default;
            return false;
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            for(int i = 0; i < Count; ++i)
            {
                yield return new KeyValuePair<TKey, TValue>(keys[i], values[i]);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
