using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Queries
{
    public abstract class FloatOption : Option
    {
        public double Min
        {
            get => min;
            set
            {
                min = value;
                if(min > value)
                {
                    this.value = min;
                }
                if(min > max)
                {
                    max = min;
                }
                OnChanged();
            }
        }
        public double Max
        {
            get => max;
            set
            {
                max = value;
                if(max < value)
                {
                    this.value = max;
                }
                if(max < min)
                {
                    min = max;
                }
                OnChanged();
            }
        }
        public double Value
        {
            get => value;
            set
            {
                this.value = Math.Clamp(value, min, max);
                OnChanged();
            }
        }

        protected double min;
        protected double max;
        protected double value;

        public FloatOption(string name, double min, double max) : this(name, min, max, min) { }
        public FloatOption(string name, double min, double max, double value) : base(name)
        {
            Min = min;
            Max = max;
            Value = value;
        }
    }
}
