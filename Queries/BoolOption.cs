using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Queries
{
    class BoolOption : Option
    {
        public bool State
        {
            get => state;
            set
            {
                state = value;
                OnChanged();
            }
        }

        protected bool state = false;

        public BoolOption(string name) : base(name) { }
    }
}
