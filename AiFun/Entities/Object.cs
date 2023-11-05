using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using AiFun.Annotations;

namespace AiFun.Entities
{
    public abstract class Object : IUpdateable, IPositionable, INotifyPropertyChanged
    {
        private Rect _location;
        public abstract void Update(double time);

        public ObservableCollection<Object> Touching { get; set; }

        protected Object()
        {
            Touching = new ObservableCollection<Object>();
        }

        public abstract void HandleTouching();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public Rect Location
        {
            get { return _location; }
            set
            {
                if (value.Equals(_location)) return;
                _location = value;
                OnPropertyChanged();
                OnPropertyChanged("Left");
                OnPropertyChanged("Top");
            }
        }

        public double Left { get { return Location.Left; } }
        public double Top { get { return Location.Top;} }
    }

    public abstract class AnimateObject : Object, IMovable
    {
        private double _speed;
        private double _xVelocity;
        private double _yVelocity;

        protected void UpdateLocation(double time)
        {
            Location = new Rect(Location.Left +( time * (XVelocity * Speed))
                , Location.Top + (time * (YVelocity * Speed)), 5, 5);
            //Location.Left += time * (XVelocity * Speed);
            //Location.Top += time * (YVelocity * Speed);
        }

        public double Speed
        {
            get { return _speed; }
            set
            {
                if (value.Equals(_speed)) return;
                _speed = value;
                OnPropertyChanged();
            }
        }

        public double XVelocity
        {
            get { return _xVelocity; }
            set
            {
                if (value.Equals(_xVelocity)) return;
                _xVelocity = value;
                OnPropertyChanged();
            }
        }

        public double YVelocity
        {
            get { return _yVelocity; }
            set
            {
                if (value.Equals(_yVelocity)) return;
                _yVelocity = value;
                OnPropertyChanged();
            }
        }
    }

    public interface IUpdateable
    {
        void Update(double time);
    }

    public interface IPositionable
    {
        Rect Location { get; set; }
    }

    public interface IMovable
    {
        double Speed { get; set; }
        double XVelocity { get; set; }
        double YVelocity { get; set; }
    }
}
