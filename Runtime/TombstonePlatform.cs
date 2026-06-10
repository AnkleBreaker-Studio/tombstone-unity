using System.Runtime.InteropServices;
using UnityEngine;

namespace AnkleBreaker.Tombstone
{
    /// <summary>Maps Unity runtime info to the ingestion contract's os/arch whitelist.</summary>
    internal static class TombstonePlatform
    {
        internal static string Os()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "macos";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "linux";
                default:
                    return "other";
            }
        }

        internal static string Arch()
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X64:
                    return "x64";
                case Architecture.Arm64:
                    return "arm64";
                case Architecture.X86:
                    return "x86";
                default:
                    return "other";
            }
        }
    }
}
