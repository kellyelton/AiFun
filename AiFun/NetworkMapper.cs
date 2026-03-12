using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Encog.ML.Data.Basic;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;
using Encog.Neural.Networks.Structure;

namespace AiFun
{
    public class NetworkMapper<T> where T : class
    {
        private readonly T _instance;
        public BasicNetwork Network { get; private set; }
        private readonly Dictionary<int, Map> _inmaps;
        private readonly Dictionary<int, Map> _outmaps;

        public NetworkMapper(T cls)
        {
            _instance = cls;
            _inmaps = new Dictionary<int, Map>();
            _outmaps = new Dictionary<int, Map>();
        }

        public BasicNetwork CreateNetwork(int hidden)
        {
            Network = new BasicNetwork();
            Network.AddLayer(new BasicLayer(_inmaps.Count));
            for(var i = 0;i<hidden;i++)
            {
                var layer = new BasicLayer(_inmaps.Count);
                Network.AddLayer(layer);
            }
            Network.AddLayer(new BasicLayer(_outmaps.Count));
            Network.Structure.FinalizeStructure();
            return Network;
        }

        public void Update()
        {
            var invals = _inmaps.Select(x => x.Value.GetValue()).ToArray();
            //var outvals = new double[_outmaps.Count];
            var ins = new BasicMLData(invals, false);
            var o = Network.Compute(ins);
            //Network.Compute(invals, outvals);
            foreach (var i in _outmaps)
            {
                i.Value.SetValue(o[i.Key]);
            }
            //foreach (var i in _inmaps)
            //{
            //    Network.InputNodes[i.Key].Value = i.Value.GetValue();
            //}
            //foreach (var i in _outmaps)
            //{
            //    i.Value.SetValue(Network.OutputNodes[i.Key].Value);
            //}
        }

        public void MapInput(Expression<Func<T, double>> expression)
        {
            var propInfo = GetProperty(expression);
            _inmaps.Add(_inmaps.Count, new Map(propInfo, _instance));
        }

        public void MapInput(Expression<Func<T, double>> expression, Func<double, double> adjust)
        {
            var propInfo = GetProperty(expression);
            _inmaps.Add(_inmaps.Count, new Map(propInfo, _instance, adjust));
        }

        public void MapInputNormalizedToUnit(Expression<Func<T, double>> expression, double min, double max)
        {
            MapInput(expression, x => x.NormalizeToUnit(min, max));
        }

        public void MapInputNormalizedToSignedUnit(Expression<Func<T, double>> expression, double min, double max)
        {
            MapInput(expression, x => x.NormalizeToSignedUnit(min, max));
        }

        public void MapInputNormalized(Expression<Func<T, double>> expression, double min, double max)
        {
            MapInputNormalizedToSignedUnit(expression, min, max);
        }

        public void MapOutput(Expression<Func<T, double>> expression)
        {
            var propInfo = GetProperty(expression);
            _outmaps.Add(_outmaps.Count, new Map(propInfo, _instance));
        }

        public void MapOutput(Expression<Func<T, double>> expression, Func<double, double> adjust)
        {
            var propInfo = GetProperty(expression);
            _outmaps.Add(_outmaps.Count, new Map(propInfo, _instance, adjust));
        }

        public void MapOutputDenormalizedFromUnit(Expression<Func<T, double>> expression, double min, double max)
        {
            MapOutput(expression, x => x.DenormalizeFromUnit(min, max));
        }

        public void MapOutputDenormalizedFromSignedUnit(Expression<Func<T, double>> expression, double min, double max)
        {
            MapOutput(expression, x => x.DenormalizeFromSignedUnit(min, max));
        }

        public void MapOutputDenormalized(Expression<Func<T, double>> expression, double min, double max)
        {
            MapOutputDenormalizedFromSignedUnit(expression, min, max);
        }

        protected PropertyInfo GetProperty(Expression<Func<T, double>> expression)
        {
            Type type = typeof(T);

            MemberExpression member = expression.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    expression.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    expression.ToString()));

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format(
                    "Expresion '{0}' refers to a property that is not from type {1}.",
                    expression.ToString(),
                    type));
            return propInfo;
        }
    }
}