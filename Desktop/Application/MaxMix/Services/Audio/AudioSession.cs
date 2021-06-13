using MaxMix.Framework;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.IO;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over the AudioSessionControl
    /// CSCore class.
    /// </summary>
    public class AudioSession : IAudioSession, IAudioSessionEventsHandler
    {
        private readonly NLog.Logger m_Logger = NLog.LogManager.GetCurrentClassLogger();

        #region Constructor
        public AudioSession(AudioSessionControl session)
        {
            Session = session;
            Session.RegisterEventClient(this);

            SessionIdentifier = Session.GetSessionIdentifier;

            IsSystemSound = Session.IsSystemSoundsSession || Session.GetProcessID == 0;
            string appId = SessionIdentifier.ExtractAppPath();
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
                try { _volume = (int)Math.Round(Session.SimpleAudioVolume.Volume * 100); }
                catch (Exception e) { m_Logger.Debug(e, nameof(Volume)); }

                return _volume;
            }
            set
            {
                if (_volume == value)
                    return;

                _isNotifyEnabled = false;
                _volume = value;
                try { Session.SimpleAudioVolume.Volume = value / 100f; }
                catch (Exception e) { m_Logger.Debug(e, nameof(Volume)); }
            }
        }

        /// <inheritdoc/>
        public bool IsMuted
        {
            get
            {
                try { _isMuted = Session.SimpleAudioVolume.Mute; }
                catch { }

                return _isMuted;
            }
            set
            {
                if (_isMuted == value)
                    return;

                _isNotifyEnabled = false;
                _isMuted = value;
                try { Session.SimpleAudioVolume.Mute = value; }
                catch { }
            }
        }
        #endregion

        #region Private Methods
        private void UpdateDisplayName()
        {
            var displayName = Session.DisplayName;
            if (IsSystemSound)
            {
                displayName = "System Sounds";
            }
            else
            {
                if (Session.GetProcessID != 0)
                {
                    try
                    {
                        var process = System.Diagnostics.Process.GetProcessById((int)Session.GetProcessID);
                        if (string.IsNullOrEmpty(displayName)) { try { displayName = process.GetProductName(); } catch { } }
                        if (string.IsNullOrEmpty(displayName)) { try { displayName = process.MainWindowTitle; } catch { } }
                        if (string.IsNullOrEmpty(displayName)) { try { displayName = process.ProcessName; } catch { } }
                        if (string.IsNullOrEmpty(displayName)) { try { displayName = process.GetMainModuleFileName(); } catch { } }
                    }
                    catch { }
                }
                if (string.IsNullOrEmpty(displayName)) { displayName = Path.GetFileNameWithoutExtension(Session.GetSessionIdentifier.ExtractAppPath()); }
                if (string.IsNullOrEmpty(displayName)) { displayName = "Unnamed"; }
                displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
            }
            DisplayName = displayName;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            try
            {
                Session.UnRegisterEventClient(this);
            }
            catch { }

            try { Session.Dispose(); }
            catch { }

            // Set to null
            Session = null;
        }

        void IAudioSessionEventsHandler.OnVolumeChanged(float volume, bool isMuted)
        {
            if (!_isNotifyEnabled)
            {
                _isNotifyEnabled = true;
                return;
            }

            VolumeChanged?.Invoke(this);
        }

        void IAudioSessionEventsHandler.OnDisplayNameChanged(string displayName)
        {
            // NOOP
        }

        void IAudioSessionEventsHandler.OnIconPathChanged(string iconPath)
        {
            // NOOP
        }

        void IAudioSessionEventsHandler.OnChannelVolumeChanged(uint channelCount, IntPtr newVolumes, uint channelIndex)
        {
            // NOOP
        }

        void IAudioSessionEventsHandler.OnGroupingParamChanged(ref Guid groupingId)
        {
            // NOOP
        }

        void IAudioSessionEventsHandler.OnStateChanged(AudioSessionState state)
        {
            if (state != AudioSessionState.AudioSessionStateExpired)
                return;

            SessionEnded?.Invoke(this);
        }

        void IAudioSessionEventsHandler.OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            // NOOP
        }
        #endregion
    }
}
