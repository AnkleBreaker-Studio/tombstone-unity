# Tombstone Unity package — `com.anklebreaker.tombstone`

UPM package that captures **managed C# exceptions**, **session heartbeats**, **analytics
events**, **breadcrumbs**, and **player bug reports** in a Unity game and uploads them to a
Tombstone ingestion endpoint with a per-game SDK token. The native crash core
([`../native/`](../native/)) reports native crashes through the same endpoints once
integrated. Follows the AnkleBreaker `com.anklebreaker.*` convention and C# naming standard.

## What's automatic

After `Tombstone.Init` (or zero-code auto-init from the config asset), the SDK handles most
cases with **zero further integration**:

| Capability | Automatic? | How |
|---|---|---|
| Unhandled exceptions (main + background threads) | ✅ Automatic | `Application.logMessageReceivedThreaded`, deduped (≤1 report per signature per 60s; repeats become a counter breadcrumb) |
| Unobserved `Task` exceptions | ✅ Automatic | `TaskScheduler.UnobservedTaskException` (observed + reported, never escalated) |
| AppDomain unhandled exceptions | ✅ Automatic | `AppDomain.CurrentDomain.UnhandledException` (write-ahead persisted before the process dies) |
| Errors / warnings / logs as breadcrumbs | ✅ Automatic | Every Unity log line → 50-entry ring, attached to crashes & bug reports |
| Player log upload on crash | ✅ Automatic | Rolling ~512 KB `session.log`, PUT to a presigned URL after the crash report's 2xx |
| Unclean shutdown (hard crash / OOM kill / force quit) | ✅ Automatic (next launch) | `session.lock` marker + preserved `previous-session.log`, reported as signature `unclean-shutdown` |
| Session heartbeats / CCU | ✅ Automatic | Every N seconds (default 60) |
| Offline durability + retry | ✅ Automatic | Write-ahead queue, exponential backoff, next-launch retry |
| Player identification | One-liner | `Tombstone.SetUser("user-123", steamId)` |
| Analytics events | One-liner | `Tombstone.TrackEvent("level_complete", props)` |
| Player bug reports (log attached automatically) | One-liner | `Tombstone.ReportBug("…", category)` |
| Manual breadcrumbs | One-liner | `Tombstone.AddBreadcrumb("…", level, category)` |
| Caught-but-interesting exceptions | One-liner | `Tombstone.ReportException(ex)` |

All three autonomy systems can be toggled on the config asset (`Auto Capture Exceptions`,
`Upload Logs`, `Detect Unclean Shutdown` — default ON) and are consent-gated: with
*Require Consent* enabled, nothing is captured, mirrored, or reported until
`Tombstone.SetConsent(true)`.

## Install

**Via UPM git URL** — Window ▸ Package Manager ▸ `+` ▸ *Add package from git URL…*:

```
https://github.com/AnkleBreaker-Studio/tombstone.git?path=unity
```

Or add to `Packages/manifest.json`:

```jsonc
{ "dependencies": { "com.anklebreaker.tombstone": "https://github.com/AnkleBreaker-Studio/tombstone.git?path=unity" } }
```

Or copy `unity/` into your project's `Packages/`. Requires Unity **6 (6000.0)+** (Mono and IL2CPP).

## Editor plugin & account sign-in

The package ships an editor plugin (UI Toolkit, forge-dark themed like the web dashboard).
**It requires a Tombstone account** — until you sign in, the editor tooling is inert and a
gentle one-time prompt points you to the Hub.

1. **Sign in** — `Window ▸ Tombstone ▸ Sign In` (or the Hub's sign-in button). Credentials
   are exchanged for an editor token stored **per-user in EditorPrefs** — it never enters
   the project folder or version control. No account? The window links to the signup page.
2. **Link the project** — `Window ▸ Tombstone ▸ Hub` → pick your studio + game (loaded from
   your account) → **Link this project**. This mints a per-game `tmb_` SDK token and writes
   endpoint + token into the `TombstoneConfig` asset (created at
   `Assets/Tombstone/Resources/TombstoneConfig.asset` if missing) — the same asset the
   runtime auto-init reads. The game binding is saved to
   `ProjectSettings/TombstoneSettings.asset` (safe to commit; it contains ids, not secrets).
3. **Watch the dashboard** — the Hub's *Dashboard* tab shows live crash-free %, 24h/7d crash
   counts, a spike banner, the top-10 signatures (click → opens the signature in the web
   dashboard), and a 30-day trend. Refresh manually or enable 60s auto-refresh.
4. **Project Settings** — `Edit ▸ Project Settings ▸ Tombstone`: endpoint URL override
   (self-hosted/staging), heartbeat interval and consent defaults (written into the config
   asset), unlink, and sign out.

## Quick start

1. Sign in and **link this project** from the editor Hub (above) — it mints the SDK token
   and writes the config for you. (Manual alternative: mint a `tmb_…` token from the game's
   *SDK tokens* page on the web dashboard.)
2. Initialize the SDK, either way:

**Zero-code:** `Create ▸ Tombstone ▸ Config`, fill in your token + endpoint, place the
asset under any `Resources/` folder named `TombstoneConfig`. It auto-initializes on load
(enable *Require Consent* to stay silent until `SetConsent(true)`).

**Manual:**
```csharp
using System.Collections.Generic;
using AnkleBreaker.Tombstone;

Tombstone.Init("tmb_…", "https://your-tenant.example.com");   // once, at boot
Tombstone.SetConsent(true);                                    // GDPR / store-policy gate
Tombstone.SetUser("user-123", steamId: "7656119…");            // once auth resolves

// Analytics events (events & funnels screens)
Tombstone.TrackEvent("level_complete",
    new Dictionary<string, string> { { "level", "3" }, { "difficulty", "hard" } });

// Breadcrumbs: the log trail attached to the next crash / bug report
Tombstone.AddBreadcrumb("matchmaking started", BreadcrumbLevel.Info, category: "net");

// In-game feedback form
Tombstone.ReportBug("Quest log empty after load", category: "ui");

// Caught-but-interesting exceptions
try { Load(); } catch (Exception e) { Tombstone.ReportException(e); }
```

3. Trigger a test exception — the crash, its signature, and the 30-day trend appear on the
   game dashboard within seconds.

## What it does
- **Managed exceptions** → `Application.logMessageReceivedThreaded` (background threads too),
  plus `TaskScheduler.UnobservedTaskException` and `AppDomain.CurrentDomain.UnhandledException`
  → SHA-256 signature over the message + normalized top frames → `POST /api/v1/ingest/crashes`.
  Identical signatures dedupe to ≤1 report/min (repeats ride the breadcrumb trail as a counter).
- **Session log**: every log line mirrors into a rolling ~512 KB
  `persistentDataPath/Tombstone/session.log` (in-memory buffer, flushed off the main thread at
  most once per 5s + a final flush on the crash path). Crash and bug reports request a log
  upload (`"log": true`); after the 2xx the SDK PUTs the file (text/plain) to the presigned
  `data.logUpload.url` from the response — reusing the upload queue's backoff, never storing
  the presign across launches.
- **Unclean shutdowns**: Init writes `Tombstone/session.lock`; a clean quit
  (`Application.quitting`) removes it. If a marker survives to the next launch, the previous
  session died hard (native crash, OOM kill, force quit) — the SDK reports a synthetic crash
  (signature `unclean-shutdown`, previous session's buildVersion/os/arch) and uploads the
  preserved `previous-session.log`. If the write-ahead queue already held a managed crash from
  that session, no synthetic report is sent (no double-counting) — the restored crash's retry
  carries the previous log instead.
- **Session heartbeats** → `POST /api/v1/ingest/heartbeats` every N seconds (default 60,
  clamped 15–600) with `sessionId`/`buildVersion`/`os`/`arch`/`userId` — feeds the Live
  Fleet/CCU, Sessions, and Releases screens.
- **Analytics events** → `Tombstone.TrackEvent(name, props)` → `POST /api/v1/ingest/events`
  (flat attributes, clamped to the server contract: ≤32 entries, 64-char keys, 512-char values).
- **Bug reports** → `Tombstone.ReportBug(...)` → `POST /api/v1/ingest/bug-reports`.
- **Breadcrumbs**: every Unity log line + manual `AddBreadcrumb(...)` entries land in a fixed
  50-entry ring buffer (zero allocations beyond the stored strings) and ride along on crashes
  and bug reports.
- **Offline-first + durable**: crashes/bugs are written to disk *before* the first upload
  attempt (write-ahead), uploads retry in-session with exponential backoff (2s→32s, 5
  attempts), poison payloads (HTTP 4xx) are dropped, and anything undelivered retries on the
  next launch from `Application.persistentDataPath/Tombstone` (server de-dups by ULID).
- **Consent-aware**: until `SetConsent(true)` (when required), *nothing* is captured or sent —
  including heartbeats.
- **Fail-silent**: the SDK never throws into game code; internal failures are logged once with
  a `[Tombstone]` prefix.
- Auto-fills `buildVersion` (`Application.version`) and `os`/`arch` (platform mapping), cached
  at Init so capture is safe from any thread.

## Public API
```csharp
Tombstone.Init(gameToken, endpoint, heartbeatIntervalSeconds = 60f);
Tombstone.SetUser(userId, steamId = null);
Tombstone.SetConsent(bool granted);
Tombstone.TrackEvent(name, Dictionary<string,string> props = null);
Tombstone.AddBreadcrumb(message, BreadcrumbLevel level = Info, category = null);
Tombstone.ReportException(exception);
Tombstone.ReportBug(message, category = null);
```

## Conventions
AnkleBreaker C# standard (csharp-unity skill): private fields `_camelCase`, public
methods `PascalCase`, private methods `camelCase`, constants `UPPER_SNAKE_CASE`,
ScriptableObjects `{Name}SO`. Unity message methods stay PascalCase. The SDK keeps a thin
static facade (Sentry-style) rather than the Manager/HandlerData triad because it ships to
external studios and must not depend on AnkleBreaker's in-game framework. IL2CPP/AOT-safe:
no reflection-based serialization beyond JsonUtility, no dynamic codegen.

## Not yet
- Native crash core (Windows SEH / POSIX signals / Mach) + on-disk minidump upload —
  managed exceptions are covered today; native is the next track (`../native/`, plan written).
- Screenshot attachment on bug reports (server presign flow exists; SDK side pending).
- Breadcrumb `category` as a first-class wire field (currently folded into the message as a
  `[category] ` prefix — the ingest schema has no category field yet).

> Target engine confirmed: **Unity** (Mithrall, appId 2838890).
