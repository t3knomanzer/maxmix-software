using System.Collections.Generic;

namespace MaxMix.Services.Communication
{
    static class FirmwareVersions
    {
        // This is the set of firmware versions that work for this application,
        // Since most of the work is now app side, we only need to reset this list
        // if there is a change to the Communication Messages themselves that makes
        // the firmware / app incompatible.
        static HashSet<string> s_Valid = new HashSet<string>
        {
#if DEBUG
            "0.0.0",
#endif
            "1.5.0",
            "1.5.1",
            "1.5.2",
            "1.5.3",
            "1.5.4",
            "1.5.5"
        };

        public static bool IsCompatible(string version)
        {
            return s_Valid.Contains(version);
        }
    }
}
