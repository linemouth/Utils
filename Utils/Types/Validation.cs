using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    public static class Validation
    {
        public static void ThrowIfNegative(long value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name, "Argument cannot be negative.");
            }
        }
        public static void ThrowIfNegative(double value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name, "Argument cannot be negative.");
            }
        }
        public static void ThrowIfOutsideRange(long value, long minimum, long maximum, string name)
        {
            if (value < minimum)
            {
                throw new ArgumentOutOfRangeException(name, $"Argument cannot be less than {minimum}.");
            }
            if (value > maximum)
            {
                throw new ArgumentOutOfRangeException(name, $"Argument cannot be greater than {maximum}.");
            }
        }
        public static void ThrowIfOutsideRange(double value, double minimum, double maximum, string name)
        {
            if (value < minimum)
            {
                throw new ArgumentOutOfRangeException(name, $"Argument cannot be less than {minimum}.");
            }
            if (value > maximum)
            {
                throw new ArgumentOutOfRangeException(name, $"Argument cannot be greater than {maximum}.");
            }
        }
        public static void ThrowIfNull(object value, string name)
        {
            if(value == null)
            {
                throw new ArgumentNullException(name, "Argument cannot be null.");
            }
        }
        public static void ThrowIfEmpty<T>(IEnumerable<T> collection, string name)
        {
            if (!collection.Any())
            {
                throw new ArgumentOutOfRangeException(name, "Argument cannot be negative.");
            }
        }
    }
}
