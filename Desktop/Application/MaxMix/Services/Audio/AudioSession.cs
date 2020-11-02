using CSCore.CoreAudioAPI;
using MaxMix.Framework;
using System;
using System.IO;

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
            Session = session;
            Session.RegisterAudioSessionNotification(_events);

            _session2 = Session.QueryInterface<AudioSessionControl2>();
            _simpleAudio = Session.QueryInterface<SimpleAudioVolume>();

            _events.StateChanged += OnStateChanged;
            _events.SimpleVolumeChanged += OnVolumeChanged;

            SessionIdentifier = _session2.SessionInstanceIdentifier;

            IsSystemSound = _session2.IsSystemSoundSession || _session2.ProcessID == 0 || _session2.Process == null;
            string appId = SessionIdentifier.ExtractAppId();
            Id = IsSystemSound ? int.MinValue : appId.GetHashCode();

            UpdateDisplayName();
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
        private AudioSessionControl2 _session2;
        private SimpleAudioVolume _simpleAudio;
        private bool _isNotifyEnabled = true;

        private int _volume;
        private bool _isMuted;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public AudioSessionControl Session { get; private set; }

        /// <inheritdoc/>
        public int Id { get; protected set; }

        /// <inheritdoc/>
        public string SessionIdentifier { get; protected set; }

        /// <inheritdoc/>
        public string DisplayName { get; protected set; }

        public bool IsDefault => IsSystemSound;

        /// <inheritdoc/>
        public bool IsSystemSound { get; protected set; }

        /// <inheritdoc/>
        public int Volume
        {
            get
            {
                try { _volume = (int)Math.Round(_simpleAudio.MasterVolume * 100); }
                catch (Exception e) { AppLogging.DebugLogException(nameof(Volume), e); }

                return _volume;
            }
            set
            {
                if (_volume == value)
                    return;

                _isNotifyEnabled = false;
                _volume = value;
                try { _simpleAudio.MasterVolume = value / 100f; }
                catch (Exception e) { AppLogging.DebugLogException(nameof(Volume), e); }
            }
        }

        /// <inheritdoc/>
        public bool IsMuted
        {
            get
            {
                try { _isMuted = _simpleAudio.IsMuted; }
                catch { }

                return _isMuted;
            }
            set
            {
                if (_isMuted == value)
                    return;

                _isNotifyEnabled = false;
                _isMuted = value;
                try { _simpleAudio.IsMuted = value; }
                catch { }
            }
        }
        #endregion

        #region Private Methods
        private void UpdateDisplayName()
        {
            var displayName = _session2.DisplayName;
            if (IsSystemSound) {
                displayName = "System Sounds";
            }
            else
            {
                if (_session2.Process != null)
                {
                    if (string.IsNullOrEmpty(displayName)) { try { displayName = _session2.Process.GetProductName(); } catch { } }
                    if (string.IsNullOrEmpty(displayName)) { try { displayName = _session2.Process.MainWindowTitle; } catch { } }
                    if (string.IsNullOrEmpty(displayName)) { try { displayName = _session2.Process.ProcessName; } catch { } }
                    if (string.IsNullOrEmpty(displayName)) { try { displayName = _session2.Process.GetMainModuleFileName(); } catch { } }
                }
                if (string.IsNullOrEmpty(displayName)) { displayName = Path.GetFileNameWithoutExtension(_session2.SessionIdentifier.ExtractAppPath()); }
                if (string.IsNullOrEmpty(displayName)) { displayName = "Unnamed"; }
                displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
            }
            DisplayName = displayName;
        }
        #endregion

        #region Event Handlers
        private void OnVolumeChanged(object sender, AudioSessionSimpleVolumeChangedEventArgs e)
        {
            if (!_isNotifyEnabled)
            {
                _isNotifyEnabled = true;
                return;
            }

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
            try
            {
                // Do unregistration, can throw
                if (_events != null)
                {
                    _events.StateChanged -= OnStateChanged;
                    _events.SimpleVolumeChanged -= OnVolumeChanged;
                    Session.UnregisterAudioSessionNotification(_events);
                }
            }
            catch { }

            // Do disposal chains, each can throw
            try { _session2.Dispose(); }
            catch { }
            try { Session.Dispose(); }
            catch { }

            // Set to null
            _simpleAudio = null;
            _session2 = null;
            Session = null;
        }
        #endregion
    }
}
