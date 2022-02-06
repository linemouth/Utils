using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public static class TandemExtensions
    {
        public static int Count<A, B>(IEnumerable<A> itemsA, IEnumerable<B> itemsB)
        {
            IEnumerator<A> a = itemsA.GetEnumerator();
            IEnumerator<B> b = itemsB.GetEnumerator();
            int count = 0;

            while(a.MoveNext() && b.MoveNext())
            {
                ++count;
            }
            return count;
        }
        public static void ForEach<A, B>(IEnumerable<A> itemsA, IEnumerable<B> itemsB, Action<A, B> algorithm)
        {
            IEnumerator<A> a = itemsA.GetEnumerator();
            IEnumerator<B> b = itemsB.GetEnumerator();

            while(a.MoveNext() && b.MoveNext())
            {
                algorithm(a.Current, b.Current);
            }
        }
        public static IEnumerable<T> Select<A, B, T>(IEnumerable<A> itemsA, IEnumerable<B> itemsB, Func<A, B, T> algorithm)
        {
            IEnumerator<A> a = itemsA.GetEnumerator();
            IEnumerator<B> b = itemsB.GetEnumerator();

            while(a.MoveNext() && b.MoveNext())
            {
                yield return algorithm(a.Current, b.Current);
            }
        }
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<TKey> keys, IEnumerable<TValue> values) => Select(keys, values, (k, v) => new KeyValuePair<TKey, TValue>(k, v)).ToDictionary(p => p.Key, p => p.Value);
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(IEnumerable<TKey> keys, IEnumerable<TValue> values, IEqualityComparer<TKey> comparer) => Select(keys, values, (k, v) => new KeyValuePair<TKey, TValue>(k, v)).ToDictionary(p => p.Key, p => p.Value, comparer);
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TValue, TElement>(IEnumerable<TKey> keys, IEnumerable<TValue> values, Func<TValue, TElement> elementSelector) => Select(keys, values, (k, v) => new KeyValuePair<TKey, TElement>(k, elementSelector(v))).ToDictionary(p => p.Key, p => p.Value);
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TValue, TElement>(IEnumerable<TKey> keys, IEnumerable<TValue> values, Func<TValue, TElement> elementSelector, IEqualityComparer<TKey> comparer) => Select(keys, values, (k, v) => new KeyValuePair<TKey, TElement>(k, elementSelector(v))).ToDictionary(p => p.Key, p => p.Value, comparer);

        public static int Count<A, B, C>(IEnumerable<A> itemsA, IEnumerable<B> itemsB, IEnumerable<C> itemsC)
        {
            IEnumerator<A> a = itemsA.GetEnumerator();
            IEnumerator<B> b = itemsB.GetEnumerator();
            IEnumerator<C> c = itemsC.GetEnumerator();
            int count = 0;

            while(a.MoveNext() && b.MoveNext() && c.MoveNext())
            {
                ++count;
            }
            return count;
        }
        public static void ForEach<A, B, C>(IEnumerable<A> itemsA, IEnumerable<B> itemsB, IEnumerable<C> itemsC, Action<A, B, C> algorithm)
        {
            IEnumerator<A> a = itemsA.GetEnumerator();
            IEnumerator<B> b = itemsB.GetEnumerator();
            IEnumerator<C> c = itemsC.GetEnumerator();

            while(a.MoveNext() && b.MoveNext() && c.MoveNext())
            {
                algorithm(a.Current, b.Current, c.Current);
            }
        }
        public static IEnumerable<T> Select<A, B, C, T>(IEnumerable<A> itemsA, IEnumerable<B> itemsB, IEnumerable<C> itemsC, Func<A, B, C, T> algorithm)
        {
            IEnumerator<A> a = itemsA.GetEnumerator();
            IEnumerator<B> b = itemsB.GetEnumerator();
            IEnumerator<C> c = itemsC.GetEnumerator();

            while(a.MoveNext() && b.MoveNext() && c.MoveNext())
            {
                yield return algorithm(a.Current, b.Current, c.Current);
            }
        }
    }
}
