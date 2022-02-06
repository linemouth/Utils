using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public interface IColor
    {
        float[] Channels { get; }
        ColorChannelInfo[] ChannelInfos { get; }
        string ToString(string format);
        Rgb ToRgb();
        Argb ToArgb();
    }
}
