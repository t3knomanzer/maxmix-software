using CSCore.CoreAudioAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over the MMDevice CSCore class.
    /// </summary>
    public class AudioDevice : IAudioSession
    {
        #region Constructor
        public AudioDevice(MMDevice device, bool visibleSystemSounds = false)
        {
            _device = device;
            _visibleSystemSounds = visibleSystemSounds;

            _sessionManager = AudioSessionManager2.FromMMDevice(_device);
            _endpointVolume = AudioEndpointVolume.FromDevice(_device);

            _endpointVolume.RegisterControlChangeNotify(_callback);
            _callback.NotifyRecived += OnEndpointVolumeChanged;
            _sessionManager.SessionCreated += OnSessionCreated;
        }
        #endregion

        #region Events
        /// <inheritdoc/>
        public event Action<IAudioSession> VolumeChanged;

        /// <inheritdoc/>
        public event Action<IAudioSession> SessionEnded;

        /// <summary>
        /// Occurs when the audio session has been created.
        /// </summary>
        public event Action<IAudioSession> SessionCreated;
        #endregion

        #region Fields
        private AudioEndpointVolumeCallback _callback = new AudioEndpointVolumeCallback();
        private MMDevice _device;
        private AudioSessionManager2 _sessionManager;
        private AudioEndpointVolume _endpointVolume;

        private IDictionary<int, IAudioSession> _sessions = new ConcurrentDictionary<int, IAudioSession>();
        private bool _visibleSystemSounds = false;
        private bool _isNotifyEnabled = true;
        
        private int _volume;
        private bool _isMuted;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public int ID => _device.DeviceID.GetHashCode();

        /// <inheritdoc/>
        public string DisplayName => "Master";

        /// <inheritdoc/>
        public bool IsSystemSound { get; protected set; }

        /// <inheritdoc/>
        public int Volume
        {
            get
            {
                try { _volume = (int)Math.Round(_endpointVolume.MasterVolumeLevelScalar * 100); }
                catch { }

                return _volume;
            }
            set
            {
                if (_volume == value)
                    return;

                _isNotifyEnabled = false;
                _volume = value;
                _endpointVolume.MasterVolumeLevelScalar = value / 100f;
            }
        }

        /// <inheritdoc/>
        public bool IsMuted
        {
            get
            {
                try { _isMuted = _endpointVolume.IsMuted; }
                catch { }

                return _isMuted;
            }
            set
            {
                if (_isMuted == value)
                    return;

                _isNotifyEnabled = false;
                _isMuted = value;
                _endpointVolume.IsMuted = value;
            }
        }
        #endregion

        #region Private Methods
        private void RegisterSession(IAudioSession session)
        {
            if (!_visibleSystemSounds && session.IsSystemSound)
            {
                session.Dispose();
                return;
            }

            var audioSession = session as AudioSession;
            var fileName = audioSession.Process.GetMainModuleFileName(); // QUESTION: Should we use session.GroupingParam instead?

            // If we are able to grab the fileName for the process, group it with sessions from the same fileName
            if (!string.IsNullOrEmpty(fileName))
            {
                var groupID = fileName.GetHashCode();

                AudioSessionGroup sessionGroup;
                if (_sessions.TryGetValue(groupID, out var group))
                {
                    // We have a previously constrcuted group, so just add this session to that group and early out.
                    sessionGroup = group as AudioSessionGroup;
                    sessionGroup.AddSession(session);
                    return;
                }

                // Need to create a new group for this session and register it
                sessionGroup = new AudioSessionGroup(groupID, session.DisplayName);
                sessionGroup.AddSession(session);
                session = sessionGroup;
            }

            _sessions.Add(session.ID, session);
            session.SessionEnded += OnSessionEnded;
            session.VolumeChanged += OnSessionVolumeChanged;

            SessionCreated?.Invoke(session);
        }

        private bool ValidateSession(AudioSessionControl session)
        {
            var session2 = session.QueryInterface<AudioSessionControl2>();
            return session2.Process != null;
        }
        #endregion

        #region Public Methods
        public void InitializeSessions()
        {
            using (var sessionEnumerator = _sessionManager.GetSessionEnumerator())
            {
                foreach (var session in sessionEnumerator)
                {
                   if(ValidateSession(session))
                        RegisterSession(new AudioSession(session));
                }
            }
        }

        public void SetVisibleSystemSounds(bool value)
        {
            if (_visibleSystemSounds == value)
                return;

            _visibleSystemSounds = value;
            if (_sessions.Count == 0)
                return;

            if (!_visibleSystemSounds)
            {
                // Remove existing sessions
                var systemSessions = _sessions.Where(x => x.Value.IsSystemSound).Select(x => x.Value).ToArray();
                foreach (var session in systemSessions)
                    OnSessionEnded(session);
            }
            else
            {
                // Add sessions for system sounds
                InitializeSessions();
            }
        }

        public void SetSessionVolume(int id, int volume, bool isMuted)
        {
            if (!_sessions.TryGetValue(id, out var session))
                return;

            session.Volume = volume;
            session.IsMuted = isMuted;
        }
        #endregion

        #region Event Handlers
        private void OnSessionCreated(object sender, SessionCreatedEventArgs e)
        {
            if (ValidateSession(e.NewSession))
                RegisterSession(new AudioSession(e.NewSession));
        }

        private void OnEndpointVolumeChanged(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            if (!_isNotifyEnabled)
            {
                _isNotifyEnabled = true;
                return;
            }

            VolumeChanged?.Invoke(this);
        }

        private void OnSessionVolumeChanged(IAudioSession session)
        {
            VolumeChanged?.Invoke(session);
        }

        private void OnSessionEnded(IAudioSession session)
        {
            if (!_sessions.Remove(session.ID))
                return;

            session.SessionEnded -= OnSessionEnded;
            session.VolumeChanged -= OnSessionVolumeChanged;
            session.Dispose();

            SessionEnded?.Invoke(session);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            foreach (var session in _sessions.Values)
            {
                session.SessionEnded -= OnSessionEnded;
                session.VolumeChanged -= OnSessionVolumeChanged;
                session.Dispose();
            }

            _sessions.Clear();

            _callback.NotifyRecived -= OnEndpointVolumeChanged;
            _sessionManager.SessionCreated -= OnSessionCreated;

            _endpointVolume?.UnregisterControlChangeNotify(_callback);
            _endpointVolume?.Dispose();
            _endpointVolume = null;
            _callback = null;

            _sessionManager?.Dispose();
            _sessionManager = null;

            _device?.Dispose();
            _device = null;
        }
        #endregion
    }
}
