using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over the AudioSessionControl
    /// CSCore class.
    /// </summary>
    public class AudioSessionWrapper : IDisposable
    {
        #region Constructor
        public AudioSessionWrapper(AudioSessionControl session)
        {
            _events = new AudioSessionEvents();
            _events.StateChanged += OnStateChanged;
            _events.SimpleVolumeChanged += OnSimpleVolumeChanged;

            _session = session;
            _session.RegisterAudioSessionNotification(_events);

            _session2 = _session.QueryInterface<AudioSessionControl2>();
            _simpleAudio = _session.QueryInterface<SimpleAudioVolume>();

            _isNotifyEnabled = true;
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised when the volume of this session has changed.
        /// </summary>
        public event EventHandler<AudioSessionSimpleVolumeChangedEventArgs> SimpleVolumeChanged;

        /// <summary>
        /// Raised when this session has been disconnected and can be destroyed.
        /// </summary>
        public event EventHandler<AudioSessionDisconnectedEventArgs> SessionDisconnected;
        #endregion

        #region Fields
        private AudioSessionEvents _events;
        private AudioSessionControl _session;
        private AudioSessionControl2 _session2;
        private SimpleAudioVolume _simpleAudio;
        private bool _isNotifyEnabled;
        #endregion

        #region Properties
        /// <summary>
        /// The process ID of this session.
        /// </summary>
        public int ProcessID => _session2.ProcessID;

        /// <summary>
        /// The path to the icon used for this session.
        /// </summary>
        public string IconPath => _session2.IconPath;

        /// <summary>
        /// ???
        /// </summary>
        public bool IsSingleProcessSessionession => _session2.IsSingleProcessSession;

        /// <summary>
        /// Indicates if this is the session responsible for windows system sounds.
        /// </summary>
        public bool IsSystemSoundSession => _session2.IsSystemSoundSession;

        /// <summary>
        /// A reference to the process that created this session.
        /// </summary>
        public Process Process => _session2.Process;

        /// <summary>
        /// The display name of the process that created this session.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var displayName = _session2.DisplayName;
                if (string.IsNullOrEmpty(displayName)) { displayName = _session2.Process.ProcessName; }
                if (string.IsNullOrEmpty(displayName)) { displayName = _session2.Process.MainWindowTitle; }
                if (string.IsNullOrEmpty(displayName)) { displayName = "Unnamed"; }

                // Capitalize first letter
                displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);

                return displayName;
            }
        }

        /// <summary>
        /// Current volume of this session (0-100).
        /// </summary>
        public int Volume
        {
            get => (int)(_simpleAudio.MasterVolume * 100);
            set
            {
                _isNotifyEnabled = false;
                _simpleAudio.MasterVolume = value / 100f;
            }
        }

        /// <summary>
        /// Current mute state of this session.
        /// </summary>
        public bool IsMuted
        {
            get => _simpleAudio.IsMuted;
            set
            {
                _isNotifyEnabled = false;
                _simpleAudio.IsMuted = value;
            }
        }
        #endregion                  

        #region Event Handlers
        private void OnSimpleVolumeChanged(object sender, AudioSessionSimpleVolumeChangedEventArgs e)
        {
            if (!_isNotifyEnabled)
            {
                _isNotifyEnabled = true;
                return;
            }

            SimpleVolumeChanged?.Invoke(this, e);
        }

        private void OnStateChanged(object sender, AudioSessionStateChangedEventArgs e)
        {
            if (e.NewState == AudioSessionState.AudioSessionStateExpired)
            {
                SessionDisconnected?.Invoke(this, new AudioSessionDisconnectedEventArgs(AudioSessionDisconnectReason.DisconnectReasonSessionDisconnected));
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_events != null)
            {
                _events.StateChanged -= OnStateChanged;
                _events.SimpleVolumeChanged -= OnSimpleVolumeChanged;
                _session.UnregisterAudioSessionNotification(_events);
            }

            _session = null;
            _session2 = null;
            _simpleAudio = null;
            _events = null;
        }
        #endregion
    }
}
