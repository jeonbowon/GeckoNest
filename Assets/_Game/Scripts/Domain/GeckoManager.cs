using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>돌봄 행동의 결과. UI는 이 값으로 게코 반응(동작·말풍선·효과음)을 고른다.</summary>
public enum CareResult
{
    Done,       // 정상 반영
    Refused,    // 이미 충분함 (배부름 · 목 안 마름 · 이미 깨끗함) — 수치·아이템 변화 없음
    Annoyed,    // 너무 자주 쓰다듬음 — 애정도는 오르지 않고 기분이 살짝 내려감
    Failed,     // 게코 없음 · 재고 없음
}

/// <summary>게코 목록에 보여줄 돌봄 필요 표시 (GeckoManager.AlertOf)</summary>
public enum GeckoAlert
{
    None,      // 잘 지냄
    Dirty,     // 청결 20 이하
    Thirsty,   // 목마름 30 이하
    Hungry,    // 배고픔 30 이하
    Sick,      // 건강 20 이하
}

/// <summary>먹이 한 번의 실제 효과 (상한에 걸려 덜 오른 만큼은 빠진 값). UI가 반응과 말풍선을 고르는 데 쓴다.</summary>
public struct FeedEffect
{
    public bool  favorite;    // 이 종이 좋아하는 먹이
    public float hunger;
    public float mood;        // 먹이 기분 + 좋아하는 먹이 보너스
    public float health;
    public float growthExp;
    public float moltBonus;   // 이번에 쌓인 허물 성공률 (0.1 = 10%)
    public float affection;
}

/// <summary>
/// 다음 성장 단계 조건과 지금 값. 성장 판정(EvaluateGrowth)과 화면 표시(성장 단계 글자 누르기)가 같은 계산을 쓴다.
/// need* 가 0이면 그 단계에는 해당 조건이 없다.
/// </summary>
public struct GrowthCheck
{
    public int   nextStage;                // -1 = 다 자람
    public float ageDays,   needDays;      // 나이는 먹이 성장치 반영 (EffectiveAgeDays)
    public int   moltCount, needMolts;
    public float health,    needHealth;
    public float affection, needAffection;

    public bool IsAdult      => nextStage < 0;
    public bool DaysMet      => ageDays   >= needDays;
    public bool MoltsMet     => moltCount >= needMolts;
    public bool HealthMet    => health    >= needHealth;
    public bool AffectionMet => affection >= needAffection;
    public bool AllMet       => !IsAdult && DaysMet && MoltsMet && HealthMet && AffectionMet;
}

public class GeckoManager
{
    // 상태 감소율 ([TBD] — 수치 조정 시 여기서만 변경)
    private const float HUNGER_DECAY      = 4f;    // [TBD] /h
    private const float THIRST_DECAY      = 5f;    // [TBD] /h
    private const float CLEAN_DECAY       = 0.67f; // /h
    private const float MOOD_DECAY        = 1f;    // [TBD] /h
    private const float HEALTH_DECAY      = 1f;    // hunger/thirst 0일 때 /h
    private const float HEALTH_REGEN      = 0.5f;  // [TBD] /h — 배고픔·목마름이 둘 다 넉넉한 동안
    private const float HEALTH_REGEN_CARE = 50f;   // [TBD] 배고픔·목마름이 둘 다 이 값보다 높아야 회복
    public  const float CLEAN_MOOD_THRESHOLD = 20f;   // 청결이 이 아래면 기분이 더 줄어든다
    private const float CLEAN_MOOD_PENALTY   = 0.5f;  // [TBD] /h

    private const float WATER_RESTORE     = 40f;   // [TBD]
    private const float PET_MOOD_BONUS    = 5f;    // [TBD]
    private const float PET_AFFECTION     = 3f;
    private const float FEED_AFFECTION    = 2f;
    private const float WATER_AFFECTION   = 1f;
    private const float CLEAN_RESTORE     = 60f;   // [TBD]
    private const float CLEAN_AFFECTION   = 1f;

    // 먹이 — 좋은 먹이일수록 이득 (먹이별 수치는 ItemSO 에셋)
    public  const float FAVORITE_MOOD_BONUS     = 3f;     // [TBD] 좋아하는 먹이 기분 추가
    private const float FAVORITE_AFFECTION_MULT = 2f;     // [TBD] 좋아하는 먹이 애정도 배수
    private const float MAX_FOOD_MOLT_BONUS     = 0.15f;  // [TBD] 먹이로 쌓을 수 있는 허물 성공률 상한
    private const float GROWTH_EXP_HOURS        = 3f;     // [TBD] 성장치 1 = 성장 일수 3시간 앞당김
    private const float MAX_GROWTH_SPEEDUP      = 0.30f;  // [TBD] 먹이로 줄일 수 있는 성장 기간 비율 상한 (30%)

    // 돌봄 제한 — 연타로 수치가 의미 없어지는 것을 막고, 게코의 반응 자체를 재미로 만든다
    private const float CARE_FULL_THRESHOLD = 95f;    // [TBD] 이 이상이면 먹이·물·청소를 거절
    // 연달아 쓰다듬어도 좋아하는 횟수는 GeckoBond.PetLimit (기본 4, 유대 Lv.2부터 6) [TBD]
    private const float PET_FATIGUE_RECOVER = 0.125f; // [TBD] 초당 회복량 (8초에 1회분)
    private const float PET_ANNOY_MOOD      = 2f;     // [TBD] 귀찮게 했을 때 기분 하락

    private const float MOLT_BASE_RATE    = 0.70f; // [TBD]
    private const float MOLT_THIRST_BONUS = 0.15f;
    private const float MOLT_HEALTH_BONUS = 0.10f;
    private const float MOLT_FAIL_RESET   = 30f;   // 실패 시 moltProgress 리셋값
    private const float MOLT_EXP_BONUS    = 20f;   // [TBD]

    // 허물 진행 — 첫 허물은 첫날 안에, 이후는 3일 주기 (2026-09-17 A안: 거의 매일~3일마다 성장·허물 사건)
    private const float FIRST_MOLT_PROGRESS_PER_HOUR = 100f / 12f; // [TBD] 12시간에 첫 허물
    private const float MOLT_PROGRESS_PER_HOUR       = 100f / 72f; // [TBD] 3일에 100% 달성

    // 성장 단계 조건 (실제 경과 일수 — 먹이 성장치로 최대 30% 앞당겨진다). 어덜트까지 약 2주
    // 허물 시점 0.5 · 3.5 · 6.5 · 9.5일 → 주버나일(3일)에 1회, 서브어덜트(7일)에 2회, 어덜트(14일)에 3회가 딱 채워진다
    private const float GROWTH_DAYS_0_TO_1          = 1f;   // [TBD] 해츨링 → 베이비
    private const float GROWTH_DAYS_1_TO_2          = 3f;   // [TBD] 베이비 → 주버나일
    private const float GROWTH_DAYS_2_TO_3          = 7f;   // [TBD] 주버나일 → 서브어덜트
    private const float GROWTH_DAYS_3_TO_4          = 14f;  // [TBD] 서브어덜트 → 어덜트
    private const float GROWTH_DAYS_NATURAL_DEATH   = 900f; // 자연사 (실제 날짜)

    private const int   GROWTH_MOLT_REQ_1_TO_2      = 1;    // [TBD]
    private const int   GROWTH_MOLT_REQ_2_TO_3      = 2;    // [TBD]
    private const int   GROWTH_MOLT_REQ_3_TO_4      = 3;    // [TBD]
    private const float GROWTH_HEALTH_REQ_2_TO_3    = 50f;
    private const float GROWTH_AFFECTION_REQ_3_TO_4 = 60f;

    private readonly PlayerRepository _repo;
    private readonly TimeManager      _time;

    // 쓰다듬기 피로도 — 런타임 전용, 저장하지 않는다 (앱을 다시 켜면 초기화돼도 문제없음)
    private struct PetFatigue
    {
        public float  amount;
        public double lastSeconds;
    }
    private readonly Dictionary<string, PetFatigue> _petFatigue = new Dictionary<string, PetFatigue>();

    // UI 통지용 이벤트
    public event Action<GeckoData> OnStateChanged;
    public event Action<GeckoData> OnMoltSuccess;
    public event Action<GeckoData> OnMoltFail;
    public event Action<GeckoData> OnGrowthUp;

    /// <summary>돌봄이 실제로 이루어졌다 (거절·삐짐 제외) — 오늘의 돌봄 목표가 센다 (AppBootstrap이 RewardManager에 연결)</summary>
    public event Action<CareKind>  OnCareDone;

    public GeckoManager(PlayerRepository repo, TimeManager time)
    {
        _repo = repo;
        _time = time;
    }

    // ── 먹이 ──────────────────────────────────────────────────

    public CareResult FeedGecko(string id, ItemSO item) => FeedGecko(id, item, out _);

    public CareResult FeedGecko(string id, ItemSO item, out FeedEffect effect)
    {
        effect = default;
        var g = _repo.GetGecko(id);
        if (g == null || item == null) return CareResult.Failed;

        // 배부르면 먹지 않는다 — 아이템도 차감하지 않는다 (배를 채우지 않는 영양제는 예외)
        if (item.hungerRestore > 0f && g.hunger >= CARE_FULL_THRESHOLD)
            return CareResult.Refused;

        // 다 자란 게코에게 성장만 주는 먹이(성장촉진제)는 쓸모없다 — 거절하고 차감하지 않는다
        if (IsUselessFood(g, item))
            return CareResult.Refused;

        // 인벤토리에서 1개 차감 — 수량 부족이면 중단 (이중 클릭 방어)
        if (!_repo.RemoveItem(item.itemId, 1))
        {
            Debug.LogWarning($"[GeckoManager] FeedGecko — {item.itemId} 재고 없음");
            return CareResult.Failed;
        }

        bool  favorite  = IsFavoriteFood(g, item);
        float growthExp = IsAdult(g) ? 0f : item.growthExpGain;   // 다 자라면 성장치를 쌓지 않는다 (말풍선에도 안 나온다)
        float hunger0   = g.hunger, mood0 = g.mood, health0 = g.health, affection0 = g.affection, molt0 = g.moltBonus;

        g.hunger    = Mathf.Min(100f, g.hunger    + item.hungerRestore);
        g.mood      = Mathf.Min(100f, g.mood      + item.moodBonus + (favorite ? FAVORITE_MOOD_BONUS : 0f));
        g.health    = Mathf.Min(100f, g.health    + item.healthRestore);
        g.growthExp += growthExp;
        AddAffection(g, FEED_AFFECTION * (favorite ? FAVORITE_AFFECTION_MULT : 1f));
        g.moltBonus = Mathf.Min(MAX_FOOD_MOLT_BONUS, g.moltBonus + item.moltBonus);

        effect = new FeedEffect
        {
            favorite  = favorite,
            hunger    = g.hunger    - hunger0,
            mood      = g.mood      - mood0,
            health    = g.health    - health0,
            growthExp = growthExp,
            moltBonus = g.moltBonus - molt0,
            affection = g.affection - affection0,
        };

        _repo.GetPlayerData().lastFoodItemId = item.itemId;
        _repo.UpdateGecko(g);
        EvaluateGrowth(id);
        _repo.Save();
        Debug.Log($"[GeckoManager] FeedGecko — {g.name} {item.itemId}{(favorite ? " (좋아함)" : "")} hunger: {g.hunger:F1} 성장치: {g.growthExp:F0} 허물보너스: {g.moltBonus:P0} (남은 {_repo.GetItemCount(item.itemId)})");
        OnStateChanged?.Invoke(g);
        OnCareDone?.Invoke(CareKind.Feed);
        return CareResult.Done;
    }

    /// <summary>이 게코의 종이 좋아하는 먹이인가 (ItemSO.preferredSpeciesIds)</summary>
    public static bool IsFavoriteFood(GeckoData g, ItemSO item)
    {
        if (g == null || item == null || item.preferredSpeciesIds == null) return false;
        return Array.IndexOf(item.preferredSpeciesIds, g.speciesId) >= 0;
    }

    // ── 어덜트 (마지막 단계) ──────────────────────────────────

    public const int ADULT_STAGE       = 4;
    public const int ADULT_REWARD_COIN = 500;   // [TBD] 가고일 분양가와 같게 — 다 키우면 바로 새 친구를 들일 수 있게
    public const int ADULT_REWARD_GEM  = 5;     // [TBD] 성장촉진제 1개 값
    public const int ADULT_REWARD_REPEAT_COIN = 100;   // [TBD] 같은 종을 또 키웠을 때 — 무한 코인 방지

    public static bool IsAdult(GeckoData g) => g != null && g.growthStage >= ADULT_STAGE;

    /// <summary>
    /// 어덜트 달성 보상. 종마다 처음 키운 어덜트만 크게(코인 500 · 젬 5), 같은 종 두 번째부터는 코인 100.
    /// 받은 종은 `ProgressData.adultSpeciesIds`에 남는다 (EvaluateGrowth).
    /// </summary>
    public static void AdultReward(ProgressData progress, string speciesId, out int coin, out int gem)
    {
        bool first = progress == null || progress.adultSpeciesIds == null
                     || !progress.adultSpeciesIds.Contains(speciesId);
        coin = first ? ADULT_REWARD_COIN : ADULT_REWARD_REPEAT_COIN;
        gem  = first ? ADULT_REWARD_GEM  : 0;
    }

    /// <summary>마지막 어덜트 달성 때 실제로 준 보상 — OnGrowthUp 직전에 채운다 (사건 대기열이 복사해 간다)</summary>
    public int LastAdultRewardCoin { get; private set; }
    public int LastAdultRewardGem  { get; private set; }
    public int LastAdultsRaised    { get; private set; }   // 그때까지 키운 어덜트 수 (새로 열린 장식 알림)
    public GeckoMorph.Reveal LastMorph { get; private set; }   // 어덜트가 될 때 드러난 모프 (OnMorphRevealed)

    /// <summary>어덜트가 되어 모프가 드러났다 — OnGrowthUp 바로 뒤. 사건 대기열이 연출한다</summary>
    public event Action<GeckoData> OnMorphRevealed;

    private readonly System.Random _morphRng = new System.Random();

    // ── 돌봄 필요 표시 (게코 목록) ─────────────────────────────

    public const float ALERT_HEALTH = 20f;   // [TBD] 상태 표의 위험 기준과 같게
    public const float ALERT_HUNGER = 30f;
    public const float ALERT_THIRST = 30f;
    public const float ALERT_CLEAN  = 20f;

    /// <summary>가장 급한 것 하나 — 아픔 → 배고픔 → 목마름 → 청소</summary>
    public static GeckoAlert AlertOf(GeckoData g)
    {
        if (g == null)                    return GeckoAlert.None;
        if (g.health      <= ALERT_HEALTH) return GeckoAlert.Sick;
        if (g.hunger      <= ALERT_HUNGER) return GeckoAlert.Hungry;
        if (g.thirst      <= ALERT_THIRST) return GeckoAlert.Thirsty;
        if (g.cleanliness <= ALERT_CLEAN)  return GeckoAlert.Dirty;
        return GeckoAlert.None;
    }

    /// <summary>
    /// 가장 먼저 돌봄이 필요한 게코 (배고픔·목마름이 threshold까지 떨어지는 시간이 가장 짧은).
    /// 알림 예약이 모든 게코를 보게 한다. 게코가 없으면 null.
    /// </summary>
    public static GeckoData MostUrgent(IList<GeckoData> geckos, float threshold, out float hours)
    {
        GeckoData best = null;
        hours = float.MaxValue;
        if (geckos == null) return null;
        foreach (var g in geckos)
        {
            if (g == null) continue;
            float h = HoursUntilCareNeeded(g, threshold);
            if (h < hours) { hours = h; best = g; }
        }
        if (best == null) hours = 0f;
        return best;
    }

    /// <summary>
    /// 다 자란 게코에게 쓸모없는 먹이 — 성장치 말고 다른 효과가 하나도 없다 (지금은 성장촉진제).
    /// 선반은 "필요 없음"으로 보여주고, 주면 거절한다 (재고 그대로).
    /// </summary>
    public static bool IsUselessFood(GeckoData g, ItemSO item)
        => IsAdult(g) && item != null && item.growthExpGain > 0f
           && item.hungerRestore <= 0f && item.thirstRestore <= 0f && item.moodBonus <= 0f
           && item.healthRestore <= 0f && item.moltBonus <= 0f;

    // ── 물 ────────────────────────────────────────────────────

    public CareResult GiveWater(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null) return CareResult.Failed;
        if (g.thirst >= CARE_FULL_THRESHOLD) return CareResult.Refused;

        g.thirst    = Mathf.Min(100f, g.thirst    + WATER_RESTORE);
        AddAffection(g, WATER_AFFECTION);

        _repo.UpdateGecko(g);
        _repo.Save();
        OnStateChanged?.Invoke(g);
        OnCareDone?.Invoke(CareKind.Water);
        return CareResult.Done;
    }

    // ── 쓰다듬기 ──────────────────────────────────────────────

    public CareResult Pet(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null) return CareResult.Failed;

        if (!RegisterPet(id, GeckoBond.PetLimit(g)))   // 유대 Lv.2부터 더 오래 좋아한다
        {
            g.mood = Mathf.Max(0f, g.mood - PET_ANNOY_MOOD);
            _repo.UpdateGecko(g);
            _repo.Save();
            OnStateChanged?.Invoke(g);
            return CareResult.Annoyed;
        }

        g.mood      = Mathf.Min(100f, g.mood      + PET_MOOD_BONUS);
        AddAffection(g, PET_AFFECTION);

        _repo.UpdateGecko(g);
        _repo.Save();
        OnStateChanged?.Invoke(g);
        OnCareDone?.Invoke(CareKind.Pet);
        return CareResult.Done;
    }

    // ── 유대 (GeckoBond) ─────────────────────────────────────

    /// <summary>유대 레벨이 올랐다 — 보상은 이미 들어간 뒤 (LastBond*에 레벨·보상). 사건 대기열이 연출한다</summary>
    public event Action<GeckoData> OnBondLevelUp;

    public int LastBondLevel { get; private set; }
    public int LastBondCoin  { get; private set; }
    public int LastBondGem   { get; private set; }

#if UNITY_EDITOR
    /// <summary>에디터 전용 — 테스트 메뉴가 값을 직접 바꾼 뒤 화면을 갱신시킨다 (GameManager.DebugAddBond)</summary>
    public void DebugNotifyChanged(GeckoData g) => OnStateChanged?.Invoke(g);
#endif

    // 돌봄의 애정도 — 100까지는 애정도, 넘는 몫은 유대(하루 한도). 레벨이 오르면 보상
    private void AddAffection(GeckoData g, float amount)
    {
        GeckoBond.AddAffection(g, amount, RewardManager.TodayNumber());
        CheckBondLevel(g);
    }

    /// <summary>유대 레벨이 보상받은 레벨보다 높으면 그 사이 보상을 모두 주고 알린다. 저장은 부르는 쪽에서</summary>
    public void CheckBondLevel(GeckoData g)
    {
        if (g == null) return;
        int level = GeckoBond.Level(g);
        if (level <= g.bondRewardedLevel) return;

        int coin = 0, gem = 0;
        for (int lv = Mathf.Max(1, g.bondRewardedLevel + 1); lv <= level; lv++)
        {
            coin += GeckoBond.LEVEL_REWARDS[lv].coin;
            gem  += GeckoBond.LEVEL_REWARDS[lv].gem;
        }
        var data = _repo.GetPlayerData();
        data.coin += coin;
        data.gem  += gem;
        g.bondRewardedLevel = level;

        LastBondLevel = level;
        LastBondCoin  = coin;
        LastBondGem   = gem;
        Debug.Log($"[GeckoManager] 유대 레벨 — {g.name}: Lv.{level} (점수 {GeckoBond.Points(g):F0}) 코인 +{coin} 젬 +{gem}");
        OnBondLevelUp?.Invoke(g);
    }

    /// <summary>쓰다듬기 피로도를 쌓는다. 한도 안이면 true (좋아함), 넘으면 false (귀찮아함).</summary>
    private bool RegisterPet(string id, float limit)
    {
        double now = TimeSpan.FromTicks(_time.GetNowTicks()).TotalSeconds;
        _petFatigue.TryGetValue(id, out var f);

        float recovered = (float)Math.Max(0.0, now - f.lastSeconds) * PET_FATIGUE_RECOVER;
        f.amount      = Mathf.Max(0f, f.amount - recovered);
        f.lastSeconds = now;

        // 탭 사이에도 조금씩 회복되므로 (예: 3.97) 반 칸 여유를 두고 판정한다 → 연달아 정확히 LIMIT번까지 좋아함
        bool ok = f.amount < limit - 0.5f;
        f.amount = Mathf.Min(f.amount + 1f, limit + 2f);   // 계속 연타하면 조금 더 오래 삐친다
        _petFatigue[id] = f;
        return ok;
    }

    // ── 청소 ──────────────────────────────────────────────────

    public CareResult Clean(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null) return CareResult.Failed;
        if (g.cleanliness >= CARE_FULL_THRESHOLD) return CareResult.Refused;

        g.cleanliness = Mathf.Min(100f, g.cleanliness + CLEAN_RESTORE);
        AddAffection(g, CLEAN_AFFECTION);

        _repo.UpdateGecko(g);
        _repo.Save();
        OnStateChanged?.Invoke(g);
        OnCareDone?.Invoke(CareKind.Clean);
        return CareResult.Done;
    }

    // ── 시간 경과 보정 ─────────────────────────────────────────

    /// <summary>
    /// 모든 게코에 마지막 갱신 이후 흐른 시간을 반영한다.
    /// 앱 시작 · 백그라운드 복귀 · 실행 중 주기 호출을 모두 이것 하나로 처리한다. 저장은 호출한 쪽에서.
    /// </summary>
    public void ApplyElapsedProgressAll()
    {
        // 처리 중 이벤트 구독자가 목록을 바꿔도 안전하게 복사본으로 돈다
        var geckos = _repo.GetPlayerData().geckos.ToArray();
        foreach (var g in geckos)
            ApplyElapsedProgress(g.id);
    }

    public void ApplyElapsedProgress(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null) return;

        // 0 이하 = 기기 시계를 과거로 돌림 → 시계가 따라올 때까지 진행하지 않는다
        float hours = _time.GetElapsedHours(g.lastUpdatedTicks);
        if (hours <= 0f) return;

        ApplyOfflineProgress(id, hours);
    }

    public void ApplyOfflineProgress(string id, float elapsedHours)
    {
        var g = _repo.GetGecko(id);
        if (g == null || elapsedHours <= 0f) return;

        float h = _time.ClampOfflineProgress(elapsedHours);

        // 구간 안에서 "언제부터"인지 미리 잰다 — 모두 줄어들기 전 값 기준
        float regenHours  = Mathf.Min(h, HoursUntilCareNeeded(g, HEALTH_REGEN_CARE));  // 배고픔·목마름이 둘 다 50을 넘는 동안
        float starveStart = HoursUntilCareNeeded(g, 0f);                               // 둘 중 먼저 0이 되는 시각
        float dirtyStart  = Mathf.Max(0f, (g.cleanliness - CLEAN_MOOD_THRESHOLD) / CLEAN_DECAY);

        g.hunger      = Mathf.Max(0f, g.hunger      - HUNGER_DECAY * h);
        g.thirst      = Mathf.Max(0f, g.thirst      - THIRST_DECAY * h);
        g.cleanliness = Mathf.Max(0f, g.cleanliness - CLEAN_DECAY  * h);
        g.mood        = Mathf.Max(0f, g.mood        - MOOD_DECAY   * h);

        if (regenHours > 0f)
            g.health = Mathf.Min(100f, g.health + HEALTH_REGEN * regenHours);

        // 배고픔·목마름이 0이 된 **뒤부터** 건강이 준다
        // (예전에는 구간 끝에 0이면 경과 시간 전체를 뺐다 — 80에서 24시간 방치 시 -8이어야 할 것이 -24)
        float starveHours = Mathf.Max(0f, h - starveStart);
        if (starveHours > 0f)
            g.health = Mathf.Max(0f, g.health - HEALTH_DECAY * starveHours);

        // 청결이 20 이하가 된 뒤부터 기분에 추가 패널티
        float dirtyHours = Mathf.Max(0f, h - dirtyStart);
        if (dirtyHours > 0f)
            g.mood = Mathf.Max(0f, g.mood - CLEAN_MOOD_PENALTY * dirtyHours);

        // 허물 진행도 누적 (상한 100f)
        g.moltProgress = Mathf.Min(100f, g.moltProgress + MoltRatePerHour(g) * h);

        g.lastUpdatedTicks = _time.GetNowTicks();

        _repo.UpdateGecko(g);
        OnStateChanged?.Invoke(g);

        // 허물 자동 판정 — 100 도달 시
        if (g.moltProgress >= 100f)
            TryMolt(id);

        // 성장 자동 판정
        EvaluateGrowth(id);
    }

    private static float MoltRatePerHour(GeckoData g)
        => g.moltCount == 0 ? FIRST_MOLT_PROGRESS_PER_HOUR : MOLT_PROGRESS_PER_HOUR;

    /// <summary>
    /// 배고픔·목마름 중 먼저 threshold까지 떨어지는 데 걸리는 시간(시간). 이미 그 아래면 0.
    /// 알림 예약(`NotificationScheduler`)이 "언제 돌봐야 하는지" 계산할 때 쓴다.
    /// </summary>
    public static float HoursUntilCareNeeded(GeckoData g, float threshold)
    {
        if (g == null) return 0f;
        float hunger = (g.hunger - threshold) / HUNGER_DECAY;
        float thirst = (g.thirst - threshold) / THIRST_DECAY;
        return Mathf.Max(0f, Mathf.Min(hunger, thirst));
    }

    /// <summary>목마름이 배고픔보다 먼저(같으면 목마름) threshold에 닿는가 — 알림 문구 선택용</summary>
    public static bool ThirstFirst(GeckoData g, float threshold)
    {
        if (g == null) return false;
        return (g.thirst - threshold) / THIRST_DECAY <= (g.hunger - threshold) / HUNGER_DECAY;
    }

    // ── 성장 판정 ──────────────────────────────────────────────

    /// <summary>
    /// 성장 조건에 쓰는 나이(일). 실제 경과 일수 + 먹이 성장치(1 = 3시간).
    /// 앞당기는 양은 필요한 실제 날짜의 30%까지만 — 돈으로 성장을 건너뛰지 못하게 한다
    /// (예: 14일 조건은 아무리 먹여도 실제 9.8일은 지나야 채워진다).
    /// 성장치는 단계가 오를 때 0으로 돌아가므로 단계마다 새로 쌓는다.
    /// </summary>
    public static float EffectiveAgeDays(float realDays, float growthExp)
    {
        float bonus = Mathf.Max(0f, growthExp) * GROWTH_EXP_HOURS / 24f;
        float cap   = Mathf.Max(0f, realDays) * MAX_GROWTH_SPEEDUP / (1f - MAX_GROWTH_SPEEDUP);
        return realDays + Mathf.Min(bonus, cap);
    }

    public void EvaluateGrowth(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null || g.growthStage >= 4) return;

        float realDays = _time.GetElapsedDays(g.createdAtTicks);

        // 자연사 판정 (실제 900일)
        if (realDays >= GROWTH_DAYS_NATURAL_DEATH)
        {
            Debug.Log($"[GeckoManager] 자연사 — {g.name} ({realDays:F0}일) [TBD: STEP 6에서 처리]");
            return;
        }

        var check = CheckGrowth(g, realDays);
        if (!check.AllMet) return;

        int prev = g.growthStage;
        g.growthStage++;
        g.growthExp = 0f;
        _repo.UpdateGecko(g);

        // 다 자랐다 — 한 번만 보상 (단계는 되돌아가지 않으므로 여기는 게코마다 한 번만 지난다)
        if (IsAdult(g))
        {
            var data = _repo.GetPlayerData();
            data.progress ??= new ProgressData();
            data.progress.adultSpeciesIds ??= new List<string>();
            AdultReward(data.progress, g.speciesId, out int coin, out int gem);
            data.coin += coin;
            data.gem  += gem;
            if (!data.progress.adultSpeciesIds.Contains(g.speciesId))
                data.progress.adultSpeciesIds.Add(g.speciesId);
            data.progress.adultCount++;
            LastAdultRewardCoin = coin;
            LastAdultRewardGem  = gem;
            LastAdultsRaised    = data.progress.adultCount;
            LastMorph           = GeckoMorph.Assign(data, g, _morphRng, reward: true);   // 모프가 드러난다 (처음 얻으면 보상)
            Debug.Log($"[GeckoManager] 어덜트 달성 보상 — {g.name} ({g.speciesId}): 코인 +{coin} 젬 +{gem} (키운 어덜트 {data.progress.adultCount}마리)");
        }

        _repo.Save();
        Debug.Log($"[GeckoManager] 성장 단계 상승 — {g.name}: stage {prev} → {g.growthStage} (실제 {realDays:F1}일, 성장치 반영 {check.ageDays:F1}일)");
        OnGrowthUp?.Invoke(g);
        if (IsAdult(g) && !string.IsNullOrEmpty(LastMorph.morphId)) OnMorphRevealed?.Invoke(g);   // 성장 연출 다음에 무늬 연출
    }

    /// <summary>선택한 게코의 다음 성장 조건 (화면 표시용). 판정과 같은 계산.</summary>
    public GrowthCheck GetGrowthCheck(string id)
    {
        var g = _repo.GetGecko(id);
        return CheckGrowth(g, g != null ? _time.GetElapsedDays(g.createdAtTicks) : 0f);
    }

    /// <summary>다음 단계 조건표 — 단계마다 조건이 하나씩 늘어난다 (날짜 → 허물 → 건강 → 애정도)</summary>
    public static GrowthCheck CheckGrowth(GeckoData g, float realDays)
    {
        var c = new GrowthCheck { nextStage = -1 };
        if (g == null || g.growthStage >= 4) return c;

        c.nextStage = g.growthStage + 1;
        c.ageDays   = EffectiveAgeDays(realDays, g.growthExp);
        c.moltCount = g.moltCount;
        c.health    = g.health;
        c.affection = g.affection;

        switch (g.growthStage)
        {
            case 0:
                c.needDays = GROWTH_DAYS_0_TO_1;
                break;
            case 1:
                c.needDays  = GROWTH_DAYS_1_TO_2;
                c.needMolts = GROWTH_MOLT_REQ_1_TO_2;
                break;
            case 2:
                c.needDays   = GROWTH_DAYS_2_TO_3;
                c.needMolts  = GROWTH_MOLT_REQ_2_TO_3;
                c.needHealth = GROWTH_HEALTH_REQ_2_TO_3;
                break;
            default:
                c.needDays      = GROWTH_DAYS_3_TO_4;
                c.needMolts     = GROWTH_MOLT_REQ_3_TO_4;
                c.needAffection = GROWTH_AFFECTION_REQ_3_TO_4;
                break;
        }
        return c;
    }

    // ── 첫 만남 (부화 연출) ───────────────────────────────────

    /// <summary>새 게임 첫 홈 화면에서 알이 깨지는 연출을 보여줘야 하는가 (한 번만)</summary>
    public bool NeedsHatchIntro()
    {
        var data = _repo.GetPlayerData();
        return data.progress != null && !data.progress.hatchIntroSeen && data.geckos.Count > 0;
    }

    /// <summary>부화 연출이 끝났다 — 기록하고 바로 저장 (도중에 앱이 꺼지면 다음 실행 때 다시 보여준다)</summary>
    public void CompleteHatchIntro()
    {
        var data = _repo.GetPlayerData();
        data.progress ??= new ProgressData();
        if (data.progress.hatchIntroSeen) return;
        data.progress.hatchIntroSeen = true;
        _repo.Save();
    }

    // ── 허물 판정 ──────────────────────────────────────────────

    public bool TryMolt(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null || g.moltProgress < 100f) return false;

        float rate = MOLT_BASE_RATE + g.moltBonus;   // 먹이(두비아·칼슘)로 쌓인 보너스 포함
        if (g.thirst > 50f) rate += MOLT_THIRST_BONUS;
        if (g.health > 60f) rate += MOLT_HEALTH_BONUS;

        bool ok = UnityEngine.Random.value < rate;
        g.moltBonus = 0f;   // 1회용 — 성공·실패와 상관없이 쓰고 나면 사라진다

        if (ok)
        {
            g.moltProgress = 0f;
            g.moltCount++;
            if (!IsAdult(g)) g.growthExp += MOLT_EXP_BONUS;   // 다 자라면 성장치를 쌓지 않는다
            _repo.UpdateGecko(g);
            _repo.Save();
            OnMoltSuccess?.Invoke(g);
        }
        else
        {
            g.moltProgress = MOLT_FAIL_RESET;
            _repo.UpdateGecko(g);
            _repo.Save();
            OnMoltFail?.Invoke(g);
        }

        return ok;
    }
}
