using MaxMix.Services.Communication;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MaxMix.ViewModels
{
    /// <summary>
    /// Manages interfacing between user and application settings system.
    /// </summary>
    internal class SettingsViewModel : BaseViewModel
    {
        #region Constructor
        public SettingsViewModel()
        {
            _settings = Properties.Settings.Default;
        }
        #endregion

        #region Events
        #endregion

        #region Consts
        #endregion

        #region Fields
        private Properties.Settings _settings;
        #endregion

        #region Properties
        /// <summary>
        /// Should this app be run at Windows startup.
        /// </summary>
        public bool RunAtStartup
        {
            get => IsRunAtStartup();
            set => SetRunAtStartup(value);
        }

        /// <summary>
        /// When a new session is created, notify the device to make it
        /// the current displayed item.
        /// </summary>
        public bool DisplayNewSession
        {
            get => _settings.DisplayNewSession;
            set
            {
                if (_settings.DisplayNewSession == value)
                    return;
                _settings.DisplayNewSession = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Wether the device should be put to sleep (power savings) or not 
        /// after it's been inactive by the time defined by SleepAfterSeconds.
        /// </summary>
        public bool SleepWhenInactive
        {
            get => _settings.SleepWhenInactive;
            set
            {
                if (_settings.SleepWhenInactive == value)
                    return;
                _settings.SleepWhenInactive = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Time for the device to be inactive before is put to sleep.
        /// </summary>
        public int SleepAfterSeconds
        {
            get => _settings.SleepAfterSeconds;
            set
            {
                if (_settings.SleepAfterSeconds == value)
                    return;
                _settings.SleepAfterSeconds = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Enable to loop around the applications list.
        /// </summary>
        public bool LoopAroundItems
        {
            get => _settings.LoopAroundItems;
            set
            {
                if (_settings.LoopAroundItems == value)
                    return;
                _settings.LoopAroundItems = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Value used in the acceleration algorithm.
        /// Increasing the divisor will reduce the acceleration effect.
        /// </summary>
        public uint AccelerationPercentage
        {
            get => _settings.AccelerationPercentage;
            set
            {
                if (_settings.AccelerationPercentage == value)
                    return;
                _settings.AccelerationPercentage = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Value used to detect a double click from the encoder switch.
        /// </summary>
        public ushort DoubleTapTime
        {
            get => _settings.DoubleTapTime;
            set
            {
                if (_settings.DoubleTapTime == value)
                    return;
                _settings.DoubleTapTime = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Value used to set the light color for minimum volume
        /// </summary>
        public uint VolumeMinColor
        {
            get => _settings.VolumeMinColor;
            set
            {
                if (_settings.VolumeMinColor == value)
                    return;
                _settings.VolumeMinColor = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Value used to set the light color for maximum volume
        /// </summary>
        public uint VolumeMaxColor
        {
            get => _settings.VolumeMaxColor;
            set
            {
                if (_settings.VolumeMaxColor == value)
                    return;
                _settings.VolumeMaxColor = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Value used to set the color of the channel A in mix mode
        /// </summary>
        public uint MixChannelAColor
        {
            get => _settings.MixChannelAColor;
            set
            {
                if (_settings.MixChannelAColor == value)
                    return;
                _settings.MixChannelAColor = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Value used to set the color of the channel B in mix mode
        /// </summary>
        public uint MixChannelBColor
        {
            get => _settings.MixChanneBColor;
            set
            {
                if (_settings.MixChanneBColor == value)
                    return;
                _settings.MixChanneBColor = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Value used to set the light color for maximum volume
        /// </summary>
        public DisplayMode StartupMode
        {
            get => _settings.StartupMode;
            set
            {
                if (_settings.StartupMode == value)
                    return;
                if (_settings.StartupMode == DisplayMode.MODE_SPLASH || _settings.StartupMode == DisplayMode.MODE_MAX)
                    return;
                _settings.StartupMode = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Commands
        #endregion

        #region Overrides
        public override void Start() { }

        public override void Stop()
        {
            _settings.Save();
        }

        #endregion

        #region Private Methods
        private bool IsRunAtStartup()
        {
            try
            {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Assembly curAssembly = Assembly.GetExecutingAssembly();
                return (string)key.GetValue(curAssembly.GetName().Name) == curAssembly.Location;
            }
            catch
            {
            }
            return false;
        }
        private void SetRunAtStartup(bool enabled)
        {
            try
            {
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                Assembly curAssembly = Assembly.GetExecutingAssembly();

                if (enabled)
                {
                    key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
                }
                else
                {
                    key.DeleteValue(curAssembly.GetName().Name);
                }
            }
            catch
            {
            }
        }
        #endregion

        #region EventHandlers
        protected override void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            base.SetProperty(ref field, value, name);
            _settings.Save();

        }
        #endregion
    }
}