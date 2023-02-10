using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public struct Blender
    {
        public enum Mode
        {
            Add,
            Subtract,
            Multiply,
            Mask,
            Silhouette,
            Min,
            Max,
            SoftMin,
            SoftMax
        }

        public Mode mode;


        public float Blend(float a, float b, float min, float max)
        {
            switch(mode)
            {
                case Mode.Add: return a + b;
                case Mode.Subtract: return a - b;
                case Mode.Multiply: return a * b;
                case Mode.Mask: return a * b.Clamp(0, 1);
                case Mode.Silhouette: return a * (1 - b).Clamp(0, 1);
                case Mode.Min: return Math.Min(a, b);
                case Mode.Max: return Math.Max(a, b);
                case Mode.SoftMin: return Math.SoftMin(a, b);
                case Mode.SoftMax: return Math.SoftMax(a, b);
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported BlendMode: '{mode}'");
            }
        }
    }
}
