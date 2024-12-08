using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Utils
{
    public enum EnqueueMode {
        Throw,
        Expand,
        DropNew,
        DropOld
    }
    public class CircularBuffer<T> : IEnumerable<T>, IEnumerable
    {
        /// <summary>The total number of items in the buffer.</summary>
        public int Count { get; private set; } = 0;
        /// <summary>Determines how to handle adding data when the buffer is full.</summary>
        public EnqueueMode EnqueueMode { get; set; } = EnqueueMode.Throw;
        /// <summary>The total number of items that can be stored without resizing the buffer.</summary>
        public int Capacity
        {
            get => buffer.Length;
            set => Resize(value);
        }
        public int Available => Capacity - Count;
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
        public CircularBuffer(int capacity, EnqueueMode enqueueMode = EnqueueMode.Throw)
        {
            EnqueueMode = enqueueMode;
            buffer = new T[capacity];
        }
        public CircularBuffer(IEnumerable<T> items, EnqueueMode enqueueMode = EnqueueMode.Throw)
        {
            EnqueueMode = enqueueMode;
            buffer = items.ToArray();
            Count = buffer.Length;
        }
        public CircularBuffer(int capacity, IEnumerable<T> items, EnqueueMode enqueueMode = EnqueueMode.Throw) : this(capacity, enqueueMode)
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
            if (Count < capacity)
            {
                Enqueue_Internal(item);
            }
            else
            {
                switch (EnqueueMode)
                {
                    default:
                    case EnqueueMode.Throw:
                        throw new InvalidOperationException("Buffer does not have enough space to accept new item.");
                    case EnqueueMode.Expand:
                        Resize(Math.Max(capacity * 2, 1));
                        Enqueue_Internal(item);
                        break;
                    case EnqueueMode.DropNew:
                        // Do nothing
                        break;
                    case EnqueueMode.DropOld:
                        Dequeue_Internal();
                        Enqueue_Internal(item);
                        break;
                }
            }
        }
        public bool TryEnqueue(T item)
        {
            int capacity = Capacity;
            if (capacity == 0)
            {
                return false;
            }

            if (Count < capacity)
            {
                Enqueue_Internal(item);
                return true;
            }
            else
            {
                switch (EnqueueMode)
                {
                    default:
                    case EnqueueMode.Throw:
                        return false;
                    case EnqueueMode.Expand:
                        Resize(Math.Max(capacity * 2, 1));
                        Enqueue_Internal(item);
                        return true;
                    case EnqueueMode.DropNew:
                        // Do nothing
                        return true;
                    case EnqueueMode.DropOld:
                        Dequeue_Internal();
                        Enqueue_Internal(item);
                        return true;
                }
            }
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
        public void EnqueueRange(T[] items) => EnqueueRange(items, 0, items.Length);
        public void EnqueueRange(T[] items, int index, int count)
        {
            int available = Available;
            int capacity = Capacity;
            if (count <= available)
            {
                CopyFrom_Internal(items, index, count);
                Count += count;
            }
            else
            {
                switch (EnqueueMode)
                {
                    default:
                    case EnqueueMode.Throw:
                        throw new InvalidOperationException("Buffer does not have enough space to accept all new items.");
                    case EnqueueMode.Expand:
                        Resize(Math.Max(Capacity * 2, Count + count));
                        CopyFrom_Internal(items, index, count);
                        Count += count;
                        break;
                    case EnqueueMode.DropNew:
                        CopyFrom_Internal(items, index, available);
                        Count += available;
                        break;
                    case EnqueueMode.DropOld:
                        if (count >= Capacity)
                        {
                            // Replace the buffer with the last items in the source items.
                            Array.Copy(items, count - Capacity, buffer, 0, Capacity);
                            tail = head = 0;
                        }
                        else
                        {
                            // Copy the last items to the buffer.
                            CopyFrom_Internal(items, index + head, count);
                            tail = head;
                        }
                        Count = Capacity;
                        break;
                }
            }
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
        private void CopyFrom_Internal(T[] source, int index, int count)
        {
            // Copy data
            int capacity = Capacity;
            int count1 = capacity - head;
            if(count <= count1)
            {
                Array.Copy(source, index, buffer, head, count);
            }
            else
            {
                int count2 = count - count1;
                Array.Copy(source, index, buffer, head, count1);
                index += count1;
                Array.Copy(source, index, buffer, 0, count2);
            }

            // Update count and head
            head += count;
            if (head >= capacity)
            {
                head -= capacity;
            }
        }
    }
}
