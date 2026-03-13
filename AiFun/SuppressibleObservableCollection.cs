using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AiFun
{
    public class SuppressibleObservableCollection<T> : ObservableCollection<T>
    {
        private bool _changesDuringSuppression;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (Entities.Object.SuppressNotifications)
            {
                _changesDuringSuppression = true;
                return;
            }
            base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (Entities.Object.SuppressNotifications) return;
            base.OnPropertyChanged(e);
        }

        public void FlushSuppressedChanges()
        {
            if (!_changesDuringSuppression) return;
            _changesDuringSuppression = false;
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            base.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            base.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        }
    }
}
