using System;
using System.Collections.Generic;
using System.Linq;
using static Utils.Histogram;

namespace Utils
{
    public class Histogram
    {
        /// <summary>Represents a bin in the histogram.</summary>
        public struct Bin
        {
            /// <summary>Gets the minimum value of the bin.</summary>
            public readonly double Min;
            /// <summary>Gets the maximum value of the bin.</summary>
            public readonly double Max;
            /// <summary>Gets the center value of the bin.</summary>
            public double Center => 0.5 * (Min + Max);
            /// <summary>Gets the width of the bin.</summary>
            public double Width => Max - Min;
            /// <summary>Gets the count of samples in the bin.</summary>
            public readonly ulong Count;
            /// <summary>Gets the count of samples below this bin.</summary>
            public readonly ulong CountBelow;
            /// <summary>Gets the count of samples above this bin.</summary>
            public ulong CountAbove => SampleCount - CountBelow - Count;
            /// <summary>Gets the proportion of samples in the bin.</summary>
            public double Value => (double)Count / SampleCount;
            /// <summary>Gets the proportion of samples below this bin.</summary>
            public double Below => (double)CountBelow / SampleCount;
            /// <summary>Gets the proportion of samples above this bin.</summary>
            public double Above => (double)CountAbove / SampleCount;

            private readonly ulong SampleCount;

            /// <summary>Initializes a new instance of the <see cref="Bin"/> struct.</summary>
            /// <param name="min">The minimum value of the bin.</param>
            /// <param name="max">The maximum value of the bin.</param>
            /// <param name="count">The count of samples in the bin.</param>
            /// <param name="sampleCount">The total number of samples.</param>
            /// <param name="countBelow">The count of samples below this bin.</param>
            public Bin(double min, double max, ulong count, ulong sampleCount, ulong countBelow)
            {
                Min = min;
                Max = max;
                Count = count;
                CountBelow = countBelow;
                SampleCount = sampleCount;
            }
        }

        /// <summary>Gets or sets the number of bins in the histogram.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when bin count is less than or equal to zero.</exception>
        public int BinCount
        {
            get => bins.Length;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("Bin count must be greater than zero.");
                }
                if (bins == null || value != bins.Length)
                {
                    bins = new ulong[value];
                }
            }
        }
        /// <summary>Gets the minimum value of the histogram range.</summary>
        public double Min { get; private set; }
        /// <summary>Gets the maximum value of the histogram range.</summary>
        public double Max { get; private set; }
        /// <summary>Gets the mean value of the samples in the histogram.</summary>
        public double Mean
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < BinCount; ++i)
                {
                    double center = (i + 0.5) * Range / BinCount + Min;
                    sum += center * bins[i];
                }
                return sum / SampleCount;
            }
        }
        /// <summary>Gets the range (difference between maximum and minimum values) of the histogram.</summary>
        public double Range => Max - Min;
        /// <summary>Gets the width of each bin in the histogram.</summary>
        public double BinWidth => Range / BinCount;
        /// <summary>Gets the total number of samples in the histogram.</summary>
        public ulong SampleCount { get; private set; }
        /// <summary>Gets the peak (maximum count) of the histogram.</summary>
        public ulong Peak { get; private set; }
        /// <summary>Limits the accumulation of samples to those within the range [Min, Max]. Otherwise, outliers will be added to the nearest bin.</summary>
        public bool LimitToRange { get; set; }
        /// <summary>Gets the <see cref="Bin"/> at the specified index.</summary>
        /// <param name="index">The index of the bin.</param>
        /// <returns>The <see cref="Bin"/> at the specified index.</returns>
        public Bin this[int index]
        {
            get
            {
                ulong samplesBelow = 0;
                for(int i = 0; i < index; ++i)
                {
                    samplesBelow += bins[i];
                }
                double scale = Range / BinCount;
                double min = Min + index * scale;
                double max = Min * (index + 1) * scale;
                return new Bin(min, max, bins[index], SampleCount, samplesBelow);
            }
        }

        private const int defaultBinCount = 128;
        private ulong[] bins;

        /// <summary>Initializes a new instance of the <see cref="Histogram"/> class with the specified bin count, minimum, and maximum values.</summary>
        /// <param name="binCount">The number of bins.</param>
        /// <param name="min">The minimum value of the histogram range.</param>
        /// <param name="max">The maximum value of the histogram range.</param>
        /// <param name="limitToRange">If true, only accumulate samples within the range; otherwise, include all samples, placing those outside the range into the closest bin.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when bin count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">Thrown when maximum is less than or equal to minimum.</exception>
        public Histogram(int binCount, double min, double max, bool limitToRange = true)
        {
            BinCount = binCount;
            SetRange(min, max);
            LimitToRange = limitToRange;
        }
        /// <summary>Initializes a new instance of the <see cref="Histogram"/> class with the default bin count, minimum, and maximum values.</summary>
        /// <param name="min">The minimum value of the histogram range.</param>
        /// <param name="max">The maximum value of the histogram range.</param>
        /// <param name="limitToRange">If true, only accumulate samples within the range; otherwise, include all samples, placing those outside the range into the closest bin.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when bin count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">Thrown when maximum is less than or equal to minimum.</exception>
        public Histogram(double min, double max, bool limitToRange = true) : this(defaultBinCount, min, max, limitToRange) { }
        /// <summary>Initializes a new instance of the <see cref="Histogram"/> class with the specified data, default bin count, minimum, and maximum values.</summary>
        /// <param name="data">The data to evaluate.</param>
        /// <param name="min">The minimum value of the histogram range.</param>
        /// <param name="max">The maximum value of the histogram range.</param>
        /// <param name="limitToRange">If true, only accumulate samples within the range; otherwise, include all samples, placing those outside the range into the closest bin.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when bin count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">Thrown when maximum is less than or equal to minimum.</exception>
        public Histogram(IEnumerable<double> data, double min, double max, bool limitToRange = true) : this(data, defaultBinCount, min, max, limitToRange) { }
        /// <summary>Initializes a new instance of the <see cref="Histogram"/> class with the specified data, bin count, minimum, and maximum values.</summary>
        /// <param name="data">The data to evaluate.</param>
        /// <param name="binCount">The number of bins.</param>
        /// <param name="min">The minimum value of the histogram range.</param>
        /// <param name="max">The maximum value of the histogram range.</param>
        /// <param name="limitToRange">If true, only accumulate samples within the range; otherwise, include all samples, placing those outside the range into the closest bin.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when bin count is less than or equal to zero.</exception>
        /// <exception cref="ArgumentException">Thrown when maximum is less than or equal to minimum.</exception>
        public Histogram(IEnumerable<double> data, int binCount, double min, double max, bool limitToRange = true) : this(binCount, min, max, limitToRange)
        {
            Evaluate(data);
        }
        /// <summary>Sets the range of the histogram.</summary>
        /// <param name="min">The minimum value of the histogram range.</param>
        /// <param name="max">The maximum value of the histogram range.</param>
        /// <exception cref="ArgumentException">Thrown when maximum is less than or equal to minimum.</exception>
        public void SetRange(double min, double max)
        {
            if (max <= min)
            {
                throw new ArgumentException("Range maximum must be greater than minimum.");
            }

            Min = min;
            Max = max;
        }
        /// <summary>Evaluates the histogram with the specified data.</summary>
        /// <param name="data">The data to evaluate.</param>
        public void Evaluate(IEnumerable<double> samples)
        {
            Array.Clear(bins, 0, bins.Length);
            Push(samples);
        }
        public void Push(IEnumerable<double> samples)
        {
            double binWidth = BinWidth;
            SampleCount = 0;

            // Accumulate data.
            if (LimitToRange)
            {
                foreach (var sample in samples)
                {
                    if (sample >= Min && sample <= Max)
                    {
                        int binIndex = (int)((sample - Min) / binWidth);
                        if (binIndex >= 0 && binIndex < BinCount)
                        {
                            ++bins[binIndex];
                        }
                    }
                }
            }
            else
            {
                int lastBinIndex = BinCount - 1;
                foreach (var value in samples)
                {
                    if (value <= Min)
                    {
                        ++bins[0];
                    }
                    else if (value >= Max)
                    {
                        ++bins[lastBinIndex];
                    }
                    else
                    {
                        int binIndex = (int)((value - Min) / binWidth);
                        ++bins[Math.Clamp(binIndex, 0, lastBinIndex)];
                    }
                }
            }

            // Determine peak value and total sample count.
            for (int i = 0; i < BinCount; ++i)
            {
                SampleCount += bins[i];
                ulong binValue = bins[i];
                if (bins[i] > Peak)
                {
                    Peak = bins[i];
                }
            }
        }
        public void Push(double sample)
        {
            Push(new double[] { sample });
        }
        /// <summary>Gets the value at the specified quantile.</summary>
        /// <param name="quantile">The quantile (between 0 and 1).</param>
        /// <returns>The value at the specified quantile.</returns>
        public double GetQuantile(double quantile)
        {
            if(quantile <= 0)
            {
                return Min;
            }
            if(quantile >= 1)
            {
                return Max;
            }

            ulong targetCount = (ulong)(quantile * SampleCount);
            ulong accumulatedCount = 0;

            for (int i = 0; i < BinCount; ++i)
            {
                accumulatedCount += bins[i];
                if (accumulatedCount >= targetCount)
                {
                    // Perform linear interpolation within the bin.
                    Bin bin = this[i];
                    return bin.Min + bin.Width * (accumulatedCount - bin.CountBelow) / bin.Count;
                }
            }

            return Max;
        }
    }
}
