using MaxMix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Audio
{
    internal interface IAudioSessionService
    {
        void Start();
        void Stop();
        void SetVolume(int pid, int volume);
        void SetMute(int pid, bool isMuted);

        event AudioSessionDelegate SessionCreated;
        event EventHandler<int> SessionRemoved;
        event AudioSessionVolumeDelegate SessionVolumeChanged;
    }
}
