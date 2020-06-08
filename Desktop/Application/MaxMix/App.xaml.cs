using MaxMix.ViewModels;
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

namespace MaxMix
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            var window = new MainWindow();

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            window.Title = string.Format("{0} {1}", assemblyName, assemblyVersion);

            var dataContext = new MainViewModel();
            dataContext.ExitRequested += OnExitRequested;

            window.DataContext = dataContext;
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
