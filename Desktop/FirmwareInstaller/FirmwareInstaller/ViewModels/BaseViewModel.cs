using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirmwareInstaller.Framework.Mvvm;

namespace FirmwareInstaller.ViewModels
{
    internal class BaseViewModel : ObservableObject
    {
        public virtual void Start() { }
        public virtual void Stop() { }
    }
}
