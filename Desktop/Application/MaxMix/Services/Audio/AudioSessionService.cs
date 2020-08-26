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
        private IDictionary<int, IAudioSession> _devices = new ConcurrentDictionary<int, IAudioSession>();
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
            foreach (var session in _devices.Values)
                session.Dispose();

            _devices.Clear();
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
            if (_devices.Count == 0)
                return;

            foreach (var device in _devices)
            {
                var deviceSession = device.Value as AudioDevice;
                deviceSession.SetVisibleSystemSounds(value);
            }
        }

        /// <summary>
        /// Sets the volume of an audio session.
        /// </summary>
        /// <param name="id">The App Id of the target session.</param>
        /// <param name="volume">The desired volume.</param>
        public void SetSessionVolume(int id, int volume, bool isMuted)
        {
            if (!_devices.TryGetValue(id, out var session))
            {
                foreach (var device in _devices)
                {
                    var deviceSession = device.Value as AudioDevice;
                    deviceSession.SetSessionVolume(id, volume, isMuted);
                }
                return;
            }

            session.Volume = volume;
            session.IsMuted = isMuted;
        }
        #endregion

        #region Private Methods
        private void Initialize(object stateInfo)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                var device = new AudioDevice(enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia), _visibleSystemSounds);                   
                if(_devices.ContainsKey(device.ID))
                {
                    _devices[device.ID].Dispose();
                    _devices.Remove(device.ID);
                }

                _devices.Add(device.ID, device);
                OnSessionCreated(device);

                device.SessionCreated += OnSessionCreated;
                device.SessionEnded += OnSessionRemoved;
                device.VolumeChanged += OnSessionVolumeChanged;
                device.InitializeSessions();
            }
        }
        #endregion

        #region Event Handlers
        private void OnSessionVolumeChanged(IAudioSession session)
        {
            RaiseSessionVolumeChanged(session.ID, session.Volume, session.IsMuted);
        }

        private void OnSessionCreated(IAudioSession session)
        {
            RaiseSessionCreated(session.ID, session.DisplayName, session.Volume, session.IsMuted);
        }

        private void OnSessionRemoved(IAudioSession session)
        {
            if (_devices.Remove(session.ID))
            {
                var deviceSession = session as AudioDevice;
                deviceSession.SessionCreated -= OnSessionCreated;
                deviceSession.SessionEnded -= OnSessionRemoved;
                deviceSession.VolumeChanged -= OnSessionVolumeChanged;
                session.Dispose();
            }

            RaiseSessionRemoved(session.ID);
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
