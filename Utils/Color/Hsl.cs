using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public struct Hsl : IColor, IEquatable<Hsl>
    {
        public float h;
        public float s;
        public float l;
        public float a;
        public float[] Channels => new float[] { h, s, l, a };
        public ColorChannelInfo[] ChannelInfos => channelInfos;

        private static readonly ColorChannelInfo[] channelInfos = new ColorChannelInfo[]
        {
            new ColorChannelInfo("Hue",        "H", 0, 360, Math.Repeat, new ColorChannelFormat[] { new ColorChannelFormat("", 1), new ColorChannelFormat("deg", 1) }),
            new ColorChannelInfo("Saturation", "S", 0, 1,   Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%",   1) }),
            new ColorChannelInfo("Lightness",  "L", 0, 1,   Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%",   1) }),
            new ColorChannelInfo("Alpha",      "A", 0, 1,   Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%",   1) }, 1),
        };

        public static explicit operator Argb(Hsl hsl) => hsl.ToArgb();
        public static explicit operator Rgb (Hsl hsl) => hsl.ToRgb();
        public static explicit operator Hsv (Hsl hsl) => hsl.ToHsv();
        public static explicit operator Cmyk(Hsl hsl) => hsl.ToCmyk();
        public static explicit operator Xyl (Hsl hsl) => hsl.ToXyl();
        public static Hsl Parse(string text, out string format) => Color.Parse(text, out format).ToHsl();
        public static Hsl Parse(string text) => Color.Parse(text).ToHsl();
        public static bool TryParse(string text, out Hsl hsl, out string format)
        {
            if (Color.TryParse(text, out IColor color, out format))
            {
                hsl = color.ToHsl();
                return true;
            }
            hsl = default;
            return false;
        }
        public Hsl(float hue, float saturation, float lightness, float alpha = 1)
        {
            h = hue;
            s = saturation;
            l = lightness;
            a = alpha;
        }
        public Argb ToArgb() => ToRgb().ToArgb();
        public Rgb ToRgb()
        {
            Color.HslToRgb(h, s, l, out float r, out float g, out float b);
            return new Rgb(r, g, b, a);
        }
        public Hsl ToHsl() => this;
        public Hsv ToHsv()
        {
            Color.HslToHsv(this.s, l, out float s, out float v);
            return new Hsv(h, s, v, a);
        }
        public Cmyk ToCmyk() => ToRgb().ToCmyk();
        public Xyl ToXyl()
        {
            Color.HslToXyl(h, s, l, out float x, out float y);
            return new Xyl(x, y, l, a);
        }
        public override int GetHashCode() => ToArgb().GetHashCode();
        public string ToString(string format = "hsl()") => Color.ToString(this, format);
        public override string ToString() => ToString();
        public override bool Equals(object obj) => obj is IColor color && Equals(color);
        public bool Equals(IColor other) => Approximately(other);
        public bool Equals(Hsl other) => Approximately(other);
        public bool Approximately(IColor other, float margin = 1e-3f) => Color.Approximately(this, other, margin);
        public Hsl LerpUnclamped(IColor b, float t) => (Hsl)Color.LerpUnclamped(this, b, t);
        public Hsl Lerp(IColor b, float t) => (Hsl)Color.Lerp(this, b, t);
    }
}
