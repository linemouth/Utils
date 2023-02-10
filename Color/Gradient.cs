using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public abstract class Gradient<P, V> : ICollection<GradientStop<P, V>> where P : IEquatable<P>
    {
        public int Count => stops.Count;
        public bool IsReadOnly => false;
        public abstract P Min { get; }
        public abstract P Max { get; }

        protected List<GradientStop<P, V>> stops;

        public void Add(GradientStop<P, V> item) => stops.Add(item);
        public void Add(P position, V value) => stops.Add(new GradientStop<P, V>(position, value));
        public void Clear() => stops.Clear();
        public bool Contains(GradientStop<P, V> item) => stops.Contains(item);
        public void CopyTo(GradientStop<P, V>[] array, int arrayIndex) => stops.CopyTo(array, arrayIndex);
        public IEnumerator<GradientStop<P, V>> GetEnumerator() => stops.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Remove(GradientStop<P, V> item) => stops.Remove(item);
        public bool Remove(P position)
        {
            int index = stops.FindLastIndex(stop => stop.position.Equals(position));
            if(index >= 0)
            {
                stops.RemoveAt(index);
                return true;
            }
            return false;
        }
        public abstract V Sample(P position);
    }
}
