#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Isolated Play Mode fixture; never opens the player's save.</summary>
public static class HakoAgePreviewFixture
{
    public const string SessionKey = "HAKO.AgePreview";
    public const string SaveKey = "HAKO.AgePreviewSave";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        var repo = new PlayerRepository(new SaveManager(SessionState.GetString(SaveKey, "age_preview_test")));
        Loc.Init("ko");
        repo.EnsureStarterGecko();
        repo.GetPlayerData().progress.hatchIntroSeen = true;
        repo.GetPlayerData().settings.notificationOn = false;
        var time = new TimeManager();
        var gecko = new GeckoManager(repo, time);
        GameManager.Initialize(repo, time, gecko, new StoreManager(repo), new TerrariumManager(repo),
            new RewardManager(repo), new SettingsManager(repo), new GeckoEventQueue(gecko));
        AdMobService.Create(false);
    }
}
#endif
