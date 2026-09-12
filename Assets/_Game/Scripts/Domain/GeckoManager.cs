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

public class GeckoManager
{
    // 상태 감소율 ([TBD] — 수치 조정 시 여기서만 변경)
    private const float HUNGER_DECAY      = 4f;    // [TBD] /h
    private const float THIRST_DECAY      = 5f;    // [TBD] /h
    private const float CLEAN_DECAY       = 0.67f; // /h
    private const float MOOD_DECAY        = 1f;    // [TBD] /h
    private const float HEALTH_DECAY      = 1f;    // hunger/thirst 0일 때 /h

    private const float WATER_RESTORE     = 40f;   // [TBD]
    private const float PET_MOOD_BONUS    = 5f;    // [TBD]
    private const float PET_AFFECTION     = 3f;
    private const float FEED_AFFECTION    = 2f;
    private const float WATER_AFFECTION   = 1f;
    private const float CLEAN_RESTORE     = 60f;   // [TBD]
    private const float CLEAN_AFFECTION   = 1f;

    // 돌봄 제한 — 연타로 수치가 의미 없어지는 것을 막고, 게코의 반응 자체를 재미로 만든다
    private const float CARE_FULL_THRESHOLD = 95f;    // [TBD] 이 이상이면 먹이·물·청소를 거절
    private const float PET_FATIGUE_LIMIT   = 4f;     // [TBD] 연달아 쓰다듬어도 좋아하는 횟수
    private const float PET_FATIGUE_RECOVER = 0.125f; // [TBD] 초당 회복량 (8초에 1회분)
    private const float PET_ANNOY_MOOD      = 2f;     // [TBD] 귀찮게 했을 때 기분 하락

    private const float MOLT_BASE_RATE    = 0.70f; // [TBD]
    private const float MOLT_THIRST_BONUS = 0.15f;
    private const float MOLT_HEALTH_BONUS = 0.10f;
    private const float MOLT_FAIL_RESET   = 30f;   // 실패 시 moltProgress 리셋값
    private const float MOLT_EXP_BONUS    = 20f;   // [TBD]

    // 허물 진행 — 첫 허물은 빠르게(첫 주 안에 큰 이벤트를 경험), 이후는 약 21일 주기
    private const float FIRST_MOLT_PROGRESS_PER_HOUR = 1.67f; // [TBD] ~60시간(2.5일)에 첫 허물
    private const float MOLT_PROGRESS_PER_HOUR       = 0.20f; // [TBD] ~21일에 100% 달성

    // 성장 단계 조건 (실제 경과 일수)
    private const float GROWTH_DAYS_0_TO_1          = 15f;  // 해츨링 → 베이비
    private const float GROWTH_DAYS_1_TO_2          = 30f;  // 베이비 → 주버나일
    private const float GROWTH_DAYS_2_TO_3          = 60f;  // 주버나일 → 서브어덜트
    private const float GROWTH_DAYS_3_TO_4          = 120f; // 서브어덜트 → 어덜트
    private const float GROWTH_DAYS_NATURAL_DEATH   = 900f; // 자연사

    private const int   GROWTH_MOLT_REQ_1_TO_2      = 1;
    private const int   GROWTH_MOLT_REQ_2_TO_3      = 3;
    private const int   GROWTH_MOLT_REQ_3_TO_4      = 5;
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

    public GeckoManager(PlayerRepository repo, TimeManager time)
    {
        _repo = repo;
        _time = time;
    }

    // ── 먹이 ──────────────────────────────────────────────────

    public CareResult FeedGecko(string id, ItemSO item)
    {
        var g = _repo.GetGecko(id);
        if (g == null || item == null) return CareResult.Failed;

        // 배부르면 먹지 않는다 — 아이템도 차감하지 않는다 (배를 채우지 않는 영양제는 예외)
        if (item.hungerRestore > 0f && g.hunger >= CARE_FULL_THRESHOLD)
            return CareResult.Refused;

        // 인벤토리에서 1개 차감 — 수량 부족이면 중단 (이중 클릭 방어)
        if (!_repo.RemoveItem(item.itemId, 1))
        {
            Debug.LogWarning($"[GeckoManager] FeedGecko — {item.itemId} 재고 없음");
            return CareResult.Failed;
        }

        g.hunger    = Mathf.Min(100f, g.hunger    + item.hungerRestore);
        g.mood      = Mathf.Min(100f, g.mood      + item.moodBonus);
        g.health    = Mathf.Min(100f, g.health    + item.healthRestore);
        g.growthExp += item.growthExpGain;
        g.affection = Mathf.Min(100f, g.affection + FEED_AFFECTION);

        _repo.UpdateGecko(g);
        EvaluateGrowth(id);
        _repo.Save();
        Debug.Log($"[GeckoManager] FeedGecko — {g.name} hunger: {g.hunger:F1} (남은 {item.itemId}: {_repo.GetItemCount(item.itemId)})");
        OnStateChanged?.Invoke(g);
        return CareResult.Done;
    }

    // ── 물 ────────────────────────────────────────────────────

    public CareResult GiveWater(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null) return CareResult.Failed;
        if (g.thirst >= CARE_FULL_THRESHOLD) return CareResult.Refused;

        g.thirst    = Mathf.Min(100f, g.thirst    + WATER_RESTORE);
        g.affection = Mathf.Min(100f, g.affection + WATER_AFFECTION);

        _repo.UpdateGecko(g);
        _repo.Save();
        OnStateChanged?.Invoke(g);
        return CareResult.Done;
    }

    // ── 쓰다듬기 ──────────────────────────────────────────────

    public CareResult Pet(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null) return CareResult.Failed;

        if (!RegisterPet(id))
        {
            g.mood = Mathf.Max(0f, g.mood - PET_ANNOY_MOOD);
            _repo.UpdateGecko(g);
            _repo.Save();
            OnStateChanged?.Invoke(g);
            return CareResult.Annoyed;
        }

        g.mood      = Mathf.Min(100f, g.mood      + PET_MOOD_BONUS);
        g.affection = Mathf.Min(100f, g.affection + PET_AFFECTION);

        _repo.UpdateGecko(g);
        _repo.Save();
        OnStateChanged?.Invoke(g);
        return CareResult.Done;
    }

    /// <summary>쓰다듬기 피로도를 쌓는다. 한도 안이면 true (좋아함), 넘으면 false (귀찮아함).</summary>
    private bool RegisterPet(string id)
    {
        double now = TimeSpan.FromTicks(_time.GetNowTicks()).TotalSeconds;
        _petFatigue.TryGetValue(id, out var f);

        float recovered = (float)Math.Max(0.0, now - f.lastSeconds) * PET_FATIGUE_RECOVER;
        f.amount      = Mathf.Max(0f, f.amount - recovered);
        f.lastSeconds = now;

        // 탭 사이에도 조금씩 회복되므로 (예: 3.97) 반 칸 여유를 두고 판정한다 → 연달아 정확히 LIMIT번까지 좋아함
        bool ok = f.amount < PET_FATIGUE_LIMIT - 0.5f;
        f.amount = Mathf.Min(f.amount + 1f, PET_FATIGUE_LIMIT + 2f);   // 계속 연타하면 조금 더 오래 삐친다
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
        g.affection   = Mathf.Min(100f, g.affection   + CLEAN_AFFECTION);

        _repo.UpdateGecko(g);
        _repo.Save();
        OnStateChanged?.Invoke(g);
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

        g.hunger      = Mathf.Max(0f, g.hunger      - HUNGER_DECAY * h);
        g.thirst      = Mathf.Max(0f, g.thirst      - THIRST_DECAY * h);
        g.cleanliness = Mathf.Max(0f, g.cleanliness - CLEAN_DECAY  * h);
        g.mood        = Mathf.Max(0f, g.mood        - MOOD_DECAY   * h);

        if (g.hunger <= 0f || g.thirst <= 0f)
            g.health = Mathf.Max(0f, g.health - HEALTH_DECAY * h);

        // Cleanliness 20 이하 → Mood 추가 패널티
        if (g.cleanliness <= 20f)
            g.mood = Mathf.Max(0f, g.mood - 0.5f * h);

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

    // ── 성장 판정 ──────────────────────────────────────────────

    public void EvaluateGrowth(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null || g.growthStage >= 4) return;

        float ageDays = _time.GetElapsedDays(g.createdAtTicks);

        // 자연사 판정 (900일)
        if (ageDays >= GROWTH_DAYS_NATURAL_DEATH)
        {
            Debug.Log($"[GeckoManager] 자연사 — {g.name} ({ageDays:F0}일) [TBD: STEP 6에서 처리]");
            return;
        }

        bool canLevelUp = g.growthStage switch
        {
            0 => ageDays >= GROWTH_DAYS_0_TO_1,
            1 => ageDays >= GROWTH_DAYS_1_TO_2
                 && g.moltCount >= GROWTH_MOLT_REQ_1_TO_2,
            2 => ageDays >= GROWTH_DAYS_2_TO_3
                 && g.moltCount >= GROWTH_MOLT_REQ_2_TO_3
                 && g.health   >= GROWTH_HEALTH_REQ_2_TO_3,
            3 => ageDays >= GROWTH_DAYS_3_TO_4
                 && g.moltCount  >= GROWTH_MOLT_REQ_3_TO_4
                 && g.affection  >= GROWTH_AFFECTION_REQ_3_TO_4,
            _ => false,
        };

        if (!canLevelUp) return;

        int prev = g.growthStage;
        g.growthStage++;
        g.growthExp = 0f;
        _repo.UpdateGecko(g);
        _repo.Save();
        Debug.Log($"[GeckoManager] 성장 단계 상승 — {g.name}: stage {prev} → {g.growthStage} (age {ageDays:F1}일)");
        OnGrowthUp?.Invoke(g);
    }

    // ── 허물 판정 ──────────────────────────────────────────────

    public bool TryMolt(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null || g.moltProgress < 100f) return false;

        float rate = MOLT_BASE_RATE;
        if (g.thirst > 50f) rate += MOLT_THIRST_BONUS;
        if (g.health > 60f) rate += MOLT_HEALTH_BONUS;

        bool ok = UnityEngine.Random.value < rate;

        if (ok)
        {
            g.moltProgress = 0f;
            g.moltCount++;
            g.growthExp += MOLT_EXP_BONUS;
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
