using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class ListEnumerator<T> : IEnumerator<T>
    {
        public int Index { get; set; } = 0;
        public bool HasNext => (List.Count - Index) > 1;
        public T Current
        {
            get => Index < List.Count ? List[Index] : default;
            set
            {
                lock(this)
                {
                    if(Index < List.Count)
                    {
                        List[Index] = value;
                    }
                }
            }
        }
        object IEnumerator.Current { get; }

        private readonly List<T> List;

        public ListEnumerator(List<T> list) => List = list;
        public bool MoveNext()
        {
            lock(this)
            {
                ++Index;
                return Index < List.Count;
            }
        }
        public bool TryGetNext(out T value)
        {
            lock(this)
            {
                if(MoveNext())
                {
                    value = Current;
                    return true;
                }
            }
            value = default;
            return false;
        }
        public List<T> GetNext(int maxCount)
        {
            lock(this)
            {
                int remainingItems = List.Count - maxCount - 1;
                if(remainingItems < maxCount)
                {
                    maxCount = remainingItems;
                }
                List<T> results = List.GetRange(Index + 1, maxCount);
                Index += maxCount;
                return results;
            }
        }
        public void Reset()
        {
            lock(this)
            {
                Index = 0;
            }
        }
        public void Dispose() { }
    }
}
