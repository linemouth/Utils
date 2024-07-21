using System;
using System.Runtime.InteropServices;

namespace Utils
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Argb : IColor, IEquatable<Argb>
    {
        public byte b;
        public byte g;
        public byte r;
        public byte a;
        public float[] Channels => new float[] { r, g, b, a };
        public ColorChannelInfo[] ChannelInfos => channelInfos;
        public static Argb transparent  => new Argb(0, 0, 0, 0);
        public static Argb black        => new Argb(  0,   0,   0);
        public static Argb red          => new Argb(255,   0,   0);
        public static Argb yellow       => new Argb(255, 255,   0);
        public static Argb green        => new Argb(  0, 255,   0);
        public static Argb cyan         => new Argb(  0, 255, 255);
        public static Argb blue         => new Argb(  0,   0, 255);
        public static Argb magenta      => new Argb(255,   0, 255);
        public static Argb white        => new Argb(255, 255, 255);
        public static Argb gray         => new Argb(127, 127, 127);

        private static readonly ColorChannelInfo[] channelInfos = new ColorChannelInfo[]
        {
            new ColorChannelInfo("Red",   "R", 0, 255, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 0), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Green", "G", 0, 255, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 0), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Blue",  "B", 0, 255, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 0), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Alpha", "A", 0, 255, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 0), new ColorChannelFormat("%", 1) }, 255),
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
        public Argb(int argb)
        {
            unchecked
            {
                a = (byte)((argb >> 24) & 0xFF);
                r = (byte)((argb >> 16) & 0xFF);
                g = (byte)((argb >> 8) & 0xFF);
                b = (byte)(argb & 0xFF);
            }
        }
        public Argb(uint argb) : this((int)argb) { }
        public Argb(byte red, byte green, byte blue, byte alpha = 255)
        {
            a = alpha;
            r = red;
            g = green;
            b = blue;
        }
        public Argb(int red, int green, int blue, int alpha = 255) : this((byte)red, (byte)green, (byte)blue, (byte)alpha) { }
        public Argb ToArgb() => this;
        public Rgb ToRgb() => new Rgb(r / 255f, g / 255f, b / 255f, a / 255f);
        public Hsl ToHsl()
        {
            Color.RgbToHsl(r / 255f, g / 255f, b / 255f, out float h, out float s, out float l);
            return new Hsl(h, s, l, a / 255f);
        }
        public Hsv ToHsv()
        {
            Color.RgbToHsv(r / 255f, g / 255f, b / 255f, out float h, out float s, out float v);
            return new Hsv(h, s, v, a / 255f);
        }
        public Cmyk ToCmyk()
        {
            Color.RgbToCmyk(r / 255f, g / 255f, b / 255f, out float c, out float m, out float y, out float k);
            return new Cmyk(c, m, y, k, a / 255f);
        }
        public Xyl ToXyl() => ToHsl().ToXyl();
        public override int GetHashCode() => (int)this;
        public string ToString(string format = "#rgba8") => Color.ToString(this, format);
        public override string ToString() => ToString();
        public override bool Equals(object obj) => obj is IColor color && Equals(color);
        public bool Equals(IColor other) => Equals(other.ToArgb());
        public bool Equals(Argb other) => (int)this == (int)other;
        public bool Approximately(IColor other, float margin = 1e-3f) => Color.Approximately(this, other, margin);
        public void Premultiply()
        {
            if(a > 0)
            {
                float alpha = a / 255f;
                r = (byte)(r * alpha);
                g = (byte)(g * alpha);
                b = (byte)(b * alpha);
            }
            else
            {
                r = g = b = 0;
            }
        }
        public void Unpremultiply()
        {
            if(a > 0)
            {
                float alpha = a / 255f;
                r = (byte)(r / alpha);
                g = (byte)(g / alpha);
                b = (byte)(b / alpha);
            }
            else
            {
                r = g = b = 0;
            }
        }
        public Argb LerpUnclamped(IColor b, float t) => (Argb)Color.LerpUnclamped(this, b, t);
        public Argb Lerp(IColor b, float t) => (Argb)Color.Lerp(this, b, t);
    }
}
