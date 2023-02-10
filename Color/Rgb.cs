using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public struct Rgb : IColor
    {
        public static readonly Rgb transparent = new Rgb(0, 0, 0, 0);
        public static readonly Rgb black       = new Rgb(0, 0, 0, 1);
        public static readonly Rgb white       = new Rgb(1, 1, 1, 1);
        public static readonly Rgb red         = new Rgb(1, 0, 0, 1);
        public static readonly Rgb green       = new Rgb(0, 1, 0, 1);
        public static readonly Rgb blue        = new Rgb(0, 0, 1, 1);
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
        public float Luminance
        {
            get
            {
                float luminance = 0.2126f * Color.LinearToGamma(r) + 0.7152f * Color.LinearToGamma(g) + 0.0722f * Color.LinearToGamma(b);
                return luminance;
            }
        }
        public float Lightness
        {
            get
            {
                float luminance = Luminance;
                // These constants come from 216 / 24389 according to the CIE standard.
                return luminance < 0.008856451679036f ? luminance * 9.03296296296296296f : Math.Pow(luminance, 0.33333333333f) * 1.16f - 0.16f;
            }
        }

        private static readonly ColorChannelInfo[] channelInfos = new ColorChannelInfo[]
        {
            new ColorChannelInfo("Red",   "R", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Green", "G", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Blue",  "B", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Alpha", "A", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }, false),
        };

        public static explicit operator Argb(Rgb rgb) => rgb.ToArgb();
        public static explicit operator Hsl (Rgb rgb) => rgb.ToHsl();
        public static explicit operator Hsv (Rgb rgb) => rgb.ToHsv();
        public static explicit operator Cmyk(Rgb rgb) => rgb.ToCmyk();
        public static explicit operator Xyl (Rgb rgb) => rgb.ToXyl();
        public static Rgb Parse(string text, out string format) => Color.Parse(text, out format).ToRgb();
        public static Rgb Parse(string text) => Color.Parse(text).ToRgb();
        public static bool TryParse(string text, out Rgb rgb, out string format)
        {
            if (Color.TryParse(text, out IColor color, out format))
            {
                rgb = color.ToRgb();
                return true;
            }
            rgb = default;
            return false;
        }
        public static bool TryParse(string text, out Rgb rgb) => TryParse(text, out rgb, out _);
        public static Rgb AlphaBlend(Rgb top, Rgb bottom)
        {
            if (top.a == 0)
            {
                return bottom;
            }
            else if (top.a == 1)
            {
                return top;
            }
            double bottomStrength = 1 - top.a;
            double alpha = bottom.a * bottomStrength + top.a;
            bottomStrength *= bottom.a / alpha;
            double topStrength = top.a / alpha;
            return new Rgb(
                bottom.r * bottomStrength + top.r * topStrength,
                bottom.g * bottomStrength + top.g * topStrength,
                bottom.b * bottomStrength + top.b * topStrength,
                alpha
            );
        }
        public static Rgb LerpUnclamped(Rgb a, Rgb b, double t) => new Rgb(Math.Lerp(a.r, b.r, t), Math.Lerp(a.g, b.g, t), Math.Lerp(a.b, b.b, t), Math.Lerp(a.a, b.a, t));
        public static Rgb Lerp(Rgb a, Rgb b, double t) => LerpUnclamped(a, b, Math.Clamp(t, 0, 1));
        public Rgb(float r = 0, float g = 0, float b = 0, float a = 1)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        public Rgb(double r, double g, double b, double a = 1) : this((float)r, (float)g, (float)b, (float)a) { }
        public string ToString(string format = "xyl()") => Color.ToString(this, format);
        public Rgb ToRgb() => this;
        public Argb ToArgb()
        {
            return new Argb(
                (byte)(Math.Clamp(r * 255 + 0.5, 0, 255)),
                (byte)(Math.Clamp(g * 255 + 0.5, 0, 255)),
                (byte)(Math.Clamp(b * 255 + 0.5, 0, 255)),
                (byte)(Math.Clamp(a * 255 + 0.5, 0, 255))
            );
        }
        public Hsl ToHsl()
        {
            Color.RgbToHsl(r, g, b, out double h, out double s, out double l);
            return new Hsl(h, s, l, a);
        }
        public Hsv ToHsv()
        {
            Color.RgbToHsv(r, g, b, out double h, out double s, out double v);
            return new Hsv(h, s, v, a);
        }
        public Cmyk ToCmyk()
        {
            Color.RgbToCmyk(r, g, b, out double c, out double m, out double y, out double k);
            return new Cmyk(c, m, y, k, a);
        }
        public Xyl ToXyl() => ToHsl().ToXyl();
        public Rgb LerpUnclamped(Rgb b, double t) => LerpUnclamped(this, b, t);
        public Rgb Lerp(Rgb b, double t) => Lerp(this, b, t);
    }
}
