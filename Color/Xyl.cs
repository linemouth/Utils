using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    // This model uses a space in which the z-axis is lightness, and the x-y axes represent hue and saturation. This results in a volume that is two cones whose faces meet.
    // At the top vertex is white. The bottom vertex is black. The circumference is 100% saturation and 50% lightness and spans all hues. The center is 50% gray.
    public struct Xyl : IColor, IEquatable<Xyl>
    {
        public float x;
        public float y;
        public float l;
        public float a;
        public float[] Channels => new float[] { x, y, l, a };
        public ColorChannelInfo[] ChannelInfos => channelInfos;

        private static readonly ColorChannelInfo[] channelInfos = new ColorChannelInfo[]
        {
            new ColorChannelInfo("X",         "X", -1, 1, Math.Repeat, new ColorChannelFormat[] { new ColorChannelFormat("", 3) }),
            new ColorChannelInfo("Y",         "Y", -1, 1, Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3) }),
            new ColorChannelInfo("Lightness", "L",  0, 1, Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }),
            new ColorChannelInfo("Alpha",     "A",  0, 1, Math.Clamp,  new ColorChannelFormat[] { new ColorChannelFormat("", 3), new ColorChannelFormat("%", 1) }, 1),
        };

        public static explicit operator Argb(Xyl xyl) => xyl.ToArgb();
        public static explicit operator Rgb (Xyl xyl) => xyl.ToRgb();
        public static explicit operator Hsl (Xyl xyl) => xyl.ToHsl();
        public static explicit operator Hsv (Xyl xyl) => xyl.ToHsv();
        public static explicit operator Cmyk(Xyl xyl) => xyl.ToCmyk();
        public static Xyl Parse(string text, out string format) => Color.Parse(text, out format).ToXyl();
        public static Xyl Parse(string text) => Color.Parse(text).ToXyl();
        public static bool TryParse(string text, out Xyl xyl, out string format)
        {
            if (Color.TryParse(text, out IColor color, out format))
            {
                xyl = color.ToXyl();
                return true;
            }
            xyl = default;
            return false;
        }
        public Xyl(float x, float y, float lightness, float alpha = 1)
        {
            this.x = x;
            this.y = y;
            l = lightness;
            a = alpha;
        }
        public Rgb ToRgb() => ToHsl().ToRgb();
        public Argb ToArgb() => ToHsl().ToArgb();
        public Hsl ToHsl()
        {
            Color.XylToHsl(x, y, l, out float h, out float s);
            return new Hsl(h, s, l, a);
        }
        public Hsv ToHsv() => ToHsl().ToHsv();
        public Cmyk ToCmyk() => ToHsl().ToCmyk();
        public Xyl ToXyl() => this;
        public override int GetHashCode() => ToArgb().GetHashCode();
        public string ToString(string format = "xyl()") => Color.ToString(this, format);
        public override string ToString() => ToString();
        public override bool Equals(object obj) => obj is IColor color && Equals(color);
        public bool Equals(IColor other) => Approximately(other);
        public bool Equals(Xyl other) => Approximately(other);
        public bool Approximately(IColor other, float margin = 1e-3f) => Color.Approximately(this, other, margin);
        public Xyl LerpUnclamped(IColor b, float t) => (Xyl)Color.LerpUnclamped(this, b, t);
        public Xyl Lerp(IColor b, float t) => (Xyl)Color.Lerp(this, b, t);
    }
}
