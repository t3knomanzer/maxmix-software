namespace MaxMix.Services.Audio
{
    public delegate void DefaultAudioDeviceChangedDelegate(object sender, int id);
    public delegate void AudioDeviceCreatedDelegate(object sender, int id, string displayName, int volume, bool isMuted);
    public delegate void AudioDeviceRemovedDelegate(object sender, int id);
    public delegate void AudioDeviceVolumeDelegate(object sender, int id, int volume, bool isMuted);

    public delegate void AudioSessionCreatedDelegate(object sender, int id, string displayName, int volume, bool isMuted);
    public delegate void AudioSessionRemovedDelegate(object sender, int id);
    public delegate void AudioSessionVolumeDelegate(object sender, int id, int volume, bool isMuted);
}
