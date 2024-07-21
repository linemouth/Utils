using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utils
{
    public static class Color
    {
        public struct NamedColor
        {
            public readonly string Name;
            public readonly string Group;
            public readonly Argb Color;
            public readonly bool IsCss;

            internal readonly Xyl Xyl;

            public NamedColor(string name, string group, Argb color, bool isCss = false)
            {
                Name = name;
                Group = group;
                Color = color;
                IsCss = isCss;
                Xyl = (Xyl)Color;
            }
        }
        public static NamedColor[] NamedColors => namedColors.ToArray();
        public static NamedColor[] CssColors => namedColors.Where(namedColor => namedColor.IsCss).ToArray();

        private static readonly Regex hexValueRegex = new Regex(@"(?<prefix>#|0x)?(?<value>[\da-fA-F]+)\b", RegexOptions.Compiled);
        private static readonly Regex modelValueRegex = new Regex(@"(?<model>\w+)\s*\(\s*(?<values>).*?\s*\)", RegexOptions.Compiled);
        private static readonly Regex hexFormatRegex = new Regex(@"(?<prefix>#|0x|0X|)(?<channels>rgb|argb|rgba|RGB|ARGB|RGBA)(?<bits>\d+)", RegexOptions.Compiled);
        private static readonly Regex modelFormatRegex = new Regex(@"(?<model>\w+)(?:\s*\(\s*(?<channels>.*?)\s*\))?", RegexOptions.Compiled | RegexOptions.Multiline);

        internal static readonly List<NamedColor> namedColors = InitializeNamedColors();

        public static float LinearToGamma(float channel) => channel < 0.04045f ? channel / 12.92f : Math.Pow((channel + 0.055f) / 1.055f, 2.2f);
        public static float GammaToLinear(float channel) => Math.Pow(channel, 1 / 2.2f);
        public static byte FloatToByte(float value) => Convert.ToByte(Math.Clamp(value) * 255.0f);
        public static float ByteToFloat(byte value) => value / 255.0f;
        public static byte DoubleToByte(double value) => Convert.ToByte(Math.Clamp(value) * 255.0d);
        public static double ByteToDouble(byte value) => value / 255.0d;
        public static void HsvToRgb(float hue, float saturation, float value, out float red, out float green, out float blue)
        {
            float h = Math.Repeat(hue, 0, 360);
            float s = Math.Clamp(saturation);
            float v = Math.Clamp(value);

            float c = v * s;
            float sextant = (h / 60f);
            float x = c * (1 - Math.Abs(sextant % 2 - 1));
            float m = v - c;

            SextantToRgb((int)sextant, c + m, x + m, m, out red, out green, out blue);
        }
        public static void RgbToHsv(float red, float green, float blue, out float hue, out float saturation, out float value)
        {
            float r = Math.Clamp(red);
            float g = Math.Clamp(green);
            float b = Math.Clamp(blue);

            float[] rgb = new float[] { r, g, b };
            rgb.MinMaxIndex(out int minIndex, out int maxIndex);
            float max = rgb[maxIndex];
            float min = rgb[minIndex];
            float range = max - min;

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
        public static void HslToRgb(float hue, float saturation, float lightness, out float red, out float green, out float blue)
        {
            float h = Math.Repeat(hue, 0, 360);
            float s = Math.Clamp(saturation);
            float l = Math.Clamp(lightness);

            float c = (1 - Math.Abs(2 * l - 1)) * s;
            float sextant = (h / 60f);
            float x = c * (1 - Math.Abs(sextant % 2 - 1));
            float m = l - c / 2;

            SextantToRgb((int)sextant, c + m, x + m, m, out red, out green, out blue);
        }
        public static void RgbToHsl(float red, float green, float blue, out float hue, out float saturation, out float lightness)
        {
            float r = Math.Clamp(red);
            float g = Math.Clamp(green);
            float b = Math.Clamp(blue);

            float[] rgb = new float[] { r, g, b };
            rgb.MinMaxIndex(out int minIndex, out int maxIndex);
            float max = rgb[maxIndex];
            float min = rgb[minIndex];
            float range = max - min;

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
        public static void HslToHsv(float hslSaturation, float lightness, out float hsvSaturation, out float value)
        {
            value = lightness + hslSaturation * Math.Min(lightness, 1 - lightness);
            hsvSaturation = value > 0 ? 2 - 2 * lightness / value : 0;
        }
        public static void HsvToHsl(float hsvSaturation, float value, out float hslSaturation, out float lightness)
        {
            lightness = (2 - hsvSaturation) * value / 2;
            float lm = Math.Min(lightness, 1 - lightness);
            hslSaturation = lm > 0 ? (value - lightness) / lm : 0;
        }
        public static void RgbToCmyk(float red, float green, float blue, out float cyan, out float magenta, out float yellow, out float black)
        {
            float maxColor = Math.Max(red, Math.Max(green, blue));
            black = 1 - maxColor;
            if(maxColor > 0)
            {
                cyan = (1 - red - black) / maxColor;
                magenta = (1 - green - black) / maxColor;
                yellow = (1 - blue - black) / maxColor;
            }
            else
            {
                cyan = magenta = yellow = 0;
            }
        }
        public static void CmykToRgb(float cyan, float magenta, float yellow, float black, out float red, out float green, out float blue) {
            red = (1 - cyan) * (1 - black);
            green = (1 - magenta) * (1 - black);
            blue = (1 - yellow) * (1 - black);
        }
        public static void HslToXyl(float hue, float saturation, float lightness, out float x, out float y)
        {
            float radians = hue * (float)Math.PI / 180;
            float radius = 1 - Math.Abs(1 - 2 * lightness);
            x = Math.Cos(radians) * radius * saturation;
            y = Math.Sin(radians) * radius * saturation;
        }
        public static void XylToHsl(float x, float y, float l, out float h, out float s)
        {
            h = Math.Atan2(y, x) * (float)Math.RadToDeg;
            float radius = 0.5f - Math.Abs(0.5f - l);
            s = radius > 0 ? Math.Sqrt(x * x + y * y) / radius : 0;
        }
        public static NamedColor GetNearestColor(IColor color, bool cssOnly)
        {
            Xyl xyl = color.ToXyl();
            NamedColor bestColor = new NamedColor("Black", "Gray", new Argb(0, 0, 0), true);
            float bestSqrDistance = float.PositiveInfinity;

            foreach(NamedColor namedColor in namedColors)
            {
                if(!cssOnly || namedColor.IsCss)
                {
                    float dx = xyl.x - namedColor.Xyl.x;
                    float dy = xyl.y - namedColor.Xyl.y;
                    float sqrDistance = dx * dx + dy * dy;
                    if(sqrDistance < bestSqrDistance)
                    {
                        bestColor = namedColor;
                        bestSqrDistance = sqrDistance;
                    }
                }
            }

            return bestColor;
        }
        public static IColor Parse(string text) => Parse(text, out _);
        public static IColor Parse(string text, out string format)
        {
            // Hex
            if(hexValueRegex.TryMatch(text, out Match match))
            {
                string v = match.Groups["value"].Value;
                format = match.Groups["prefix"].Value;
                Rgb rgb = new Rgb();

                switch(v.Length)
                {
                    case 3: // rgb4
                        rgb.r = int.Parse(v.Substring(0, 1), NumberStyles.AllowHexSpecifier) / 15f;
                        rgb.g = int.Parse(v.Substring(1, 1), NumberStyles.AllowHexSpecifier) / 15f;
                        rgb.b = int.Parse(v.Substring(2, 1), NumberStyles.AllowHexSpecifier) / 15f;
                        format += "hex4";
                        break;
                    case 6: // rgb8
                        rgb.r = int.Parse(v.Substring(0, 2), NumberStyles.AllowHexSpecifier) / 255f;
                        rgb.g = int.Parse(v.Substring(2, 2), NumberStyles.AllowHexSpecifier) / 255f;
                        rgb.b = int.Parse(v.Substring(4, 2), NumberStyles.AllowHexSpecifier) / 255f;
                        format += "hex8";
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
        public static bool TryParse(string text, out IColor color) => TryParse(text, out color, out _);
        public static bool TryParse(string text, out IColor color, out string format)
        {
            try
            {
                color = Parse(text, out format);
                return true;
            }
            catch
            {
                color = default;
                format = null;
                return false;
            }
        }
        public static float GetDistance(IColor a, IColor b)
        {
            Xyl xylA = a.ToXyl();
            Xyl xylB = b.ToXyl();
            float dX2 = Math.Sqr(xylA.x - xylB.x);
            float dY2 = Math.Sqr(xylA.y - xylB.y);
            float dL2 = Math.Sqr(xylA.l - xylB.l);
            return Math.Sqrt(dX2 + dY2 + dL2);
        }
        public static string ToString(IColor color, string format)
        {
            // Hex
            if(hexFormatRegex.TryMatch(format, out Match match))
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
                    case "rgb":  channels = new float[] { rgb.r, rgb.g, rgb.b }; break;
                    case "argb": channels = new float[] { rgb.a, rgb.r, rgb.g, rgb.b }; break;
                    case "rgba": channels = new float[] { rgb.r, rgb.g, rgb.b, rgb.a }; break;
                    default: throw new ArgumentException($"Invalid hex channel format: '{channelFormat}'");
                }

                // Calculate value
                ulong value = 0;
                int scale = ~(~0 << bits);
                int padding = (int)Math.Ceiling(channels.Length * bits / 4);
                for(int i = 0; i < channels.Length; i++)
                {
                    ulong channelValue = (ulong)(channels[i] * scale + 0.5f);
                    value = channelValue | value << bits;
                }

                // Assemble formatted string
                return prefix + value.ToString($"{(uppercase ? "X" : "x")}{padding}");
            }

            // Color Model
            if(modelFormatRegex.TryMatch(format, out match))
            {
                // Select model
                string model = match.Groups["model"].Value;
                switch(model)
                {
                    case "rgb":  color = color.ToRgb(); break;
                    case "hsl":  color = color.ToHsl();  break;
                    case "hsv":  color = color.ToHsv();  break;
                    case "cmyk": color = color.ToCmyk(); break;
                    case "xyl":  color = color.ToXyl();  break;
                    default: throw new ArgumentException($"Unsupported color model: '{model}'.");
                }

                // Parse channel formats
                string[] channelFormats = RegexExtensions.spaceCommaRegex.Split(match.Groups["channels"].Value).Where(channel => !string.IsNullOrWhiteSpace(channel)).ToArray();
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
                    bool required = info.optionalDefault == null || info.optionalDefault != value;

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

            throw new ArgumentException($"Unknown color format: '{format}'.");
        }
        public static IColor LerpUnclamped(this IColor a, IColor b, float t) => a.ToRgb().LerpUnclamped(b.ToRgb(), t);
        public static IColor Lerp(this IColor from, IColor to, float t) => LerpUnclamped(from, to, Math.Clamp(t));
        public static bool Approximately(IColor a, IColor b, float margin = 1e-3f)
        {
            Rgb A = a.ToRgb();
            Rgb B = b.ToRgb();
            float difference = Math.Abs(A.r - B.r) + Math.Abs(A.g - B.g) + Math.Abs(A.b - B.b) + Math.Abs(A.a - B.a);
            return difference <= margin;
        }

        internal static void SextantToRgb(int sextant, float c_m, float x_m, float m, out float red, out float green, out float blue)
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
        internal static List<NamedColor> InitializeNamedColors()
        {
            string namedColorText = @"
Acadia,                    Brown,  35312C
Acapulco,                  Green,  75AA94
Aero Blue,                 Green,  C0E8D5
Affair,                    Violet, 745085
Afghan Tan,                Yellow, 905E26
Air Force Blue,            Blue,   5D8AA8
Akaroa,                    Yellow, BEB29A
Alabaster,                 Gray,   F2F0E6
Albescent White,           Yellow, E1DACB
Alert Tan,                 Orange, 954E2C
Alice Blue,                Blue,   F0F8FF, true
Alizarin,                  Red,    E32636
Allports,                  Blue,   1F6A7D
Almond,                    Yellow, EED9C4
Almond Frost,              Brown,  9A8678
Alpine,                    Yellow, AD8A3B
Alto,                      Gray,   CDC6C5
Aluminium,                 Gray,   848789
Amaranth,                  Red,    E52B50
Amazon,                    Green,  387B54
Amber,                     Yellow, FFBF00
Americano,                 Brown,  8A7D72
Amethyst,                  Violet, 9966CC
Amethyst Smoke,            Violet, 95879C
Amour,                     Violet, F5E6EA
Amulet,                    Green,  7D9D72
Anakiwa,                   Blue,   8CCEEA
Antique Brass,             Orange, 6C461F
Antique White,             White,  FAEBD7, true
Anzac,                     Yellow, C68E3F
Apache,                    Yellow, D3A95C
Apple,                     Green,  66B348
Apple Blossom,             Red,    A95249
Apple Green,               Green,  DEEADC
Apricot,                   Orange, FBCEB1
Apricot White,             Yellow, F7F0DB
Aqua,                      Blue,   00FFFF, true
Aqua Haze,                 Gray,   D9DDD5
Aqua Spring,               Green,  E8F3E8
Aqua Squeeze,              Gray,   DBE4DC
Aquamarine,                Blue,   7FFFD4, true
Arapawa,                   Blue,   274A5D
Armadillo,                 Gray,   484A46
Army green,                Green,  4B5320
Arrowtown,                 Yellow, 827A67
Arsenic,                   Gray,   3B444B
Ash,                       Green,  BEBAA7
Asparagus,                 Green,  7BA05B
Astra,                     Yellow, EDD5A6
Astral,                    Blue,   376F89
Astronaut,                 Blue,   445172
Astronaut Blue,            Blue,   214559
Athens Gray,               Gray,   DCDDDD
Aths Special,              Yellow, D5CBB2
Atlantis,                  Green,  9CD03B
Atoll,                     Green,  2B797A
Atomic,                    Blue,   3D4B52
Atomic Tangerine,          Orange, FF9966
Au Chico,                  Brown,  9E6759
Aubergine,                 Brown,  372528
Auburn,                    Brown,  712F2C
Australian Mint,           Green,  EFF8AA
Avocado,                   Green,  95986B
Axolotl,                   Green,  63775A
Azalea,                    Red,    F9C0C4
Aztec,                     Green,  293432
Azure,                     Blue,   F0FFFF, true
Baby Blue,                 Blue,   6FFFFF
Bahama Blue,               Blue,   25597F
Bahia,                     Green,  A9C01C
Baker's Chocolate,         Brown,  5C3317
Bali Hai,                  Blue,   849CA9
Baltic Sea,                Gray,   3C3D3E
Banana Mania,              Yellow, FBE7B2
Bandicoot,                 Green,  878466
Barberry,                  Green,  D2C61F
Barley Corn,               Yellow, B6935C
Barley White,              Yellow, F7E5B7
Barossa,                   Violet, 452E39
Bastille,                  Blue,   2C2C32
Battleship Gray,           Gray,   51574F
Bay Leaf,                  Green,  7BB18D
Bay Of Many,               Blue,   353E64
Bazaar,                    Brown,  8F7777
Beauty Bush,               Red,    EBB9B3
Beaver,                    Brown,  926F5B
Beeswax,                   Yellow, E9D7AB
Beige,                     Brown,  F5F5DC, true
Bermuda,                   Green,  86D2C1
Bermuda Gray,              Blue,   6F8C9F
Beryl Green,               Green,  BCBFA8
Bianca,                    Yellow, F4EFE0
Big Stone,                 Blue,   334046
Bilbao,                    Green,  3E8027
Biloba Flower,             Violet, AE99D2
Birch,                     Yellow, 3F3726
Bird Flower,               Green,  D0C117
Biscay,                    Blue,   2F3C53
Bismark,                   Blue,   486C7A
Bison Hide,                Yellow, B5AC94
Bisque,                    Brown,  FFE4C4, true
Bistre,                    Brown,  3D2B1F
Bitter,                    Green,  88896C
Bitter Lemon,              Green,  D2DB32
Bittersweet,               Orange, FE6F5E
Bizarre,                   Orange, E7D2C8
Black,                     Gray,   000000, true
Black Bean,                Green,  232E26
Black Forest,              Green,  2C3227
Black Haze,                Gray,   E0DED7
Black Magic,               Brown,  332C22
Black Marlin,              Blue,   383740
Black Pearl,               Blue,   1E272C
Black Rock,                Blue,   2C2D3C
Black Rose,                Red,    532934
Black Russian,             Gray,   24252B
Black Squeeze,             Gray,   E5E6DF
Black White,               Gray,   E5E4DB
Blackberry,                Violet, 43182F
Blackcurrant,              Violet, 2E183B
Blanc,                     Yellow, D9D0C1
Blanched Almond,           Brown,  FFEBCD, true
Bleach White,              Yellow, EBE1CE
Blizzard Blue,             Blue,   A3E3ED
Blossom,                   Red,    DFB1B6
Blue,                      Blue,   0000FF, true
Blue Bayoux,               Blue,   62777E
Blue Bell,                 Blue,   9999CC
Blue Chalk,                Violet, E3D6E9
Blue Charcoal,             Blue,   262B2F
Blue Chill,                Green,  408F90
Blue Diamond,              Violet, 4B2D72
Blue Dianne,               Green,  35514F
Blue Gem,                  Violet, 4B3C8E
Blue Haze,                 Violet, BDBACE
Blue Lagoon,               Green,  00626F
Blue Marguerite,           Violet, 6A5BB1
Blue Romance,              Green,  D8F0D2
Blue Smoke,                Green,  78857A
Blue Stone,                Green,  166461
Blue Violet,               Violet, 8A2BE2, true
Blue Whale,                Blue,   1E3442
Blue Zodiac,               Blue,   3C4354
Blumine,                   Blue,   305C71
Blush,                     Red,    B55067
Bokara Gray,               Gray,   2A2725
Bole,                      Brown,  79443B
Bombay,                    Gray,   AEAEAD
Bon Jour,                  Gray,   DFD7D2
Bondi Blue,                Blue,   0095B6
Bone,                      Orange, DBC2AB
Bordeaux,                  Red,    4C1C24
Bossanova,                 Violet, 4C3D4E
Boston Blue,               Blue,   438EAC
Botticelli,                Blue,   92ACB4
Bottle Green,              Green,  254636
Boulder,                   Gray,   7C817C
Bouquet,                   Violet, A78199
Bourbon,                   Orange, AF6C3E
Bracken,                   Brown,  5B3D27
Brandy,                    Orange, DCB68A
Brandy Punch,              Orange, C07C40
Brandy Rose,               Red,    B6857A
Brass,                     Yellow, B5A642
Breaker Bay,               Green,  517B78
Brick Red,                 Red,    C62D42
Bridal Heath,              Orange, F8EBDD
Bridesmaid,                Orange, FAE6DF
Bright Green,              Green,  66FF00
Bright Gray,               Gray,   57595D
Bright Red,                Red,    922A31
Bright Sun,                Yellow, ECBD2C
Bright Turquoise,          Blue,   08E8DE
Brilliant Rose,            Red,    FF55A3
Brink Pink,                Red,    FB607F
British Racing Green,      Green,  004225
Bronco,                    Brown,  A79781
Bronze,                    Brown,  CD7F32
Bronze Olive,              Yellow, 584C25
Bronzetone,                Yellow, 434C28
Broom,                     Yellow, EECC24
Brown,                     Brown,  A52A2A, true
Brown Bramble,             Brown,  53331E
Brown Derby,               Brown,  594537
Brown Pod,                 Brown,  3C241B
Bubbles,                   Green,  E6F2EA
Buccaneer,                 Red,    6E5150
Bud,                       Green,  A5A88F
Buddha Gold,               Yellow, BC9B1B
Buff,                      Yellow, F0DC82
Bulgarian Rose,            Red,    482427
Bull Shot,                 Orange, 75442B
Bunker,                    Gray,   292C2F
Bunting,                   Blue,   2B3449
Burgundy,                  Red,    800020
Burly Wood,                Brown,  DEB887, true
Burnham,                   Green,  234537
Burning Sand,              Orange, D08363
Burnt Crimson,             Red,    582124
Burnt Orange,              Orange, FF7034
Burnt Sienna,              Brown,  E97451
Burnt Umber,               Brown,  8A3324
Buttercup,                 Yellow, DA9429
Buttered Rum,              Yellow, 9D702E
Butterfly Bush,            Violet, 68578C
Buttermilk,                Yellow, F6E0A4
Buttery White,             Yellow, F1EBDA
Cab Sav,                   Red,    4A2E32
Cabaret,                   Red,    CD526C
Cabbage Pont,              Green,  4C5544
Cactus,                    Green,  5B6F55
Cadet Blue,                Blue,   5F9EA0, true
Cadillac,                  Red,    984961
Cafe Royale,               Brown,  6A4928
Calico,                    Brown,  D5B185
California,                Orange, E98C3A
Calypso,                   Blue,   3D7188
Camarone,                  Green,  206937
Camelot,                   Red,    803A4B
Cameo,                     Brown,  CCA483
Camouflage,                Yellow, 4F4D32
Camouflage Green,          Green,  78866B
Can Can,                   Red,    D08A9B
Canary,                    Yellow, FFFF99
Cannon Pink,               Red,    8E5164
Cape Cod,                  Gray,   4E5552
Cape Honey,                Yellow, FEE0A5
Cape Palliser,             Orange, 75482F
Caper,                     Green,  AFC182
Caput Mortuum,             Brown,  592720
Caramel,                   Yellow, FFD59A
Cararra,                   Green,  EBE5D5
Cardin Green,              Green,  1B3427
Cardinal,                  Red,    C41E3A
Careys Pink,               Red,    C99AA0
Caribbean Green,           Green,  00CC99
Carissma,                  Red,    E68095
Carla,                     Green,  F5F9CB
Carmine,                   Red,    960018
Carnaby Tan,               Brown,  5B3A24
Carnation Pink,            Red,    FFA6C9
Carousel Pink,             Red,    F8DBE0
Carrot Orange,             Orange, ED9121
Casablanca,                Yellow, F0B253
Casal,                     Blue,   3F545A
Cascade,                   Green,  8CA8A0
Cashmere,                  Brown,  D1B399
Casper,                    Blue,   AAB5B8
Castro,                    Red,    44232F
Catalina Blue,             Blue,   273C5A
Catskill White,            Gray,   E0E4DC
Cavern Pink,               Red,    E0B8B1
Ce Soir,                   Violet, 9271A7
Cedar,                     Brown,  463430
Celadon,                   Green,  ACE1AF
Celery,                    Green,  B4C04C
Celeste,                   Green,  D2D2C0
Cello,                     Blue,   3A4E5F
Celtic,                    Green,  2B3F36
Cement,                    Brown,  857158
Cerise,                    Violet, DE3163
Cerulean,                  Blue,   007BA7
Cerulean Blue,             Blue,   2A52BE
Chablis,                   Red,    FDE9E0
Chalet Green,              Green,  5A6E41
Chalky,                    Yellow, DFC281
Chambray,                  Blue,   475877
Chamois,                   Yellow, E8CD9A
Champagne,                 Yellow, EED9B6
Chantilly,                 Red,    EDB8C7
Charade,                   Blue,   394043
Charcoal,                  Gray,   464646
Chardon,                   Orange, F8EADF
Chardonnay,                Yellow, FFC878
Charlotte,                 Blue,   A4DCE6
Charm,                     Red,    D0748B
Chartreuse,                Green,  7FFF00, true
Chartreuse Yellow,         Yellow, DFFF00
Chateau Green,             Green,  419F59
Chatelle,                  Violet, B3ABB6
Chathams Blue,             Blue,   2C5971
Chelsea Cucumber,          Green,  88A95B
Chelsea Gem,               Orange, 95532F
Chenin,                    Yellow, DEC371
Cherokee,                  Yellow, F5CD82
Cherry Pie,                Violet, 372D52
Cherub,                    Red,    F5D7DC
Chestnut,                  Brown,  B94E48
Chetwode Blue,             Blue,   666FB4
Chicago,                   Gray,   5B5D56
Chiffon,                   Green,  F0F5BB
Chilean Fire,              Orange, D05E34
Chilean Heath,             Green,  F9F7DE
China Ivory,               Green,  FBF3D3
Chino,                     Yellow, B8AD8A
Chinook,                   Green,  9DD3A8
Chocolate,                 Brown,  D2691E, true
Christalle,                Violet, 382161
Christi,                   Green,  71A91D
Christine,                 Orange, BF652E
Chrome White,              Green,  CAC7B7
Cigar,                     Brown,  7D4E38
Cinder,                    Gray,   242A2E
Cinderella,                Red,    FBD7CC
Cinnabar,                  Red,    E34234
Cioccolato,                Brown,  5D3B2E
Citron,                    Green,  8E9A21
Citrus,                    Green,  9FB70A
Clam Shell,                Orange, D2B3A9
Claret,                    Red,    6E2233
Classic Rose,              Violet, F4C8DB
Clay Creek,                Yellow, 897E59
Clear Day,                 Green,  DFEFEA
Clinker,                   Brown,  463623
Cloud,                     Yellow, C2BCB1
Cloud Burst,               Blue,   353E4F
Cloudy,                    Brown,  B0A99F
Clover,                    Green,  47562F
Cobalt,                    Blue,   0047AB
Cocoa Bean,                Red,    4F3835
Cocoa Brown,               Brown,  35281E
Coconut Cream,             Green,  E1DABB
Cod Gray,                  Gray,   2D3032
Coffee,                    Yellow, 726751
Coffee Bean,               Brown,  362D26
Cognac,                    Red,    9A463D
Cola,                      Brown,  3C2F23
Cold Purple,               Violet, 9D8ABF
Cold Turkey,               Red,    CAB5B2
Columbia Blue,             Blue,   9BDDFF
Comet,                     Blue,   636373
Como,                      Green,  4C785C
Conch,                     Green,  A0B1AE
Concord,                   Gray,   827F79
Concrete,                  Gray,   D2D1CD
Confetti,                  Green,  DDCB46
Congo Brown,               Brown,  654D49
Conifer,                   Green,  B1DD52
Contessa,                  Red,    C16F68
Copper,                    Red,    DA8A67
Copper Canyon,             Orange, 77422C
Copper Rose,               Violet, 996666
Copper Rust,               Red,    95524C
Coral,                     Orange, FF7F50, true
Coral Candy,               Red,    F5D0C9
Coral Red,                 Red,    FF4040
Coral Tree,                Red,    AB6E67
Corduroy,                  Green,  404D49
Coriander,                 Green,  BBB58D
Cork,                      Brown,  5A4C42
Corn,                      Yellow, FBEC5D
Corn Field,                Green,  F8F3C4
Corn Flower Blue,          Blue,   42426F
Corn Harvest,              Yellow, 8D702A
Corn Silk,                 Yellow, FFF8DC, true
Cornflower,                Blue,   93CCEA
Cornflower Blue,           Blue,   6495ED, true
Corvette,                  Orange, E9BA81
Cosmic,                    Violet, 794D60
Cosmic Latte,              White,  E1F8E7
Cosmos,                    Red,    FCD5CF
Costa Del Sol,             Green,  625D2A
Cotton Candy,              Red,    FFB7D5
Cotton Seed,               Yellow, BFBAAF
County Green,              Green,  1B4B35
Cowboy,                    Brown,  443736
Crab Apple,                Red,    87382F
Crail,                     Red,    A65648
Cranberry,                 Red,    DB5079
Crater Brown,              Brown,  4D3E3C
Cream,                     White,  FFFDD0
Cream Brulee,              Yellow, FFE39B
Cream Can,                 Yellow, EEC051
Creole,                    Brown,  393227
Crete,                     Green,  77712B
Crimson,                   Red,    DC143C, true
Crocodile,                 Yellow, 706950
Crown Of Thorns,           Red,    763C33
Cruise,                    Green,  B4E2D5
Crusoe,                    Green,  165B31
Crusta,                    Orange, F38653
Cumin,                     Orange, 784430
Cumulus,                   Green,  F5F4C1
Cupid,                     Red,    F5B2C5
Curious Blue,              Blue,   3D85B8
Cutty Sark,                Green,  5C8173
Cyprus,                    Green,  0F4645
Dairy Cream,               Yellow, EDD2A4
Daisy Bush,                Violet, 5B3E90
Dallas,                    Brown,  664A2D
Dandelion,                 Yellow, FED85D
Danube,                    Blue,   5B89C0
Dark Blue,                 Blue,   00008B, true
Dark Brown,                Brown,  654321
Dark Cerulean,             Blue,   08457E
Dark Chestnut,             Red,    986960
Dark Coral,                Orange, CD5B45
Dark Cyan,                 Green,  008B8B, true
Dark Goldenrod,            Yellow, B8860B
Dark Gray,                 Gray,   A9A9A9, true
Dark Green,                Green,  013220
Dark Green Copper,         Green,  4A766E
Dark Khaki,                Yellow, BDB76B, true
Dark Magenta,              Violet, 8B008B, true
Dark Olive Green,          Green,  556B2F, true
Dark Orange,               Orange, FF8C00, true
Dark Orchid,               Violet, 9932CC, true
Dark Pastel Green,         Green,  03C03C
Dark Pink,                 Red,    E75480
Dark Purple,               Violet, 871F78
Dark Red,                  Red,    8B0000, true
Dark Rum,                  Brown,  45362B
Dark Salmon,               Orange, E9967A, true
Dark Sea Green,            Green,  8FBC8F, true
Dark Slate,                Green,  465352
Dark Slate Blue,           Blue,   483D8B, true
Dark Slate Gray,           Gray,   2F4F4F, true
Dark Spring Green,         Green,  177245
Dark Tan,                  Brown,  97694F
Dark Tangerine,            Orange, FFA812
Dark Turquoise,            Blue,   00CED1, true
Dark Violet,               Violet, 9400D3, true
Dark Wood,                 Brown,  855E42
Davy's Gray,               Gray,   788878
Dawn,                      Green,  9F9D91
Dawn Pink,                 Orange, E6D6CD
De York,                   Green,  85CA87
Deco,                      Green,  CCCF82
Deep Blush,                Red,    E36F8A
Deep Bronze,               Brown,  51412D
Deep Cerise,               Violet, DA3287
Deep Fir,                  Green,  193925
Deep Koamaru,              Violet, 343467
Deep Lilac,                Violet, 9955BB
Deep Magenta,              Violet, CC00CC
Deep Pink,                 Red,    FF1493, true
Deep Sea,                  Green,  167E65
Deep Sky Blue,             Blue,   00BFFF, true
Deep Teal,                 Green,  19443C
Del Rio,                   Brown,  B5998E
Dell,                      Green,  486531
Delta,                     Gray,   999B95
Deluge,                    Violet, 8272A4
Denim,                     Blue,   1560BD
Derby,                     Yellow, F9E4C6
Desert,                    Orange, A15F3B
Desert Sand,               Brown,  EDC9AF
Desert Storm,              Gray,   EDE7E0
Dew,                       Green,  E7F2E9
Diesel,                    Gray,   322C2B
Dim Gray,                  Gray,   696969, true
Dingley,                   Green,  607C47
Disco,                     Red,    892D4F
Dixie,                     Yellow, CD8431
Dodger Blue,               Blue,   1E90FF, true
Dolly,                     Green,  F5F171
Dolphin,                   Violet, 6A6873
Domino,                    Brown,  6C5B4C
Don Juan,                  Brown,  5A4F51
Donkey Brown,              Brown,  816E5C
Dorado,                    Brown,  6E5F56
Double Colonial White,     Yellow, E4CF99
Double Pearl Lusta,        Yellow, E9DCBE
Double Spanish White,      Yellow, D2C3A3
Dove Gray,                 Gray,   777672
Downy,                     Green,  6FD2BE
Drover,                    Yellow, FBEB9B
Dune,                      Gray,   514F4A
Dust Storm,                Orange, E5CAC0
Dusty Gray,                Gray,   AC9B9B
Dutch White,               Yellow, F0DFBB
Eagle,                     Green,  B0AC94
Earls Green,               Green,  B8A722
Early Dawn,                Yellow, FBF2DB
East Bay,                  Blue,   47526E
East Side,                 Violet, AA8CBC
Eastern Blue,              Blue,   00879F
Ebb,                       Red,    E6D8D4
Ebony,                     Gray,   313337
Ebony Clay,                Gray,   323438
Echo Blue,                 Blue,   A4AFCD
Eclipse,                   Gray,   3F3939
Ecru,                      Brown,  C2B280
Ecru White,                Green,  D6D1C0
Ecstasy,                   Orange, C96138
Eden,                      Green,  266255
Edgewater,                 Green,  C1D8C5
Edward,                    Green,  97A49A
Egg Sour,                  Yellow, F9E4C5
Eggplant,                  Violet, 990066
Egyptian Blue,             Blue,   1034A6
El Paso,                   Green,  39392C
El Salva,                  Red,    8F4E45
Electric Blue,             Blue,   7DF9FF
Electric Indigo,           Violet, 6600FF
Electric Lime,             Green,  CCFF00
Electric Purple,           Violet, BF00FF
Elephant,                  Blue,   243640
Elf Green,                 Green,  1B8A6B
Elm,                       Green,  297B76
Emerald,                   Green,  50C878
Eminence,                  Violet, 6E3974
Emperor,                   Gray,   50494A
Empress,                   Gray,   7C7173
Endeavour,                 Blue,   29598B
Energy Yellow,             Yellow, F5D752
English Holly,             Green,  274234
Envy,                      Green,  8BA58F
Equator,                   Yellow, DAB160
Espresso,                  Red,    4E312D
Eternity,                  Green,  2D2F28
Eucalyptus,                Green,  329760
Eunry,                     Red,    CDA59C
Evening Sea,               Green,  26604F
Everglade,                 Green,  264334
Fair Pink,                 Orange, F3E5DC
Falcon,                    Brown,  6E5A5B
Fallow,                    Brown,  C19A6B
Falu Red,                  Red,    801818
Fantasy,                   Orange, F2E6DD
Fedora,                    Violet, 625665
Feijoa,                    Green,  A5D785
Feldgrau,                  Gray,   4D5D53
Feldspar,                  Red,    D19275
Fern,                      Green,  63B76C
Fern Green,                Green,  4F7942
Ferra,                     Brown,  876A68
Festival,                  Yellow, EACC4A
Feta,                      Green,  DBE0D0
Fiery Orange,              Orange, B1592F
Fiji Green,                Green,  636F22
Finch,                     Green,  75785A
Finlandia,                 Green,  61755B
Finn,                      Violet, 694554
Fiord,                     Blue,   4B5A62
Fire,                      Orange, 8F3F2A
Fire Brick,                Red,    B22222, true
Fire Bush,                 Yellow, E09842
Fire Engine Red,           Red,    CE1620
Firefly,                   Green,  314643
Flame Pea,                 Orange, BE5C48
Flame Red,                 Red,    86282E
Flamenco,                  Orange, EA8645
Flamingo,                  Orange, E1634F
Flax,                      Yellow, EEDC82
Flint,                     Green,  716E61
Flirt,                     Red,    7A2E4D
Floral White,              White,  FFFAF0, true
Foam,                      Green,  D0EAE8
Fog,                       Violet, D5C7E8
Foggy Gray,                Gray,   A7A69D
Forest Green,              Green,  228B22, true
Forget Me Not,             Yellow, FDEFDB
Fountain Blue,             Blue,   65ADB2
Frangipani,                Yellow, FFD7A0
Free Speech Aquamarine,    Green,  029D74
Free Speech Blue,          Blue,   4156C5
Free Speech Green,         Green,  09F911
Free Speech Magenta,       Red,    E35BD8
Free Speech Red,           Red,    C00000
French Gray,               Gray,   BFBDC1
French Lilac,              Violet, DEB7D9
French Pass,               Blue,   A4D2E0
French Rose,               Red,    F64A8A
Friar Gray,                Gray,   86837A
Fringy Flower,             Green,  B4E1BB
Froly,                     Red,    E56D75
Frost,                     Green,  E1E4C5
Frosted Mint,              Green,  E2F2E4
Frostee,                   Green,  DBE5D2
Fruit Salad,               Green,  4BA351
Fuchsia,                   Violet, C154C1
Fuchsia Pink,              Red,    FF77FF
Fuego,                     Green,  C2D62E
Fuel Yellow,               Yellow, D19033
Fun Blue,                  Blue,   335083
Fun Green,                 Green,  15633D
Fuscous Gray,              Gray,   3C3B3C
Fuzzy Wuzzy Brown,         Brown,  C45655
Gable Green,               Green,  2C4641
Gainsboro,                 White,  DCDCDC, true
Gallery,                   Gray,   DCD7D1
Galliano,                  Yellow, D8A723
Gamboge,                   Yellow, E49B0F
Geebung,                   Yellow, C5832E
Genoa,                     Green,  31796D
Geraldine,                 Red,    E77B75
Geyser,                    Gray,   CBD0CF
Ghost,                     Blue,   C0BFC7
Ghost White,               White,  F8F8FF, true
Gigas,                     Violet, 564786
Gimblet,                   Green,  B9AD61
Gin,                       Green,  D9DFCD
Gin Fizz,                  Yellow, F8EACA
Givry,                     Yellow, EBD4AE
Glacier,                   Blue,   78B1BF
Glade Green,               Green,  5F8151
Go Ben,                    Yellow, 786E4C
Goblin,                    Green,  34533D
Gold,                      Yellow, FFD700, true
Gold Drop,                 Orange, D56C30
Gold Tips,                 Yellow, E2B227
Golden Bell,               Orange, CA8136
Golden Brown,              Brown,  996515
Golden Dream,              Yellow, F1CC2B
Golden Fizz,               Green,  EBDE31
Golden Glow,               Yellow, F9D77E
Golden Poppy,              Yellow, FCC200
Golden Sand,               Yellow, EACE6A
Golden Tainoi,             Yellow, FFC152
Golden Yellow,             Yellow, FFDF00
Goldenrod,                 Yellow, DBDB70
Gondola,                   Gray,   373332
Gordons Green,             Green,  29332B
Gorse,                     Green,  FDE336
Gossamer,                  Green,  399F86
Gossip,                    Green,  9FD385
Gothic,                    Blue,   698890
Governor Bay,              Blue,   51559B
Grain Brown,               Yellow, CAB8A2
Grandis,                   Yellow, FFCD73
Granite Green,             Yellow, 8B8265
Granny Apple,              Green,  C5E7CD
Granny Smith,              Green,  7B948C
Granny Smith Apple,        Green,  9DE093
Grape,                     Violet, 413D4B
Graphite,                  Yellow, 383428
Gravel,                    Gray,   4A4B46
Green,                     Green,  008000, true
Green House,               Green,  3E6334
Green Kelp,                Green,  393D2A
Green Leaf,                Green,  526B2D
Green Mist,                Green,  BFC298
Green Pea,                 Green,  266242
Green Smoke,               Green,  9CA664
Green Spring,              Green,  A9AF99
Green Vogue,               Blue,   23414E
Green Waterloo,            Green,  2C2D24
Green White,               Green,  DEDDCB
Green Yellow,              Green,  ADFF2F, true
Grenadier,                 Orange, C14D36
Gray,                      Gray,   808080, true
Gray Chateau,              Gray,   9FA3A7
Gray Nickel,               Green,  BDBAAE
Gray Nurse,                Gray,   D1D3CC
Gray Olive,                Yellow, A19A7F
Gray Suit,                 Blue,   9391A0
Gray-Asparagus,            Green,  465945
Guardsman Red,             Red,    952E31
Gulf Blue,                 Blue,   343F5C
Gulf Stream,               Green,  74B2A8
Gull Gray,                 Gray,   A4ADB0
Gum Leaf,                  Green,  ACC9B2
Gumbo,                     Green,  718F8A
Gun Powder,                Violet, 484753
Gunmetal,                  Blue,   2C3539
Gunsmoke,                  Gray,   7A7C76
Gurkha,                    Green,  989171
Hacienda,                  Yellow, 9E8022
Hairy Heath,               Brown,  633528
Haiti,                     Violet, 2C2A35
Half And Half,             Green,  EDE7C8
Half Baked,                Blue,   558F93
Half Colonial White,       Yellow, F2E5BF
Half Dutch White,          Yellow, FBF0D6
Half Pearl Lusta,          Yellow, F1EAD7
Half Spanish White,        Yellow, E6DBC7
Hampton,                   Yellow, E8D4A2
Han Purple,                Violet, 5218FA
Harlequin,                 Green,  3FFF00
Harley Davidson Orange,    Orange, C93413
Harp,                      Green,  CBCEC0
Harvest Gold,              Yellow, EAB76A
Havana,                    Brown,  3B2B2C
Havelock Blue,             Blue,   5784C1
Hawaiian Tan,              Orange, 99522B
Hawkes Blue,               Blue,   D2DAED
Heath,                     Red,    4F2A2C
Heather,                   Blue,   AEBBC1
Heathered Gray,            Brown,  948C7E
Heavy Metal,               Gray,   46473E
Heliotrope,                Violet, DF73FF
Hemlock,                   Yellow, 69684B
Hemp,                      Brown,  987D73
Highball,                  Green,  928C3C
Highland,                  Green,  7A9461
Hillary,                   Green,  A7A07E
Himalaya,                  Yellow, 736330
Hint Of Green,             Green,  DFF1D6
Hint Of Red,               Gray,   F5EFEB
Hint Of Yellow,            Green,  F6F5D7
Hippie Blue,               Blue,   49889A
Hippie Green,              Green,  608A5A
Hippie Pink,               Red,    AB495C
Hit Gray,                  Gray,   A1A9A8
Hit Pink,                  Orange, FDA470
Hokey Pokey,               Yellow, BB8E34
Hoki,                      Blue,   647D86
Holly,                     Green,  25342B
Hollywood Cerise,          Red,    F400A1
Honey Flower,              Violet, 5C3C6D
Honeydew,                  White,  F0FFF0, true
Honeysuckle,               Green,  E8ED69
Hopbush,                   Violet, CD6D93
Horizon,                   Blue,   648894
Horses Neck,               Yellow, 6D562C
Hot Curry,                 Yellow, 815B28
Hot Magenta,               Red,    FF00CC
Hot Pink,                  Red,    FF69B4, true
Hot Purple,                Violet, 4E2E53
Hot Toddy,                 Yellow, A7752C
Humming Bird,              Green,  CEEFE4
Hunter Green,              Green,  355E3B
Hurricane,                 Brown,  8B7E77
Husk,                      Yellow, B2994B
Ice Cold,                  Green,  AFE3D6
Iceberg,                   Green,  CAE1D9
Illusion,                  Red,    EF95AE
Inch Worm,                 Green,  B0E313
Indian Red,                Red,    CD5C5C, true
Indian Tan,                Brown,  4F301F
Indigo,                    Violet, 4B0082, true
Indochine,                 Orange, 9C5B34
International Klein Blue,  Blue,   002FA7
International Orange,      Orange, FF4F00
Iris Blue,                 Blue,   03B4C8
Irish Coffee,              Brown,  62422B
Iron,                      Gray,   CBCDCD
Ironside Gray,             Gray,   706E66
Ironstone,                 Brown,  865040
Islamic Green,             Green,  009900
Island Spice,              Yellow, F8EDDB
Ivory,                     White,  FFFFF0, true
Jacarta,                   Violet, 3D325D
Jacko Bean,                Brown,  413628
Jacksons Purple,           Violet, 3D3F7D
Jade,                      Green,  00A86B
Jaffa,                     Orange, E27945
Jagged Ice,                Green,  CAE7E2
Jagger,                    Violet, 3F2E4C
Jaguar,                    Blue,   29292F
Jambalaya,                 Brown,  674834
Japanese Laurel,           Green,  2F7532
Japonica,                  Orange, CE7259
Java,                      Green,  259797
Jazz,                      Red,    5F2C2F
Jazzberry Jam,             Red,    A50B5E
Jelly Bean,                Blue,   44798E
Jet Stream,                Green,  BBD0C9
Jewel,                     Green,  136843
Jon,                       Gray,   463D3E
Jonquil,                   Green,  EEF293
Jordy Blue,                Blue,   7AAAE0
Judge Gray,                Brown,  5D5346
Jumbo,                     Gray,   878785
Jungle Green,              Green,  29AB87
Jungle Mist,               Green,  B0C4C4
Juniper,                   Green,  74918E
Just Right,                Orange, DCBFAC
Kabul,                     Brown,  6C5E53
Kaitoke Green,             Green,  245336
Kangaroo,                  Green,  C5C3B0
Karaka,                    Green,  2D2D24
Karry,                     Orange, FEDCC1
Kashmir Blue,              Blue,   576D8E
Kelly Green,               Green,  4CBB17
Kelp,                      Green,  4D503C
Kenyan Copper,             Red,    6C322E
Keppel,                    Green,  5FB69C
Khaki,                     Yellow, F0E68C, true
Kidnapper,                 Green,  BFC0AB
Kilamanjaro,               Gray,   3A3532
Killarney,                 Green,  49764F
Kimberly,                  Violet, 695D87
Kingfisher Daisy,          Violet, 583580
Kobi,                      Red,    E093AB
Kokoda,                    Green,  7B785A
Korma,                     Orange, 804E2C
Koromiko,                  Yellow, FEB552
Kournikova,                Yellow, F9D054
La Palma,                  Green,  428929
La Rioja,                  Green,  BAC00E
Las Palmas,                Green,  C6DA36
Laser,                     Yellow, C6A95E
Laser Lemon,               Yellow, FFFF66
Laurel,                    Green,  6E8D71
Lavender,                  Violet, E6E6FA, true
Lavender Blue,             Blue,   CCCCFF
Lavender Blush,            Violet, FFF0F5, true
Lavender Gray,             Gray,   BDBBD7
Lavender Pink,             Red,    FBAED2
Lavender Rose,             Red,    FBA0E3
Lawn Green,                Green,  7CFC00, true
Leather,                   Brown,  906A54
Lemon,                     Yellow, FDE910
Lemon Chiffon,             Yellow, FFFACD, true
Lemon Ginger,              Yellow, 968428
Lemon Grass,               Green,  999A86
Licorice,                  Blue,   2E3749
Light Blue,                Blue,   ADD8E6, true
Light Coral,               Orange, F08080, true
Light Cyan,                Blue,   E0FFFF, true
Light Goldenrod,           Yellow, EEDD82
Light Goldenrod Yellow,    Yellow, FAFAD2, true
Light Green,               Green,  90EE90, true
Light Gray,                Gray,   D3D3D3, true
Light Pink,                Red,    FFB6C1, true
Light Salmon,              Orange, FFA07A, true
Light Sea Green,           Green,  20B2AA, true
Light Sky Blue,            Blue,   87CEFA, true
Light Slate Blue,          Blue,   8470FF
Light Slate Gray,          Gray,   778899, true
Light Steel Blue,          Blue,   B0C4DE, true
Light Wood,                Brown,  856363
Light Yellow,              Yellow, FFFFE0, true
Lightning Yellow,          Yellow, F7A233
Lilac,                     Violet, C8A2C8
Lilac Bush,                Violet, 9470C4
Lily,                      Violet, C19FB3
Lily White,                Gray,   E9EEEB
Lima,                      Green,  7AAC21
Lime,                      Green,  00FF00, true
Lime Green,                Green,  32CD32, true
Limeade,                   Green,  5F9727
Limerick,                  Green,  89AC27
Linen,                     White,  FAF0E6, true
Link Water,                Blue,   C7CDD8
Lipstick,                  Red,    962C54
Liver,                     Brown,  534B4F
Livid Brown,               Brown,  312A29
Loafer,                    Green,  DBD9C2
Loblolly,                  Green,  B3BBB7
Lochinvar,                 Green,  489084
Lochmara,                  Blue,   316EA0
Locust,                    Green,  A2A580
Log Cabin,                 Green,  393E2E
Logan,                     Blue,   9D9CB4
Lola,                      Violet, B9ACBB
London Hue,                Violet, AE94AB
Lonestar,                  Red,    522426
Lotus,                     Brown,  8B504B
Loulou,                    Violet, 4C3347
Lucky,                     Green,  AB9A1C
Lucky Point,               Blue,   292D4F
Lunar Green,               Green,  4E5541
Lusty,                     Red,    782E2C
Luxor Gold,                Yellow, AB8D3F
Lynch,                     Blue,   697D89
Mabel,                     Blue,   CBE8E8
Macaroni And Cheese,       Orange, FFB97B
Madang,                    Green,  B7E3A8
Madison,                   Blue,   2D3C54
Madras,                    Brown,  473E23
Magenta,                   Violet, FF00FF, true
Magic Mint,                Green,  AAF0D1
Magnolia,                  White,  F8F4FF
Mahogany,                  Brown,  CA3435
Mai Tai,                   Orange, A56531
Maire,                     Yellow, 2A2922
Maize,                     Yellow, E3B982
Makara,                    Brown,  695F50
Mako,                      Gray,   505555
Malachite,                 Green,  0BDA51
Malachite Green,           Green,  97976F
Malibu,                    Blue,   66B7E1
Mallard,                   Green,  3A4531
Malta,                     Brown,  A59784
Mamba,                     Violet, 766D7C
Manatee,                   Blue,   8D90A1
Mandalay,                  Yellow, B57B2E
Mandarian Orange,          Orange, 8E2323
Mandy,                     Red,    CD525B
Mandys Pink,               Orange, F5B799
Mango Tango,               Orange, E77200
Manhattan,                 Orange, E2AF80
Mantis,                    Green,  7FC15C
Mantle,                    Green,  96A793
Manz,                      Green,  E4DB55
Mardi Gras,                Violet, 352235
Marigold,                  Yellow, B88A3D
Mariner,                   Blue,   42639F
Maroon,                    Brown,  800000, true
Marshland,                 Green,  2B2E26
Martini,                   Brown,  B7A8A3
Martinique,                Violet, 3C3748
Marzipan,                  Yellow, EBC881
Masala,                    Brown,  57534B
Matisse,                   Blue,   365C7D
Matrix,                    Red,    8E4D45
Matterhorn,                Gray,   524B4B
Mauve,                     Violet, E0B0FF
Mauve Taupe,               Red,    915F6D
Mauvelous,                 Red,    F091A9
Maverick,                  Violet, C8B1C0
Maya Blue,                 Blue,   73C2FB
McKenzie,                  Orange, 8C6338
Medium Aquamarine,         Blue,   66CDAA, true
Medium Blue,               Blue,   0000CD, true
Medium Carmine,            Red,    AF4035
Medium Goldenrod,          Yellow, EAEAAE
Medium Orchid,             Violet, BA55D3, true
Medium Purple,             Violet, 9370DB, true
Medium Sea Green,          Green,  3CB371, true
Medium Slate Blue,         Blue,   7B68EE, true
Medium Spring Green,       Green,  00FA9A, true
Medium Turquoise,          Blue,   48D1CC, true
Medium Violet Red,         Red,    C71585, true
Medium Wood,               Brown,  A68064
Melanie,                   Red,    E0B7C2
Melanzane,                 Violet, 342931
Melon,                     Red,    FEBAAD
Melrose,                   Violet, C3B9DD
Mercury,                   Gray,   D5D2D1
Merino,                    Yellow, E1DBD0
Merlin,                    Gray,   4F4E48
Merlot,                    Red,    73343A
Metallic Bronze,           Red,    554A3C
Metallic Copper,           Red,    6E3D34
Metallic Gold,             Yellow, D4AF37
Meteor,                    Orange, BB7431
Meteorite,                 Violet, 4A3B6A
Mexican Red,               Red,    9B3D3D
Mid Gray,                  Gray,   666A6D
Midnight,                  Blue,   21303E
Midnight Blue,             Blue,   191970, true
Midnight Express,          Blue,   21263A
Midnight Moss,             Green,  242E28
Mikado,                    Brown,  3F3623
Milan,                     Green,  F6F493
Milano Red,                Red,    9E3332
Milk Punch,                Yellow, F3E5C0
Milk White,                Gray,   DCD9CD
Millbrook,                 Brown,  595648
Mimosa,                    Green,  F5F5CC
Mindaro,                   Green,  DAEA6F
Mine Shaft,                Blue,   373E41
Mineral Green,             Green,  506355
Ming,                      Green,  407577
Minsk,                     Violet, 3E3267
Mint Cream,                White,  F5FFFA, true
Mint Green,                Green,  98FF98
Mint Julep,                Green,  E0D8A7
Mint Tulip,                Green,  C6EADD
Mirage,                    Blue,   373F43
Mischka,                   Blue,   A5A9B2
Mist Gray,                 Gray,   BAB9A9
Misty Rose,                Violet, FFE4E1, true
Mobster,                   Violet, 605A67
Moccaccino,                Red,    582F2B
Moccasin,                  Yellow, FFE4B5, true
Mocha,                     Red,    6F372D
Mojo,                      Red,    97463C
Mona Lisa,                 Red,    FF9889
Monarch,                   Red,    6B252C
Mondo,                     Brown,  554D42
Mongoose,                  Brown,  A58B6F
Monsoon,                   Gray,   7A7679
Montana,                   Gray,   393B3C
Monte Carlo,               Green,  7AC5B4
Moody Blue,                Violet, 8378C7
Moon Glow,                 Green,  F5F3CE
Moon Mist,                 Green,  CECDB8
Moon Raker,                Violet, C0B2D7
Moon Yellow,               Yellow, F0C420
Morning Glory,             Blue,   9ED1D3
Morocco Brown,             Brown,  442D21
Mortar,                    Gray,   565051
Mosque,                    Green,  005F5B
Moss Green,                Green,  ADDFAD
Mountain Meadow,           Green,  1AB385
Mountain Mist,             Gray,   A09F9C
Mountbatten Pink,          Violet, 997A8D
Muddy Waters,              Yellow, A9844F
Muesli,                    Brown,  9E7E53
Mulberry,                  Violet, C54B8C
Mule Fawn,                 Brown,  884F40
Mulled Wine,               Violet, 524D5B
Mustard,                   Yellow, FFDB58
My Pink,                   Red,    D68B80
My Sin,                    Yellow, FDAE45
Myrtle,                    Green,  21421E
Mystic,                    Gray,   D8DDDA
Nandor,                    Green,  4E5D4E
Napa,                      Yellow, A39A87
Narvik,                    Green,  E9E6DC
Navajo White,              Brown,  FFDEAD, true
Navy,                      Blue,   000080, true
Navy Blue,                 Blue,   0066CC
Nebula,                    Green,  B8C6BE
Negroni,                   Orange, EEC7A2
Neon Blue,                 Blue,   4D4DFF
Neon Carrot,               Orange, FF9933
Neon Pink,                 Violet, FF6EC7
Nepal,                     Blue,   93AAB9
Neptune,                   Green,  77A8AB
Nero,                      Gray,   252525
Neutral Green,             Green,  AAA583
Nevada,                    Gray,   666F6F
New Amber,                 Orange, 6D3B24
New Midnight Blue,         Blue,   00009C
New Orleans,               Yellow, E4C385
New Tan,                   Brown,  EBC79E
New York Pink,             Red,    DD8374
Niagara,                   Green,  29A98B
Night Rider,               Gray,   332E2E
Night Shadz,               Red,    A23D54
Nile Blue,                 Blue,   253F4E
Nobel,                     Gray,   A99D9D
Nomad,                     Yellow, A19986
Nordic,                    Blue,   1D393C
Norway,                    Green,  A4B88F
Nugget,                    Yellow, BC9229
Nutmeg,                    Brown,  7E4A3B
Oasis,                     Yellow, FCEDC5
Observatory,               Green,  008F70
Ocean Green,               Green,  4CA973
Ochre,                     Brown,  CC7722
Off Green,                 Green,  DFF0E2
Off Yellow,                Yellow, FAF3DC
Oil,                       Gray,   313330
Old Brick,                 Red,    8A3335
Old Copper,                Red,    73503B
Old Gold,                  Yellow, CFB53B
Old Lace,                  White,  FDF5E6, true
Old Lavender,              Violet, 796878
Old Rose,                  Red,    C02E4C
Olive,                     Green,  808000, true
Olive Drab,                Green,  6B8E23, true
Olive Green,               Green,  B5B35C
Olive Haze,                Yellow, 888064
Olivetone,                 Green,  747028
Olivine,                   Orange, 9AB973
Onahau,                    Blue,   C2E6EC
Onion,                     Yellow, 48412B
Opal,                      Green,  A8C3BC
Opium,                     Brown,  987E7E
Oracle,                    Green,  395555
Orange,                    Orange, FFA500, true
Orange Peel,               Orange, FFA000
Orange Red,                Orange, FF4500, true
Orange Roughy,             Orange, A85335
Orange White,              Yellow, EAE3CD
Orchid,                    Violet, DA70D6, true
Orchid White,              Yellow, F1EBD9
Orient,                    Blue,   255B77
Oriental Pink,             Red,    C28E88
Orinoco,                   Green,  D2D3B3
Oslo Gray,                 Gray,   818988
Ottoman,                   Green,  D3DBCB
Outer Space,               Gray,   2D383A
Outrageous Orange,         Orange, FF6037
Oxford Blue,               Blue,   28353A
Oxley,                     Green,  6D9A78
Oyster Bay,                Blue,   D1EAEA
Oyster Pink,               Red,    D4B5B0
Paarl,                     Orange, 864B36
Pablo,                     Yellow, 7A715C
Pacific Blue,              Blue,   009DC4
Paco,                      Brown,  4F4037
Padua,                     Green,  7EB394
Palatinate Purple,         Violet, 682860
Pale Brown,                Brown,  987654
Pale Chestnut,             Red,    DDADAF
Pale Cornflower Blue,      Blue,   ABCDEF
Pale Goldenrod,            Yellow, EEE8AA, true
Pale Green,                Green,  98FB98, true
Pale Leaf,                 Green,  BDCAA8
Pale Magenta,              Violet, F984E5
Pale Oyster,               Brown,  9C8D72
Pale Pink,                 Red,    FADADD
Pale Prim,                 Green,  F9F59F
Pale Rose,                 Red,    EFD6DA
Pale Sky,                  Blue,   636D70
Pale Slate,                Gray,   C3BEBB
Pale Taupe,                Gray,   BC987E
Pale Turquoise,            Blue,   AFEEEE, true
Pale Violet Red,           Red,    DB7093, true
Palm Green,                Green,  20392C
Palm Leaf,                 Green,  36482F
Pampas,                    Gray,   EAE4DC
Panache,                   Green,  EBF7E4
Pancho,                    Orange, DFB992
Panda,                     Yellow, 544F3A
Papaya Whip,               Yellow, FFEFD5, true
Paprika,                   Red,    7C2D37
Paradiso,                  Green,  488084
Parchment,                 Yellow, D0C8B0
Paris Daisy,               Green,  FBEB50
Paris M,                   Violet, 312760
Paris White,               Green,  BFCDC0
Parsley,                   Green,  305D35
Pastel Green,              Green,  77DD77
Patina,                    Green,  639283
Pattens Blue,              Blue,   D3E5EF
Paua,                      Violet, 2A2551
Pavlova,                   Yellow, BAAB87
Payne's Gray,              Gray,   404048
Peach,                     Orange, FFCBA4
Peach Puff,                Yellow, FFDAB9, true
Peach-Orange,              Orange, FFCC99
Peach-Yellow,              Yellow, FADFAD
Peanut,                    Brown,  7A4434
Pear,                      Yellow, D1E231
Pearl Bush,                Orange, DED1C6
Pearl Lusta,               Yellow, EAE0C8
Peat,                      Yellow, 766D52
Pelorous,                  Blue,   2599B2
Peppermint,                Green,  D7E7D0
Perano,                    Blue,   ACB9E8
Perfume,                   Violet, C2A9DB
Periglacial Blue,          Green,  ACB6B2
Periwinkle,                Blue,   C3CDE6
Persian Blue,              Blue,   1C39BB
Persian Green,             Green,  00A693
Persian Indigo,            Violet, 32127A
Persian Pink,              Red,    F77FBE
Persian Plum,              Red,    683332
Persian Red,               Red,    CC3333
Persian Rose,              Red,    FE28A2
Persimmon,                 Red,    EC5800
Peru,                      Brown,  CD853F, true
Peru Tan,                  Orange, 733D1F
Pesto,                     Yellow, 7A7229
Petite Orchid,             Red,    DA9790
Pewter,                    Green,  91A092
Pharlap,                   Brown,  826663
Picasso,                   Green,  F8EA97
Picton Blue,               Blue,   5BA0D0
Pig Pink,                  Red,    FDD7E4
Pigment Green,             Green,  00A550
Pine Cone,                 Brown,  756556
Pine Glade,                Green,  BDC07E
Pine Green,                Green,  01796F
Pine Tree,                 Green,  2A2F23
Pink,                      Red,    FFC0CB, true
Pink Flamingo,             Red,    FF66FF
Pink Flare,                Red,    D8B4B6
Pink Lace,                 Red,    F6CCD7
Pink Lady,                 Orange, F3D7B6
Pink Swan,                 Gray,   BFB3B2
Piper,                     Orange, 9D5432
Pipi,                      Yellow, F5E6C4
Pippin,                    Red,    FCDBD2
Pirate Gold,               Yellow, BA782A
Pixie Green,               Green,  BBCDA5
Pizazz,                    Orange, E57F3D
Pizza,                     Yellow, BF8D3C
Plantation,                Green,  3E594C
Plum,                      Violet, DDA0DD, true
Pohutukawa,                Red,    651C26
Polar,                     Green,  E5F2E7
Polo Blue,                 Blue,   8AA7CC
Pompadour,                 Violet, 6A1F44
Porcelain,                 Gray,   DDDCDB
Porsche,                   Orange, DF9D5B
Port Gore,                 Blue,   3B436C
Portafino,                 Green,  F4F09B
Portage,                   Blue,   8B98D8
Portica,                   Yellow, F0D555
Pot Pourri,                Orange, EFDCD4
Potters Clay,              Brown,  845C40
Powder Blue,               Blue,   B0E0E6, true
Prairie Sand,              Red,    883C32
Prelude,                   Violet, CAB4D4
Prim,                      Violet, E2CDD5
Primrose,                  Green,  E4DE8E
Promenade,                 Green,  F8F6DF
Provincial Pink,           Orange, F6E3DA
Prussian Blue,             Blue,   003366
Psychedelic Purple,        Violet, DD00FF
Puce,                      Red,    CC8899
Pueblo,                    Orange, 6E3326
Puerto Rico,               Green,  59BAA3
Pumice,                    Green,  BAC0B4
Pumpkin,                   Orange, FF7518
Punga,                     Yellow, 534931
Purple,                    Violet, 800080, true
Purple Heart,              Violet, 652DC1
Purple Mountain's Majesty, Violet, 9678B6
Purple Taupe,              Gray,   50404D
Putty,                     Yellow, CDAE70
Quarter Pearl Lusta,       Green,  F2EDDD
Quarter Spanish White,     Yellow, EBE2D2
Quartz,                    White,  D9D9F3
Quicksand,                 Brown,  C3988B
Quill Gray,                Gray,   CBC9C0
Quincy,                    Brown,  6A5445
Racing Green,              Green,  232F2C
Radical Red,               Red,    FF355E
Raffia,                    Yellow, DCC6A0
Rain Forest,               Green,  667028
Rainee,                    Green,  B3C1B1
Rajah,                     Orange, FCAE60
Rangoon Green,             Green,  2B2E25
Raven,                     Blue,   6F747B
Raw Sienna,                Brown,  D27D46
Raw Umber,                 Brown,  734A12
Razzle Dazzle Rose,        Red,    FF33CC
Razzmatazz,                Red,    E30B5C
Rebel,                     Brown,  453430
Red,                       Red,    FF0000, true
Red Berry,                 Red,    701F28
Red Damask,                Orange, CB6F4A
Red Devil,                 Red,    662A2C
Red Orange,                Orange, FF3F34
Red Oxide,                 Red,    5D1F1E
Red Robin,                 Red,    7D4138
Red Stage,                 Orange, AD522E
Medium Red Violet,         Violet, BB3385
Redwood,                   Red,    5B342E
Reef,                      Green,  D1EF9F
Reef Gold,                 Yellow, A98D36
Regal Blue,                Blue,   203F58
Regent Gray,               Blue,   798488
Regent St Blue,            Blue,   A0CDD9
Remy,                      Red,    F6DEDA
Reno Sand,                 Orange, B26E33
Resolution Blue,           Blue,   323F75
Revolver,                  Violet, 37363F
Rhino,                     Blue,   3D4653
Rice Cake,                 Green,  EFECDE
Rice Flower,               Green,  EFF5D1
Rich Blue,                 Blue,   5959AB
Rich Gold,                 Orange, A15226
Rio Grande,                Green,  B7C61A
Riptide,                   Green,  89D9C8
River Bed,                 Blue,   556061
Robin's Egg Blue,          Blue,   00CCCC
Rock,                      Brown,  5A4D41
Rock Blue,                 Blue,   93A2BA
Rock Spray,                Orange, 9D442D
Rodeo Dust,                Brown,  C7A384
Rolling Stone,             Green,  6D7876
Roman,                     Red,    D8625B
Roman Coffee,              Brown,  7D6757
Romance,                   Gray,   F4F0E6
Romantic,                  Orange, FFC69E
Ronchi,                    Yellow, EAB852
Roof Terracotta,           Red,    A14743
Rope,                      Orange, 8E593C
Rose,                      Red,    D3A194
Rose Bud,                  Red,    FEAB9A
Rose Bud Cherry,           Red,    8A2D52
Rose Of Sharon,            Orange, AC512D
Rose Taupe,                Violet, 905D5D
Rose White,                Red,    FBEEE8
Rosy Brown,                Brown,  BC8F8F, true
Roti,                      Yellow, B69642
Rouge,                     Red,    A94064
Royal Blue,                Blue,   4169E1, true
Royal Heath,               Red,    B54B73
Royal Purple,              Violet, 6B3FA0
Ruby,                      Red,    E0115F
Rum,                       Violet, 716675
Rum Swizzle,               Green,  F1EDD4
Russet,                    Brown,  80461B
Russett,                   Brown,  7D655C
Rust,                      Red,    B7410E
Rustic Red,                Red,    3A181A
Rusty Nail,                Orange, 8D5F2C
Saddle,                    Brown,  5D4E46
Saddle Brown,              Brown,  8B4513, true
Safety Orange,             Orange, FF6600
Saffron,                   Yellow, F4C430
Sage,                      Green,  989F7A
Sahara,                    Yellow, B79826
Sail,                      Blue,   A5CEEC
Salem,                     Green,  177B4D
Salmon,                    Red,    FA8072, true
Salomie,                   Yellow, FFD67B
Salt Box,                  Violet, 696268
Saltpan,                   Gray,   EEF3E5
Sambuca,                   Brown,  3B2E25
San Felix,                 Green,  2C6E31
San Juan,                  Blue,   445761
San Marino,                Blue,   4E6C9D
Sand Dune,                 Brown,  867665
Sandal,                    Brown,  A3876A
Sandrift,                  Brown,  AF937D
Sandstone,                 Brown,  786D5F
Sandwisp,                  Yellow, DECB81
Sandy Beach,               Orange, FEDBB7
Sandy Brown,               Brown,  F4A460, true
Sangria,                   Red,    92000A
Sanguine Brown,            Red,    6C3736
Santas Gray,               Blue,   9998A7
Sante Fe,                  Orange, A96A50
Sapling,                   Yellow, E1D5A6
Sapphire,                  Blue,   082567
Saratoga,                  Green,  555B2C
Sauvignon,                 Red,    F4EAE4
Sazerac,                   Orange, F5DEC4
Scampi,                    Violet, 6F63A0
Scandal,                   Green,  ADD9D1
Scarlet,                   Red,    FF2400
Scarlet Gum,               Violet, 4A2D57
Scarlett,                  Red,    7E2530
Scarpa Flow,               Gray,   6B6A6C
Schist,                    Green,  87876F
School Bus Yellow,         Yellow, FFD800
Schooner,                  Brown,  8D8478
Scooter,                   Blue,   308EA0
Scorpion,                  Gray,   6A6466
Scotch Mist,               Yellow, EEE7C8
Screamin' Green,           Green,  66FF66
Scrub,                     Green,  3D4031
Sea Buckthorn,             Orange, EF9548
Sea Fog,                   Gray,   DFDDD6
Sea Green,                 Green,  2E8B57, true
Sea Mist,                  Green,  C2D5C4
Sea Nymph,                 Green,  8AAEA4
Sea Pink,                  Red,    DB817E
Seagull,                   Blue,   77B7D0
Seal Brown,                Brown,  321414
Seance,                    Violet, 69326E
Seashell,                  White,  FFF5EE, true
Seaweed,                   Green,  37412A
Selago,                    Violet, E6DFE7
Selective Yellow,          Yellow, FFBA00
Semi-Sweet Chocolate,      Brown,  6B4226
Sepia,                     Brown,  9E5B40
Serenade,                  Orange, FCE9D7
Shadow,                    Green,  837050
Shadow Green,              Green,  9AC0B6
Shady Lady,                Gray,   9F9B9D
Shakespeare,               Blue,   609AB8
Shalimar,                  Green,  F8F6A8
Shamrock,                  Green,  33CC99
Shamrock Green,            Green,  009E60
Shark,                     Gray,   34363A
Sherpa Blue,               Green,  00494E
Sherwood Green,            Green,  1B4636
Shilo,                     Red,    E6B2A6
Shingle Fawn,              Brown,  745937
Ship Cove,                 Blue,   7988AB
Ship Gray,                 Gray,   4E4E4C
Shiraz,                    Red,    842833
Shocking,                  Violet, E899BE
Shocking Pink,             Red,    FC0FC0
Shuttle Gray,              Gray,   61666B
Siam,                      Green,  686B50
Sidecar,                   Yellow, E9D9A9
Sienna,                    Brown,  A0522D, true
Silk,                      Brown,  BBADA1
Silver,                    Gray,   C0C0C0, true
Silver Chalice,            Gray,   ACAEA9
Silver Sand,               Gray,   BEBDB6
Silver Tree,               Green,  67BE90
Sinbad,                    Green,  A6D5D0
Siren,                     Red,    69293B
Sirocco,                   Green,  68766E
Sisal,                     Yellow, C5BAA0
Skeptic,                   Green,  9DB4AA
Sky Blue,                  Blue,   87CEEB, true
Slate Blue,                Blue,   6A5ACD, true
Slate Gray,                Gray,   708090, true
Slugger,                   Brown,  42342B
Smalt,                     Blue,   003399
Smalt Blue,                Blue,   496267
Smoke Tree,                Orange, BB5F34
Smoky,                     Violet, 605D6B
Snow,                      White,  FFFAFA, true
Snow Drift,                Gray,   E3E3DC
Snow Flurry,               Green,  EAF7C9
Snowy Mint,                Green,  D6F0CD
Snuff,                     Violet, E4D7E5
Soapstone,                 Gray,   ECE5DA
Soft Amber,                Yellow, CFBEA5
Soft Peach,                Red,    EEDFDE
Solid Pink,                Red,    85494C
Solitaire,                 Yellow, EADAC2
Solitude,                  Blue,   E9ECF1
Sorbus,                    Orange, DD6B38
Sorrell Brown,             Brown,  9D7F61
Sour Dough,                Brown,  C9B59A
Soya Bean,                 Brown,  6F634B
Space Shuttle,             Brown,  4B433B
Spanish Green,             Green,  7B8976
Spanish White,             Yellow, DED1B7
Spectra,                   Green,  375D4F
Spice,                     Brown,  6C4F3F
Spicy Mix,                 Brown,  8B5F4D
Spicy Pink,                Red,    FF1CAE
Spindle,                   Blue,   B3C4D8
Splash,                    Yellow, F1D79E
Spray,                     Blue,   7ECDDD
Spring Bud,                Green,  A7FC00
Spring Green,              Green,  00FF7F, true
Spring Rain,               Green,  A3BD9C
Spring Sun,                Green,  F1F1C6
Spring Wood,               Gray,   E9E1D9
Sprout,                    Green,  B8CA9D
Spun Pearl,                Blue,   A2A1AC
Squirrel,                  Brown,  8F7D6B
St Tropaz,                 Blue,   325482
Stack,                     Gray,   858885
Star Dust,                 Gray,   A0A197
Stark White,               Yellow, D2C6B6
Starship,                  Green,  E3DD39
Steel Blue,                Blue,   4682B4, true
Steel Gray,                Gray,   43464B
Stiletto,                  Red,    833D3E
Stonewall,                 Yellow, 807661
Storm Dust,                Gray,   65645F
Storm Gray,                Blue,   747880
Straw,                     Yellow, DABE82
Strikemaster,              Violet, 946A81
Stromboli,                 Green,  406356
Studio,                    Violet, 724AA1
Submarine,                 Blue,   8C9C9C
Sugar Cane,                Green,  EEEFDF
Sulu,                      Green,  C6EA80
Summer Green,              Green,  8FB69C
Summer Sky,                Blue,   38B0DE
Sun,                       Orange, EF8E38
Sundance,                  Yellow, C4AA4D
Sundown,                   Red,    F8AFA9
Sunflower,                 Yellow, DAC01A
Sunglo,                    Red,    C76155
Sunglow,                   Orange, FFCC33
Sunset,                    Red,    C0514A
Sunset Orange,             Orange, FE4C40
Sunshade,                  Orange, FA9D49
Supernova,                 Yellow, FFB437
Surf,                      Green,  B8D4BB
Surf Crest,                Green,  C3D6BD
Surfie Green,              Green,  007B77
Sushi,                     Green,  7C9F2F
Suva Gray,                 Gray,   8B8685
Swamp,                     Green,  252F2F
Swans Down,                Gray,   DAE6DD
Sweet Corn,                Yellow, F9E176
Sweet Pink,                Red,    EE918D
Swirl,                     Gray,   D7CEC5
Swiss Coffee,              Gray,   DBD0CA
Tacao,                     Orange, F6AE78
Tacha,                     Yellow, D2B960
Tahiti Gold,               Orange, DC722A
Tahuna Sands,              Yellow, D8CC9B
Tall Poppy,                Red,    853534
Tallow,                    Yellow, A39977
Tamarillo,                 Red,    752B2F
Tan,                       Brown,  D2B48C, true
Tana,                      Green,  B8B5A1
Tangaroa,                  Blue,   1E2F3C
Tangerine,                 Orange, F28500
Tangerine Yellow,          Yellow, FFCC00
Tango,                     Orange, D46F31
Tapa,                      Green,  7C7C72
Tapestry,                  Red,    B37084
Tara,                      Green,  DEF1DD
Tarawera,                  Blue,   253C48
Tasman,                    Gray,   BAC0B3
Taupe,                     Gray,   483C32
Taupe Gray,                Gray,   8B8589
Tawny Port,                Red,    643A48
Tax Break,                 Blue,   496569
Te Papa Green,             Green,  2B4B40
Tea,                       Yellow, BFB5A2
Tea Green,                 Green,  D0F0C0
Tea Rose,                  Orange, F883C2
Teak,                      Yellow, AB8953
Teal,                      Blue,   008080, true
Teal Blue,                 Blue,   254855
Temptress,                 Brown,  3C2126
Tenne,                     Orange, CD5700
Tequila,                   Yellow, F4D0A4
Terra Cotta,               Red,    E2725B
Texas,                     Green,  ECE67E
Texas Rose,                Orange, FCB057
Thatch,                    Brown,  B1948F
Thatch Green,              Yellow, 544E31
Thistle,                   Violet, D8BFD8, true
Thunder,                   Gray,   4D4D4B
Thunderbird,               Red,    923830
Tia Maria,                 Orange, 97422D
Tiara,                     Gray,   B9C3BE
Tiber,                     Green,  184343
Tickle Me Pink,            Red,    FC80A5
Tidal,                     Green,  F0F590
Tide,                      Brown,  BEB4AB
Timber Green,              Green,  324336
Timberwolf,                Gray,   D9D6CF
Titan White,               Violet, DDD6E1
Toast,                     Brown,  9F715F
Tobacco Brown,             Brown,  6D5843
Tobago,                    Brown,  44362D
Toledo,                    Violet, 3E2631
Tolopea,                   Violet, 2D2541
Tom Thumb,                 Green,  4F6348
Tomato,                    Red,    FF6347, true
Tonys Pink,                Orange, E79E88
Topaz,                     Violet, 817C87
Torch Red,                 Red,    FD0E35
Torea Bay,                 Blue,   353D75
Tory Blue,                 Blue,   374E88
Tosca,                     Red,    744042
Tower Gray,                Green,  9CACA5
Tradewind,                 Green,  6DAFA7
Tranquil,                  Blue,   DDEDE9
Travertine,                Green,  E2DDC7
Tree Poppy,                Orange, E2813B
Trendy Green,              Green,  7E8424
Trendy Pink,               Violet, 805D80
Trinidad,                  Orange, C54F33
Tropical Blue,             Blue,   AEC9EB
Tropical Rain Forest,      Green,  00755E
Trout,                     Gray,   4C5356
True V,                    Violet, 8E72C7
Tuatara,                   Gray,   454642
Tuft Bush,                 Orange, F9D3BE
Tulip Tree,                Yellow, E3AC3D
Tumbleweed,                Brown,  DEA681
Tuna,                      Gray,   46494E
Tundora,                   Gray,   585452
Turbo,                     Yellow, F5CC23
Turkish Rose,              Red,    A56E75
Turmeric,                  Yellow, AE9041
Turquoise,                 Blue,   40E0D0, true
Turquoise Blue,            Blue,   6CDAE7
Turtle Green,              Green,  363E1D
Tuscany,                   Orange, AD6242
Tusk,                      Green,  E3E5B1
Tussock,                   Yellow, BF914B
Tutu,                      Red,    F8E4E3
Twilight,                  Violet, DAC0CD
Twilight Blue,             Gray,   F4F6EC
Twine,                     Yellow, C19156
Tyrian Purple,             Violet, 66023C
Ultra Pink,                Red,    FF6FFF
Ultramarine,               Blue,   120A8F
Valencia,                  Red,    D4574E
Valentino,                 Violet, 382C38
Valhalla,                  Violet, 2A2B41
Van Cleef,                 Brown,  523936
Vanilla,                   Brown,  CCB69B
Vanilla Ice,               Red,    EBD2D1
Varden,                    Yellow, FDEFD3
Venetian Red,              Red,    C80815
Venice Blue,               Blue,   2C5778
Venus,                     Violet, 8B7D82
Verdigris,                 Gray,   62603E
Verdun Green,              Green,  48531A
Vermilion,                 Red,    FF4D00
Very Dark Brown,           Brown,  5C4033
Very Light Gray,           Gray,   CDCDCD
Vesuvius,                  Orange, A85533
Victoria,                  Violet, 564985
Vida Loca,                 Green,  5F9228
Viking,                    Blue,   4DB1C8
Vin Rouge,                 Red,    955264
Viola,                     Red,    C58F9D
Violent Violet,            Violet, 2E2249
Violet,                    Violet, EE82EE, true
Violet Blue,               Violet, 9F5F9F
Violet Red,                Red,    F7468A
Viridian,                  Blue,   40826D
Viridian Green,            Green,  4B5F56
Vis Vis,                   Yellow, F9E496
Vista Blue,                Green,  97D5B3
Vista White,               Gray,   E3DFD9
Vivid Tangerine,           Orange, FF9980
Vivid Violet,              Violet, 803790
Volcano,                   Red,    4E2728
Voodoo,                    Violet, 443240
Vulcan,                    Gray,   36383C
Wafer,                     Orange, D4BBB1
Waikawa Gray,              Blue,   5B6E91
Waiouru,                   Green,  4C4E31
Wan White,                 Gray,   E4E2DC
Wasabi,                    Green,  849137
Water Leaf,                Green,  B6ECDE
Watercourse,               Green,  006E4E
Wattle,                    Green,  D6CA3D
Watusi,                    Orange, F2CDBB
Wax Flower,                Orange, EEB39E
We Peep,                   Red,    FDD7D8
Wedgewood,                 Blue,   4C6B88
Well Read,                 Red,    8E3537
West Coast,                Yellow, 5C512F
West Side,                 Orange, E5823A
Westar,                    Gray,   D4CFC5
Wewak,                     Red,    F1919A
Wheat,                     Brown,  F5DEB3, true
Wheatfield,                Yellow, DFD7BD
Whiskey,                   Orange, D29062
Whiskey Sour,              Orange, D4915D
Whisper,                   Gray,   EFE6E6
White,                     White,  FFFFFF, true
White Ice,                 Green,  D7EEE4
White Lilac,               Blue,   E7E5E8
White Linen,               Gray,   EEE7DC
White Nectar,              Green,  F8F6D8
White Pointer,             Gray,   DAD6CC
White Rock,                Green,  D4CFB4
White Smoke,               White,  F5F5F5, true
Wild Blue Yonder,          Blue,   7A89B8
Wild Rice,                 Green,  E3D474
Wild Sand,                 Gray,   E7E4DE
Wild Strawberry,           Red,    FF3399
Wild Watermelon,           Red,    FD5B78
Wild Willow,               Green,  BECA60
William,                   Green,  53736F
Willow Brook,              Green,  DFE6CF
Willow Grove,              Green,  69755C
Windsor,                   Violet, 462C77
Wine Berry,                Red,    522C35
Winter Hazel,              Yellow, D0C383
Wisp Pink,                 Red,    F9E8E2
Wisteria,                  Violet, C9A0DC
Wistful,                   Blue,   A29ECD
Witch Haze,                Green,  FBF073
Wood Bark,                 Brown,  302621
Woodburn,                  Brown,  463629
Woodland,                  Green,  626746
Woodrush,                  Yellow, 45402B
Woodsmoke,                 Gray,   2B3230
Woody Brown,               Brown,  554545
Xanadu,                    Green,  75876E
Yellow,                    Yellow, FFFF00, true
Yellow Green,              Green,  9ACD32, true
Yellow Metal,              Yellow, 73633E
Yellow Orange,             Orange, FFAE42
Yellow Sea,                Yellow, F49F35
Your Pink,                 Red,    FFC5BB
Yukon Gold,                Yellow, 826A21
Yuma,                      Yellow, C7B882
Zambezi,                   Brown,  6B5A5A
Zanah,                     Green,  B2C6B1
Zest,                      Orange, C6723B
Zeus,                      Gray,   3B3C38
Ziggurat,                  Blue,   81A6AA
Zinnwaldite,               Brown,  EBC2AF
Zircon,                    Gray,   DEE3E3
Zombie,                    Yellow, DDC283
Zorba,                     Brown,  A29589
Zuccini,                   Green,  17462E
Zumthor,                   Gray,   CDD5D5";
            Regex namedColorRegex = new Regex(@"^(?<name>.*?),\s+(?<group>\w+),\s+(?<hex>[\da-fA-F]{6})(?<css>, true)?$", RegexOptions.Compiled | RegexOptions.Multiline);
            List<NamedColor> namedColors = new List<NamedColor>();
            foreach(Match match in namedColorRegex.Matches(namedColorText))
            {
                namedColors.Add(new NamedColor(match.Groups["name"].Value, match.Groups["group"].Value, Argb.Parse(match.Groups["hex"].Value), match.Groups["css"].Success));
            }
            return namedColors;
        }
    }
}