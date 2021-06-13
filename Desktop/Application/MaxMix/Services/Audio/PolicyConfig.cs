using NAudio.CoreAudioApi;
using System;
using System.Runtime.InteropServices;

namespace MaxMix.Services.Audio
{
    [Guid("8F9FB2AA-1C0B-4D54-B6BB-B2F2A10CE03C")]
    class PolicyConfig : IDisposable
    {
        private IPolicyConfig _policyConfig;

        public PolicyConfig() 
        {
            _policyConfig = new PolicyConfigComObject() as IPolicyConfig;
        }

        public void SetDefaultEndpoint(string id, Role role)
        {
            Marshal.ThrowExceptionForHR(_policyConfig.SetDefaultEndpoint(id, role));
        }

        [ComImport]
        [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        private class PolicyConfigComObject
        {
        }

        public void Dispose()
        {
            Marshal.ReleaseComObject(_policyConfig);
            GC.SuppressFinalize(this);
        }

        ~PolicyConfig()
        {
            Dispose();
        }
    }

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPolicyConfig
    {
        void Unused1();
        void Unused2();
        void Unused3();
        void Unused4();
        void Unused5();
        void Unused6();
        void Unused7();
        void Unused8();
        void Unused9();
        void Unused10();
        int SetDefaultEndpoint([In, MarshalAs(UnmanagedType.LPWStr)] string id, Role role);
        void Unused12();
    }
}
