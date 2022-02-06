using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class Color
    {
        private static readonly Regex hexValueRegex = new Regex(@"(?<prefix>#|0x)?(?<value>[\da-fA-F]+)\b", RegexOptions.Compiled);
        private static readonly Regex modelValueRegex = new Regex(@"(?<model>\w+)\s*\(\s*(?<values>).*?\s*\)", RegexOptions.Compiled);
        private static readonly Regex hexFormatRegex = new Regex(@"(?<prefix>#?)(?<channels>rgb|argb|rgba|RGB|ARGB|RGBA)(?<bits>\d+)", RegexOptions.Compiled);
        private static readonly Regex modelFormatRegex = new Regex(@"(?<model>\w+)(?:\s*\(\s*(?<values>.*?)\s*\))?", RegexOptions.Compiled | RegexOptions.Multiline);

        public static byte FloatToByte(float value) => Convert.ToByte(Math.Clamp(value) * 255.0f);
        public static float ByteToFloat(byte value) => value / 255.0f;
        public static byte DoubleToByte(double value) => Convert.ToByte(Math.Clamp(value) * 255.0d);
        public static double ByteToDouble(byte value) => value / 255.0d;
        public static void HsvToRgb(double hue, double saturation, double value, out double red, out double green, out double blue)
        {
            double h = Math.Repeat(hue, 0, 360);
            double s = Math.Clamp(saturation);
            double v = Math.Clamp(value);

            double c = v * s;
            double sextant = (h / 60d);
            double x = c * (1 - Math.Abs(sextant % 2 - 1));
            double m = v - c;

            SextantToRgb((int)sextant, c + m, x + m, m, out red, out green, out blue);
        }
        public static void HsvToRgb(double hue, double saturation, double value, double alpha, out double red, out double green, out double blue)
        {
            if(alpha > 0)
            {
                HsvToRgb(hue, saturation, value, out red, out green, out blue);
            }
            else
            {
                red = green = blue = 0;
            }
        }
        public static void RgbToHsv(double red, double green, double blue, out double hue, out double saturation, out double value)
        {
            double r = Math.Clamp(red);
            double g = Math.Clamp(green);
            double b = Math.Clamp(blue);

            double[] rgb = new double[] { r, g, b };
            rgb.MinMaxIndex(out int minIndex, out int maxIndex);
            double max = rgb[maxIndex];
            double min = rgb[minIndex];
            double range = max - min;

            value = max;
            saturation = range > 0 ? (range / value) : 0;
            if(saturation > 0)
            {
                switch(maxIndex)
                {
                    case 0: // Red
                        hue = 60 * (((g - b) / range) % 6);
                        if(hue < 0)
                        {
                            hue += 360;
                        }
                        break;
                    case 1: // Green
                        hue = 60 * (((b - r) / range) + 2);
                        break;
                    default: // Blue
                        hue = 60 * (((r - g) / range) + 4);
                        break;
                }
            }
            else
            {
                hue = 0;
            }
        }
        public static void RgbToHsv(double red, double green, double blue, double alpha, out double hue, out double saturation, out double value)
        {
            if(alpha > 0)
            {
                RgbToHsv(red, green, blue, out hue, out saturation, out value);
            }
            else
            {
                hue = saturation = value = 0;
            }
        }
        public static void HslToRgb(double hue, double saturation, double lightness, out double red, out double green, out double blue)
        {
            double h = Math.Repeat(hue, 0, 360);
            double s = Math.Clamp(saturation);
            double l = Math.Clamp(lightness);

            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double sextant = (h / 60d);
            double x = c * (1 - Math.Abs(sextant % 2 - 1));
            double m = l - c / 2;

            SextantToRgb((int)sextant, c + m, x + m, m, out red, out green, out blue);
        }
        public static void HslToRgb(double hue, double saturation, double lightness, double alpha, out double red, out double green, out double blue)
        {
            if(alpha > 0)
            {
                HslToRgb(hue, saturation, lightness, out red, out green, out blue);
            }
            else
            {
                red = green = blue = 0;
            }
        }
        public static void RgbToHsl(double red, double green, double blue, out double hue, out double saturation, out double lightness)
        {
            double r = Math.Clamp(red);
            double g = Math.Clamp(green);
            double b = Math.Clamp(blue);

            double[] rgb = new double[] { r, g, b };
            rgb.MinMaxIndex(out int minIndex, out int maxIndex);
            double max = rgb[maxIndex];
            double min = rgb[minIndex];
            double range = max - min;

            lightness = (max + min) / 2;
            saturation = range > 0 ? (range / (1 - Math.Abs(2 * lightness - 1))) : 0;
            if(saturation > 0)
            {
                switch(maxIndex)
                {
                    case 0: // Red
                        hue = 60 * (((g - b) / range) % 6);
                        if(hue < 0)
                        {
                            hue += 360;
                        }
                        break;
                    case 1: // Green
                        hue = 60 * (((b - r) / range) + 2);
                        break;
                    default: // Blue
                        hue = 60 * (((r - g) / range) + 4);
                        break;
                }
            }
            else
            {
                hue = 0;
            }
        }
        public static void RgbToHsl(double red, double green, double blue, double alpha, out double hue, out double saturation, out double lightness)
        {
            if(alpha > 0)
            {
                RgbToHsl(red, green, blue, out hue, out saturation, out lightness);
            }
            else
            {
                hue = saturation = lightness = 0;
            }
        }
        
        public static IColor Parse(string text)
        {
            // Hex
            if(hexValueRegex.TryMatch(text, out Match match))
            {
                string v = match.Groups["value"].Value;
                Rgb rgb = new Rgb();

                switch(v.Length)
                {
                    case 3: // rgb4
                        rgb.r = int.Parse(v.Substring(0, 1), NumberStyles.AllowHexSpecifier) / 15f;
                        rgb.g = int.Parse(v.Substring(1, 1), NumberStyles.AllowHexSpecifier) / 15f;
                        rgb.b = int.Parse(v.Substring(2, 1), NumberStyles.AllowHexSpecifier) / 15f;
                        break;
                    case 6: // rgb8
                        rgb.r = int.Parse(v.Substring(0, 2), NumberStyles.AllowHexSpecifier) / 255f;
                        rgb.g = int.Parse(v.Substring(2, 2), NumberStyles.AllowHexSpecifier) / 255f;
                        rgb.b = int.Parse(v.Substring(4, 2), NumberStyles.AllowHexSpecifier) / 255f;
                        break;
                    default: // rgba*
                        long value = long.Parse(v, NumberStyles.AllowHexSpecifier);
                        int bits = v.Length;
                        int maxValue = 1 << (bits - 1);
                        rgb.r = (value >> (bits * 3) & maxValue) / maxValue;
                        rgb.g = (value >> (bits * 2) & maxValue) / maxValue;
                        rgb.b = (value >> (bits * 1) & maxValue) / maxValue;
                        rgb.a = (value               & maxValue) / maxValue;
                        break;
                }
                return rgb;
            }

            // Color Model
            if(modelValueRegex.TryMatch(text, out match))
            {

            }

            throw new ArgumentException($"Cannot parse color: '{text}'.");
        }
        public static bool TryParse(string text, out IColor color)
        {
            try
            {
                color = Parse(text);
                return true;
            }
            catch
            {
                color = default;
                return false;
            }
        }
        public static string ToString(this IColor color, string formatString)
        {
            // Hex
            if(hexFormatRegex.TryMatch(formatString, out Match match))
            {
                // Parse format
                string prefix = match.Groups["prefix"].Value;
                int bits = int.Parse(match.Groups["bits"].Value);
                string channelFormat = match.Groups["channels"].Value;
                bool uppercase = char.IsUpper(channelFormat[0]);

                // Arrange channels
                Rgb rgb = color.ToRgb();
                float[] channels;
                switch(channelFormat.ToLower())
                {
                    case "rgb": channels = new float[] { rgb.r, rgb.g, rgb.b }; break;
                    case "argb": channels = new float[] { rgb.a, rgb.r, rgb.g, rgb.b }; break;
                    case "rgba": channels = new float[] { rgb.r, rgb.g, rgb.b, rgb.a }; break;
                    default: throw new ArgumentException($"Invalid hex channel format: '{channelFormat}'");
                }

                // Calculate value
                ulong value = 0;
                int scale = 1 << bits;
                int padding = (channels.Length * bits + 7) >> 3;
                for(int i = 0; i < channels.Length; i++)
                {
                    ulong channelValue = (ulong)(channels[i] * scale + 0.5);
                    value |= channelValue << (bits * i);
                }

                // Assemble formatted string
                return prefix + value.ToString($"{padding}{(uppercase ? "X" : "x")}");
            }

            // Color Model
            if(modelFormatRegex.TryMatch(formatString, out match))
            {
                // Select model
                string model = match.Groups["model"].Value;
                switch(model)
                {
                    case "rgb":  color = color.ToRgb();  break;
                    /*case "hsl":  color = color.ToHsl();  break;
                    case "hsv":  color = color.ToHsv();  break;
                    case "cmyk": color = color.ToCmyk(); break;
                    case "xyl":  color = color.ToXyl();  break;*/
                    default: throw new ArgumentException($"Unsupported color model: '{model}'.");
                }

                // Parse channel formats
                string[] channelFormats = RegexExtensions.spaceCommaRegex.Split(match.Groups["channels"].Value);
                float[] values = color.Channels;
                ColorChannelInfo[] infos = color.ChannelInfos;
                List<string> strings = new List<string>();
                for(int i = 0; i < values.Length; i++)
                {
                    ColorChannelInfo info = infos[i];
                    ColorChannelFormat defaultFormat = info.DefaultFormat;
                    float value = values[i];
                    string suffix = null;
                    int precision = -1;
                    bool useSigFigs = false;
                    bool required = info.required;

                    // Try to use specified format.
                    if(i < channelFormats.Length)
                    {
                        string channelFormat = channelFormats[i];
                        if(ColorChannelFormat.regex.TryMatch(channelFormat, out match))
                        {
                            suffix = match.Groups["suffix"].Value;
                            required = true;
                            bool valid = false;
                            for(int f = 0; f < info.formats.Length; f++)
                            {
                                if(info.formats[f].suffix == suffix)
                                {
                                    valid = true;
                                    if(match.Groups["sigFigs"].Success)
                                    {
                                        precision = int.Parse(match.Groups["sigFigs"].Value);
                                        useSigFigs = true;
                                    }
                                    else if(match.Groups["decimals"].Success)
                                    {
                                        precision = int.Parse(match.Groups["decimals"].Value);
                                        useSigFigs = false;
                                    }
                                    else
                                    {
                                        precision = defaultFormat.precision;
                                        useSigFigs = defaultFormat.useSigFigs;
                                    }
                                    break;
                                }
                            }
                            if(!valid)
                            {
                                throw new ArgumentException($"{info.name} color channel does not support format: '{channelFormat}'.");
                            }
                        }
                        throw new ArgumentException($"Cannot parse channel format: '{channelFormats[i]}'.");
                    }
                    else // If no format was specified, use the channel default
                    {
                        suffix = defaultFormat.suffix;
                        precision = defaultFormat.precision;
                        useSigFigs = defaultFormat.useSigFigs;
                    }

                    if(required)
                    {
                        // Format string
                        if(suffix == "%")
                        {
                            value *= 100;
                        }
                        strings.Add((useSigFigs ? value.Format(precision) : value.ToString($"F{precision}")) + suffix);
                    }
                }

                // Assemble strings
                return $"{model}({string.Join(", ", strings)})";
            }

            throw new ArgumentException($"Unknown color format: '{formatString}'.");
        }
        public static IColor LerpUnclamped(this IColor a, IColor b, double t)
        {
            Rgb rgb0 = a.ToRgb();
            Rgb rgb1 = b.ToRgb();
            return new Rgb(
                Math.LerpUnclamped(rgb0.r, rgb1.r, t),
                Math.LerpUnclamped(rgb0.g, rgb1.g, t),
                Math.LerpUnclamped(rgb0.b, rgb1.b, t),
                Math.LerpUnclamped(rgb0.a, rgb1.a, t)
            );
        }
        public static IColor Lerp(this IColor a, IColor b, double t) => LerpUnclamped(a, b, Math.Clamp(t));


        internal static void SextantToRgb(int sextant, double c_m, double x_m, double m, out double red, out double green, out double blue)
        {
            switch(sextant)
            {
                case 0: // 0 <= hue < 60
                    red = c_m;
                    green = x_m;
                    blue = m;
                    break;
                case 1: // 60 <= hue < 120
                    red = x_m;
                    green = c_m;
                    blue = m;
                    break;
                case 2: // 120 <= hue < 180
                    red = m;
                    green = c_m;
                    blue = x_m;
                    break;
                case 3: // 180 <= hue < 240
                    red = m;
                    green = x_m;
                    blue = c_m;
                    break;
                case 4: // 240 <= hue < 300
                    red = x_m;
                    green = m;
                    blue = c_m;
                    break;
                default: // 300 <= hue < 360
                    red = c_m;
                    green = m;
                    blue = x_m;
                    break;
            }
        }
    }
}