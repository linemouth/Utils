using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public static class EnumeratorExtensions
    {
        public static List<T> GetNext<T>(this IEnumerator<T> enumerator, int maxCount)
        {
            List<T> results = new List<T>(maxCount);
            while(results.Count < maxCount && enumerator.MoveNext())
            {
                results.Add(enumerator.Current);
            }
            return results;
        }
    }
}
