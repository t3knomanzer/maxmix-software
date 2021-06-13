using MaxMix.Framework;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Text.RegularExpressions;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over the MMDevice CSCore class.
    /// </summary>
    public class AudioDevice : IAudioDevice, IMMNotificationClient
    {
        private readonly NLog.Logger m_Logger = NLog.LogManager.GetCurrentClassLogger();

        #region Constructor
        public AudioDevice(MMDevice device)
        {
            Device = device;

            _deviceEnumerator = new MMDeviceEnumerator();
            _deviceEnumerator.RegisterEndpointNotificationCallback(this);

            Device.AudioEndpointVolume.OnVolumeNotification += OnEndpointVolumeChanged;

            UpdateDisplayName();
            Flow = Device.DataFlow.ToDeviceFlow();
            DeviceId = Device.ID;
            Id = DeviceId.GetHashCode();

            // This is kinda silly...
            var defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(Device.DataFlow, Role.Console); // Multimedia? Communications?
            IsDefault = defaultDevice.ID == DeviceId;
            defaultDevice.Dispose();
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

        private bool _isNotifyEnabled = true;
        private int _volume;
        private bool _isMuted;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public MMDevice Device { get; private set; }

        /// <inheritdoc/>
        public int Id { get; protected set; }

        public string DeviceId { get; protected set; }

        /// <inheritdoc/>
        public string DisplayName { get; protected set; }

        /// <inheritdoc/>
        public DeviceFlow Flow { get; protected set; }

        /// <inheritdoc/>
        public bool IsDefault { get; protected set; }

        /// <inheritdoc/>
        public int Volume
        {
            get
            {
                try { _volume = (int)Math.Round(Device.AudioEndpointVolume.MasterVolumeLevelScalar * 100); }
                catch { }

                return _volume;
            }
            set
            {
                if (_volume == value)
                {
                    return;
                }

                _isNotifyEnabled = false;
                _volume = value;
                try { Device.AudioEndpointVolume.MasterVolumeLevelScalar = value / 100f; }
                catch { }
            }
        }

        /// <inheritdoc/>
        public bool IsMuted
        {
            get
            {
                try { _isMuted = Device.AudioEndpointVolume.Mute; }
                catch { }

                return _isMuted;
            }
            set
            {
                if (_isMuted == value)
                {
                    return;
                }

                _isNotifyEnabled = false;
                _isMuted = value;
                try { Device.AudioEndpointVolume.Mute = value; }
                catch { }
            }
        }
        #endregion

        #region Private Methods
        private void UpdateDisplayName()
        {
            var displayName = "Unnamed";
            try { displayName = Device.FriendlyName; } catch { }
            if (string.IsNullOrEmpty(displayName)) { displayName = "Unnamed"; }
            var match = Regex.Match(displayName, @"\(+.*\)+");
            if (match.Success)
            {
                displayName = match.Value;
                displayName = displayName.Substring(1, displayName.Length - 2);
            }

            DisplayName = displayName;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            try
            {
                Device.AudioEndpointVolume.OnVolumeNotification -= OnEndpointVolumeChanged;
                _deviceEnumerator.UnregisterEndpointNotificationCallback(this);
            }
            catch { }

            // Do disposal chains, each can throw
            try { _deviceEnumerator.Dispose(); }
            catch { }
            try { Device.Dispose(); }
            catch { }

            // Set to null
            _deviceEnumerator = null;
            Device = null;
        }
        #endregion

        private void OnEndpointVolumeChanged(AudioVolumeNotificationData data)
        {
            if (!_isNotifyEnabled)
            {
                _isNotifyEnabled = true;
                return;
            }

            DeviceVolumeChanged?.Invoke(this);
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            if (deviceId != DeviceId || newState.HasFlag(DeviceState.Active))
                return;

            DeviceRemoved?.Invoke(this);
        }

        public void OnDeviceAdded(string pwstrDeviceId)
        {
            // NOOP
        }

        public void OnDeviceRemoved(string deviceId)
        {
            if (deviceId != DeviceId)
                return;

            DeviceRemoved?.Invoke(this);
        }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow != Flow.ToDataFlow())
                return;

            bool newDefault = defaultDeviceId == DeviceId;
            if (IsDefault != newDefault)
            {
                m_Logger.Debug(string.Join("\t", nameof(OnDefaultDeviceChanged), DeviceId, newDefault));
                IsDefault = newDefault;
                DeviceDefaultChanged?.Invoke(this);
            }
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
        {
            // NOOP
        }
    }
}
