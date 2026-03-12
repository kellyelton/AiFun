using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AiFun.Annotations;

namespace AiFun
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        public Ecosystem Ecosystem
        {
            get { return _ecosystem; }
            set
            {
                if (Equals(value, _ecosystem)) return;
                _ecosystem = value;
                OnPropertyChanged();
            }
        }

        public double TickMilliseconds
        {
            get { return _tickMilliseconds; }
            set
            {
                var newValue = Math.Max(10, value);
                if (newValue.Equals(_tickMilliseconds)) return;
                _tickMilliseconds = newValue;
                _timer.Interval = TimeSpan.FromMilliseconds(_tickMilliseconds);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SimulationDeltaSeconds));
            }
        }

        public double SimulationDeltaSeconds
        {
            get { return TickMilliseconds / 1000.0; }
        }

        public bool IsPaused
        {
            get { return _isPaused; }
            set
            {
                if (value == _isPaused) return;
                _isPaused = value;
                if (_isPaused)
                    _timer.Stop();
                else
                    _timer.Start();
                OnPropertyChanged();
            }
        }

        public int StepsPerFrame
        {
            get { return _stepsPerFrame; }
            set
            {
                var newValue = Math.Max(1, value);
                if (newValue == _stepsPerFrame) return;
                _stepsPerFrame = newValue;
                OnPropertyChanged();
            }
        }

        public bool ShowVisionRays
        {
            get { return _showVisionRays; }
            set
            {
                if (value == _showVisionRays) return;
                _showVisionRays = value;
                OnPropertyChanged();
            }
        }

        private DispatcherTimer _timer;
        private Ecosystem _ecosystem;
        private double _tickMilliseconds = 60;
        private bool _isPaused;
        private int _stepsPerFrame = 1;
        private bool _showVisionRays;

        public MainWindow()
        {
            InitializeComponent();
            this.KeyUp += OnKeyUp;

            this.Loaded += OnLoaded;

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(_tickMilliseconds), DispatcherPriority.Render, OnTick, Dispatcher);
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.Loaded -= OnLoaded;
            Ecosystem = new Ecosystem((this.Content as FrameworkElement).ActualWidth, (this.Content as FrameworkElement).ActualHeight);
            GenerationGraph.BindToHistory(Ecosystem.GenerationHistory);
            Ecosystem.Reset();
            _timer.Start();
        }


        private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            if (keyEventArgs.Key == Key.F5)
            {
                Ecosystem.Reset();
            }
            else if (keyEventArgs.Key == Key.G)
            {
                Ecosystem.NewGeneration();
            }
            else if (keyEventArgs.Key == Key.Space)
            {
                IsPaused = !IsPaused;
            }
            else if (keyEventArgs.Key == Key.V)
            {
                ShowVisionRays = !ShowVisionRays;
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            var dt = SimulationDeltaSeconds;
            var steps = StepsPerFrame;

            if (steps > 1)
            {
                Entities.Object.SuppressNotifications = true;
                for (int i = 0; i < steps - 1; i++)
                    Ecosystem.Update(dt);
                Entities.Object.SuppressNotifications = false;
            }

            Ecosystem.Update(dt);
            Ecosystem.RefreshUI();
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            Ecosystem.Reset();
        }

        private void OnNextGenerationClick(object sender, RoutedEventArgs e)
        {
            Ecosystem.NewGeneration();
        }

        private void OnPauseResumeClick(object sender, RoutedEventArgs e)
        {
            IsPaused = !IsPaused;
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
