using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirmwareInstaller.Services
{
    /// <summary>
    /// Provides functionality to find available COM ports.
    /// </summary>
    internal class DiscoveryService : BaseService
    {
        #region Constructor
        public DiscoveryService() {}
        #endregion

        #region Public Methods
        /// <summary>
        /// Retrieves a list of availbale COM ports.
        /// </summary>
        /// <returns>List of available COM ports.</returns>
        public IEnumerable<string> Discover()
        {
            var result = SerialPort.GetPortNames();
            return result.ToList();
        }
        #endregion
    }
}
