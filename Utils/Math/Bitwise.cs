namespace Utils
{
    public static class Bitwise
    {
        /// <summary>Reverses the order of bits in a value.</summary>
        public static sbyte Reflect(this sbyte value) => unchecked((sbyte)Reflect((byte)value));
        /// <summary>Reverses the order of bits in a value.</summary>
        public static byte Reflect(this byte value)
        {
            value = (byte)((value >> 1) & 0x55 | (value << 1) & 0xAA); // Swap adjacent bits
            value = (byte)((value >> 2) & 0x33 | (value << 2) & 0xCC); // Swap adjacent pairs
            return (byte)((value >> 4) & 0x0F | (value << 4) & 0xF0); // Swap high and low nybbles
        }
        /// <summary>Reverses the order of bits in a value.</summary>
        public static short Reflect(this short value) => unchecked((short)Reflect((ushort)value));
        /// <summary>Reverses the order of bits in a value.</summary>
        public static ushort Reflect(this ushort value)
        {
            value = (ushort)((value >> 1) & 0x5555 | (value << 1) & 0xAAAA); // Swap adjacent bits
            value = (ushort)((value >> 2) & 0x3333 | (value << 2) & 0xCCCC); // Swap adjacent pairs
            value = (ushort)((value >> 4) & 0x0F0F | (value << 4) & 0xF0F0); // Swap adjacent nybbles
            return (ushort)((value >> 8) & 0x00FF | (value << 8) & 0xFF00); // Swap high and low bytes
        }
        /// <summary>Reverses the order of bits in a value.</summary>
        public static int Reflect(this int value) => unchecked((int)Reflect((uint)value));
        /// <summary>Reverses the order of bits in a value.</summary>
        public static uint Reflect(this uint value)
        {
            value = (value >> 1) & 0x55555555 | (value << 1) & 0xAAAAAAAA; // Swap adjacent bits
            value = (value >> 2) & 0x33333333 | (value << 2) & 0xCCCCCCCC; // Swap adjacent pairs
            value = (value >> 4) & 0x0F0F0F0F | (value << 4) & 0xF0F0F0F0; // Swap adjacent nybbles
            value = (value >> 8) & 0x00FF00FF | (value << 8) & 0xFF00FF00; // Swap adjacent bytes
            return (value >> 16) & 0x0000FFFF | (value << 16) & 0xFFFF0000; // Swap high and low shorts
        }
        /// <summary>Reverses the order of bits in a value.</summary>
        public static long Reflect(this long value) => unchecked((long)Reflect((ulong)value));
        /// <summary>Reverses the order of bits in a value.</summary>
        public static ulong Reflect(this ulong value)
        {
            value = (value >> 1) & 0x5555555555555555 | (value << 1) & 0xAAAAAAAAAAAAAAAA; // Swap adjacent bits
            value = (value >> 2) & 0x3333333333333333 | (value << 2) & 0xCCCCCCCCCCCCCCCC; // Swap adjacent pairs
            value = (value >> 4) & 0x0F0F0F0F0F0F0F0F | (value << 4) & 0xF0F0F0F0F0F0F0F0; // Swap adjacent nybbles
            value = (value >> 8) & 0x00FF00FF00FF00FF | (value << 8) & 0xFF00FF00FF00FF00; // Swap adjacent bytes
            value = (value >> 16) & 0x0000FFFF0000FFFF | (value << 16) & 0xFFFF0000FFFF0000; // Swap adjacent shorts
            return (value >> 32) & 0x00000000FFFFFFFF | (value << 32) & 0xFFFFFFFF00000000; // Swap high and low words
        }
        /// <summary>Reverses the order of bytes in a value.</summary>
        public static short Swap(this short value) => (short)((value >> 8) & 0x00FF | (value << 8) & 0xFF00); // Swap high and low bytes
        /// <summary>Reverses the order of bytes in a value.</summary>
        public static ushort Swap(this ushort value) => (ushort)((value >> 8) & 0x00FF | (value << 8) & 0xFF00); // Swap high and low bytes
        /// <summary>Reverses the order of bytes in a value.</summary>
        public static int Swap(this int value)
        {
            unchecked
            {
                value = (value >> 8) & 0x00FF00FF | (value << 8) & (int)0xFF00FF00; // Swap adjacent bytes
                return (value >> 16) & 0x0000FFFF | (value << 16) & (int)0xFFFF0000; // Swap high and low shorts
            }
        }
        /// <summary>Reverses the order of bytes in a value.</summary>
        public static uint Swap(this uint value)
        {
            value = (value >> 8) & 0x00FF00FF | (value << 8) & 0xFF00FF00; // Swap adjacent bytes
            return (value >> 16) & 0x0000FFFF | (value << 16) & 0xFFFF0000; // Swap high and low shorts
        }
        /// <summary>Reverses the order of bytes in a value.</summary>
        public static long Swap(this long value)
        {
            unchecked
            {
                value = (value >> 8) & 0x00FF00FF00FF00FF | (value << 8) & (long)0xFF00FF00FF00FF00; // Swap adjacent bytes
                value = (value >> 16) & 0x0000FFFF0000FFFF | (value << 16) & (long)0xFFFF0000FFFF0000; // Swap adjacent shorts
                return (value >> 32) & 0x00000000FFFFFFFF | (value << 32) & (long)0xFFFFFFFF00000000; // Swap high and low words
            }
        }
        /// <summary>Reverses the order of bytes in a value.</summary>
        public static ulong Swap(this ulong value)
        {
            value = (value >> 8) & 0x00FF00FF00FF00FF | (value << 8) & 0xFF00FF00FF00FF00; // Swap adjacent bytes
            value = (value >> 16) & 0x0000FFFF0000FFFF | (value << 16) & 0xFFFF0000FFFF0000; // Swap adjacent shorts
            return (value >> 32) & 0x00000000FFFFFFFF | (value << 32) & 0xFFFFFFFF00000000; // Swap high and low words
        }
    }
}
