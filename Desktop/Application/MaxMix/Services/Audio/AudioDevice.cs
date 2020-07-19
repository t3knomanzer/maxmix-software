using CSCore.CoreAudioAPI;
using System;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over multiple AudioSessions.
    /// </summary>
    public class AudioDevice : IAudioSession
    {
        #region Constructor
        public AudioDevice(MMDevice device)
        {
            _device = device;

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

            VolumeChanged?.Invoke(this);
        }

        private void OnSessionCreated(object sender, SessionCreatedEventArgs e)
        {
            SessionCreated?.Invoke(new AudioSession(e.NewSession));
        }

        // TODO: Register default device change as SessionEnded
        #endregion

        #region Public Methods
        public void InitializeSystemSessions()
        {
            using (var sessionEnumerator = _sessionManager.GetSessionEnumerator())
            {
                foreach (var session in sessionEnumerator)
                {
                    var audioSession = new AudioSession(session);
                    if (audioSession.IsSystemSound)
                        SessionCreated?.Invoke(new AudioSession(session));
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
                        SessionCreated?.Invoke(new AudioSession(session));
                    else
                        audioSession.Dispose();
                }
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            _callback.NotifyRecived -= OnVolumeChanged;

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
