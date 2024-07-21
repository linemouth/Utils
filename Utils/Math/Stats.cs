using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class Stats
    {
        public double[] Quartiles
        {
            get
            {
                Recalculate();
                return quartiles.ToArray();
            }
        }
        public double Min
        {
            get
            {
                Recalculate();
                return quartiles[0];
            }
        }
        public double Q1
        {
            get
            {
                Recalculate();
                return quartiles[1];
            }
        }
        public double Median
        {
            get
            {
                Recalculate();
                return quartiles[2];
            }
        }
        public double Q3
        {
            get
            {
                Recalculate();
                return quartiles[3];
            }
        }
        public double Max
        {
            get
            {
                Recalculate();
                return quartiles[4];
            }
        }
        public double Mean
        {
            get
            {
                Recalculate();
                return mean;
            }
        }
        public double Sigma
        {
            get
            {
                Recalculate();
                return sigma;
            }
        }

        private double[] quartiles;
        private double mean;
        private double sigma;
        private List<double> items;
        private bool changed = true;

        public Stats() : this(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN) { }
        public Stats(IEnumerable<double> items) => this.items = items.ToList();
        public Stats(double min, double median, double max) : this(min, double.NaN, median, double.NaN, max, (min + median + max) / 3.0) { }
        public Stats(double min, double median, double max, double mean) : this(min, double.NaN, median, double.NaN, max, mean) { }
        public Stats(double min, double q1, double median, double q3, double max) : this(min, q1, median, q3, max, (min + q1 + median + q3 + max) / 5.0) { }
        public Stats(double min, double q1, double median, double q3, double max, double mean)
        {
            quartiles = new double[] { min, q1, median, q3, max };
            this.mean = mean;
            changed = false;
        }
        public void Add(double item)
        {
            if(items == null)
            {
                items = new List<double>();
            }
            items.Add(item);
            changed = true;
        }
        public void Recalculate()
        {
            lock (this)
            {
                int count = items?.Count ?? 0;
                if (changed && count > 0)
                {
                    items.Sort();
                    quartiles[0] = items.First();
                    quartiles[1] = items[(count - 1) / 4];
                    quartiles[2] = items[(count - 1) / 2];
                    quartiles[3] = items[(count * 3 - 1) / 4];
                    quartiles[4] = items.Last();
                    mean = items.Sum(v => v) / count;
                    sigma = count > 1 ? Math.Sqrt(items.Sum(v => Math.Sqr(v - mean)) / (count - 1)) : 0;
                    changed = false;
                }
            }
        }
        public override string ToString()
        {
            Recalculate();
            return $"[{Min} {Median} {Max}] ({Mean})";
        }
        public string ToString(bool extraQuartiles, bool stdDev, string columnSeparator = "") => ToString(null, extraQuartiles, stdDev, columnSeparator);
        public string ToString(string format, bool extraQuartiles, bool stdDev, string columnSeparator = "")
        {
            Recalculate();
            IEnumerable<string> quartiles = Enumerable.Select(extraQuartiles ? new[] { Min, Q1, Median, Q3, Max } : new[] { Min, Median, Max }, value => format != null ? value.ToString(format) : value.ToString());
            string mean = format != null ? Mean.ToString(format) : Mean.ToString();
            string sigma = format != null ? Sigma.ToString(format) : Sigma.ToString();

            string buffer;
            if(columnSeparator != null)
            {
                buffer = $"{string.Join(columnSeparator, quartiles)}{columnSeparator}{mean}";
                if(stdDev)
                {
                    buffer += $"{columnSeparator}{sigma}";
                }
            }
            else
            {
                buffer = $"[{string.Join(" ", quartiles)}] ({mean})";
                if(stdDev)
                {
                    buffer += $" σ: {sigma}";
                }
            }
            return buffer;
        }
    }
}
