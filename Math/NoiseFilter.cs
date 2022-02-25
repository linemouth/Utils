using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils;

namespace Utils
{
    public class NoiseFilter
    {
        public enum BlendMode
        {
            Add, // Y = A + B
            Subtract, // Y = A - B
            Multiply, // Y = A * B
            Divide, // Y = A / B
            Silhouette, // Y = A if B
            Mask, // Y == A if (1 - B)
        }
        public enum SaturateMode
        {
            None,
            Clamp,
            Sigmoid,
        }
        public struct BlendNode
        {
            public BlendMode mode;
            public SaturateMode saturate;
            public int indexA;
            public int indexB;
            public float gainA;
            public float gainB;
            public float gain;
            public float min;
            public float max;


            public BlendNode(BlendMode mode, int a, int b, float gainA = 1, float gainB = 1, float gain = 1, float min = 1, float max = 1)
            {
                this.mode = mode;
                indexA = a;
                indexB = b;
            }
        }
        public enum NoiseType
        {
            Perlin
        }
        public class NoiseSource
        {
            public NoiseType type;
            public float frequency;
            public float offset;
            public float gain;

            public NoiseSource(NoiseType type, float frequency, float offset, float gain)
            {

            }
        }

        public NoiseBlendNode[] blendNodes;
        public NoiseParameter[] parameters;

        public float Evaluate(Float3 point)
        {
            float[] channels = new float[parameters.Length + blendNodes.Length];
            for(int i = 0; i < parameters.Length; i++)
            {
                int channelIndex = blendNodes.Length + i;
                Noise.Evaluate(point);
            }
            for(int i = blendNodes.Length - 1; i >= 0; --i)
            {
                float a = channels[blendNodes[i].inputA];
                float b = channels[blendNodes[i].inputB];
                channels[i] = blendNodes.Blend(a, b);
            }
            return channels[0];
        }
    }
}
