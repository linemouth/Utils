using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public struct Float4
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Float4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        public static Float4 Zero => new Float4(0, 0, 0, 0);
        public static Float4 One => new Float4(1, 1, 1, 1);
        public static Float4 PosX => new Float4(1, 0, 0, 0);
        public static Float4 PosY => new Float4(0, 1, 0, 0);
        public static Float4 PosZ => new Float4(0, 0, 1, 0);
        public static Float4 PosW => new Float4(0, 0, 0, 1);
        public static Float4 NegX => new Float4(-1, 0, 0, 0);
        public static Float4 NegY => new Float4(0, -1, 0, 0);
        public static Float4 NegZ => new Float4(0, 0, -1, 0);
        public static Float4 NegW => new Float4(0, 0, 0, -1);
    }
}
