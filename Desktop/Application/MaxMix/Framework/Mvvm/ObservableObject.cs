using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
            field = value;
            RaisePropertyChanged(name);
        }

        protected virtual void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            if (PropertyChanged != null && name != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));

            Properties.Settings.Default.Save();
        }
        #endregion
    }
}
