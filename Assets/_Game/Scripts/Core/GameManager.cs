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
#endif
}
