using MaxMix.Framework;
using MaxMix.Framework.Mvvm;
using MaxMix.Services.Audio;
using MaxMix.Services.Communication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Sentry;

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
        ModeStates m_ModeStates = ModeStates.Default();
        Dictionary<int, int> m_IndexToId = new Dictionary<int, int>();
        bool m_HasPreviouslyConnected = false;

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

        private void SendMessage(Command command, IMessage message)
        {
            if (!IsConnected)
                return;

            _communicationService.SendMessage(command, message);
        }

        private void OnDefaultDeviceChanged(object sender, int id, DeviceFlow deviceFlow)
        {
            for (int i = (int)SessionIndex.INDEX_PREVIOUS; i < (int)SessionIndex.INDEX_MAX; i++)
            {
                if (!m_IndexToId.TryGetValue(m_Sessions[i].data.id, out var sessionId))
                    continue;

                if (sessionId == id)
                {
                    m_Sessions[i].data.isDefault = true;
                    SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                }
                else if (m_Sessions[i].data.isDefault)
                {
                    m_Sessions[i].data.isDefault = false;
                    SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                }
            }
        }

        private void OnDeviceVolumeChanged(object sender, int id, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            UpdateSessionState(id, false, volume, isMuted);
        }

        private void OnAudioSessionVolumeChanged(object sender, int id, int volume, bool isMuted)
        {
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
                        SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i].data);
                    }
                    else
                    {
                        string prevName = m_Sessions[i].name;
                        m_Sessions[i].name = name;
                        if (m_Sessions[i].name != prevName)
                            SendMessage(Command.CURRENT_SESSION + i, m_Sessions[i]);
                        else
                            SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i].data);
                    }
                }
            }
        }

        private void OnDeviceCreated(object sender, int id, string displayName, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            bool isCurrentMode = deviceFlow.ToDisplayMode() == m_SessionInfo.mode;
            UpdateSessionData(id, isCurrentMode, true);
        }

        private void OnDeviceRemoved(object sender, int id, DeviceFlow deviceFlow)
        {
            bool isCurrentMode = deviceFlow.ToDisplayMode() == m_SessionInfo.mode;
            UpdateSessionData(id, isCurrentMode, false);
        }

        private void OnAudioSessionCreated(object sender, int id, string displayName, int volume, bool isMuted)
        {
            bool isCurrentMode = m_SessionInfo.mode == DisplayMode.MODE_APPLICATION || m_SessionInfo.mode == DisplayMode.MODE_GAME;
            UpdateSessionData(id, isCurrentMode, true);
        }

        private void OnAudioSessionRemoved(object sender, int id)
        {
            bool isCurrentMode = m_SessionInfo.mode == DisplayMode.MODE_APPLICATION || m_SessionInfo.mode == DisplayMode.MODE_GAME;
            UpdateSessionData(id, isCurrentMode, false);
        }

        private void UpdateSessionData(int id, bool IsCurrentMode, bool addition)
        {
            _audioSessionService.GetSessionCounts(out var output, out var input, out int application);
            m_SessionInfo.input = (byte)input;
            m_SessionInfo.output = (byte)output;
            m_SessionInfo.application = (byte)application;

            if (IsCurrentMode)
            {
                ISession[] sessions = _audioSessionService.GetSessions(m_SessionInfo.mode);
                // Do we still have sessions left?
                if (sessions.Length == 0)
                {
                    m_SessionInfo.mode = (DisplayMode)((int)m_SessionInfo.mode + 1 % (int)DisplayMode.MODE_MAX);
                    if (m_SessionInfo.mode == DisplayMode.MODE_SPLASH)
                        m_SessionInfo.mode = DisplayMode.MODE_OUTPUT;
                    sessions = _audioSessionService.GetSessions(m_SessionInfo.mode);
                    m_SessionInfo.current = FindSessionIndex(sessions, x => x.IsDefault);
                    UpdateSessionData(int.MaxValue, IsCurrentMode, addition);
                    return;
                }

                int currId = m_IndexToId[m_SessionInfo.current];
                if (id <= currId)
                {
                    if (addition)
                        m_SessionInfo.current++;
                    else if (id == currId)
                        m_SessionInfo.current = FindSessionIndex(sessions, x => x.IsDefault);
                    else if (m_SessionInfo.current > 0)
                        m_SessionInfo.current--;
                }

                if (addition && _settingsViewModel.DisplayNewSession && m_SessionInfo.mode != DisplayMode.MODE_GAME)
                {
                    PopulateIndexToIdMap(sessions);
                    m_SessionInfo.current = FindSessionIndex(sessions, x => x.Id == id);
                }

                UpdateAndFlushSessionData(sessions, true);
            }

            SendMessage(Command.SESSION_INFO, m_SessionInfo);
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            DeviceSettings settings = _settingsViewModel.ToDeviceSettings();
            SendMessage(Command.SETTINGS, settings);
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

            if (!m_HasPreviouslyConnected)
                m_SessionInfo.mode = _settingsViewModel.StartupMode;

            ISession[] sessions = _audioSessionService.GetSessions(m_SessionInfo.mode);
            _audioSessionService.GetSessionCounts(out var output, out var input, out int application);

            if (!m_HasPreviouslyConnected)
                m_SessionInfo.current = FindSessionIndex(sessions, x => x.IsDefault);
            m_SessionInfo.output = (byte)output;
            m_SessionInfo.input = (byte)input;
            m_SessionInfo.application = (byte)application;

            UpdateAndFlushSessionData(sessions, true);

            SendMessage(Command.MODE_STATES, m_ModeStates);
            SendMessage(Command.SESSION_INFO, m_SessionInfo);

            m_HasPreviouslyConnected = true;
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
                bool isNewMode = info.mode != m_SessionInfo.mode;
                m_SessionInfo.mode = info.mode;

                ISession[] sessions = _audioSessionService.GetSessions(m_SessionInfo.mode);
                if (isNewMode)
                    m_SessionInfo.current = FindSessionIndex(sessions, x => x.IsDefault);
                UpdateAndFlushSessionData(sessions, isNewMode);
                if (isNewMode)
                    SendMessage(Command.SESSION_INFO, m_SessionInfo);
            }
            else if (command == Command.ALTERNATE_SESSION)
            {
                // This comes from game mode and tells us what it selected.
                SessionData data = (SessionData)message;
                m_Sessions[(int)SessionIndex.INDEX_ALTERNATE] = data;
            }
            else if (command == Command.MODE_STATES)
            {
                ModeStates data = (ModeStates)message;
                m_ModeStates = data;
            }
        }

        byte FindSessionIndex(ISession[] sessions, Predicate<ISession> match)
        {
            var index = Array.FindIndex(sessions, match);
            if (index >= 0)
                return (byte)index;
            return 0;
        }

        void UpdateAndFlushSessionData(ISession[] data, bool updateIndexMap = false)
        {
            if (updateIndexMap)
                PopulateIndexToIdMap(data);

            int index = m_SessionInfo.current;
            if (index < 0 || index >= data.Length)
            {
                // Something caused session to get out of bounds, reset it to 0
                index = 0;
                m_SessionInfo.current = 0;
                SendMessage(Command.SESSION_INFO, m_SessionInfo);
            }
            ComputeIndexes(index, out int prevIndex, out int nextIndex);

            // The device can easily spin the encoder faster than we can respond via Serial.
            // So just flush all 3 sessions to the device to ensure it will have fresh data when it stops, whatever it stops on.
            m_Sessions[(int)SessionIndex.INDEX_CURRENT] = data.ToSessionData(index);
            m_Sessions[(int)SessionIndex.INDEX_PREVIOUS] = data.ToSessionData(prevIndex);
            m_Sessions[(int)SessionIndex.INDEX_NEXT] = data.ToSessionData(nextIndex);
            SendMessage(Command.CURRENT_SESSION, m_Sessions[(int)SessionIndex.INDEX_CURRENT]);
            SendMessage(Command.PREVIOUS_SESSION, m_Sessions[(int)SessionIndex.INDEX_PREVIOUS]);
            SendMessage(Command.NEXT_SESSION, m_Sessions[(int)SessionIndex.INDEX_NEXT]);
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
