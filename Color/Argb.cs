using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Utils
{
    public struct Argb : IColor, IEquatable<Argb>
    {
        public byte b;
        public byte g;
        public byte r;
        public byte a;
        public float[] Channels => new float[] { r, g, b, a };
        public ColorChannelInfo[] ChannelInfos => channelInfos;

        private static readonly ColorChannelInfo[] channelInfos = new ColorChannelInfo[]
        {
            new ColorChannelInfo("Red",   "R", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Green", "G", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Blue",  "B", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Alpha", "A", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }, false),
        };
        public static explicit operator Argb(int argb) => unchecked(new Argb((byte)((argb >> 16) & 0xFF), (byte)((argb >> 8) & 0xFF), (byte)(argb & 0xFF), (byte)((argb >> 24) & 0xFF)));
        public static explicit operator int(Argb argb) => unchecked(argb.a << 24 | argb.r << 16 | argb.g << 8 | argb.b);
        public static explicit operator Rgb(Argb argb) => argb.ToRgb();
        public static explicit operator Hsl(Argb argb) => argb.ToHsl();
        public static explicit operator Hsv(Argb argb) => argb.ToHsv();
        public static explicit operator Cmyk(Argb argb) => argb.ToCmyk();
        public static explicit operator Xyl(Argb argb) => argb.ToXyl();
        public static explicit operator Argb(System.Drawing.Color color) => new Argb(color.ToArgb());
        public static explicit operator System.Drawing.Color(Argb argb) => System.Drawing.Color.FromArgb((int)argb);
        public static bool operator ==(Argb a, Argb b) => (int)a == (int)b;
        public static bool operator !=(Argb a, Argb b) => (int)a == (int)b;
        public static bool Approximately(Argb a, Argb b) => Math.Abs((a.a + a.r + a.g + a.b) - (b.a + b.r + b.g + b.b)) < 16;
        public static Argb Parse(string text, out string format) => Color.Parse(text, out format).ToArgb();
        public static Argb Parse(string text) => Color.Parse(text).ToArgb();
        public static bool TryParse(string text, out Argb argb, out string format)
        {
            if (Color.TryParse(text, out IColor color, out format))
            {
                argb = color.ToArgb();
                return true;
            }
            argb = default;
            return false;
        }
        public static bool TryParse(string text, out Argb argb) => TryParse(text, out argb, out _);
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
        public string ToString(string format = "#hex8") => Color.ToString(this, format);
        public Argb ToArgb() => this;
        public Rgb ToRgb() => new Rgb(r / 255f, g / 255f, b / 255f, a / 255f);
        public Hsl ToHsl()
        {
            Color.RgbToHsl(r / 255f, g / 255f, b / 255f, out double h, out double s, out double l);
            return new Hsl(h, s, l, a / 255f);
        }
        public Hsv ToHsv()
        {
            Color.RgbToHsv(r / 255f, g / 255f, b / 255f, out double h, out double s, out double v);
            return new Hsv(h, s, v, a / 255f);
        }
        public Cmyk ToCmyk()
        {
            Color.RgbToCmyk(r / 255f, g / 255f, b / 255f, out double c, out double m, out double y, out double k);
            return new Cmyk(c, m, y, k, a / 255f);
        }
        public Xyl ToXyl() => ToHsl().ToXyl();
        public override int GetHashCode() => (int)this;
        public override bool Equals(object other) => other.GetType() == typeof(Argb) && (int)this == (int)((Argb)other);
        public bool Equals(Argb other) => (int)this == (int)other;
        public bool Approximately(Argb b) => Approximately(this, b);
    }
}