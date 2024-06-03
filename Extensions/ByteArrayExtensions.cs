﻿using System;
using System.Linq;
using System.Text;
using static Utils.Extensions.ByteArrayExtensions;

namespace Utils
{
    public static class ByteArrayExtensions
    {
        public enum Endian
        {
            System,
            Big,
            Little
        }
        public static bool MustSwap(Endian endian) => endian != Endian.System && endian == Endian.Little ^ BitConverter.IsLittleEndian;
        public static uint CRC32(this byte[] data, uint crc = 0, uint polynomial = Utils.CRC32.defaultPolynomial) => Utils.CRC32.Calculate(data, crc, polynomial);
        public static string FormatWord(this byte[] data, int wordSize = 4, ulong offset = 0, string wordPrefix = "")
        {
            string buffer = $"{wordPrefix}";
            ulong endOffset = Math.Min(offset + (ulong)wordSize, (ulong)data.Length);
            for (; offset < endOffset; ++offset)
            {
                buffer += data[offset].ToString("X2");
            }
            return buffer;
        }
        public static string FormatRow(this byte[] data, int wordSize = 4, int wordsPerRow = 4, ulong offset = 0, string indent = "", string wordPrefix = "", bool commaSeparated = false)
        {
            string buffer = $"{indent}";
            ulong endOffset = Math.Min(offset + (ulong)wordSize * (ulong)wordsPerRow, (ulong)data.Length);
            for (ulong i = 0; offset + i < endOffset; i += (ulong)wordSize)
            {
                if (i > 0)
                {
                    buffer += commaSeparated ? ", " : " ";
                }
                buffer += data.FormatWord(wordSize, offset + i, wordPrefix);
            }
            return buffer;
        }
        public static string FormatData(this byte[] data, int wordSize = 4, int wordsPerRow = 4, ulong offset = 0, string indent = "", string wordPrefix = "", bool commaSeparated = false)
        {
            string buffer = "";
            for (ulong i = 0; offset + i < (ulong)data.Length; i += (ulong)(wordSize * wordsPerRow))
            {
                if (i > 0)
                {
                    buffer += commaSeparated ? ",\n" : "\n";
                }
                buffer += $"{data.FormatRow(wordSize, wordsPerRow, offset + i, indent, wordPrefix, commaSeparated)}";
            }
            return buffer;
        }
        public static string FormatBytes(this byte[] data) => data.Aggregate("", (buffer, b) => buffer += b.ToString("X2"));
        public static byte[] GetBytes(this byte[] data, int startOffset, int count, bool reverse)
        {
            byte[] result = new byte[count];
            if (reverse)
            {
                for (int d = 0, s = startOffset + count - 1; d < count; d++, s--)
                {
                    result[d] = data[s];
                }
            }
            else
            {
                Array.Copy(data, startOffset, result, 0, count);
            }
            return result;
        }
        public static short GetShort(this byte[] data, int startOffset, bool byteSwap)
        {
            byte[] bytes = data.GetBytes(startOffset, 2, byteSwap);
            return BitConverter.ToInt16(bytes, 0);
        }
        public static ushort GetUshort(this byte[] data, int startOffset, bool byteSwap)
        {
            byte[] bytes = data.GetBytes(startOffset, 2, byteSwap);
            return BitConverter.ToUInt16(bytes, 0);
        }
        public static int GetInt(this byte[] data, int startOffset, bool byteSwap)
        {
            byte[] bytes = data.GetBytes(startOffset, 4, byteSwap);
            return BitConverter.ToInt32(bytes, 0);
        }
        public static uint GetUint(this byte[] data, int startOffset, bool byteSwap)
        {
            byte[] bytes = data.GetBytes(startOffset, 4, byteSwap);
            return BitConverter.ToUInt32(bytes, 0);
        }
        public static long GetLong(this byte[] data, int startOffset, bool byteSwap)
        {
            byte[] bytes = data.GetBytes(startOffset, 8, byteSwap);
            return BitConverter.ToInt64(bytes, 0);
        }
        public static ulong GetUlong(this byte[] data, int startOffset, bool byteSwap)
        {
            byte[] bytes = data.GetBytes(startOffset, 8, byteSwap);
            return BitConverter.ToUInt64(bytes, 0);
        }
        public static float GetFloat(this byte[] data, int startOffset, bool byteSwap)
        {
            byte[] bytes = data.GetBytes(startOffset, 4, byteSwap);
            return BitConverter.ToSingle(bytes, 0);
        }
        public static double GetDouble(this byte[] data, int startOffset, bool byteSwap)
        {
            byte[] bytes = data.GetBytes(startOffset, 8, byteSwap);
            return BitConverter.ToDouble(bytes, 0);
        }
        public static string GetString(this byte[] data, int startOffset, Encoding encoding, int count = int.MaxValue) => encoding.GetString(data, startOffset, count);
        public static string GetString(this byte[] data, int startOffset = 0, int count = int.MaxValue) => data.GetString(startOffset, Encoding.Default, count);
        public static string GetString(this byte[] data, Encoding encoding, int count = int.MaxValue) => data.GetString(0, encoding, count);
        public static byte[] ToBytes(this short value, bool byteSwap)
        {
            byte[] result = BitConverter.GetBytes(value);
            if (byteSwap)
            {
                result.Reverse();
            }
            return result;
        }
        public static byte[] ToBytes(this ushort value, bool byteSwap)
        {
            byte[] result = BitConverter.GetBytes(value);
            if (byteSwap)
            {
                result.Reverse();
            }
            return result;
        }
        public static byte[] ToBytes(this int value, bool byteSwap)
        {
            byte[] result = BitConverter.GetBytes(value);
            if (byteSwap)
            {
                result.Reverse();
            }
            return result;
        }
        public static byte[] ToBytes(this uint value, bool byteSwap)
        {
            byte[] result = BitConverter.GetBytes(value);
            if (byteSwap)
            {
                result.Reverse();
            }
            return result;
        }
        public static byte[] ToBytes(this long value, bool byteSwap)
        {
            byte[] result = BitConverter.GetBytes(value);
            if (byteSwap)
            {
                result.Reverse();
            }
            return result;
        }
        public static byte[] ToBytes(this ulong value, bool byteSwap)
        {
            byte[] result = BitConverter.GetBytes(value);
            if (byteSwap)
            {
                result.Reverse();
            }
            return result;
        }
        public static byte[] ToBytes(this float value, bool byteSwap)
        {
            byte[] result = BitConverter.GetBytes(value);
            if (byteSwap)
            {
                result.Reverse();
            }
            return result;
        }
        public static byte[] ToBytes(this double value, bool byteSwap)
        {
            byte[] result = BitConverter.GetBytes(value);
            if (byteSwap)
            {
                result.Reverse();
            }
            return result;
        }
        public static void Reverse<T>(this T[] array)
        {
            for (long a = 0, b = array.Length - 1; a < b; a++, b--)
            {
                (array[a], array[b]) = (array[b], array[a]);
            }
        }
    }
}