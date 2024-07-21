using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    public static class CharExtensions
    {
        public static bool IsNewline(this char c) => Enumerable.Contains("\n\r", c);
    }
}
