using MaxMix.Framework.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.ViewModels
{
    /// <summary>
    /// Base class of all view models in this application.
    /// </summary>
    internal class BaseViewModel : ObservableObject
    {
        /// <summary>
        /// Performs deferred initialization of dependencies.
        /// </summary>
        public virtual void Start() { }

        /// <summary>
        /// Handles the proper disposal of dependencies of this object.
        /// </summary>
        public virtual void Stop() { }
    }
}
