using MaxMix.Framework;
using MaxMix.Services.Communication;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
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
    internal class AudioSessionService : IAudioSessionService, IMMNotificationClient
    {
        private readonly NLog.Logger m_Logger = NLog.LogManager.GetCurrentClassLogger();

        #region Constructor
        public AudioSessionService() { }
        #endregion

        #region Fields
        private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;
        private readonly IDictionary<int, IAudioDevice> _devices = new ConcurrentDictionary<int, IAudioDevice>();
        private readonly IDictionary<int, IAudioSession> _sessionGoups = new ConcurrentDictionary<int, IAudioSession>();
        private readonly IDictionary<int, AudioSessionManager> _sessionManagers = new ConcurrentDictionary<int, AudioSessionManager>();
        private readonly MMDeviceEnumerator _deviceEnumerator = new MMDeviceEnumerator();
        private bool _stopping = false;
        #endregion

        #region Events
        /// <inheritdoc/>
        public event ServiceStartedDelegate ServiceStarted;

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
            _stopping = true;
            _deviceEnumerator.UnregisterEndpointNotificationCallback(this);

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
            else if (_sessionGoups.TryGetValue(id, out var session))
            {
                session.Volume = volume;
                session.IsMuted = isMuted;
            }
            else
            {
                // TODO: Raise error
            }
        }

        public void SetDefaultEndpoint(int id)
        {
            if (_devices.TryGetValue(id, out var device))
            {
                using (var policyConfig = new PolicyConfig())
                    policyConfig.SetDefaultEndpoint(device.DeviceId, Role.Multimedia);
            }

        }

        public ISession[] GetSessions(DisplayMode mode)
        {
            // TODO: Not sure if it is safe to return IEnumerable<ISession> from ConcurrentDictionary
            if (mode == DisplayMode.MODE_APPLICATION || mode == DisplayMode.MODE_GAME)
                return _sessionGoups.Values.OrderBy(x => x.Id).ToArray();
            if (mode == DisplayMode.MODE_INPUT)
                return _devices.Values.Where(x => x.Flow == DeviceFlow.Input).OrderBy(x => x.Id).ToArray();
            if (mode == DisplayMode.MODE_OUTPUT)
                return _devices.Values.Where(x => x.Flow == DeviceFlow.Output).OrderBy(x => x.Id).ToArray();
            return null;
        }

        public void GetSessionCounts(out int output, out int input, out int application)
        {
            output = _devices.Values.Count(x => x.Flow == DeviceFlow.Output);
            input = _devices.Values.Count(x => x.Flow == DeviceFlow.Input);
            application = _sessionGoups.Values.Count;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateInfo"></param>
        private void Initialize(object stateInfo)
        {
            _stopping = false;
            _deviceEnumerator.RegisterEndpointNotificationCallback(this);

            foreach (var device in _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
            {
                OnDeviceAdded(device);
            }

            foreach (var device in _devices.Values)
            {
                if (device.IsDefault)
                {
                    OnDefaultDeviceChanged(device);
                }
            }

            _synchronizationContext.Post(o => ServiceStarted?.Invoke(this), null);
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
            //AppLogging.DebugLog(nameof(RegisterSession), session.SessionIdentifier, session.DisplayName, session.Id.ToString());
            if (_sessionGoups.TryGetValue(session.Id, out var group))
            {
                var sessionGroup = group as AudioSessionGroup;
                if (!sessionGroup.ContainsSession(session))
                    sessionGroup.AddSession(session);
                else
                    session.Dispose();
            }
            else
            {
                var sessionGroup = new AudioSessionGroup(session.Id, session.DisplayName, session.IsDefault);
                sessionGroup.AddSession(session);

                _sessionGoups.Add(sessionGroup.Id, sessionGroup);
                sessionGroup.SessionEnded += OnSessionGroupEnded;
                sessionGroup.VolumeChanged += OnSessionGroupVolumeChanged;

                RaiseSessionCreated(sessionGroup.Id, sessionGroup.DisplayName, sessionGroup.Volume, sessionGroup.IsMuted);
            }
        }

        /// <summary>
        /// Unregisters the session from the service so events
        /// are not responded to anymore.
        /// </summary>
        /// <param name="session">The audio session to unregister.</param>
        private void UnregisterSessionGroup(IAudioSession session)
        {
            //AppLogging.DebugLog(nameof(UnregisterSessionGroup), session.SessionIdentifier, session.DisplayName);
            session.SessionEnded -= OnSessionGroupEnded;
            session.VolumeChanged -= OnSessionGroupVolumeChanged;
            if (_sessionGoups.Remove(session.Id))
                RaiseSessionRemoved(session.Id);

            session.Dispose();
        }

        /// <summary>
        /// Registers the device with the service so it's aware
        /// of events and they're handled properly.
        /// </summary>
        /// <param name="device"></param>
        private void RegisterDevice(IAudioDevice device)
        {
            m_Logger.Debug(string.Join("\t", nameof(RegisterDevice), device.DeviceId, device.DisplayName, device.Device.DataFlow));
            if (_devices.ContainsKey(device.Id))
            {
                device.Dispose();
                return;
            }

            _devices.Add(device.Id, device);
            device.DeviceDefaultChanged += OnDefaultDeviceChanged;
            device.DeviceVolumeChanged += OnDeviceVolumeChanged;
            device.DeviceRemoved += OnDeviceRemoved;

            RaiseDeviceCreated(device.Id, device.DisplayName, device.Volume, device.IsMuted, device.Flow);

            if (device.Flow == DeviceFlow.Output)
            {
                var sessionManager = device.Device.AudioSessionManager;
                sessionManager.OnSessionCreated += OnSessionCreated;
                _sessionManagers.Add(device.Id, sessionManager);

                var sessions = sessionManager.Sessions;
                for (int i = 0; i < sessions.Count; i++)
                    OnSessionCreated(sessions[i]);
            }
        }

        /// <summary>
        /// Unregisters the session from the service so events
        /// are not responded to anymore.
        /// <param name="device">The device to unregister</param>
        private void UnregisterDevice(IAudioDevice device)
        {
            m_Logger.Debug(string.Join("\t", nameof(UnregisterDevice), device.DeviceId, device.DisplayName));
            if (_sessionManagers.ContainsKey(device.Id))
            {
                _sessionManagers[device.Id].OnSessionCreated -= OnSessionCreated;
                _sessionManagers.Remove(device.Id);
            }

            device.DeviceDefaultChanged -= OnDefaultDeviceChanged;
            device.DeviceVolumeChanged -= OnDeviceVolumeChanged;
            device.DeviceRemoved -= OnDeviceRemoved;

            if (_devices.ContainsKey(device.Id))
            {
                _devices.Remove(device.Id);
                RaiseDeviceRemoved(device.Id, device.Flow);
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
            RaiseDefaultDeviceChanged(device.Id, device.Flow);
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
            RaiseDeviceVolumeChanged(device.Id, device.Volume, device.IsMuted, device.Flow);
        }

        private void OnSessionCreated(object sender, IAudioSessionControl newSession)
        {
            OnSessionCreated(new AudioSessionControl(newSession));
        }

        /// <summary>
        /// Handles wrapping the session into a higher level object, registering
        /// it in the service and notifying of the event.
        /// </summary>
        /// <param name="session_"></param>
        private void OnSessionCreated(AudioSessionControl session_)
        {
            var session = new AudioSession(session_);
            m_Logger.Debug(string.Join("\t", nameof(OnSessionCreated), session.SessionIdentifier, session.DisplayName, session.Id));
            RegisterSession(session);
        }

        /// <summary>
        /// Handles the removal and notification of the session from the service.
        /// </summary>
        /// <param name="sessionGroup"></param>
        private void OnSessionGroupEnded(IAudioSession sessionGroup)
        {
            UnregisterSessionGroup(sessionGroup);
        }

        /// <summary>
        /// Handles changes and notifications required when the volume
        /// of a device or it's mute state has changed.
        /// </summary>
        /// <param name="sessionGroup"></param>
        private void OnSessionGroupVolumeChanged(IAudioSession sessionGroup)
        {
            RaiseSessionVolumeChanged(sessionGroup.Id, sessionGroup.Volume, sessionGroup.IsMuted);
        }
        #endregion

        #region Event Dispatchers
        private void RaiseDefaultDeviceChanged(int id, DeviceFlow deviceFlow)
        {
            if (_stopping) return;
            if (SynchronizationContext.Current != _synchronizationContext)
            {
                _synchronizationContext.Post(o => DefaultDeviceChanged?.Invoke(this, id, deviceFlow), null);
            }
            else
            {
                DefaultDeviceChanged.Invoke(this, id, deviceFlow);
            }
        }

        private void RaiseDeviceCreated(int id, string displayName, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            if (_stopping) return;
            if (SynchronizationContext.Current != _synchronizationContext)
            {
                _synchronizationContext.Post(o => DeviceCreated?.Invoke(this, id, displayName, volume, isMuted, deviceFlow), null);
            }
            else
            {
                DeviceCreated.Invoke(this, id, displayName, volume, isMuted, deviceFlow);
            }
        }

        private void RaiseDeviceRemoved(int id, DeviceFlow deviceFlow)
        {
            if (_stopping) return;
            if (SynchronizationContext.Current != _synchronizationContext)
            {
                _synchronizationContext.Post(o => DeviceRemoved?.Invoke(this, id, deviceFlow), null);
            }
            else
            {
                DeviceRemoved?.Invoke(this, id, deviceFlow);
            }
        }

        private void RaiseDeviceVolumeChanged(int id, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            if (_stopping) return;
            if (SynchronizationContext.Current != _synchronizationContext)
            {
                _synchronizationContext.Post(o => DeviceVolumeChanged?.Invoke(this, id, volume, isMuted, deviceFlow), null);
            }
            else
            {
                DeviceVolumeChanged?.Invoke(this, id, volume, isMuted, deviceFlow);
            }
        }

        private void RaiseSessionCreated(int id, string displayName, int volume, bool isMuted)
        {
            if (_stopping) return;
            if (SynchronizationContext.Current != _synchronizationContext)
            {
                _synchronizationContext.Post(o => SessionCreated?.Invoke(this, id, displayName, volume, isMuted), null);
            }
            else
            {
                SessionCreated.Invoke(this, id, displayName, volume, isMuted);
            }
        }

        private void RaiseSessionRemoved(int id)
        {
            if (_stopping) return;
            if (SynchronizationContext.Current != _synchronizationContext)
            {
                _synchronizationContext.Post(o => SessionRemoved?.Invoke(this, id), null);
            }
            else
            {
                SessionRemoved?.Invoke(this, id);
            }
        }

        private void RaiseSessionVolumeChanged(int id, int volume, bool isMuted)
        {
            if (_stopping) return;
            if (SynchronizationContext.Current != _synchronizationContext)
            {
                _synchronizationContext.Post(o => SessionVolumeChanged?.Invoke(this, id, volume, isMuted), null);
            }
            else
            {
                SessionVolumeChanged?.Invoke(this, id, volume, isMuted);
            }
        }

        void IMMNotificationClient.OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            if (newState.HasFlag(DeviceState.Active))
            {
                try
                {
                    MMDevice device = _deviceEnumerator.GetDevice(deviceId);
                    OnDeviceAdded(device);
                }
                catch { }
            }
        }

        void IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId)
        {
            try
            {
                MMDevice device = _deviceEnumerator.GetDevice(pwstrDeviceId);
                OnDeviceAdded(device);
            }
            catch { }
        }

        void IMMNotificationClient.OnDeviceRemoved(string deviceId)
        {
            // NOOP
        }

        void IMMNotificationClient.OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            // NOOP
        }

        void IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // NOOP
        }
        #endregion
    }

}
