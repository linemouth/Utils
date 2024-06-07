using System;
using System.Collections.Generic;
using System.Linq;
using Utils;

namespace Utils.AI
{
    public class NeuralNetPopulation
    {
        public struct NeuralNetScore
        {
            public NeuralNet net;
            public double fitnessScore;

            public NeuralNetScore(NeuralNet net, double fitnessScore = 0)
            {
                this.net = net;
                this.fitnessScore = fitnessScore;
            }
        }
        public List<NeuralNetScore> Population;
        public float mutationRate;

        public void Kill()
        {
            // Calculate the minimum and maximum fitness scores
            var minFitness = Population.Min(e => e.fitnessScore);
            var maxFitness = Population.Max(e => e.fitnessScore);

            // Calculate the fitness range and midpoint
            var fitnessRange = maxFitness - minFitness;
            var fitnessMidpoint = minFitness + (fitnessRange / 2.0);

            // Remove NeuralNet instances with a sigmoid distribution of removal chance
            foreach (var net in Population)
            {
                // Calculate the removal chance for this NeuralNet using a sigmoid function
                double relativeFitness = (net.fitnessScore - fitnessMidpoint) / (fitnessRange);
                double removalChance = 1.0 / (1.0 + Math.Exp(-relativeFitness * 10));

                // Remove the NeuralNet with a probability equal to its removal chance
                if (Math.Random(1.0) < removalChance)
                {
                    Population.Remove(net);
                }
            }
        }
        public void Breed(int targetSize)
        {
            int currentPopulationSize = Population.Count;
            while (Population.Count < targetSize)
            {
                // Select a random NeuralNet from the current population
                var parent = Population[Math.Random(currentPopulationSize)];

                // Mutate the selected NeuralNet to create a new clone
                var clone = parent.net.Clone();
                clone.Mutate(mutationRate);

                // Add the mutated clone and its fitness score to the population
                Population.Add(new NeuralNetScore(clone));
            }
            currentPopulationSize = targetSize;
        }
    }
}
