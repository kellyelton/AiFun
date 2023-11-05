using System.Collections.Generic;
using System.Windows.Documents;

namespace AiFun.Entities
{
    public class Neuron
    {
        public List<Neuron> Inputs { get; set; }
        public List<Neuron> Outputs { get; set; }
    }

    public class Synapse
    {
        public Neuron From { get; set; }
        public Neuron To { get; set; }
        public double Weight { get; set; }
    }

    public class NeuralNetwork
    {
        
    }
}
