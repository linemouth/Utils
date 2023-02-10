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
        public const double DegToRad = 0.01745329251994329576923690768489;
        public const double RadToDeg = 57.295779513082320876798154814105;
        public const double DoubleLargeEpsilon = double.Epsilon * 100;
        public const double FloatLargeEpsilon = float.Epsilon * 100;
        public const double Sqrt2 = 1.4142135623730950488016887242097;
        public const double Sqrt2Pi = 2.5066282746310005024157652848110;
        public static readonly Random random = new Random();

        public static Map<string, int> SiPrefixes = new Map<string, int> { { "y", -24 }, { "z", -21 }, { "a", -18 }, { "f", -15 }, { "p", -12 }, { "n", -9 }, { "u", -6 }, { "m", -3 }, { "", 0 }, { "k", 3 }, { "M", 6 }, { "G", 9 }, { "T", 12 }, { "P", 15 }, { "E", 18 }, { "Z", 21 }, { "Y", 24 } };
        private static readonly int minSiOrder = SiPrefixes.Forward.Values.Min();
        private static readonly int maxSiOrder = SiPrefixes.Forward.Values.Max();
        private static readonly double invSqrtPi = 1 / Sqrt(PI);

        #region double
        /// <summary>Returns the absolute value of the specified value.</summary>
        public static double Abs(this double value) => SysMath.Abs(value);
        /// <summary>Returns true is the two values are within a small margin of each other.</summary>
        public static bool Approximately(this double a, double b) => SysMath.Abs(a - b) < DoubleLargeEpsilon;
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static double Clamp(this double value, double min = 0d, double max = 1d) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the shortest difference between two angles.</summary>
        public static double DeltaAngle(this double current, double target)
        {
            double delta = Repeat((target - current), 360.0F);
            if(delta > 180.0F)
            {
                delta -= 360.0F;
            }
            return delta;
        }
        /// <summary>Returns the dot product of two vectors.</summary>
        public static double DotProduct(this double[] a, double[] b)
        {
            // Validate the input.
            if(a.Length != b.Length)
            {
                throw new ArgumentException("Vectors must have the same length.");
            }

            // Compute the dot product.
            double result = 0;
            for(int i = 0; i < a.Length; i++)
            {
                result += a[i] * b[i];
            }

            return result;
        }
        /// <summary>Returns the dot product of two vectors.</summary>
        public static double DotProduct(this ICollection<double> a, ICollection<double> b)
        {
            // Validate the input.
            if(a.Count != b.Count)
            {
                throw new ArgumentException("Vectors must have the same length.");
            }

            // Compute the dot product
            double result = 0;
            using(IEnumerator<double> aEnumerator = a.GetEnumerator(), bEnumerator = b.GetEnumerator())
            {
                while(aEnumerator.MoveNext() && bEnumerator.MoveNext())
                {
                    result += aEnumerator.Current * bEnumerator.Current;
                }
            }

            return result;
        }
        /// <summary>Linearly interpolates between two values, truncating at the ends.</summary>
        public static double Lerp(this double a, double b, double t) => LerpUnclamped(a, b, Clamp(t));
        /// <summary>Linearly interpolates between two values, or extrapolates beyond them.</summary>
        public static double LerpUnclamped(this double a, double b, double t) => ((b - a) * t) + a;
        /// <summary>Linearly interpolates between two angles, wrapping as necessary.</summary>
        public static double LerpAngle(this double a, double b, double t)
        {
            double delta = Repeat((b - a), 360);
            if(delta > 180)
            {
                delta -= 360;
            }
            return a + delta * Clamp(t, 0, 1);
        }
        /// <summary>Interpolates linearly back and forth between 0 and length.</summary>
        public static double PingPong(this double t, double length)
        {
            t = Repeat(t, length * 2.0);
            return length - Abs(t - length);
        }
        /// <summary>Interpolates linearly back and forth between two values.</summary>
        public static double PingPong(this double t, double min, double max) => PingPong(t - min, max - min);
        /// <summary>Calculates the fractional position within the range.</summary>
        public static double InverseLerp(this double a, double b, double value)
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
        /// <summary>Transforms a value from one range to another.</summary>
        public static double Remap(this double value, double minIn, double maxIn, double minOut, double maxOut)
        {
            double rangeIn = maxIn - minIn;
            double rangeOut = maxOut - minOut;
            double offset = minOut - minIn;
            return ((value / rangeIn) + offset) * rangeOut;
        }
        /// <summary>Returns the modulus of the value in the range [0, max).</summary>
        public static double Repeat(this double value, double max) => value % max;
        /// <summary>Returns the modulus of the value in the range [min, max) offset by min.</summary>
        public static double Repeat(this double value, double min, double max) => (value - min) % (max - min) + min;
        /// <summary>Returns e raised to the specified power.</summary>
        public static double Exp(this double value) => SysMath.Exp(value);
        /// <summary>Returns the largest integer less than or equal to the specified value.</summary>
        public static double Floor(this double value) => SysMath.Floor(value);
        /// <summary>Returns the largest integer less than or equal to the specified value.</summary>
        public static int FloorToInt(this double value) => value >= 0 ? (int)value : (int)value - 1;
        /// <summary>Returns the smallest integer greater than or equal to the specified value.</summary>
        public static double Ceiling(this double value) => SysMath.Ceiling(value);
        /// <summary>Returns the smallest power of 2 that is greater than the absolute value.</summary>
        public static int PowerOfTwoCeiling(this double value)
        {
            throw new NotImplementedException();
            // Directly manipulate the exponent
            // https://en.wikipedia.org/wiki/Double-precision_floating-point_format
            //IEEE784 breakdown = value;
            //breakdown.Exponent
        }
        /// <summary>Returns the remainder resulting from the division of a specified number by another specified number.</summary>
        public static double IEEERemainder(this double dividend, double divisor) => SysMath.IEEERemainder(dividend, divisor);
        /// <summary>Returns true if the value is approximately an integer value.</summary>
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
        /// <summary>Gets the order of magnitude needed to bring this value to within the order scale.</summary>
        /// <param name="value">The original value to scale.</param>
        /// <param name="orderScale">For each order increment, scale the value by this amount.</param>
        /// <param name="orderIncrement">The increment between orders of magnitude.</param>
        /// <param name="minOrder">Minimum order to return.</param>
        /// <param name="maxOrder">Maximum order to return.</param>
        /// <returns>The total order increments by which the value needed to scale.</returns>
        private static int GetOrderOfMagnitude(this double value, double orderScale = 10.0, int orderIncrement = 1, int minOrder = int.MinValue, int maxOrder = int.MaxValue) => GetOrderOfMagnitude(value, out double _, orderScale, orderIncrement, minOrder, maxOrder);
        /// <summary>Gets the order of magnitude needed to bring this value to within the order scale.</summary>
        /// <param name="value">The original value to scale.</param>
        /// <param name="scaledValue">The value after having been scaled.</param>
        /// <param name="orderScale">For each order increment, scale the value by this amount.</param>
        /// <param name="orderIncrement">The increment between orders of magnitude.</param>
        /// <param name="minOrder">Minimum order to return.</param>
        /// <param name="maxOrder">Maximum order to return.</param>
        /// <returns>The total order increments by which the value needed to scale.</returns>
        private static int GetOrderOfMagnitude(this double value, out double scaledValue, double orderScale = 10.0, int orderIncrement = 1, int minOrder = int.MinValue, int maxOrder = int.MaxValue)
        {
            int order = 0;
            if(value != 0)
            {
                while(SysMath.Abs(value) >= orderScale && order < maxOrder)
                {
                    value /= orderScale;
                    order += orderIncrement;
                }
                while(SysMath.Abs(value) < 1 && order > minOrder)
                {
                    value *= orderScale;
                    order -= orderIncrement;
                }
            }
            scaledValue = value;
            return Clamp(order, minOrder, maxOrder);
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
                    metaOrder += 3;
                }
                while(isIntegral && SysMath.Abs(value) < 1 && metaOrder > minSiOrder)
                {
                    value *= siOrderScale;
                    metaOrder -= 3;
                }
                decimals = isIntegral && metaOrder < 1 ? 0 : value.GetDecimalDigits(significantDigits, truncateExactZero);
            }
            return $"{value.ToString($"F{decimals}")}{SiPrefixes[metaOrder]}";
        }
        /// <summary>Returns the larger of two values.</summary>
        public static double Max(double a, double b) => SysMath.Max(a, b);
        /// <summary>Returns the larger of three values.</summary>
        public static double Max(double a, double b, double c) => SysMath.Max(a, SysMath.Max(b, c));
        /// <summary>Returns the larger of four values.</summary>
        public static double Max(double a, double b, double c, double d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the lerger of two values, smoothing if they're within range of each other.</summary>
        public static double SoftMax(double a, double b, double range = 1)
        {
            if(range <= 0) { return Max(a, b); }
            var delta = Abs(a - b);
            if(delta > range) { return Max(a, b); }
            delta /= range;
            var mean = (a + b) / 2;
            return mean + (1 + delta * delta) * range / 4;
        }
        /// <summary>Returns the smaller of two values.</summary>
        public static double Min(double a, double b) => SysMath.Min(a, b);
        /// <summary>Returns the smaller of three values.</summary>
        public static double Min(double a, double b, double c) => SysMath.Min(a, SysMath.Min(b, c));
        /// <summary>Returns the smaller of four values.</summary>
        public static double Min(double a, double b, double c, double d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns the smaller of two values, smoothing if they're within range of each other.</summary>
        public static double SoftMin(double a, double b, double range = 1)
        {
            if(range <= 0) { return Min(a, b); }
            var delta = Abs(a - b);
            if(delta > range) { return Min(a, b); }
            delta /= range;
            var mean = (a + b) / 2;
            return mean - (1 + delta * delta) * range / 4;
        }
        /// <summary>Generates a random number in the range [0, max) with linear distribution.</summary>
        public static double Random(double max = 1.0) => random.NextDouble() * max;
        /// <summary>Generates a random number in the range [min, max) with linear distribution.</summary>
        public static double Random(double min, double max) => random.NextDouble() * (max - min) + min;
        /// <summary>Generates a random number according to an exponential decay curve.</summary>
        public static double DecayRandom(double halflife = 1.0)
        {
            double x = random.NextDouble();
            return x == 0 ? 1 : Log(1 / x, 2) * halflife;
        }
        /// <summary>Generates a random number according to a logarithmic range.</summary>
        public static double LogRandom(double min, double max) => Pow(10, Random(Log10(min), Log10(max)));
        /// <summary>
        /// Generates a random number in the range [min, max) by simulating a normal distribution using the sigmoid equation.
        /// Error: +/-1.136% (compared to Gaussian)
        /// </summary>
        public static double GaussianRandom(double center = 0.0, double stdDev = 1.0) => InverseNormal(random.NextDouble()) * stdDev + center;
        /// <summary>Returns the Rectified Linear Unit function of a value.</summary>
        public static double ReLU(this double value) => value <= 0 ? 0 : value;
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
        /// <summary>
        /// Quake Fast Inverse Square Root.
        /// https://en.wikipedia.org/wiki/Fast_inverse_square_root
        /// The magic number is for doubles is from https://cs.uwaterloo.ca/~m32rober/rsqrt.pdf
        /// Error: +0.1752%/-0%
        /// </summary>
        public static unsafe double InvSqrt(this double value)
        {
            double half = value * 0.5;
            *(long*)&value = 0x5FE6EB50C7B537A9 - (*(long*)&value >> 1);
            value *= (1.5 - half * value * value);
            //value *= (1.5 - half * value * value);
            return value;
        }
        /// <summary>Rounds a value to a specified number of fractional digits. A parameter specifies how to round the value if it is midway between two numbers.</summary>
        public static double Round(this double value, int digits, MidpointRounding mode) => SysMath.Round(value, digits, mode);
        /// <summary>Rounds a value to the nearest integer. A parameter specifies how to round the value if it is midway between two numbers.</summary>
        public static double Round(this double value, MidpointRounding mode) => SysMath.Round(value, mode);
        /// <summary>Rounds a value to a specified number of fractional digits.</summary>
        public static double Round(this double value, int digits) => SysMath.Round(value, digits);
        /// <summary>Rounds a value to the nearest integral value.</summary>
        public static double Round(this double value) => SysMath.Round(value);
        /// <summary>Rounds a value to the nearest integer.</summary>
        public static int RoundToInt(this double value) => (int)value.Round();
        /// <summary>Rounds a value to the nearest long integer.</summary>
        public static long RoundToLong(this double value) => (int)value.Round();
        /// <summary>Performs division as normal, but if the denominator is 0, returns +/-Infinity for a nonzero numerator, otherwise returns 0.</summary>
        public static double SafeDivide(this double numerator, double denominator) => SafeDivide(numerator, denominator, numerator >= 0 ? double.PositiveInfinity : double.NegativeInfinity);
        /// <summary>Performs division as normal, but if the denominator is 0, returns the default value.</summary>
        public static double SafeDivide(this double numerator, double denominator, double defaultValue) => denominator == 0 ? defaultValue : numerator / denominator;
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
        /// <summary>Gauss Error Function</summary>
        public static double Erf(this double value)
        {
            return invSqrtPi * Exp(-Pow(value, 2));
            /*double sign = 1;
            if (value < 0)
            {
                sign = -1;
                value = -value;
            }
            return sign * MathInternal.erfLut.Sample(value);*/
        }
        /// <summary>Derivative of Gauss Error Function</summary>
        public static double DErf(this double value) => -2 * invSqrtPi * value * Exp(-value * value);
        /// <summary>Complementary Gauss Error Function</summary>
        public static double Erfc(this double value) => 1 - Erf(value);
        /// <summary>Gauss Probability Distribution Function</summary>
        public static double NormalPdf(this double value) => Exp(-(value * value)) / Sqrt2Pi;
        /// <summary>Gauss Cumulative Distribution Function</summary>
        public static double NormalCdf(this double value) => Erfc(value / Sqrt2) / 2;
        /// <summary>Calculates the sigmoid of a number using an approximation.</summary>
        public static double SmoothHeaviside(this double value) => value <= 0 ? 0 : value >= 1 ? 1 : 0.5 * (2 + Cos(2 * PI * value));
        /// <summary>Smoothly steps from min to max entirely within the range [min, max].</summary>
        public static double SmoothClamp(this double value, double min = 0, double max = 1)
        {
            return value > max ? max : value < min ? min : max + (min - max) / (1.0 + Exp((max - min) * (2.0 * value - min - max) / ((value - min) * (max - value))));
        }
        /// <summary>Smoothly clamps to the range with some spillover beyond the range [min, max].</summary>
        public static double SoftClamp(this double value, double min = 0, double max = 1)
        {
            double mid = (min + max) * 0.5;
            return mid + SmoothClamp((value - mid) * 0.5, min - mid, max - mid);
        }
        /// <summary>Calculates the sigmoid of a number.</summary>
        public static double Sigmoid(this double value) => 1.0 / (1.0 + SysMath.Exp(-value));
        /// <summary>Calculates the sigmoid of a number using an approximation.</summary>
        public static double FastSigmoid(this double value) => value / (1.0 + SysMath.Abs(value));
        /// <summary>Returns the normal distribution's cumulative probability for the given standard deviation.</summary>
        public static double NormalCdf(this double value, double mean = 0, double stdDev = 1) => (1 + Erf((value - mean) / (stdDev * Sqrt2))) / 2;
        /// <summary>
        /// Approximates the inverse of the normal distribution's CDF.
        /// Returns the approximate standard deviation for the given probability.
        /// </summary>
        public static double InverseNormal(this double value) => Log(value / (1 - value));
        /// <summary>Incrementally moves a value towards a target.</summary>
        public static double MoveTowards(this double current, double target, double maxDelta)
        {
            if(Abs(target - current) <= maxDelta)
            {
                return target;
            }
            return current + Sign(target - current) * maxDelta;
        }
        /// <summary>Incrementally moves an angle towards a target.</summary>
        public static double MoveTowardsAngle(this double current, double target, double maxDelta)
        {
            double deltaAngle = DeltaAngle(current, target);
            if(-maxDelta < deltaAngle && deltaAngle < maxDelta)
            {
                return target;
            }
            target = current + deltaAngle;
            return MoveTowards(current, target, maxDelta);
        }
        /// <summary>Smoothly increments from min to max.</summary>
        public static double SmoothStep(this double from, double to, double t)
        {
            t = Clamp(t, 0, 1);
            t = -2.0 * t * t * t + 3.0 * t * t;
            return to * t + from * (1 - t);
        }
        /// <summary>Smoothly moves a value towards a target with inertia.</summary>
        public static double SmoothDamp(this double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed, double deltaTime)
        {
            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Max(0.0001, smoothTime);
            double omega = 2 / smoothTime;
            double x = omega * deltaTime;
            double exp = 1.0 / (1.0 + x + 0.48 * x * x + 0.235 * x * x * x);
            double change = current - target;
            double originalTo = target;

            // Clamp maximum speed
            double maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            target = current - change;
            double temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            double output = target + (change + temp) * exp;

            // Prevent overshooting
            if(originalTo - current > 0.0 == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }
            return output;
        }
        /// <summary>Smoothly moves an angle towards a target with inertia.</summary>
        public static double SmoothDampAngle(this double current, double target, ref double currentVelocity, double smoothTime, double maxSpeed, double deltaTime) => SmoothDamp(current, current + DeltaAngle(current, target), ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        #endregion

        #region float
        /// <summary>Returns the absolute value of the specified value.</summary>
        public static float Abs(float value) => SysMath.Abs(value);
        /// <summary>Returns true is the two values are within a small margin of each other.</summary>
        public static bool Approximately(this float a, float b) => SysMath.Abs(a - b) < FloatLargeEpsilon;
        /// <summary>Returns the value limited to the range [min, max].</summary>
        public static float Clamp(this float value, float min = 0f, float max = 1f) => value > max ? max : value < min ? min : value;
        /// <summary>Returns the shortest difference between two angles.</summary>
        public static float DeltaAngle(this float current, float target) => (float)DeltaAngle((double)current, (double)target);
        /// <summary>Returns the dot product of two vectors.</summary>
        public static float DotProduct(this float[] a, float[] b)
        {
            // Validate the input.
            if(a.Length != b.Length)
            {
                throw new ArgumentException("Vectors must have the same length.");
            }

            // Compute the dot product.
            double result = 0;
            for(int i = 0; i < a.Length; i++)
            {
                result += a[i] * b[i];
            }

            return (float)result;
        }
        /// <summary>Returns the dot product of two vectors.</summary>
        public static float DotProduct(this ICollection<float> a, ICollection<float> b)
        {
            // Validate the input.
            if(a.Count != b.Count)
            {
                throw new ArgumentException("Vectors must have the same length.");
            }

            // Compute the dot product
            double result = 0;
            using(IEnumerator<float> aEnumerator = a.GetEnumerator(), bEnumerator = b.GetEnumerator())
            {
                while(aEnumerator.MoveNext() && bEnumerator.MoveNext())
                {
                    result += aEnumerator.Current * bEnumerator.Current;
                }
            }

            return (float)result;
        }
        /// <summary>Linearly interpolates between two values, truncating at the ends.</summary>
        public static float Lerp(this float a, float b, float t) => LerpUnclamped(a, b, Clamp(t));
        /// <summary>Linearly interpolates between two values, truncating at the ends.</summary>
        public static float Lerp(this float a, float b, double t) => LerpUnclamped(a, b, Clamp(t));
        /// <summary>Linearly interpolates between two values, or extrapolates beyond them.</summary>
        public static float LerpUnclamped(this float a, float b, float t) => (float)LerpUnclamped((double)a, (double)b, (double)t);
        /// <summary>Linearly interpolates between two values, or extrapolates beyond them.</summary>
        public static float LerpUnclamped(this float a, float b, double t) => (float)LerpUnclamped((double)a, (double)b, t);
        /// <summary>Linearly interpolates between two angles, wrapping as necessary.</summary>
        public static float LerpAngle(this float a, float b, float t) => (float)LerpAngle((double)a, (double)b, (double)t);
        /// <summary>Linearly interpolates between two angles, wrapping as necessary.</summary>
        public static float LerpAngle(this float a, float b, double t) => (float)LerpAngle((double)a, (double)b, t);
        /// <summary>Interpolates linearly back and forth between 0 and length.</summary>
        public static float PingPong(this float t, float length) => (float)PingPong((double)t, (double)length);
        /// <summary>Interpolates linearly back and forth between two values.</summary>
        public static float PingPong(this float t, float min, float max) => (float)PingPong((double)t - (double)min, (double)max - (double)min);
        /// <summary>Calculates the fractional position within the range.</summary>
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
        /// <summary>Transforms a value from one range to another.</summary>
        public static float Remap(float value, float minIn, float maxIn, float minOut, float maxOut)
        {
            float rangeIn = maxIn - minIn;
            float rangeOut = maxOut - minOut;
            float offset = minOut - minIn;
            return (((value / rangeIn) + offset) * rangeOut);
        }
        /// <summary>Returns the modulus of the value in the range [0, max).</summary>
        public static float Repeat(this float value, float max) => value % max;
        /// <summary>Returns the modulus of the value in the range [min, max) offset by min.</summary>
        public static float Repeat(this float value, float min, float max) => (value - min) % (max - min) + min;
        /// <summary>Returns e raised to the specified power.</summary>
        public static float Exp(this float value) => (float)SysMath.Exp((double)value);
        /// <summary>Returns the largest integer less than or equal to the specified value.</summary>
        public static float Floor(this float value) => (float)SysMath.Floor((double)value);
        /// <summary>Returns the largest integer less than or equal to the specified value.</summary>
        public static int FloorToInt(this float value) => value >= 0 ? (int)value : (int)value - 1;
        /// <summary>Returns the smallest integer greater than or equal to the specified value.</summary>
        public static float Ceiling(this float value) => (float)SysMath.Ceiling((double)value);
        /// <summary>Returns the smallest power of 2 that is greater than the value.</summary>
        //public static int PowerOfTwoCeiling(this float value) => PowerOfTwoCeiling((double)value);
        /// <summary>Returns the remainder resulting from the division of a specified number by another specified number.</summary>
        public static float IEEERemainder(float dividend, float divisor) => (float)SysMath.IEEERemainder((double)dividend, (double)divisor);
        /// <summary>Returns true if the value is approximately an integer value.</summary>
        public static bool IsInteger(this float value) => Abs(value % 1) <= FloatLargeEpsilon;
        /// <summary>Returns the logarithm of a specified number in a specified base.</summary>
        public static float Log(this float a, float b) => (float)SysMath.Log((double)a, (double)b);
        /// <summary>Returns the natural (base e) logarithm of a specified number.</summary>
        public static float Log(this float value) => (float)SysMath.Log((double)value);
        /// <summary>Returns the base 10 logarithm of a specified number.</summary>
        public static float Log10(this float value) => (float)SysMath.Log10((double)value);
        /// <summary>Returns the number of decimal places required to represent a certain number of significant digits.</summary>
        public static int GetDecimalDigits(this float value, int digits, bool truncateExactZero = false) => GetDecimalDigits((double)value, digits, truncateExactZero);
        /// <summary>Gets the order of magnitude needed to bring this value to within the order scale.</summary>
        /// <param name="value">The original value to scale.</param>
        /// <param name="orderScale">For each order increment, scale the value by this amount.</param>
        /// <param name="orderIncrement">The increment between orders of magnitude.</param>
        /// <param name="minOrder">Minimum order to return.</param>
        /// <param name="maxOrder">Maximum order to return.</param>
        /// <returns>The total order increments by which the value needed to scale.</returns>
        private static int GetOrderOfMagnitude(this float value, float orderScale = 10f, int orderIncrement = 1, int minOrder = int.MinValue, int maxOrder = int.MaxValue) => GetOrderOfMagnitude((double)value, (double)orderScale, orderIncrement, minOrder, maxOrder);
        /// <summary>Gets the order of magnitude needed to bring this value to within the order scale.</summary>
        /// <param name="value">The original value to scale.</param>
        /// <param name="scaledValue">The value after having been scaled.</param>
        /// <param name="orderScale">For each order increment, scale the value by this amount.</param>
        /// <param name="orderIncrement">The increment between orders of magnitude.</param>
        /// <param name="minOrder">Minimum order to return.</param>
        /// <param name="maxOrder">Maximum order to return.</param>
        /// <returns>The total order increments by which the value needed to scale.</returns>
        private static int GetOrderOfMagnitude(this float value, out float scaledValue, float orderScale = 10f, int orderIncrement = 1, int minOrder = int.MinValue, int maxOrder = int.MaxValue)
        {
            int result = GetOrderOfMagnitude(value, out scaledValue, orderScale, orderIncrement, minOrder, maxOrder);
            return result;
        }
        /// <summary>Formats a number to a string using standard notation.</summary>
        public static string Format(this float value, int significantDigits, bool truncateExactZero = false) => Format((double)value, significantDigits, truncateExactZero);
        /// <summary>Formats a number to a string using scientific notation.</summary>
        public static string Format(this float value, int significantDigits, int orderThreshold, bool truncateExactZero = false, bool isIntegral = false) => Format((double)value, significantDigits, orderThreshold, truncateExactZero, isIntegral);
        /// <summary>Formats a number to a string using SI prefixes to scale the value.</summary>
        public static string Format(this float value, int significantDigits, double siOrderScale, bool truncateExactZero = false, bool isIntegral = false) => Format((double)value, significantDigits, siOrderScale, truncateExactZero, isIntegral);
        /// <summary>Returns the larger of two values.</summary>
        public static float Max(float a, float b) => SysMath.Max(a, b);
        /// <summary>Returns the larger of three values.</summary>
        public static float Max(float a, float b, float c) => SysMath.Max(a, SysMath.Max(b, c));
        /// <summary>Returns the larger of four values.</summary>
        public static float Max(float a, float b, float c, float d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the larger of two values, smoothing if they're within range of each other.</summary>
        public static float SoftMax(float a, float b, float range = 1) => (float)SoftMax((double)a, (double)b, (double)range);
        /// <summary>Returns the smaller of two values.</summary>
        public static float Min(float a, float b) => SysMath.Min(a, b);
        /// <summary>Returns the smaller of three values.</summary>
        public static float Min(float a, float b, float c) => SysMath.Min(a, SysMath.Min(b, c));
        /// <summary>Returns the smaller of four values.</summary>
        public static float Min(float a, float b, float c, float d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Returns the smaller of two values, smoothing if they're within range of each other.</summary>
        public static float SoftMin(float a, float b, float range = 1) => (float)SoftMin((double)a, (double)b, (double)range);
        /// <summary>Generates a random number in the range [0, max).</summary>
        public static float Random(float max = 1.0f) => (float)Random((double)max);
        /// <summary>Generates a random number in the range [min, max).</summary>
        public static float Random(float min, float max) => (float)Random((double)min, (double)max);
        /// <summary>Generates a random number according to an exponential decay curve.</summary>
        public static float DecayRandom(float halflife = 1.0f) => (float)DecayRandom((double)halflife);
        /// <summary>Generates a random number according to a logarithmic range.</summary>
        public static float LogRandom(float min, float max) => (float)LogRandom((double)min, (double)max);
        /// <summary>Returns the Rectified Linear Unit function of a value.</summary>
        public static float ReLU(this float value) => value <= 0 ? 0 : value;
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
        /// <summary>
        /// Quake Fast Inverse Square Root.
        /// https://en.wikipedia.org/wiki/Fast_inverse_square_root
        /// Error: +0.1752%/-0%
        /// </summary>
        public static unsafe float InvSqrt(this float value)// => SysMath.InvSqrt(value);
        {
            float half = 0.5f * value;
            *(int*)&value = 0x5F3759DF - (*(int*)&value >> 1);
            value *= (1.5f - half * value * value);
            //value *= (1.5f - half * value * value);
            return value;
        }
        /// <summary>Rounds a value to a specified number of fractional digits. A parameter specifies how to round the value if it is midway between two numbers.</summary>
        public static float Round(this float value, int digits, MidpointRounding mode) => (float)SysMath.Round((double)value, digits, mode);
        /// <summary>Rounds a value to the nearest integer. A parameter specifies how to round the value if it is midway between two numbers.</summary>
        public static float Round(this float value, MidpointRounding mode) => (float)SysMath.Round((double)value, mode);
        /// <summary>Rounds a value to a specified number of fractional digits.</summary>
        public static float Round(this float value, int digits) => (float)SysMath.Round((double)value, digits);
        /// <summary>Rounds a value to the nearest integral value.</summary>
        public static float Round(this float value) => (float)SysMath.Round((double)value);
        /// <summary>Rounds a value to the nearest integer.</summary>
        public static int RoundToInt(this float value) => (int)value.Round();
        /// <summary>Rounds a value to the nearest long integer.</summary>
        public static long RoundToLong(this float value) => (int)value.Round();
        /// <summary>Performs division as normal, but if the denominator is 0, returns +/-Infinity for a nonzero numerator, otherwise returns 0.</summary>
        public static float SafeDivide(this float numerator, float denominator) => SafeDivide(numerator, denominator, numerator >= 0 ? float.PositiveInfinity : float.NegativeInfinity);
        /// <summary>Performs division as normal, but if the denominator is 0, returns the default value.</summary>
        public static float SafeDivide(this float numerator, float denominator, float defaultValue) => denominator == 0 ? defaultValue : numerator / denominator;
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
        /// <summary>Gauss Error Function</summary>
        public static float Erf(this float value) => (float)Erf(value);
        /// <summary>Complementary Gauss Error Function</summary>
        public static float Erfc(this float value) => (float)Erfc((double)value);
        /// <summary>Gauss Probability Distribution Function</summary>
        public static float NormalPdf(this float value) => (float)NormalPdf((double)value);
        /// <summary>Gauss Cumulative Distribution Function</summary>
        public static float NormalCdf(this float value) => (float)NormalCdf((double)value);
        /// <summary>Calculates the sigmoid of a number using an approximation.</summary>
        public static float SmoothHeaviside(this float value) => (float)SmoothHeaviside((double)value);
        /// <summary>Smoothly steps from min to max entirely within the range [min, max].</summary>
        public static float SmoothClamp(this float value, float min = 0f, float max = 1f) => (float)SmoothClamp((double)value, (double)min, (double)max);
        /// <summary>Smoothly clamps to the range with some spillover beyond the range [min, max].</summary>
        public static float SoftClamp(this float value, float min = 0f, float max = 1f) => (float)SoftClamp((double)value, (double)min, (double)max);
        /// <summary>Calculates the sigmoid of a number.</summary>
        public static float Sigmoid(this float value) => (float)(1.0 / (1.0 + SysMath.Exp((double)-value)));
        /// <summary>Calculates the sigmoid of a number using an approximation.</summary>
        public static float FastSigmoid(this float value) { double v = value; return (float)(v / (1.0 + SysMath.Abs(v))); }
        /// <summary>Incrementally moves a value towards a target.</summary>
        public static float MoveTowards(this float current, float target, float maxDelta)
        {
            if(Abs(target - current) <= maxDelta)
            {
                return target;
            }
            return current + Sign(target - current) * maxDelta;
        }
        /// <summary>Incrementally moves an angle towards a target.</summary>
        public static float MoveTowardsAngle(this float current, float target, float maxDelta)
        {
            float deltaAngle = DeltaAngle(current, target);
            if(-maxDelta < deltaAngle && deltaAngle < maxDelta)
            {
                return target;
            }
            target = current + deltaAngle;
            return MoveTowards(current, target, maxDelta);
        }
        /// <summary>Smoothly increments from min to max.</summary>
        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp(t, 0, 1);
            t = -2 * t * t * t + 3 * t * t;
            return to * t + from * (1 - t);
        }
        /// <summary>Smoothly moves a value towards a target with inertia.</summary>
        public static float SmoothDamp(this float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Max(0.0001f, smoothTime);
            float omega = 2 / smoothTime;
            float x = omega * deltaTime;
            float exp = 1 / (1 + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            target = current - change;
            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;

            // Prevent overshooting
            if(originalTo - current > 0 == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }
            return output;
        }
        /// <summary>Smoothly moves an angle towards a target with inertia.</summary>
        public static float SmoothDampAngle(this float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            double currentVelocityD = (double)currentVelocity;
            float result = (float)SmoothDampAngle((double)current, (double)target, ref currentVelocityD, (double)smoothTime, (double)maxSpeed, (double)deltaTime);
            currentVelocity = (float)currentVelocityD;
            return result;
        }
        /// <summary>Alternative implementation that smoothly moves an angle towards a target with inertia.</summary>
        public static float SmoothDampAngle2(this float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            target = current + DeltaAngle(current, target);

            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Max(0.0001f, smoothTime);
            float omega = 2 / smoothTime;
            float x = omega * deltaTime;
            float exp = 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;
            change = Clamp(change, -maxChange, maxChange);
            target = current - change;
            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;

            // Prevent overshooting
            if(originalTo - current > 0.0 == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }
            return output;
        }
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
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static sbyte Repeat(this sbyte value, sbyte min = 0, sbyte max = 1) => (sbyte)Repeat((int)value, (int)min, (int)max);
        /// <summary>Returns a mask that spans the value's MSB to bit 0 (LSB).</summary>
        public static sbyte Mask(this sbyte value) => (sbyte)Mask((byte)value);
        /// <summary>Returns the larger of two values.</summary>
        public static sbyte Max(sbyte a, sbyte b) => SysMath.Max(a, b);
        public static sbyte Max(sbyte a, sbyte b, sbyte c) => SysMath.Max(a, SysMath.Max(b, c));
        public static sbyte Max(sbyte a, sbyte b, sbyte c, sbyte d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static sbyte Min(sbyte a, sbyte b) => SysMath.Min(a, b);
        public static sbyte Min(sbyte a, sbyte b, sbyte c) => SysMath.Min(a, SysMath.Min(b, c));
        public static sbyte Min(sbyte a, sbyte b, sbyte c, sbyte d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Generates a random integer in the range [0, max).</summary>
        public static sbyte Random(sbyte max = sbyte.MaxValue) => (sbyte)random.Next(0, max);
        /// <summary>Generates a random integer in the range [min, max).</summary>
        public static sbyte Random(sbyte min, sbyte max) => (sbyte)random.Next(min, max);
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
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static byte Repeat(this byte value, byte min = 0, byte max = 1) => (byte)Repeat((int)value, (int)min, (int)max);
        /// <summary>Returns a mask that spans the value's MSB to bit 0 (LSB).</summary>
        public static byte Mask(this byte value)
        {
            if(value > 1)
            {
                for(int msbIndex = 2; msbIndex < 8; ++msbIndex)
                {
                    byte mask = (byte)(byte.MaxValue << msbIndex);
                    if((mask & value) == 0)
                    {
                        return (byte)(~mask);
                    }
                }
            }
            return value;
        }
        /// <summary>Returns the larger of two values.</summary>
        public static byte Max(byte a, byte b) => SysMath.Max(a, b);
        public static byte Max(byte a, byte b, byte c) => SysMath.Max(a, SysMath.Max(b, c));
        public static byte Max(byte a, byte b, byte c, byte d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static byte Min(byte a, byte b) => SysMath.Min(a, b);
        public static byte Min(byte a, byte b, byte c) => SysMath.Min(a, SysMath.Min(b, c));
        public static byte Min(byte a, byte b, byte c, byte d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Generates a random integer in the range [0, max).</summary>
        public static byte Random(byte max = byte.MaxValue) => (byte)random.Next(0, max);
        /// <summary>Generates a random integer in the range [min, max).</summary>
        public static byte Random(byte min, byte max) => (byte)random.Next(min, max);
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
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static short Repeat(this short value, short min = 0, short max = 1) => (short)Repeat((int)value, (int)min, (int)max);
        /// <summary>Returns a mask that spans the value's MSB to bit 0 (LSB).</summary>
        public static short Mask(this short value) => (short)Mask((ushort)value);
        /// <summary>Returns the larger of two values.</summary>
        public static short Max(short a, short b) => SysMath.Max(a, b);
        public static short Max(short a, short b, short c) => SysMath.Max(a, SysMath.Max(b, c));
        public static short Max(short a, short b, short c, short d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static short Min(short a, short b) => SysMath.Min(a, b);
        public static short Min(short a, short b, short c) => SysMath.Min(a, SysMath.Min(b, c));
        public static short Min(short a, short b, short c, short d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Generates a random integer in the range [0, max).</summary>
        public static short Random(short max = short.MaxValue) => (short)random.Next(0, max);
        /// <summary>Generates a random integer in the range [min, max).</summary>
        public static short Random(short min, short max) => (short)random.Next(min, max);
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
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static ushort Repeat(this ushort value, ushort min = 0, ushort max = 1) => (ushort)Repeat((int)value, (int)min, (int)max);
        /// <summary>Returns a mask that spans the value's MSB to bit 0 (LSB).</summary>
        public static ushort Mask(this ushort value)
        {
            if(value > 1)
            {
                for(int msbIndex = 2; msbIndex < 16; ++msbIndex)
                {
                    ushort mask = (ushort)(ushort.MaxValue << msbIndex);
                    if((mask & value) == 0)
                    {
                        return (ushort)(~mask);
                    }
                }
            }
            return value;
        }
        /// <summary>Returns the larger of two values.</summary>
        public static ushort Max(ushort a, ushort b) => SysMath.Max(a, b);
        public static ushort Max(ushort a, ushort b, ushort c) => SysMath.Max(a, SysMath.Max(b, c));
        public static ushort Max(ushort a, ushort b, ushort c, ushort d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static ushort Min(ushort a, ushort b) => SysMath.Min(a, b);
        public static ushort Min(ushort a, ushort b, ushort c) => SysMath.Min(a, SysMath.Min(b, c));
        public static ushort Min(ushort a, ushort b, ushort c, ushort d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Generates a random integer in the range [0, max).</summary>
        public static ushort Random(ushort max = ushort.MaxValue) => (ushort)random.Next(0, max);
        /// <summary>Generates a random integer in the range [min, max).</summary>
        public static ushort Random(ushort min, ushort max) => (ushort)random.Next(min, max);
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
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static int Repeat(this int value, int min = 0, int max = 1) => (value - min) % (max - min) + min;
        /// <summary>Returns a mask that spans the value's MSB to bit 0 (LSB).</summary>
        public static int Mask(this int value) => (int)Mask((uint)value);
        /// <summary>Returns the larger of two values.</summary>
        public static int Max(int a, int b) => SysMath.Max(a, b);
        public static int Max(int a, int b, int c) => SysMath.Max(a, SysMath.Max(b, c));
        public static int Max(int a, int b, int c, int d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static int Min(int a, int b) => SysMath.Min(a, b);
        public static int Min(int a, int b, int c) => SysMath.Min(a, SysMath.Min(b, c));
        public static int Min(int a, int b, int c, int d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Generates a random integer in the range [0, max).</summary>
        public static int Random(int max = int.MaxValue) => random.Next(0, max);
        /// <summary>Generates a random integer in the range [min, max).</summary>
        public static int Random(int min, int max) => random.Next(min, max);
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
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static uint Repeat(this uint value, uint min = 0, uint max = 1) => (value - min) % (max - min) + min;
        /// <summary>Returns a mask that spans the value's MSB to bit 0 (LSB).</summary>
        public static uint Mask(this uint value)
        {
            if(value > 1)
            {
                for(int msbIndex = 2; msbIndex < 16; ++msbIndex)
                {
                    uint mask = (uint)(uint.MaxValue << msbIndex);
                    if((mask & value) == 0)
                    {
                        return (uint)(~mask);
                    }
                }
            }
            return value;
        }
        /// <summary>Returns the larger of two values.</summary>
        public static uint Max(uint a, uint b) => SysMath.Max(a, b);
        public static uint Max(uint a, uint b, uint c) => SysMath.Max(a, SysMath.Max(b, c));
        public static uint Max(uint a, uint b, uint c, uint d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static uint Min(uint a, uint b) => SysMath.Min(a, b);
        public static uint Min(uint a, uint b, uint c) => SysMath.Min(a, SysMath.Min(b, c));
        public static uint Min(uint a, uint b, uint c, uint d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Generates a random integer in the range [0, max).</summary>
        public static uint Random(uint max = uint.MaxValue) => Random(0, max);
        /// <summary>Generates a random integer in the range [min, max).</summary>
        public static uint Random(uint min, uint max) => (uint)((random.NextDouble() * (max - min)) + min);
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
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static long Repeat(this long value, long min = 0, long max = 1) => (value - min) % (max - min) + min;
        /// <summary>Returns a mask that spans the value's MSB to bit 0 (LSB).</summary>
        public static long Mask(this long value) => (long)Mask((ulong)value);
        /// <summary>Returns the larger of two values.</summary>
        public static long Max(long a, long b) => SysMath.Max(a, b);
        public static long Max(long a, long b, long c) => SysMath.Max(a, SysMath.Max(b, c));
        public static long Max(long a, long b, long c, long d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static long Min(long a, long b) => SysMath.Min(a, b);
        public static long Min(long a, long b, long c) => SysMath.Min(a, SysMath.Min(b, c));
        public static long Min(long a, long b, long c, long d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Generates a random integer in the range [0, max).</summary>
        public static long Random(long max = long.MaxValue) => (long)Random(0, (ulong)max);
        /// <summary>Generates a random integer in the range [min, max).</summary>
        public static long Random(long min, long max) => (long)Random(0, (ulong)(max - min)) + min;
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
        /// <summary>Returns the modulus of the value and the range [min, max] offset by min.</summary>
        public static ulong Repeat(this ulong value, ulong min = 0, ulong max = 1) => (value - min) % (max - min) + min;
        /// <summary>Returns a mask that spans the value's MSB to bit 0 (LSB).</summary>
        public static ulong Mask(this ulong value)
        {
            if(value > 1)
            {
                for(int msbIndex = 2; msbIndex < 16; ++msbIndex)
                {
                    ulong mask = (ulong)(ulong.MaxValue << msbIndex);
                    if((mask & value) == 0)
                    {
                        return (ulong)(~mask);
                    }
                }
            }
            return value;
        }
        /// <summary>Returns the larger of two values.</summary>
        public static ulong Max(ulong a, ulong b) => SysMath.Max(a, b);
        public static ulong Max(ulong a, ulong b, ulong c) => SysMath.Max(a, SysMath.Max(b, c));
        public static ulong Max(ulong a, ulong b, ulong c, ulong d) => SysMath.Max(SysMath.Max(a, b), SysMath.Max(c, d));
        /// <summary>Returns the smaller of two values.</summary>
        public static ulong Min(ulong a, ulong b) => SysMath.Min(a, b);
        public static ulong Min(ulong a, ulong b, ulong c) => SysMath.Min(a, SysMath.Min(b, c));
        public static ulong Min(ulong a, ulong b, ulong c, ulong d) => SysMath.Min(SysMath.Min(a, b), SysMath.Min(c, d));
        /// <summary>Generates a random integer in the range [0, max).</summary>
        public static ulong Random(ulong max = ulong.MaxValue) => Random(0, max);
        /// <summary>Generates a random integer in the range [min, max).</summary>
        public static ulong Random(ulong min, ulong max)
        {
            ulong range = max - min;
            ulong mask = Mask(range);
            byte[] bytes = new byte[8];
            for(int i = 0; i < 100; i++)
            {
                random.NextBytes(bytes);
                ulong result = mask & bytes.ToUlong();
                if(result < range)
                {
                    return result + min;
                }
            }
            throw new ArithmeticException("Failed to generate a valid random value");
        }
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