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
        public int Count => items.Count;
        public bool IsReadOnly => false;

        private List<T> items = new List<T>();
        private readonly IComparer<T> comparer;

        public PriorityQueue() : this(Comparer<T>.Default) { }
        public PriorityQueue(IComparer<T> comparer) => this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        public PriorityQueue(IEnumerable<T> collection) : this(collection, Comparer<T>.Default) { }
        public PriorityQueue(IEnumerable<T> collection, IComparer<T> comparer) : this(comparer)
        {
            foreach(T item in collection)
            {
                Add(item);
            }
        }
        public void Add(T item)
        {
            items.Add(item);
            HeapifyUp(items.Count - 1);
        }
        public void Clear() => items.Clear();
        public bool Contains(T item) => items.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
        public T Dequeue()
        {
            if (items.Count == 0)
            {
                throw new InvalidOperationException("The priority queue is empty.");
            }

            T root = items[0];
            T lastItem = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);

            if (items.Count > 0)
            {
                items[0] = lastItem;
                HeapifyDown(0);
            }

            return root;
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
        public bool TryPeek(out T item)
        {
            if(items.Count > 0)
            {
                item = items[0];
                return true;
            }
            item = default;
            return false;
        }
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Remove(T item) => items.Remove(item);

        private void HeapifyUp(int index)
        {
            T item = items[index];
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (comparer.Compare(item, items[parentIndex]) >= 0) break;

                items[index] = items[parentIndex];
                index = parentIndex;
            }
            items[index] = item;
        }
        private void HeapifyDown(int index)
        {
            int childIndex = 2 * index + 1;
            T item = items[index];

            while (childIndex < items.Count)
            {
                int rightChildIndex = childIndex + 1;
                if (rightChildIndex < items.Count && comparer.Compare(items[rightChildIndex], items[childIndex]) < 0)
                {
                    childIndex = rightChildIndex;
                }

                if (comparer.Compare(item, items[childIndex]) <= 0)
                {
                    break;
                }

                items[index] = items[childIndex];
                index = childIndex;
                childIndex = 2 * index + 1;
            }

            items[index] = item;
        }
    }
}
