using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Utils.ArgParse
{
    public static class ArgParseExtensions
    {
        public static IEnumerable<string> FlattenItems(this IEnumerable<string> arguments)
        {
            List<string> items = new List<string>();
            foreach(string argument in arguments)
            {
                if(File.Exists(argument))
                {
                    items.AddRange(File.ReadAllLines(argument));
                }
                else
                {
                    items.Add(argument);
                }
            }
            return items;
        }
    }
}
