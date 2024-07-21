using System;
using System.Collections.Generic;
using System.Text;

namespace Utils.AI
{
    public class ActivationFunction
    {
        public readonly string Name;

        private Func<float, float> f_x;
        private Func<float, float> f_dx;

        public ActivationFunction(string name, Func<float, float> f_x, Func<float, float> f_dx)
        {
            Name = name;
            this.f_x = f_x;
            this.f_dx = f_dx;
        }
        public float CalculateValue(float x) => f_x(x);
        public float CalculateDerivative(float x) => f_dx(x);
    }
}
