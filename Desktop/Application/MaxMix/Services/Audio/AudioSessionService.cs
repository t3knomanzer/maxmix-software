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
    /// Provides a higher level interface to interact with windows audio
    /// devices and sessions and adds extra features.
    /// </summary>
    internal class AudioSessionService : IAudioSessionService
    {
        #region Constructor
        public AudioSessionService() { }
        #endregion

        #region Fields
        private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;
        private readonly IDictionary<int, IAudioDevice> _devices = new ConcurrentDictionary<int, IAudioDevice>();
        private readonly IDictionary<int, IAudioSession> _sessionGoups = new ConcurrentDictionary<int, IAudioSession>();
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
            // Initialization needs to happen in it's own thread for CSCore
            // to work properly.
            ThreadPool.QueueUserWorkItem(Initialize);
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (_deviceEnumerator != null)
            {
                _deviceEnumerator.DeviceAdded -= OnDeviceAdded;
                _deviceEnumerator = null;
            }

            foreach (var device in _devices.Values)
            {
                UnregisterDevice(device);
                device.Dispose();
            }

            foreach (var session in _sessionGoups.Values)
            {
                UnregisterSessionGroup(session);
                session.Dispose();
            }

            _devices.Clear();
            _sessionGoups.Clear();
        }


        /// <summary>
        /// Sets the volume of a device or session.
        /// </summary>
        /// <param name="id">The Id of the target session.</param>
        /// <param name="volume">The desired volume from 0 to 100.</param>
        /// <param name="isMuted">Wether the session should be muted.</param>
        public void SetItemVolume(int id, int volume, bool isMuted)
        {
            if (_devices.TryGetValue(id, out var device))
            {
                device.Volume = volume;
                device.IsMuted = isMuted;
            }
            else if(_sessionGoups.TryGetValue(id, out var session))
            {
                session.Volume = volume;
                session.IsMuted = isMuted;
            }
            else 
            { 
                // TODO: Raise error
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateInfo"></param>
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

        /// <summary>
        /// Checks that the session references a valid process.
        /// There are situations where we may get invalid sessions pointing
        /// at null processes which cause issues further down.
        /// </summary>
        /// <param name="session">The session to validate.</param>
        /// <returns>Wether the session is valid or not.</returns>
        private bool ValidateSession(AudioSessionControl session)
        {
            var session2 = session.QueryInterface<AudioSessionControl2>();
            return session2.Process != null;
        }

        /// <summary>
        /// Retrives the id of the group that this session belongs to.
        /// </summary>
        /// <param name="session">The session to get the id for.</param>
        /// <returns>The id used for the group of this session.</returns>
        private int GetSessionGroupId(IAudioSession session)
        {
            var fileName = (session as AudioSession).Process.GetMainModuleFileName();
            if (!string.IsNullOrEmpty(fileName))
                return fileName.GetHashCode();

            return session.Id;
        }

        /// <summary>
        /// Registers the session with the service so it's aware
        /// of events and they're handled properly. 
        /// Sessions are always groupped regardless if they belong to a parent process or not.
        /// In the case that they don't have a parent process, the group will contain just one session.
        /// </summary>
        /// <param name="session">The audio session to register.</param>
        private void RegisterSession(IAudioSession session)
        {
            var groupId = GetSessionGroupId(session);
            if (_sessionGoups.TryGetValue(groupId, out var group))
            {
                var sessionGroup = group as AudioSessionGroup;
                if (!sessionGroup.ContainsSession(session))
                    sessionGroup.AddSession(session);
                else
                    session.Dispose();
            }
            else
            {
                var sessionGroup = new AudioSessionGroup(groupId, session.DisplayName);
                sessionGroup.AddSession(session);

                _sessionGoups.Add(groupId, sessionGroup);
                sessionGroup.SessionEnded += OnSessionGroupEnded;
                sessionGroup.VolumeChanged += OnSessionGroupVolumeChanged;

                RaiseSessionCreated(groupId, sessionGroup.DisplayName, sessionGroup.Volume, sessionGroup.IsMuted);
            }
        }

        /// <summary>
        /// Unregisters the session from the service so events
        /// are not responded to anymore.
        /// </summary>
        /// <param name="sessionGroup"></param>
        private void UnregisterSessionGroup(IAudioSession sessionGroup)
        {
            sessionGroup.SessionEnded -= OnSessionGroupEnded;
            sessionGroup.VolumeChanged -= OnSessionGroupVolumeChanged;
            if (_sessionGoups.ContainsKey(sessionGroup.Id))
            {
                _sessionGoups.Remove(sessionGroup.Id);
                RaiseSessionRemoved(sessionGroup.Id);
            }

            sessionGroup.Dispose();
        }

        /// <summary>
        /// Registers the device with the service so it's aware
        /// of events and they're handled properly.
        /// </summary>
        /// <param name="device"></param>
        private void RegisterDevice(IAudioDevice device)
        {
            _devices.Add(device.ID, device);
            device.DeviceDefaultChanged += OnDefaultDeviceChanged;
            device.DeviceRemoved += OnDeviceRemoved;
            device.DeviceVolumeChanged += OnDeviceVolumeChanged;

            RaiseDeviceCreated(device.ID, device.DisplayName, device.Volume, device.IsMuted);
        }

        /// <summary>
        /// Unregisters the session from the service so events
        /// are not responded to anymore.
        /// <param name="device">The device to unregister</param>
        private void UnregisterDevice(IAudioDevice device)
        {
            device.DeviceDefaultChanged -= OnDefaultDeviceChanged;
            device.DeviceRemoved -= OnDeviceRemoved;
            device.DeviceVolumeChanged -= OnDeviceVolumeChanged;
            if (_devices.ContainsKey(device.ID))
            {
                _devices.Remove(device.ID);
                RaiseDeviceRemoved(device.ID);
            }

            device.Dispose();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles 
        /// </summary>
        /// <param name="device"></param>
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
            if(e.TryGetDevice(out var device))
                OnDeviceAdded(device);
        }

        /// <summary>
        /// Handles wrapping the device into a higher level object, registering
        /// it in the service and notifying of the event.
        /// </summary>
        /// <param name="device_">A CSCore audio device object.</param>
        private void OnDeviceAdded(MMDevice device_)
        {
            var device = new AudioDevice(device_);
            if (_devices.ContainsKey(device.ID))
            {
                device.Dispose();
                return;
            }

            RegisterDevice(device);
        }

        /// <summary>
        /// Handles the removal and notification of the device from the service.
        /// </summary>
        /// <param name="device">The device to remove</param>
        private void OnDeviceRemoved(IAudioDevice device)
        {
            UnregisterDevice(device);
        }

        /// <summary>
        /// Handles changes and notifications required when the volume
        /// of a device or it's mute state has changed.
        /// </summary>
        /// <param name="device"></param>
        private void OnDeviceVolumeChanged(IAudioDevice device)
        {
            RaiseDeviceVolumeChanged(device.ID, device.Volume, device.IsMuted);
        }

        private void OnSessionCreated(object sender, SessionCreatedEventArgs e)
        {
            OnSessionCreated(e.NewSession);
        }

        /// <summary>
        /// Handles wrapping the session into a higher level object, registering
        /// it in the service and notifying of the event.
        /// </summary>
        /// <param name="session_"></param>
        private void OnSessionCreated(AudioSessionControl session_)
        {
            var session = new AudioSession(session_);
            RegisterSession(session);
        }

        /// <summary>
        /// Handles the removal and notification of the session from the service.
        /// </summary>
        /// <param name="session"></param>
        private void OnSessionGroupEnded(IAudioSession session)
        {
            UnregisterSessionGroup(session);
        }

        /// <summary>
        /// Handles changes and notifications required when the volume
        /// of a device or it's mute state has changed.
        /// </summary>
        /// <param name="session"></param>
        private void OnSessionGroupVolumeChanged(IAudioSession session)
        {
            RaiseSessionVolumeChanged(session.Id, session.Volume, session.IsMuted);
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
