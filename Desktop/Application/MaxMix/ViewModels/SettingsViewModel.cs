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
        public bool DisplayNewSession
        {
            get => _settings.DisplayNewSession;
            set
            {
                _settings.DisplayNewSession = value;
                RaisePropertyChanged();
            }
        }

        public bool SleepWhenInactive
        {
            get => _settings.SleepWhenInactive;
            set
            {
                _settings.SleepWhenInactive = value;
                RaisePropertyChanged();
            }
        }

        public int SleepAfterSeconds
        {
            get => _settings.SleepAfterSeconds;
            set
            {
                _settings.SleepAfterSeconds = value;
                RaisePropertyChanged();
            }
        }
        public bool CheckForUpdates
        {
            get => _settings.CheckForUpdates;
            set
            {
                _settings.CheckForUpdates = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Commands
        #endregion

        #region Public Methods
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