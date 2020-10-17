using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MaxMix.Framework.Mvvm
{
    internal class ObservableObject : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Methods
        protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (field.Equals(value))
                return;

            field = value;
            RaisePropertyChanged(name);
        }

        protected virtual void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            if (string.IsNullOrEmpty(name))
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}
