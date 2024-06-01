using System;

namespace Utils
{
    public class ColorChannelInfo
    {
        public readonly string name;
        public readonly string abbreviation;
        public readonly float min;
        public readonly float max;
        public readonly Func<float, float, float, float> clampFunction;
        public readonly ColorChannelFormat[] formats;
        public readonly float? optionalDefault;
        public ColorChannelFormat DefaultFormat => formats[0];

        public ColorChannelInfo(string name, string abbreviation, float min, float max, Func<float, float, float, float> clampFunction, ColorChannelFormat[] formats, float? optionalDefault = null)
        {
            this.name = name;
            this.abbreviation = abbreviation;
            this.min = min;
            this.max = max;
            this.clampFunction = clampFunction;
            this.formats = formats;
            this.optionalDefault = optionalDefault;
        }
        public bool IsSupportedFormat(ColorChannelFormat format)
        {
            for(int i = 0; i < formats.Length; i++)
            {
                if(formats[i].suffix == format.suffix)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
