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
        /// <summary>
        /// Use this method to start any operations that can be reset or restarted
        /// such as service initialization.
        /// </summary>
        public virtual void Start() { }

        /// <summary>
        /// Use this method to dispose any resources used.
        /// </summary>
        public virtual void Stop() { }
    }
}
