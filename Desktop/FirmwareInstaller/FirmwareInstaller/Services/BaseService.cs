using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirmwareInstaller.Services
{
    class BaseService : IService
    {
        #region IService
        public event EventHandler<string> Error;

        public virtual void Start()
        {
        }

        public virtual void Stop()
        {
        }
        #endregion

        #region Protected Methods
        protected virtual void RaiseError(string message)
        {
            Error?.Invoke(this, message);
        }
        #endregion
    }
}
