using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over multiple AudioSessionControl
    /// CSCore class.
    /// </summary>
    public class AudioSessionGroup : IDisposable
    {
        #region Constructor
        public AudioSessionGroup()
        {
            _wrappers = new ConcurrentDictionary<int, AudioSessionWrapper>();
        }
        #endregion

        #region Fields
        private IDictionary<int, AudioSessionWrapper> _wrappers;
        #endregion

        #region Properties
        /// <summary>
        /// The process ID of this session.
        /// </summary>
        private int ProcessID => _wrappers.First().Value.ProcessID;

        /// <summary>
        /// The path to the icon used for this session.
        /// </summary>
        public string IconPath => _wrappers.First().Value.IconPath;

        /// <summary>
        /// ???
        /// </summary>
        public bool IsSingleProcessSession => _wrappers.First().Value.IsSingleProcessSession;

        /// <summary>
        /// Indicates if this is the session responsible for windows system sounds.
        /// </summary>
        public bool IsSystemSoundSession => _wrappers.First().Value.IsSystemSoundSession;

        /// <summary>
        /// A reference to the process that created this session.
        /// </summary>
        public Process Process => _wrappers.First().Value.Process;

        public int AppID { get; protected set; }

        /// <summary>
        /// The display name of the process that created this session.
        /// </summary>
        public string DisplayName => _wrappers.First().Value.DisplayName;

        /// <summary>
        /// Current volume of this session (0-100).
        /// </summary>
        public int Volume
        {
            get => _wrappers.First().Value.Volume;
            set
            {
                foreach (var wrapper in _wrappers)
                    wrapper.Value.Volume = value;
            }
        }

        /// <summary>
        /// Current mute state of this session.
        /// </summary>
        public bool IsMuted
        {
            get => _wrappers.First().Value.IsMuted;
            set
            {
                foreach (var wrapper in _wrappers)
                    wrapper.Value.IsMuted = value;
            }
        }
        #endregion

        #region Public Methods
        public void AddWrapper(AudioSessionWrapper wrapper)
        {
            _wrappers.Add(wrapper.ProcessID, wrapper);
        }

        public bool RemoveWrapper(AudioSessionWrapper wrapper)
        {
            _wrappers.Remove(wrapper.ProcessID);
            return _wrappers.Count > 0;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_wrappers != null)
            {
                foreach (var wrapper in _wrappers.Values)
                    wrapper.Dispose();

                _wrappers.Clear();
            }
        }
        #endregion
    }
}
