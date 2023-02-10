using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class PromisedResult<T>
    {
        public T Value => value;
        public bool Ready { get; private set; }

        private T value = default;

        public void Set(T value)
        {
            this.value = value;
            Ready = true;
        }
    }
}
