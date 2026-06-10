using UnityEngine;

namespace AnkleBreaker.Tombstone
{
    /// <summary>
    /// Single internal logger for the SDK. Every internal failure funnels through here so the
    /// SDK never throws into game code, and so SDK-emitted lines carry a stable prefix that the
    /// breadcrumb recorder filters out (prevents a Tombstone-warning → breadcrumb feedback loop).
    /// </summary>
    internal static class TombstoneLog
    {
        /// <summary>Prefix on every SDK log line; breadcrumb capture skips lines that start with it.</summary>
        internal const string PREFIX = "[Tombstone] ";

        /// <summary>Log a non-fatal SDK warning. Never throws.</summary>
        internal static void Warn(string message)
        {
            try { Debug.LogWarning(PREFIX + message); }
            catch { /* logging must never take the game down */ }
        }
    }
}
