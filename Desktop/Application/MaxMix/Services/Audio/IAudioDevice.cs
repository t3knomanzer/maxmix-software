using CSCore.CoreAudioAPI;
using System;

namespace MaxMix.Services.Audio
{
    public interface IAudioDevice : IDisposable
    {
        #region Properties
        /// <summary>
        /// 
        /// </summary>
        MMDevice Device { get; }

        /// <summary>
        /// The computed Identifier for this session.
        /// </summary>
        int ID { get; }

        /// <summary>
        /// 
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// The display name of the process that created this session.
        /// </summary>
        string DisplayName { get; }

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
        /// 
        /// </summary>
        event Action<IAudioDevice> DeviceDefaultChanged;

        /// <summary>
        /// 
        /// </summary>
        event Action<IAudioDevice> DeviceRemoved;

        /// <summary>
        /// 
        /// </summary>
        event Action<IAudioDevice> DeviceVolumeChanged;
        #endregion
    }
}