using CSCore.CoreAudioAPI;
using System;

namespace MaxMix.Services.Audio
{
    public interface IAudioSession : IDisposable, ISession
    {
        #region Properties
        /// <summary>
        /// The computed Identifier for this session.
        /// </summary>
        new int Id { get; }

        /// <summary>
        /// The Session Identifier string as provided by CoreAudio.
        /// </summary>
        string SessionIdentifier { get; }

        /// <summary>
        /// The display name of the process that created this session.
        /// </summary>
        new string DisplayName { get; }

        /// <summary>
        /// IsDefault always returns false for IAudioSession
        /// </summary>
        new bool IsDefault { get; }

        /// <summary>
        /// Current volume of this session (0-100).
        /// </summary>
        new int Volume { get; set; }

        /// <summary>
        /// Current mute state of this session.
        /// </summary>
        new bool IsMuted { get; set; }
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
