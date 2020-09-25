using System;
using System.Timers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using MaxMix.Services;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using MaxMix.Framework.Mvvm;
using MaxMix.Services.Communication;
using System.Runtime.CompilerServices;
using System.Reflection;

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
                _settings.MixChannelAColor = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Value used to set the color of the channel B in mix mode
        /// </summary>
        public uint MixChannelBColor
        {
            get => _settings.MixChannelBColor;
            set
            {
                _settings.MixChannelBColor = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Value used to detect a double click from the encoder switch.
        /// </summary>
        public string ItemsBlackList
        {
            get => _settings.ItemsBlackList;
            set
            {
                _settings.ItemsBlackList = value;
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
                return ((string)(key.GetValue(curAssembly.GetName().Name)) == curAssembly.Location);
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
        #endregion
    }
}