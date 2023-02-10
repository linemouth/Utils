using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public struct Float2
    {
        /// <summary>Shorthand for writing Float2(0, 0).</summary>
        public static Float2 zero => new Float2(0, 0);
        /// <summary>Shorthand for writing Float2(1, 1).</summary>
        public static Float2 one => new Float2(1, 1);
        /// <summary>Shorthand for writing Float2(0, 1).</summary>
        public static Float2 up => new Float2(0, 1);
        /// <summary>Shorthand for writing Float2(0, -1).</summary>
        public static Float2 down => new Float2(0, -1);
        /// <summary>Shorthand for writing Float2(-1, 0).</summary>
        public static Float2 left => new Float2(-1, 0);
        /// <summary>Shorthand for writing Float2(1, 0).</summary>
        public static Float2 right => new Float2(1, 0);
        public float x;
        public float y;
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return this.x;
                    case 1: return this.y;
                    default: throw new IndexOutOfRangeException("Invalid Float2 index!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: this.x = value; break;
                    case 1: this.y = value; break;
                    default: throw new IndexOutOfRangeException("Invalid Float2 index!");
                }
            }
        }
        /// <summary>Returns the length of this vector.</summary>
        public float Magnitude => (x * x + y * y).Sqrt();
        /// <summary>Returns the squared length of this vector.</summary>
        public float SqrMagnitude => (x * x + y * y);
        /// <summary>Returns the taxi distance of this vector.</summary>
        public float AbsSum => Math.Abs(x) + Math.Abs(y);

        /// <summary>Constructs a new vector with given x, y components.</summary>
        public Float2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public static Float2 operator +(Float2 a, Float2 b) => new Float2(a.x + b.x, a.y + b.y);
        public static Float2 operator -(Float2 a, Float2 b) => new Float2(a.x - b.x, a.y - b.y);
        public static Float2 operator -(Float2 a) => new Float2(-a.x, -a.y);
        public static Float2 operator *(Float2 a, float d) => new Float2(a.x * d, a.y * d);
        public static Float2 operator *(float d, Float2 a) => new Float2(a.x * d, a.y * d);
        public static Float2 operator /(Float2 a, float d) => new Float2(a.x / d, a.y / d);
        public static bool operator ==(Float2 a, Float2 b) => (a - b).SqrMagnitude < Math.DoubleLargeEpsilon;
        public static bool operator !=(Float2 a, Float2 b) => (a - b).SqrMagnitude >= Math.DoubleLargeEpsilon;
        /// <summary>Reflects a vector off the vector defined by a normal.</summary>
        public static Float2 Reflect(Float2 inDirection, Float2 inNormal) => (-2f * Dot(inNormal, inDirection) * inNormal) + inDirection;
        /// <summary>Dot Product of two vectors.</summary>
        public static float Dot(Float2 a, Float2 b) => a.x * b.x + a.y * b.y;
        /// <summary>Returns the angle in degrees between from and to.</summary>
        public static float Angle(Float2 from, Float2 to) => (float)(Math.Acos(Math.Clamp(Dot(from.Normalized(), to.Normalized()), -1, 1)) * Math.RadToDeg);
        /// <summary>Returns the distance between a and b.</summary>
        public static float Distance(Float2 a, Float2 b) => (a - b).Magnitude;
        /// <summary>Returns a vector that is made from the smallest components of two vectors.</summary>
        public static Float2 Min(Float2 a, Float2 b) => new Float2(Math.Min(a.x, b.x), Math.Min(a.y, b.y));
        /// <summary>Returns a vector that is made from the largest components of two vectors.</summary>
        public static Float2 Max(Float2 a, Float2 b) => new Float2(Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        /// <summary>Set x and y components of an existing Float2.</summary>
        public void Set(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        /// <summary>Linearly interpolates between vectors a and b by t.</summary>
        public static Float2 Lerp(Float2 a, Float2 b, float t) => LerpUnclamped(a, b, Math.Clamp(t));
        /// <summary>Linearly interpolates between vectors a and b by t.</summary>
        public static Float2 LerpUnclamped(Float2 a, Float2 b, float t) => new Float2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
        /// <summary>Moves a point current towards target.</summary>
        public static Float2 MoveTowards(Float2 current, Float2 target, float maxDistanceDelta)
        {
            Float2 delta = target - current;
            float magnitude = delta.Magnitude;
            return (magnitude <= maxDistanceDelta || magnitude == 0) ? target : current + delta / magnitude * maxDistanceDelta;
        }
        /// <summary>Multiplies two vectors component-wise.</summary>
        public static Float2 Scale(Float2 a, Float2 b) => new Float2(a.x * b.x, a.y * b.y);
        /// <summary>Multiplies every component of this vector by the same component of scale.</summary>
        public void Scale(Float2 scale)
        {
            x *= scale.x;
            y *= scale.y;
        }
        /// <summary>Makes this vector have a magnitude of 1.</summary>
        public void Normalize()
        {
            float magnitude = Magnitude;
            this = magnitude > Math.DoubleLargeEpsilon ? this / magnitude : zero;
        }
        /// <summary>Returns this vector with a magnitude of 1.</summary>
        public Float2 Normalized()
        {
            float magnitude = Magnitude;
            return magnitude > Math.DoubleLargeEpsilon ? new Float2(x / magnitude, y / magnitude) : zero;
        }
        /// <summary>Returns a nicely formatted string for this vector.</summary>
        public override string ToString() => $"({x:F1}, {y:F1})";
        /// <summary>Formats a vector to a string using standard notation.</summary>
        public string Format(int significantDigits, bool truncateExactZero = false) => $"({x.Format(significantDigits, truncateExactZero)}, {y.Format(significantDigits, truncateExactZero)})";
        /// <summary>Formats a vector to a string using scientific notation.</summary>
        public string Format(int significantDigits, int orderThreshold, bool truncateExactZero = false, bool isIntegral = false) => $"({x.Format(significantDigits, orderThreshold, truncateExactZero, isIntegral)}, {y.Format(significantDigits, orderThreshold, truncateExactZero, isIntegral)})";
        /// <summary>Formats a vector to a string using SI prefixes to scale the values.</summary>
        public string Format(int significantDigits, float siOrderScale, bool truncateExactZero = false, bool isIntegral = false) => $"({x.Format(significantDigits, siOrderScale, truncateExactZero, isIntegral)}, {y.Format(significantDigits, siOrderScale, truncateExactZero, isIntegral)})";
        /// <summary>Returns a nicely formatted string for this vector.</summary>
        public string ToString(string format) => $"({x.ToString(format)}, {y.ToString(format)})";
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);
        public override bool Equals(object other) => other is Float2 vector2 && x.Equals(vector2.x) && y.Equals(vector2.y);
        /// <summary>Returns a copy of vector with its magnitude clamped to maxLength.</summary>
        public Float2 ClampedMagnitude(float maxLength) => (SqrMagnitude > (maxLength * maxLength)) ? (Normalized() * maxLength) : this;
        public static Float2 SmoothDamp(Float2 current, Float2 target, ref Float2 currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Math.Max(0.000001f, smoothTime);
            float num1 = 2 / smoothTime;
            float num2 = num1 * deltaTime;
            float num3 = 1 / (1 + num2 + 0.479999989271164f * num2 * num2 + 0.234999999403954f * num2 * num2 * num2);
            Float2 vector = current - target;
            Float2 vector2_1 = target;
            float maxLength = maxSpeed * smoothTime;
            Float2 vector2_2 = vector.ClampedMagnitude(maxLength);
            target = current - vector2_2;
            Float2 vector2_3 = (currentVelocity + num1 * vector2_2) * deltaTime;
            currentVelocity = (currentVelocity - num1 * vector2_3) * num3;
            Float2 vector2_4 = target + (vector2_2 + vector2_3) * num3;
            if (Dot(vector2_1 - current, vector2_4 - vector2_1) > 0)
            {
                vector2_4 = vector2_1;
                currentVelocity = (vector2_4 - vector2_1) / deltaTime;
            }
            return vector2_4;
        }
    }
}
