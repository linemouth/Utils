using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.ArgParse
{
    public enum ArgumentMode
    {
        Store, // Stores the passed value.
        StoreConst, // Stores a predetermined value.
        Append, // Appends the passed value to a list.
        AppendConst, // Appends a predetermined value to a list.
        Count, // Counts the number of times an argument appears.
        Command // Indicate control flow of the program
    }
}
