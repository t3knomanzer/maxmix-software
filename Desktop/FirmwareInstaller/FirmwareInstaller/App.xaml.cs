using FirmwareInstaller.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace FirmwareInstaller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            var window = new MainWindow();

            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            window.Title = string.Format("{0} {1}", "MaxMix Firmware Installer", assemblyVersion);

            var dataContext = new MainViewModel();
            dataContext.ExitRequested += OnExitRequested;

            window.DataContext = dataContext;
            dataContext.Start();

            window.Show();
        }

        private void OnExitRequested(object sender, EventArgs e)
        {
            var dataContext = (MainViewModel)sender;
            dataContext.Stop();

            Application.Current.Shutdown();
        }
    }
}
