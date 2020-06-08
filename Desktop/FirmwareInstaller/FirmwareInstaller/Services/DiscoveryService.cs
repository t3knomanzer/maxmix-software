using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirmwareInstaller.Services
{
    internal class DiscoveryService : BaseService
    {
        #region Constructor
        public DiscoveryService() {}
        #endregion

        #region Public Methods
        public IEnumerable<string> Discover()
        {
            var result = SerialPort.GetPortNames();
            return result.ToList();
        }
        #endregion
    }
}
