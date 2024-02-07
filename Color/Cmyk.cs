using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public struct Cmyk : IColor, IEquatable<Cmyk>
    {
        public float c;
        public float m;
        public float y;
        public float k;
        public float a;
        public float[] Channels => new float[] { c, m, y, k, a };
        public ColorChannelInfo[] ChannelInfos => channelInfos;

        private static readonly ColorChannelInfo[] channelInfos = new ColorChannelInfo[]
        {
            new ColorChannelInfo("Cyan",    "C", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Magenta", "M", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Yellow",  "Y", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Black",   "K", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Alpha",   "A", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }, 1),
        };

        public static explicit operator Argb(Cmyk cmyk) => cmyk.ToArgb();
        public static explicit operator Rgb (Cmyk cmyk) => cmyk.ToRgb();
        public static explicit operator Hsl (Cmyk cmyk) => cmyk.ToHsl();
        public static explicit operator Hsv (Cmyk cmyk) => cmyk.ToHsv();
        public static explicit operator Xyl (Cmyk cmyk) => cmyk.ToXyl();
        public static Cmyk Parse(string text, out string format) => Color.Parse(text, out format).ToCmyk();
        public static Cmyk Parse(string text) => Color.Parse(text).ToCmyk();
        public static bool TryParse(string text, out Cmyk cmyk, out string format)
        {
            if (Color.TryParse(text, out IColor color, out format))
            {
                cmyk = color.ToCmyk();
                return true;
            }
            cmyk = default;
            return false;
        }
        public static bool TryParse(string text, out Cmyk cmyk) => TryParse(text, out cmyk, out _);
        public Cmyk(float cyan, float magenta, float yellow, float black, float alpha = 1)
        {
            c = cyan;
            m = magenta;
            y = yellow;
            k = black;
            a = alpha;
        }
        public Argb ToArgb() => ToRgb().ToArgb();
        public Rgb ToRgb()
        {
            Color.CmykToRgb(c, m, y, k, out float r, out float g, out float b);
            return new Rgb(r, g, b, a);
        }
        public Hsl ToHsl() => ToRgb().ToHsl();
        public Hsv ToHsv() => ToRgb().ToHsv();
        public Cmyk ToCmyk() => this;
        public Xyl ToXyl() => ToHsl().ToXyl();
        public override int GetHashCode() => ToArgb().GetHashCode();
        public string ToString(string format = "cmyk()") => Color.ToString(this, format);
        public override string ToString() => ToString();
        public override bool Equals(object obj) => obj is IColor color && Equals(color);
        public bool Equals(IColor other) => Approximately(other);
        public bool Equals(Cmyk other) => Approximately(other);
        public bool Approximately(IColor other, float margin = 1e-3f) => Color.Approximately(this, other, margin);
        public Cmyk LerpUnclamped(IColor b, float t) => (Cmyk)Color.LerpUnclamped(this, b, t);
        public Cmyk Lerp(IColor b, float t) => (Cmyk)Color.Lerp(this, b, t);
    }
}
