using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class Conversion
    {
        private static readonly Regex booleanTrueRegex = new Regex("t(?:rue)?|y(?:es)?|on|1", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex booleanFalseRegex = new Regex("f(?:alse)?|n(?:o)?|off|0", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex hexadecimalRegex = new Regex("(?<=^(?:0x|#)?)[a-f0-9]+(?=$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex binaryRegex = new Regex("(?<=^b?)[01]+(?=$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly TypeCode[] integerTypeCodes = { TypeCode.Char, TypeCode.SByte, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64 };

        public static T Convert<T>(this IConvertible value) where T : IConvertible
        {
            Type toType = typeof(T);
            try
            {
                return (T)System.Convert.ChangeType(value, toType);
            }
            catch(FormatException) // The data couldn't be converted automatically, but maybe we can try some alternatives...
            {
                if(toType.IsValueType)
                {
                    T toInstance = default;
                    TypeCode toTypeCode = toInstance.GetTypeCode();

                    // Maybe the data is a string starting with '0x' or 'b' that we're trying to convert to an integer:
                    if(value.GetTypeCode() == TypeCode.String && integerTypeCodes.Contains(toTypeCode))
                    {
                        string str = (string)value;
                        if(RegexExtensions.TryMatch(hexadecimalRegex, str, out string capture))
                        {
                            return (T)System.Convert.ChangeType(System.Convert.ToUInt64(capture, 16), toType);
                        }
                        if(RegexExtensions.TryMatch(binaryRegex, str, out capture))
                        {
                            return (T)System.Convert.ChangeType(System.Convert.ToUInt64(capture, 2), toType);
                        }
                    }
                }

                throw; // I couldn't adjust the conversion to make it work. Give up and let the exception continue to unwind the stack.
            }
        }
        public static IEnumerable<T> ConvertAll<T>(this IEnumerable<IConvertible> items) where T : IConvertible => items.Select(item => Convert<T>(item));
        public static T[] ConvertAll<T>(this IConvertible[] items) where T : IConvertible => items.ConvertAll<T>().ToArray();
        public static bool Equals<T>(IEnumerable<T> a, IEnumerable<T> b) => a == null && b == null || Enumerable.SequenceEqual(a, b);
        public static bool Equals<T>(T a, T b) => a?.Equals(b) ?? b == null;
        public static string Serialize(this IConvertible value, int significantDigits)
        {
            switch(value?.GetTypeCode() ?? TypeCode.Empty)
            {
                case TypeCode.Single: return $"{((float)value).Format(significantDigits, true)}";
                case TypeCode.Double: return $"{((double)value).Format(significantDigits, true)}";
                default: return Serialize(value);
            }
        }
        public static string Serialize(this IConvertible value)
        {
            switch(value?.GetTypeCode() ?? TypeCode.Empty)
            {
                case TypeCode.Empty:    return "null";
                case TypeCode.Boolean:  return (bool)value ? "true" : "false";
                case TypeCode.SByte:    return $"{(sbyte)value}";
                case TypeCode.Int16:    return $"{(short)value}";
                case TypeCode.Int32:    return $"{(int)value}";
                case TypeCode.Int64:    return $"{(long)value}";
                case TypeCode.Byte:     return $"0x{(byte)value:X2}";
                case TypeCode.UInt16:   return $"0x{(ushort)value:X4}";
                case TypeCode.UInt32:   return $"0x{(uint)value:X8}";
                case TypeCode.UInt64:   return $"0x{(ulong)value:X6}";
                case TypeCode.Single:   return $"{(float)value}";
                case TypeCode.Double:   return $"{(float)value}";
                case TypeCode.Decimal:  return $"{(decimal)value}";
                case TypeCode.DateTime: return $"{(DateTime)value}";
                case TypeCode.String:   return $"\"{(string)value}\"";
                default: return null;
            }
        }
        public static T Parse<T>(string text)
        {
            if(text == null)
            {
                throw new ArgumentNullException("value");
            }
            else if(text.Length > 0)
            {
                Type type = typeof(T);
                if(type == typeof(bool))
                {
                    if(booleanTrueRegex.IsMatch(text))
                    {
                        return (T)(object)true;
                    }
                    else if(booleanFalseRegex.IsMatch(text))
                    {
                        return (T)(object)false;
                    }
                    throw new FormatException($"'{text}' could not be parsed as a bool.");
                }
                else if(type == typeof(int))
                {
                    return (T)(object)int.Parse(text);
                }
                else if(type == typeof(uint))
                {
                    if(text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)(object)System.Convert.ToUInt32(text, 16);
                    }
                    else if(text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)(object)System.Convert.ToUInt32(text, 2);
                    }
                    else
                    {
                        return (T)(object)uint.Parse(text);
                    }
                }
                else if(type.IsEnum)
                {
                    return (T)Enum.Parse(type, text);
                }
                else
                {
                    return (T)System.Convert.ChangeType(text, typeof(T));
                }
            }
            else
            {
                throw new ArgumentException("string parameter 'value' cannot be empty");
            }
        }
        public static T TryParse<T>(string value, T defaultValue = default(T))
        {
            try
            {
                return Parse<T>(value);
            }
            catch { }
            return defaultValue;
        }
        public static bool TryParse<T>(string value, out T result, T defaultValue = default(T))
        {
            try
            {
                result = Parse<T>(value);
                return true;
            }
            catch { }
            result = defaultValue;
            return false;
        }
        public static byte[] StructToByteArray(object source, bool endianSwap = false)
        {
            int size = Marshal.SizeOf(source);
            byte[] data = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(source, ptr, true);
            Marshal.Copy(ptr, data, 0, size);
            Marshal.FreeHGlobal(ptr);

            if(endianSwap)
            {
                for(int i = 0; i < data.Length; i += 4)
                {
                    byte tmp = data[i];
                    data[i] = data[i + 3];
                    data[i + 3] = tmp;
                    tmp = data[i + 2];
                    data[i + 2] = data[i + 1];
                    data[i + 1] = tmp;
                }
            }

            return data;
        }
        public static byte[] UInt32toByteArray(uint value, bool swap = false)
        {
            byte[] data = BitConverter.GetBytes(value);

            if(swap)
            {
                Array.Reverse(data);
            }

            return data;
        }
        public static uint ByteArraytoUInt32(byte[] value, bool swap = false)
        {
            if(swap)
            {
                Array.Reverse(value);
            }

            return BitConverter.ToUInt32(value, 0);
        }
        public static Type GenerateType(Type baseType, int arrayRank = 0) => arrayRank == 0 ? baseType : baseType.MakeArrayType(arrayRank);
        public static Type GenerateType(string baseTypeName, int arrayRank = 0) => arrayRank == 0 ? Type.GetType(baseTypeName) : Type.GetType(baseTypeName).MakeArrayType(arrayRank);
        public static Type GetElementType(Type type)
        {
            if(type.IsArray)
            {
                type = type.GetElementType();
            }
            return type;
        }
    }
}