using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using AiFun.Entities;
using Encog.Neural.Networks;
using Object = AiFun.Entities.Object;

namespace AiFun
{
    public class Animal : AnimateObject
    {
        public double AvailableEnergy
        {
            get { return _availableEnergy; }
            set
            {
                if (value.Equals(_availableEnergy)) return;
                _availableEnergy = value;
                OnPropertyChanged();
            }
        }

        public double TurnDeltaPerTick
        {
            get { return _turnDeltaPerTick; }
            set
            {
                if (value.Equals(_turnDeltaPerTick)) return;
                _turnDeltaPerTick = value;
                OnPropertyChanged();
            }
        }

        public bool IsDead
        {
            get { return _isDead; }
            set
            {
                if (_isDead == value) return;
                if (value == true)
                {
                    TimeOfDeath = _eco.SimulationTime;
                    LengthOfLife = TimeOfDeath - _born;
                }
                _isDead = value;
                OnPropertyChanged("IsDead");
            }
        }

        public bool WasEaten
        {
            get { return _wasEaten; }
            private set
            {
                if (value == _wasEaten) return;
                _wasEaten = value;
                OnPropertyChanged();
            }
        }

        public double LookingAngle
        {
            get => _lookingAngle;
            set
            {
                if (value.Equals(_lookingAngle)) return;
                _lookingAngle = value;
                OnPropertyChanged();
            }
        }

        public BasicNetwork Brain { get { return _mapper.Network; } }

        public double MovementEfficency { get; private set; }
        public double PregnancyGene { get; private set; }
        public double PregnancyDuration => _eco.MinPregnancyDuration + PregnancyGene * (_eco.MaxPregnancyDuration - _eco.MinPregnancyDuration);
        public int HiddenNeurons { get; private set; }
        /// <summary>
        /// <![CDATA[< .5 is male, > .5 is female, .5 is both]]>
        /// </summary>
        public double Sex { get; private set; }
        public bool IsMale { get { return Sex <= .5; } }
        public bool IsFemale { get { return Sex >= .5; } }
        public bool IsPregnant
        {
            get { return _isPregnant; }
            private set
            {
                if (value == _isPregnant) return;
                _isPregnant = value;
                if (IsPregnant)
                {
                    TimeImpregnated = _eco.SimulationTime;
                }
                OnPropertyChanged();
            }
        }

        public double VisionDistance
        {
            get { return _visionDistance; }
            set
            {
                if (value.Equals(_visionDistance)) return;
                _visionDistance = value;
                OnPropertyChanged();
            }
        }

        public double WallAhead { get; private set; }
        public double AliveCreatureAhead { get; private set; }
        public double DeadCreatureAhead { get; private set; }
        public double FoodAhead { get; private set; }
        public double FoodEnergyAhead { get; private set; }
        public double DistanceToObjectAhead { get; private set; }

        public double EatDesire
        {
            get { return _eatDesire; }
            set
            {
                if (value.Equals(_eatDesire)) return;
                _eatDesire = value;
                OnPropertyChanged();
            }
        }

        public double BreedDesire
        {
            get { return _breedDesire; }
            set
            {
                if (value.Equals(_breedDesire)) return;
                _breedDesire = value;
                OnPropertyChanged();
            }
        }

        public string VisionRayColor
        {
            get
            {
                if (WallAhead > 0) return "#CCFF4444";
                if (AliveCreatureAhead > 0) return "#CCFFAA00";
                if (DeadCreatureAhead > 0) return "#CC44CC44";
                if (FoodAhead > 0) return "#CC00CC00";
                return "#44888888";
            }
        }

        /// <summary>
        /// Nose dot color reflecting dominant desire:
        /// Red = eat desire dominant, Yellow = breed desire dominant, Gray = neutral
        /// </summary>
        public string DesireIndicatorColor
        {
            get
            {
                if (IsDead) return "#FF555555";
                var diff = EatDesire - BreedDesire;
                if (diff > 0.1) return "#FFE53935";   // red — wants to eat/fight
                if (diff < -0.1) return "#FFFDD835";  // yellow — wants to breed
                return "#FF888888";                    // gray — neutral
            }
        }

        public double VisionRayDisplayLength
        {
            get
            {
                if (VisionDistance <= 0) return 0;
                // DistanceToObjectAhead is inverted: closer = higher
                // Actual distance = VisionDistance * (1 - DistanceToObjectAhead)
                // When nothing detected, DistanceToObjectAhead = 0, so this = VisionDistance (full range)
                return VisionDistance * (1.0 - DistanceToObjectAhead);
            }
        }

        public double IsFocusingOnObject
        {
            get { return FocusingObject == null ? 0 : 1; }
        }

        public Object FocusingObject
        {
            get { return _focusingObject; }
            private set
            {
                if (Equals(value, _focusingObject)) return;
                _focusingObject = value;
                OnPropertyChanged();
                OnPropertyChanged("IsFocusingOnObject");
                OnPropertyChanged("DistanceToFocusingObject");
            }
        }

        public double DistanceToFocusingObject
        {
            get
            {
                if (FocusingObject == null) return Int32.MaxValue;
                return DistanceBetweenTwoPoints(Location.TopLeft, FocusingObject.Location.TopLeft);
            }
        }

        //public double Size { get; private set; }

        public double TimeOfDeath
        {
            get { return _timeOfDeath; }
            private set
            {
                if (value.Equals(_timeOfDeath)) return;
                _timeOfDeath = value;
                OnPropertyChanged();
            }
        }

        public double LengthOfLife
        {
            get { return _lengthOfLife; }
            private set
            {
                if (value.Equals(_lengthOfLife)) return;
                _lengthOfLife = value;
                OnPropertyChanged();
                OnPropertyChanged("Fitness");
            }
        }

        public double DistanceTraveled
        {
            get { return _distanceTraveled; }
            set
            {
                if (value.Equals(_distanceTraveled)) return;
                _distanceTraveled = value;
                OnPropertyChanged();
                OnPropertyChanged("Fitness");
            }
        }

        public double DeltaTurn
        {
            get { return _deltaTurn; }
            set
            {
                if (value.Equals(_deltaTurn)) return;
                _deltaTurn = value;
                OnPropertyChanged();
                OnPropertyChanged("Fitness");
            }
        }

        public double TimeImpregnated
        {
            get { return _timeImpregnated; }
            private set
            {
                if (value.Equals(_timeImpregnated)) return;
                _timeImpregnated = value;
                OnPropertyChanged();
            }
        }
        public double Fitness
        {
            get
            {
                // Composite fitness: primary=survival, secondary=babies, tertiary=distance
                // Weights chosen so primary always dominates secondary, etc.
                return LengthOfLife * 1_000_000_000
                     + BabiesCreated * 10_000_000
                     + DistanceTraveled * 1;
            }
        }

        public double BabiesCreated { get; set; }
        public double OthersEaten { get; set; }
        public double FoodEaten { get; set; }

        // Genetic color channels [0, 1] — inherited via crossover
        public double ColorR { get; private set; }
        public double ColorG { get; private set; }
        public double ColorB { get; private set; }

        public string BodyColor
        {
            get
            {
                var r = (byte)(ColorR * 255);
                var g = (byte)(ColorG * 255);
                var b = (byte)(ColorB * 255);
                return $"#{r:X2}{g:X2}{b:X2}";
            }
        }

        // Origin indicator — set once in constructor, never inherited
        public AnimalOrigin Origin { get; private set; }

        public string StrokeColor
        {
            get
            {
                return Origin switch
                {
                    AnimalOrigin.Elite => "#FF1565C0",   // blue
                    AnimalOrigin.Natural => "#FFFDD835", // yellow
                    _ => "#FF757575",                    // gray for random
                };
            }
        }

        private static Random _rnd = new Random();
        private NetworkMapper<Animal> _mapper;
        private bool _isDead;
        private double _born;
        private bool _isPregnant;
        private double _availableEnergy;
        private double _timeOfDeath;
        private double _lengthOfLife;
        private double _timeImpregnated;
        private FNData[] _fatherBrainSnapshot;
        private double _fatherMovementEfficency;
        private int _fatherHiddenNeurons;
        private double _fatherVisionDistance;
        private double _fatherPregnancyGene;
        private double _fatherColorR;
        private double _fatherColorG;
        private double _fatherColorB;
        private bool _wasEaten;
        private double _lookingAngle;
        private Ecosystem _eco;
        private Object _focusingObject;
        private double _distanceTraveled;
        private double _deltaTurn;
        private double _turnDeltaPerTick;
        private double _visionDistance;
        private double _eatDesire;
        private double _breedDesire;

        public Animal(Ecosystem eco)
        {
            _eco = eco;
            _born = eco.SimulationTime;
            AvailableEnergy = _rnd.NextDouble().DenormalizeFromUnit(0, 10000);
            Location = new Rect(_rnd.Next(0, (int)_eco.WorldWidth), _rnd.Next(0, (int)_eco.WorldHeight), 5, 5);
            XVelocity = _rnd.NextDouble().DenormalizeFromUnit(-1, 1);
            YVelocity = _rnd.NextDouble().DenormalizeFromUnit(-1, 1);
            LookingAngle = _rnd.NextDouble().DenormalizeFromUnit(0, 360);
            Speed = _rnd.NextDouble();
            MovementEfficency = _rnd.NextDouble();
            PregnancyGene = _rnd.NextDouble();
            Sex = _rnd.NextDouble();
            IsPregnant = false;
            HiddenNeurons = _rnd.Next(0, 5);
            VisionDistance = _rnd.NextDouble().DenormalizeFromUnit(0, _eco.MaxVisionDistance);
            ColorR = _rnd.NextDouble();
            ColorG = _rnd.NextDouble();
            ColorB = _rnd.NextDouble();
            Origin = AnimalOrigin.Random;
            SetupNetwork().Randomize();
        }

        public Animal(Ecosystem eco, Animal p1, Animal p2)
        {
            _eco = eco;
            _born = eco.SimulationTime;
            AvailableEnergy = _rnd.NextDouble().DenormalizeFromUnit(0, 10000);
            Location = new Rect(_rnd.Next(0, (int)_eco.WorldWidth), _rnd.Next(0, (int)_eco.WorldHeight), 5, 5);
            XVelocity = _rnd.NextDouble().DenormalizeFromUnit(-1, 1);
            YVelocity = _rnd.NextDouble().DenormalizeFromUnit(-1, 1);
            LookingAngle = _rnd.NextDouble().DenormalizeFromUnit(0, 360);
            Speed = _rnd.NextDouble();
            IsPregnant = false;
            Sex = _rnd.NextDouble();
            Origin = AnimalOrigin.Elite;
            Breed(p1, p2, SetupNetwork());
        }

        internal Animal(Ecosystem eco, Animal mother, FNData[] fatherBrain,
            double fatherMovEff, int fatherHidden, double fatherVision,
            double fatherPregnancyGene, double fatherColorR, double fatherColorG, double fatherColorB)
        {
            _eco = eco;
            _born = eco.SimulationTime;
            AvailableEnergy = _rnd.NextDouble().DenormalizeFromUnit(0, 10000);
            Location = new Rect(
                Math.Clamp(mother.Location.Left + _rnd.Next(-20, 20), 1, _eco.WorldWidth - 1),
                Math.Clamp(mother.Location.Top + _rnd.Next(-20, 20), 1, _eco.WorldHeight - 1), 5, 5);
            XVelocity = _rnd.NextDouble().DenormalizeFromUnit(-1, 1);
            YVelocity = _rnd.NextDouble().DenormalizeFromUnit(-1, 1);
            LookingAngle = _rnd.NextDouble().DenormalizeFromUnit(0, 360);
            Speed = _rnd.NextDouble();
            IsPregnant = false;
            Sex = _rnd.NextDouble();
            Origin = AnimalOrigin.Natural;
            BreedFromSnapshot(mother, fatherBrain, fatherMovEff, fatherHidden, fatherVision,
                fatherPregnancyGene, fatherColorR, fatherColorG, fatherColorB, SetupNetwork());
        }

        public override void Update(double time)
        {
            if (AvailableEnergy <= 0)
            {
                IsDead = true;
                return;
            }
            var curAng = LookingAngle;
            _mapper.Update();
            LookingAngle = NormalizeAngle(curAng + TurnDeltaPerTick);

            var headingRadians = LookingAngle * (Math.PI / 180.0);
            XVelocity = Math.Cos(headingRadians);
            YVelocity = Math.Sin(headingRadians);

            var curLocation = Location;
            this.UpdateLocation(time);
            var distanceTraveled = DistanceBetweenTwoPoints(curLocation.TopLeft, Location.TopLeft);
            DistanceTraveled += Math.Abs(distanceTraveled);
            DeltaTurn += Math.Abs(curAng - LookingAngle);
            //var distanceTraveled = Math.Sqrt(Math.Pow((Left - curleft), 2) + Math.Pow((Top - curtop), 2));
            AvailableEnergy -= (distanceTraveled * MovementEfficency) * _eco.MovementEnergyCostMultiplier;
            // Just basic life enegry loss shit
            AvailableEnergy -= _eco.BaseEnergyDrainPerSecond * time;
            //if (distanceTraveled < 1)
            //{
            //    AvailableEnergy -= 100;
            //}
            // Vision energy drain
            AvailableEnergy -= VisionDistance * _eco.VisionEnergyCostMultiplier * time;
            // Pregnancy energy drain
            if (IsPregnant)
                AvailableEnergy -= _eco.PregnancyEnergyCostMultiplier * time;

            // Update vision properties from ray cast
            UpdateVision();
        }

        public override void HandleTouching()
        {
            // If we're dead, nothing to do
            if (IsDead) return;
            foreach (var other in Touching)
            {
                // Food eating is automatic — no agency needed
                if (other is FoodPellet food && !food.IsConsumed)
                {
                    var gained = food.Bite(_eco.FoodBiteSize);
                    AvailableEnergy += gained;
                    FoodEaten += gained;
                    continue;
                }

                if (other is not Animal o) continue;

                o.Touching.Remove(this);

                // Dead creature = food source, eaten in bite-sized chunks
                if (o.IsDead)
                {
                    if (o.AvailableEnergy <= 0) continue;
                    var gained = Math.Min(_eco.FoodBiteSize, o.AvailableEnergy);
                    o.AvailableEnergy -= gained;
                    this.AvailableEnergy += gained;
                    this.FoodEaten += gained;
                    if (o.AvailableEnergy <= 0)
                        o.WasEaten = true;
                    Trace.WriteLine("Ate a chunk of corpse");
                    continue;
                }

                // Live creature encounter — desires determine behavior
                if (BreedDesire > EatDesire)
                {
                    // Attempt to breed first
                    if (CanBreed(o) && o.CanBreed(this))
                    {
                        Trace.WriteLine("Breeding");
                        AvailableEnergy -= 100 * MovementEfficency;
                        o.AvailableEnergy -= 100 * o.MovementEfficency;

                        if (IsFemale)
                            Impregnate(o);
                        if (o.IsFemale)
                            o.Impregnate(this);
                        this.BabiesCreated++;
                        o.BabiesCreated++;
                        return;
                    }
                    // Breeding not possible — creature passes on fighting
                    // (evolving high BreedDesire in wrong contexts is a disadvantage)
                }
                else if (EatDesire > BreedDesire)
                {
                    // Attempt to kill — energy comparison determines who wins
                    // Loser dies but retains energy as a scavengeable corpse
                    if (this.AvailableEnergy > o.AvailableEnergy)
                    {
                        o.IsDead = true;
                    }
                    else if (o.AvailableEnergy > this.AvailableEnergy)
                    {
                        IsDead = true;
                        return;
                    }
                }
                // else: equal desires — do nothing
            }
        }

        public void UpdateVision()
        {
            var result = _eco.ObjectAlongLine(LookingAngle, Location.TopLeft, VisionDistance);

            FocusingObject = result.HitObject;
            WallAhead = result.HitType == VisionHitType.Wall ? 1 : 0;
            AliveCreatureAhead = result.HitType == VisionHitType.AliveCreature ? 1 : 0;
            DeadCreatureAhead = result.HitType == VisionHitType.DeadCreature ? 1 : 0;
            FoodAhead = result.HitType == VisionHitType.Food ? 1 : 0;

            if (result.HitType == VisionHitType.Food && result.HitObject is FoodPellet foodPellet)
                FoodEnergyAhead = Math.Clamp(foodPellet.Energy / _eco.FoodMaxEnergy, 0, 1);
            else if (result.HitType == VisionHitType.DeadCreature && result.HitObject is Animal deadAnimal)
                FoodEnergyAhead = Math.Clamp(deadAnimal.AvailableEnergy / 10000.0, 0, 1);
            else
                FoodEnergyAhead = 0;

            if (result.HitType != VisionHitType.None && VisionDistance > 0)
                DistanceToObjectAhead = 1.0 - (result.Distance / VisionDistance);
            else
                DistanceToObjectAhead = 0;
        }

        protected bool CanBreed(Animal other)
        {
            if (IsPregnant) return false;
            if (IsFemale && other.IsMale) return true;
            if (IsMale && other.IsFemale) return true;
            return false;
        }

        protected void Impregnate(Animal father)
        {
            _fatherBrainSnapshot = father.Brain.GetFNData().ToArray();
            _fatherMovementEfficency = father.MovementEfficency;
            _fatherHiddenNeurons = father.HiddenNeurons;
            _fatherVisionDistance = father.VisionDistance;
            _fatherPregnancyGene = father.PregnancyGene;
            _fatherColorR = father.ColorR;
            _fatherColorG = father.ColorG;
            _fatherColorB = father.ColorB;
            IsPregnant = true;
        }

        public Animal PopBaby()
        {
            var baby = new Animal(_eco, this, _fatherBrainSnapshot,
                _fatherMovementEfficency, _fatherHiddenNeurons, _fatherVisionDistance,
                _fatherPregnancyGene, _fatherColorR, _fatherColorG, _fatherColorB);
            baby.Origin = AnimalOrigin.Natural;
            _fatherBrainSnapshot = null;
            IsPregnant = false;
            return baby;
        }

        private BasicNetwork SetupNetwork()
        {
            _mapper = new NetworkMapper<Animal>(this);
            //_mapper.MapInputNormalized(x => x.XVelocity, -1, 1);
            //_mapper.MapInputNormalized(x => x.YVelocity, -1, 1);
            //_mapper.MapInputNormalized(x => x.Left, 0, 2000);
            //_mapper.MapInputNormalized(x => x.Top, 0, 2000);
            //_mapper.MapInput(x => x.Speed);

            // Input audit:
            // AvailableEnergy: non-negative magnitude -> [0,1]
            _mapper.MapInput(x => x.AvailableEnergy, x => x.Clamp(0, 10000).NormalizeToUnit(0, 10000));

            // LookingAngle: absolute heading magnitude -> [0,1]
            _mapper.MapInputNormalizedToUnit(x => x.LookingAngle, 0, 360);

            // Vision inputs: at most one of WallAhead/AliveCreatureAhead/DeadCreatureAhead/FoodAhead is 1
            _mapper.MapInput(x => x.WallAhead);
            _mapper.MapInput(x => x.AliveCreatureAhead);
            _mapper.MapInput(x => x.DeadCreatureAhead);

            // Food vision inputs
            _mapper.MapInput(x => x.FoodAhead);
            _mapper.MapInput(x => x.FoodEnergyAhead);

            // DistanceToObjectAhead: already [0,1], inverted (closer = higher)
            _mapper.MapInput(x => x.DistanceToObjectAhead);

            // Output audit:
            // Speed: allow stop and slow movement, but with enough upper range to be visible -> [0,20]
            _mapper.MapOutputDenormalizedFromSignedUnit(x => x.Speed, 0, 20);

            // TurnDeltaPerTick: relative heading change per tick -> [-10,+10] degrees
            _mapper.MapOutputDenormalizedFromSignedUnit(x => x.TurnDeltaPerTick, -10, 10);

            // Interaction desires: both [0, 1] — creature decides eat vs breed on contact
            _mapper.MapOutputDenormalizedFromSignedUnit(x => x.EatDesire, 0, 1);
            _mapper.MapOutputDenormalizedFromSignedUnit(x => x.BreedDesire, 0, 1);

            return _mapper.CreateNetwork(HiddenNeurons);
        }

        private void Breed(Animal a1, Animal a2, BasicNetwork net)
        {
            MovementEfficency = MovementEfficency.SetToRandom(a1.MovementEfficency, a2.MovementEfficency);
            HiddenNeurons = HiddenNeurons.SetToRandom(a1.HiddenNeurons, a2.HiddenNeurons);
            VisionDistance = VisionDistance.SetToRandom(a1.VisionDistance, a2.VisionDistance);
            PregnancyGene = PregnancyGene.SetToRandom(a1.PregnancyGene, a2.PregnancyGene);
            ColorR = ColorR.SetToRandom(a1.ColorR, a2.ColorR);
            ColorG = ColorG.SetToRandom(a1.ColorG, a2.ColorG);
            ColorB = ColorB.SetToRandom(a1.ColorB, a2.ColorB);

            CrossoverBrainWeights(net, a1.Brain.GetFNData().ToArray(), a2.Brain.GetFNData().ToArray());
        }

        private void BreedFromSnapshot(Animal mother, FNData[] fatherBrain,
            double fatherMovEff, int fatherHidden, double fatherVision,
            double fatherPregnancyGene, double fatherColorR, double fatherColorG, double fatherColorB,
            BasicNetwork net)
        {
            MovementEfficency = MovementEfficency.SetToRandom(mother.MovementEfficency, fatherMovEff);
            HiddenNeurons = HiddenNeurons.SetToRandom(mother.HiddenNeurons, fatherHidden);
            VisionDistance = VisionDistance.SetToRandom(mother.VisionDistance, fatherVision);
            PregnancyGene = PregnancyGene.SetToRandom(mother.PregnancyGene, fatherPregnancyGene);
            ColorR = ColorR.SetToRandom(mother.ColorR, fatherColorR);
            ColorG = ColorG.SetToRandom(mother.ColorG, fatherColorG);
            ColorB = ColorB.SetToRandom(mother.ColorB, fatherColorB);

            CrossoverBrainWeights(net, mother.Brain.GetFNData().ToArray(), fatherBrain);
        }

        private void CrossoverBrainWeights(BasicNetwork net, FNData[] parent1Weights, FNData[] parent2Weights)
        {
            var fnet = net.GetFNData().ToArray();
            foreach (var f in fnet)
            {
                var w1 = parent1Weights.FirstOrDefault(x => x.Equals(f));
                var w2 = parent2Weights.FirstOrDefault(x => x.Equals(f));

                if (w1 == null && w2 == null)
                {
                    f.Weight = _rnd.NextDouble().DenormalizeFromUnit(-1, 1);
                    continue;
                }
                if (w1 == null)
                {
                    f.Weight = w2.Weight;
                    continue;
                }
                if (w2 == null)
                {
                    f.Weight = w1.Weight;
                    continue;
                }
                f.Weight = f.Weight.SetToRandom(w1.Weight, w2.Weight, _eco.MutationRate);
            }
            net.SetFNData(fnet);
        }

        public double DistanceBetweenTwoPoints(Point p1, Point p2)
        {
            double dist = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            return dist;
        }

        internal override void RefreshBindings()
        {
            base.RefreshBindings();
            OnPropertyChanged(nameof(LookingAngle));
            OnPropertyChanged(nameof(VisionRayColor));
            OnPropertyChanged(nameof(VisionRayDisplayLength));
            OnPropertyChanged(nameof(BodyColor));
            OnPropertyChanged(nameof(StrokeColor));
            OnPropertyChanged(nameof(DesireIndicatorColor));
            OnPropertyChanged(nameof(AvailableEnergy));
            OnPropertyChanged(nameof(IsDead));
            OnPropertyChanged(nameof(IsPregnant));
        }

        private static double NormalizeAngle(double angle)
        {
            var normalized = angle % 360;
            if (normalized < 0)
                normalized += 360;
            return normalized;
        }

    }

    public enum AnimalOrigin
    {
        Random,
        Elite,
        Natural
    }
}