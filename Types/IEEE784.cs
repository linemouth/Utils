using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public struct IEEE784
    {
        public int Sign => Negative ? -1 : 1;
        public bool Negative
        {
            get => (intValue & 0x1000000000000000) != 0;
            set => intValue = (intValue & 0x7FFFFFFFFFFFFFFF) | (value ? 0x8000000000000000 : 0);
        }
        public int Exponent
        {
            get => (int)((intValue & 0x7FF0000000000000) >> 52);
            set
            {
                if (value > 1024 || value < -1023)
                {
                    throw new ArgumentOutOfRangeException($"IEEE754 Exponent must be in the range [-126, 127].");
                }
                intValue = (intValue & 0x800FFFFFFFFFFFFF) | ((ulong)(value & 0x7FF) << 52);
            }
        }
        public long Mantissa
        {
            get => (long)(intValue & 0x000FFFFFFFFFFFFF);
            set => intValue = (intValue & 0xFFF0000000000000) | (ulong)(value & 0x000FFFFFFFFFFFFF);
        }

        private ulong intValue;

        public static unsafe implicit operator double(IEEE784 value) => *(double*)&value.intValue;
        public static implicit operator IEEE784(double value) => new IEEE784(value);
        public unsafe IEEE784(double value) => intValue = *(ulong*)&value;
        public override string ToString() => $"IEEE784[{(Negative ? '-' : '+')} 2^{Exponent} * 2^-{Mantissa}]";
    }
}
