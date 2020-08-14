using CSCore.CoreAudioAPI;
using Sentry.Protocol;
using System;
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
        public AudioSessionService()
        {
            _synchronizationContext = SynchronizationContext.Current;
        }
        #endregion

        #region Fields
        private readonly SynchronizationContext _synchronizationContext;
        private readonly IDictionary<int, IAudioDevice> _devices = new ConcurrentDictionary<int, IAudioDevice>();
        private readonly IDictionary<int, IAudioSession> _sessions = new ConcurrentDictionary<int, IAudioSession>();
        private MMDeviceEnumerator _deviceEnumerator;
        private AudioSessionManager2 _sessionManager;
        #endregion

        #region Events
        /// <inheritdoc/>
        public event DefaultAudioDeviceChangedDelegate DefaultDeviceChanged;

        /// <inheritdoc/>
        public event AudioDeviceCreatedDelegate DeviceCreated;

        /// <inheritdoc/>
        public event AudioDeviceRemovedDelegate DeviceRemoved;

        /// <inheritdoc/>
        public event AudioDeviceVolumeDelegate DeviceVolumeChanged;

        /// <inheritdoc/>
        public event AudioSessionCreatedDelegate SessionCreated;

        /// <inheritdoc/>
        public event AudioSessionRemovedDelegate SessionRemoved;

        /// <inheritdoc/>
        public event AudioSessionVolumeDelegate SessionVolumeChanged;
        #endregion

        #region Public Methods
        /// <inheritdoc/>
        public void Start()
        {
            ThreadPool.QueueUserWorkItem(Initialize);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            foreach (AudioDevice device in _devices.Values)
            {
                device.SessionCreated -= OnSessionCreated;
                device.SessionEnded -= OnSessionRemoved;
                device.VolumeChanged -= OnSessionVolumeChanged;
                device.Dispose();
            }

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

                _deviceEnumerator = null;
            }

            if(_sessionManager != null)
            {
                _sessionManager.SessionCreated -= OnSessionCreated;
                _sessionManager.Dispose();

                _sessionManager = null;
            }
        }

        /// <summary>
        /// Sets the volume of an audio session.
        /// </summary>
        /// <param name="id">The App Id of the target session.</param>
        /// <param name="volume">The desired volume.</param>
        public void SetSessionVolume(int id, int volume, bool isMuted)
        {
            if (_devices.TryGetValue(id, out var device))
            {
                device.Volume = volume;
                device.IsMuted = isMuted;
                return;
            }

            if (_sessions.TryGetValue(id, out var session))
            {
                session.Volume = volume;
                session.IsMuted = isMuted;
                return;
            }
        }
        #endregion

        #region Private Methods
        private void Initialize(object stateInfo)
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _deviceEnumerator.DeviceAdded += OnDeviceAdded;

            foreach (var device in _deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
                OnDeviceAdded(device);

            var defaultDevice = _devices.Values.FirstOrDefault(o => o.IsDefault);
            if(defaultDevice != null)
                OnDefaultDeviceChanged(defaultDevice);
        }

        private bool ValidateSession(AudioSessionControl session)
        {
            var session2 = session.QueryInterface<AudioSessionControl2>();
            return session2.Process != null;
        }

        private void RegisterSession(IAudioSession session_)
        {  
            var session = session_ as AudioSession;
            var fileName = session.Process.GetMainModuleFileName();

            // If we are able to grab the fileName for the process, group it with sessions from the same fileName
            if (!string.IsNullOrEmpty(fileName))
            {
                var device = new AudioDevice(enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia), _visibleSystemSounds);                   

                // TODO: We are getting crashes sometimes caused by the key already existing. This should never happen since Stop should always be called
                // before Start but for some reason it is happening.
                if(_devices.ContainsKey(device.ID))
                    Stop();

                _devices.Add(device.ID, device);
                OnSessionCreated(device);

                device.SessionCreated += OnSessionCreated;
                device.SessionEnded += OnSessionRemoved;
                device.VolumeChanged += OnSessionVolumeChanged;
                device.InitializeSessions();
            }

            _sessions.Add(session_.ID, session_);
            session_.SessionEnded += OnSessionEnded;
            session_.VolumeChanged += OnSessionVolumeChanged;

            RaiseSessionCreated(session_.ID, session_.DisplayName, session_.Volume, session_.IsMuted);
        }
        #endregion

        #region Event Handlers
        private void OnDefaultDeviceChanged(IAudioDevice device)
        {
            if (_sessionManager != null)
            {
                _sessionManager.SessionCreated -= OnSessionCreated;
                _sessionManager.Dispose();
            }

            _sessionManager = AudioSessionManager2.FromMMDevice(device.Device);
            _sessionManager.SessionCreated += OnSessionCreated;

            RaiseDefaultDeviceChanged(device.ID);

            foreach (var session in _sessionManager.GetSessionEnumerator())
            {
                if(ValidateSession(session))
                    OnSessionCreated(session);
            }
        }

        private void OnDeviceAdded(object sender, DeviceNotificationEventArgs e)
        {
            e.TryGetDevice(out var device);
            if (device != null)
                OnDeviceAdded(device);
        }

        private void OnDeviceAdded(MMDevice device_)
        {
            var device = new AudioDevice(device_);
            if (_devices.ContainsKey(device.ID))
            {
                device.Dispose();
                return;
            }

            _devices.Add(device.ID, device);
            device.DeviceDefaultChanged += OnDefaultDeviceChanged;
            device.DeviceRemoved += OnDeviceRemoved;
            device.DeviceVolumeChanged += OnDeviceVolumeChanged;

            RaiseDeviceCreated(device.ID, device.DisplayName, device.Volume, device.IsMuted);
        }

        private void OnDeviceRemoved(IAudioDevice device)
        {
            RaiseDeviceRemoved(device.ID);
        }

        private void OnDeviceVolumeChanged(IAudioDevice device)
        {
            RaiseDeviceVolumeChanged(device.ID, device.Volume, device.IsMuted);
        }

        private void OnSessionCreated(object sender, SessionCreatedEventArgs e)
        {
            OnSessionCreated(e.NewSession);
        }

        private void OnSessionCreated(AudioSessionControl session_)
        {
            var session = new AudioSession(session_);
            if (_sessions.ContainsKey(session.ID))
            {
                session.Dispose();
                return;
            }

            RegisterSession(session);
        }

        private void OnSessionEnded(IAudioSession session)
        {
            if (_sessions.Remove(session.ID)) 
            {
                session.SessionEnded -= OnSessionEnded;
                session.VolumeChanged -= OnSessionVolumeChanged;
                session.Dispose();
            }

            RaiseSessionRemoved(session.ID);
        }

        private void OnSessionVolumeChanged(IAudioSession session)
        {
            RaiseSessionVolumeChanged(session.ID, session.Volume, session.IsMuted);
        }
        #endregion

        #region Event Dispatchers
        private void RaiseDefaultDeviceChanged(int id)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => DefaultDeviceChanged?.Invoke(this, id), null);
            else
                DefaultDeviceChanged.Invoke(this, id);
        }

        private void RaiseDeviceCreated(int id, string displayName, int volume, bool isMuted)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => DeviceCreated?.Invoke(this, id, displayName, volume, isMuted), null);
            else
                DeviceCreated.Invoke(this, id, displayName, volume, isMuted);
        }

        private void RaiseDeviceRemoved(int id)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => DeviceRemoved?.Invoke(this, id), null);
            else
                DeviceRemoved?.Invoke(this, id);
        }

        private void RaiseDeviceVolumeChanged(int id, int volume, bool isMuted)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => DeviceVolumeChanged?.Invoke(this, id, volume, isMuted), null);
            else
                DeviceVolumeChanged?.Invoke(this, id, volume, isMuted);
        }

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
