using System;
using UnityEngine;

public partial class RewardManager   // 도감·업적은 RewardManager.Collection.cs
{
    private readonly PlayerRepository _repo;

    // 7일 순환 보상 테이블 (coin, gem)
    private static readonly (int coin, int gem)[] REWARD_TABLE =
    {
        (50,  0),  // day 1
        (75,  0),  // day 2
        (100, 0),  // day 3
        (125, 0),  // day 4
        (150, 0),  // day 5
        (200, 0),  // day 6
        (250, 3),  // day 7 — 젬 보너스
    };

    public RewardManager(PlayerRepository repo)
    {
        _repo = repo;
    }

    // ── 공개 API ──────────────────────────────────────────────

    /// <summary>
    /// 하루가 바뀌는 시각을 이 기기 시간으로 (보상 팝업 안내용, "09:00").
    /// 기준은 UTC 자정이다 (2026-09-21 결정) — 현지 자정으로 하면 시간대를 바꿔 하루치를 여러 번 받을 수 있다.
    /// 그래서 "언제 바뀌는지"를 화면에 알려 준다. 한국은 오전 9시, 인도처럼 30분 시차인 곳은 05:30으로 나온다.
    /// </summary>
    public static string LocalResetTimeText()
    {
        var utcMidnight = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);
        return utcMidnight.ToLocalTime().ToString("HH:mm");
    }

    /// <summary>오늘 아직 보상을 받지 않았으면 true.</summary>
    public bool CanClaim()
    {
        var data = _repo.GetPlayerData().dailyReward;
        if (data.lastClaimedTicks == 0) return true;

        var lastClaimed = new DateTime(data.lastClaimedTicks, DateTimeKind.Utc);
        return DateTime.UtcNow.Date > lastClaimed.Date;
    }

    /// <summary>
    /// 화면에 보여줄 연속 일수. 오늘 아직 안 받았으면 받으면 될 일수, 이미 받았으면 오늘 받은 일수.
    /// (예전에는 받기 전에 저장된 어제 일수를 보여줘서 "연속 1일"인데 2일째 보상이 뜨는 식으로 어긋났다)
    /// </summary>
    public int GetStreak()
    {
        var data = _repo.GetPlayerData().dailyReward;
        return CanClaim() ? CalcNextStreak(data) : Mathf.Max(1, data.streakDays);
    }

    /// <summary>GetStreak()일째 보상 — 받기 전이면 받을 보상, 받은 뒤면 오늘 받은 보상.</summary>
    public (int coin, int gem) PeekReward() => RewardOf(GetStreak());

    /// <summary>
    /// 일일 보상 지급. CanClaim()이 false이면 아무 일도 하지 않고 (0,0) 반환.
    /// </summary>
    public (int coin, int gem) ClaimReward()
    {
        if (!CanClaim())
        {
            Debug.LogWarning("[RewardManager] 오늘 이미 보상을 받았습니다.");
            return (0, 0);
        }

        var playerData = _repo.GetPlayerData();
        var reward     = playerData.dailyReward;

        int nextStreak         = CalcNextStreak(reward);
        reward.streakDays      = nextStreak;
        reward.lastClaimedTicks = DateTime.UtcNow.Ticks;

        var (coin, gem) = RewardOf(nextStreak);
        playerData.coin += coin;
        playerData.gem  += gem;

        _repo.Save();

        Debug.Log($"[RewardManager] 일일 보상 지급 — 연속 {nextStreak}일 / 코인 +{coin} 젬 +{gem}");
        return (coin, gem);
    }

    // ── 오늘의 돌봄 목표 ──────────────────────────────────────
    // 하루(UTC 날짜 — 일일 보상과 같은 기준)마다 먹이·물·쓰다듬기·청소를 채우면 코인. 매일 들어올 이유를 만든다.

    public const int GOAL_REWARD_COIN = 100;   // [TBD] 일일 보상(50~250) 사이

    /// <summary>목표 진행이 올랐다 — (종류, 지금, 목표, 전부 채움). 목표를 넘긴 돌봄은 알리지 않는다</summary>
    public event Action<CareKind, int, int, bool> OnGoalProgress;

    public static int GoalTarget(CareKind kind)
    {
        switch (kind)
        {
            case CareKind.Feed:  return 2;   // [TBD]
            case CareKind.Water: return 2;   // [TBD]
            case CareKind.Pet:   return 3;   // [TBD]
            default:             return 1;   // [TBD] 청소
        }
    }

    public int GoalCount(CareKind kind)
    {
        var d = TodayGoal();
        switch (kind)
        {
            case CareKind.Feed:  return d.fed;
            case CareKind.Water: return d.watered;
            case CareKind.Pet:   return d.petted;
            default:             return d.cleaned;
        }
    }

    public bool GoalsComplete
    {
        get
        {
            foreach (CareKind kind in Enum.GetValues(typeof(CareKind)))
                if (GoalCount(kind) < GoalTarget(kind)) return false;
            return true;
        }
    }

    public bool GoalsClaimed    => TodayGoal().claimed;
    public bool CanClaimGoals() => GoalsComplete && !GoalsClaimed;

    /// <summary>돌봄 한 번을 센다 (GeckoManager.OnCareDone). 목표를 이미 채운 종류는 더 세지 않는다</summary>
    public void RecordCare(CareKind kind)
    {
        CountCareForAchievements(kind);   // 업적은 목표를 넘긴 돌봄도 센다 (RewardManager.Collection)

        var d = TodayGoal();
        int now = GoalCount(kind), target = GoalTarget(kind);
        if (now >= target)
        {
            if (kind == CareKind.Feed || kind == CareKind.Pet) _repo.Save();   // 업적 숫자만 올랐다
            return;
        }

        now++;
        switch (kind)
        {
            case CareKind.Feed:  d.fed     = now; break;
            case CareKind.Water: d.watered = now; break;
            case CareKind.Pet:   d.petted  = now; break;
            default:             d.cleaned = now; break;
        }
        _repo.Save();
        OnGoalProgress?.Invoke(kind, now, target, GoalsComplete);
    }

    /// <summary>오늘의 돌봄 보상 지급. 다 채우지 않았거나 이미 받았으면 0</summary>
    public int ClaimGoals()
    {
        if (!CanClaimGoals()) return 0;

        var data = _repo.GetPlayerData();
        data.coin += GOAL_REWARD_COIN;
        data.dailyGoal.claimed = true;
        data.progress ??= new ProgressData();
        data.progress.goalDays++;   // 업적 "꾸준한 돌봄"
        _repo.Save();

        Debug.Log($"[RewardManager] 오늘의 돌봄 보상 — 코인 +{GOAL_REWARD_COIN}");
        return GOAL_REWARD_COIN;
    }

    // ── 어덜트의 선물 ──────────────────────────────────────────
    // 잘 지내는 어덜트가 하루 한 번 홈 바닥에 선물을 남긴다 — 다 키운 게코를 계속 돌볼 이유.
    // 게코마다 따로라 다른 게코도 보러 가게 된다 (게코 목록 "선물이 있어요").

    public const float    GIFT_MIN_STAT    = 50f;    // [TBD] 배고픔·목마름·청결·기분·건강이 모두 이보다 높아야
    public const int      GIFT_COIN_MIN    = 20;     // [TBD]
    public const int      GIFT_COIN_MAX    = 40;     // [TBD] (포함) — 5마리면 하루 최대 200
    public const float    GIFT_FOOD_CHANCE = 0.2f;   // [TBD] 먹이 1개가 함께 나올 확률
    public static readonly string[] GIFT_FOODS = { "cricket_small", "mealworm" };

    public struct Gift
    {
        public int    coin;
        public string foodId;   // 없으면 null
    }

    /// <summary>오늘 날짜 번호 (UTC — 일일 보상·돌봄 목표와 같은 기준)</summary>
    public static int TodayNumber() => (int)(DateTime.UtcNow.Date.Ticks / TimeSpan.TicksPerDay);

    public static bool CanGift(GeckoData g) => CanGift(g, TodayNumber());

    public static bool CanGift(GeckoData g, int today)
    {
        if (!GeckoManager.IsAdult(g) || g.giftDay == today) return false;
        return g.hunger > GIFT_MIN_STAT && g.thirst > GIFT_MIN_STAT && g.cleanliness > GIFT_MIN_STAT
            && g.mood   > GIFT_MIN_STAT && g.health > GIFT_MIN_STAT;
    }

    /// <summary>선물 받기 — 코인과 가끔 먹이. 받을 수 없으면 false (아무 변화 없음)</summary>
    public bool ClaimGift(string geckoId, System.Random rng, out Gift gift)
    {
        gift = default;
        var g = _repo.GetGecko(geckoId);
        if (g == null || !CanGift(g)) return false;

        rng ??= new System.Random();
        gift.coin = rng.Next(GIFT_COIN_MIN, GIFT_COIN_MAX + 1);
        if (rng.NextDouble() < GIFT_FOOD_CHANCE)
            gift.foodId = GIFT_FOODS[rng.Next(GIFT_FOODS.Length)];

        var data = _repo.GetPlayerData();
        data.coin += gift.coin;
        if (gift.foodId != null) _repo.AddItem(gift.foodId, 1);
        g.giftDay = TodayNumber();
        _repo.Save();

        Debug.Log($"[RewardManager] 어덜트의 선물 — {g.name}: 코인 +{gift.coin}{(gift.foodId != null ? " + " + gift.foodId : "")}");
        return true;
    }

    // 오늘 목표 — 날짜가 바뀌었으면 새로 시작 (저장은 진행이 오를 때)
    private DailyGoalData TodayGoal()
    {
        var data  = _repo.GetPlayerData();
        int today = TodayNumber();
        if (data.dailyGoal == null || data.dailyGoal.day != today)
            data.dailyGoal = new DailyGoalData { day = today };
        return data.dailyGoal;
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────

    /// <summary>streak일째 보상 (7일마다 순환)</summary>
    private static (int coin, int gem) RewardOf(int streak)
        => REWARD_TABLE[(Mathf.Max(1, streak) - 1) % REWARD_TABLE.Length];

    /// <summary>
    /// 다음 클레임 시 적용될 streak 계산.
    /// 어제 받았으면 +1, 더 오래됐으면 1로 리셋.
    /// </summary>
    private static int CalcNextStreak(DailyRewardData data)
    {
        if (data.lastClaimedTicks == 0) return 1;

        var lastClaimed = new DateTime(data.lastClaimedTicks, DateTimeKind.Utc).Date;
        var yesterday   = DateTime.UtcNow.Date.AddDays(-1);

        return lastClaimed == yesterday ? data.streakDays + 1 : 1;
    }
}
