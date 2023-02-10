using System;
using System.Linq;

namespace Utils
{
    public static class ByteArray
    {
        public enum Endian
        {
            System,
            Big,
            Little
        }
        public static uint CRC32(this byte[] data, uint crc = 0, uint polynomial = Utils.CRC32.defaultPolynomial) => Utils.CRC32.Calculate(data, crc, polynomial);
        public static string FormatWord(this byte[] data, int wordSize = 4, ulong offset = 0, string wordPrefix = "")
        {
            string buffer = $"{wordPrefix}";
            ulong endOffset = Math.Min(offset + (ulong)wordSize, (ulong)data.Length);
            for(; offset < endOffset; ++offset)
            {
                buffer += data[offset].ToString("X2");
            }
            return buffer;
        }
        public static string FormatRow(this byte[] data, int wordSize = 4, int wordsPerRow = 4, ulong offset = 0, string indent = "", string wordPrefix = "", bool commaSeparated = false)
        {
            string buffer = $"{indent}";
            ulong endOffset = Math.Min(offset + (ulong)wordSize * (ulong)wordsPerRow, (ulong)data.Length);
            for(ulong i = 0; offset + i < endOffset; i += (ulong)wordSize)
            {
                if(i > 0)
                {
                    buffer += commaSeparated ? ", " : " ";
                }
                buffer += FormatWord(data, wordSize, offset + i, wordPrefix);
            }
            return buffer;
        }
        public static string FormatData(this byte[] data, int wordSize = 4, int wordsPerRow = 4, ulong offset = 0, string indent = "", string wordPrefix = "", bool commaSeparated = false)
        {
            string buffer = "";
            for(ulong i = 0; offset + i < (ulong)data.Length; i += (ulong)(wordSize * wordsPerRow))
            {
                if(i > 0)
                {
                    buffer += commaSeparated ? ",\n" : "\n";
                }
                buffer += $"{FormatRow(data, wordSize, wordsPerRow, offset + i, indent, wordPrefix, commaSeparated)}";
            }
            return buffer;
        }
        public static string FormatBytes(this byte[] data) => data.Aggregate("", (buffer, b) => buffer += b.ToString("X2"));
        public static short ToShort(this byte[] data, Endian endian = Endian.System)
        {
            short result = BitConverter.ToInt16(data, 0);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.Swap();
            }
            return result;
        }
        public static ushort ToUshort(this byte[] data, Endian endian = Endian.System)
        {
            ushort result = BitConverter.ToUInt16(data, 0);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.Swap();
            }
            return result;
        }
        public static int ToInt(this byte[] data, Endian endian = Endian.System)
        {
            int result = BitConverter.ToInt32(data, 0);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.Swap();
            }
            return result;
        }
        public static uint ToUint(this byte[] data, Endian endian = Endian.System)
        {
            uint result = BitConverter.ToUInt32(data, 0);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.Swap();
            }
            return result;
        }
        public static long ToLong(this byte[] data, Endian endian = Endian.System)
        {
            long result = BitConverter.ToInt64(data, 0);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.Swap();
            }
            return result;
        }
        public static ulong ToUlong(this byte[] data, Endian endian = Endian.System)
        {
            ulong result = BitConverter.ToUInt64(data, 0);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.Swap();
            }
            return result;
        }
        public static byte[] ToBytes(this short value, Endian endian = Endian.System)
        {
            byte[] result = BitConverter.GetBytes(value);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.ReverseArray();
            }
            return result;
        }
        public static byte[] ToBytes(this ushort value, Endian endian = Endian.System)
        {
            byte[] result = BitConverter.GetBytes(value);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.ReverseArray();
            }
            return result;
        }
        public static byte[] ToBytes(this int value, Endian endian = Endian.System)
        {
            byte[] result = BitConverter.GetBytes(value);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.ReverseArray();
            }
            return result;
        }
        public static byte[] ToBytes(this uint value, Endian endian = Endian.System)
        {
            byte[] result = BitConverter.GetBytes(value);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.ReverseArray();
            }
            return result;
        }
        public static byte[] ToBytes(this long value, Endian endian = Endian.System)
        {
            byte[] result = BitConverter.GetBytes(value);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.ReverseArray();
            }
            return result;
        }
        public static byte[] ToBytes(this ulong value, Endian endian = Endian.System)
        {
            byte[] result = BitConverter.GetBytes(value);
            if(endian != Endian.System && ((endian == Endian.Little) ^ BitConverter.IsLittleEndian))
            {
                result.ReverseArray();
            }
            return result;
        }
    }
}
