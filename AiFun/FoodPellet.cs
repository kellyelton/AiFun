using System;
using System.Windows;
using System.Windows.Media;
using AiFun.Entities;

namespace AiFun
{
    public class FoodPellet : AnimateObject
    {
        private readonly Ecosystem _eco;
        private double _energy;

        public double Energy
        {
            get => _energy;
            set
            {
                if (value.Equals(_energy)) return;
                _energy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplaySize));
                OnPropertyChanged(nameof(FillColor));
                UpdateLocationSize();
            }
        }

        public bool IsConsumed => Energy <= 0;

        /// <summary>
        /// Circle diameter scales from 4px at min energy to 16px at max energy.
        /// </summary>
        public double DisplaySize
        {
            get
            {
                if (_eco == null || _eco.FoodMaxEnergy <= 0) return 4;
                var fraction = Math.Clamp(Energy / _eco.FoodMaxEnergy, 0, 1);
                return 4 + fraction * 12;
            }
        }

        public Color FillColor
        {
            get
            {
                if (_eco == null || _eco.FoodMaxEnergy <= 0)
                    return Color.FromRgb(144, 238, 144);
                var fraction = Math.Clamp(Energy / _eco.FoodMaxEnergy, 0, 1);
                // Light green (144,238,144) -> dark green (0,100,0)
                var r = (byte)(144 - fraction * 144);
                var g = (byte)(238 - fraction * 138);
                var b = (byte)(144 - fraction * 144);
                return Color.FromRgb(r, g, b);
            }
        }

        public FoodPellet(Ecosystem eco)
        {
            _eco = eco;
            Speed = 0;
            XVelocity = 0;
            YVelocity = 0;
            var rnd = new Random();
            var x = rnd.Next(0, (int)eco.WorldWidth);
            var y = rnd.Next(0, (int)eco.WorldHeight);
            _energy = eco.FoodMinStartEnergy; // set backing field directly to avoid UpdateLocationSize before Location exists
            var size = DisplaySize;
            Location = new Rect(x, y, size, size);
        }

        public override void Update(double time)
        {
            if (IsConsumed) return;
            Energy = Math.Min(Energy + _eco.FoodGrowthRate * time, _eco.FoodMaxEnergy);
        }

        public override void HandleTouching()
        {
            // Food doesn't initiate interactions — animals eat food in Animal.HandleTouching
        }

        /// <summary>
        /// Animal bites this pellet. Returns actual energy consumed.
        /// </summary>
        private void UpdateLocationSize()
        {
            var size = DisplaySize;
            var oldLoc = Location;
            // Preserve center position
            var centerX = oldLoc.Left + oldLoc.Width / 2;
            var centerY = oldLoc.Top + oldLoc.Height / 2;
            Location = new Rect(centerX - size / 2, centerY - size / 2, size, size);
        }

        public double Bite(double biteSize)
        {
            var actual = Math.Min(biteSize, Energy);
            Energy -= actual;
            return actual;
        }
    }
}
