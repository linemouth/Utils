using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.Queries
{
    public class Option
    {
        public string Name { get; set; }
        public event Action Changed;

        public Option(string name) => Name = name;

        protected void OnChanged() => Changed?.Invoke();
    }
}
