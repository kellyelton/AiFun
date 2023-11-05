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

namespace AiFun
{
    public class Ecosystem : INotifyPropertyChanged
    {
        public ObservableCollection<AnimateObject> AnimateObjects { get; set; }

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
        public double WorldWidth { get { return _worldWidth; } }
        public double WorldHeight { get { return _worldHeight; } }

        private double _worldWidth;
        private double _worldHeight;
        private int _generationCount;


        public Ecosystem(double width, double height)
        {
            _worldWidth = width;
            _worldHeight = height;
            AnimateObjects = new ObservableCollection<AnimateObject>();
            _deadObjects = new List<AnimateObject>();
        }

        public void Create<T>(T obj) where T : AnimateObject
        {
            AnimateObjects.Add(obj);
        }

        public void Reset()
        {
            AnimateObjects.Clear();
            for (var i = 0; i < 100; i++)
            {
                var an = new Animal(this);
                AnimateObjects.Add(an);
            }
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
            var a1 = bestOrder[0];
            var a2 = bestOrder[1];
            AnimateObjects.Clear();
            _deadObjects.Clear();
            // Calculate da best guys and merge em
            for (var i = 0; i < 75; i++)
            {
                var an = new Animal(this, a1, a2);
                AnimateObjects.Add(an);
            }
            for (var i = 0; i < 25; i++)
            {
                var an = new Animal(this);
                AnimateObjects.Add(an);
            }
            GenerationCount++;
        }

        public Object ObjectAlongLine(double angle, Point start)
        {
            var xSign = 0;
            var ySign = 0;

            var checkList = new List<AnimateObject>();
            if (angle <= 90)
            {
                xSign = 1;
                ySign = -1;
                checkList = AnimateObjects
                    .Where(x => x.Location.Left >= start.X)
                    .Where(x => x.Location.Top <= start.Y).ToList();
            }
            else if (angle > 90 && angle <= 180)
            {
                xSign = 1;
                ySign = 1;
                checkList = AnimateObjects
                    .Where(x => x.Location.Left >= start.X)
                    .Where(x => x.Location.Top >= start.Y).ToList();
            }
            else if (angle > 180 && angle <= 270)
            {
                xSign = -1;
                ySign = 1;
                checkList = AnimateObjects
                    .Where(x => x.Location.Left <= start.X)
                    .Where(x => x.Location.Top >= start.Y).ToList();
            }
            else if (angle > 270 && angle <= 360)
            {
                xSign = -1;
                ySign = -1;
                checkList = AnimateObjects
                    .Where(x => x.Location.Left <= start.X)
                    .Where(x => x.Location.Top <= start.Y).ToList();
            }

            {
                var min = xSign == 1 ? _worldWidth : 0;
                double x = start.X;
                double y = start.Y;
                while (true)
                {
                    x += (xSign * 5);
                    if (x <= 0 || x >= _worldWidth) break;
                    y = Math.Sin(angle);

                    // Only check ones that fall in the proper coords
                    foreach (var other in checkList)
                    {
                        if (other.Location.Contains(x, y)) return other;
                    }
                }
            }
            return null;
        }

        public void Update(double time)
        {
            foreach (var an in AnimateObjects.ToList())
            {
                an.Update(60);
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
                    if (anim.IsPregnant && anim.TimeImpregnated < System.DateTime.Now.AddSeconds(-5) && anim.IsDead == false)
                    {
                        Trace.WriteLine("Popped a baby");
                        AnimateObjects.Add(anim.PopBaby());
                    }

                    if (anim.IsDead && anim.TimeOfDeath < System.DateTime.Now.AddSeconds(-10))
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
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}