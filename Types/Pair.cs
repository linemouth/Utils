using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class Pair<T, U>
    {
        public T A;
        public U B;

        public Pair(T a, U b)
        {
            A = a;
            B = b;
        }
    }
}
