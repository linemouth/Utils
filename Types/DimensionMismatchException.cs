using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class DimensionMismatchException : ArgumentException
    {
        public DimensionMismatchException(string message) : base(message)
        {
        }
    }
}
