using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils.Collections
{
    public class CircularBuffer<T> : IEnumerable<T>, IEnumerable
    {
        public int Count { get; private set; } = 0;
        public bool CanGrow { get; set; } = true;
        public int Capacity
        {
            get => buffer?.Length ?? 0;
            set => Resize(value);
        }

        private T[] buffer = null;
        private int head = 0;
        private int tail = 0;

        public CircularBuffer() { }
        public CircularBuffer(int capacity, bool canGrow = true)
        {
            CanGrow = canGrow;
            if (capacity > 0)
            {
                buffer = new T[capacity];
            }
        }
        public CircularBuffer(IEnumerable<T> items, bool canGrow = true)
        {
            CanGrow = canGrow;
            buffer = items.ToArray();
            Count = buffer.Length;
        }
        public CircularBuffer(int capacity, IEnumerable<T> items, bool canGrow = true)
        {
            CanGrow = canGrow;
            Capacity = capacity;
            if (!CanGrow && items.Count() > Capacity)
            {
                throw new InvalidOperationException("CircularBuffer cannot accomodate all items.");
            }
            Array.Copy(items.ToArray(), buffer, items.Count());
        }
        public void Enqueue(T item)
        {
            if (Count == buffer.Length)
            {
                if (!CanGrow)
                {
                    throw new InvalidOperationException("Buffer is full");
                }
                Resize(Capacity * 2);
            }

            buffer[head] = item;
            head = ++head % Capacity;
            Count++;
        }
        public T Dequeue()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Buffer is empty");
            }

            T item = buffer[tail];
            tail = ++tail % Capacity;
            Count--;
            return item;
        }
        public void Clear() => head = tail = Count = 0;
        public T Peek()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Buffer is Empty");
            }
            return buffer[head];
        }
        public bool Contains(T item)
        {
            foreach(T element in this)
            {
                if(EqualityComparer<T>.Default.Equals(element, item))
                {
                    return true;
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

        private void Resize(int capacity)
        {
            if (capacity < Count)
            {
                throw new InvalidOperationException("Cannot resize CircularBuffer to fewer items than it currently contains.");
            }

            T[] array = new T[capacity];
            CopyTo(array, 0);

            buffer = array;
            head = Count;
            tail = 0;
        }
    }
}
