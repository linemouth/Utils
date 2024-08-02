using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public struct Hsv : IColor, IEquatable<Hsv>
    {
        public float h;
        public float s;
        public float v;
        public float a;
        public float[] Channels => new float[] { h, s, v, a };
        public ColorChannelInfo[] ChannelInfos => channelInfos;

        private static readonly ColorChannelInfo[] channelInfos = new ColorChannelInfo[]
        {
            new ColorChannelInfo("Hue",        "H", 0, 360, Math.Repeat, new ColorChannelFormat[] { new ColorChannelFormat("", 1), new ColorChannelFormat("deg", 1) }),
            new ColorChannelInfo("Saturation", "S", 0, 1,   Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%",   1) }),
            new ColorChannelInfo("Value",      "V", 0, 1,   Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%",   1) }),
            new ColorChannelInfo("Alpha",      "A", 0, 1,   Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%",   1) }, 1),
        };

        public static Hsv Parse(string text, out string format) => Color.Parse(text, out format).ToHsv();
        public static Hsv Parse(string text) => Color.Parse(text).ToHsv();
        public static bool TryParse(string text, out Hsv hsv, out string format)
        {
            if (Color.TryParse(text, out IColor color, out format))
            {
                hsv = color.ToHsv();
                return true;
            }
            hsv = default;
            return false;
        }
        public Hsv(double hue = 0, double saturation = 0, double value = 0, double alpha = 1) : this((float)hue, (float)saturation, (float)value, (float)alpha) { }
        public Hsv(float hue = 0, float saturation = 0, float value = 0, float alpha = 1)
        {
            h = hue;
            s = saturation;
            v = value;
            a = alpha;
        }
        public T ToModel<T>() where T : IColor => Color.ToModel<T>(this);
        public Argb ToArgb() => ToRgb().ToArgb();
        public Rgb ToRgb()
        {
            Color.HsvToRgb(h, s, v, out float r, out float g, out float b);
            return new Rgb(r, g, b, a);
        }
        public Hsl ToHsl()
        {
            Color.HsvToHsl(this.s, v, out float s, out float l);
            return new Hsl(h, s, l, a);
        }
        public Hsv ToHsv() => this;
        public Cmyk ToCmyk() => ToRgb().ToCmyk();
        public Xyl ToXyl() => ToHsl().ToXyl();
        public override int GetHashCode() => ToArgb().GetHashCode();
        public string ToString(string format = "hsv()") => Color.ToString(this, format);
        public override string ToString() => ToString();
        public override bool Equals(object obj) => obj is IColor color && Equals(color);
        public bool Equals(IColor other) => Approximately(other);
        public bool Equals(Hsv other) => Approximately(other);
        public bool Approximately(IColor other, float margin = 1e-3f) => Color.Approximately(this, other, margin);
        public Hsv LerpUnclamped(IColor b, float t) => (Hsv)Color.LerpUnclamped(this, b, t);
        public Hsv Lerp(IColor b, float t) => (Hsv)Color.Lerp(this, b, t);
    }
}
