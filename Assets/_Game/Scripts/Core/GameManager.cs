using UnityEngine;

public class GameManager
{
    public static GameManager Instance { get; private set; }

    private PlayerRepository _repo;
    private TimeManager      _time;
    private GeckoManager     _gecko;
    private StoreManager     _store;
    private TerrariumManager _terrarium;
    private RewardManager    _reward;
    private SettingsManager  _settings;
    private GeckoEventQueue  _events;

    public static void Initialize(
        PlayerRepository repo, TimeManager time,
        GeckoManager gecko, StoreManager store,
        TerrariumManager terrarium,
        RewardManager reward, SettingsManager settings,
        GeckoEventQueue events)
    {
        Instance = new GameManager
        {
            _repo      = repo,
            _time      = time,
            _gecko     = gecko,
            _store     = store,
            _terrarium = terrarium,
            _reward    = reward,
            _settings  = settings,
            _events    = events,
        };
        Debug.Log("[GameManager] 초기화 완료");
    }

    // ── 게코 ──────────────────────────────────────────────────

    public GeckoData GetSelectedGecko()
    {
        var data = _repo.GetPlayerData();
        if (string.IsNullOrEmpty(data.selectedGeckoId)) return null;
        return _repo.GetGecko(data.selectedGeckoId);
    }

    public void SetSelectedGecko(string id)
    {
        var data = _repo.GetPlayerData();
        if (_repo.GetGecko(id) == null)
        {
            Debug.LogWarning($"[GameManager] 유효하지 않은 게코 ID: {id}");
            return;
        }
        data.selectedGeckoId = id;
        _repo.Save();
    }

    // ── 재화 ──────────────────────────────────────────────────

    public bool SpendCoin(int amount)
    {
        var data = _repo.GetPlayerData();
        if (data.coin < amount) return false;
        data.coin -= amount;
        _repo.Save();
        return true;
    }

    public bool SpendGem(int amount)
    {
        var data = _repo.GetPlayerData();
        if (data.gem < amount) return false;
        data.gem -= amount;
        _repo.Save();
        return true;
    }

    public void AddCoin(int amount)
    {
        var data = _repo.GetPlayerData();
        data.coin += amount;
        _repo.Save();
    }

    public void AddGem(int amount)
    {
        var data = _repo.GetPlayerData();
        data.gem += amount;
        _repo.Save();
    }

    // ── 데이터 접근 ───────────────────────────────────────────

    public PlayerData GetPlayerData() => _repo.GetPlayerData();

    // ── 매니저 접근자 (UI에서 사용) ───────────────────────────

    public GeckoManager      Gecko     => _gecko;
    public StoreManager      Store     => _store;
    public TerrariumManager  Terrarium => _terrarium;
    public RewardManager     Reward    => _reward;
    public SettingsManager   Settings  => _settings;
    public TimeManager       Time      => _time;

    /// <summary>아직 화면에 보여주지 않은 성장·허물 사건 (홈 화면이 꺼내 연출한다)</summary>
    public GeckoEventQueue   Events    => _events;

#if UNITY_EDITOR
    // ── 개발용 ────────────────────────────────────────────────

    // 잘 돌봤다고 가정할 때 한 번에 진행하는 시간 — 목마름 100이 8시간 뒤 60이라
    // 허물 판정의 목마름 보너스(> 50)가 유지되고, 배고픔·목마름이 0이 되지 않아 건강도 줄지 않는다
    private const float DEBUG_CARED_STEP_HOURS = 8f;

    /// <summary>
    /// 에디터 전용 — hours만큼 시간이 흐른 것처럼 만든다 (메뉴 Hako > 검사 > 시간 건너뛰기).
    /// 기준 시각을 과거로 옮긴 뒤 평소와 같은 시간 보정 경로로 반영하므로 성장·허물 사건, 일일 보상도 실제와 똑같이 동작한다.
    /// caredFor = false: 내버려 둔 경우 — 한 번에 반영하므로 오프라인 상한(48시간)이 걸린다.
    /// caredFor = true : 잘 돌봤다고 가정 — 8시간씩 나눠 진행하며 구간마다 배고픔·목마름·청결·기분을 채운다.
    ///                   나이·허물·성장이 건너뛴 기간만큼 실제 순서대로 일어난다 (애정도는 그대로).
    /// </summary>
    public void DebugSkipTime(float hours, bool caredFor = false)
    {
        var data = _repo.GetPlayerData();
        if (data.dailyGoal != null) data.dailyGoal.day -= (int)(hours / 24f);   // 하루 넘게 건너뛰면 오늘의 돌봄 목표도 새로
        foreach (var g in data.geckos)                                          // 어덜트의 선물도 새로
            if (g.giftDay > 0) g.giftDay -= (int)(hours / 24f);
        float left = hours;
        while (left > 0f)
        {
            float step = caredFor ? Mathf.Min(DEBUG_CARED_STEP_HOURS, left) : left;
            left -= step;

            long delta = System.TimeSpan.FromHours(step).Ticks;
            foreach (var g in data.geckos)
            {
                if (caredFor)
                {
                    g.hunger = g.thirst = g.cleanliness = g.mood = 100f;
                }
                g.lastUpdatedTicks -= delta;
                g.createdAtTicks   -= delta;
            }
            if (data.dailyReward.lastClaimedTicks > 0)
                data.dailyReward.lastClaimedTicks -= delta;

            _gecko.ApplyElapsedProgressAll();
        }

        _repo.Save();
        Debug.Log($"[GameManager] 시간 건너뛰기 +{hours}h ({(caredFor ? "잘 돌봄" : "내버려 둠")})");
    }

    /// <summary>
    /// 에디터 전용 — 테스트용 재화 지급 (메뉴 Hako > 검사 > 재화). 바로 저장한다.
    /// 홈 윗줄 숫자는 매 프레임 데이터를 읽어 카운트업된다. 상점·꾸미기 화면은 나갔다 들어오면 반영.
    /// </summary>
    public void DebugAddCurrency(int coin, int gem)
    {
        var data = _repo.GetPlayerData();
        data.coin += coin;
        data.gem  += gem;
        _repo.Save();
        Debug.Log($"[GameManager] 테스트 재화 — 코인 +{coin} · 젬 +{gem} (보유 코인 {data.coin} · 젬 {data.gem})");
    }

    /// <summary>
    /// 에디터 전용 — 선택 게코의 유대 점수를 더한다 (메뉴 Hako > 검사 > 유대). 애정도를 먼저 채우고 나머지는
    /// 넘친 몫에 바로 더한다 (하루 한도 무시). 레벨이 오르면 평소처럼 보상 + 레벨업 연출. 바로 저장한다.
    /// </summary>
    public void DebugAddBond(float points)
    {
        var g = GetSelectedGecko();
        if (g == null || points <= 0f) return;
        float toAffection = Mathf.Clamp(100f - g.affection, 0f, points);
        g.affection    += toAffection;
        g.bondOverflow += points - toAffection;
        ApplyDebugBond(g, $"+{points:F0}");
    }

    /// <summary>에디터 전용 — 선택 게코를 다음 유대 레벨 점수까지 올린다 (최고 레벨이면 그대로)</summary>
    public void DebugBondNextLevel()
    {
        var g = GetSelectedGecko();
        if (g == null) return;
        float next = GeckoBond.NextPoints(GeckoBond.Level(g));
        if (next < 0f)
        {
            Debug.Log($"[GameManager] 테스트 유대 — {g.name}은(는) 이미 최고 레벨");
            return;
        }
        DebugAddBond(Mathf.Ceil(next - GeckoBond.Points(g)));
    }

    /// <summary>에디터 전용 — 선택 게코의 유대를 처음으로 (애정도 0 · 넘친 몫 0 · 보상 받은 레벨 0 — 다시 오르면 보상도 다시)</summary>
    public void DebugResetBond()
    {
        var g = GetSelectedGecko();
        if (g == null) return;
        g.affection = g.bondOverflow = g.bondToday = 0f;
        g.bondRewardedLevel = 0;
        ApplyDebugBond(g, "처음으로");
    }

    /// <summary>
    /// 에디터 전용 — 선택 게코(어덜트)의 모프를 바꾼다 (메뉴 Hako > 검사 > 모프).
    /// next = true면 그 종 모프를 차례로, false면 확률대로 다시 뽑기. 도감에 기록하지만 보상은 없다
    /// </summary>
    public void DebugChangeMorph(bool next)
    {
        var g = GetSelectedGecko();
        if (g == null || !GeckoManager.IsAdult(g)) return;
        var list = GeckoMorph.ForSpecies(g.speciesId);
        if (list.Count == 0) return;

        if (next)
        {
            int i = list.FindIndex(m => m.id == g.morphId);
            g.morphId = list[(i + 1) % list.Count].id;
        }
        else
        {
            g.morphId = GeckoMorph.Roll(g.speciesId, new System.Random()).id;
        }
        GeckoMorph.Record(_repo.GetPlayerData(), g.morphId);
        _repo.UpdateGecko(g);
        _repo.Save();
        _gecko.DebugNotifyChanged(g);
        var def = GeckoMorph.Find(g.morphId);
        Debug.Log($"[GameManager] 테스트 모프 — {g.name}: {g.morphId} ({def.rarity})");
    }

    private void ApplyDebugBond(GeckoData g, string what)
    {
        _gecko.CheckBondLevel(g);        // 레벨이 올랐으면 보상 + 사건 (홈 화면이 연출)
        _repo.UpdateGecko(g);
        _repo.Save();
        _gecko.DebugNotifyChanged(g);    // 홈 윗줄 "유대 N"·게이지 갱신
        Debug.Log($"[GameManager] 테스트 유대 {what} — {g.name}: 점수 {GeckoBond.Points(g):F0} (애정도 {g.affection:F0} + 넘친 몫 {g.bondOverflow:F0}) · Lv.{GeckoBond.Level(g)}");
    }
#endif
}
