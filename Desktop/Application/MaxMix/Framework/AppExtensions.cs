using MaxMix.Services.Audio;
using MaxMix.Services.Communication;
using MaxMix.ViewModels;
using System;

namespace MaxMix.Framework
{
    internal static class AppExtensions
    {
        public static DeviceSettings ToDeviceSettings(this SettingsViewModel model)
        {
            DeviceSettings settings = DeviceSettings.Default();
            settings.sleepAfterSeconds = (byte)(model.SleepWhenInactive ? model.SleepAfterSeconds : 0);
            settings.accelerationPercentage = (byte)model.AccelerationPercentage;
            settings.continuousScroll = model.LoopAroundItems;
            //_settingsViewModel.DoubleTapTime
            settings.volumeMinColor.SetBytes(BitConverter.GetBytes(model.VolumeMinColor));
            settings.volumeMaxColor.SetBytes(BitConverter.GetBytes(model.VolumeMaxColor));
            settings.mixChannelAColor.SetBytes(BitConverter.GetBytes(model.MixChannelAColor));
            settings.mixChannelBColor.SetBytes(BitConverter.GetBytes(model.MixChannelBColor));
            return settings;
        }

        public static SessionData ToSessionData(this IAudioDevice audioDevice, int index)
        {
            SessionData session = SessionData.Default();
            session.name = audioDevice.DisplayName;
            session.data.id = (byte)index;
            session.data.isDefault = audioDevice.IsDefault;
            session.data.volume = (byte)audioDevice.Volume;
            session.data.isMuted = audioDevice.IsMuted;
            return session;
        }

        public static SessionData ToSessionData(this IAudioSession audioSession, int index)
        {
            SessionData session = SessionData.Default();
            session.name = audioSession.DisplayName;
            session.data.id = (byte)index;
            session.data.isDefault = false;
            session.data.volume = (byte)audioSession.Volume;
            session.data.isMuted = audioSession.IsMuted;
            return session;
        }
    }
}
