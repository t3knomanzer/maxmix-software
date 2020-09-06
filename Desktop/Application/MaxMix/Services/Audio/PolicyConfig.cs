using CSCore.CoreAudioAPI;
using CSCore.Win32;
using System;
using System.Runtime.InteropServices;

namespace MaxMix.Services.Audio
{
    [Guid("8F9FB2AA-1C0B-4D54-B6BB-B2F2A10CE03C")]
    class PolicyConfig : ComObject
    {
        public PolicyConfig() : base(CreatePolicyConfig()) { }

        private static IntPtr CreatePolicyConfig()
        {
            var obj = new PolicyConfigObject();
            if (obj is IPolicyConfig c)
                return Marshal.GetComInterfaceForObject(c, typeof(IPolicyConfig));
            throw new NotSupportedException("Unable to create PolicyConfig on this OS.");
        }

        public void SetDefaultEndpoint(string id, Role role)
        {
            var obj = Marshal.GetObjectForIUnknown(BasePtr);
            int result = unchecked((int)0x80090011); // Object not found error
            if (obj is IPolicyConfig c)
                result = c.SetDefaultEndpoint(id, role);
            Marshal.ThrowExceptionForHR(result);
        }

        [ComImport]
        [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        private class PolicyConfigObject
        {
        }
    }

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPolicyConfig : IUnknown
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
