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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;

namespace MaxMix
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        IDisposable _errorReporter;
        private static Mutex _singleInstanceMutex = null;

        private void InitErrorReporting()
        {
            _errorReporter = SentrySdk.Init("https://54cf266b03ed4ee380b0577653172a98@o431430.ingest.sentry.io/5382488");
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new User { Username = Environment.MachineName };
            });

        }

        void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            SentrySdk.CaptureException(e.Exception);
            _errorReporter.Dispose();
        }

        private bool IsApplicationRunning()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            _singleInstanceMutex = new Mutex(true, assemblyName, out bool mutexAcquired);
            return mutexAcquired;
        }

        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            if (!IsApplicationRunning())
            {
                // Application is already running
                Debug.WriteLine("[App] Application is already running, exiting.");
                Application.Current.Shutdown();
                return;
            }

            if (!Debugger.IsAttached)
            {
                // Initialize error reporing only if not running from Visual Studio.
                InitErrorReporting();
                DispatcherUnhandledException += OnDispatcherUnhandledException;
            }

            var window = new MainWindow();

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
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
            if (_errorReporter != null)
            {
                _errorReporter.Dispose();
            }
            _singleInstanceMutex.Dispose();

            Application.Current.Shutdown();
        }


    }
}
