using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.AI
{
    public class NeuralNet
    {
        public List<List<float>> Values { get; private set; } = new List<List<float>>();
        public List<float> Inputs => Values.First();
        public List<float> Outputs => Values.Last();
        public List<List<Neuron>> Layers { get; private set; } = new List<List<Neuron>>(); // The layers of the network, where each layer is a list of nodes or neurons.
        public List<Neuron> OutputLayer => Layers[Layers.Count - 1];
        public float learningRate; // The learning rate of the network, which determines the step size for weight updates.
        public NeuralState State = NeuralState.Uninitialized;
        public float Cost
        {
            get
            {
                float cost = 0;
                var layerIterator = Layers.GetEnumerator();
                if(layerIterator.MoveNext())
                {
                    int previousCount = layerIterator.Current.Count;
                    cost = previousCount;
                    while(layerIterator.MoveNext())
                    {
                        int count = layerIterator.Current.Count;
                        cost += count * (previousCount + 1);
                        previousCount = count;
                    }
                }
                return cost;
            }
        }

        public NeuralNet(List<List<Neuron>> layers, float learningRate = 0.05f)
        {
            Layers = layers;
            this.learningRate = learningRate;
        }
        public NeuralNet(IEnumerable<int> layerSizes, float learningRate = 0.05f)
        {
            this.learningRate = learningRate;
            Layers = new List<List<Neuron>>();
            foreach (int layerSize in layerSizes)
            {
                List<Neuron> layer = new List<Neuron>();
                for (int n = 0; n < layerSize; ++n)
                {
                    layer.Add(new Neuron());
                }
                Layers.Add(layer);
            }
        }
        public NeuralNet Clone()
        {
            NeuralNet net = new NeuralNet(Layers.Select(layer => layer.Count), learningRate);
            for (int l = 0; l < Layers.Count; ++l)
            {
                for (int n = 0; n < Layers[l].Count; ++n)
                {
                    net.Layers[l][n] = Layers[l][n].Clone();
                }
            }
            return net;
        }

        /// <summary>Propagates the values from the inputs to the outputs.</summary>
        /// <param name="input">The input stimulus values.</param>
        /// <returns>The calculated output neuron activation states.</returns>
        public List<float> ForwardPropagate(List<float> input)
        {
            Values.Clear();
            Values.Add(input);

            for (int l = 0; l < Layers.Count; ++l)
            {
                List<Neuron> layer = Layers[l];
                List<float> inputs = Values[l];
                List<float> outputs = new List<float>();
                for (int n = 0; n < layer.Count; ++n)
                {
                    Neuron neuron = layer[n];
                    outputs.Add(neuron.ForwardPropagate(inputs));
                }
                Values.Add(outputs);
            }

            State = NeuralState.Calculated;
            return Outputs;
        }
        /// <summary>Computes the errors of the network and updates the weights of the network using gradient descent.</summary>
        /// <param name="desiredOutputs">The desired output values for the given inputs.</param>
        /// <returns>The general quadratic error.</returns>
        public float BackPropagate(List<float> desiredOutputs)
        {
            // Define local variables for easier understanding and maintenance
            float generalError = 0; // General quadratic error
            List<float> deltas = null; // List to store delta values for updating weights in each layer
            float momentum = 0.0f; // You should define this momentum variable or use it from a higher scope if intended

            // The backpropagation algorithm starts from the output layer and propagates the error backwards to the input
            List<Neuron> outputLayer = Layers.Last();
            for(int n = 0; n < outputLayer.Count; ++n)
            {
                Neuron neuron = outputLayer[n];
                float output = Values.Last()[n];
                float error = (desiredOutputs[n] - output) * output * (1 - output); // Local error for output layer
                generalError += error * error; // Accumulate the squared error for the general error

                // Initialize the delta list for output layer neurons
                if(deltas == null)
                {
                    deltas = new List<float>(outputLayer.Count);
                }

                for(int i = 0; i < neuron.Weights.Count; ++i) // Proceed to update the weights of the neuron
                {
                    // Assuming 'momentum' is a defined variable, you can add it to the weight update equation
                    float weightUpdate = learningRate * error * Values[Layers.Count - 2][i] + neuron.DeltaValues[i] * momentum;

                    neuron.DeltaValues[i] = weightUpdate;
                    neuron.Weights[i] += weightUpdate;
                }
            }

            // Backpropagate hidden layers
            for(int l = Layers.Count - 2; l > 0; --l) // Note the change in loop range to exclude input and output layers
            {
                List<Neuron> layer = Layers[l];
                List<Neuron> nextLayer = Layers[l + 1];
                List<float> nextLayerDeltas = new List<float>(layer.Count); // Corrected: Initialize a list to store merged deltas from the next layer

                for(int n = 0; n < layer.Count; ++n)
                {
                    Neuron neuron = layer[n];
                    float output = Values[l][n];

                    // Calculate the error for this layer using the delta values of the next layer
                    float error = output * (1 - output) * nextLayer.Select(neuronInNextLayer => neuronInNextLayer.DeltaValues[n] * neuronInNextLayer.Weights[n]).Sum();

                    // Update neuron's delta values
                    for(int i = 0; i < neuron.DeltaValues.Count; ++i)
                    {
                        float weightUpdate = learningRate * error * Values[l - 1][i] + neuron.DeltaValues[i] * momentum;
                        neuron.DeltaValues[i] = weightUpdate;
                        neuron.Weights[i] += weightUpdate;
                    }

                    // Add the error to the corresponding element of nextLayerDeltas
                    nextLayerDeltas.Add(error);
                }
            }

            // Process the input layer
            List<Neuron> inputLayer = Layers[0];
            List<Neuron> nextLayerIn = Layers[1];
            List<float> nextLayerDeltasIn = new List<float>(inputLayer.Count); // Corrected: Initialize a list to store merged deltas from the next layer
            for(int n = 0; n < inputLayer.Count; ++n)
            {
                Neuron neuron = inputLayer[n];
                float output = Values[0][n];

                // Calculate the error for this layer using the delta values of the next layer
                float error = output * (1 - output) * nextLayerIn.Select(neuronInNextLayer => neuronInNextLayer.DeltaValues[n] * neuronInNextLayer.Weights[n]).Sum();

                // Update neuron's delta values
                for(int i = 0; i < neuron.DeltaValues.Count; ++i)
                {
                    float weightUpdate = learningRate * error * Values[0][i] + neuron.DeltaValues[i] * momentum;
                    neuron.DeltaValues[i] = weightUpdate;
                    neuron.Weights[i] += weightUpdate;
                }

                // Add the error to the corresponding element of nextLayerDeltasIn
                nextLayerDeltasIn.Add(error);
            }

            // Return the general error divided by 2
            return generalError * 0.5f;
        }
        /// <summary>Applies random mutations to the network's biases and geometry.</summary>
        public void Mutate(float mutationRate = 0.05f)
        {
            // There is a chance to duplicate a layer
            if (mutationRate >= Math.Random(1.0f))
            {
                int l = Math.Random(Layers.Count - 1);
                List<Neuron> layer = Layers[l].Select(neuron => neuron.Clone()).ToList();
                Layers.Insert(l, layer);
            }
            // There is also a chance to remove a layer
            else if (Layers.Count > 2 && mutationRate >= Math.Random(1.0f))
            {
                int layerIndex = Math.Random(1, Layers.Count - 1);
            }

            int lastneuroneuronCount = Layers.Last().Count;
            for (int l = 1; l < Layers.Count - 2; ++l)
            {
                List<Neuron> layer = Layers[l];
                // There is a chance to add a neuron to a hidden layer
                if (mutationRate >= Math.Random(1.0f))
                {
                    Neuron neuron = layer[Math.Random(layer.Count - 1)].Clone();
                    layer.Add(neuron);
                }
                // Or remove a neuron
                else if (layer.Count > Layers.Last().Count() && mutationRate >= Math.Random(1.0f))
                {
                    layer.RemoveAt(Math.Random(layer.Count - 1));
                }

                // Mutate each neuron
                foreach(Neuron neuron in layer)
                {
                    neuron.Mutate(mutationRate);
                }
            }
        }
        /// <summary>Returns a clone of the network with random mutations applied to its biases and geometry.</summary>
        public NeuralNet Mutated(float mutationRate = 0.05f)
        {
            NeuralNet clone = Clone();
            clone.Mutate();
            return clone;
        }
    }
}
