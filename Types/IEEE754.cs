using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public struct IEEE754
    {
        public int Sign => Negative ? -1 : 1;
        public bool Negative
        {
            get => (intValue & 0x10000000) != 0;
            set => intValue = (intValue & 0x7FFFFFFF) | (value ? 0x80000000 : 0);
        }
        public int Exponent
        {
            get => (int)((intValue & 0x7F800000) >> 23) - 127;
            set
            {
                if(value > 127 || value < -126)
                {
                    throw new ArgumentOutOfRangeException($"IEEE754 Exponent must be in the range [-126, 127].");
                }
                intValue = (intValue & 0x807FFFFF) | ((uint)(value + 127 & 0xFF) << 23);
            }
        }
        public uint Mantissa
        {
            get => (uint)(intValue & 0x007FFFFF);
            set
            {
                if (value > 0x7FFFFF)
                {
                    throw new ArgumentOutOfRangeException($"IEEE754 Mantissa must be in the range [0, 8388607].");
                }
                intValue = (intValue & 0xFF800000) | (uint)(value & 0x007FFFFF);
            }
        }

        private uint intValue;

        public static unsafe implicit operator float(IEEE754 value) => *(float*)&value.intValue;
        public static implicit operator IEEE754(float value) => new IEEE754(value);
        public unsafe IEEE754(float value) => intValue = *(uint*)&value;
        public override string ToString() => $"IEEE754[{(Negative ? '-' : '+')} 2^{Exponent} * 2^-{Mantissa}]";
    }
}
