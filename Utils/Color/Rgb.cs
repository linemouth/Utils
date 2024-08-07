﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public struct Rgb : IColor, IEquatable<Rgb>
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
        public bool AlphaOversaturated => a < 0 || a > 1;
        public bool RedOversaturated => r < 0 || r > 1;
        public bool GreenOversaturated => g < 0 || g > 1;
        public bool BlueOversaturated => b < 0 || b > 1;
        public bool IsOversaturated => a < 0 || a > 1 || r < 0 || r > 1 || g < 0 || g > 1 || b < 0 || b > 1;
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
            new ColorChannelInfo("Alpha", "A", 0, 1, Math.Clamp, new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }, 1),
        };

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
            float bottomStrength = 1 - top.a;
            float alpha = bottom.a * bottomStrength + top.a;
            bottomStrength *= bottom.a / alpha;
            float topStrength = top.a / alpha;
            return new Rgb(
                bottom.r * bottomStrength + top.r * topStrength,
                bottom.g * bottomStrength + top.g * topStrength,
                bottom.b * bottomStrength + top.b * topStrength,
                alpha
            );
        }
        public static Rgb LerpUnclamped(Rgb a, Rgb b, float t) => new Rgb(Math.Lerp(a.r, b.r, t), Math.Lerp(a.g, b.g, t), Math.Lerp(a.b, b.b, t), Math.Lerp(a.a, b.a, t));
        public static Rgb Lerp(Rgb a, Rgb b, float t) => LerpUnclamped(a, b, Math.Clamp(t, 0, 1));
        public Rgb(double r = 0, double g = 0, double b = 0, double a = 1) : this((float)r, (float)g, (float)b, (float)a) { }
        public Rgb(float r = 0, float g = 0, float b = 0, float a = 1)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
        public T ToModel<T>() where T : IColor => Color.ToModel<T>(this);
        public Rgb ToRgb() => this;
        public Argb ToArgb()
        {
            return new Argb(
                (byte)(Math.Clamp(r * 255 + 0.5f, 0, 255)),
                (byte)(Math.Clamp(g * 255 + 0.5f, 0, 255)),
                (byte)(Math.Clamp(b * 255 + 0.5f, 0, 255)),
                (byte)(Math.Clamp(a * 255 + 0.5f, 0, 255))
            );
        }
        public Hsl ToHsl()
        {
            Color.RgbToHsl(r, g, b, out float h, out float s, out float l);
            return new Hsl(h, s, l, a);
        }
        public Hsv ToHsv()
        {
            Color.RgbToHsv(r, g, b, out float h, out float s, out float v);
            return new Hsv(h, s, v, a);
        }
        public Cmyk ToCmyk()
        {
            Color.RgbToCmyk(r, g, b, out float c, out float m, out float y, out float k);
            return new Cmyk(c, m, y, k, a);
        }
        public Xyl ToXyl() => ToHsl().ToXyl();
        public override int GetHashCode() => ToArgb().GetHashCode();
        public string ToString(string format = "rgb()") => Color.ToString(this, format);
        public override string ToString() => ToString();
        public override bool Equals(object obj) => obj is IColor color && Equals(color);
        public bool Equals(IColor other) => Approximately(other);
        public bool Equals(Rgb other) => Approximately(other);
        public bool Approximately(IColor other, float margin = 1e-3f) => Color.Approximately(this, other, margin);
        public void Premultiply()
        {
            if(a > 0)
            {
                r *= a;
                g *= a;
                b *= a;
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
                r /= a;
                g /= a;
                b /= a;
            }
            else
            {
                r = g = b = 0;
            }
        }
        public Rgb LerpUnclamped(IColor to, float t)
        {
            Rgb rgb = (Rgb)to;
            return new Rgb(
                Math.LerpUnclamped(r, rgb.r, t),
                Math.LerpUnclamped(g, rgb.g, t),
                Math.LerpUnclamped(b, rgb.b, t),
                Math.LerpUnclamped(a, rgb.a, t)
            );
        }
        public Rgb Lerp(IColor to, float t) => (Rgb)Color.Lerp(this, to, t);
    }
}
