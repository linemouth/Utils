using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public interface IColor : IEquatable<IColor>
    {
        float[] Channels { get; }
        ColorChannelInfo[] ChannelInfos { get; }

        string ToString(string format);
        T ToModel<T>() where T : IColor;
        Argb ToArgb();
        Rgb ToRgb();
        Hsl ToHsl();
        Hsv ToHsv();
        Cmyk ToCmyk();
        Xyl ToXyl();
    }
}
