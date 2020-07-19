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

        // TODO: Delete, updates are checked automatically at application launch.
        public bool ContinuousScroll
        {
            get => _settings.ContinuousScroll;
            set
            {
                _settings.ContinuousScroll = value;
                RaisePropertyChanged();
            }
        }

        public bool SystemSounds
        {
            get => _settings.SystemSounds;
            set
            {
                _settings.SystemSounds = value;
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
        #endregion

        #region EventHandlers
        #endregion
    }
}