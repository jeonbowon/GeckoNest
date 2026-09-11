using UnityEngine;

/// <summary>
/// 게임 데이터(GeckoManager) ↔ 게코 움직임(GeckoMotor) 연결.
///
/// - 선택된 게코의 상태값으로 기분(기쁨/졸림/화남)과 허물 상태를 정한다
/// - 허물 성공/실패, 성장 이벤트에 맞는 동작을 재생한다
/// - HomeUIController가 먹이/물/쓰다듬기/청소 버튼에서 Trigger*를 호출한다
///
/// 이전 버전은 Animator 트리거를 썼으나 클립이 없어 동작하지 않았고,
/// 람다로 구독한 이벤트가 씬 전환 후에도 남아 파괴된 Animator를 호출하는 문제가 있었다.
/// 지금은 이름 있는 메서드로 구독하고 OnDisable에서 모두 해제한다.
/// </summary>
public class GeckoAnimatorController : MonoBehaviour
{
    private const float POLL_INTERVAL = 0.5f;

    // 기분 판정 기준 — 이전 Animator 파라미터(IsHappy/IsSleepy/IsAngry/IsMolting)와 같은 값
    private const float HAPPY_MOOD      = 70f;
    private const float HAPPY_AFFECTION = 50f;
    private const float SLEEPY_MOOD     = 35f;
    private const float ANGRY_HUNGER    = 20f;
    private const float ANGRY_MOOD      = 30f;
    private const float MOLT_READY      = 80f;

    [SerializeField] private GeckoMotor _motor;

    private GeckoManager _gecko;
    private float _pollTimer;
    private bool  _warned;

    // ── 생명주기 ──────────────────────────────────────────────

    private void Awake()
    {
        if (_motor == null) _motor = GetComponent<GeckoMotor>();
    }

    private void OnEnable()
    {
        // GameManager 없음 = Boot 씬을 거치지 않고 실행. HomeUIController가 이미 에러를 띄운다.
        if (GameManager.Instance == null) return;

        _gecko = GameManager.Instance.Gecko;
        _gecko.OnStateChanged += HandleStateChanged;
        _gecko.OnMoltSuccess  += HandleMoltSuccess;
        _gecko.OnMoltFail     += HandleMoltFail;
        _gecko.OnGrowthUp     += HandleGrowthUp;

        SyncWithSelectedGecko(immediate: true);
    }

    private void OnDisable()
    {
        if (_gecko == null) return;
        _gecko.OnStateChanged -= HandleStateChanged;
        _gecko.OnMoltSuccess  -= HandleMoltSuccess;
        _gecko.OnMoltFail     -= HandleMoltFail;
        _gecko.OnGrowthUp     -= HandleGrowthUp;
        _gecko = null;
    }

    private void Update()
    {
        _pollTimer -= Time.deltaTime;
        if (_pollTimer > 0f) return;
        _pollTimer = POLL_INTERVAL;
        SyncWithSelectedGecko(immediate: false);
    }

    // ── 공개 메서드 (HomeUIController에서 호출) ───────────────

    public void TriggerFeedCatch() => Play(GeckoAction.Tongue_FeedCatch);
    public void TriggerDrink()     => Play(GeckoAction.Tongue_Drink);
    public void TriggerPet()       => Play(GeckoAction.Pet_Reaction);
    public void TriggerClean()     => Play(GeckoAction.Happy_LookUp);

    /// <summary>상태값 → 기분. 화남 > 졸림 > 기쁨 > 보통 순으로 우선한다.</summary>
    public static GeckoMood ResolveMood(GeckoData g)
    {
        if (g == null) return GeckoMood.Normal;
        if (g.hunger < ANGRY_HUNGER && g.mood < ANGRY_MOOD) return GeckoMood.Angry;
        if (g.mood < SLEEPY_MOOD) return GeckoMood.Sleepy;
        if (g.mood > HAPPY_MOOD && g.affection > HAPPY_AFFECTION) return GeckoMood.Happy;
        return GeckoMood.Normal;
    }

    // ── 이벤트 수신 ───────────────────────────────────────────

    private void HandleStateChanged(GeckoData g)
    {
        if (IsSelected(g)) SyncWithSelectedGecko(immediate: false);
    }

    private void HandleMoltSuccess(GeckoData g)
    {
        if (IsSelected(g)) Play(GeckoAction.Molt_Finish);
    }

    private void HandleMoltFail(GeckoData g)
    {
        if (IsSelected(g)) Play(GeckoAction.Molt_Start);   // 실패해도 껍질이 들뜨는 연출은 보여준다
    }

    private void HandleGrowthUp(GeckoData g)
    {
        if (!IsSelected(g) || !HasMotor()) return;
        _motor.SetGrowthStage(g.growthStage, immediate: false);
        _motor.Play(GeckoAction.LevelUp_Pulse);
    }

    // ── 내부 ──────────────────────────────────────────────────

    private void SyncWithSelectedGecko(bool immediate)
    {
        if (GameManager.Instance == null || !HasMotor()) return;

        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;

        _motor.SetMood(ResolveMood(g));
        _motor.SetMolting(g.moltProgress >= MOLT_READY);
        _motor.SetGrowthStage(g.growthStage, immediate);
    }

    private void Play(GeckoAction action)
    {
        if (HasMotor()) _motor.Play(action);
    }

    private static bool IsSelected(GeckoData g)
    {
        if (g == null || GameManager.Instance == null) return false;
        return GameManager.Instance.GetPlayerData().selectedGeckoId == g.id;
    }

    private bool HasMotor()
    {
        if (_motor != null) return true;
        if (!_warned)
        {
            _warned = true;
            Debug.LogWarning("[GeckoAnimatorController] GeckoMotor가 없습니다 — 메뉴 Hako > Gecko > ① 프록시 게코 만들기를 실행하세요.", this);
        }
        return false;
    }
}
