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

        private DispatcherTimer _timer;
        private Ecosystem _ecosystem;

        public MainWindow()
        {
            InitializeComponent();
            this.KeyUp += OnKeyUp;

            this.Loaded += OnLoaded;

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(60), DispatcherPriority.Render, OnTick, Dispatcher);
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            this.Loaded -= OnLoaded;
            Ecosystem = new Ecosystem((this.Content as FrameworkElement).ActualWidth, (this.Content as FrameworkElement).ActualHeight);
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
        }

        private void OnTick(object sender, EventArgs e)
        {
            Ecosystem.Update(60);
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
