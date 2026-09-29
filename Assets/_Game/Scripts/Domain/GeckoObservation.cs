using UnityEngine;

public enum GeckoNeed { None, Food, Water, Clean, Recovery, Rest, Shedding }
public enum GeckoActivity { Watching, Exploring, Foraging, SeekingWater, Resting, Shedding, Climbing, Perching, Hiding, Approaching, Startled, Drinking, Basking }

/// <summary>Read-only interpretation of care state. Never advances time or changes saved data.</summary>
public static class GeckoObservation
{
    public const float CARE_THRESHOLD = 55f; // [TBD] show needs before a critical warning
    public const float RECOVERY_THRESHOLD = 45f; // [TBD]

    public static GeckoNeed Need(GeckoData g)
    {
        if (g == null) return GeckoNeed.None;
        // Food/water take priority over resting, including a hungry gecko with low mood.
        if (g.thirst < CARE_THRESHOLD && g.thirst <= g.hunger) return GeckoNeed.Water;
        if (g.hunger < CARE_THRESHOLD) return GeckoNeed.Food;
        if (g.thirst < CARE_THRESHOLD) return GeckoNeed.Water;
        if (g.cleanliness < CARE_THRESHOLD) return GeckoNeed.Clean;
        if (g.health < RECOVERY_THRESHOLD) return GeckoNeed.Recovery;
        if (g.moltProgress >= 80f) return GeckoNeed.Shedding;
        if (g.mood < 35f) return GeckoNeed.Rest;
        return GeckoNeed.None;
    }

    // Stable individual pacing; no new save fields and no session-dependent string hash.
    public static float Curiosity(string id)
    {
        uint hash = 2166136261;
        if (id != null) foreach (char c in id) hash = unchecked((hash ^ c) * 16777619);
        return 0.85f + (hash % 301) / 1000f; // [TBD] 0.85..1.15
    }

    public static float Pace(GeckoData g)
    {
        if (g == null) return 1f;
        float condition = Mathf.Lerp(0.65f, 1.05f, Mathf.Clamp01(g.health / 80f));
        return condition * Curiosity(g.id);
    }
}
