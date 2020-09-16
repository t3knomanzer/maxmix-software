using CSCore.CoreAudioAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over the MMDevice CSCore class.
    /// </summary>
    public class AudioDevice : IAudioDevice
    {
        #region Constructor
        public AudioDevice(MMDevice device)
        {
            Device = device;

            _deviceEnumerator = new MMDeviceEnumerator();
            _deviceEnumerator.DefaultDeviceChanged += OnDefaultDeviceChanged;
            _deviceEnumerator.DeviceRemoved += OnDeviceRemoved;
            _deviceEnumerator.DeviceStateChanged += OnDeviceStateChanged;

            _endpointVolume = AudioEndpointVolume.FromDevice(Device);
            _endpointVolume.RegisterControlChangeNotify(_callback);
            _callback.NotifyRecived += OnEndpointVolumeChanged;

            UpdateDisplayName();
            Flow = Device.DataFlow == DataFlow.Capture ? DeviceFlow.Input : DeviceFlow.Output;
        }
        #endregion

        #region Events
        /// <inheritdoc/>
        public event Action<IAudioDevice> DeviceDefaultChanged;

        /// <inheritdoc/>
        public event Action<IAudioDevice> DeviceRemoved;

        /// <inheritdoc/>
        public event Action<IAudioDevice> DeviceVolumeChanged;

        #endregion

        #region Fields
        private MMDeviceEnumerator _deviceEnumerator;
        private AudioEndpointVolume _endpointVolume;
        private AudioEndpointVolumeCallback _callback = new AudioEndpointVolumeCallback();

        private bool _isNotifyEnabled = true;

        private bool _isDefault;
        private bool _wasDefault;
        private int _volume;
        private bool _isMuted;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public MMDevice Device { get; private set; }

        /// <inheritdoc/>
        public int Id => Device.DeviceID.GetHashCode();

        /// <inheritdoc/>
        public string DisplayName { get; protected set; }

        /// <inheritdoc/>
        public DeviceFlow Flow { get; protected set; }

        /// <inheritdoc/>
        public bool IsDefault
        {
            get
            {
                try
                {
                    var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    _isDefault = defaultDevice.DeviceID.GetHashCode() == Device.DeviceID.GetHashCode();
                }
                catch { }

                return _isDefault;
            }
        }

        /// <inheritdoc/>
        public int Volume
        {
            get
            {
                try { _volume = (int)Math.Round(_endpointVolume.MasterVolumeLevelScalar * 100); }
                catch { }

                return _volume;
            }
            set
            {
                if (_volume == value)
                    return;

                _isNotifyEnabled = false;
                _volume = value;
                try { _endpointVolume.MasterVolumeLevelScalar = value / 100f; }
                catch { }
            }
        }

        /// <inheritdoc/>
        public bool IsMuted
        {
            get
            {
                try { _isMuted = _endpointVolume.IsMuted; }
                catch { }

                return _isMuted;
            }
            set
            {
                if (_isMuted == value)
                    return;

                _isNotifyEnabled = false;
                _isMuted = value;
                try { _endpointVolume.IsMuted = value; }
                catch { }
            }
        }
        #endregion

        #region Private Methods
        private void UpdateDisplayName()
        {
            var displayName = Device.FriendlyName;
            var match = Regex.Match(displayName, @"\(+.*\)+");
            if (match.Success)
            {
                displayName = match.Value;
                displayName = displayName.Substring(1, displayName.Length - 2);
            }

            DisplayName = displayName;
        }
        #endregion

        #region Event Handlers
        private void OnDefaultDeviceChanged(object sender, DefaultDeviceChangedEventArgs e)
        {
            // For some reason this event triggers twice.
            // We keep track of the previous state, and only raise the event if
            // we are now the default device and were not before.
            if (e.DeviceId.GetHashCode() == Device.DeviceID.GetHashCode())
                if (!_wasDefault)
                {
                    _wasDefault = true;
                    DeviceDefaultChanged?.Invoke(this);
                }
            else
                _wasDefault = false;
        }

        private void OnDeviceStateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            if (e.DeviceState == DeviceState.NotPresent || e.DeviceState == DeviceState.UnPlugged)
            {
                e.TryGetDevice(out var device);
                if (device.DeviceID.GetHashCode() == Device.DeviceID.GetHashCode())
                    DeviceRemoved?.Invoke(this);
            }
        }

        private void OnDeviceRemoved(object sender, DeviceNotificationEventArgs e)
        {
            e.TryGetDevice(out var device);
            if (device.DeviceID.GetHashCode() == Device.DeviceID.GetHashCode())              
                DeviceRemoved?.Invoke(this);
        }
        private void OnEndpointVolumeChanged(object sender, AudioEndpointVolumeCallbackEventArgs e)
        {
            if (!_isNotifyEnabled)
            {
                _isNotifyEnabled = true;
                return;
            }

            DeviceVolumeChanged?.Invoke(this);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_deviceEnumerator != null)
            {
                _deviceEnumerator.DefaultDeviceChanged -= OnDefaultDeviceChanged;
                _deviceEnumerator.DeviceRemoved -= OnDeviceRemoved;
                _deviceEnumerator.DeviceStateChanged -= OnDeviceStateChanged;
            }

            if(_callback != null)
                _callback.NotifyRecived -= OnEndpointVolumeChanged;

            if(_endpointVolume != null)
                _endpointVolume.UnregisterControlChangeNotify(_callback);

            _deviceEnumerator = null;
            _endpointVolume = null;
            _callback = null;

            Device = null;
        }
        #endregion
    }
}
