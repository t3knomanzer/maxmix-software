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
        void SetEndpointVolume(int volume, bool isMuted);
        void SetSessionVolume(int pid, int volume, bool isMuted);

        event AudioEndpointDelegate EndpointCreated;
        event AudioEndpointVolumeDelegate EndpointVolumeChanged;
        event AudioSessionDelegate SessionCreated;
        event EventHandler<int> SessionRemoved;
        event AudioSessionVolumeDelegate SessionVolumeChanged;
    }
}
