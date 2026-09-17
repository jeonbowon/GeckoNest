using UnityEngine;

/// <summary>유대 레벨마다 풀리는 것 (값 = 필요한 레벨)</summary>
public enum BondPerk
{
    None     = 0,
    Greet    = 1,   // 홈에 들어오면 앞발 인사 "왔구나!"
    PetLover = 2,   // 연달아 좋아하는 쓰다듬기 4 → 6, 하트 더 많이
    Come     = 3,   // 빈 바닥을 두 번 톡톡 → 그 자리로 다가옴
    Trick    = 4,   // 쓰다듬으면 가끔 공중 한 바퀴
    Palm     = 5,   // 게코를 길게 누르면 손바닥에 올라옴
}

/// <summary>
/// 유대 레벨 (2026-09-17) — 유대 점수 = 애정도(0~100) + 넘친 몫. 애정도가 가득 찬 뒤에도 돌봄이 쌓여
/// Lv.4~5로 오른다. 넘친 몫은 하루 DAILY_OVERFLOW_CAP까지만 (연타로 몰아서 못 올리게).
/// 점수·레벨 계산만 여기, 적립·보상은 GeckoManager(AddAffection · CheckBondLevel).
/// </summary>
public static class GeckoBond
{
    public const int   MAX_LEVEL          = 5;
    public const float DAILY_OVERFLOW_CAP = 30f;   // [TBD]

    // 레벨 n에 필요한 점수 (0번은 Lv.0) [TBD]
    public static readonly float[] LEVEL_POINTS = { 0f, 20f, 50f, 100f, 180f, 300f };

    // 레벨 n에 도달하면 받는 보상 [TBD]
    public static readonly (int coin, int gem)[] LEVEL_REWARDS = { (0, 0), (20, 0), (50, 0), (0, 2), (100, 0), (0, 5) };

    public const int PET_LIMIT       = 4;   // GeckoManager 기본 쓰다듬기 한도와 같게
    public const int PET_LIMIT_LOVER = 6;   // [TBD] Lv.2부터

    public static float Points(GeckoData g) => g == null ? 0f : Mathf.Max(0f, g.affection) + Mathf.Max(0f, g.bondOverflow);

    public static int LevelOf(float points)
    {
        int level = 0;
        for (int i = 1; i <= MAX_LEVEL; i++)
            if (points >= LEVEL_POINTS[i]) level = i;
        return level;
    }

    public static int Level(GeckoData g) => LevelOf(Points(g));

    /// <summary>다음 레벨에 필요한 점수 (최고 레벨이면 -1)</summary>
    public static float NextPoints(int level) => level >= MAX_LEVEL ? -1f : LEVEL_POINTS[level + 1];

    public static bool Has(GeckoData g, BondPerk perk) => perk == BondPerk.None || Level(g) >= (int)perk;

    public static BondPerk PerkOf(int level)
        => level >= 1 && level <= MAX_LEVEL ? (BondPerk)level : BondPerk.None;

    public static int PetLimit(GeckoData g) => Has(g, BondPerk.PetLover) ? PET_LIMIT_LOVER : PET_LIMIT;

    /// <summary>오늘 넘친 몫을 더 쌓을 수 있는 양 (날짜가 바뀌었으면 한도 전부)</summary>
    public static float RoomToday(GeckoData g, int today)
    {
        if (g == null) return 0f;
        float used = g.bondDay == today ? g.bondToday : 0f;
        return Mathf.Max(0f, DAILY_OVERFLOW_CAP - used);
    }

    public static bool TodayFull(GeckoData g, int today) => RoomToday(g, today) <= 0f;

    /// <summary>
    /// 애정도를 더한다 — 100까지는 애정도, 넘는 몫은 오늘 한도 안에서 bondOverflow. 실제로 더한 애정도를 돌려준다.
    /// </summary>
    public static float AddAffection(GeckoData g, float amount, int today)
    {
        if (g == null || amount <= 0f) return 0f;
        float toAffection = Mathf.Clamp(100f - g.affection, 0f, amount);
        g.affection += toAffection;

        float over = amount - toAffection;
        if (over > 0f)
        {
            if (g.bondDay != today)
            {
                g.bondDay   = today;
                g.bondToday = 0f;
            }
            float add = Mathf.Min(over, RoomToday(g, today));
            g.bondOverflow += add;
            g.bondToday    += add;
        }
        return toAffection;
    }
}
