using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Utils
{
    public class NoiseFilter
    {
        public enum FilterMode
        {
            None,
            Clamp,
            Sigmoid,
            SmoothClamp,
            SoftClamp
        }
        public enum BlendMode
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Silhouette,
            Mask
        }
        public enum NoiseType
        {
            Wave,
            Perlin
        }
        public struct FilterParams
        {
            public FilterMode mode;
            public float a;
            public float b;
            public float c;
            public float d;

            public FilterParams(FilterMode mode, float a, float b, float c, float d)
            {
                this.mode = mode;
                this.a = a;
                this.b = b;
                this.c = c;
                this.d = d;
            }
        }
        public struct BlendParams
        {
            public BlendMode blendMode;
            public int indexA;
            public int indexB;
            public FilterParams filterA;
            public FilterParams filterB;

            public BlendParams(BlendMode blendMode, int indexA, int indexB, FilterParams filterA, FilterParams filterB)
            {
                this.blendMode = blendMode;
                this.indexA = indexA;
                this.indexB = indexB;
                this.filterA = filterA;
                this.filterB = filterB;
            }
        }
        public struct NoiseParams
        {
            public NoiseType type;
            public float frequency;
            public float offset;
            public float gain;

            public NoiseParams(NoiseType type, float frequency, float offset, float gain)
            {
                this.type = type;
                this.frequency = frequency;
                this.offset = offset;
                this.gain = gain;
            }
        }

        public BlendParams[] blendNodes;
        public NoiseParams[] sources;
        public FilterParams filterParams;

        public float Evaluate(Float3 point)
        {
            float[] channels = new float[blendNodes.Length + sources.Length];
            for(int i = 0; i < sources.Length; i++)
            {
                int channelIndex = blendNodes.Length + i;
                Noise.Evaluate(point);
            }
            for(int i = blendNodes.Length - 1; i >= 0; --i)
            {
                int indexA = blendNodes[i].indexA;
                int indexB = blendNodes[i].indexB;
                float a = Filter(channels[indexA]);
                float b = Filter(channels[indexB]);
                float y = 0;
                switch(blendNodes[i].blendMode)
                {
                    case BlendMode.Add: // Y = A + B
                    default:
                        y = a + b;
                        break;
                    case BlendMode.Subtract: // Y = A - B
                        y = a - b;
                        break;
                    case BlendMode.Multiply: // Y = A * B
                        y = a * b;
                        break;
                    case BlendMode.Divide: // Y = A / B
                        y = Math.SafeDivide(a, b);
                        break;
                    case BlendMode.Silhouette: // Y = A if B
                        y = a * Math.SoftClamp(b);
                        break;
                    case BlendMode.Mask: // Y == A if (1 - B)
                        y = a * Math.SoftClamp(1 - b);
                        break;
                }
                channels[i] = y;
            }
            return channels[0];
        }

        private float Filter(float v)
        {
            throw new NotImplementedException();
        }

        public static float Filter(float value, FilterMode filterMode, float a, float b, float c, float d)
        {
            switch(filterMode)
            {
                case FilterMode.Clamp:
                    return Math.Clamp(value, a, b);
                case FilterMode.Sigmoid:
                    return Math.Sigmoid(value);
                case FilterMode.SmoothClamp:
                    return Math.SmoothClamp(value, a, b);
                case FilterMode.SoftClamp:
                    return Math.SoftClamp(value, a, b);
                default:
                    return value;
            }
        }
    }
}
