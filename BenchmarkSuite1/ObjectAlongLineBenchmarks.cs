using System;
using System.Windows;
using AiFun.Entities;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace AiFun.Benchmarks
{
    [CPUUsageDiagnoser]
    public class ObjectAlongLineBenchmarks
    {
        private Ecosystem _ecosystem;
        private Point _start;
        private double _angle;
        [Params(100, 250, 500)]
        public int Population { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _ecosystem = new Ecosystem(2000, 2000);
            _start = new Point(1000, 1000);
            _angle = 0;
            var seeker = new BenchmarkAnimateObject
            {
                Location = new Rect(_start.X, _start.Y, 5, 5)
            };
            _ecosystem.AnimateObjects.Add(seeker);
            for (var i = 0; i < Population; i++)
            {
                var x = 50 + ((i % 50) * 30);
                var y = 50 + ((i / 50) * 30);
                if (Math.Abs(y - _start.Y) < 10)
                {
                    y += 120;
                }

                var obj = new BenchmarkAnimateObject
                {
                    Location = new Rect(x, y, 5, 5)
                };
                _ecosystem.AnimateObjects.Add(obj);
            }

            _ecosystem.RebuildSpatialIndex();
        }

        [Benchmark]
        public AiFun.Entities.Object Scan()
        {
            return _ecosystem.ObjectAlongLine(_angle, _start);
        }

        private sealed class BenchmarkAnimateObject : AnimateObject
        {
            public override void Update(double time)
            {
            }

            public override void HandleTouching()
            {
            }
        }
    }
}