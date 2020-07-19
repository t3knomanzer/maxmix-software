using CSCore.CoreAudioAPI;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides functionality to interact with windows audio sessions.
    /// </summary>
    internal class AudioSessionService : IAudioSessionService
    {
        #region Constructor
        public AudioSessionService(bool visibleSystemSounds = false)
        {
            _synchronizationContext = SynchronizationContext.Current;
            _visibleSystemSounds = visibleSystemSounds;
        }
        #endregion

        #region Fields
        private readonly SynchronizationContext _synchronizationContext;
        private IDictionary<int, IAudioSession> _sessions = new ConcurrentDictionary<int, IAudioSession>();
        private bool _visibleSystemSounds = false;
        #endregion

        #region Events
        /// <summary>
        /// Raised when a new audio session has been created.
        /// </summary>
        public event AudioSessionCreatedDelegate SessionCreated;

        /// <summary>
        /// Raised when a previously active audio session has been removed.
        /// </summary>
        public event AudioSessionRemovedDelegate SessionRemoved;

        /// <summary>
        /// Raised when the volume for an active session has changed.
        /// </summary>
        public event AudioSessionVolumeDelegate SessionVolumeChanged;
        #endregion

        #region Public Methods
        /// <summary>
        /// Deferred initialization of dependencies.
        /// </summary>
        public void Start()
        {
            ThreadPool.QueueUserWorkItem(Initialize);
        }

        /// <summary>
        /// Deferred disposal of dependencies used by this instance.
        /// </summary>
        public void Stop()
        {
            foreach (var group in _sessions.Values)
                group.Dispose();

            _sessions.Clear();
        }

        /// <summary>
        /// Toggles the displaying of the System Sounds audio session.
        /// </summary>
        /// <param name="value">The desiered visibility of system sounds.</param>
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
                    UnregisterSession(session);
            }
            else
            {
                // Add sessions for system sounds
                var deviceSessions = _sessions.Where(x => x.Value is AudioDevice).Select(x => x.Value as AudioDevice).ToArray();
                foreach (var session in deviceSessions)
                    session.InitializeSystemSessions();
            }
        }

        /// <summary>
        /// Sets the volume of an audio session.
        /// </summary>
        /// <param name="id">The App Id of the target session.</param>
        /// <param name="volume">The desired volume.</param>
        public void SetSessionVolume(int id, int volume, bool isMuted)
        {
            if (!_sessions.TryGetValue(id, out var session))
                return;

            session.Volume = volume;
            session.IsMuted = isMuted;
        }
        #endregion

        #region Private Methods
        private void Initialize(object stateInfo)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var device = new AudioDevice(enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));
                device.SessionCreated += OnSessionCreated;
                RegisterSession(device);
                if (_visibleSystemSounds)
                    device.InitializeSystemSessions();
                device.InitializeSessions();
            }
        }

        private void RegisterSession(IAudioSession session)
        {
            _sessions.Add(session.ID, session);
            session.SessionEnded += OnSessionRemoved;
            session.VolumeChanged += OnSessionVolumeChanged;
            RaiseSessionCreated(session.ID, session.DisplayName, session.Volume, session.IsMuted);
        }

        private void UnregisterSession(IAudioSession session)
        {
            if (!_sessions.Remove(session.ID))
                return;

            session.SessionEnded -= OnSessionRemoved;
            session.VolumeChanged -= OnSessionVolumeChanged;

            var id = session.ID;
            session.Dispose();
            RaiseSessionRemoved(id);
        }
        #endregion

        #region Event Handlers
        private void OnSessionVolumeChanged(IAudioSession session)
        {
            RaiseSessionVolumeChanged(session.ID, session.Volume, session.IsMuted);
        }

        private void OnSessionCreated(IAudioSession session)
        {
            // All sessions comming in are AudioSession types from AudioDevice types
            // QUESTION: Should grouping be done at the service level, or the device level?
            // Service level is data oriented, Device level is object oriented.
            // For now lets handle it here and change it based on the review
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

            RegisterSession(session);
        }

        private void OnSessionRemoved(IAudioSession session)
        {
            UnregisterSession(session);
        }
        #endregion

        #region Event Dispatchers
        private void RaiseSessionCreated(int id, string displayName, int volume, bool isMuted)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => SessionCreated?.Invoke(this, id, displayName, volume, isMuted), null);
            else
                SessionCreated.Invoke(this, id, displayName, volume, isMuted);
        }

        private void RaiseSessionRemoved(int id)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => SessionRemoved?.Invoke(this, id), null);
            else
                SessionRemoved?.Invoke(this, id);
        }

        private void RaiseSessionVolumeChanged(int id, int volume, bool isMuted)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => SessionVolumeChanged?.Invoke(this, id, volume, isMuted), null);
            else
                SessionVolumeChanged?.Invoke(this, id, volume, isMuted);
        }
        #endregion
    }

}
