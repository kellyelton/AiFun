using System;
using System.Diagnostics;
using System.Reflection;

namespace AiFun
{
    public class Map
    {
        private readonly PropertyInfo _prop;
        private readonly object _instance;
        private readonly Func<double, double> _adjust;

        public Map(PropertyInfo prop, object o, Func<double, double> adjust = null)
        {
            _adjust = adjust;
            _prop = prop;
            _instance = o;
        }

        public double GetValue()
        {
            var ret = (double)_prop.GetValue(_instance);
            if (_adjust != null)
                ret = _adjust(ret);
            return ret;
        }

        public void SetValue(double d)
        {
            if (_adjust != null)
                d = _adjust(d);
            _prop.SetValue(_instance, d);
        }

        public double GetRealValue()
        {
            var ret = (double)_prop.GetValue(_instance);
            return ret;
        }
    }
}