using MaxMix.Framework.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.ViewModels
{
    internal class BaseViewModel : ObservableObject
    {
        public virtual void Start() { }
        public virtual void Stop() { }
    }
}
