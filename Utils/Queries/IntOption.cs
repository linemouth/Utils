using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Queries
{
    public abstract class IntOption : Option
    {
        public long Min
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
        public long Max
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
        public long Value
        {
            get => value;
            set
            {
                this.value = Math.Clamp(value, min, max);
                OnChanged();
            }
        }

        protected long min;
        protected long max;
        protected long value;

        public IntOption(string name, long min, long max) : this(name, min, max, min) { }
        public IntOption(string name, long min, long max, long value) : base(name)
        {
            Min = min;
            Max = max;
            Value = value;
        }
    }
}
