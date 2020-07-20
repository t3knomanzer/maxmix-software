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
            _callback.NotifyRecived += OnVolumeChanged;
            _sessionManager.SessionCreated += OnSessionCreated;

            DisplayName = _device.FriendlyName;
            ID = _device.DeviceID.GetHashCode();
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
        #endregion

        #region Properties
        /// <inheritdoc/>
        public int ID { get; protected set; }

        /// <inheritdoc/>
        public string DisplayName { get; protected set; }

        /// <inheritdoc/>
        public bool IsSystemSound { get; protected set; }

        /// <inheritdoc/>
        public int Volume
        {
            get => (int)Math.Round(_endpointVolume.MasterVolumeLevelScalar * 100);
            set
            {
                if (Volume == value)
                    return;

                _isNotifyEnabled = false;
                _endpointVolume.MasterVolumeLevelScalar = value / 100f;
                _isNotifyEnabled = true;
            }
        }

        /// <inheritdoc/>
        public bool IsMuted
        {
            get => _endpointVolume.IsMuted;
            set
            {
                if (IsMuted == value)
                    return;

                _isNotifyEnabled = false;
                _endpointVolume.IsMuted = value;
                _isNotifyEnabled = true;
            }
        }
        #endregion

        #region Private Methods
        private void OnVolumeChanged(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            if (!_isNotifyEnabled)
                return;

            // Convert to IAudioSession and call OnSessionVolumeChanged
            OnSessionVolumeChanged(this);
        }

        private void OnSessionVolumeChanged(IAudioSession session)
        {
            VolumeChanged?.Invoke(session);
        }

        private void OnSessionCreated(object sender, SessionCreatedEventArgs e)
        {
            // Convert to IAudioSession and call OnSessionCreated
            OnSessionCreated(new AudioSession(e.NewSession));
        }

        private void OnSessionCreated(IAudioSession session)
        {
            if (!_visibleSystemSounds && session.IsSystemSound)
            {
                session.Dispose();
                return;
            }

            var audioSession = session as AudioSession;
            var fileName = audioSession.Process.GetMainModuleFileName();

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
            session.SessionEnded += OnSessionRemoved;
            session.VolumeChanged += OnSessionVolumeChanged;

            // Raise session created event
            SessionCreated?.Invoke(session);
        }

        private void OnSessionRemoved(IAudioSession session)
        {
            if (!_sessions.Remove(session.ID))
                return;

            session.SessionEnded -= OnSessionRemoved;
            session.VolumeChanged -= OnSessionVolumeChanged;
            session.Dispose();
            SessionEnded?.Invoke(session);
        }
        #endregion

        #region Public Methods
        public void InitializeSystemSessions()
        {
            if (!_visibleSystemSounds)
                return;

            using (var sessionEnumerator = _sessionManager.GetSessionEnumerator())
            {
                foreach (var session in sessionEnumerator)
                {
                    var audioSession = new AudioSession(session);
                    if (audioSession.IsSystemSound)
                        OnSessionCreated(new AudioSession(session));
                    else
                        audioSession.Dispose();
                }
            }
        }

        public void InitializeSessions()
        {
            using (var sessionEnumerator = _sessionManager.GetSessionEnumerator())
            {
                foreach (var session in sessionEnumerator)
                {
                    var audioSession = new AudioSession(session);
                    if (!audioSession.IsSystemSound)
                        OnSessionCreated(new AudioSession(session));
                    else
                        audioSession.Dispose();
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
                    OnSessionRemoved(session);
            }
            else
            {
                // Add sessions for system sounds
                InitializeSystemSessions();
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            foreach (var session in _sessions.Values)
            {
                session.SessionEnded -= OnSessionRemoved;
                session.VolumeChanged -= OnSessionVolumeChanged;
                session.Dispose();
            }

            _sessions.Clear();

            _callback.NotifyRecived -= OnVolumeChanged;
            _sessionManager.SessionCreated -= OnSessionCreated;

            _endpointVolume?.UnregisterControlChangeNotify(_callback);
            _endpointVolume?.Dispose();
            _endpointVolume = null;

            _sessionManager?.Dispose();
            _sessionManager = null;

            _device?.Dispose();
            _device = null;
        }
        #endregion
    }
}
