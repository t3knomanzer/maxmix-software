using MaxMix.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over multiple AudioSessions.
    /// </summary>
    public class AudioSessionGroup : IAudioSession
    {
        #region Constructor
        public AudioSessionGroup(int id, string displayName, bool isDefault)
        {
            Id = id;
            DisplayName = displayName;
            SessionIdentifier = $"{Id}: {DisplayName}";
            IsDefault = isDefault;
        }
        #endregion

        #region Events
        /// <inheritdoc/>
        public event Action<IAudioSession> VolumeChanged;

        /// <inheritdoc/>
        public event Action<IAudioSession> SessionEnded;
        #endregion

        #region Fields
        private readonly IDictionary<string, IAudioSession> _sessions = new ConcurrentDictionary<string, IAudioSession>();
        private int _volume = 100;
        private bool _isMuted = false;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public int Id { get; protected set; }

        /// <inheritdoc/>
        public string SessionIdentifier { get; protected set; }

        /// <inheritdoc/>
        public string DisplayName { get; protected set; }

        /// <inheritdoc/>
        public bool IsDefault { get; protected set; }

        /// <inheritdoc/>
        public int Volume
        {
            get => _volume;
            set => SetVolume(value);
        }

        /// <inheritdoc/>
        public bool IsMuted
        {
            get => _isMuted;
            set => SetIsMuted(value);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public void AddSession(IAudioSession session)
        {
            _sessions.Add(session.SessionIdentifier, session);
            session.VolumeChanged += OnVolumeChanged;
            session.SessionEnded += OnSessionEnded;

            if (_sessions.Count == 1)
            {
                _volume = session.Volume;
                _isMuted = session.IsMuted;
            }
        }

        public bool ContainsSession(IAudioSession session)
        {
            return _sessions.ContainsKey(session.SessionIdentifier);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        private void SetVolume(int value)
        {
            if (_volume == value)
                return;

            _volume = value;
            foreach (var session in _sessions.Values)
                session.Volume = value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        private void SetIsMuted(bool value)
        {
            if (_isMuted == value)
                return;

            _isMuted = value;
            foreach (var session in _sessions.Values)
                session.IsMuted = value;
        }
        #endregion

        #region Event Handlers
        private void OnVolumeChanged(IAudioSession session)
        {
            _volume = session.Volume;
            _isMuted = session.IsMuted;

            VolumeChanged?.Invoke(this);
        }

        private void OnSessionEnded(IAudioSession session)
        {
            AppLogging.DebugLog(nameof(OnSessionEnded), session.SessionIdentifier, session.DisplayName, session.Id.ToString());
            _sessions.Remove(session.SessionIdentifier);
            session.Dispose();

            if (_sessions.Count > 0)
                return;

            SessionEnded?.Invoke(this);
        }
        #endregion

        #region IDisposable
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            foreach (var session in _sessions.Values)
            {
                session.VolumeChanged -= OnVolumeChanged;
                session.SessionEnded -= OnSessionEnded;
                session.Dispose();
            }

            _sessions.Clear();
        }
        #endregion
    }
}
