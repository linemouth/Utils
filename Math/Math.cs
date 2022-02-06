using System;
using System.Collections.Generic;
using System.Linq;
using SysMath = System.Math;

namespace Utils
{
    public static class Math
    {
        /// <summary>Represents the ratio of the circumference of a circle to its diameter Ï€.</summary>
        public const double PI = 3.1415926535897932384626433832795;
        /// <summary>Represents the natural logarithmic base.</summary>
        public const double E = 2.7182818284590452353602874713527;
        public const double Deg2Rad = 0.01745329251994329576923690768489;
        public const double Rad2Deg = 57.295779513082320876798154814105;
        public const double DoubleLargeEpsilon = double.Epsilon * 100;
        public const double FloatLargeEpsilon = float.Epsilon * 100;

        private static readonly Dictionary<int, string> siPrefixes = new Dictionary<int, string> { { -24, "y" }, { -21, "z" }, { -18, "a" }, { -15, "f" }, { -12, "p" }, { -9, "n" }, { -6, "u" }, { -3, "m" }, { 0, "" }, { 3, "k" }, { 6, "M" }, { 9, "G" }, { 12, "T" }, { 15, "P" }, { 18, "E" }, { 21, "Z" }, { 24, "Y" } };
        private static readonly int minSiOrder = siPrefixes.Keys.Min();
        private static readonly int maxSiOrder = siPrefixes.Keys.Max();

        #region double
        /// <summary>Returns the absolute value of the specified value.</summary>
        public static double Abs(this double value) => SysMath.Abs(value);
        public static bool Approximately(this double a, double b) => SysMath.Abs(a - b) < DoubleLargeEpsilon;
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static double Clamp(this double value, double min = 0d, double max = 1d) => value > max ? max : value < min ? min : value;
        public static double Lerp(this double a, double b, double t) => LerpUnclamped(a, b, Clamp(t));
        public static double LerpUnclamped(this double a, double b, double t) => ((b - a) * t) + a;
        public static double InverseLerp(double a, double b, double value)
        {
            double result;
            if(a != b)
            {
                result = Clamp((value - a) / (b - a));
            }
            else
            {
                result = 0f;
            }
            return result;
        }
        public static double Remap(double value, double minIn, double maxIn, double minOut, double maxOut)
        {
            double rangeIn = maxIn - minIn;
            double rangeOut = maxOut - minOut;
            double offset = minOut - minIn;
            return ((value / rangeIn) + offset) * rangeOut;
        }
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static double Repeat(this double value, double min = 0d, double max = 1d) => value % (max - min) + min;
        /// <summary>Returns e raised to the specified power.</summary>
        public static double Exp(this double value) => SysMath.Exp(value);
        /// <summary>Returns the largest integer less than or equal to the specified value.</summary>
        public static double Floor(this double value) => SysMath.Floor(value);
        /// <summary>Returns the largest integer less than or equal to the specified value.</summary>
        public static int FloorToInt(this double value) => value >= 0 ? (int)value : (int)value - 1;
        /// <summary>Returns the smallest integer greater than or equal to the specified value.</summary>
        public static double Ceiling(this double value) => SysMath.Ceiling(value);
        /// <summary>Returns the remainder resulting from the division of a specified number by another specified number.</summary>
        public static double IEEERemainder(this double dividend, double divisor) => SysMath.IEEERemainder(dividend, divisor);
        public static bool IsInteger(this double value) => Abs(value % 1) <= DoubleLargeEpsilon;
        /// <summary>Returns the logarithm of a specified number in a specified base.</summary>
        public static double Log(this double a, double b) => SysMath.Log(a, b);
        /// <summary>Returns the natural (base e) logarithm of a specified number.</summary>
        public static double Log(this double d) => SysMath.Log(d);
        /// <summary>Returns the base 10 logarithm of a specified number.</summary>
        public static double Log10(this double d) => SysMath.Log10(d);
        /// <summary>Returns the number of decimal places required to represent a certain number of significant digits.</summary>
        public static int GetDecimalDigits(this double d, int digits, bool truncateExactZero = false)
        {
            if(d == 0)
            {
                return truncateExactZero ? 0 : digits;
            }

            double scaled = d;
            while(digits > 0 && Abs(scaled) >= 1)
            {
                scaled /= 10;
                digits--;
            }
            while(Abs(scaled) < 0.1)
            {
                scaled *= 10;
                digits++;
            }
            return digits;
        }
        private static int GetOrderOfMagnitude(double value, double orderScale = 10.0, int orderIncrement = 1, int minOrder = int.MinValue, int maxOrder = int.MaxValue) => GetOrderOfMagnitude(value, out double _, orderIncrement, minOrder, maxOrder);
        private static int GetOrderOfMagnitude(double value, out double scaledValue, double orderScale = 10.0, int orderIncrement = 1, int minOrder = int.MinValue, int maxOrder = int.MaxValue)
        {
            int order = 0;
            if(value != 0)
            {
                while(SysMath.Abs(value) >= orderScale)
                {
                    value /= orderScale;
                    order += orderIncrement;
                }
                while(SysMath.Abs(value) < 1)
                {
                    value *= orderScale;
                    order -= orderIncrement;
                }
            }
            scaledValue = value;
            return order;
        }
        /// <summary>Formats a number to a string using standard notation.</summary>
        public static string Format(this double d, int significantDigits, bool truncateExactZero = false)
        {
            double value = d;
            int decimals = value == 0 ?
                (truncateExactZero ? 0 : significantDigits) : // Special zero handling
                SysMath.Max(0, significantDigits - 1 - GetOrderOfMagnitude(value)); // Handle significant digits by order
            return $"{value.ToString($"F{decimals}")}";
        }
        /// <summary>Formats a number to a string using scientific notation.</summary>
        public static string Format(this double d, int significantDigits, int orderThreshold, bool truncateExactZero = false, bool isIntegral = false)
        {
            double value = d;
            int decimals;
            if(value == 0)
            {
                decimals = truncateExactZero ? 0 : significantDigits;
                return $"{value.ToString($"F{decimals}")}";
            }
            else
            {
                int order = GetOrderOfMagnitude(value, out double scaled);
                if(SysMath.Abs(order) >= orderThreshold)
                {
                    value = scaled;
                    decimals = significantDigits - 1;
                    return $"{value.ToString($"F{decimals}")}e{order}";
                }
                else
                {
                    decimals = SysMath.Max(0, significantDigits - 1 - order); // Handle significant digits by order
                    return $"{value.ToString($"F{decimals}")}";
                }
            }
        }
        /// <summary>Formats a number to a string using SI prefixes to scale the value.</summary>
        public static string Format(this double d, int significantDigits, double siOrderScale, bool truncateExactZero = false, bool isIntegral = false)
        {
            double value = d;
            int metaOrder = 0;
            int decimals;
            if(value == 0)
            {
                decimals = truncateExactZero ? 0 : significantDigits;
            }
            else
            {
                while(SysMath.Abs(value) >= siOrderScale && metaOrder < maxSiOrder)
                {
                    value /= siOrderScale;
                    ++metaOrder;
                }
                while(isIntegral && SysMath.Abs(value) < 1 && metaOrder > minSiOrder)
                {
                    value *= siOrderScale;
                    --metaOrder;
                }
                decimals = isIntegral && metaOrder < 1 ? 0 : value.GetDecimalDigits(significantDigits, truncateExactZero);
            }
            return $"{value.ToString($"F{decimals}")}{siPrefixes[metaOrder]}";
        }
        /// <summary>Returns the larger of two values.</summary>
        public static double Max(double a, double b) => SysMath.Max(a, b);
        public static double Max(double a, double b, double c) => SysMath.Max(a, SysMath.Max(b, c));
        public static double Max(double a, double b, double c, double d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static double Min(double a, double b) => SysMath.Min(a, b);
        public static double Min(double a, double b, double c) => SysMath.Min(a, SysMath.Min(b, c));
        public static double Min(double a, double b, double c, double d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static double Pow(this double value, double power) => SysMath.Pow(value, power);
        /// <summary>Returns the square root of a specified number.</summary>
        public static double Sqrt(this double value) => SysMath.Sqrt(value);
        /// <summary>Returns the square of a number.</summary>
        public static double Sqr(this double value) => value * value;
        /// <summary>Returns the cube of a number.</summary>
        public static double Cube(this double value) => value * value * value;
        /// <summary>Returns the number raised to the fourth power.</summary>
        public static double Pow4(this double value) { value *= value; return value * value; }
        /// <summary>Rounds a value to a specified number of fractional digits. A parameter specifies how to round the value if it is midway between two numbers.</summary>
        public static double Round(this double value, int digits, MidpointRounding mode) => SysMath.Round(value, digits, mode);
        /// <summary>Rounds a value to the nearest integer. A parameter specifies how to round the value if it is midway between two numbers.</summary>
        public static double Round(this double value, MidpointRounding mode) => SysMath.Round(value, mode);
        /// <summary>Rounds a value to a specified number of fractional digits.</summary>
        public static double Round(this double value, int digits) => SysMath.Round(value, digits);
        /// <summary>Rounds a value to the nearest integral value.</summary>
        public static double Round(this double value) => SysMath.Round(value);
        public static int RoundToInt(this double value) => (int)value.Round();
        public static long RoundToLong(this double value) => (int)value.Round();
        /// <summary>Performs division as normal, but if the denominator is 0, returns +/-Infinity for a nonzero numerator, otherwise returns 0.</summary>
        public static double SafeDivide(this double numerator, double denominator)
        {
            if(denominator == 0)
            {
                return numerator == 0 ? 0 : numerator > 0 ? double.PositiveInfinity : double.NegativeInfinity;
            }
            return numerator / denominator;
        }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this double value) => SysMath.Sign(value);
        /// <summary>Returns the sine of the specified angle.</summary>
        public static double Sin(this double value) => SysMath.Sin(value);
        /// <summary>Returns the hyperbolic sine of the specified angle.</summary>
        public static double Sinh(this double value) => SysMath.Sinh(value);
        /// <summary>Returns the cosine of the specified angle.</summary>
        public static double Cos(this double d) => SysMath.Cos(d);
        /// <summary>Returns the hyperbolic cosine of the specified angle.</summary>
        public static double Cosh(this double value) => SysMath.Cosh(value);
        /// <summary>Returns the tangent of the specified angle.</summary>
        public static double Tan(this double value) => SysMath.Tan(value);
        /// <summary>Returns the hyperbolic tangent of the specified angle.</summary>
        public static double Tanh(this double value) => SysMath.Tanh(value);
        /// <summary>Returns the angle whose sine is the specified number.</summary>
        public static double Asin(this double value) => SysMath.Asin(value);
        /// <summary>Returns the angle whose cosine is the specified number.</summary>
        public static double Acos(this double value) => SysMath.Acos(value);
        /// <summary>Returns the angle whose tangent is the specified number.</summary>
        public static double Atan(this double value) => SysMath.Atan(value);
        /// <summary>Returns the angle whose tangent is the quotient of two specified numbers.</summary>
        public static double Atan2(this double y, double x) => SysMath.Atan2(y, x);
        /// <summary>Calculates the integral part of a specified value.</summary>
        public static double Truncate(this double value) => SysMath.Truncate(value);
        /// <summary>Calculates the sigmoid of a number.</summary>
        public static double Sigmoid(this double value) => 1.0 / (1.0 + SysMath.Exp(-value));
        /// <summary>Calculates the sigmoid of a number using an approximation.</summary>
        public static double FastSigmoid(this double value) => value / (1.0 + SysMath.Abs(value));
        #endregion

        #region float
        /// <summary>Returns the absolute value of the specified value.</summary>
        public static float Abs(float value) => SysMath.Abs(value);
        public static bool Approximately(this float a, float b) => SysMath.Abs(a - b) < FloatLargeEpsilon;
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static float Clamp(this float value, float min = 0f, float max = 1f) => value > max ? max : value < min ? min : value;
        public static float Lerp(this float a, float b, float t) => LerpUnclamped(a, b, Clamp(t));
        public static float Lerp(this float a, float b, double t) => LerpUnclamped(a, b, Clamp(t));
        public static float LerpUnclamped(this float a, float b, float t) => (float)LerpUnclamped((double)a, (double)b, (double)t);
        public static float LerpUnclamped(this float a, float b, double t) => (float)LerpUnclamped((double)a, (double)b, (double)t);
        public static float InverseLerp(float a, float b, float value)
        {
            float result;
            if(a != b)
            {
                result = Clamp((value - a) / (b - a));
            }
            else
            {
                result = 0f;
            }
            return result;
        }
        public static float Remap(float value, float minIn, float maxIn, float minOut, float maxOut)
        {
            double rangeIn = maxIn - minIn;
            double rangeOut = maxOut - minOut;
            double offset = minOut - minIn;
            return (float)(((value / rangeIn) + offset) * rangeOut);
        }
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static float Repeat(this float value, float min = 0f, float max = 1f) => (float)Repeat((double)value, (double)min, (double)max);
        /// <summary>Returns e raised to the specified power.</summary>
        public static float Exp(this float value) => (float)SysMath.Exp((double)value);
        /// <summary>Returns the largest integer less than or equal to the specified value.</summary>
        public static float Floor(this float value) => (float)SysMath.Floor((double)value);
        /// <summary>Returns the largest integer less than or equal to the specified value.</summary>
        public static int FloorToInt(this float value) => value >= 0 ? (int)value : (int)value - 1;
        /// <summary>Returns the smallest integer greater than or equal to the specified value.</summary>
        public static float Ceiling(this float value) => (float)SysMath.Ceiling((double)value);
        /// <summary>Returns the remainder resulting from the division of a specified number by another specified number.</summary>
        public static float IEEERemainder(float dividend, float divisor) => (float)SysMath.IEEERemainder((double)dividend, (double)divisor);
        public static bool IsInteger(this float value) => Abs(value % 1) <= FloatLargeEpsilon;
        /// <summary>Returns the logarithm of a specified number in a specified base.</summary>
        public static float Log(this float a, float b) => (float)SysMath.Log((double)a, (double)b);
        /// <summary>Returns the natural (base e) logarithm of a specified number.</summary>
        public static float Log(this float value) => (float)SysMath.Log((double)value);
        /// <summary>Returns the base 10 logarithm of a specified number.</summary>
        public static float Log10(this float value) => (float)SysMath.Log10((double)value);
        /// <summary>Returns the number of decimal places required to represent a certain number of significant digits.</summary>
        public static int GetDecimalDigits(this float value, int digits, bool truncateExactZero = false) => GetDecimalDigits((double)value, digits, truncateExactZero);
        /// <summary>Formats a number to a string using standard notation.</summary>
        public static string Format(this float value, int significantDigits, bool truncateExactZero = false) => Format((double)value, significantDigits, truncateExactZero);
        /// <summary>Formats a number to a string using scientific notation.</summary>
        public static string Format(this float value, int significantDigits, int orderThreshold, bool truncateExactZero = false, bool isIntegral = false) => Format((double)value, significantDigits, orderThreshold, truncateExactZero, isIntegral);
        /// <summary>Formats a number to a string using SI prefixes to scale the value.</summary>
        public static string Format(this float value, int significantDigits, double siOrderScale, bool truncateExactZero = false, bool isIntegral = false) => Format((double)value, significantDigits, siOrderScale, truncateExactZero, isIntegral);
        /// <summary>Returns the larger of two values.</summary>
        public static float Max(float a, float b) => SysMath.Max(a, b);
        public static float Max(float a, float b, float c) => SysMath.Max(a, SysMath.Max(b, c));
        public static float Max(float a, float b, float c, float d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static float Min(float a, float b) => SysMath.Min(a, b);
        public static float Min(float a, float b, float c) => SysMath.Min(a, SysMath.Min(b, c));
        public static float Min(float a, float b, float c, float d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static float Pow(this float value, float power) => (float)SysMath.Pow((double)value, (double)power);
        /// <summary>Returns the square root of a specified number.</summary>
        public static float Sqrt(this float value) => (float)SysMath.Sqrt((double)value);
        /// <summary>Returns the square of a number.</summary>
        public static float Sqr(this float value) => value * value;
        /// <summary>Returns the cube of a number.</summary>
        public static float Cube(this float value) { double v = value; return (float)(v * v * v); }
        /// <summary>Returns a number raised to the fourth power.</summary>
        public static float Pow4(this float value) { double v = value; v *= v; return (float)(v * v); }
        /// <summary>Rounds a value to a specified number of fractional digits. A parameter specifies how to round the value if it is midway between two numbers.</summary>
        public static float Round(this float value, int digits, MidpointRounding mode) => (float)SysMath.Round((double)value, digits, mode);
        /// <summary>Rounds a value to the nearest integer. A parameter specifies how to round the value if it is midway between two numbers.</summary>
        public static float Round(this float value, MidpointRounding mode) => (float)SysMath.Round((double)value, mode);
        /// <summary>Rounds a value to a specified number of fractional digits.</summary>
        public static float Round(this float value, int digits) => (float)SysMath.Round((double)value, digits);
        /// <summary>Rounds a value to the nearest integral value.</summary>
        public static float Round(this float value) => (float)SysMath.Round((double)value);
        public static int RoundToInt(this float value) => (int)value.Round();
        public static long RoundToLong(this float value) => (int)value.Round();
        /// <summary>Performs division as normal, but if the denominator is 0, returns +/-Infinity for a nonzero numerator, otherwise returns 0.</summary>
        public static float SafeDivide(this float numerator, float denominator)
        {
            if(denominator == 0)
            {
                return numerator == 0 ? 0 : numerator > 0 ? float.PositiveInfinity : float.NegativeInfinity;
            }
            return numerator / denominator;
        }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this float value) => SysMath.Sign(value);
        /// <summary>Returns the sine of the specified angle.</summary>
        public static float Sin(this float value) => (float)SysMath.Sin((double)value);
        /// <summary>Returns the hyperbolic sine of the specified angle.</summary>
        public static float Sinh(this float value) => (float)SysMath.Sinh((double)value);
        /// <summary>Returns the cosine of the specified angle.</summary>
        public static float Cos(this float value) => (float)SysMath.Cos((double)value);
        /// <summary>Returns the hyperbolic cosine of the specified angle.</summary>
        public static float Cosh(this float value) => (float)SysMath.Cosh((double)value);
        /// <summary>Returns the tangent of the specified angle.</summary>
        public static float Tan(this float value) => (float)SysMath.Tan((double)value);
        /// <summary>Returns the hyperbolic tangent of the specified angle.</summary>
        public static float Tanh(this float value) => (float)SysMath.Tanh((double)value);
        /// <summary>Returns the angle whose sine is the specified number.</summary>
        public static float Asin(this float value) => (float)SysMath.Asin((double)value);
        /// <summary>Returns the angle whose cosine is the specified number.</summary>
        public static float Acos(this float value) => (float)SysMath.Acos((double)value);
        /// <summary>Returns the angle whose tangent is the specified number.</summary>
        public static float Atan(this float value) => (float)SysMath.Atan((double)value);
        /// <summary>Returns the angle whose tangent is the quotient of two specified numbers.</summary>
        public static float Atan2(this float y, float x) => (float)SysMath.Atan2((double)y, (double)x);
        /// <summary>Calculates the integral part of a specified value.</summary>
        public static float Truncate(this float value) => (float)SysMath.Truncate((double)value);
        /// <summary>Calculates the integral part of a specified value.</summary>
        public static int TruncateToInt(this float value) => value >= 0 ? (int)value : (int)value - 1;
        /// <summary>Calculates the sigmoid of a number.</summary>
        public static float Sigmoid(this float value) => (float)(1.0 / (1.0 + SysMath.Exp((double)-value)));
        /// <summary>Calculates the sigmoid of a number using an approximation.</summary>
        public static float FastSigmoid(this float value) { double v = value; return (float)(v / (1.0 + SysMath.Abs(v))); }
        #endregion

        #region sbyte
        /// <summary>Returns the absolute value of the specified value.</summary>
        public static sbyte Abs(this sbyte value) => SysMath.Abs(value);
        /// <summary>Returns the greatest common denominator of the specified value.<br/><b>Note: </b><em>This will always return a positive number.</em></summary>
        public static sbyte GCD(sbyte a, sbyte b)
        {
            a = Abs(a);
            b = Abs(b);
            while(a != 0 && b != 0)
            {
                if(a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }
            return (sbyte)(a | b);
        }
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static sbyte Clamp(this sbyte value, sbyte min = 0, sbyte max = 1) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the larger of two values.</summary>
        public static sbyte Max(sbyte a, sbyte b) => SysMath.Max(a, b);
        public static sbyte Max(sbyte a, sbyte b, sbyte c) => SysMath.Max(a, SysMath.Max(b, c));
        public static sbyte Max(sbyte a, sbyte b, sbyte c, sbyte d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static sbyte Min(sbyte a, sbyte b) => SysMath.Min(a, b);
        public static sbyte Min(sbyte a, sbyte b, sbyte c) => SysMath.Min(a, SysMath.Min(b, c));
        public static sbyte Min(sbyte a, sbyte b, sbyte c, sbyte d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static sbyte Pow(sbyte value, sbyte exponent) => (sbyte)SysMath.Pow(value, exponent);
        /// <summary>Returns the square of a number.</summary>
        public static sbyte Sqr(sbyte value) => (sbyte)(value * value);
        /// <summary>Returns the cube of a number.</summary>
        public static sbyte Cube(sbyte value) => (sbyte)(value * value * value);
        /// <summary>Returns a number raised to the fourth power.</summary>
        public static sbyte Pow4(sbyte value) { value *= value; return (sbyte)(value * value); }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this sbyte value) => SysMath.Sign(value);
        #endregion

        #region byte
        /// <summary>Returns the greatest common denominator of the specified value.</summary>
        public static byte GCD(byte a, byte b)
        {
            while(a != 0 && b != 0)
            {
                if(a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }
            return (byte)(a | b);
        }
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static byte Clamp(this byte value, byte min = 0, byte max = 1) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the larger of two values.</summary>
        public static byte Max(byte a, byte b) => SysMath.Max(a, b);
        public static byte Max(byte a, byte b, byte c) => SysMath.Max(a, SysMath.Max(b, c));
        public static byte Max(byte a, byte b, byte c, byte d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static byte Min(byte a, byte b) => SysMath.Min(a, b);
        public static byte Min(byte a, byte b, byte c) => SysMath.Min(a, SysMath.Min(b, c));
        public static byte Min(byte a, byte b, byte c, byte d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static byte Pow(byte value, byte exponent) => (byte)SysMath.Pow(value, exponent);
        /// <summary>Returns the square of a number.</summary>
        public static byte Sqr(byte value) => (byte)(value * value);
        /// <summary>Returns the cube of a number.</summary>
        public static byte Cube(byte value) => (byte)(value * value * value);
        /// <summary>Returns a number raised to the fourth power.</summary>
        public static byte Pow4(byte value) { value *= value; return (byte)(value * value); }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this byte value) => value == 0 ? 0 : 1;
        #endregion

        #region short
        /// <summary>Returns the absolute value of the specified value.</summary>
        public static short Abs(this short value) => SysMath.Abs(value);
        /// <summary>Returns the greatest common denominator of the specified value.<br/><b>Note: </b><em>This will always return a positive number.</em></summary>
        public static short GCD(short a, short b)
        {
            a = Abs(a);
            b = Abs(b);
            while(a != 0 && b != 0)
            {
                if(a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }
            return (short)(a | b);
        }
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static short Clamp(this short value, short min = 0, short max = 1) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the larger of two values.</summary>
        public static short Max(short a, short b) => SysMath.Max(a, b);
        public static short Max(short a, short b, short c) => SysMath.Max(a, SysMath.Max(b, c));
        public static short Max(short a, short b, short c, short d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static short Min(short a, short b) => SysMath.Min(a, b);
        public static short Min(short a, short b, short c) => SysMath.Min(a, SysMath.Min(b, c));
        public static short Min(short a, short b, short c, short d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static short Pow(short value, short exponent) => (short)SysMath.Pow(value, exponent);
        /// <summary>Returns the square of a number.</summary>
        public static short Sqr(short value) => (short)(value * value);
        /// <summary>Returns the cube of a number.</summary>
        public static short Cube(short value) => (short)(value * value * value);
        /// <summary>Returns a number raised to the fourth power.</summary>
        public static short Pow4(short value) { value *= value; return (short)(value * value); }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this short value) => SysMath.Sign(value);
        #endregion

        #region ushort
        /// <summary>Returns the greatest common denominator of the specified value.</summary>
        public static ushort GCD(ushort a, ushort b)
        {
            while(a != 0 && b != 0)
            {
                if(a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }
            return (ushort)(a | b);
        }
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static ushort Clamp(this ushort value, ushort min = 0, ushort max = 1) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the larger of two values.</summary>
        public static ushort Max(ushort a, ushort b) => SysMath.Max(a, b);
        public static ushort Max(ushort a, ushort b, ushort c) => SysMath.Max(a, SysMath.Max(b, c));
        public static ushort Max(ushort a, ushort b, ushort c, ushort d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static ushort Min(ushort a, ushort b) => SysMath.Min(a, b);
        public static ushort Min(ushort a, ushort b, ushort c) => SysMath.Min(a, SysMath.Min(b, c));
        public static ushort Min(ushort a, ushort b, ushort c, ushort d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static ushort Pow(ushort value, ushort exponent) => (ushort)SysMath.Pow(value, exponent);
        /// <summary>Returns the square of a number.</summary>
        public static ushort Sqr(ushort value) => (ushort)(value * value);
        /// <summary>Returns the cube of a number.</summary>
        public static ushort Cube(ushort value) => (ushort)(value * value * value);
        /// <summary>Returns a number raised to the fourth power.</summary>
        public static ushort Pow4(ushort value) { value *= value; return (ushort)(value * value); }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this ushort value) => value == 0 ? 0 : 1;
        #endregion

        #region int
        /// <summary>Returns the absolute value of the specified value.</summary>
        public static int Abs(this int value) => SysMath.Abs(value);
        /// <summary>Returns the greatest common denominator of the specified value.<br/><b>Note: </b><em>This will always return a positive number.</em></summary>
        public static int GCD(int a, int b)
        {
            a = Abs(a);
            b = Abs(b);
            while(a != 0 && b != 0)
            {
                if(a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }
            return a | b;
        }
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static int Clamp(this int value, int min = 0, int max = 1) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the larger of two values.</summary>
        public static int Max(int a, int b) => SysMath.Max(a, b);
        public static int Max(int a, int b, int c) => SysMath.Max(a, SysMath.Max(b, c));
        public static int Max(int a, int b, int c, int d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static int Min(int a, int b) => SysMath.Min(a, b);
        public static int Min(int a, int b, int c) => SysMath.Min(a, SysMath.Min(b, c));
        public static int Min(int a, int b, int c, int d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static int Pow(int value, int exponent) => (int)SysMath.Pow(value, exponent);
        /// <summary>Returns the square of a number.</summary>
        public static int Sqr(int value) => value * value;
        /// <summary>Returns the cube of a number.</summary>
        public static int Cube(int value) => value * value * value;
        /// <summary>Returns a number raised to the fourth power.</summary>
        public static int Pow4(int value) { value *= value; return value * value; }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this int value) => SysMath.Sign(value);
        #endregion

        #region uint
        /// <summary>Returns the greatest common denominator of the specified value.</summary>
        public static uint GCD(uint a, uint b)
        {
            while(a != 0 && b != 0)
            {
                if(a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }
            return a | b;
        }
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static uint Clamp(this uint value, uint min = 0, uint max = 1) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the larger of two values.</summary>
        public static uint Max(uint a, uint b) => SysMath.Max(a, b);
        public static uint Max(uint a, uint b, uint c) => SysMath.Max(a, SysMath.Max(b, c));
        public static uint Max(uint a, uint b, uint c, uint d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static uint Min(uint a, uint b) => SysMath.Min(a, b);
        public static uint Min(uint a, uint b, uint c) => SysMath.Min(a, SysMath.Min(b, c));
        public static uint Min(uint a, uint b, uint c, uint d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static uint Pow(uint value, uint exponent) => (uint)SysMath.Pow(value, exponent);
        /// <summary>Returns the square of a number.</summary>
        public static uint Sqr(uint value) => value * value;
        /// <summary>Returns the cube of a number.</summary>
        public static uint Cube(uint value) => value * value * value;
        /// <summary>Returns a number raised to the fourth power.</summary>
        public static uint Pow4(uint value) { value *= value; return value * value; }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this uint value) => value == 0 ? 0 : 1;
        #endregion

        #region long
        /// <summary>Returns the absolute value of the specified value.</summary>
        public static long Abs(this long value) => SysMath.Abs(value);
        /// <summary>Returns the greatest common denominator of the specified value.<br/><b>Note: </b><em>This will always return a positive number.</em></summary>
        public static long GCD(long a, long b)
        {
            a = Abs(a);
            b = Abs(b);
            while(a != 0 && b != 0)
            {
                if(a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }
            return a | b;
        }
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static long Clamp(this long value, long min = 0, long max = 1) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the larger of two values.</summary>
        public static long Max(long a, long b) => SysMath.Max(a, b);
        public static long Max(long a, long b, long c) => SysMath.Max(a, SysMath.Max(b, c));
        public static long Max(long a, long b, long c, long d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static long Min(long a, long b) => SysMath.Min(a, b);
        public static long Min(long a, long b, long c) => SysMath.Min(a, SysMath.Min(b, c));
        public static long Min(long a, long b, long c, long d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static long Pow(long value, long exponent) => (long)SysMath.Pow(value, exponent);
        /// <summary>Returns the square of a number.</summary>
        public static long Sqr(long value) => value * value;
        /// <summary>Returns the cube of a number.</summary>
        public static long Cube(long value) => value * value * value;
        /// <summary>Returns a number raised to the fourth power.</summary>
        public static long Pow4(long value) { value *= value; return value * value; }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this long value) => SysMath.Sign(value);
        #endregion

        #region ulong
        /// <summary>Returns the greatest common denominator of the specified value.</summary>
        public static ulong GCD(ulong a, ulong b)
        {
            while(a != 0 && b != 0)
            {
                if(a > b)
                {
                    a %= b;
                }
                else
                {
                    b %= a;
                }
            }
            return a | b;
        }
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static ulong Clamp(this ulong value, ulong min = 0, ulong max = 1) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the larger of two values.</summary>
        public static ulong Max(ulong a, ulong b) => SysMath.Max(a, b);
        public static ulong Max(ulong a, ulong b, ulong c) => SysMath.Max(a, SysMath.Max(b, c));
        public static ulong Max(ulong a, ulong b, ulong c, ulong d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static ulong Min(ulong a, ulong b) => SysMath.Min(a, b);
        public static ulong Min(ulong a, ulong b, ulong c) => SysMath.Min(a, SysMath.Min(b, c));
        public static ulong Min(ulong a, ulong b, ulong c, ulong d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns a specified number raised to the specified power.</summary>
        public static ulong Pow(ulong value, ulong exponent) => (ulong)SysMath.Pow(value, exponent);
        /// <summary>Returns the square of a number.</summary>
        public static ulong Sqr(ulong value) => value * value;
        /// <summary>Returns the cube of a number.</summary>
        public static ulong Cube(ulong value) => value * value * value;
        /// <summary>Returns a number raised to the fourth power.</summary>
        public static ulong Pow4(ulong value) { value *= value; return value * value; }
        /// <summary>Returns a value indicating the sign of the specified value.</summary>
        public static int Sign(this ulong value) => value == 0 ? 0 : 1;
        #endregion
    }
}