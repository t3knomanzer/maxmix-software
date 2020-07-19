namespace MaxMix.Services.Audio
{
    internal interface IAudioSessionService
    {
        void Start();
        void Stop();
        void SetVisibleSystemSounds(bool visibleSystemSounds);
        void SetSessionVolume(int id, int volume, bool isMuted);

        event AudioSessionCreatedDelegate SessionCreated;
        event AudioSessionRemovedDelegate SessionRemoved;
        event AudioSessionVolumeDelegate SessionVolumeChanged;
    }
}
