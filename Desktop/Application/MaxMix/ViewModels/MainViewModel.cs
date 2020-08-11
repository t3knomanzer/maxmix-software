using MaxMix.Framework;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CSCore.CoreAudioAPI;
using System.Diagnostics;
using MaxMix.Services;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using MaxMix.Framework.Mvvm;
using MaxMix.Services.Communication;
using MaxMix.Services.Audio;
using System.Windows.Forms;

namespace MaxMix.ViewModels
{
    /// <summary>
    /// Main application view model class to be used as data context.
    /// </summary>
    internal class MainViewModel : BaseViewModel
    {
        #region Constructor
        public MainViewModel()
        {
            _serializationService = new CobsSerializationService();
            _serializationService.RegisterType<MessageHandShakeRequest>(0);
            _serializationService.RegisterType<MessageHandShakeResponse>(1);
            _serializationService.RegisterType<MessageDisplayData>(2);

            _settingsViewModel = new SettingsViewModel();
            _settingsViewModel.PropertyChanged += OnSettingsChanged;

            _deviceViewModel = new DeviceViewModel();

            _audioSessionService = new AudioSessionService(_settingsViewModel.SystemSounds);
            _audioSessionService.SessionCreated += OnAudioSessionCreated;
            _audioSessionService.SessionRemoved += OnAudioSessionRemoved;
            _audioSessionService.SessionVolumeChanged += OnAudioSessionVolumeChanged;
            
            _discoveryService = new DiscoveryService(_serializationService);
            _discoveryService.DeviceDiscovered += OnDeviceDiscovered;

            _communicationService = new CommunicationService(_serializationService);
            _communicationService.MessageReceived += OnMessageReceived;
            _communicationService.Error += OnCommunicationError;
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised to indicate the the shutdown of the application has been requested.
        /// </summary>
        public event EventHandler ExitRequested;
        #endregion

        #region Consts
        private const int _baudRate = 115200;
        #endregion
         
        #region Fields
        private ISerializationService _serializationService;
        private IAudioSessionService _audioSessionService;
        private IDiscoveryService _discoveryService;
        private ICommunicationService _communicationService;
        private bool _isActive;
        private bool _isConnected;
        private SettingsViewModel _settingsViewModel;
        private DeviceViewModel _deviceViewModel;
        private ICommand _activateCommand;
        private ICommand _deactivateCommand;
        private ICommand _requestExitCommand;
        #endregion

        #region Properties
        /// <summary>
        /// Holds the current state of the application.
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// Status of the connection to a maxmix device.
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        /// <summary>
        /// Holds a reference to an instance of a settings view model.
        /// </summary>
        public SettingsViewModel SettingsViewModel
        {
            get => _settingsViewModel;
            private set => SetProperty(ref _settingsViewModel, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public DeviceViewModel DeviceViewModel
        {
            get => _deviceViewModel;
            private set => SetProperty(ref _deviceViewModel, value);
        }
        #endregion

        #region Commands
        /// <summary>
        /// Sets the active state of the application to true.
        /// </summary>
        public ICommand ActivateCommand
        {
            get
            {
                if (_activateCommand == null)
                    _activateCommand = new DelegateCommand(() => IsActive = true);
                return _activateCommand;
            }
        }

        /// <summary>
        /// Sets the active state of the application to false.
        /// </summary>
        public ICommand DeactivateCommand
        {
            get
            {
                if (_deactivateCommand == null)
                    _deactivateCommand = new DelegateCommand(() => IsActive = false);
                return _deactivateCommand;
            }
        }

        /// <summary>
        /// Requests the shutdown process and notifies others by raising the ExitRequested event.
        /// </summary>
        public ICommand RequestExitCommand
        {
            get
            {
                if (_requestExitCommand == null)
                    _requestExitCommand = new DelegateCommand(() => RaiseExitRequested());
                return _requestExitCommand;
            }
        }
        #endregion

        #region Overrides

        public override void Start()
        {
            _discoveryService.Start(_baudRate);
            _settingsViewModel.Start();
            _deviceViewModel.Start();
        }

        public override void Stop()
        {
            _audioSessionService.Stop();
            _discoveryService.Stop();
            _communicationService.Stop();
            _settingsViewModel.Stop();
            _deviceViewModel.Stop();
        }
        #endregion

        #region Public Methods
        byte[] _pixels;
        public void SetPixels(byte[] pixels)
        {
            _pixels = pixels;
        }
        #endregion

        #region Private Methods
        private void RaiseExitRequested()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region EventHandlers
        private void OnAudioSessionCreated(object sender, int id, string displayName, int volume, bool isMuted)
        {
        }
         
        private void OnAudioSessionRemoved(object sender, int id)
        {
        }

        private void OnAudioSessionVolumeChanged(object sender, int id, int volume, bool isMuted)
        {
        }

        private void OnDeviceDiscovered(object sender, string portName)
        {
            IsConnected = true;

            _communicationService.Start(portName, _baudRate);
            _audioSessionService.Start();

            var msg = new MessageDisplayData(_pixels);
            _communicationService.Send(msg);
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void OnMessageReceived(object sender, IMessage message)
        {
        }

        private void OnCommunicationError(object sender, string e)
        {
            IsConnected = false;

            Stop();
            Start();
        }
        #endregion
    }
}