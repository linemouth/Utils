using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Utils
{
    public static class CollectionExtensions
    {
        /// <summary>Returns the first value of a SortedDictionary for which the key is before or equal to the given key.</summary>
        /// <param name="key">A value by which to lookup, The key does not need to exist in the dictionary, but must be comparable to the other keys.</param>
        /// <param name="defaultValue">The value to return if the dictionary is empty.</param>
        /// <returns>The first value before or equal to the given key.</returns>
        public static T GetFirstValueOrDefault<K, T>(this SortedDictionary<K, T> dictionary, K key, T defaultValue = default)
        {
            if(dictionary.Count == 0)
            {
                return defaultValue;
            }
            Func<K, K, int> compare = dictionary.Comparer != null ? (Func<K, K, int>)dictionary.Comparer.Compare : Comparer<K>.Default.Compare;
            key = dictionary.Keys.FirstOrDefault(k => compare(k, key) >= 0);
            return key == null ? dictionary.Values.First() : dictionary[key];
			/*
			KeyValuePair<K, T> entry = default;
            foreach(KeyValuePair<K, T> item in dictionary)
            {
                entry = item;
                if(compare(item.Key, key) >= 0)
                {
                    break;
                }
            }    
            return entry.Value;
			*/
        }
        public static T GetValueOrDefault<K, T>(this IDictionary<K, T> dictionary, K key, T defaultValue) => dictionary.TryGetValue(key, out T value) ? value : defaultValue;
        public static T Random<T>(this T[] list) => list[Math.Random(list.Length)];
        public static T Random<T>(this IList<T> list) => list[Math.Random(list.Count)];
        public static T Random<T>(this IEnumerable<T> list) => list.OrderBy(item => Math.Random(0f, 1f)).First();
        public static List<(int index, T item)> Rank<T>(this IEnumerable<T> list) where T : IComparable => Rank(list, item => item);
        public static List<(int index, T item)> Rank<T, K>(this IEnumerable<T> list, Func<T, K> selector = null) where K : IComparable
        {
            List<(int index, T item)> sortedList = new List<(int index, T item)>(list.Count());
            int index = 0;
            foreach(T item in list)
            {
                sortedList.Add((index, item));
                ++index;
            }
            return sortedList.OrderBy(entry => selector(entry.item)).ToList();
        }
        public static void SortBy<T, K>(this List<T> list, Func<T, K> selector, bool descending = false) where K : IComparable
        {
            if(descending)
            {
                list.Sort((a, b) => selector(b).CompareTo(selector(a)));
            }
            else
            {
                list.Sort((a, b) => selector(a).CompareTo(selector(b)));
            }
        }
        public static T TakeFirst<T>(this IList<T> list) => list.TakeAt(0);
        public static T TakeLast<T>(this IList<T> list) => list.TakeAt(list.Count - 1);
        public static T Take<T>(this IList<T> list, T item) => list.TakeAt(list.IndexOf(item));
        public static T TakeAt<T>(this IList<T> list, int index)
        {
            T result = list.ElementAt(index);
            list.RemoveAt(index);
            return result;
        }
        public static void Enqueue<T>(this LinkedList<T> list, T value) => list.AddLast(value);
        public static T Dequeue<T>(this LinkedList<T> list)
        {
            T result = list.First.Value;
            list.RemoveFirst();
            return result;
        }
        public static void AddRange<T>(this LinkedList<T> list, IEnumerable<T> items)
        {
            foreach(T item in items)
            {
                list.AddLast(item);
            }
        }
        public static int CompareTo<T>(this IEnumerable<T> a, IEnumerable<T> b) where T : IComparable<T>
        {
            int result = a.Count() - b.Count();
            IEnumerator<T> iterA = a.GetEnumerator();
            IEnumerator<T> iterB = a.GetEnumerator();
            for(int i = 0; result == 0 && iterA.MoveNext() && iterB.MoveNext(); ++i)
            {
                result = iterA.Current.CompareTo(iterB.Current);
            }
            return result;
        }
        /// <summary>Finds the item in the list that has the minimum value for a specified key, based on the default comparer for the key type.</summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <typeparam name="K">The type of the key used for comparison.</typeparam>
        /// <param name="list">A list from which to select the minimum item.</param>
        /// <param name="selector">A function to extract the key for each item.</param>
        /// <returns>The item with the minimum key value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if either the list or selector are null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the list is empty.</exception>
        /// <remarks>This method iterates through the sequence once, finding the element with the minimum value for the specified key based on the default comparer for the key type. If multiple elements have the same minimum value, the first one encountered is returned.</remarks>
        public static T MinBy<T, K>(this IEnumerable<T> list, Func<T, K> selector) where K : IComparable<K>
        {
            if(list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if(selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }
            using(IEnumerator<T> enumerator = list.GetEnumerator())
            {
                if(!enumerator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                T minItem = enumerator.Current;
                K minKey = selector(minItem);
                while(enumerator.MoveNext())
                {
                    T item = enumerator.Current;
                    K key = selector(item);

                    if(key.CompareTo(minKey) < 0)
                    {
                        minItem = item;
                        minKey = key;
                    }
                }
                return minItem;
            }
        }
        /// <summary>Finds the item in the list that has the maximum value for a specified key, based on the default comparer for the key type.</summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <typeparam name="K">The type of the key used for comparison.</typeparam>
        /// <param name="list">A list from which to select the maximum item.</param>
        /// <param name="selector">A function to extract the key for each item.</param>
        /// <returns>The item with the maximum key value.</returns>
        /// <exception cref="ArgumentNullException">Thrown if either the list or selector are null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the list is empty.</exception>
        /// <remarks>This method iterates through the sequence once, finding the element with the maximum value for the specified key based on the default comparer for the key type. If multiple elements have the same maximum value, the first one encountered is returned.</remarks>
        public static T MaxBy<T, K>(this IEnumerable<T> list, Func<T, K> selector) where K : IComparable<K>
        {
            if(list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if(selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }
            using(IEnumerator<T> enumerator = list.GetEnumerator())
            {
                if(!enumerator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                T maxItem = enumerator.Current;
                K maxKey = selector(maxItem);
                while(enumerator.MoveNext())
                {
                    T item = enumerator.Current;
                    K key = selector(item);

                    if(key.CompareTo(maxKey) > 0)
                    {
                        maxItem = item;
                        maxKey = key;
                    }
                }
                return maxItem;
            }
        }
        public static bool MinMaxIndex<T>(this IEnumerable<T> list, out int minIndex, out int maxIndex) where T : IComparable => MinMaxIndex(list, out minIndex, out maxIndex, (a, b) => a.CompareTo(b) < 0);
        public static bool MinMaxIndex<T>(this IEnumerable<T> list, out int minIndex, out int maxIndex, Func<T, T, bool> lessThan) where T : IComparable
        {
            minIndex = maxIndex = -1;
            if(list != null && list.Count() > 0)
            {
                IEnumerator<T> iter = list.GetEnumerator();
                iter.MoveNext();
                int index = 0;

                // Begin by assuming the first element is the only element
                minIndex = maxIndex = index;
                T minValue = iter.Current;
                T maxValue = iter.Current;

                // Iterate through elements 1 through n
                while(iter.MoveNext())
                {
                    // Keep track of the index
                    ++index;
                    T current = iter.Current;

                    // If the current element compares greater than maxValue, replace it
                    if(lessThan(maxValue, current))
                    {
                        maxValue = current;
                        maxIndex = index;
                    }

                    // If the current element compares less than minValue, replace it
                    if(lessThan(current, minValue))
                    {
                        minValue = current;
                        minIndex = index;
                    }
                }
                return true;
            }
            return false;
        }
        /// <summary>Swaps the elements at two indices in a list.</summary>
        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            if(indexA != indexB)
            {
                T tmp = list[indexA];
                list[indexA] = list[indexB];
                list[indexB] = tmp;
            }
        }
        /// <summary>Swaps the elements at two indices in an array.</summary>
        public static void Swap<T>(this T[] array, int indexA, int indexB)
        {
            if(indexA != indexB)
            {
                T tmp = array[indexA];
                array[indexA] = array[indexB];
                array[indexB] = tmp;
            }
        }
        /// <summary>Swaps items within blocks of swapBoundary size in a list.</summary>
        public static void Swap<T>(this IList<T> list, int swapBoundary)
        {
            if(list.Count % swapBoundary != 0)
            {
                throw new ArgumentException("The input data must have a length that is a multiple of the byte-swap boundary.");
            }
            int swapsPerWord = swapBoundary / 2;
            for(int i = 0; i < list.Count; i += swapBoundary)
            {
                for(int j = 0; j < swapsPerWord; ++j)
                {
                    int a = i + j;
                    int b = i + swapBoundary - 1 - j;
                    T temp = list[a];
                    list[a] = list[b];
                    list[b] = temp;
                }
            }
        }
        /// <summary>Swaps items within blocks of swapBoundary size in an array.</summary>
        public static void Swap<T>(this T[] array, int swapBoundary)
        {
            if(array.Length % swapBoundary != 0)
            {
                throw new ArgumentException("The input data must have a length that is a multiple of the byte-swap boundary.");
            }
            int swapsPerWord = swapBoundary / 2;
            for(int i = 0; i < array.Length; i += swapBoundary)
            {
                for(int j = 0; j < swapsPerWord; ++j)
                {
                    int a = i + j;
                    int b = i + swapBoundary - 1 - j;
                    T temp = array[a];
                    array[a] = array[b];
                    array[b] = temp;
                }
            }
        }
        public static int[] GetLengths(this Array array)
        {
            int[] lengths = new int[array.Rank];
            for(int i = 0; i < lengths.Length; ++i)
            {
                lengths[i] = array.GetLength(i);
            }
            return lengths;
        }
        /// <summary>
        /// Returns Nth smallest element from the list. Here n starts from 0 so that n=0 returns minimum, n=1 returns 2nd smallest element etc.
        /// Note: specified list would be mutated in the process.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 216
        /// </summary>
        public static T NthOrderStatistic<T>(this IList<T> list, int n, Random rnd = null) where T : IComparable<T> => NthOrderStatistic(list, n, 0, list.Count - 1, rnd);
        /// <summary>Note: specified list would be mutated in the process.</summary>
        public static T Median<T>(this IList<T> list) where T : IComparable<T> => list.NthOrderStatistic((list.Count - 1) / 2);
        public static double Median<T>(this IEnumerable<T> sequence, Func<T, double> getValue)
        {
            var list = sequence.Select(getValue).ToList();
            return list.NthOrderStatistic((list.Count - 1) / 2);
        }
        public static void Shuffle<T>(this IList<T> list)
        {
            int count = list.Count;
            for(int a = 0; a < count; ++a)
            {
                int b = Math.Random(0, count);
                list.Swap(a, a == b ? count - a : b);
            }
        }

        private static T NthOrderStatistic<T>(this IList<T> list, int n, int start, int end, Random rnd) where T : IComparable<T>
        {
            while(true)
            {
                var pivotIndex = list.Partition(start, end, rnd);
                if(pivotIndex == n)
                    return list[pivotIndex];

                if(n < pivotIndex)
                    end = pivotIndex - 1;
                else
                    start = pivotIndex + 1;
            }
        }
        /// <summary>
        /// Partitions the given list around a pivot element such that all elements on left of pivot are <= pivot
        /// and the ones at thr right are > pivot. This method can be used for sorting, N-order statistics such as
        /// as median finding algorithms.
        /// Pivot is selected ranodmly if random number generator is supplied else its selected as last element in the list.
        /// Reference: Introduction to Algorithms 3rd Edition, Corman et al, pp 171
        /// </summary>
        private static int Partition<T>(this IList<T> list, int start, int end, Random rnd = null) where T : IComparable<T>
        {
            if(rnd != null)
            {
                list.Swap(end, rnd.Next(start, end + 1));
            }

            var pivot = list[end];
            var lastLow = start - 1;
            for(var i = start; i < end; i++)
            {
                if(list[i].CompareTo(pivot) <= 0)
                {
                    list.Swap(i, ++lastLow);
                }
            }
            list.Swap(end, ++lastLow);
            return lastLow;
        }
    }
}