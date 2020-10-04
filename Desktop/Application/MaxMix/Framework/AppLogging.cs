using System;
using System.Diagnostics;

namespace MaxMix.Framework
{
    internal static class AppLogging
    {
        [Conditional("DEBUG")]
        public static void DebugLog(params string[] msgs)
        {
            Console.WriteLine(string.Join("\t", msgs));
        }
    }
}
