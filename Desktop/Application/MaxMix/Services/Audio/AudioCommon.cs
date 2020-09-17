using Sentry.Protocol;

namespace MaxMix.Services.Audio
{
    #region Enums
    public enum DeviceFlow
    {
        Input = 0,
        Output = 1
    }
    #endregion

    #region Delegates
    public delegate void DefaultAudioDeviceChangedDelegate(object sender, int id, DeviceFlow deviceFlow);
    public delegate void AudioDeviceCreatedDelegate(object sender, int id, string displayName, int volume, bool isMuted, DeviceFlow deviceFlow);
    public delegate void AudioDeviceRemovedDelegate(object sender, int id, DeviceFlow deviceFlow);
    public delegate void AudioDeviceVolumeDelegate(object sender, int id, int volume, bool isMuted, DeviceFlow deviceFlow);

    public delegate void AudioSessionCreatedDelegate(object sender, int id, string displayName, int volume, bool isMuted);
    public delegate void AudioSessionRemovedDelegate(object sender, int id);
    public delegate void AudioSessionVolumeDelegate(object sender, int id, int volume, bool isMuted);
    #endregion
}
