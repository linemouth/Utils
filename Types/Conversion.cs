using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class Conversion
    {
        public enum TypeCategory
        {
            Null,
            Bool,
            Signed,
            Unsigned,
            Float,
            String
        }

        private static readonly Regex booleanTrueRegex = new Regex("t(?:rue)?|y(?:es)?|on|1", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex booleanFalseRegex = new Regex("f(?:alse)?|n(?:o)?|off|0", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex hexadecimalRegex = new Regex("(?<=^(?:0x|0X|#)?)[a-fA-F0-9]+(?=$)", RegexOptions.Compiled);
        private static readonly Regex binaryRegex = new Regex("(?<=^b?)[01]+(?=$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex scientificNotationRegex = new Regex(@"\b(?<value>[\d\.]+)e(?<order>[-\d]+)\b", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly Regex siNotationRegex = new Regex($"\b(?<value>[\\d\\.]+)\\s*(?<prefix>[{string.Join(null, Math.SiPrefixes.Keys)}]?)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly TypeCode[] integerTypeCodes = { TypeCode.Char, TypeCode.SByte, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64 };
        private static readonly TypeCode[] floatTypeCodes = { TypeCode.Single, TypeCode.Double, TypeCode.Decimal };

        public static unsafe float ReinterpretAsFloat(this int value) => *(float*)&value;
        public static unsafe float ReinterpretAsFloat(this uint value) => *(float*)&value;
        public static unsafe double ReinterpretAsDouble(this long value) => *(float*)&value;
        public static unsafe double ReinterpretAsDouble(this ulong value) => *(float*)&value;
        public static unsafe int ReinterpretAsInt(this float value) => *(int*)&value;
        public static unsafe uint ReinterpretAsUint(this float value) => *(uint*)&value;
        public static unsafe long ReinterpretAsLong(this double value) => *(long*)&value;
        public static unsafe ulong ReinterpretAsUlong(this double value) => *(ulong*)&value;
        public static IConvertible UnboxIConvertible(this object o)
        {
            if(o is IConvertible) { return (IConvertible)o; }
            else if (o is byte) { return (byte)o; }
            else if (o is sbyte) { return (sbyte)o; }
            else if (o is ushort) { return (ushort)o; }
            else if (o is short) { return (short)o; }
            else if (o is uint) { return (uint)o; }
            else if (o is int) { return (int)o; }
            else if (o is ulong) { return (ulong)o; }
            else if (o is long) { return (long)o; }
            else if (o is float) { return (float)o; }
            else if (o is double) { return (double)o; }
            else if (o is decimal) { return (decimal)o; }
            else if (o is string) { return (string)o; }
            throw new ArgumentException($"Argument of type {o.GetType()} is not IConvertible.");
        }
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
        public static bool TryParseBinary(string text, out byte value)
        {
            if(binaryRegex.TryMatch(text, out string capture))
            {
                value = System.Convert.ToByte(capture, 2);
                return true;
            }
            value = 0;
            return false;
        }
        public static bool TryParseBinary(string text, out ushort value)
        {
            if(binaryRegex.TryMatch(text, out string capture))
            {
                value = System.Convert.ToUInt16(capture, 2);
                return true;
            }
            value = 0;
            return false;
        }
        public static bool TryParseBinary(string text, out uint value)
        {
            if(binaryRegex.TryMatch(text, out string capture))
            {
                value = System.Convert.ToUInt32(capture, 2);
                return true;
            }
            value = 0;
            return false;
        }
        public static bool TryParseBinary(string text, out ulong value)
        {
            if(binaryRegex.TryMatch(text, out string capture))
            {
                value = System.Convert.ToUInt64(capture, 2);
                return true;
            }
            value = 0;
            return false;
        }
        public static bool TryParseHex(string text, out byte value)
        {
            if(hexadecimalRegex.TryMatch(text, out string capture))
            {
                value = System.Convert.ToByte(capture, 16);
                return true;
            }
            value = 0;
            return false;
        }
        public static bool TryParseHex(string text, out ushort value)
        {
            if(hexadecimalRegex.TryMatch(text, out string capture))
            {
                value = System.Convert.ToUInt16(capture, 16);
                return true;
            }
            value = 0;
            return false;
        }
        public static bool TryParseHex(string text, out uint value)
        {
            if(hexadecimalRegex.TryMatch(text, out string capture))
            {
                value = System.Convert.ToUInt32(capture, 16);
                return true;
            }
            value = 0;
            return false;
        }
        public static bool TryParseHex(string text, out ulong value)
        {
            if(hexadecimalRegex.TryMatch(text, out string capture))
            {
                value = System.Convert.ToUInt64(capture, 16);
                return true;
            }
            value = 0;
            return false;
        }
        public static bool TryParse(string text, out IConvertible result, out TypeCategory detectedType)
        {
            detectedType = TypeCategory.Null;
            result = null;

            if (text.StartsWith("\"") && text.EndsWith("\""))
            {
                result = text.Substring(1, text.Length - 2);
                detectedType = TypeCategory.String;
                return true;
            }
            else if (long.TryParse(text, out long longValue))
            {
                result = longValue;
                detectedType = TypeCategory.Signed;
                return true;
            }
            else if (ulong.TryParse(text, out ulong ulongValue))
            {
                result = ulongValue;
                detectedType = TypeCategory.Unsigned;
                return true;
            }
            else if (booleanTrueRegex.IsMatch(text))
            {
                result = true;
                detectedType = TypeCategory.Bool;
                return true;
            }
            else if (booleanFalseRegex.IsMatch(text))
            {
                result = false;
                detectedType = TypeCategory.Bool;
                return true;
            }
            else if (double.TryParse(text, out double doubleValue))
            {
                result = doubleValue;
                detectedType = TypeCategory.Float;
                return true;
            }
            else if (TryParseHex(text, out ulongValue))
            {
                result = ulongValue;
                detectedType = TypeCategory.Unsigned;
            }
            else if (scientificNotationRegex.TryMatch(text, out Match match) && double.TryParse(match.Groups.Get("value"), out doubleValue) && int.TryParse(match.Groups.Get("order"), out int order))
            {
                result = doubleValue * Math.Pow(10, order);
                detectedType = TypeCategory.Float;
                return true;
            }
            else if (siNotationRegex.TryMatch(text, out match) && double.TryParse(match.Groups.Get("value"), out doubleValue) && Math.SiPrefixes.Forward.TryGetValue(match.Groups.Get("prefix"), out order))
            {
                result = doubleValue * Math.Pow(10, order);
                detectedType = TypeCategory.Float;
                return true;
            }
            else if (TryParseBinary(text, out ulongValue))
            {
                result = ulongValue;
                detectedType = TypeCategory.Unsigned;
            }

            return false;
        }
        public static IConvertible Parse(string text, out TypeCategory detectedType)
        {
            if(TryParse(text, out IConvertible result, out detectedType))
            {
                return result;
            }
            throw new FormatException($"Could not automatically parse '{text}'");
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
        public static T TryParse<T>(string value, T defaultValue = default)
        {
            try
            {
                return Parse<T>(value);
            }
            catch { }
            return defaultValue;
        }
        public static bool TryParse<T>(string value, out T result, T defaultValue = default)
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
                    data.Swap(i    , i + 3);
                    data.Swap(i + 1, i + 2);
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