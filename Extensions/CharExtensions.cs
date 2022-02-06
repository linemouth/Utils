using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public static class CharExtensions
    {
        public static bool IsOneOfThese(this char c, IEnumerable<char> chars)
        {
            foreach(char testChar in chars)
            {
                if(c == testChar)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsNewline(this char c) => IsOneOfThese(c, "\n\r");
    }
}
