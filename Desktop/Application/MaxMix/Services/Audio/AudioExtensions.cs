using MaxMix.Services.Communication;
using NAudio.CoreAudioApi;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MaxMix.Services.Audio
{
    static class AudioExtensions
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

        #region Process Acess Flags
        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const int SYNCHRONIZE = 0x00100000;
        #endregion

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int access, bool inheritHandle, int processID);

        public static string GetMainModuleFileName(this Process process, int buffer = 1024)
        {
            var fileNameBuilder = new StringBuilder(buffer);
            uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
            IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE, false, process.Id);
            return QueryFullProcessImageName(handle, 0, fileNameBuilder, ref bufferLength) ? fileNameBuilder.ToString() : null;
        }

        public static string GetProductName(this Process process)
        {
            string fileName = process.GetMainModuleFileName();
            if (string.IsNullOrEmpty(fileName)) return null;
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(fileName);
                return versionInfo.ProductName;
            }
            catch
            {
                return "";
            }
        }

        public static void SetDefaultEndpoint(string deviceID, Role role)
        {
            using (var policyConfig = new PolicyConfig())
                policyConfig.SetDefaultEndpoint(deviceID, role);
        }

        // SessionIdentifier Example:
        // {0.0.0.00000000}.{09e7aedd-9901-4c3c-8ce3-e581147143e6}|\Device\HarddiskVolume11\Steam\steam.exe%b{00000000-0000-0000-0000-000000000000}
        // Format:
        // <Device ID>|<App Path>%b<Unknown GUID>

        public static string ExtractDeviceId(this string id)
        {
            var i1 = id.IndexOf('|');
            if (i1 < 0)
                return id;
            return id.Substring(0, i1);
        }

        public static string ExtractAppPath(this string id)
        {
            var i1 = id.IndexOf('|');
            if (i1 < 0)
                return id;

            var i2 = id.IndexOf("%b", i1);
            if (i2 < 0)
                return id;
            return id.Substring(i1 + 1, i2 - i1 - 1);
        }

        public static string ExtractUnknownGuid(this string id)
        {
            var i1 = id.IndexOf("%b");
            if (i1 < 0)
                return id;
            return id.Substring(i1 + 1, id.Length - i1 - 1);
        }

        public static DataFlow ToDataFlow(this DeviceFlow d)
        {
            return d == DeviceFlow.Input ? DataFlow.Capture : DataFlow.Render;
        }

        public static DeviceFlow ToDeviceFlow(this DataFlow d)
        {
            return d == DataFlow.Capture ? DeviceFlow.Input : DeviceFlow.Output;
        }

        public static DisplayMode ToDisplayMode(this DeviceFlow d)
        {
            return d == DeviceFlow.Input ? DisplayMode.MODE_INPUT : DisplayMode.MODE_OUTPUT;
        }
    }
}
