using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.Queries
{
    public class StringResolver
    {
        public string Pattern;
        public readonly List<Option> Options = new List<Option>();

        public StringResolver(string pattern, IEnumerable<Option> options)
        {
            Pattern = pattern;
            Options = options.ToList();
        }
    }
}
