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

        public static SessionData ToSessionData(this ISession[] audioDevices, int index)
        {
            SessionData session = SessionData.Default();
            session.name = audioDevices[index].DisplayName;
            session.data.id = (byte)index;
            session.data.isDefault = audioDevices[index].IsDefault;
            session.data.volume = (byte)audioDevices[index].Volume;
            session.data.isMuted = audioDevices[index].IsMuted;
            return session;
        }
    }
}
