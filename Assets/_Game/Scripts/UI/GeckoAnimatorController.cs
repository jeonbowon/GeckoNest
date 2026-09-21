using UnityEngine;

/// <summary>
/// 게임 데이터(GeckoManager) ↔ 게코 움직임(GeckoMotor) 연결.
///
/// - 선택된 게코의 상태값으로 기분(기쁨/졸림/화남)과 허물 상태를 정한다
/// - 선택된 게코의 종(GeckoSpeciesSO)에 맞춰 그림과 눈 깜빡임을 정한다
/// - HomeUIController가 먹이/물/쓰다듬기/청소 버튼과 성장·허물 사건 연출에서 이 클래스를 부른다
///
/// 성장·허물 사건은 여기서 직접 구독하지 않는다. 사건은 GeckoEventQueue에 쌓이고,
/// HomeUIController가 하나씩 꺼내 PresentEvent로 넘긴다 (부팅 중 생긴 사건도 놓치지 않고, 연출끼리 겹치지 않게).
/// 이름은 예전 그대로지만 Animator를 쓰지 않는다.
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
    private float  _pollTimer;
    private bool   _warned;
    private string _speciesApplied;   // 이미 그림·깜빡임을 적용한 종
    private int    _heldStage = -1;   // 0 이상이면 이 단계 크기로 붙잡아 둔다 (성장 연출 대기 중)

    public bool IsBusy => _motor != null && _motor.IsBusy;
    public GeckoMotor Motor => _motor;

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

        // 아직 연출하지 않은 성장이 있으면, 성장 전 크기로 보여줬다가 연출과 함께 커지게 한다
        var selected = GameManager.Instance.GetSelectedGecko();
        var events   = GameManager.Instance.Events;
        _heldStage = selected != null && events != null && events.TryGetPendingGrowthFrom(selected.id, out int fromStage)
            ? Mathf.Max(0, fromStage)
            : -1;

        SyncWithSelectedGecko(immediate: true);
    }

    private void OnDisable()
    {
        if (_gecko == null) return;
        _gecko.OnStateChanged -= HandleStateChanged;
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
    public void TriggerRefuse()    => Play(GeckoAction.Refuse);
    public void TriggerAnnoyed()   => Play(GeckoAction.Angry_TailFlick);
    public void TriggerFeedBig()   => Play(GeckoAction.Tongue_FeedBig);   // 큰 먹이 — 오래 오물오물
    public void TriggerHappy()     => Play(GeckoAction.Happy_LookUp);     // 좋아하는 먹이
    public void TriggerLick()      => Play(GeckoAction.Tongue_Lick);      // 영양제를 할짝
    public void TriggerSurprise()  => Play(GeckoAction.Surprise);         // 몸통을 콕 — 깜짝

    /// <summary>게코를 직접 만졌을 때의 부위별 반응 등 — 동작을 그대로 재생</summary>
    public void TriggerAction(GeckoAction action) => Play(action);

    /// <summary>그곳을 바라본다 — 빈 바닥을 누른 곳 · 선물 상자 (GeckoMotor.LookAt)</summary>
    public void LookAt(Vector3 world, float hold = 1.4f)
    {
        if (HasMotor()) _motor.LookAt(world, hold);
    }

    /// <summary>성장·허물 사건 연출. 선택된 게코의 사건일 때만 부른다.</summary>
    public void PresentEvent(GeckoEvent e)
    {
        if (!HasMotor()) return;

        switch (e.type)
        {
            case GeckoEventType.GrowthUp:
                _heldStage = -1;
                _motor.SetGrowthStage(e.growthStage, immediate: false);   // 연출과 함께 서서히 커진다
                _motor.Play(GeckoAction.LevelUp_Pulse);
                break;
            case GeckoEventType.MoltSuccess:
                _motor.Play(GeckoAction.Molt_Finish);
                break;
            case GeckoEventType.MoltFail:
                _motor.Play(GeckoAction.Molt_Start);   // 실패해도 껍질이 들뜨는 연출은 보여준다
                break;
            case GeckoEventType.MorphReveal:
                RevealMorph();
                break;
            case GeckoEventType.BondUp:
                _motor.Play(GeckoAction.Happy_LookUp);
                break;
        }
    }

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

    // ── 내부 ──────────────────────────────────────────────────

    private void SyncWithSelectedGecko(bool immediate)
    {
        if (GameManager.Instance == null || !HasMotor()) return;

        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;

        ApplySpecies(g.speciesId);
        ApplyMorph(g);
        _motor.SetMood(ResolveMood(g), immediate);
        _motor.SetMolting(g.moltProgress >= MOLT_READY, immediate);
        _motor.SetGrowthStage(_heldStage >= 0 ? _heldStage : g.growthStage, immediate);
    }

    private void ApplySpecies(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId) || speciesId == _speciesApplied) return;
        _speciesApplied = speciesId;

        var species = Resources.Load<GeckoSpeciesSO>($"Species/{speciesId}");
        if (species == null) return;

        _motor.CanBlink = species.canBlink;
        if (species.skin != null && _motor.Rig != null)
            _motor.Rig.SetSkin(species.skin, useStageSkins: false);   // 종 전용 그림에는 크레스티드 단계별 그림을 섞지 않는다
    }

    // 모프 색·무늬 — 어덜트이고 모프 연출이 끝났으면 모프, 아니면 종별 기본색 (GeckoMorph.LookOf)
    private void ApplyMorph(GeckoData g)
    {
        if (_motor.Rig == null) return;
        var events   = GameManager.Instance != null ? GameManager.Instance.Events : null;
        bool pending = events != null && events.HasPending(g.id, GeckoEventType.MorphReveal);
        var look     = GeckoMorph.LookOf(g, GeckoManager.IsAdult(g) && !pending);
        _motor.Rig.SetMorph(look.body, look.pattern, look.patternColor, GeckoMorph.SeedOf(g));
    }

    /// <summary>모프 연출 — 드러난 모프 색으로 바꾸고 기뻐한다 (사건 대기열에서 꺼낸 뒤)</summary>
    public void RevealMorph()
    {
        if (!HasMotor() || GameManager.Instance == null) return;
        var g = GameManager.Instance.GetSelectedGecko();
        if (g != null) ApplyMorph(g);
        _motor.Play(GeckoAction.Happy_LookUp);
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
