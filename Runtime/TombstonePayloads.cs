using System;

namespace AnkleBreaker.Tombstone
{
    // Wire DTOs serialized with JsonUtility. CONTRACT WARNING: JsonUtility serializes EVERY
    // public field — null strings become "" and null arrays become []. The server normalizes
    // empty optional ids via cleanOptionalId(); tests/unity-contract.test.ts pins these exact
    // shapes against the Zod ingest schemas. Never rename/add/remove a field here without
    // checking the server schema and the contract test.

    /// <summary>Crash report wire shape for <c>POST /api/v1/ingest/crashes</c>.</summary>
    [Serializable]
    public class CrashPayload
    {
        /// <summary>UTC ISO-8601 timestamp of the exception.</summary>
        public string occurredAtIso;
        /// <summary>Game build version (<c>Application.version</c>).</summary>
        public string buildVersion;
        /// <summary>Whitelisted OS name: windows | macos | linux | other.</summary>
        public string os;
        /// <summary>Whitelisted CPU architecture: x64 | arm64 | x86 | other.</summary>
        public string arch;
        /// <summary>Client-side SHA-256 fingerprint (message + normalized top frames).</summary>
        public string signature;
        /// <summary>Short human-readable hint (the exception message), max 512 chars.</summary>
        public string stackHint;
        /// <summary>Full managed stack trace, max 8192 chars (server derives the canonical signature).</summary>
        public string stackTrace;
        /// <summary>Player id set via <see cref="Tombstone.SetUser"/> ("" when anonymous).</summary>
        public string userId;
        /// <summary>Steam64 id set via <see cref="Tombstone.SetUser"/> ("" when absent).</summary>
        public string steamId;
        /// <summary>Recent log trail leading up to the crash (oldest → newest).</summary>
        public Breadcrumb[] breadcrumbs;
        /// <summary>When true the server returns <c>data.logUpload</c> (presigned PUT for the
        /// session log). JsonUtility serializes <c>false</c> when unset — the server treats
        /// <c>"log":false</c> as no-log, so the plain bool field is contract-safe.</summary>
        public bool log;
    }

    /// <summary>One entry of the recent-log trail attached to crashes and bug reports.</summary>
    [Serializable]
    public class Breadcrumb
    {
        /// <summary>UTC ISO-8601 timestamp of the log line.</summary>
        public string tsIso;
        /// <summary>Severity label (Unity LogType name or Info/Warning/Error for manual crumbs).</summary>
        public string level;
        /// <summary>Log message, max 512 chars. Manual category is folded in as a "[category] " prefix.</summary>
        public string message;
    }

    /// <summary>Player bug report wire shape for <c>POST /api/v1/ingest/bug-reports</c>.</summary>
    [Serializable]
    public class BugPayload
    {
        /// <summary>UTC ISO-8601 timestamp of the report.</summary>
        public string occurredAtIso;
        /// <summary>Game build version (<c>Application.version</c>).</summary>
        public string buildVersion;
        /// <summary>Whitelisted OS name: windows | macos | linux | other.</summary>
        public string os;
        /// <summary>Whitelisted CPU architecture: x64 | arm64 | x86 | other.</summary>
        public string arch;
        /// <summary>Optional free-form category (e.g. "ui"), max 32 chars ("" when absent).</summary>
        public string category;
        /// <summary>Player-written report body, max 4000 chars.</summary>
        public string message;
        /// <summary>Player id set via <see cref="Tombstone.SetUser"/> ("" when anonymous).</summary>
        public string userId;
        /// <summary>Steam64 id set via <see cref="Tombstone.SetUser"/> ("" when absent).</summary>
        public string steamId;
        /// <summary>Recent log trail leading up to the report (oldest → newest).</summary>
        public Breadcrumb[] breadcrumbs;
        /// <summary>When true the server returns <c>data.logUpload</c> (presigned PUT for the
        /// session log). Same JsonUtility quirk/contract note as <see cref="CrashPayload.log"/>.</summary>
        public bool log;
    }

    /// <summary>Session heartbeat wire shape for <c>POST /api/v1/ingest/heartbeats</c> (CCU / Sessions / Fleet).</summary>
    [Serializable]
    public class HeartbeatPayload
    {
        /// <summary>Stable per-launch session id (GUID minted at Init).</summary>
        public string sessionId;
        /// <summary>UTC ISO-8601 timestamp of the heartbeat.</summary>
        public string occurredAtIso;
        /// <summary>Game build version (<c>Application.version</c>) — feeds the Releases/Fleet screens.</summary>
        public string buildVersion;
        /// <summary>Whitelisted OS name: windows | macos | linux | other.</summary>
        public string os;
        /// <summary>Whitelisted CPU architecture: x64 | arm64 | x86 | other.</summary>
        public string arch;
        /// <summary>Player id set via <see cref="Tombstone.SetUser"/> ("" when anonymous) — feeds the Sessions screen.</summary>
        public string userId;
    }

    // ── Ingest response DTOs (parse-only) ───────────────────────────────────────────────────
    // JsonUtility.FromJson is lenient: unknown response fields are ignored, and absent nested
    // objects come back default-constructed — so the only reliable presence check is
    // string.IsNullOrEmpty(logUpload.url). Internal: never part of the public SDK surface.

    /// <summary>Envelope of a 2xx ingest response (<c>{"success":true,"data":{...}}</c>).</summary>
    [Serializable]
    internal sealed class IngestResponse
    {
        /// <summary>Server success flag (informational — HTTP status is authoritative).</summary>
        public bool success;
        /// <summary>Response payload; <c>logUpload</c> is present when the request set <c>"log":true</c>.</summary>
        public IngestResponseData data;
    }

    /// <summary>Data member of a crash / bug-report ingest response.</summary>
    [Serializable]
    internal sealed class IngestResponseData
    {
        /// <summary>Presigned session-log upload target (empty url when not granted).</summary>
        public LogUploadTarget logUpload;
    }

    /// <summary>Presigned S3 PUT target for the session log (content-type text/plain).</summary>
    [Serializable]
    internal sealed class LogUploadTarget
    {
        /// <summary>Presigned PUT URL (time-limited; a 403 on PUT means it expired — drop).</summary>
        public string url;
        /// <summary>S3 object key the server stored on the crash/bug row.</summary>
        public string key;
    }
}
