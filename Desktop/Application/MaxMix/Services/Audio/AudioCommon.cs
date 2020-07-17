using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Audio
{
    public delegate void AudioEndpointDelegate(object sender, string displayName, int volume, bool isMuted);
    public delegate void AudioEndpointVolumeDelegate(object sender, int volume, bool isMuted);
    public delegate void AudioSessionDelegate(object sender, int appID, string displayName, int volume, bool isMuted);
    public delegate void AudioSessionNameDelegate(object sender, int appID, string displayName);
    public delegate void AudioSessionVolumeDelegate(object sender, int appID, int volume, bool isMuted);
}
