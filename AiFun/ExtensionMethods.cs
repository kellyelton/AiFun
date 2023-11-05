using Encog.Neural.Networks;
using System;
using System.Collections.Generic;
using Encog.MathUtil.Randomize;

namespace AiFun
{
    public static class ExtensionMethods
    {
        private static Random _rnd = new Random();
        public static BasicNetwork Randomize(this BasicNetwork net)
        {
            net.Reset();
            //var fnet = net.Flat;
            //for (var i = 0; i < fnet.Weights.Length; i++)
            //{
            //    fnet.Weights[i] = _rnd.NextDouble();
            //}
            for (int fromLayer = 0; fromLayer < net.LayerCount - 1; ++fromLayer)
                Randomize(net, fromLayer);
            return net;
        }

        public static void Randomize(BasicNetwork network, int fromLayer)
        {
            int totalNeuronCount = network.GetLayerTotalNeuronCount(fromLayer);
            int layerNeuronCount = network.GetLayerNeuronCount(fromLayer + 1);
            for (int fromNeuron = 0; fromNeuron < totalNeuronCount; ++fromNeuron)
            {
                for (int toNeuron = 0; toNeuron < layerNeuronCount; ++toNeuron)
                {
                    //double v = Randomize(network.GetWeight(fromLayer, fromNeuron, toNeuron));
                    double v = _rnd.NextDouble().Denormalize(-1, 1);
                    network.SetWeight(fromLayer, fromNeuron, toNeuron, v);
                }
            }
        }

        public static double Normalize(this double num, double curMin, double curMax)
        {
            var ret = (num - curMin) / (curMax - curMin);
            ret = ret*2;
            ret = ret - 1;
            return ret;
        }

        public static double Normalize(this int num, double curMin, double curMax)
        {
            return (num - curMin) / (curMax - curMin);
        }

        public static double Denormalize(this double num, double min, double max)
        {
            return (num * (max - min) + min);
        }

        public static T SetToRandom<T>(this T num, T first, T second)
        {
            num = _rnd.NextDouble() > 0.5 ? first : second;
            return num;
        }

        public static double SetToRandom(this double num, double first, double second, double bias)
        {
            var n = _rnd.NextDouble();
            if (n >= bias)
            {
                num = _rnd.NextDouble();
                return num;
            }
            num = SetToRandom(num, first, second);
            return num;
        }

        public static double Clamp(this double num, double min, double max)
        {
            if (num < min)
                num = min;
            if (num > max)
                num = max;
            return num;
        }

        public static IEnumerable<FNData> GetFNData(this BasicNetwork net)
        {
            for (int fromLayer = 0; fromLayer < net.LayerCount - 1; ++fromLayer)
            {
                int totalNeuronCount = net.GetLayerTotalNeuronCount(fromLayer);
                int layerNeuronCount = net.GetLayerNeuronCount(fromLayer + 1);
                for (int fromNeuron = 0; fromNeuron < totalNeuronCount; ++fromNeuron)
                {
                    for (int toNeuron = 0; toNeuron < layerNeuronCount; ++toNeuron)
                    {
                        yield return new FNData()
                        {
                            FromNeuron = fromNeuron,
                            ToNeuron = toNeuron,
                            Layer = fromLayer,
                            Weight = net.GetWeight(fromLayer, fromNeuron, toNeuron)
                        };
                    }
                }
            }
        }

        public static void SetFNData(this BasicNetwork net, IEnumerable<FNData> data)
        {
            foreach (var dat in data)
            {
                net.SetWeight(dat.Layer, dat.FromNeuron, dat.ToNeuron, dat.Weight);
            }
        }
    }

    public class FNData
    {
        public int Layer;
        public int FromNeuron;
        public int ToNeuron;
        public double Weight;

        public bool Equals(FNData other)
        {
            if (other == null) return false;
            if (Layer != other.Layer) return false;
            if (FromNeuron != other.FromNeuron) return false;
            if (ToNeuron != other.ToNeuron) return false;
            return true;
        }
    }
}
