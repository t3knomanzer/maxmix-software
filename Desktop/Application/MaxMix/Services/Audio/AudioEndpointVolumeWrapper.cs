using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over the AudioSessionControl
    /// CSCore class.
    /// </summary>
    public class AudioEndpointVolumeWrapper : IDisposable
    {
        #region Constructor
        public AudioEndpointVolumeWrapper(AudioEndpointVolume endpointVolume)
        {
            _endpointVolume = endpointVolume;

            _callback = new AudioEndpointVolumeCallback();
            _callback.NotifyRecived += OnEndpointNotifyReceived;
            _endpointVolume.RegisterControlChangeNotify(_callback);

            _isNotifyEnabled = true;
        }
        #endregion

        #region Events
        /// <summary>
        /// Raised when the volume of this session has changed.
        /// </summary>
        public event EventHandler<AudioEndpointVolumeCallbackEventArgs> VolumeChanged;
        #endregion

        #region Fields
        private AudioEndpointVolume _endpointVolume;
        private AudioEndpointVolumeCallback _callback;
        private bool _isNotifyEnabled;
        #endregion

        #region Properties
        /// <summary>
        /// The display name of the process that created this session.
        /// </summary>
        public string DisplayName
        {
            get => "Master";
        }

        /// <summary>
        /// Current volume of this session (0-100).
        /// </summary>
        public int Volume
        {
            get => (int)(_endpointVolume.MasterVolumeLevelScalar * 100);
            set
            {
                _isNotifyEnabled = false;
                _endpointVolume.MasterVolumeLevelScalar = value / 100f;
            }
        }

        /// <summary>
        /// Current mute state of this session.
        /// </summary>
        public bool IsMuted
        {
            get => _endpointVolume.IsMuted;
            set
            {
                _isNotifyEnabled = false;
                _endpointVolume.IsMuted = value;
            }
        }
        #endregion

        #region Event Handlers
        private void OnEndpointNotifyReceived(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            if (!_isNotifyEnabled)
            {
                _isNotifyEnabled = true;
                return;
            }

            VolumeChanged?.Invoke(this, e);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_callback != null)
                _endpointVolume.UnregisterControlChangeNotify(_callback);

            _endpointVolume = null;
            _callback = null;
    }
        #endregion
    }
}
