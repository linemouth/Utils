using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public struct Float3
    {
        public float X;
        public float Y;
        public float Z;

        public Float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        public static Float3 Zero => new Float3(0, 0, 0);
        public static Float3 One => new Float3(1, 1, 1);
        public static Float3 PosX => new Float3(1, 0, 0);
        public static Float3 PosY => new Float3(0, 1, 0);
        public static Float3 PosZ => new Float3(0, 0, 1);
        public static Float3 NegX => new Float3(-1, 0, 0);
        public static Float3 NegY => new Float3(0, -1, 0);
        public static Float3 NegZ => new Float3(0, 0, -1);
        public static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z + b.Z;
        public float SqrMagnitude => X * X + Y * Y + Z * Z;
        public float AbsSum => Math.Abs(X) + Math.Abs(Y) + Math.Abs(Z);
    }
}