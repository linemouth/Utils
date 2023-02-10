using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public struct Hsl : IColor
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
            new ColorChannelInfo("Alpha",      "A", 0, 1,   Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%",   1) }, false),
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
        public Hsl(double hue, double saturation, double lightness, double alpha = 1) : this((float)hue, (float)saturation, (float)lightness, (float)alpha) { }
        public string ToString(string format = "hsl()") => Color.ToString(this, format);
        public Argb ToArgb() => ToRgb().ToArgb();
        public Rgb ToRgb()
        {
            Color.HslToRgb(h, s, l, out double r, out double g, out double b);
            return new Rgb(r, g, b, a);
        }
        public Hsl ToHsl() => this;
        public Hsv ToHsv()
        {
            Color.HslToHsv(this.s, l, out double s, out double v);
            return new Hsv(h, s, v, a);
        }
        public Cmyk ToCmyk() => ToRgb().ToCmyk();
        public Xyl ToXyl()
        {
            Color.HslToXyl(h, s, l, out double x, out double y);
            return new Xyl(x, y, l, a);
        }
    }
}
