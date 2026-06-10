using UnityEngine;
using AnkleBreaker.Tombstone;

/// <summary>
/// Two ways to start Tombstone:
///   1) Zero-code: create a TombstoneConfig asset (Create ▸ Tombstone ▸ Config), fill in the
///      token + endpoint, and drop it under any Resources/ folder. It auto-inits on load.
///   2) Manual: put this component on a GameObject in your first scene (or call from your own
///      bootstrap). Replace the token + endpoint with your game's values from the dashboard.
/// </summary>
public sealed class TombstoneBootstrap : MonoBehaviour
{
    [SerializeField] private string _gameToken = "tmb_REPLACE_ME";
    [SerializeField] private string _endpoint = "https://your-tenant.example.com";

    private void Awake()
    {
        Tombstone.Init(_gameToken, _endpoint);
        // Once your auth resolves the player:
        // Tombstone.SetUser("user-123", steamId: "7656119...");
        // Analytics events (events & funnels screens):
        // Tombstone.TrackEvent("level_complete", new Dictionary<string, string> { { "level", "3" } });
        // Mark interesting moments for the crash/bug trail:
        // Tombstone.AddBreadcrumb("matchmaking started", BreadcrumbLevel.Info, category: "net");
        // From an in-game feedback form:
        // Tombstone.ReportBug("Quest log is empty after loading a save.", category: "ui");
    }
}
