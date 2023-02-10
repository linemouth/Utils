using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class SortedList<T> : ICollection<T>, IEnumerable<T>, IEnumerable
    {
        public T this[int index] { get => _items[index]; set => _items[index] = value; }
        public int Count { get; }
        public bool IsReadOnly { get; }

        private readonly List<T> _items;
        private readonly IComparer<T> _comparer;

        public SortedList() : this(0) { }
        public SortedList(int capacity) : this(capacity, default) { }
        public SortedList(IComparer<T> comparer) : this(0, comparer) { }
        public SortedList(int capacity, IComparer<T> comparer)
        {
            _items = new List<T>(capacity);
            _comparer = Comparer<T>.Default;
        }
        public void Add(T item)
        {
            _items.Add(item);
            _items.Sort(_comparer);
        }
        public void Clear() => _items.Clear();
        public bool Contains(T item) => _items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        public bool Remove(T item) => _items.Remove(item);
        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }
}
