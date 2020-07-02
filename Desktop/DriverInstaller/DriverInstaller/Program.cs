using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DriverInstaller
{
    class Program
    {
        /// <summary>
        /// Gets the path to the given system executable depending on OS architecture.
        /// </summary>
        /// <param name="executable">Name of the system executable.</param>
        /// <returns>The OS architecture independent path to the given executable.</returns>
        static string GetArchitectureExePath(string executable)
        {
            var result = string.Empty;
            var sys32 = Environment.ExpandEnvironmentVariables($"%SystemRoot%\\System32\\{executable}");
            var sysna = Environment.ExpandEnvironmentVariables($"%SystemRoot%\\Sysnative\\{executable}");

            if (File.Exists(sys32))
                result = sys32;
            else if (File.Exists(sysna))
                result = sysna;

            return result;
        }

        /// <summary>
        /// Gets the path to the current running executable.
        /// </summary>
        /// <returns>Current running exe path.</returns>
        static string CurrentPath()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        /// <summary>
        /// Installs the driver to the system using pnputil.
        /// </summary>
        static void Install()
        {
            // TODO: Move this to a constant field.
            var driverFilename = "ch341ser.inf";
            var driverFilePath = Path.Combine(CurrentPath(), "Driver", driverFilename);
            if (!File.Exists(driverFilePath))
                throw new FileNotFoundException($"Can't find driver at:\n {driverFilePath}");

            // TODO: Move exe filename to a constant field.
            var executable = GetArchitectureExePath("pnputil.exe");
            if (string.IsNullOrEmpty(executable))
                throw new FileNotFoundException("Can't find pnputil.exe. Please install driver manually.");

            Process process;
            string output = string.Empty;

            process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = $"\"{executable}\"";
            process.StartInfo.Arguments = "/enum-drivers";
            process.Start();

            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Skip if the driver is already installed
            if (output.ToLowerInvariant().Contains(driverFilename))
                return;

            process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = $"\"{executable}\"";
            process.StartInfo.Arguments = $"/add-driver \"{driverFilePath}\" /install";
            process.Start();

            output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();


            if (output.ToLowerInvariant().Contains("failed"))
                throw new InvalidOperationException("Error installing driver");
        }

        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Not used.</param>
        /// <returns>Standard DOS exit code.</returns>
        static int Main(string[] args)
        {
            Console.WriteLine("Installing driver...");

            try
            {
                Install();
                Console.WriteLine("Done!");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return 0;
        }
    }
}
