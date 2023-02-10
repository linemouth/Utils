using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public struct Hsv : IColor
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
            new ColorChannelInfo("Alpha",      "A", 0, 1,   Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%",   1) }, false),
        };

        public static explicit operator Argb(Hsv hsv) => hsv.ToArgb();
        public static explicit operator Rgb (Hsv hsv) => hsv.ToRgb();
        public static explicit operator Hsl (Hsv hsv) => hsv.ToHsl();
        public static explicit operator Cmyk(Hsv hsv) => hsv.ToCmyk();
        public static explicit operator Xyl (Hsv hsv) => hsv.ToXyl();
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
        public Hsv(float hue = 0, float saturation = 0, float value = 0, float alpha = 1)
        {
            h = hue;
            s = saturation;
            v = value;
            a = alpha;
        }
        public Hsv(double hue, double saturation, double value, double alpha = 1) : this((float)hue, (float)saturation, (float)value, (float)alpha) { }
        public string ToString(string format = "hsv()") => Color.ToString(this, format);
        public Argb ToArgb() => ToRgb().ToArgb();
        public Rgb ToRgb()
        {
            Color.HsvToRgb(h, s, v, out double r, out double g, out double b);
            return new Rgb(r, g, b, a);
        }
        public Hsl ToHsl()
        {
            Color.HsvToHsl(this.s, v, out double s, out double l);
            return new Hsl(h, s, l, a);
        }
        public Hsv ToHsv() => this;
        public Cmyk ToCmyk() => ToRgb().ToCmyk();
        public Xyl ToXyl() => ToHsl().ToXyl();
    }
}
