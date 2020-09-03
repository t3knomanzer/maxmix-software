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
        private readonly IDictionary<int, AudioSessionManager2 >_sessionManagers = new ConcurrentDictionary<int, AudioSessionManager2>();
        private readonly MMDeviceEnumerator _deviceEnumerator = new MMDeviceEnumerator();
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
            foreach (var device in _devices.Values)
            {
                UnregisterDevice(device);
                device.Dispose();
            }

            foreach (var sessionGroup in _sessionGoups.Values)
            {
                UnregisterSessionGroup(sessionGroup);
                sessionGroup.Dispose();
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
            foreach (var device in _deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
                OnDeviceAdded(device);

            var defaultDevice = _devices.Values.FirstOrDefault(o => o.IsDefault);
            if (defaultDevice != null)
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
            if (_sessionGoups.Remove(sessionGroup.Id))
                RaiseSessionRemoved(sessionGroup.Id);

            sessionGroup.Dispose();
        }

        /// <summary>
        /// Registers the device with the service so it's aware
        /// of events and they're handled properly.
        /// </summary>
        /// <param name="device"></param>
        private void RegisterDevice(IAudioDevice device)
        {
            if (_devices.ContainsKey(device.Id))
            {
                device.Dispose();
                return;
            }

            _devices.Add(device.Id, device);
            device.DeviceDefaultChanged += OnDefaultDeviceChanged;
            device.DeviceVolumeChanged += OnDeviceVolumeChanged;

            var sessionManager = AudioSessionManager2.FromMMDevice(device.Device);
            sessionManager.SessionCreated += OnSessionCreated;
            _sessionManagers.Add(device.Id, sessionManager);

            RaiseDeviceCreated(device.Id, device.DisplayName, device.Volume, device.IsMuted);

            foreach (var session in sessionManager.GetSessionEnumerator())
            {
                if (ValidateSession(session))
                    OnSessionCreated(session);
            }
        }

        /// <summary>
        /// Unregisters the session from the service so events
        /// are not responded to anymore.
        /// <param name="device">The device to unregister</param>
        private void UnregisterDevice(IAudioDevice device)
        {
            if (_sessionManagers.ContainsKey(device.Id))
            {
                _sessionManagers[device.Id].SessionCreated -= OnSessionCreated;
                _sessionManagers.Remove(device.Id);
            }

            device.DeviceDefaultChanged -= OnDefaultDeviceChanged;
            device.DeviceVolumeChanged -= OnDeviceVolumeChanged;

            if (_devices.ContainsKey(device.Id))
            {
                _devices.Remove(device.Id);
                RaiseDeviceRemoved(device.Id);
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
            RaiseDefaultDeviceChanged(device.Id);
        }

        /// <summary>
        /// Handles wrapping the device into a higher level object, registering
        /// it in the service and notifying of the event.
        /// </summary>
        /// <param name="device_">A CSCore audio device object.</param>
        private void OnDeviceAdded(MMDevice device_)
        {
            var device = new AudioDevice(device_);
            RegisterDevice(device);
        }

        /// <summary>
        /// Handles changes and notifications required when the volume
        /// of a device or it's mute state has changed.
        /// </summary>
        /// <param name="device"></param>
        private void OnDeviceVolumeChanged(IAudioDevice device)
        {
            RaiseDeviceVolumeChanged(device.Id, device.Volume, device.IsMuted);
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
