using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class PriorityQueue<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection<T>
    {
        public int Count { get; private set; }
        public bool IsReadOnly => false;

        private List<T> items = new List<T>();
        private IComparer<T> comparer;

        public PriorityQueue() { }
        public PriorityQueue(IEnumerable<T> collection) : this(collection, Comparer<T>.Default) { }
        public PriorityQueue(IComparer<T> comparer) => this.comparer = comparer;
        public PriorityQueue(IEnumerable<T> collection, IComparer<T> comparer) : this(comparer)
        {
            foreach(T item in collection)
            {
                Add(item);
            }
        }
        public void Add(T item)
        {
            int count = items.Count;
            if(count == 0)
            {
                items.Add(item);
            }
            else
            {
                int min = 0;
                int max = count;
                int delta = max - min;
                int index = (delta >> 1) + min;

                while(delta > 0)
                {
                    int order = comparer.Compare(item, items[index]);
                    if(order == 0)
                    {
                        break;
                    }
                    else if(order > 0)
                    {
                        min = index + 1;
                    }
                    else if(order < 0)
                    {
                        max = index;
                    }
                    delta = max - min;
                    index = (delta >> 1) + min;
                }

                items.Insert(index, item);
            }
        }
        public void Clear() => items.Clear();
        public bool Contains(T item) => items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
        public T Dequeue()
        {
            T item = items[0];
            items.RemoveAt(0);
            return item;
        }
        public bool TryDequeue(out T item)
        {
            if(items.Count > 0)
            {
                item = Dequeue();
                return true;
            }
            item = default;
            return false;
        }
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Remove(T item) => items.Remove(item);
    }
}
