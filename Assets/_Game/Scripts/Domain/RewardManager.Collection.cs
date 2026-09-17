using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>업적이 세는 값</summary>
public enum AchievementStat
{
    Molts,      // 모든 게코의 허물 횟수 합 (GeckoData.moltCount)
    Adults,     // 키운 어덜트 수 (ProgressData.adultCount)
    Pets,       // 쓰다듬기 (ProgressData.petCount)
    Feeds,      // 먹이 주기 (ProgressData.feedCount)
    GoalDays,   // 오늘의 돌봄 보상을 받은 날 (ProgressData.goalDays)
    Geckos,     // 지금 함께 키우는 게코 수
    BondLevel,  // 가장 친한 게코의 유대 레벨 (GeckoBond)
    Morphs,     // 얻은 모프 수 (ProgressData.morphIds)
}

/// <summary>업적 하나 — 이름·설명 문구는 번역표 achieve.{id} · achieve.desc.{stat}</summary>
public readonly struct AchievementDef
{
    public readonly string          id;
    public readonly AchievementStat stat;
    public readonly int             target;
    public readonly int             coin;
    public readonly int             gem;

    public AchievementDef(string id, AchievementStat stat, int target, int coin, int gem)
    {
        this.id = id; this.stat = stat; this.target = target; this.coin = coin; this.gem = gem;
    }

    public string NameKey => "achieve." + id;
    public string DescKey => "achieve.desc." + stat.ToString().ToLowerInvariant();
}

/// <summary>게코 종 목록 — Resources/Species 전체, 가격 → id 순서 (도감 줄 순서)</summary>
public static class SpeciesCatalog
{
    private static GeckoSpeciesSO[] s_all;

    public static IReadOnlyList<GeckoSpeciesSO> All
    {
        get
        {
            if (s_all == null)
            {
                var list = new List<GeckoSpeciesSO>(Resources.LoadAll<GeckoSpeciesSO>("Species"));
                list.RemoveAll(s => s == null || string.IsNullOrEmpty(s.speciesId));
                list.Sort((a, b) => a.coinPrice != b.coinPrice
                    ? a.coinPrice.CompareTo(b.coinPrice)
                    : string.CompareOrdinal(a.speciesId, b.speciesId));
                s_all = list.ToArray();
            }
            return s_all;
        }
    }
}

// 게코 도감 · 업적 (2026-09-17 어덜트 이후 2단계)
public partial class RewardManager
{
    // ── 도감 ──────────────────────────────────────────────────
    // 종마다 도장 두 개: 만남(키워 본 적 있음) · 어덜트(어덜트까지 키움 = adultSpeciesIds)

    public const int BOOK_MEET_COIN     = 50;   // [TBD] 새 종을 처음 분양하면
    public const int BOOK_COMPLETE_GEM  = 20;   // [TBD] 모든 종을 어덜트까지 키우면 한 번

    public bool HasMet(string speciesId)
    {
        var p = _repo.GetPlayerData().progress;
        return p != null && p.unlockedSpeciesIds != null && p.unlockedSpeciesIds.Contains(speciesId);
    }

    public bool HasRaisedAdult(string speciesId)
    {
        var p = _repo.GetPlayerData().progress;
        return p != null && p.adultSpeciesIds != null && p.adultSpeciesIds.Contains(speciesId);
    }

    /// <summary>
    /// 이 종을 만났다고 기록. 처음이면 true, reward면 코인 BOOK_MEET_COIN (기본 게코는 보상 없이 기록만).
    /// 저장은 부르는 쪽에서 (분양 · 기본 게코 모두 이어서 저장한다)
    /// </summary>
    public static bool RecordMet(PlayerData data, string speciesId, bool reward)
    {
        if (data == null || string.IsNullOrEmpty(speciesId)) return false;
        data.progress ??= new ProgressData();
        data.progress.unlockedSpeciesIds ??= new List<string>();
        if (data.progress.unlockedSpeciesIds.Contains(speciesId)) return false;

        data.progress.unlockedSpeciesIds.Add(speciesId);
        if (reward) data.coin += BOOK_MEET_COIN;
        Debug.Log($"[RewardManager] 도감 — 새 종 {speciesId}{(reward ? $" (코인 +{BOOK_MEET_COIN})" : "")}");
        return true;
    }

    /// <summary>어덜트 도장을 찍은 종 수 / 전체 종 수</summary>
    public int BookAdults(IReadOnlyList<GeckoSpeciesSO> species, out int total)
    {
        total = 0;
        int n = 0;
        if (species == null) return 0;
        foreach (var s in species)
        {
            if (s == null) continue;
            total++;
            if (HasRaisedAdult(s.speciesId)) n++;
        }
        return n;
    }

    public bool BookComplete(IReadOnlyList<GeckoSpeciesSO> species)
    {
        int n = BookAdults(species, out int total);
        return total > 0 && n >= total;
    }

    public bool BookRewardClaimed
    {
        get
        {
            var p = _repo.GetPlayerData().progress;
            return p != null && p.bookRewardClaimed;
        }
    }

    public bool CanClaimBook(IReadOnlyList<GeckoSpeciesSO> species) => BookComplete(species) && !BookRewardClaimed;

    /// <summary>도감 완성 보상 — 받을 수 없으면 0</summary>
    public int ClaimBook(IReadOnlyList<GeckoSpeciesSO> species)
    {
        if (!CanClaimBook(species)) return 0;
        var data = _repo.GetPlayerData();
        data.gem += BOOK_COMPLETE_GEM;
        data.progress.bookRewardClaimed = true;
        _repo.Save();
        Debug.Log($"[RewardManager] 도감 완성 보상 — 젬 +{BOOK_COMPLETE_GEM}");
        return BOOK_COMPLETE_GEM;
    }

    // ── 업적 ──────────────────────────────────────────────────

    public static readonly AchievementDef[] ACHIEVEMENTS =
    {
        new AchievementDef("first_molt",   AchievementStat.Molts,    1,   50,  0),   // [TBD] 목표·보상
        new AchievementDef("molt_master",  AchievementStat.Molts,    20,  200, 0),
        new AchievementDef("first_adult",  AchievementStat.Adults,   1,   0,   3),
        new AchievementDef("gecko_family", AchievementStat.Adults,   3,   0,   10),
        new AchievementDef("gentle_hand",  AchievementStat.Pets,     100, 150, 0),
        new AchievementDef("good_meal",    AchievementStat.Feeds,    50,  150, 0),
        new AchievementDef("steady_care",  AchievementStat.GoalDays, 7,   0,   5),
        new AchievementDef("full_house",   AchievementStat.Geckos,   3,   100, 0),
        new AchievementDef("best_friend",  AchievementStat.BondLevel, GeckoBond.MAX_LEVEL, 0, 10),
        new AchievementDef("morph_collector", AchievementStat.Morphs, 6, 0, 10),
    };

    public static bool TryGetAchievement(string id, out AchievementDef def)
    {
        foreach (var a in ACHIEVEMENTS)
            if (a.id == id) { def = a; return true; }
        def = default;
        return false;
    }

    public int StatValue(AchievementStat stat)
    {
        var data = _repo.GetPlayerData();
        var p    = data.progress;
        switch (stat)
        {
            case AchievementStat.Molts:
                int molts = 0;
                foreach (var g in data.geckos) if (g != null) molts += g.moltCount;
                return molts;
            case AchievementStat.Adults:   return p != null ? p.adultCount : 0;
            case AchievementStat.Pets:     return p != null ? p.petCount   : 0;
            case AchievementStat.Feeds:    return p != null ? p.feedCount  : 0;
            case AchievementStat.GoalDays: return p != null ? p.goalDays   : 0;
            case AchievementStat.BondLevel:
                int best = 0;
                foreach (var g in data.geckos) if (g != null) best = Mathf.Max(best, GeckoBond.Level(g));
                return best;
            case AchievementStat.Morphs:   return p != null && p.morphIds != null ? p.morphIds.Count : 0;
            default:                       return data.geckos.Count;
        }
    }

    /// <summary>화면에 보여줄 진행 (목표를 넘지 않게)</summary>
    public int AchievementProgress(AchievementDef def) => Mathf.Min(StatValue(def.stat), def.target);

    public bool IsAchieved(AchievementDef def) => StatValue(def.stat) >= def.target;

    public bool IsClaimed(string id)
    {
        var p = _repo.GetPlayerData().progress;
        return p != null && p.achievements != null && p.achievements.Contains(id);
    }

    public bool CanClaimAchievement(AchievementDef def) => IsAchieved(def) && !IsClaimed(def.id);

    /// <summary>업적 보상 받기 — 받을 수 없으면 false</summary>
    public bool ClaimAchievement(string id, out int coin, out int gem)
    {
        coin = gem = 0;
        if (!TryGetAchievement(id, out var def) || !CanClaimAchievement(def)) return false;

        var data = _repo.GetPlayerData();
        data.progress.achievements ??= new List<string>();
        data.progress.achievements.Add(id);
        data.coin += def.coin;
        data.gem  += def.gem;
        coin = def.coin;
        gem  = def.gem;
        _repo.Save();
        Debug.Log($"[RewardManager] 업적 보상 — {id}: 코인 +{coin} 젬 +{gem}");
        return true;
    }

    /// <summary>받을 수 있는 보상 수 (업적 + 도감 완성) — 게코 목록 "도감" 버튼 숫자</summary>
    public int ClaimableCount(IReadOnlyList<GeckoSpeciesSO> species)
    {
        int n = CanClaimBook(species) ? 1 : 0;
        foreach (var a in ACHIEVEMENTS) if (CanClaimAchievement(a)) n++;
        return n;
    }

    // 이번 실행에서 이미 알린 업적 (저장하지 않는다 — 다시 켜면 받지 않은 업적을 한 번 더 알린다)
    private readonly HashSet<string> _announced = new HashSet<string>();

    /// <summary>새로 달성했지만 아직 알리지 않은 업적 (홈 알림). 한 번 돌려준 것은 다시 돌려주지 않는다</summary>
    public List<AchievementDef> TakeNewlyAchieved()
    {
        List<AchievementDef> list = null;
        foreach (var a in ACHIEVEMENTS)
        {
            if (_announced.Contains(a.id) || !CanClaimAchievement(a)) continue;
            _announced.Add(a.id);
            (list ??= new List<AchievementDef>()).Add(a);
        }
        return list;
    }

    // 쓰다듬기·먹이 수 (RecordCare가 부른다 — 저장은 RecordCare에서)
    private void CountCareForAchievements(CareKind kind)
    {
        var data = _repo.GetPlayerData();
        data.progress ??= new ProgressData();
        if (kind == CareKind.Pet)  data.progress.petCount++;
        if (kind == CareKind.Feed) data.progress.feedCount++;
    }
}
