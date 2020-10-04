using MaxMix.Framework;
using MaxMix.Framework.Mvvm;
using MaxMix.Services.Audio;
using MaxMix.Services.Communication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace MaxMix.ViewModels
{
    /// <summary>
    /// Main application view model class to be used as data context.
    /// </summary>
    internal class MainViewModel : BaseViewModel
    {
        #region UI Bindings
        /// <summary>
        /// Holds a reference to an instance of a settings view model.
        /// </summary>
        public SettingsViewModel SettingsViewModel
        {
            get => _settingsViewModel;
            private set => SetProperty(ref _settingsViewModel, value);
        }

        /// <summary>
        /// Holds the current state of the application.
        /// </summary>
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// Status of the connection to a maxmix device.
        /// </summary>
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        /// <summary>
        /// Sets the active state of the application to true.
        /// </summary>
        private ICommand _activateCommand;
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
        private ICommand _deactivateCommand;
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
        private ICommand _requestExitCommand;
        public ICommand RequestExitCommand
        {
            get
            {
                if (_requestExitCommand == null)
                    _requestExitCommand = new DelegateCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));
                return _requestExitCommand;
            }
        }
        #endregion

        // Device State Tracking
        SessionInfo m_SessionInfo = SessionInfo.Default();
        SessionData[] m_Sessions = new SessionData[(int)SessionIndex.INDEX_MAX] { SessionData.Default(), SessionData.Default(), SessionData.Default(), SessionData.Default() };
        Dictionary<int, int> m_IndexToId = new Dictionary<int, int>();
        Dictionary<int, int> m_IdToIndex = new Dictionary<int, int>();

        private IAudioSessionService _audioSessionService;
        private CommunicationService _communicationService;
        private SettingsViewModel _settingsViewModel;

        public MainViewModel()
        {
            _settingsViewModel = new SettingsViewModel();
            _settingsViewModel.PropertyChanged += OnSettingsChanged;

            _audioSessionService = new AudioSessionService();
            _audioSessionService.DefaultDeviceChanged += OnDefaultDeviceChanged;
            _audioSessionService.DeviceCreated += OnDeviceCreated;
            _audioSessionService.DeviceRemoved += OnDeviceRemoved;
            _audioSessionService.DeviceVolumeChanged += OnDeviceVolumeChanged;
            _audioSessionService.SessionCreated += OnAudioSessionCreated;
            _audioSessionService.SessionRemoved += OnAudioSessionRemoved;
            _audioSessionService.SessionVolumeChanged += OnAudioSessionVolumeChanged;

            _communicationService = new CommunicationService();
            _communicationService.OnMessageRecieved += OnMessageRecieved;
            _communicationService.OnDeviceConnected += OnDeviceConnected;
            _communicationService.OnDeviceDisconnected += OnDeviceDisconnected;
            _communicationService.OnFirmwareIncompatible += OnFirmwareIncompatible;
        }

        /// <summary>
        /// Raised to indicate the the shutdown of the application has been requested.
        /// </summary>
        public event EventHandler ExitRequested;

        public override void Start()
        {
            _communicationService.Start();
            _audioSessionService.Start();
            _settingsViewModel.Start();
        }

        public override void Stop()
        {
            _communicationService.Stop();
            _audioSessionService.Stop();
            _settingsViewModel.Stop();
        }

        private void OnDefaultDeviceChanged(object sender, int id, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            for (int i = (int)SessionIndex.INDEX_PREVIOUS; i < (int)SessionIndex.INDEX_MAX; i++)
            {
                if (m_Sessions[i].data.id == id)
                {
                    m_Sessions[i].data.isDefault = true;
                    _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                }
                else if (m_Sessions[i].data.isDefault)
                {
                    m_Sessions[i].data.isDefault = false;
                    _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                }
            }
        }

        private void OnDeviceCreated(object sender, int id, string displayName, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            if (m_SessionInfo.mode == DisplayMode.MODE_APPLICATION || m_SessionInfo.mode == DisplayMode.MODE_GAME)
                return;

            UpdateSessionData(id, deviceFlow, true);
        }

        private void OnDeviceRemoved(object sender, int id, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            if (m_SessionInfo.mode == DisplayMode.MODE_APPLICATION || m_SessionInfo.mode == DisplayMode.MODE_GAME)
                return;

            UpdateSessionData(id, deviceFlow, false);
        }

        private void UpdateSessionData(int id, DeviceFlow deviceFlow, bool addition)
        {
            IAudioDevice[] outputs = _audioSessionService.GetAudioDevices(DeviceFlow.Output);
            IAudioDevice[] inputs = _audioSessionService.GetAudioDevices(DeviceFlow.Input);
            m_SessionInfo.output = (byte)outputs.Length;
            m_SessionInfo.input = (byte)inputs.Length;
            if (deviceFlow.ToDisplayMode() == m_SessionInfo.mode)
            {
                int index = m_IdToIndex[id];
                if (index <= m_SessionInfo.current && m_SessionInfo.current > 0)
                {
                    if (addition)
                        m_SessionInfo.current++;
                    else
                        m_SessionInfo.current--;
                }
            }

            UpdateAndFlushSessionData(deviceFlow == DeviceFlow.Input ? inputs : outputs, true);
            _communicationService.SendMessage(Command.SESSION_INFO, m_SessionInfo);
        }

        private void OnDeviceVolumeChanged(object sender, int id, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            UpdateSessionState(id, false, volume, isMuted);
        }

        private void OnAudioSessionCreated(object sender, int id, string displayName, int volume, bool isMuted)
        {
            if (!IsConnected)
                return;

            if (m_SessionInfo.mode != DisplayMode.MODE_APPLICATION && m_SessionInfo.mode != DisplayMode.MODE_GAME)
                return;

            UpdateSessionData(id, true);
        }

        private void OnAudioSessionRemoved(object sender, int id)
        {
            if (!IsConnected)
                return;

            if (m_SessionInfo.mode != DisplayMode.MODE_APPLICATION && m_SessionInfo.mode == DisplayMode.MODE_GAME)
                return;

            UpdateSessionData(id, false);
        }

        private void UpdateSessionData(int id, bool addition)
        {
            IAudioSession[] sessions = _audioSessionService.GetAudioSessions();
            int index = m_IdToIndex[id];
            if (index <= m_SessionInfo.current && m_SessionInfo.current > 0)
            {
                if (addition)
                    m_SessionInfo.current++;
                else
                    m_SessionInfo.current--;
            }
            m_SessionInfo.application = (byte)sessions.Length;

            UpdateAndFlushSessionData(sessions, true);
            _communicationService.SendMessage(Command.SESSION_INFO, m_SessionInfo);
        }

        private void OnAudioSessionVolumeChanged(object sender, int id, int volume, bool isMuted)
        {
            if (!IsConnected)
                return;

            UpdateSessionState(id, false, volume, isMuted);
        }

        private void UpdateSessionState(int id, bool isDefault, int volume, bool isMuted, string name = null)
        {
            for (int i = (int)SessionIndex.INDEX_PREVIOUS; i < (int)SessionIndex.INDEX_MAX; i++)
            {
                if (m_Sessions[i].data.id == id)
                {
                    m_Sessions[i].data.isDefault = isDefault;
                    m_Sessions[i].data.volume = (byte)volume;
                    m_Sessions[i].data.isMuted = isMuted;
                    if (string.IsNullOrEmpty(name))
                        _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                    else
                    {
                        string prevName = m_Sessions[i].name;
                        m_Sessions[i].name = name;
                        if (m_Sessions[i].name != prevName)
                            _communicationService.SendMessage(Command.CURRENT_SESSION + i, m_Sessions[i]);
                        else
                            _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                    }
                    break;
                }
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!IsConnected)
                return;

            DeviceSettings settings = _settingsViewModel.ToDeviceSettings();
            _communicationService.SendMessage(Command.SETTINGS, settings);
        }

        /****************************************
         * Communication Events
         ****************************************/
        private void OnFirmwareIncompatible(string obj)
        {
            // TODO: Display msg to user that firmware needs to be updated
        }

        private void OnDeviceDisconnected()
        {
            IsConnected = false;
        }

        private void OnDeviceConnected()
        {
            IsConnected = true;
            OnSettingsChanged(null, null);
            // Send device initial screen data

            // NOTE: we can now have a setting to determin the initial screen
            IAudioDevice[] outputs = _audioSessionService.GetAudioDevices(DeviceFlow.Output);
            m_SessionInfo.mode = DisplayMode.MODE_OUTPUT;
            m_SessionInfo.current = (byte)Array.FindIndex(outputs, x => x.IsDefault);
            m_SessionInfo.output = (byte)outputs.Length;
            m_SessionInfo.input = (byte)_audioSessionService.GetAudioDevices(DeviceFlow.Input).Length;
            m_SessionInfo.application = (byte)_audioSessionService.GetAudioSessions().Length;

            UpdateAndFlushSessionData(outputs, true);

            _communicationService.SendMessage(Command.SESSION_INFO, m_SessionInfo);
        }

        private void OnMessageRecieved(Command command, IMessage message)
        {
            if (command == Command.VOLUME_CURR_CHANGE)
            {
                // isDefault, Volume, or isMuted changed for id (index)
                VolumeData vol = (VolumeData)message;
                _audioSessionService.SetItemVolume(m_IndexToId[vol.id], vol.volume, vol.isMuted);
            } 
            else if (command == Command.SESSION_INFO)
            {
                // current, or mode
                SessionInfo info = (SessionInfo)message;
                m_SessionInfo.current = info.current;
                bool updateIndexMap = info.mode != m_SessionInfo.mode;
                m_SessionInfo.mode = info.mode;

                if (m_SessionInfo.mode == DisplayMode.MODE_APPLICATION || m_SessionInfo.mode == DisplayMode.MODE_GAME)
                {
                    IAudioSession[] sessions = _audioSessionService.GetAudioSessions();
                    UpdateAndFlushSessionData(sessions, updateIndexMap);
                }
                else
                {
                    IAudioDevice[] devices = _audioSessionService.GetAudioDevices(m_SessionInfo.mode == DisplayMode.MODE_INPUT ? DeviceFlow.Input : DeviceFlow.Output);
                    UpdateAndFlushSessionData(devices, updateIndexMap);
                }
            }
        }

        void UpdateAndFlushSessionData(IAudioDevice[] data, bool updateIndexMap = false)
        {
            int index = m_SessionInfo.current;
            if (updateIndexMap)
                PopulateIndexToIdMap(data);
            ComputeIndexes(index, out int prevIndex, out int nextIndex);

            // The device can easily spin the encoder faster than we can respond via Serial.
            // So just flush all 3 sessions to the device to ensure it will have fresh data when it stops, whatever it stops on.
            m_Sessions[(int)SessionIndex.INDEX_CURRENT] = data[index].ToSessionData(index);
            m_Sessions[(int)SessionIndex.INDEX_PREVIOUS] = data[prevIndex].ToSessionData(prevIndex);
            m_Sessions[(int)SessionIndex.INDEX_NEXT] = data[nextIndex].ToSessionData(nextIndex);
            _communicationService.SendMessage(Command.CURRENT_SESSION, m_Sessions[(int)SessionIndex.INDEX_CURRENT]);
            _communicationService.SendMessage(Command.PREVIOUS_SESSION, m_Sessions[(int)SessionIndex.INDEX_PREVIOUS]);
            _communicationService.SendMessage(Command.NEXT_SESSION, m_Sessions[(int)SessionIndex.INDEX_NEXT]);
        }

        void UpdateAndFlushSessionData(IAudioSession[] data, bool updateIndexMap = false)
        {
            int index = m_SessionInfo.current;
            if (updateIndexMap)
                PopulateIndexToIdMap(data);
            ComputeIndexes(index, out int prevIndex, out int nextIndex);

            // The device can easily spin the encoder faster than we can respond via Serial.
            // So just flush all 3 sessions to the device to ensure it will have fresh data when it stops, whatever it stops on.
            m_Sessions[(int)SessionIndex.INDEX_CURRENT] = data[index].ToSessionData(index);
            m_Sessions[(int)SessionIndex.INDEX_PREVIOUS] = data[prevIndex].ToSessionData(prevIndex);
            m_Sessions[(int)SessionIndex.INDEX_NEXT] = data[nextIndex].ToSessionData(nextIndex);
            _communicationService.SendMessage(Command.CURRENT_SESSION, m_Sessions[(int)SessionIndex.INDEX_CURRENT]);
            _communicationService.SendMessage(Command.PREVIOUS_SESSION, m_Sessions[(int)SessionIndex.INDEX_PREVIOUS]);
            _communicationService.SendMessage(Command.NEXT_SESSION, m_Sessions[(int)SessionIndex.INDEX_NEXT]);
        }

        void ComputeIndexes(int index, out int previous, out int next)
        {
            previous = (index - 1 + m_IndexToId.Count) % m_IndexToId.Count;
            next = (index + 1) % m_IndexToId.Count;
        }

        void PopulateIndexToIdMap(IAudioDevice[] devices)
        {
            m_IndexToId.Clear();
            m_IdToIndex.Clear();
            for (int i = 0; i < devices.Length; i++)
            {
                m_IndexToId[i] = devices[i].Id;
                m_IdToIndex[devices[i].Id] = i;
            }
        }

        void PopulateIndexToIdMap(IAudioSession[] sessions)
        {
            m_IndexToId.Clear();
            m_IdToIndex.Clear();
            for (int i = 0; i < sessions.Length; i++)
            {
                m_IndexToId[i] = sessions[i].Id;
                m_IdToIndex[sessions[i].Id] = i;
            }
        }
    }
}
 