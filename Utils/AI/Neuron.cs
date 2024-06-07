using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace Utils.AI
{
    public class Neuron
    {
        // The inputs and weights of the neuron,
        // where each input is multiplied by the
        // corresponding weight and summed to
        // compute the output of the neuron
        public List<float> Inputs
        {
            get => inputs;
            set
            {
                inputs = value;
                while(Weights.Count < inputs.Count)
                {
                    Weights.Add(0);
                }
                State = NeuralState.Initialized;
            }
        }
        public List<float> Weights { get; private set; } = null;
        public List<float> DeltaValues { get; private set; } = null;
        public float Bias;
        public float Activation { get; private set; } = 0;
        public float Output { get; private set; } = 0;
        public float Error { get; private set; } = 0;
        public ActivationFunction ActivationFunction { get; private set; }
        public NeuralState State;
        public static List<ActivationFunction> ActivationFunctions = new List<ActivationFunction>()
        {
            new ActivationFunction("ReLU", x => Math.Max(0, x), x => x > 0 ? 1 : 0),
            new ActivationFunction("Unity", x => x, x => 1),
            new ActivationFunction("Abs", x => Math.Abs(x), x => x > 0 ? 1 : -1),
            new ActivationFunction("Clamp", x => Math.Clamp(x, 0, 1), x => x > 0 && x < 1 ? 1 : -1),
            new ActivationFunction("Sin", x => Math.Sin(x), x => Math.Cos(x)),
            new ActivationFunction("Sin", x => Math.Cos(x), x => -Math.Sin(x)),
            new ActivationFunction("Sigmoid", x => Math.Sigmoid(x), x => Math.Exp(-x) / Math.Sigmoid(x)),
            new ActivationFunction("Erf", x => Math.Erf(x), x => Math.Exp(-x) / Math.Sigmoid(x))
        };

        private List<float> inputs = null;

        public Neuron(int inputCount, ActivationFunction activationFunction = null) : this(new List<float>(inputCount), activationFunction) { }
        public Neuron(List<float> weights, ActivationFunction activationFunction = null)
        {
            Weights = weights;
            State = NeuralState.Uninitialized;
            ActivationFunction = activationFunction ?? ActivationFunctions[0];
        }
        public Neuron(ActivationFunction activationFunction = null) : this(new List<float>(), activationFunction) { }
        public Neuron Clone() => new Neuron(new List<float>(Weights), ActivationFunction);

        /// <summary>Computes the output of the neuron by performing a sum-product of its inputs and bias, post-processed through its activation function.</summary>
        /// <param name="inputs">The input stimulus values.</param>
        /// <returns>The activation state of the neuron.</returns>
        public float ForwardPropagate(List<float> inputs)
        {
            Inputs = inputs;
            Activation = Math.DotProduct(Inputs, Weights) + Bias;
            Output = ActivationFunction.CalculateValue(Activation);
            State = NeuralState.Calculated;
            return Output;
        }

        // The BackPropagate method, which computes
        // the error of the neuron using the error
        // of the next layer and the weights of the
        // neuron, and updates the weights of the
        // neuron using the error and the learning
        // rate of the network
        public float BackPropagate(float learningRate, float expectedOutput)
        {
            float error = Output - expectedOutput;
            float errorGradient = ActivationFunction.CalculateDerivative(Activation) * error;

            // Update the weights of the connections to this neuron
            for (int i = 0; i < Weights.Count; i++)
            {
                Weights[i] -= learningRate * Inputs[i] * errorGradient;
            }

            State = NeuralState.Trained;
            return error;
        }
        public void Mutate(float mutationRate)
        {
            // There is a chance to change the activation function
            if (mutationRate > Math.Random(1f))
            {
                ActivationFunction = ActivationFunctions[Math.Random(ActivationFunctions.Count - 1)];
            }

            // There is a chance to change the bias
            if (mutationRate > Math.Random(1f))
            {
                Bias += Math.Random(-0.1f, 0.1f) * Math.Max(Math.Abs(Bias), 1);
            }

            // There is a chance to change weights
            for (int i = 0; i < Weights.Count; i++)
            {
                if (mutationRate > Math.Random(1f))
                {
                    Weights[i] += Math.Random(-0.1f, 0.1f) * Math.Max(Math.Abs(Weights[i]), 1);
                }
            }
        }
    }
}