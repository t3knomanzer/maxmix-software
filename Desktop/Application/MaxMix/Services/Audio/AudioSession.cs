using CSCore.CoreAudioAPI;
using System;
using System.Diagnostics;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over the AudioSessionControl
    /// CSCore class.
    /// </summary>
    public class AudioSession : IAudioSession
    {
        #region Constructor
        public AudioSession(AudioSessionControl session)
        {
            _session = session;
            _session.RegisterAudioSessionNotification(_events);

            _session2 = _session.QueryInterface<AudioSessionControl2>();
            _simpleAudio = _session.QueryInterface<SimpleAudioVolume>();

            _events.StateChanged += OnStateChanged;
            _events.SimpleVolumeChanged += OnVolumeChanged;

            UpdateDisplayName();
            if (IsSystemSound)
                ID = DisplayName.GetHashCode();
            else
                ID = _session2.ProcessID;
        }
        #endregion

        #region Events
        /// <inheritdoc/>
        public event Action<IAudioSession> VolumeChanged;

        /// <inheritdoc/>
        public event Action<IAudioSession> SessionEnded;
        #endregion

        #region Fields
        private AudioSessionEvents _events = new AudioSessionEvents();
        private AudioSessionControl _session;
        private AudioSessionControl2 _session2;
        private SimpleAudioVolume _simpleAudio;
        private bool _isNotifyEnabled = true;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public int ID { get; protected set; }

        /// <inheritdoc/>
        public string DisplayName { get; protected set; }

        /// <inheritdoc/>
        public bool IsSystemSound => _session2.IsSystemSoundSession || _session2.ProcessID == 0;

        /// <summary>
        /// The ProcessID that created the audio session.
        /// </summary>
        public int ProcessID => _session2.ProcessID;

        /// <summary>
        /// The process that created the audio session.
        /// </summary>
        public Process Process => _session2.Process;

        /// <inheritdoc/>
        public int Volume
        {
            get => (int)(_simpleAudio.MasterVolume * 100);
            set
            {
                if (Volume == value)
                    return;

                _isNotifyEnabled = false;
                _simpleAudio.MasterVolume = value / 100f;
                _isNotifyEnabled = true;
            }
        }

        /// <inheritdoc/>
        public bool IsMuted
        {
            get => _simpleAudio.IsMuted;
            set
            {
                if (IsMuted == value)
                    return;

                _isNotifyEnabled = false;
                _simpleAudio.IsMuted = value;
                _isNotifyEnabled = true;
            }
        }
        #endregion

        #region Private Methods
        private void UpdateDisplayName()
        {
            var displayName = _session2.DisplayName;
            if (IsSystemSound) { displayName = "System Sounds"; }
            if (string.IsNullOrEmpty(displayName)) { displayName = _session2.Process.MainWindowTitle; }
            if (string.IsNullOrEmpty(displayName)) { displayName = _session2.Process.ProcessName; }
            if (string.IsNullOrEmpty(displayName)) { displayName = "Unnamed"; }
            DisplayName = displayName;
        }
        #endregion

        #region Event Handlers
        private void OnVolumeChanged(object sender, AudioSessionSimpleVolumeChangedEventArgs e)
        {
            if (!_isNotifyEnabled)
                return;

            VolumeChanged?.Invoke(this);
        }

        private void OnStateChanged(object sender, AudioSessionStateChangedEventArgs e)
        {
            if (e.NewState != AudioSessionState.AudioSessionStateExpired)
                return;

            SessionEnded?.Invoke(this);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            _events.StateChanged -= OnStateChanged;
            _events.SimpleVolumeChanged -= OnVolumeChanged;

            _session?.UnregisterAudioSessionNotification(_events);
            _session?.Dispose();
            _session = null;

            _session2?.Dispose();
            _session2 = null;

            _simpleAudio?.Dispose();
            _simpleAudio = null;
        }
        #endregion
    }
}
