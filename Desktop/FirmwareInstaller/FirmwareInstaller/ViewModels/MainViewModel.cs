using System;
using System.Timers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using FirmwareInstaller.Services;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using FirmwareInstaller.Framework.Mvvm;
using FirmwareInstaller.Services.Update;
using System.Runtime.CompilerServices;
using Octokit;

namespace FirmwareInstaller.ViewModels
{
    internal class MainViewModel : BaseViewModel
    {
        #region Constructor
        public MainViewModel()
        {
            _logLock = new object();

            _discoveryService = new DiscoveryService();
            _discoveryService.Error += OnServiceError;

            _downloadService = new DownloadService();
            _downloadService.Error += OnServiceError;

            _installService = new InstallService();
            _installService.Error += OnServiceError;

            IsBusy = true;
            InitLog();
            InitPorts();
            InitVersions();
            IsBusy = false;
        }
        #endregion

        #region Events
        public event EventHandler ExitRequested;
        #endregion

        #region Consts
        private readonly object _logLock;
        #endregion

        #region Fields
        private DiscoveryService _discoveryService;
        private DownloadService _downloadService;
        private InstallService _installService;

        private DelegateCommand _installCommand;
        private DelegateCommand _requestExitCommand;

        private string _selectedPort;
        private string _selectedVersion;
        private IList<string> _logList;
        private string _log;
        private bool _isBusy = false;
        private bool _useOldBootloader = true;
        #endregion

        #region Properties
        /// <summary>
        /// Indicates that sensitive operations are taking place and user 
        /// input should be blocked.
        /// </summary>
        public bool IsBusy 
        { 
            get => _isBusy;
            private set
            {
                _isBusy = value;
                RaisePropertyChanged();
                InstallCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Selected COM port.
        /// </summary>
        public string SelectedPort
        {
            get => _selectedPort;
            set
            {
                _selectedPort = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Selected version to download.
        /// </summary>
        public string SelectedVersion
        {
            get => _selectedVersion;
            set
            {
                _selectedVersion = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Indicates wether if the connected board is using
        /// the new or old bootloader. This is determines the baudrate
        /// used to upload the sketch.
        /// </summary>
        public bool UseOldBootloader
        {
            get => _useOldBootloader;
            set
            {
                _useOldBootloader = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// List of available COM ports.
        /// </summary>
        public ObservableCollection<string> Ports { get; private set; }

        /// <summary>
        /// List of available versions to download.
        /// </summary>
        public ObservableCollection<string> Versions { get; private set; }

        /// <summary>
        /// Plain text operations log.
        /// </summary>
        public string Log
        {
            get => _log;
            private set
            {
                _log = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands
        /// <summary>
        /// Starts the installation process using the selected COM
        /// port and version.
        /// </summary>
        public DelegateCommand InstallCommand
        {
            get
            {
                if (_installCommand == null)
                    _installCommand = new DelegateCommand(() => Install(), CanInstall);
                return _installCommand;
            }
        }

        /// <summary>
        /// Raises the exitRequested event to start the process to shutdown the application.
        /// </summary>
        public DelegateCommand RequestExitCommand
        {
            get
            {
                if (_requestExitCommand == null)
                    _requestExitCommand = new DelegateCommand(() => RaiseExitRequested());
                return _requestExitCommand;
            }
        }
        #endregion

        #region Public Methods
        public override void Start()
        {
        }

        public override void Stop()
        {
        }
        #endregion

        #region Private Methods
        private void InitPorts()
        {
            SendLog("Discovering COM devices...");
            var ports = _discoveryService.Discover();
            Ports = new ObservableCollection<string>(ports);

            SelectedPort = Ports.FirstOrDefault();
        }

        private async void InitVersions()
        {
            SendLog("Retrieving available versions...");
            Versions = new ObservableCollection<string>();

            var versions = await _downloadService.RetrieveVersions();
            foreach (var version in versions)
                Versions.Add(version);

            SelectedVersion = Versions.FirstOrDefault();
        }

        private void InitLog()
        {
            lock(_logLock)
                _logList = new List<string>();
        }

        private void ClearLog()
        {
            lock (_logLock)
                _logList.Clear();

            Log = string.Empty;
        }

        private void SendLog(string message)
        {
            lock (_logLock)
                _logList.Add(message);

            var logString = string.Empty;

            lock (_logLock)
                foreach (var item in _logList)
                    logString += $"{item}\n";

            Log = logString;
        }

        private bool CanInstall()
        {
            return !string.IsNullOrEmpty(SelectedPort) &&
                   !string.IsNullOrEmpty(SelectedVersion) &&
                   !IsBusy;
        }

        private async void Install()
        {
            IsBusy = true;
            ClearLog();

            SendLog($"Downloading version {_selectedVersion}...");
            var versionFilePath = await _downloadService.DownloadVersionAsync(_selectedVersion);
            if (string.IsNullOrEmpty(versionFilePath))
            {
                IsBusy = false;
                return;
            }

            SendLog($"Installing file {versionFilePath} to {_selectedPort}");
            await _installService.InstallAsync(versionFilePath, _selectedPort, _useOldBootloader);

            IsBusy = false;
        }

        private void RaiseExitRequested()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region EventHandlers
        private void OnServiceError(object sender, string e)
        {
            SendLog(e);
        }
        #endregion

        #region Overrides
        protected override void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            base.RaisePropertyChanged(name);

            if (name == nameof(SelectedPort) || name == nameof(SelectedVersion) || name == nameof(IsBusy))
                InstallCommand.RaiseCanExecuteChanged();
        }
        #endregion
    }
}