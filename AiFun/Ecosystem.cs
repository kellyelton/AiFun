using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using AiFun.Annotations;
using AiFun.Entities;
using Object = AiFun.Entities.Object;
using System.Collections.Specialized;

namespace AiFun
{
    public class Ecosystem : INotifyPropertyChanged
    {
        private const int SpatialCellSize = 40;

        public ObservableCollection<AnimateObject> AnimateObjects { get; set; }

        public int InitialPopulation
        {
            get { return _initialPopulation; }
            set
            {
                if (value == _initialPopulation) return;
                _initialPopulation = Math.Max(2, value);
                OnPropertyChanged();
            }
        }

        public int ElitePopulation
        {
            get { return _elitePopulation; }
            set
            {
                if (value == _elitePopulation) return;
                _elitePopulation = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public int RandomPopulation
        {
            get { return _randomPopulation; }
            set
            {
                if (value == _randomPopulation) return;
                _randomPopulation = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public double BaseEnergyDrainPerSecond
        {
            get { return _baseEnergyDrainPerSecond; }
            set
            {
                if (value.Equals(_baseEnergyDrainPerSecond)) return;
                _baseEnergyDrainPerSecond = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public double MovementEnergyCostMultiplier
        {
            get { return _movementEnergyCostMultiplier; }
            set
            {
                if (value.Equals(_movementEnergyCostMultiplier)) return;
                _movementEnergyCostMultiplier = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public double PregnancyDurationSeconds
        {
            get { return _pregnancyDurationSeconds; }
            set
            {
                if (value.Equals(_pregnancyDurationSeconds)) return;
                _pregnancyDurationSeconds = Math.Max(0.5, value);
                OnPropertyChanged();
            }
        }

        public double CorpseDecaySeconds
        {
            get { return _corpseDecaySeconds; }
            set
            {
                if (value.Equals(_corpseDecaySeconds)) return;
                _corpseDecaySeconds = Math.Max(0.5, value);
                OnPropertyChanged();
            }
        }

        public double MaxVisionDistance
        {
            get { return _maxVisionDistance; }
            set
            {
                if (value.Equals(_maxVisionDistance)) return;
                _maxVisionDistance = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public double VisionEnergyCostMultiplier
        {
            get { return _visionEnergyCostMultiplier; }
            set
            {
                if (value.Equals(_visionEnergyCostMultiplier)) return;
                _visionEnergyCostMultiplier = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public int AliveCount
        {
            get { return AnimateObjects.OfType<Animal>().Count(); }
        }

        public int DeadCount
        {
            get { return _deadObjects.OfType<Animal>().Count(); }
        }

        public int PregnantCount
        {
            get { return AnimateObjects.OfType<Animal>().Count(x => x.IsPregnant); }
        }

        public double AverageEnergy
        {
            get
            {
                var animals = AnimateObjects.OfType<Animal>().ToList();
                if (animals.Count == 0) return 0;
                return animals.Average(x => x.AvailableEnergy);
            }
        }

        public double AverageSpeed
        {
            get
            {
                var animals = AnimateObjects.OfType<Animal>().ToList();
                if (animals.Count == 0) return 0;
                return animals.Average(x => Math.Sqrt((x.XVelocity * x.XVelocity) + (x.YVelocity * x.YVelocity)) * x.Speed);
            }
        }

        public int GenerationCount
        {
            get { return _generationCount; }
            set
            {
                if (value == _generationCount) return;
                _generationCount = value;
                OnPropertyChanged();
            }
        }

        private List<AnimateObject> _deadObjects { get; set; }
        public ObservableCollection<GenerationStats> GenerationHistory { get; private set; }
        public double WorldWidth { get { return _worldWidth; } }
        public double WorldHeight { get { return _worldHeight; } }

        private double _worldWidth;
        private double _worldHeight;
        private int _generationCount;
        private int _initialPopulation = 100;
        private int _elitePopulation = 75;
        private int _randomPopulation = 25;
        private double _baseEnergyDrainPerSecond = 100;
        private double _movementEnergyCostMultiplier = 10;
        private double _pregnancyDurationSeconds = 5;
        private double _corpseDecaySeconds = 10;
        private double _maxVisionDistance = 300;
        private double _visionEnergyCostMultiplier = 0.5;

        public double SimulationTime { get; private set; }

        private int _peakPopulation;

        public Ecosystem(double width, double height)
        {
            _worldWidth = width;
            _worldHeight = height;
            AnimateObjects = new ObservableCollection<AnimateObject>();
            _deadObjects = new List<AnimateObject>();
            GenerationHistory = new ObservableCollection<GenerationStats>();
            AnimateObjects.CollectionChanged += (sender, args) =>
            {
                RaiseSummaryPropertyChanged();
                if (AnimateObjects.Count > _peakPopulation)
                    _peakPopulation = AnimateObjects.Count;
            };
        }

        public void Create<T>(T obj) where T : AnimateObject
        {
            AnimateObjects.Add(obj);
        }

        public void Reset()
        {
            SimulationTime = 0;
            GenerationCount = 0;
            _peakPopulation = 0;
            AnimateObjects.Clear();
            _deadObjects.Clear();
            GenerationHistory.Clear();
            for (var i = 0; i < InitialPopulation; i++)
            {
                var an = new Animal(this);
                AnimateObjects.Add(an);
            }
            RaiseSummaryPropertyChanged();
        }

        public void NewGeneration()
        {
            var bestOrder = _deadObjects.OfType<Animal>()
                //.Where(x=>x.DistanceTraveled > 5)
                .OrderByDescending(x => x.DistanceTraveled)
                .ThenByDescending(x => x.DeltaTurn)
                .ThenByDescending(x => x.LengthOfLife)
                //.ThenByDescending(x => x.OthersEaten)
                .ThenByDescending(x => x.BabiesCreated)
                .Take(2)
                .ToList();
            if (bestOrder.Count < 2)
            {
                Reset();
                return;
            }

            var a1 = bestOrder[0];
            var a2 = bestOrder[1];

            // Record generation stats before clearing
            var allDead = _deadObjects.OfType<Animal>().ToList();
            if (allDead.Count > 0)
            {
                GenerationHistory.Add(new GenerationStats
                {
                    Generation = GenerationCount,
                    BestSurvivalTime = allDead.Max(x => x.LengthOfLife),
                    AvgSurvivalTime = allDead.Average(x => x.LengthOfLife),
                    BestDistance = allDead.Max(x => x.DistanceTraveled),
                    AvgDistance = allDead.Average(x => x.DistanceTraveled),
                    AvgVisionDistance = allDead.Average(x => x.VisionDistance),
                    TotalBabies = (int)allDead.Sum(x => x.BabiesCreated),
                    PopulationPeak = _peakPopulation
                });
            }

            AnimateObjects.Clear();
            _deadObjects.Clear();
            _peakPopulation = 0;
            SimulationTime = 0;
            // Calculate da best guys and merge em
            for (var i = 0; i < ElitePopulation; i++)
            {
                var an = new Animal(this, a1, a2);
                AnimateObjects.Add(an);
            }
            for (var i = 0; i < RandomPopulation; i++)
            {
                var an = new Animal(this);
                AnimateObjects.Add(an);
            }
            GenerationCount++;
            RaiseSummaryPropertyChanged();
        }

        public Object ObjectAlongLine(double angle, Point start)
        {
            var result = ObjectAlongLine(angle, start, visionDistance: Math.Max(_worldWidth, _worldHeight));
            return result.HitObject;
        }

        public VisionResult ObjectAlongLine(double angle, Point start, double visionDistance)
        {
            var objects = AnimateObjects;
            var objectCount = objects.Count;

            var startX = start.X;
            var startY = start.Y;
            var radians = angle * (Math.PI / 180.0);
            var xDelta = Math.Cos(radians);
            var yDelta = Math.Sin(radians);
            const double stepDistance = 5.0;
            var xStep = xDelta * stepDistance;
            var yStep = yDelta * stepDistance;
            var x = startX + xStep;
            var y = startY + yStep;

            // Build spatial index
            var spatialIndex = new Dictionary<long, List<AnimateObject>>(objectCount);
            for (var i = 0; i < objectCount; i++)
            {
                var obj = objects[i];
                var location = obj.Location;
                var minCellX = (int)(location.Left / SpatialCellSize);
                var maxCellX = (int)(location.Right / SpatialCellSize);
                var minCellY = (int)(location.Top / SpatialCellSize);
                var maxCellY = (int)(location.Bottom / SpatialCellSize);

                for (var cellX = minCellX; cellX <= maxCellX; cellX++)
                {
                    for (var cellY = minCellY; cellY <= maxCellY; cellY++)
                    {
                        var key = ComposeSpatialKey(cellX, cellY);
                        if (spatialIndex.TryGetValue(key, out var bucket) == false)
                        {
                            bucket = new List<AnimateObject>();
                            spatialIndex[key] = bucket;
                        }

                        bucket.Add(obj);
                    }
                }
            }

            // Ray march
            for (var distance = stepDistance; distance <= visionDistance; distance += stepDistance)
            {
                // Check wall hit (ray exits world bounds)
                if (x <= 0 || x >= _worldWidth || y <= 0 || y >= _worldHeight)
                {
                    return new VisionResult
                    {
                        HitType = VisionHitType.Wall,
                        Distance = distance,
                        HitObject = null
                    };
                }

                var cellKey = ComposeSpatialKey((int)(x / SpatialCellSize), (int)(y / SpatialCellSize));
                if (spatialIndex.TryGetValue(cellKey, out var candidates))
                {
                    for (var i = 0; i < candidates.Count; i++)
                    {
                        var other = candidates[i];
                        var location = other.Location;

                        if (Math.Abs(location.Left - startX) < 0.001 && Math.Abs(location.Top - startY) < 0.001)
                            continue;

                        if (location.Contains(x, y))
                        {
                            var hitType = VisionHitType.AliveCreature;
                            if (other is Animal animal && animal.IsDead)
                                hitType = VisionHitType.DeadCreature;

                            return new VisionResult
                            {
                                HitType = hitType,
                                Distance = distance,
                                HitObject = other
                            };
                        }
                    }
                }

                x += xStep;
                y += yStep;
            }

            return new VisionResult
            {
                HitType = VisionHitType.None,
                Distance = 0,
                HitObject = null
            };
        }

        private static long ComposeSpatialKey(int x, int y)
        {
            return ((long)x << 32) | (uint)y;
        }

        public void Update(double time)
        {
            SimulationTime += time;

            foreach (var an in AnimateObjects.ToList())
            {
                an.Update(time);
                if (an is Animal)
                {
                    if (an.Location.Left <= 0 || an.Location.Top <= 0 || an.Location.Left >= _worldWidth || an.Location.Top >= _worldHeight)
                    {
                        Trace.WriteLine("Touched death side");
                        AnimateObjects.Remove(an);
                        _deadObjects.Add(an);
                        continue;
                    }
                    var anim = an as Animal;
                    if (anim.IsPregnant && (SimulationTime - anim.TimeImpregnated) >= PregnancyDurationSeconds && anim.IsDead == false)
                    {
                        Trace.WriteLine("Popped a baby");
                        AnimateObjects.Add(anim.PopBaby());
                    }

                    if (anim.IsDead && (SimulationTime - anim.TimeOfDeath) >= CorpseDecaySeconds)
                    {
                        Trace.WriteLine("Disposed of the body");
                        AnimateObjects.Remove(anim);
                        _deadObjects.Add(anim);
                        continue;
                    }

                    if (anim.IsDead && anim.WasEaten)
                    {
                        Trace.WriteLine("Was Eaten");
                        AnimateObjects.Remove(anim);
                        _deadObjects.Add(anim);
                        continue;
                    }
                }
                foreach (var oan in AnimateObjects)
                {
                    if (an == oan) continue;
                    //var r2 = new Rect(oan.Left, oan.Top, 5, 5);
                    if (an.Location.IntersectsWith(oan.Location) == false) continue;
                    if (an.Touching.Contains(oan) == false)
                    {
                        Trace.WriteLine("Got a touch");
                        an.Touching.Add(oan);
                    }
                    if (oan.Touching.Contains(an) == false)
                    {
                        Trace.WriteLine("Got a touch");
                        oan.Touching.Add(an);
                    }

                }
            }
            foreach (var an in AnimateObjects)
            {
                an.HandleTouching();
                an.Touching.Clear();
            }
            if (AnimateObjects.Count == 0)
            {
                NewGeneration();
                return;
            }

            RaiseSummaryPropertyChanged();
        }

        public void RefreshUI()
        {
            foreach (var obj in AnimateObjects)
            {
                obj.RefreshBindings();
            }
            OnPropertyChanged(nameof(GenerationCount));
            RaiseSummaryPropertyChanged();
        }

        private void RaiseSummaryPropertyChanged()
        {
            OnPropertyChanged(nameof(AliveCount));
            OnPropertyChanged(nameof(DeadCount));
            OnPropertyChanged(nameof(PregnantCount));
            OnPropertyChanged(nameof(AverageEnergy));
            OnPropertyChanged(nameof(AverageSpeed));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (Entities.Object.SuppressNotifications) return;
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

