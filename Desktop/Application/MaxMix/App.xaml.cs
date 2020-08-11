using MaxMix.ViewModels;
using Sentry;
using Sentry.Protocol;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace MaxMix
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var dataContext = new MainViewModel();
            dataContext.ExitRequested += OnExitRequested;

            var window = new MainWindow();
            window.Title = string.Format("{0} {1}", assemblyName, assemblyVersion);
            window.DataContext = dataContext;

            var deviceWindow = new DeviceWindow();
            deviceWindow.Title = string.Format("{0} {1}", assemblyName, assemblyVersion);
            deviceWindow.DataContext = dataContext;
            deviceWindow.Show();

            dataContext.Start();
        }

        private void OnExitRequested(object sender, EventArgs e)
        {
            var viewModel = (MainViewModel)sender;
            viewModel.Stop();

            // Calling dispose explicitly on closing so the icon dissapears from the windows task bar.
            var window = (MainWindow)Application.Current.MainWindow;            
            window.taskbarIcon.Dispose();

            Application.Current.Shutdown();
        }


    }
}
