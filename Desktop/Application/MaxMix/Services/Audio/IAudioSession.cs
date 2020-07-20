using System;

namespace MaxMix.Services.Audio
{
    public interface IAudioSession : IDisposable
    {
        #region Properties
        /// <summary>
        /// The computed Identifier for this session.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// The display name of the process that created this session.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Is this session for the system sound.
        /// </summary>
        bool IsSystemSound { get; }

        /// <summary>
        /// Current volume of this session (0-100).
        /// </summary>
        int Volume { get; set; }

        /// <summary>
        /// Current mute state of this session.
        /// </summary>
        bool IsMuted { get; set; }
        #endregion

        #region Events
        /// <summary>
        /// Raised when the volume for an active session has changed.
        /// </summary>
        event Action<IAudioSession> VolumeChanged;

        /// <summary>
        /// Raised when a previously active audio session has been removed.
        /// </summary>
        event Action<IAudioSession> SessionEnded;
        #endregion
    }
}
