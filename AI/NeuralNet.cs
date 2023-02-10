using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.AI
{
    public class NeuralNet
    {
        public List<List<float>> Values { get; private set; }
        public List<float> Inputs => Values.First();
        public List<float> Outputs => Values.Last();
        public List<List<Neuron>> Layers; // The layers of the network, where each layer is a list of nodes or neurons.
        public List<Neuron> OutputLayer => Layers[Layers.Count - 1];
        public float learningRate; // The learning rate of the network, which determines the step size for weight updates.
        public NeuralState State = NeuralState.Uninitialized;

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

        // The ForwardPropagate method, which propagates
        // the input through the network and computes the
        // output of the network
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
        public float BackPropagate(List<float> desiredOutputs)
        {
            //function train, teaches the network to recognize a pattern given a desired output
            float errorg = 0; // General quadratic error
            float errorc; // Local error
            float sum = 0;
            float csum = 0;
            float delta;
            float output;

            // The backpropagation algorithm starts from the output layer and propagates the error backwards to the input
            {
                List<Neuron> layer = Layers.Last();
                for (int n = 0; n < layer.Count; ++n)
                {
                    Neuron neuron = layer[n];
                    int l = Layers.Count - 1;
                    output = Values.Last()[n]; //copy this value to facilitate calculations from the algorithm we can take the error value as
                    errorc = (desiredOutputs[n] - output) * output * (1 - output); //and the general error as the sum of delta values. Where delta is the squared difference of the desired value with the output value quadratic error
                    errorg += (desiredOutputs[n] - output) * (desiredOutputs[n] - output);

                    for (int i = 0; i < Values[l].Count; ++i) // Proceed to update the weights of the neuron
                    {
                        //neuron.deltaValues[i] = learningRate * errorc * Values[l][i] + neuron.deltaValues[i] * momentum; //update the delta value
                        //neuron.Weights[i] += neuron.deltaValues[i]; // Update the weight values
                        //sum += neuron.Weights[i] * errorc; // Need this to propagate to the next layer
                    }
                }
            }

            // Backpropagate hidden layers
            for (int l = (Layers.Count - 1); l >= 0; --l)
            {
                List<Neuron> layer = Layers[l];
                for (int n = 0; n < layer.Count; ++n)
                {
                    Neuron neuron = layer[n];
                    output = Values[l + 1][n];
                    errorc = output * (1 - output) * sum; // Calculate the error for this layer

                    for (int i = 0; i < Values[l].Count; ++i) // Update neuron weights
                    {
                        //delta = neuron.deltaValues[i];
                        //neuron.deltaValues[i] = learningRate * errorc * Values[l][i] + delta * momentum;
                        //neuron.Weights[i] += neuron.deltaValues[i];
                        //csum += neuron.Weights[i] * errorc; // Needed for next layer
                    }
                }
                sum = csum;
                csum = 0;
            }

            // Process the input layer
            {
                List<Neuron> layer = Layers[0];
                for (int n = 0; n < layer.Count; ++n)
                {
                    Neuron neuron = layer[n];
                    output = Values[1][n];
                    errorc = output * (1 - output) * sum;

                    for (int i = 0; i < Values[0].Count; ++i)
                    {
                        //delta = neuron.deltaValues[i];
                        //neuron.deltaValues[i] = learningRate * errorc * Values[0][i] + delta * momentum;
                        //neuron.Weights[i] += neuron.deltaValues[i]; //update weights
                    }
                }
            }

            //return the general error divided by 2
            return errorg * 0.5f;
        }
        /// <summary>Applies random mutations to the network's biases and geometry.</summary>
        public void Mutate(float mutationRate)
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
    }
}
