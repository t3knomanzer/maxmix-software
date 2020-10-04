using CSCore.Codecs.FLAC;
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
        /// The Device Identifier string as provided by CoreAudio.
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// The computed Identifier for this session.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// 
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// The display name of the process that created this session.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The direction of the audio flow for this device, input for capture devices and output for render devices.
        /// </summary>
        DeviceFlow Flow { get; }

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