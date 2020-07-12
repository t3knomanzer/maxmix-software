using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FirmwareInstaller.Services
{
    /// <summary>
    /// Manages installing firmware to a maxmix device using avrdude.
    /// </summary>
    internal class InstallService : BaseService
    {
        #region Constructor
        public InstallService()
        {
            var rootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            _exeFilePath = Path.Combine(rootPath, "Resources", "avrdude.exe");
            _configFilePath = Path.Combine(rootPath, "Resources", "avrdude.conf");
        }

        #endregion

        #region Events
        #endregion

        #region Fields
        private readonly string _exeFilePath;
        private readonly string _configFilePath;
        #endregion

        #region Properties
        #endregion

        #region Private Methods
        #endregion

        #region Public Methods
        /// <summary>
        /// Installs the firmware file to the given port. The port should have a maxmix
        /// device plugged in and not be in use by other applications.
        /// </summary>
        /// <param name="filePath">The absolute local file path to the .hex firmware file.</param>
        /// <param name="port">The COM port where the maxmix device is plugged in.</param>
        /// <returns></returns>
        public Task InstallAsync(string filePath, string port, bool useOldBootloader)
        {
            return Task.Run(() =>
            {
                var baudRate = useOldBootloader ? "57600" : "115200";

                var processInfo = new ProcessStartInfo
                {
                    FileName = _exeFilePath,
                    Arguments = $"-C\"{_configFilePath}\" -v -patmega328p -carduino -P{port} -b{baudRate} -D -U\"flash:w:{filePath}:i\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = new Process() { StartInfo = processInfo };
                process.OutputDataReceived += OnOutputDataReceived;
                process.ErrorDataReceived += OnErrorDataReceived;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                process.OutputDataReceived -= OnOutputDataReceived;
                process.ErrorDataReceived -= OnErrorDataReceived;
                process.Dispose();
            });
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            RaiseError(e.Data);
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            RaiseError(e.Data);
        }
        #endregion

        #region Event Methods
        #endregion
    }
}
