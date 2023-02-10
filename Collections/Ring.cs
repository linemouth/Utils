using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class Ring<T> : IList<T>, ICollection<T>, IEnumerable<T>
    {
        public int Capacity => array.Length;
        public int Count { get; private set; }
        public bool IsReadOnly => false;
        public T this[int index]
        {
            get
            {
                if(index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException("Invalid index in Ring buffer.");
                }
                return array[ClampIndex(tail + index)];
            }
            set
            {
                if(index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException("Invalid index in Ring buffer.");
                }
                array[ClampIndex(tail + index)] = value;
            }
        }

        protected T[] array;
        protected int head;
        protected int tail;
        protected int count;

        public Ring(int capacity) => array = new T[capacity];
        public bool Push(T item)
        {
            bool result = false;
            if(Count < Capacity)
            {
                // Add value
                array[head] = item;

                // Update indices
                ++Count;
                ++head;
                if(head >= Capacity)
                {
                    // Wrap head index
                    head = 0;
                }
                result = true;
            }
            return result;
        }
        public void Add(T item) => this.Push(item);

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        protected int ClampIndex(int index) => Math.Repeat(index, 0, Capacity);
    }
}
