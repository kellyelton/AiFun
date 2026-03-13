using System;
using System.Collections.Generic;
using System.Windows;
using AiFun.Entities;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace AiFun.Benchmarks
{
    [CPUUsageDiagnoser]
    public class UpdateCollisionBenchmarks
    {
        private Ecosystem _ecosystem;
        private List<(Rect location, double xVel, double yVel, double speed)> _savedState;
        [Params(100, 250)]
        public int Population { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _ecosystem = new Ecosystem(2000, 2000);
            Entities.Object.SuppressNotifications = true;
            var rnd = new Random(42);
            for (var i = 0; i < Population; i++)
            {
                var animal = new Animal(_ecosystem);
                animal.Location = new Rect(50 + ((i % 20) * 10) + rnd.NextDouble() * 5, 50 + ((i / 20) * 10) + rnd.NextDouble() * 5, 5, 5);
                _ecosystem.AnimateObjects.Add(animal);
            }

            _ecosystem.FoodTargetCount = Population;
            _ecosystem.SpawnFoodToTarget();
            // Snapshot initial positions to restore between iterations
            _savedState = new List<(Rect, double, double, double)>();
            foreach (var obj in _ecosystem.AnimateObjects)
            {
                if (obj is AnimateObject ao)
                    _savedState.Add((ao.Location, ao.XVelocity, ao.YVelocity, ao.Speed));
            }
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Restore positions so each iteration measures identical initial conditions
            for (int i = 0; i < _ecosystem.AnimateObjects.Count && i < _savedState.Count; i++)
            {
                var obj = _ecosystem.AnimateObjects[i];
                var saved = _savedState[i];
                obj.Location = saved.location;
                if (obj is AnimateObject ao)
                {
                    ao.XVelocity = saved.xVel;
                    ao.YVelocity = saved.yVel;
                    ao.Speed = saved.speed;
                }

                obj.Touching.Clear();
            }

            _ecosystem.SimulationTime = 0;
        }

        [Benchmark]
        public int SimulationUpdate()
        {
            _ecosystem.Update(0.01);
            return _ecosystem.AnimateObjects.Count;
        }
    }
}