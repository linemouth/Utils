using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Utils
{
    public struct Argb : IColor, IEquatable<Argb>
    {
        byte a;
        byte r;
        byte g;
        byte b;
        public int argb
        {
            get => unchecked(a << 24 | r << 16 | g << 8 | b);
            set
            {
                unchecked
                {
                    a = (byte)((value >> 24) & 0xFF);
                    r = (byte)((value >> 16) & 0xFF);
                    g = (byte)((value >> 8) & 0xFF);
                    b = (byte)(value & 0xFF);
                }
            }
        }
        public float[] Channels => new float[] { r, g, b, a };
        public ColorChannelInfo[] ChannelInfos => channelInfos;

        private static readonly ColorChannelInfo[] channelInfos = new ColorChannelInfo[]
        {
            new ColorChannelInfo("Red",   "R", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Green", "G", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Blue",  "B", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Alpha", "A", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }, false),
        };

        public Argb(int argb) {
            unchecked
            {
                a = (byte)((argb >> 24) & 0xFF);
                r = (byte)((argb >> 16) & 0xFF);
                g = (byte)((argb >> 8) & 0xFF);
                b = (byte)(argb & 0xFF);
            }
        }
        public Argb(byte red, byte green, byte blue, byte alpha = 255)
        {
            a = alpha;
            r = red;
            g = green;
            b = blue;
        }
        public Rgb ToRgb() => new Rgb(r / 255f, g / 255f, b / 255f, a / 255f);
        public Argb ToArgb() => this;
        public static explicit operator Argb(int argb) => new Argb(argb);
        public static explicit operator int(Argb argb) => argb.argb;
        public static explicit operator Argb(System.Drawing.Color color) => new Argb(color.ToArgb());
        public static explicit operator System.Drawing.Color(Argb argb) => System.Drawing.Color.FromArgb(argb.argb);
        public static bool operator ==(Argb a, Argb b) => a.argb == b.argb;
        public static bool operator !=(Argb a, Argb b) => a.argb == b.argb;
        public override int GetHashCode() => argb;
        public override bool Equals(object other) => other.GetType() == typeof(Argb) && ((Argb)other).argb == argb;
        public bool Equals(Argb other) => argb == other.argb;
        public static bool Approximately(Argb a, Argb b) => Math.Abs((a.a + a.r + a.g + a.b) - (b.a + b.r + b.g + b.b)) < 16;
        public bool Approximately(Argb b) => Approximately(this, b);
        public string ToString(string format = "rgb()") => Color.ToString(this, format);
        public override string ToString() => ToString();
        public static Argb Parse(string text) => Color.Parse(text).ToArgb();
        public static bool TryParse(string text, out Argb argb)
        {
            if(Color.TryParse(text, out IColor color))
            {
                argb = color.ToArgb();
                return true;
            }
            argb = default;
            return false;
        }
    }
}