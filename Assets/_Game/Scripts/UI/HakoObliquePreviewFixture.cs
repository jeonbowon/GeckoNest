#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>Batch screenshot fixture only. Never reads the player's save or runs in a player build.</summary>
public static class HakoObliquePreviewFixture
{
    public const string SessionKey = "HAKO.ObliquePreview";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        Loc.Init("ko");
        var repo = new PlayerRepository(new SaveManager("oblique_preview_" + Guid.NewGuid().ToString("N")));
        repo.EnsureStarterGecko();
        var data = repo.GetPlayerData();
        data.progress.hatchIntroSeen = true;
        data.progress.adultCount = 3;
        data.progress.achievements.Add("first_adult");
        data.progress.achievements.Add("gecko_family");
        data.coin = 240; data.gem = 8;
        var g = repo.GetGecko(data.selectedGeckoId);
        g.growthStage = 4; g.hunger = 78; g.thirst = 86; g.health = 92; g.mood = 82; g.cleanliness = 88;
        g.affection = 65; g.moltProgress = 23;
        g.giftDay = RewardManager.TodayNumber();
        data.terrarium.decorSlots[0] = "decor_moss_rock";
        data.terrarium.decorSlots[1] = "decor_cave";
        data.terrarium.decorSlots[2] = "decor_branch";
        data.terrarium.decorPositions[0] = new Vector2(-310,485);
        data.terrarium.decorPositions[1] = new Vector2(325,695);
        var time = new TimeManager();
        var gecko = new GeckoManager(repo,time);
        var rewards = new RewardManager(repo); gecko.OnCareDone += rewards.RecordCare;
        GameManager.Initialize(repo,time,gecko,new StoreManager(repo),new TerrariumManager(repo),rewards,
            new SettingsManager(repo),new GeckoEventQueue(gecko));
        Debug.Log("[ObliquePreview] Isolated in-memory adult fixture, no player save loaded.");
    }
}
#endif
