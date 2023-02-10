using System;
using System.Collections.Generic;

namespace Utils
{
    public class LookupTable : List<KeyValuePair<double, double>>
    {
        public void Add(double x, double y) => Add(new KeyValuePair<double, double>(x, y));
        public double Sample(double x)
        {
            int min = 0;
            int max = Count - 1;
            while(max - min > 1)
            {
                int index = (max - min) / 2;
                KeyValuePair<double, double> entry = this[index];
                if (x == entry.Key)
                {
                    return entry.Value;
                }
                else if(x < entry.Key)
                {
                    max = index;
                }
                else
                {
                    min = index;
                }
            }
            KeyValuePair<double, double> a = this[min];
            KeyValuePair<double, double> b = this[max];
            double t = Math.InverseLerp(a.Key, b.Key, x);
            return Math.Lerp(a.Value, b.Value, t);
        }
    }
}
