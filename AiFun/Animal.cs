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

        public bool IsDead
        {
            get { return _isDead; }
            set
            {
                if (_isDead == value) return;
                if (value == true)
                {
                    TimeOfDeath = DateTime.Now;
                    LengthOfLife = new TimeSpan(TimeOfDeath.Ticks - _born.Ticks);
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
                    TimeImpregnated = DateTime.Now;
                }
                OnPropertyChanged();
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

        public DateTime TimeOfDeath
        {
            get { return _timeOfDeath; }
            private set
            {
                if (value.Equals(_timeOfDeath)) return;
                _timeOfDeath = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan LengthOfLife
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
            }
        }

        public DateTime TimeImpregnated
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
                return LengthOfLife.Ticks;
            }
        }

        public double BabiesCreated { get; set; }
        public double OthersEaten { get; set; }

        private static Random _rnd = new Random();
        private NetworkMapper<Animal> _mapper;
        private bool _isDead;
        private DateTime _born;
        private bool _isPregnant;
        private double _availableEnergy;
        private DateTime _timeOfDeath;
        private TimeSpan _lengthOfLife;
        private DateTime _timeImpregnated;
        private Animal _baby;
        private bool _wasEaten;
        private double _lookingAngle;
        private Ecosystem _eco;
        private Object _focusingObject;
        private double _distanceTraveled;
        private double _deltaTurn;

        public Animal(Ecosystem eco)
        {
            _eco = eco;
            _born = DateTime.Now;
            AvailableEnergy = _rnd.NextDouble().Denormalize(0, 10000);
            Location = new Rect(_rnd.Next(0, (int)_eco.WorldWidth), _rnd.Next(0, (int)_eco.WorldHeight), 5, 5);
            XVelocity = _rnd.NextDouble().Denormalize(-1, 1);
            YVelocity = _rnd.NextDouble().Denormalize(-1, 1);
            Speed = _rnd.NextDouble();
            MovementEfficency = _rnd.NextDouble();
            Sex = _rnd.NextDouble();
            IsPregnant = false;
            HiddenNeurons = _rnd.Next(0, 5);
            SetupNetwork().Randomize();
        }

        public Animal(Ecosystem eco, Animal p1, Animal p2)
        {
            _eco = eco;
            AvailableEnergy = _rnd.NextDouble().Denormalize(0, 10000);
            Location = new Rect(_rnd.Next(0, (int)_eco.WorldWidth), _rnd.Next(0, (int)_eco.WorldHeight), 5, 5);
            XVelocity = _rnd.NextDouble().Denormalize(-1, 1);
            YVelocity = _rnd.NextDouble().Denormalize(-1, 1);
            Speed = _rnd.NextDouble();
            IsPregnant = false;
            Sex = _rnd.NextDouble();
            Breed(p1, p2, SetupNetwork());
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
            FocusingObject = _eco.ObjectAlongLine(LookingAngle, Location.TopLeft);
        }

        public override void HandleTouching()
        {
            // If we're dead, nothing to do
            if (IsDead) return;
            foreach (var other in Touching)
            {
                // don't worry about inanimate objects right now
                if ((other is AnimateObject) == false) continue;
                // If not an animal, don't worry about it now
                if ((other is Animal) == false) continue;

                var o = other as Animal;
                o.Touching.Remove(this);
                // If the other guy is dead, we should probably eat them
                if (o.IsDead)
                {
                    Trace.WriteLine("Ate a guy");
                    this.OthersEaten++;
                    this.AvailableEnergy += 1000;
                    o.WasEaten = true;
                    return;
                }

                // Lets do some sex. First we check if it's doable
                if (CanBreed(o) && o.CanBreed(this))
                {
                    Trace.WriteLine("Breeding");
                    // Both lose energy
                    AvailableEnergy -= 100 * MovementEfficency;
                    o.AvailableEnergy -= 100 * o.MovementEfficency;

                    if (IsFemale)
                        Impregnate(o);
                    if (o.IsFemale)
                        Impregnate(this);
                    this.BabiesCreated++;
                    o.BabiesCreated++;
                    return;
                }

                // Breeding not possible, only thing left 2 do is eet eachother
                if (this.AvailableEnergy > o.AvailableEnergy)
                {
                    o.AvailableEnergy = 0;
                    o.IsDead = true;
                    o.WasEaten = true;
                    this.AvailableEnergy += 20;
                }
                else if (o.AvailableEnergy > this.AvailableEnergy)
                {
                    AvailableEnergy = 0;
                    IsDead = true;
                    WasEaten = true;
                    o.AvailableEnergy += 20;
                }
            }
        }

        protected bool CanBreed(Animal other)
        {
            if (IsPregnant) return false;
            if (IsFemale && other.IsMale) return true;
            if (IsMale && other.IsFemale) return true;
            return false;
        }

        protected void Impregnate(Animal other)
        {
            _baby = new Animal(this._eco, this, other);
            IsPregnant = true;
        }

        public Animal PopBaby()
        {
            var ret = _baby;
            _baby = null;
            IsPregnant = false;
            return ret;

        }

        private BasicNetwork SetupNetwork()
        {
            _mapper = new NetworkMapper<Animal>(this);
            //_mapper.MapInputNormalized(x => x.XVelocity, -1, 1);
            //_mapper.MapInputNormalized(x => x.YVelocity, -1, 1);
            //_mapper.MapInputNormalized(x => x.Left, 0, 2000);
            //_mapper.MapInputNormalized(x => x.Top, 0, 2000);
            //_mapper.MapInput(x => x.Speed);
            _mapper.MapInput(x => x.AvailableEnergy, x => x.Clamp(0, 10000).Normalize(0, 10000));
            _mapper.MapInputNormalized(x => x.LookingAngle, 0, 360);
            _mapper.MapInput(x => x.IsFocusingOnObject);
            _mapper.MapInputNormalized(x => x.DistanceToFocusingObject, 0, Int32.MaxValue);

            _mapper.MapOutputDenormalized(x => x.XVelocity, -1, 1);
            _mapper.MapOutputDenormalized(x => x.YVelocity, -1, 1);
            _mapper.MapOutput(x => x.Speed, x => x / 4);
            _mapper.MapOutput(x=>x.LookingAngle, x=>Math.Abs(x.Denormalize(0, 360)));
            //_mapper.MapOutputDenormalized(x => x.LookingAngle, 0, 360);
            return _mapper.CreateNetwork(HiddenNeurons);
        }

        private void Breed(Animal a1, Animal a2, BasicNetwork net)
        {
            MovementEfficency.SetToRandom(a1.MovementEfficency, a2.MovementEfficency);
            HiddenNeurons.SetToRandom(a1.HiddenNeurons, a2.HiddenNeurons);

            var fnet = net.GetFNData().ToArray();
            var a1f = a1.Brain.GetFNData().ToArray();
            var a2f = a2.Brain.GetFNData().ToArray();
            foreach (var f in fnet)
            {
                var w1 = a1f.FirstOrDefault(x => x.Equals(f));
                var w2 = a2f.FirstOrDefault(x => x.Equals(f));

                if (w1 == null && w2 == null)
                {
                    f.Weight = _rnd.NextDouble().Normalize(-1, 1);
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
                f.Weight = f.Weight.SetToRandom(w1.Weight, w2.Weight, .15);
            }
            net.SetFNData(fnet);
        }

        public double DistanceBetweenTwoPoints(Point p1, Point p2)
        {
            double dist = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            return dist;
        }
    }
}