using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using AiFun.Annotations;
using AiFun.Entities;
using Object = AiFun.Entities.Object;
using System.Collections.Specialized;

namespace AiFun
{
    internal struct HallOfFameEntry
    {
        public double[] Weights;
        public double MovementEfficency;
        public double VisionDistance;
        public double PregnancyGene;
        public double ColorR;
        public double ColorG;
        public double ColorB;
        public double Fitness;
    }

    public class Ecosystem : INotifyPropertyChanged
    {
        private const int SpatialCellSize = 40;

        public SuppressibleObservableCollection<AnimateObject> AnimateObjects { get; set; }

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

        public double MinPregnancyDuration
        {
            get { return _minPregnancyDuration; }
            set
            {
                if (value.Equals(_minPregnancyDuration)) return;
                _minPregnancyDuration = Math.Max(0.5, value);
                OnPropertyChanged();
            }
        }

        public double MaxPregnancyDuration
        {
            get { return _maxPregnancyDuration; }
            set
            {
                if (value.Equals(_maxPregnancyDuration)) return;
                _maxPregnancyDuration = Math.Max(1, value);
                OnPropertyChanged();
            }
        }

        public double PregnancyEnergyCostMultiplier
        {
            get { return _pregnancyEnergyCostMultiplier; }
            set
            {
                if (value.Equals(_pregnancyEnergyCostMultiplier)) return;
                _pregnancyEnergyCostMultiplier = Math.Max(0, value);
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

        public int VisionRayCount
        {
            get { return _visionRayCount; }
            set
            {
                if (value == _visionRayCount) return;
                _visionRayCount = Math.Max(1, value);
                OnPropertyChanged();
            }
        }

        public double VisionFieldOfView
        {
            get { return _visionFieldOfView; }
            set
            {
                if (value.Equals(_visionFieldOfView)) return;
                _visionFieldOfView = Math.Max(1, value);
                OnPropertyChanged();
            }
        }

        public double MutationRate
        {
            get { return _mutationRate; }
            set
            {
                if (value.Equals(_mutationRate)) return;
                _mutationRate = Math.Max(0, Math.Min(1, value));
                OnPropertyChanged();
            }
        }

        public double MutationStepSize
        {
            get { return _mutationStepSize; }
            set
            {
                if (value.Equals(_mutationStepSize)) return;
                _mutationStepSize = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public int HiddenLayerSize
        {
            get { return _hiddenLayerSize; }
            set
            {
                if (value == _hiddenLayerSize) return;
                _hiddenLayerSize = Math.Max(1, value);
                OnPropertyChanged();
            }
        }

        public int TournamentSize
        {
            get { return _tournamentSize; }
            set
            {
                if (value == _tournamentSize) return;
                _tournamentSize = Math.Max(2, value);
                OnPropertyChanged();
            }
        }

        public int HallOfFameSize
        {
            get { return _hallOfFameSize; }
            set
            {
                if (value == _hallOfFameSize) return;
                _hallOfFameSize = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public int HallOfFameGenerations
        {
            get { return _hallOfFameGenerations; }
            set
            {
                if (value == _hallOfFameGenerations) return;
                _hallOfFameGenerations = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public int FoodTargetCount
        {
            get { return _foodTargetCount; }
            set
            {
                if (value == _foodTargetCount) return;
                _foodTargetCount = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public double FoodMinStartEnergy
        {
            get { return _foodMinStartEnergy; }
            set
            {
                if (value.Equals(_foodMinStartEnergy)) return;
                _foodMinStartEnergy = Math.Max(1, value);
                OnPropertyChanged();
            }
        }

        public double FoodMaxEnergy
        {
            get { return _foodMaxEnergy; }
            set
            {
                if (value.Equals(_foodMaxEnergy)) return;
                _foodMaxEnergy = Math.Max(1, value);
                OnPropertyChanged();
            }
        }

        public double FoodGrowthRate
        {
            get { return _foodGrowthRate; }
            set
            {
                if (value.Equals(_foodGrowthRate)) return;
                _foodGrowthRate = Math.Max(0, value);
                OnPropertyChanged();
            }
        }

        public double FoodBiteSize
        {
            get { return _foodBiteSize; }
            set
            {
                if (value.Equals(_foodBiteSize)) return;
                _foodBiteSize = Math.Max(1, value);
                OnPropertyChanged();
            }
        }

        public int FoodCount
        {
            get { return AnimateObjects.OfType<FoodPellet>().Count(); }
        }

        public int AliveCount
        {
            get { return AnimateObjects.OfType<Animal>().Count(); }
        }

        /// <summary>
        /// Genetic diversity of living creatures as a percentage (0-100).
        /// Average coefficient of variation across key genetic traits.
        /// High = diverse population, low = converged/inbred.
        /// </summary>
        public double GeneticDiversity
        {
            get
            {
                var animals = AnimateObjects.OfType<Animal>().Where(a => !a.IsDead).ToList();
                if (animals.Count < 2) return 0;

                var traits = new Func<Animal, double>[]
                {
                    a => a.MovementEfficency,
                    a => a.VisionDistance,
                    a => a.PregnancyGene,
                    a => a.ColorR,
                    a => a.ColorG,
                    a => a.ColorB,
                    a => a.Sex,
                };

                double totalCv = 0;
                int validTraits = 0;
                foreach (var trait in traits)
                {
                    var values = animals.Select(trait).ToList();
                    var mean = values.Average();
                    if (mean < 0.0001) continue; // skip near-zero means to avoid division issues
                    var variance = values.Average(v => (v - mean) * (v - mean));
                    var stdDev = Math.Sqrt(variance);
                    totalCv += stdDev / mean;
                    validTraits++;
                }

                if (validTraits == 0) return 0;
                // CV of 0.5 is fairly diverse for [0,1] traits, scale so that maps to ~100%
                return Math.Min(100, (totalCv / validTraits) * 200);
            }
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
        private List<HallOfFameEntry[]> _hallOfFame = new List<HallOfFameEntry[]>();
        public ObservableCollection<GenerationStats> GenerationHistory { get; private set; }
        public double WorldWidth { get { return _worldWidth; } }
        public double WorldHeight { get { return _worldHeight; } }

        private double _worldWidth;
        private double _worldHeight;
        private int _generationCount;
        private int _initialPopulation = 100;
        private int _elitePopulation = 75;
        private int _randomPopulation = 10;
        private double _baseEnergyDrainPerSecond = 30;
        private double _movementEnergyCostMultiplier = 3;
        private double _minPregnancyDuration = 15;
        private double _maxPregnancyDuration = 60;
        private double _pregnancyEnergyCostMultiplier = 50;
        private double _corpseDecaySeconds = 10;
        private double _maxVisionDistance = 300;
        private double _visionEnergyCostMultiplier = 0.05;
        private int _visionRayCount = 5;
        private double _visionFieldOfView = 120;
        private int _hiddenLayerSize = 12;
        private double _mutationRate = 0.03;
        private double _mutationStepSize = 0.1;
        private int _tournamentSize = 5;
        private int _hallOfFameSize = 5;
        private int _hallOfFameGenerations = 5;
        private int _foodTargetCount = 150;
        private double _foodMinStartEnergy = 200;
        private double _foodMaxEnergy = 2000;
        private double _foodGrowthRate = 10;
        private double _foodBiteSize = 400;

        public double SimulationTime { get; internal set; }

        private int _peakPopulation;
        private static Random _rnd = new Random();
        private Dictionary<long, List<AnimateObject>> _spatialIndex = new();
        private bool _spatialIndexDirty = true;

        public Ecosystem(double width, double height)
        {
            _worldWidth = width;
            _worldHeight = height;
            AnimateObjects = new SuppressibleObservableCollection<AnimateObject>();
            _deadObjects = new List<AnimateObject>();
            GenerationHistory = new ObservableCollection<GenerationStats>();
            AnimateObjects.CollectionChanged += (sender, args) =>
            {
                _spatialIndexDirty = true;
                RaiseSummaryPropertyChanged();
                if (AnimateObjects.Count > _peakPopulation)
                    _peakPopulation = AnimateObjects.Count;
            };
        }

        public void SpawnFoodToTarget()
        {
            var currentFood = AnimateObjects.OfType<FoodPellet>().Count();
            for (var i = currentFood; i < FoodTargetCount; i++)
            {
                AnimateObjects.Add(new FoodPellet(this));
            }
        }

        public void RemoveConsumedFood()
        {
            var consumed = AnimateObjects.OfType<FoodPellet>().Where(f => f.IsConsumed).ToList();
            foreach (var f in consumed)
                AnimateObjects.Remove(f);
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
            _hallOfFame.Clear();
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
            var deadAnimals = _deadObjects.OfType<Animal>().ToList();
            if (deadAnimals.Count < 2)
            {
                Reset();
                return;
            }

            // Record generation stats before clearing
            if (deadAnimals.Count > 0)
            {
                GenerationHistory.Add(new GenerationStats
                {
                    Generation = GenerationCount,
                    BestSurvivalTime = deadAnimals.Max(x => x.LengthOfLife),
                    AvgSurvivalTime = deadAnimals.Average(x => x.LengthOfLife),
                    BestDistance = deadAnimals.Max(x => x.DistanceTraveled),
                    AvgDistance = deadAnimals.Average(x => x.DistanceTraveled),
                    AvgVisionDistance = deadAnimals.Average(x => x.VisionDistance),
                    TotalBabies = (int)deadAnimals.Sum(x => x.BabiesCreated),
                    PopulationPeak = _peakPopulation,
                    TotalFoodEaten = deadAnimals.Sum(x => x.FoodEaten)
                });
            }

            // Snapshot top performers for hall of fame before clearing
            if (HallOfFameSize > 0 && HallOfFameGenerations > 0)
            {
                deadAnimals.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
                var count = Math.Min(HallOfFameSize, deadAnimals.Count);
                var entries = new HallOfFameEntry[count];
                for (int i = 0; i < count; i++)
                {
                    var a = deadAnimals[i];
                    entries[i] = new HallOfFameEntry
                    {
                        Weights = a.Brain.GetFNData().Select(f => f.Weight).ToArray(),
                        MovementEfficency = a.MovementEfficency,
                        VisionDistance = a.VisionDistance,
                        PregnancyGene = a.PregnancyGene,
                        ColorR = a.ColorR,
                        ColorG = a.ColorG,
                        ColorB = a.ColorB,
                        Fitness = a.Fitness,
                    };
                }
                _hallOfFame.Add(entries);
                while (_hallOfFame.Count > HallOfFameGenerations)
                    _hallOfFame.RemoveAt(0);
            }

            AnimateObjects.Clear();
            _deadObjects.Clear();
            _peakPopulation = 0;
            SimulationTime = 0;

            // Create elite offspring using tournament selection
            for (var i = 0; i < ElitePopulation; i++)
            {
                var parent1 = TournamentSelect(deadAnimals);
                var parent2 = TournamentSelect(deadAnimals);
                var an = new Animal(this, parent1, parent2);
                AnimateObjects.Add(an);
            }
            for (var i = 0; i < RandomPopulation; i++)
            {
                var donor = deadAnimals[_rnd.Next(deadAnimals.Count)];
                var an = new Animal(this, donor, mutationMultiplier: 10.0);
                AnimateObjects.Add(an);
            }
            // Spawn hall of fame clones
            for (var g = 0; g < _hallOfFame.Count; g++)
            {
                var gen = _hallOfFame[g];
                for (var i = 0; i < gen.Length; i++)
                {
                    var clone = new Animal(this, gen[i], hallOfFameRank: i + 1);
                    AnimateObjects.Add(clone);
                }
            }
            GenerationCount++;
            RaiseSummaryPropertyChanged();
        }

        private Animal TournamentSelect(List<Animal> candidates)
        {
            var tourneySize = Math.Min(TournamentSize, candidates.Count);
            Animal best = null;
            for (var i = 0; i < tourneySize; i++)
            {
                var candidate = candidates[_rnd.Next(candidates.Count)];
                if (best == null || candidate.Fitness > best.Fitness)
                    best = candidate;
            }
            return best!;
        }

        public Object ObjectAlongLine(double angle, Point start)
        {
            var result = ObjectAlongLine(angle, start, visionDistance: Math.Max(_worldWidth, _worldHeight));
            return result.HitObject;
        }

        public VisionResult ObjectAlongLine(double angle, Point start, double visionDistance)
        {
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

            if (_spatialIndexDirty)
                RebuildSpatialIndex();

            var spatialIndex = _spatialIndex;

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
                            if (other is FoodPellet)
                                hitType = VisionHitType.Food;
                            else if (other is Animal animal && animal.IsDead)
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

        public void RebuildSpatialIndex()
        {
            var objects = AnimateObjects;
            var objectCount = objects.Count;

            // Clear and reuse existing dictionary to reduce allocations
            foreach (var bucket in _spatialIndex.Values)
                bucket.Clear();

            _spatialIndexDirty = false;

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
                        if (_spatialIndex.TryGetValue(key, out var bucket) == false)
                        {
                            bucket = new List<AnimateObject>();
                            _spatialIndex[key] = bucket;
                        }

                        bucket.Add(obj);
                    }
                }
            }
        }

        public void Update(double time)
        {
            SimulationTime += time;

            RebuildSpatialIndex();

            foreach (var an in AnimateObjects.ToList())
            {
                an.Update(time);
                if (an is Animal)
                {
                    if (an.Location.Left <= 0 || an.Location.Top <= 0 || an.Location.Left >= _worldWidth || an.Location.Top >= _worldHeight)
                    {
                        AnimateObjects.Remove(an);
                        _deadObjects.Add(an);
                        continue;
                    }
                    var anim = an as Animal;
                    if (anim.IsPregnant && (SimulationTime - anim.TimeImpregnated) >= anim.PregnancyDuration && anim.IsDead == false)
                    {
                        AnimateObjects.Add(anim.PopBaby());
                    }

                    if (anim.IsDead && (SimulationTime - anim.TimeOfDeath) >= CorpseDecaySeconds)
                    {
                        AnimateObjects.Remove(anim);
                        _deadObjects.Add(anim);
                        continue;
                    }

                    if (anim.IsDead && anim.WasEaten)
                    {
                        AnimateObjects.Remove(anim);
                        _deadObjects.Add(anim);
                        continue;
                    }
                }
                var loc = an.Location;
                var minCX = (int)(loc.Left / SpatialCellSize);
                var maxCX = (int)(loc.Right / SpatialCellSize);
                var minCY = (int)(loc.Top / SpatialCellSize);
                var maxCY = (int)(loc.Bottom / SpatialCellSize);
                for (var cx = minCX; cx <= maxCX; cx++)
                {
                    for (var cy = minCY; cy <= maxCY; cy++)
                    {
                        var key = ComposeSpatialKey(cx, cy);
                        if (!_spatialIndex.TryGetValue(key, out var bucket)) continue;
                        for (var bi = 0; bi < bucket.Count; bi++)
                        {
                            var oan = bucket[bi];
                            if (an == oan) continue;
                            if (loc.IntersectsWith(oan.Location) == false) continue;
                            if (an.Touching.Contains(oan) == false)
                                an.Touching.Add(oan);
                            if (oan.Touching.Contains(an) == false)
                                oan.Touching.Add(an);
                        }
                    }
                }
            }
            foreach (var an in AnimateObjects)
            {
                an.HandleTouching();
                an.Touching.Clear();
            }
            // Remove consumed food and respawn to target count
            RemoveConsumedFood();
            SpawnFoodToTarget();

            if (AnimateObjects.OfType<Animal>().Count() == 0)
            {
                NewGeneration();
                return;
            }

            RaiseSummaryPropertyChanged();
        }

        public void RefreshUI()
        {
            if (AnimateObjects.Count > _peakPopulation)
                _peakPopulation = AnimateObjects.Count;
            AnimateObjects.FlushSuppressedChanges();
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
            OnPropertyChanged(nameof(FoodCount));
            OnPropertyChanged(nameof(GeneticDiversity));
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

