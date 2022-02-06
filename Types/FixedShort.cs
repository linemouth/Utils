using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public struct FixedShort
    {
        public byte Low;
        public byte High;
        public byte[] Bytes
        {
            get => new byte[2] { High, Low };
            set
            {
                if(value.Length != 2)
                {
                    throw new ArgumentException("Data must be exactly 2 bytes long.");
                }
                High = value[0];
                Low = value[1];
            }
        }
        public double FloatValue
        {
            get => IntValue * Resolution;
            set => IntValue = (int)Math.Round(value / Resolution);
        }
        public int IntValue
        {
            get => High << 8 | Low;
            set
            {
                Low = (byte)(0xFF & value);
                High = (byte)(0xFF & (value >> 8));
            }
        }
        public readonly double Resolution;

        public FixedShort(double resolution, int value = 0) : this(resolution, 0, 0) { IntValue = value; }
        public FixedShort(double resolution, double value) : this(resolution, 0, 0) { FloatValue = value; }
        public FixedShort(double resolution, byte low, byte high)
        {
            Resolution = resolution;
            Low = low;
            High = high;
        }
    }
}
