#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Isolated Play Mode fixture; never opens the player's save.</summary>
public static class HakoReadabilityFixture
{
    public const string SessionKey = "HAKO.ReadabilityFixture";
    public const string SaveKey = "HAKO.ReadabilityFixtureSave";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        var repo = new PlayerRepository(new SaveManager(SessionState.GetString(SaveKey, "readability_preview_test")));
        Loc.Init("ko");
        repo.EnsureStarterGecko();
        repo.GetPlayerData().progress.hatchIntroSeen = true;
        repo.GetPlayerData().settings.notificationOn = false;
        repo.GetPlayerData().settings.adAudienceVersion = AdAudiencePolicy.Version;
        repo.GetPlayerData().settings.adAudience = AdAudience.Unknown;
        var time = new TimeManager();
        var gecko = new GeckoManager(repo, time);
        GameManager.Initialize(repo, time, gecko, new StoreManager(repo), new TerrariumManager(repo),
            new RewardManager(repo), new SettingsManager(repo), new GeckoEventQueue(gecko));
        AdMobService.Create(false);
    }
}
#endif
