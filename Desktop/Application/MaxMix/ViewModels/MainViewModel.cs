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

        private IAudioSessionService _audioSessionService;
        private CommunicationService _communicationService;
        private SettingsViewModel _settingsViewModel;

        public MainViewModel()
        {
            _settingsViewModel = new SettingsViewModel();
            _settingsViewModel.PropertyChanged += OnSettingsChanged;

            _communicationService = new CommunicationService();
            _communicationService.OnMessageRecieved += OnMessageRecieved;
            _communicationService.OnDeviceConnected += OnDeviceConnected;
            _communicationService.OnDeviceDisconnected += OnDeviceDisconnected;
            _communicationService.OnFirmwareIncompatible += OnFirmwareIncompatible;

            _audioSessionService = new AudioSessionService();
            _audioSessionService.DefaultDeviceChanged += OnDefaultDeviceChanged;
            _audioSessionService.DeviceCreated += OnDeviceCreated;
            _audioSessionService.DeviceRemoved += OnDeviceRemoved;
            _audioSessionService.DeviceVolumeChanged += OnDeviceVolumeChanged;
            _audioSessionService.SessionCreated += OnAudioSessionCreated;
            _audioSessionService.SessionRemoved += OnAudioSessionRemoved;
            _audioSessionService.SessionVolumeChanged += OnAudioSessionVolumeChanged;
            _audioSessionService.ServiceStarted += (_) => { _communicationService.Start(); };
        }

        /// <summary>
        /// Raised to indicate the the shutdown of the application has been requested.
        /// </summary>
        public event EventHandler ExitRequested;

        public override void Start()
        {
            _settingsViewModel.Start();
            _audioSessionService.Start();
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
                if (!m_IndexToId.TryGetValue(m_Sessions[i].data.id, out var sessionId))
                    continue;

                if (sessionId == id)
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

        private void OnDeviceVolumeChanged(object sender, int id, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            UpdateSessionState(id, false, volume, isMuted);
        }

        private void OnAudioSessionVolumeChanged(object sender, int id, int volume, bool isMuted)
        {
            if (!IsConnected)
                return;

            UpdateSessionState(id, false, volume, isMuted);
        }

        private void UpdateSessionState(int id, bool isDefault, int volume, bool isMuted, string name = null)
        {
            for (int i = (int)SessionIndex.INDEX_CURRENT; i < (int)SessionIndex.INDEX_MAX; i++)
            {
                if (!m_IndexToId.TryGetValue(m_Sessions[i].data.id, out var sessionId))
                    continue;

                if (sessionId == id)
                {
                    m_Sessions[i].data.isDefault = isDefault;
                    m_Sessions[i].data.volume = (byte)volume;
                    m_Sessions[i].data.isMuted = isMuted;
                    if (string.IsNullOrEmpty(name))
                    {
                        _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i].data);
                    }
                    else
                    {
                        string prevName = m_Sessions[i].name;
                        m_Sessions[i].name = name;
                        if (m_Sessions[i].name != prevName)
                            _communicationService.SendMessage(Command.CURRENT_SESSION + i, m_Sessions[i]);
                        else
                            _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i].data);
                    }
                    break;
                }
            }
        }

        private void OnDeviceCreated(object sender, int id, string displayName, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            bool isCurrentMode = deviceFlow.ToDisplayMode() == m_SessionInfo.mode;
            UpdateSessionData(id, isCurrentMode, true, deviceFlow.ToDisplayMode());
        }

        private void OnDeviceRemoved(object sender, int id, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            bool isCurrentMode = deviceFlow.ToDisplayMode() == m_SessionInfo.mode;
            UpdateSessionData(id, isCurrentMode, false, deviceFlow.ToDisplayMode());
        }

        private void OnAudioSessionCreated(object sender, int id, string displayName, int volume, bool isMuted)
        {
            if (!IsConnected)
                return;

            bool isCurrentMode = m_SessionInfo.mode == DisplayMode.MODE_APPLICATION || m_SessionInfo.mode == DisplayMode.MODE_GAME;
            UpdateSessionData(id, isCurrentMode, true, DisplayMode.MODE_APPLICATION);
        }

        private void OnAudioSessionRemoved(object sender, int id)
        {
            if (!IsConnected)
                return;

            bool isCurrentMode = m_SessionInfo.mode == DisplayMode.MODE_APPLICATION || m_SessionInfo.mode == DisplayMode.MODE_GAME;
            UpdateSessionData(id, isCurrentMode, false, DisplayMode.MODE_APPLICATION);
        }

        private void UpdateSessionData(int id, bool updateCurrent, bool addition, DisplayMode updateMode)
        {
            ISession[] sessions = _audioSessionService.GetSessions(updateMode);
            if (updateMode == DisplayMode.MODE_INPUT)
                m_SessionInfo.input = (byte)sessions.Length;
            else if (updateMode == DisplayMode.MODE_OUTPUT)
                m_SessionInfo.output = (byte)sessions.Length;
            else
                m_SessionInfo.application = (byte)sessions.Length;

            if (updateCurrent)
            {
                // Do we still have sessions left?
                if (sessions.Length == 0)
                {
                    m_SessionInfo.mode = (DisplayMode)((int)m_SessionInfo.mode + 1 % (int)DisplayMode.MODE_MAX);
                    if (m_SessionInfo.mode == DisplayMode.MODE_SPLASH)
                        m_SessionInfo.mode = DisplayMode.MODE_OUTPUT;
                    m_SessionInfo.current = 0;

                    UpdateSessionData(int.MaxValue, updateCurrent, addition, m_SessionInfo.mode);

                    _communicationService.SendMessage(Command.SESSION_INFO, m_SessionInfo);
                    return;
                }

                int currId = m_IndexToId[m_SessionInfo.current];
                if (id <= currId)
                {
                    if (addition)
                        m_SessionInfo.current++;
                    else if (id == currId)
                        m_SessionInfo.current = 0;
                    else if (m_SessionInfo.current > 0)
                        m_SessionInfo.current--;
                }
                if (addition && _settingsViewModel.DisplayNewSession)
                {
                    PopulateIndexToIdMap(sessions);
                    m_SessionInfo.current = (byte)Array.FindIndex(sessions, x => x.Id == id);
                }
                UpdateAndFlushSessionData(sessions, true);
            }
            _communicationService.SendMessage(Command.SESSION_INFO, m_SessionInfo);
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
            ISession[] sessions = _audioSessionService.GetSessions(DisplayMode.MODE_OUTPUT);
            m_SessionInfo.mode = DisplayMode.MODE_OUTPUT;
            m_SessionInfo.current = (byte)Array.FindIndex(sessions, x => x.IsDefault);
            m_SessionInfo.output = (byte)sessions.Length;
            m_SessionInfo.input = (byte)_audioSessionService.GetSessions(DisplayMode.MODE_INPUT).Length;
            m_SessionInfo.application = (byte)_audioSessionService.GetSessions(DisplayMode.MODE_APPLICATION).Length;

            UpdateAndFlushSessionData(sessions, true);

            _communicationService.SendMessage(Command.SESSION_INFO, m_SessionInfo);
        }

        private void OnMessageRecieved(Command command, IMessage message)
        {
            if (command == Command.VOLUME_CURR_CHANGE || command == Command.VOLUME_ALT_CHANGE)
            {
                // isDefault, Volume, or isMuted changed for id (index)
                VolumeData vol = (VolumeData)message;
                if (!m_IndexToId.TryGetValue(vol.id, out var sessionId))
                    return;

                var isDefault = m_Sessions[command - Command.VOLUME_CURR_CHANGE].data.isDefault;
                m_Sessions[command - Command.VOLUME_CURR_CHANGE].data = vol;
                _audioSessionService.SetItemVolume(sessionId, vol.volume, vol.isMuted);
                if (vol.isDefault && !isDefault)
                    _audioSessionService.SetDefaultEndpoint(sessionId);
            }
            else if (command == Command.SESSION_INFO)
            {
                // current, or mode
                SessionInfo info = (SessionInfo)message;
                m_SessionInfo.current = info.current;
                bool updateIndexMap = info.mode != m_SessionInfo.mode;
                m_SessionInfo.mode = info.mode;

                ISession[] sessions = _audioSessionService.GetSessions(m_SessionInfo.mode);
                UpdateAndFlushSessionData(sessions, updateIndexMap);
            }
        }

        void UpdateAndFlushSessionData(ISession[] data, bool updateIndexMap = false)
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
            previous = index;
            next = index;
            if (m_IndexToId.Count == 0)
                return;

            previous = (index - 1 + m_IndexToId.Count) % m_IndexToId.Count;
            next = (index + 1) % m_IndexToId.Count;
        }

        void PopulateIndexToIdMap(ISession[] devices)
        {
            m_IndexToId.Clear();
            for (int i = 0; i < devices.Length; i++)
                m_IndexToId[i] = devices[i].Id;
        }
    }
}
