using CSCore.CoreAudioAPI;
using MaxMix.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

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
        private AudioSessionManager2 _sessionManager;
        private AudioEndpointVolumeWrapper _endpointVolumeWrapper;
        private IDictionary<int, AudioSessionWrapper> _wrappers;
        #endregion

        #region Events
        /// <summary>
        /// Raised when the volume for an active session has changed.
        /// </summary>
        public event AudioEndpointDelegate EndpointCreated;

        /// <summary>
        /// Raised when the volume for an active session has changed.
        /// </summary>
        public event AudioEndpointVolumeDelegate EndpointVolumeChanged;

        /// <summary>
        /// Raised when a new audio session has been created.
        /// </summary>
        public event AudioSessionDelegate SessionCreated;

        /// <summary>
        /// Raised when a previously active audio session has been removed.
        /// </summary>
        public event EventHandler<int> SessionRemoved;

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
            _wrappers = new Dictionary<int, AudioSessionWrapper>();
            new Thread(() => InitializeWrappers()).Start();
        }

        /// <summary>
        /// Deferred disposal of dependencies used by this instance.
        /// </summary>
        public void Stop()
        {
            if (_sessionManager != null)
            {
                _sessionManager.SessionCreated -= OnSessionCreated;
                _sessionManager.Dispose();
            }

            if(_endpointVolumeWrapper != null)
            {
                _endpointVolumeWrapper.Dispose();
            }

            if (_wrappers != null)
            {
                foreach (var wrapper in _wrappers.Values)
                    wrapper.Dispose();

                _wrappers.Clear();
            }
        }

        /// <summary>
        /// Sets the volume of the endpoint (master volume).
        /// </summary>
        /// <param name="volume">The desired volume.</param>
        /// <param name="isMuted">The mute state of the endpoint.</param>
        public void SetEndpointVolume(int volume, bool isMuted)
        {
            _endpointVolumeWrapper.Volume = volume;
            _endpointVolumeWrapper.IsMuted = isMuted;
        }

        /// <summary>
        /// Sets the volume of an audio session.
        /// </summary>
        /// <param name="pid">The process Id of the target session.</param>
        /// <param name="volume">The desired volume.</param>
        public void SetSessionVolume(int pid, int volume, bool isMuted)
        {
            if (!_wrappers.ContainsKey(pid))
                return;

            _wrappers[pid].Volume = volume;
            _wrappers[pid].IsMuted = isMuted;
        }
        #endregion

        #region Private Methods
        private void InitializeWrappers()
        {
            AudioEndpointVolume endpointVolume;
            GetDefaultEndpointObjects(DataFlow.Render, out _sessionManager, out endpointVolume);

            InitializeEndpointWrapper(endpointVolume);
            InitializeSessionWrappers(_sessionManager);            
        }

        private void InitializeEndpointWrapper(AudioEndpointVolume endpointVolume)
        {
            _endpointVolumeWrapper = new AudioEndpointVolumeWrapper(endpointVolume);
            _endpointVolumeWrapper.VolumeChanged += OnEndpointVolumeChanged;
            RaiseEndpointCreated(_endpointVolumeWrapper.DisplayName, _endpointVolumeWrapper.Volume, _endpointVolumeWrapper.IsMuted);
        }

        private void InitializeSessionWrappers(AudioSessionManager2 sessionManager)
        {
            sessionManager.SessionCreated += OnSessionCreated;

            using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                foreach (var session in sessionEnumerator)
                {
                    var session2 = session.QueryInterface<AudioSessionControl2>();
                    if (session2.IsSystemSoundSession)
                        continue;

                    var wrapper = RegisterSession(session);
                    RaiseSessionCreated(wrapper.ProcessID, wrapper.DisplayName, wrapper.Volume, wrapper.IsMuted);
                }
        }

        private AudioSessionWrapper RegisterSession(AudioSessionControl session)
        {
            var wrapper = new AudioSessionWrapper(session);
            _wrappers[wrapper.ProcessID] = wrapper;

            wrapper.SimpleVolumeChanged += OnSimpleVolumeChanged;
            wrapper.SessionDisconnected += OnSessionRemoved;

            return wrapper;
        }

        private void UnregisterSession(AudioSessionWrapper wrapper)
        {
            _wrappers.Remove(wrapper.ProcessID);

            wrapper.SimpleVolumeChanged -= OnSimpleVolumeChanged;
            wrapper.SessionDisconnected -= OnSessionRemoved;
            wrapper.Dispose();
        }

        private void GetDefaultEndpointObjects(DataFlow dataFlow, out AudioSessionManager2 sessionManager, out AudioEndpointVolume endpointVolume)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    sessionManager = AudioSessionManager2.FromMMDevice(device);
                    endpointVolume = AudioEndpointVolume.FromDevice(device);
                }
            }
        }
        #endregion

        #region Event Handlers
        private void OnEndpointVolumeChanged(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            var wrapper = (AudioEndpointVolumeWrapper)sender;
            RaiseEndpointVolumeChanged(wrapper.Volume, wrapper.IsMuted);
        }

        private void OnSessionCreated(object sender, SessionCreatedEventArgs e)
        {
            var session = e.NewSession;
            var wrapper = RegisterSession(session);
            RaiseSessionCreated(wrapper.ProcessID, wrapper.DisplayName, wrapper.Volume, wrapper.IsMuted);
        }

        private void OnSessionRemoved(object sender, AudioSessionDisconnectedEventArgs e)
        {
            var wrapper = (AudioSessionWrapper)sender;
            RaiseSessionRemoved(wrapper.ProcessID);
            UnregisterSession(wrapper);
        }

        private void OnSimpleVolumeChanged(object sender, AudioSessionSimpleVolumeChangedEventArgs e)
        {
            var wrapper = (AudioSessionWrapper)sender;
            RaiseSessionVolumeChanged(wrapper.ProcessID, wrapper.Volume, wrapper.IsMuted);
        }
        #endregion

        #region Event Dispatchers
        private void RaiseEndpointCreated(string displayName, int volume, bool isMuted)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => EndpointCreated?.Invoke(this, displayName, volume, isMuted), null);
            else
                EndpointCreated.Invoke(this, displayName, volume, isMuted);
        }

        private void RaiseEndpointVolumeChanged(int volume, bool isMuted)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => EndpointVolumeChanged?.Invoke(this, volume, isMuted), null);
            else
                EndpointVolumeChanged?.Invoke(this, volume, isMuted);
        }

        private void RaiseSessionCreated(int pid, string displayName, int volume, bool isMuted)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => SessionCreated?.Invoke(this, pid, displayName, volume, isMuted), null);
            else
                SessionCreated.Invoke(this, pid, displayName, volume, isMuted);
        }

        private void RaiseSessionRemoved(int pid)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => SessionRemoved?.Invoke(this, pid), null);
            else
                SessionRemoved?.Invoke(this, pid);
        }

        private void RaiseSessionVolumeChanged(int pid, int volume, bool isMuted)
        {
            if (SynchronizationContext.Current != _synchronizationContext)
                _synchronizationContext.Post(o => SessionVolumeChanged?.Invoke(this, pid, volume, isMuted), null);
            else
                SessionVolumeChanged?.Invoke(this, pid, volume, isMuted);
        }
        #endregion
    }

}
