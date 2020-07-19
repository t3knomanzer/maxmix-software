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
    }
}
