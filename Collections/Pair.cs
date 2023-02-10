using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Collections
{
    public class Pair<Tkey, TValue> : IEquatable<Pair<Tkey, TValue>>
    {
        public Tkey Key;
        public TValue Value;

        public Pair(Tkey key, TValue value)
        {
            Key = key;
            Value = value;
        }
        public bool Equals(Pair<Tkey, TValue> other) => Key.Equals(other.Key) && Value.Equals(other.Value);
    }
}
