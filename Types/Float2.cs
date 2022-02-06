using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public struct Float2
    {
        public float X;
        public float Y;

        public Float2(float x, float y)
        {
            X = x;
            Y = y;
        }
        public static Float2 Zero => new Float2(0, 0);
        public static Float2 One => new Float2(1, 1);
        public static Float2 PosX => new Float2(1, 0);
        public static Float2 PosY => new Float2(0, 1);
        public static Float2 NegX => new Float2(-1, 0);
        public static Float2 NegY => new Float2(0, -1);
        public static float Dot(Float2 a, Float2 b) => a.X * b.X + a.Y * b.Y;
        public float SqrMagnitude => X * X + Y * Y;
        public float AbsSum => Math.Abs(X) + Math.Abs(Y);
    }
}
