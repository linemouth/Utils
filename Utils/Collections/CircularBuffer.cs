using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Utils.Collections
{
    public class CircularBuffer<T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>The total number of items in the buffer.</summary>
        public int Count { get; private set; } = 0;
        /// <summary>If the buffer is full, can it be expanded to accomodate more items?</summary>
        public bool IsExpandable { get; set; } = true;
        /// <summary>The total number of items that can be stored without resizing the buffer.</summary>
        public int Capacity
        {
            get => buffer.Length;
            set => Resize(value);
        }
        public T this[int index] {
            get {
                index = CalculateIndex(index);
                return buffer[index];
            }
            set
            {
                index = CalculateIndex(index);
                buffer[index] = value;
            }
        }

        private T[] buffer;
        private int head = 0; // The index at which new elements are inserted.
        private int tail = 0; // The index from which the oldest elements are read.

        public CircularBuffer() {
            buffer = new T[0];
        }
        public CircularBuffer(int capacity, bool canGrow = true)
        {
            IsExpandable = canGrow;
            buffer = new T[capacity];
        }
        public CircularBuffer(IEnumerable<T> items, bool canGrow = true)
        {
            IsExpandable = canGrow;
            buffer = items.ToArray();
            Count = buffer.Length;
        }
        public CircularBuffer(int capacity, IEnumerable<T> items, bool canGrow = true) : this(capacity, canGrow)
        {
            if(Capacity < items.Count())
            {
                throw new InvalidOperationException("CircularBuffer cannot accomodate all items.");
            }
            Array.Copy(items.ToArray(), buffer, items.Count());
        }
        public void Enqueue(T item)
        {
            int capacity = Capacity;
            if (Count == capacity)
            {
                if (!IsExpandable)
                {
                    throw new InvalidOperationException("Buffer is full");
                }
                Resize(Math.Max(capacity * 2, 1));
            }

            Enqueue_Internal(item);
        }
        public bool TryEnqueue(T item)
        {
            int capacity = Capacity;
            if (Count == capacity)
            {
                if (!IsExpandable)
                {
                    return false;
                }
                Resize(Math.Max(capacity * 2, 1));
            }

            Enqueue_Internal(item);
            return true;
        }
        public T Dequeue()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Buffer is empty");
            }

            return Dequeue_Internal();
        }
        public bool TryDequeue(out T value)
        {
            if (Count > 0)
            {
                value = Dequeue_Internal();
                return true;
            }
            value = default;
            return false;
        }
        public void Clear() => head = tail = Count = 0;
        public T Peek()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Buffer is Empty");
            }
            return buffer[tail];
        }
        public bool TryPeek(out T value)
        {
            if(Count > 0)
            {
                value = buffer[tail];
                return true;
            }
            value = default;
            return false;
        }
        public bool Contains(T item)
        {
            if (Count > 0)
            {
                foreach (T element in this)
                {
                    if (EqualityComparer<T>.Default.Equals(element, item))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (Count > 0)
            {
                if (tail < head)
                {
                    Array.Copy(buffer, tail, array, 0, Count);
                }
                else
                {
                    int endCount = Capacity - tail;
                    Array.Copy(buffer, tail, array, 0, endCount);
                    Array.Copy(buffer, 0, array, endCount, head);
                }
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            if(Count > 0)
            {
                for (int i = tail; i != head; i = ++i % Capacity)
                {
                    yield return buffer[i];
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int CalculateIndex(int index)
        {
            if (index < 0)
            {
                index = Count + index;
            }
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException($"Index out of range: {index}");
            }
            index += tail;
            if (index >= Capacity)
            {
                index -= Capacity;
            }
            return index;
        }
        private void Resize(int capacity)
        {
            if (capacity < Count)
            {
                throw new InvalidOperationException("Cannot resize CircularBuffer to fewer items than it currently contains.");
            }

            T[] array = new T[capacity];
            if (Count > 0)
            {
                CopyTo(array, 0);
            }
            buffer = array;
            head = Count;
            tail = 0;
        }
        private void Enqueue_Internal(T item)
        {
            int capacity = Capacity;
            buffer[head] = item;
            ++head;
            ++Count;
            if (head >= capacity)
            {
                head -= capacity;
            }
        }
        private T Dequeue_Internal()
        {
            int capacity = Capacity;
            T item = buffer[tail];
            ++tail;
            --Count;
            if (tail >= capacity)
            {
                tail -= capacity;
            }
            return item;
        }
    }
}
