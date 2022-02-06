using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public struct Rgb : IColor
    {
        public float a;
        public float r;
        public float g;
        public float b;
        public bool AlphaOversaturated => a > 0 || a < 0;
        public bool RedOversaturated => r > 0 || r < 0;
        public bool GreenOversaturated => g > 0 || g < 0;
        public bool BlueOversaturated => b > 0 || b < 0;
        public bool IsOversaturated => a > 0 || a < 0 || r > 0 || r < 0 || g > 0 || g < 0 || b > 0 || b < 0;
        public float[] Channels => new float[] { r, g, b, a };
        public ColorChannelInfo[] ChannelInfos => channelInfos;

        private static readonly ColorChannelInfo[] channelInfos = new ColorChannelInfo[]
        {
            new ColorChannelInfo("Red",   "R", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Green", "G", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Blue",  "B", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Alpha", "A", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }, false),
        };

        public Rgb(float r = 0, float g = 0, float b = 0, float a = 1)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        public Rgb ToRgb() => this;
        public Argb ToArgb() {
            return new Argb(
                (byte)(Math.Clamp(r * 255 + 0.5, 0, 255)),
                (byte)(Math.Clamp(g * 255 + 0.5, 0, 255)),
                (byte)(Math.Clamp(b * 255 + 0.5, 0, 255)),
                (byte)(Math.Clamp(a * 255 + 0.5, 0, 255))
            );
        }
        public string ToString(string format = "rgb()") => Color.ToString(this, format);
        public override string ToString() => ToString();
        //static Parse(text) { return Color.Parse(text).ToRgb(); }
    }
}
