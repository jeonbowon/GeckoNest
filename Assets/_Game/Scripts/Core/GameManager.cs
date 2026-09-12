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
}
