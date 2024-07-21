using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class EnumExtensions
    {
        public static bool HasAny<T>(this T flags, T flagToTest) where T : System.Enum
        {
            if(!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                throw new InvalidOperationException("The given enum type is not decorated with Flag attribute.");
            }
            if(flagToTest.Equals(0))
            {
                throw new ArgumentOutOfRangeException(nameof(flagToTest), "Value must not be 0");
            }
            int a = Convert.ToInt32(flags);
            int b = Convert.ToInt32(flagToTest);
            return (a & b) != 0;
        }
        public static bool HasAll<T>(this T flags, T flagToTest) where T : System.Enum
        {
            if(!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                throw new InvalidOperationException("The given enum type is not decorated with Flag attribute.");
            }
            if(flagToTest.Equals(0))
            {
                throw new ArgumentOutOfRangeException(nameof(flagToTest), "Value must not be 0");
            }
            int a = Convert.ToInt32(flags);
            int b = Convert.ToInt32(flagToTest);
            return (a & b) == b;
        }
        public static bool HasNone<T>(this T flags, T flagToTest) where T : System.Enum
        {
            if(!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                throw new InvalidOperationException("The given enum type is not decorated with Flag attribute.");
            }
            if(flagToTest.Equals(0))
            {
                throw new ArgumentOutOfRangeException(nameof(flagToTest), "Value must not be 0");
            }
            int a = Convert.ToInt32(flags);
            int b = Convert.ToInt32(flagToTest);
            return (a & b) == 0;
        }
        public static bool HasNotAll<T>(this T flags, T flagToTest) where T : System.Enum
        {
            if(!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                throw new InvalidOperationException("The given enum type is not decorated with Flag attribute.");
            }
            if(flagToTest.Equals(0))
            {
                throw new ArgumentOutOfRangeException(nameof(flagToTest), "Value must not be 0");
            }
            int a = Convert.ToInt32(flags);
            int b = Convert.ToInt32(flagToTest);
            int intersection = a & b;
            return intersection != 0 && intersection != b;
        }
        public static bool HasAtMost<T>(this T flags, T flagToTest) where T : System.Enum
        {
            if(!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                throw new InvalidOperationException("The given enum type is not decorated with Flag attribute.");
            }
            if(flagToTest.Equals(0))
            {
                throw new ArgumentOutOfRangeException(nameof(flagToTest), "Value must not be 0");
            }
            int a = Convert.ToInt32(flags);
            int b = Convert.ToInt32(flagToTest);
            return (a & ~b) == 0;
        }
        public static T Filter<T>(this T flags, T flagToTest) where T : System.Enum
        {
            if(!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                throw new InvalidOperationException("The given enum type is not decorated with Flag attribute.");
            }
            if(flagToTest.Equals(0))
            {
                throw new ArgumentOutOfRangeException(nameof(flagToTest), "Value must not be 0");
            }
            int a = Convert.ToInt32(flags);
            int b = Convert.ToInt32(flagToTest);
            return (T)(object)(a & b);
        }
        public static T Mask<T>(this T flags, T flagToTest) where T : System.Enum
        {
            if(!Attribute.IsDefined(typeof(T), typeof(FlagsAttribute)))
            {
                throw new InvalidOperationException("The given enum type is not decorated with Flag attribute.");
            }
            if(flagToTest.Equals(0))
            {
                throw new ArgumentOutOfRangeException(nameof(flagToTest), "Value must not be 0");
            }
            int a = Convert.ToInt32(flags);
            int b = Convert.ToInt32(flagToTest);
            return (T)(object)(a & ~b);
        }
    }
}
