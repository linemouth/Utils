using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public struct GradientStop<P, V>
    {
        public P position;
        public V value;

        public GradientStop(P position, V value)
        {
            this.position = position;
            this.value = value;
        }
    }
}
