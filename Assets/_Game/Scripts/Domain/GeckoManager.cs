using System;
using UnityEngine;

public class GeckoManager
{
    // 오프라인 상태 감소율 ([TBD] — 수치 조정 시 여기서만 변경)
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

    private const float MOLT_BASE_RATE    = 0.70f; // [TBD]
    private const float MOLT_THIRST_BONUS = 0.15f;
    private const float MOLT_HEALTH_BONUS = 0.10f;
    private const float MOLT_FAIL_RESET   = 30f;   // 실패 시 moltProgress 리셋값
    private const float MOLT_EXP_BONUS    = 20f;   // [TBD]
    private const float MOLT_PROGRESS_PER_HOUR = 0.20f; // [TBD] ~20일에 100% 달성

    // 성장 단계 조건 (게임 일수 = 실제 일수 / 실제 1년 = 게임 60일)
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

    public void FeedGecko(string id, ItemSO item)
    {
        var g = _repo.GetGecko(id);
        if (g == null || item == null) return;

        g.hunger    = Mathf.Min(100f, g.hunger    + item.hungerRestore);
        g.mood      = Mathf.Min(100f, g.mood      + item.moodBonus);
        g.health    = Mathf.Min(100f, g.health    + item.healthRestore);
        g.growthExp += item.growthExpGain;
        g.affection = Mathf.Min(100f, g.affection + FEED_AFFECTION);

        _repo.UpdateGecko(g);
        EvaluateGrowth(id);
        _repo.Save();
        Debug.Log($"[GeckoManager] FeedGecko — {g.name} hunger: {g.hunger:F1}");
        OnStateChanged?.Invoke(g);
    }

    // ── 물 ────────────────────────────────────────────────────

    public void GiveWater(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null) return;

        g.thirst    = Mathf.Min(100f, g.thirst    + WATER_RESTORE);
        g.affection = Mathf.Min(100f, g.affection + WATER_AFFECTION);

        _repo.UpdateGecko(g);
        _repo.Save();
        OnStateChanged?.Invoke(g);
    }

    // ── 쓰다듬기 ──────────────────────────────────────────────

    public void Pet(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null) return;

        g.mood      = Mathf.Min(100f, g.mood      + PET_MOOD_BONUS);
        g.affection = Mathf.Min(100f, g.affection + PET_AFFECTION);

        _repo.UpdateGecko(g);
        _repo.Save();
        OnStateChanged?.Invoke(g);
    }

    // ── 청소 ──────────────────────────────────────────────────

    public void Clean(string id)
    {
        var g = _repo.GetGecko(id);
        if (g == null) return;

        g.cleanliness = Mathf.Min(100f, g.cleanliness + CLEAN_RESTORE);
        g.affection   = Mathf.Min(100f, g.affection   + CLEAN_AFFECTION);

        _repo.UpdateGecko(g);
        _repo.Save();
        OnStateChanged?.Invoke(g);
    }

    // ── 오프라인 보정 ──────────────────────────────────────────

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
        g.moltProgress = Mathf.Min(100f, g.moltProgress + MOLT_PROGRESS_PER_HOUR * h);

        g.lastUpdatedTicks = _time.GetNowTicks();

        _repo.UpdateGecko(g);
        OnStateChanged?.Invoke(g);

        // 허물 자동 판정 — 100 도달 시
        if (g.moltProgress >= 100f)
            TryMolt(id);

        // 성장 자동 판정
        EvaluateGrowth(id);
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
