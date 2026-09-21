using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeUIController : MonoBehaviour
{
    // ── 상단 바 ───────────────────────────────────────────────
    [Header("상단 바")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _gemText;

    // ── 게코 정보 ──────────────────────────────────────────────
    [Header("게코 정보")]
    [SerializeField] private TMP_Text _geckoNameText;
    [SerializeField] private TMP_Text _growthStageText;    // 해츨링 / 베이비 / …

    // ── 상태 게이지 ───────────────────────────────────────────
    [Header("상태 게이지 (Image fillAmount)")]
    [SerializeField] private Image _hungerFill;
    [SerializeField] private Image _thirstFill;
    [SerializeField] private Image _moodFill;
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _cleanlinessFill;

    // ── 경고 색상 ──────────────────────────────────────────────
    private static readonly Color COLOR_WARNING = new Color(1f, 0.27f, 0.27f);
    private static readonly Color COLOR_WARNING_SOFT = new Color(1f, 0.55f, 0.55f);

    // 게이지 평소 색 — ART_GUIDE 팔레트 계열 (위험 구간에서는 위 경고색으로 깜빡인다)
    private static readonly Color GAUGE_HUNGER = new Color(0.95f, 0.65f, 0.35f);   // #F2A65A 주황
    private static readonly Color GAUGE_THIRST = new Color(0.56f, 0.83f, 1.00f);   // #8FD4FF 물방울
    private static readonly Color GAUGE_MOOD   = new Color(1.00f, 0.56f, 0.64f);   // #FF8FA3 하트 핑크
    private static readonly Color GAUGE_HEALTH = new Color(0.56f, 0.84f, 0.58f);   // #8FD694 초록
    private static readonly Color GAUGE_CLEAN  = new Color(1.00f, 0.91f, 0.62f);   // #FFE89E 반짝 금색
    private const float WARNING_THRESHOLD = 30f;

    // ── 허물 진행 ──────────────────────────────────────────────
    [Header("허물")]
    [SerializeField] private GameObject _moltBadge;        // moltProgress 80+ 시 표시
    [SerializeField] private Image      _moltProgressFill; // moltProgress 게이지 (optional)

    // ── 허물/성장 결과 알림 ───────────────────────────────────
    [Header("결과 알림 패널")]
    [SerializeField] private GameObject _resultPanel;      // 알림 루트 오브젝트
    [SerializeField] private TMP_Text   _resultText;       // 알림 텍스트
    private const float RESULT_DISPLAY_SECONDS = 2.5f;
    private Coroutine   _resultCoroutine;

    // ── 행동 버튼 ──────────────────────────────────────────────
    [Header("행동 버튼")]
    [SerializeField] private Button   _feedButton;
    [SerializeField] private TMP_Text _feedItemText;   // 예: "귀뚜라미 x2" — 없으면 생략
    [SerializeField] private Button   _waterButton;
    [SerializeField] private Button   _petButton;
    [SerializeField] private Button   _cleanButton;
    [Tooltip("돌봄 버튼 위쪽 아이콘 — 0=먹이 1=물 2=쓰다듬기 3=청소 (지금은 상태 아이콘을 다시 쓴다)")]
    [SerializeField] private Sprite[] _careButtonIcons = new Sprite[4];

    // ── 네비게이션 버튼 ───────────────────────────────────────
    [Header("네비게이션")]
    [SerializeField] private Button _storeButton;
    [SerializeField] private Button _geckoListButton;
    [SerializeField] private Button _terrariumButton;
    [SerializeField] private Button _rewardButton;
    [SerializeField] private Button _settingsButton;

    // ── 팝업 패널 ──────────────────────────────────────────────
    [Header("팝업 패널")]
    [SerializeField] private GameObject _rewardPanel;
    [SerializeField] private GameObject _settingsPanel;

    // ── 테라리움 비주얼 ───────────────────────────────────────
    [Header("테라리움 비주얼")]
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _floorImage;
    [SerializeField] private Image[] _decorImages;  // 장식 슬롯 4개

    [Header("테라리움 장식 에셋 (Inspector에서 드래그)")]
    [SerializeField] private DecorItemSO[] _allDecorItems;

    // ── 게코 영역 ──────────────────────────────────────────────
    [Header("게코")]
    [SerializeField] private GeckoAnimatorController _geckoAnimator;

    // ── 이동 AI ───────────────────────────────────────────────
    [Header("게코 이동 AI")]
    [SerializeField] private GeckoMovementAI _geckoMovement;

    // ── 성장 단계 ──────────────────────────────────────────────
    [Header("성장 단계 아이콘")]
    [SerializeField] private Image    _growthStageIcon;
    [SerializeField] private Sprite[] _growthStageSprites; // 0=Egg, 1=Baby, 2=Juvenile, 3=Sub-Adult, 4=Adult

    // 성장 단계 이름·게코 한마디·결과 알림 문구는 번역표 Loc에 있다 (stage.N · line.* · event.*)
    private const float PET_LINE_CHANCE = 0.35f;
    private const float FED_LINE_CHANCE = 0.4f;

    // ── 화면 반응 수치 ─────────────────────────────────────────
    private const float GAUGE_SPEED     = 7f;     // 게이지가 목표값으로 따라가는 속도
    private const float COUNT_UP_TIME   = 0.6f;   // 코인·젬 숫자가 올라가는 시간
    private const float EVENT_START_GAP = 0.6f;   // 홈 진입 후 사건 연출 시작까지 (페이드가 걷히길 기다림)

    private GeckoManager     _gecko;
    private TerrariumManager _terrarium;
    private GeckoFx          _fx;
    private Coroutine        _presenter;
    private int              _stageOverride = -1;   // 성장 연출 전까지 이전 단계 이름을 보여준다

    private GaugeView[] _gauges;
    private CountView   _coinView, _gemView;

    // ── 생명주기 ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[HomeUIController] GameManager가 아직 없습니다 — AppBootstrap이 초기화한 뒤 이 씬을 다시 엽니다.");
            return;
        }

        _gecko = GameManager.Instance.Gecko;
        _gecko.OnStateChanged += Refresh;

        _terrarium = GameManager.Instance.Terrarium;
        _terrarium.OnTerrariumChanged += RefreshTerrarium;

        _reward = GameManager.Instance.Reward;
        _reward.OnGoalProgress += OnGoalProgress;

        if (_geckoMovement != null)
        {
            _geckoMovement.HideChanged += OnGeckoHideChanged;   // 은신처 — 집 그림을 게코 앞/뒤로
            _geckoMovement.Perched     += OnGeckoPerched;       // 나뭇가지 위 — 말풍선
            _geckoMovement.Arrived     += OnGeckoArrived;       // 불러서 도착 — 올려다보기
        }

        _storeButton?.onClick.AddListener(OnStoreClicked);
        _geckoListButton?.onClick.AddListener(OnGeckoListClicked);
        _terrariumButton?.onClick.AddListener(OnTerrariumClicked);
        _rewardButton?.onClick.AddListener(OnRewardClicked);
        _settingsButton?.onClick.AddListener(OnSettingsClicked);

        if (_rewardPanel != null)   _rewardPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);

        // 새 게임 첫 홈이면 알이 깨지는 연출부터 (Start에서 시작) — 보상 팝업은 부화가 끝난 뒤 OnHatched에서
        _hatchPending = _gecko.NeedsHatchIntro();
        _greeted      = false;   // 유대 Lv.1 인사는 홈에 들어올 때마다 한 번

        // 일일 보상 자동 팝업 — 받을 수 있으면 앱 진입 시 표시
        if (!_hatchPending && GameManager.Instance.Reward.CanClaim() && _rewardPanel != null)
            _rewardPanel.SetActive(true);

        _feedButton.onClick.AddListener(OnFeedClicked);
        _waterButton.onClick.AddListener(OnWaterClicked);
        _petButton.onClick.AddListener(OnPetClicked);
        _cleanButton.onClick.AddListener(OnCleanClicked);

        var selected = GameManager.Instance.GetSelectedGecko();
        if (selected == null)
            Debug.LogError("[HomeUIController] GetSelectedGecko()가 null — selectedGeckoId 또는 geckos 목록을 확인하세요.");

        // 아직 연출하지 않은 성장이 있으면 그 전 단계 이름을 보여줬다가 연출 때 바꾼다
        _stageOverride = selected != null && GameManager.Instance.Events != null
                         && GameManager.Instance.Events.TryGetPendingGrowthFrom(selected.id, out int fromStage)
            ? Mathf.Max(0, fromStage)
            : -1;

        if (_resultPanel != null)
            _resultPanel.SetActive(false);

        if (_geckoMovement != null)
            _geckoMovement.enabled = !_hatchPending;                            // 부화 전에는 걷지 않는다
        if (_hatchPending) HatchIntro.SetGeckoVisible(_geckoAnimator, false);   // 첫 프레임부터 알만 보이게

        MakeFillable(_moltProgressFill);   // 허물 진행 막대도 스프라이트가 없으면 늘 가득 차 보인다
        EnsureCareButtonIcons();
        EnsureNavButtonIcons();
        EnsureGrowthInfoButton();          // 성장 단계 글자 누르기 → 다음 성장 조건
        SceneTextLocalizer.Ignore(_geckoNameText);   // 게코 이름은 번역하지 않는다 ("하코"가 영어에서 "Hako"로 바뀌지 않게)
        InitViews();
        ApplyHudReadability();              // 게이지 숫자가 만들어진 뒤 — 배경 위 글자에 그림자
        Refresh(selected);
        SnapViews();
        EnsureDecorImages();                // 씬에는 칸 4개 그림만 있다 — 늘어난 칸은 복제
        RefreshTerrarium();

        _presenter = StartCoroutine(EventPresenter());
    }

    private void Start()
    {
        // 게코 오브젝트의 Awake가 끝난 뒤에 연출 레이어를 붙인다 (OnEnable 순서는 오브젝트끼리 보장되지 않음)
        EnsureFx();
        EnsureGeckoTouch();
        if (_gift != null) _gift.SetAsLastSibling();   // OnEnable에서 먼저 놓인 선물이 터치 영역에 가리지 않게
        if (_geckoTouch != null) _geckoTouch.LongPressed = OnGeckoLongPressed;   // 유대 Lv.5 손바닥
        EnsureFloorCatcher();                                                   // 유대 Lv.3 부르기
        EnsureAtmosphere();                                                     // 비네트·먼지·앞 잎사귀
        if (_hatchPending) StartHatchIntro();   // 인사 말풍선·하트에 연출 레이어가 필요해서 Start에서
    }

    // ── 게코 직접 만지기 ──────────────────────────────────────

    private GeckoTouch _geckoTouch;

    private void EnsureGeckoTouch()
    {
        if (_geckoTouch != null || _geckoAnimator == null) return;
        var motor = _geckoAnimator.Motor;
        _geckoTouch = GeckoTouch.Create(_geckoAnimator.transform as RectTransform,
                                        motor != null ? motor.Rig : null, OnGeckoTouched);
    }

    // 부위별 반응 — 머리 = 쓰다듬기(수치·피로·오늘의 목표). 나머지는 수치 변화 없이 여러 반응 중 하나.
    // 벽에 매달려 있으면 놀라서 달아나고, 졸리거나 화나 있으면 반응이 달라진다. 연타하면 삐져서 달아난다.
    private const float POKE_WINDOW  = 3f;    // [TBD] 이 시간(초) 안에
    private const int   POKE_LIMIT   = 5;     // [TBD] 이만큼 건드리면 삐져서 달아난다
    private const float MOUTH_HUNGRY = 60f;   // [TBD] 배고픔이 이보다 낮으면 입을 만졌을 때 먹이를 조른다

    /// <summary>부위별 반응 문구 키 — 자가 검사가 번역표에 모두 있는지 확인한다</summary>
    public static readonly string[] TouchLineKeys =
    {
        "line.eye_wipe", "line.eye_no", "line.mouth", "line.mouth_hungry", "line.yawn", "line.wave", "line.paw",
        "line.giggle", "line.kick", "line.poke", "line.shiver", "line.tail_base", "line.tail",
        "line.sleepy", "line.grumpy", "line.climb", "line.annoyed",
    };

    private readonly System.Collections.Generic.Queue<float> _pokeTimes = new System.Collections.Generic.Queue<float>();

    private void OnGeckoTouched(GeckoTouchZone zone, Vector3 touchWorld)
    {
        if (SceneRouter.IsTransitioning || _hatchPending) return;
        var move = _geckoMovement != null ? _geckoMovement : null;
        if (move != null && move.IsFleeing) return;   // 달아나는 중에는 무시

        if (zone == GeckoTouchZone.Head)
        {
            OnPetClicked();   // 매달려 있어도 쓰다듬기는 된다 (연타 피로는 쓰다듬기 규칙)
            return;
        }

        var anim = Anim;
        if (anim == null) return;
        bool canMove = move != null && move.isActiveAndEnabled;

        // 연타 — 짧은 시간에 여러 번 건드리면 삐져서 달아난다 (동작 중에 누른 것도 센다)
        if (CountPoke() >= POKE_LIMIT)
        {
            _pokeTimes.Clear();
            anim.TriggerAnnoyed();
            Fx()?.Annoyed();
            Fx()?.Say(Loc.Pick("line.annoyed"));
            if (canMove) move.Flee(touchWorld);   // 꼬리를 튕긴 뒤 달아난다 (이동은 동작이 끝날 때까지 기다린다)
            Haptics.Medium();
            return;
        }
        if (anim.IsBusy) return;   // 다른 동작 중에는 무시 (누를 때마다 동작이 끊기지 않게)

        // 벽에 매달려 있다 — 깜짝 놀라 더 올라가거나 후다닥 내려간다
        if (canMove && move.IsClimbing)
        {
            React(anim, GeckoAction.Surprise, "line.climb", Sfx.Pop);
            move.Flee(touchWorld);
            return;
        }

        if (zone == GeckoTouchZone.TailTip)
        {
            if (!canMove) return;
            move.Flee(touchWorld);                 // 누른 곳 반대쪽으로 빠르게 달아난다
            Fx()?.Annoyed();                       // '흥' 소리 + 머리 위 김
            Fx()?.Say(Loc.Pick("line.tail"));      // 말풍선은 달아나는 게코를 따라간다
            Haptics.Light();
            return;
        }

        // 기분 — 졸리면 하품, 화나 있으면 꼬리를 튕기며 짜증
        var motor = anim.Motor;
        var mood  = motor != null ? motor.Mood : GeckoMood.Normal;
        if (mood == GeckoMood.Sleepy && Random.value < 0.6f)
        {
            React(anim, GeckoAction.Yawn, "line.sleepy", null);
            return;
        }
        if (mood == GeckoMood.Angry && Random.value < 0.7f)
        {
            React(anim, GeckoAction.Angry_TailFlick, "line.grumpy", null);
            Fx()?.Annoyed();
            return;
        }

        bool coin = Random.value < 0.5f;
        switch (zone)
        {
            case GeckoTouchZone.Eye:        // 크레스티드는 눈꺼풀이 없어 혀로 눈을 닦는다
                if (coin) React(anim, GeckoAction.Tongue_EyeLick, "line.eye_wipe", null);   // 핥는 소리는 GeckoFx가 낸다
                else      React(anim, GeckoAction.Refuse,         "line.eye_no",   Sfx.Refuse);
                break;

            case GeckoTouchZone.Mouth:
                var g = GameManager.Instance != null ? GameManager.Instance.GetSelectedGecko() : null;
                if (g != null && g.hunger < MOUTH_HUNGRY && Random.value < 0.6f)
                {
                    React(anim, GeckoAction.Tongue_Lick, "line.mouth_hungry", null);
                    if (_feedButton != null) StartCoroutine(PulseTab(_feedButton.transform));   // 먹이 버튼을 가리킨다
                }
                else if (coin) React(anim, GeckoAction.Tongue_Lick, "line.mouth", null);
                else           React(anim, GeckoAction.Yawn,        "line.yawn",  null);
                break;

            case GeckoTouchZone.FrontLeg:
                if (coin) React(anim, GeckoAction.Wave,     "line.wave", Sfx.Pop);
                else      React(anim, GeckoAction.PawShake, "line.paw",  Sfx.Pop);
                break;

            case GeckoTouchZone.BackLeg:
                if (coin) React(anim, GeckoAction.Jump, "line.giggle", null);   // 점프 소리는 GeckoFx가 낸다
                else      React(anim, GeckoAction.Kick, "line.kick",   Sfx.Boing);
                break;

            case GeckoTouchZone.TailBase:
                React(anim, GeckoAction.Angry_TailFlick, "line.tail_base", null);
                Fx()?.Annoyed();
                break;

            default:   // 몸통·등
                if (coin) React(anim, GeckoAction.Surprise, "line.poke",   Sfx.Pop);
                else      React(anim, GeckoAction.Shiver,   "line.shiver", Sfx.Pop);
                break;
        }
    }

    private void React(GeckoAnimatorController anim, GeckoAction action, string lineKey, Sfx? sound)
    {
        anim.TriggerAction(action);
        Fx()?.Say(Loc.Pick(lineKey));
        if (sound.HasValue) AudioManager.PlayVaried(sound.Value, 0.7f);
        Haptics.Light();
    }

    // 최근 POKE_WINDOW초 안에 건드린 횟수 (이번 포함)
    private int CountPoke()
    {
        float now = Time.unscaledTime;
        _pokeTimes.Enqueue(now);
        while (_pokeTimes.Count > 0 && now - _pokeTimes.Peek() > POKE_WINDOW) _pokeTimes.Dequeue();
        return _pokeTimes.Count;
    }

    // ── 첫 실행 부화 연출 ─────────────────────────────────────

    private bool       _hatchPending;   // 부화 연출이 끝날 때까지 사건 연출·일일 보상 팝업을 미룬다
    private HatchIntro _hatchIntro;

    private void StartHatchIntro()
    {
        if (_hatchIntro != null) return;

        var egg     = _growthStageSprites != null && _growthStageSprites.Length > 0 ? _growthStageSprites[0] : null;   // 해츨링 아이콘 = 알 그림
        var geckoRt = _geckoAnimator != null ? _geckoAnimator.transform as RectTransform : null;
        var anim    = Anim;
        _hatchIntro = HatchIntro.Create((RectTransform)transform, geckoRt, anim != null ? anim.Motor : null,
                                        _geckoMovement != null ? _geckoMovement : null, Fx(), egg, HomeFont, OnHatched);
        if (_hatchIntro == null) OnHatched();   // 게코나 알 그림이 없으면 연출 없이 넘어간다 (기록은 남긴다)
    }

    private void OnHatched()
    {
        _hatchIntro   = null;
        _hatchPending = false;
        HatchIntro.SetGeckoVisible(_geckoAnimator, true);
        if (_geckoMovement != null) _geckoMovement.enabled = true;
        if (GameManager.Instance == null) return;

        _gecko.CompleteHatchIntro();
        var g = GameManager.Instance.GetSelectedGecko();
        ShowResult(Loc.Format("hatch.born", Loc.Subject(g != null ? g.name : "")));
        StartCoroutine(OpenRewardAfterResult());
    }

    // 결과 알림이 사라진 뒤 알림 권한 → 일일 보상 팝업 (첫 실행은 부화 연출 때문에 둘 다 미뤄 두었다 — AppBootstrap 참고)
    private IEnumerator OpenRewardAfterResult()
    {
        while (_resultCoroutine != null) yield return null;
        if (GameManager.Instance != null && GameManager.Instance.Settings.GetSettings().notificationOn)
            NotificationScheduler.RequestPermission();   // Android 13+ 시스템 창
        if (GameManager.Instance != null && GameManager.Instance.Reward.CanClaim() && _rewardPanel != null)
            _rewardPanel.SetActive(true);
    }

    private void OnDisable()
    {
        if (_gecko != null)
            _gecko.OnStateChanged -= Refresh;

        if (_terrarium != null)
            _terrarium.OnTerrariumChanged -= RefreshTerrarium;

        if (_reward != null)
            _reward.OnGoalProgress -= OnGoalProgress;

        if (_geckoMovement != null)
        {
            _geckoMovement.HideChanged -= OnGeckoHideChanged;
            _geckoMovement.Perched     -= OnGeckoPerched;
            _geckoMovement.Arrived     -= OnGeckoArrived;
        }

        // 씬을 떠나면 꾸미기 편집도 끝 — 꺼지는 중이라 연출(ExitDecorEdit)은 부르지 않는다. 옮기던 위치는 DecorDragHandle이 저장
        _decorEditing = false;
        _palmRide     = false;   // 손바닥 연출은 코루틴과 함께 멈췄다 (게코는 이동 AI가 꺼지며 바닥으로)
        if (_palm != null) _palm.gameObject.SetActive(false);
        _dragSlot     = -1;

        _storeButton?.onClick.RemoveListener(OnStoreClicked);
        _geckoListButton?.onClick.RemoveListener(OnGeckoListClicked);
        _terrariumButton?.onClick.RemoveListener(OnTerrariumClicked);
        _rewardButton?.onClick.RemoveListener(OnRewardClicked);
        _settingsButton?.onClick.RemoveListener(OnSettingsClicked);

        _feedButton.onClick.RemoveListener(OnFeedClicked);
        _waterButton.onClick.RemoveListener(OnWaterClicked);
        _petButton.onClick.RemoveListener(OnPetClicked);
        _cleanButton.onClick.RemoveListener(OnCleanClicked);

        if (_presenter != null) StopCoroutine(_presenter);
        _presenter = null;
        _resultCoroutine = null;   // 비활성화되면 코루틴은 멈춘다 — 다음 진입 때 다시 시작할 수 있게
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        float dt = Time.unscaledDeltaTime;

        if (_gauges != null)
            foreach (var g in _gauges) g.Tick(dt);

        var data = GameManager.Instance.GetPlayerData();
        _coinView?.Tick(data.coin, dt);
        _gemView?.Tick(data.gem, dt);

        // 선물 상자 — 뒤 반짝이가 돌고 상자가 통통
        if (_gift != null && _gift.gameObject.activeInHierarchy && _giftGlow != null)
        {
            float t = Time.unscaledTime;
            _giftGlow.localRotation = Quaternion.Euler(0f, 0f, -40f * t);
            _giftGlow.localScale    = Vector3.one * (0.9f + 0.12f * Mathf.Sin(t * 3f));
            var box = _giftBox;
            if (box != null) box.localPosition = new Vector3(0f, GIFT_SIZE.y * 0.5f + 10f * Mathf.Abs(Mathf.Sin(t * 2.4f)), 0f);
        }
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────

    private void OnStoreClicked()     => SceneRouter.GoToStore();
    private void OnGeckoListClicked() => SceneRouter.GoToGeckoList();
    private void OnTerrariumClicked() => SceneRouter.GoToTerrarium();

    private void OnRewardClicked()
    {
        if (_rewardPanel != null) _rewardPanel.SetActive(true);
    }

    private void OnSettingsClicked()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
    }

    // ── 먹이: 선반에서 고르기 → 먹이 종류별 반응 ────────────────

    private const float FAVORITE_REACTION_WAIT = 3f;   // 받아먹는 동작이 끝나길 기다리는 최대 시간

    private FoodTray _foodTray;

    private void OnFeedClicked()
    {
        if (SceneRouter.IsTransitioning) return;

        var tray = Tray();
        if (tray != null && tray.IsOpen)
        {
            tray.Close();
            return;
        }

        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;

        var options = OwnedFoods(g);
        if (options.Count == 0)
        {
            SceneRouter.GoToStore();
            return;
        }

        if (tray == null)
        {
            FeedWith(options[0].item);   // 선반을 못 만들면 예전처럼 바로 준다
            return;
        }
        tray.Open(options, FeedWith);
    }

    private void FeedWith(ItemSO item)
    {
        var g = GameManager.Instance != null ? GameManager.Instance.GetSelectedGecko() : null;
        if (g == null || item == null) return;

        CallOutOfHide();
        switch (_gecko.FeedGecko(g.id, item, out var effect))
        {
            case CareResult.Done:
                PlayFeedReaction(item, effect);
                Haptics.Light();
                break;
            case CareResult.Refused:
                Anim?.TriggerRefuse();
                Fx()?.Refuse();
                Fx()?.Say(Loc.Pick(GeckoManager.IsUselessFood(g, item) ? "line.grown" : "line.full"));   // 다 자라서 / 배불러서
                Haptics.Light();
                break;
        }
        RefreshFeedButton();
    }

    /// <summary>먹이 종류(ItemSO.kind)와 좋아하는 먹이 여부로 반응을 고른다. 먹은 뒤에는 실제 효과를 말풍선으로</summary>
    private void PlayFeedReaction(ItemSO item, FeedEffect effect)
    {
        float sayDelay = 1.2f;
        switch (item.kind)
        {
            case FoodKind.Supplement:   // 영양제·촉진제 — 가루가 내려앉고 할짝
                Fx()?.Dust(item.growthExpGain > 0f);
                StartCoroutine(AfterDelay(0.7f, () => Anim?.TriggerLick()));
                break;
            case FoodKind.Big:          // 큰 먹이 — 받아먹고 오래 오물오물
                Anim?.TriggerFeedBig();
                Fx()?.FeedDrop(item.icon, 1.3f);
                sayDelay = 2.2f;
                break;
            default:
                Anim?.TriggerFeedCatch();
                Fx()?.FeedDrop(item.icon);
                break;
        }

        string summary = FoodTray.DescribeEffects(0f, effect.growthExp, effect.mood, effect.health, effect.moltBonus, 3, "  ");
        if (effect.favorite)
            StartCoroutine(FavoriteReaction(summary));
        else if (!string.IsNullOrEmpty(summary))
            StartCoroutine(SayLater(summary, sayDelay));
        else if (Random.value < FED_LINE_CHANCE)
            StartCoroutine(SayLater(Loc.Pick("line.fed"), sayDelay));
    }

    // 좋아하는 먹이 — 받아먹는 동작이 끝나면 폴짝 기뻐하며 하트
    private IEnumerator FavoriteReaction(string summary)
    {
        yield return new WaitForSeconds(0.3f);
        for (float t = 0f; t < FAVORITE_REACTION_WAIT && _geckoAnimator != null && _geckoAnimator.IsBusy; t += Time.deltaTime)
            yield return null;

        Anim?.TriggerHappy();
        Fx()?.Hearts();
        Fx()?.Say(string.IsNullOrEmpty(summary) ? Loc.Pick("line.favorite") : Loc.Pick("line.favorite") + "\n" + summary);
    }

    private System.Collections.Generic.List<FoodTray.Option> OwnedFoods(GeckoData g)
        => OwnedFoods(GameManager.Instance.GetPlayerData(), g);

    /// <summary>
    /// 가진 먹이 목록 — 마지막으로 준 먹이를 맨 앞에. 먹이 선반과 먹이 버튼 표시가 **같은 목록**을 쓴다
    /// (예전에는 버튼만 배를 채우는 먹이를 셌다 — 영양제만 있으면 "먹이 없음"인데 선반은 열렸다).
    /// </summary>
    public static System.Collections.Generic.List<FoodTray.Option> OwnedFoods(PlayerData data, GeckoData g)
    {
        var list = new System.Collections.Generic.List<FoodTray.Option>();
        if (data == null || data.inventory == null) return list;
        foreach (var stack in data.inventory)
        {
            if (stack.count <= 0) continue;
            var item = Resources.Load<ItemSO>($"Items/{stack.itemId}");
            if (item == null) continue;

            var option = new FoodTray.Option
            {
                item     = item,
                count    = stack.count,
                favorite = GeckoManager.IsFavoriteFood(g, item),
                grown    = GeckoManager.IsAdult(g),
                useless  = GeckoManager.IsUselessFood(g, item),
            };
            if (item.itemId == data.lastFoodItemId) list.Insert(0, option);
            else list.Add(option);
        }
        return list;
    }

    private FoodTray Tray()
    {
        if (_foodTray == null)
            _foodTray = FoodTray.Create((RectTransform)transform, HomeFont, _hungerFill != null ? _hungerFill.sprite : null);
        return _foodTray;
    }

    private static IEnumerator AfterDelay(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    private void OnWaterClicked()
    {
        if (SceneRouter.IsTransitioning) return;
        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;

        CallOutOfHide();
        switch (_gecko.GiveWater(g.id))
        {
            case CareResult.Done:
                Anim?.TriggerDrink();
                Fx()?.Mist();
                Haptics.Light();
                break;
            case CareResult.Refused:
                Anim?.TriggerRefuse();
                Fx()?.Refuse();
                Fx()?.Say(Loc.Pick("line.not_thirsty"));
                Haptics.Light();
                break;
        }
    }

    private void OnPetClicked()
    {
        if (SceneRouter.IsTransitioning) return;
        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;

        CallOutOfHide();
        switch (_gecko.Pet(g.id))
        {
            case CareResult.Done:
                var move = _geckoMovement != null ? _geckoMovement : null;
                bool onGround = move == null || (!move.IsClimbing && move.HiddenSlot < 0 && !move.IsHeld);
                if (GeckoBond.Has(g, BondPerk.Trick) && onGround && Random.value < BOND_TRICK_CHANCE)
                {
                    Anim?.TriggerAction(GeckoAction.Spin);   // 유대 Lv.4 재롱
                    AudioManager.PlayVaried(Sfx.Boing, 0.7f);
                    Fx()?.Say(Loc.Pick("line.trick"));
                }
                else
                {
                    Anim?.TriggerPet();
                    if (Random.value < PET_LINE_CHANCE) Fx()?.Say(Loc.Pick("line.pet"));
                }
                Fx()?.Hearts();
                if (GeckoBond.Has(g, BondPerk.PetLover)) StartCoroutine(AfterDelay(0.35f, () => Fx()?.Hearts()));   // 하트 더
                Haptics.Light();
                break;
            case CareResult.Annoyed:
                Anim?.TriggerAnnoyed();
                Fx()?.Annoyed();
                Fx()?.Say(Loc.Pick("line.annoyed"));
                Haptics.Medium();
                break;
        }
    }

    private void OnCleanClicked()
    {
        if (SceneRouter.IsTransitioning) return;
        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;

        CallOutOfHide();
        switch (_gecko.Clean(g.id))
        {
            case CareResult.Done:
                Anim?.TriggerClean();
                Fx()?.Sparkles();
                Haptics.Light();
                break;
            case CareResult.Refused:
                AudioManager.Play(Sfx.Pop, 0.7f);
                Fx()?.Say(Loc.Pick("line.clean"));
                break;
        }
    }

    // ── 성장·허물 사건 연출 ───────────────────────────────────

    // 사건은 GeckoEventQueue에 쌓인다 (부팅 중 생긴 것 포함). 여기서 하나씩 꺼내
    // 게코 동작 + 연출 + 결과 알림을 차례로 보여준다. 팝업이 떠 있거나 게코가 다른 동작 중이면 기다린다.
    private IEnumerator EventPresenter()
    {
        yield return new WaitForSecondsRealtime(EVENT_START_GAP);
        var poll = new WaitForSecondsRealtime(0.25f);

        while (true)
        {
            var queue = GameManager.Instance != null ? GameManager.Instance.Events : null;
            if (queue != null && queue.Count > 0 && CanPresent() && queue.TryDequeue(out var e))
                yield return Present(e);
            else if (!_greeted && CanPresent() && TryGreet())   // 유대 Lv.1 — 사건이 없으면 먼저 인사
                yield return new WaitForSecondsRealtime(1.6f);
            else if ((queue == null || queue.Count == 0) && CanPresent() && AnnounceAchievements())
                yield return new WaitForSecondsRealtime(RESULT_DISPLAY_SECONDS + 0.4f);
            else
                yield return poll;
        }
    }

    private bool CanPresent()
    {
        if (SceneRouter.IsTransitioning) return false;
        if (_hatchPending) return false;   // 첫 실행 부화 연출이 먼저
        if (_rewardPanel != null && _rewardPanel.activeInHierarchy) return false;
        if (_settingsPanel != null && _settingsPanel.activeInHierarchy) return false;
        if (_resultCoroutine != null) return false;
        if (_geckoAnimator != null && _geckoAnimator.IsBusy) return false;
        if (_foodTray != null && _foodTray.IsOpen) return false;   // 먹이를 고르는 중에는 기다린다
        return true;
    }

    private IEnumerator Present(GeckoEvent e)
    {
        var data = GameManager.Instance.GetPlayerData();
        bool isSelected = data.selectedGeckoId == e.geckoId;

        if (isSelected)
        {
            Anim?.PresentEvent(e);
            var fx = Fx();
            switch (e.type)
            {
                case GeckoEventType.GrowthUp:
                    fx?.GrowthBurst();
                    Haptics.Success();
                    _stageOverride = -1;
                    var g = GameManager.Instance.GetSelectedGecko();
                    if (g != null) RefreshGrowthInfo(g);
                    if (_growthStageText != null) StartCoroutine(Pulse(_growthStageText.transform, 1.25f));
                    StartCoroutine(SayLater(Loc.Pick("line.growth"), 1.2f));
                    if (e.growthStage >= GeckoManager.ADULT_STAGE)   // 다 자람 — 더 크게 축하
                    {
                        fx?.Hearts();
                        StartCoroutine(AfterDelay(0.6f, () => Fx()?.Sparkles()));
                    }
                    break;
                case GeckoEventType.MoltSuccess:
                    fx?.MoltFlakes(true);
                    Haptics.Success();
                    StartCoroutine(SayLater(Loc.Pick("line.molt"), 1.6f));
                    break;
                case GeckoEventType.MoltFail:
                    fx?.MoltFlakes(false);
                    Haptics.Light();
                    break;
                case GeckoEventType.MorphReveal:
                    fx?.Sparkles();
                    StartCoroutine(AfterDelay(0.5f, () => Fx()?.Sparkles()));
                    StartCoroutine(SayLater(Loc.Pick("line.morph"), 1f));
                    AudioManager.Play(Sfx.Sparkle, 0.9f);
                    Haptics.Success();
                    break;
                case GeckoEventType.BondUp:
                    fx?.Hearts();
                    StartCoroutine(AfterDelay(0.4f, () => Fx()?.Sparkles()));
                    StartCoroutine(SayLater(Loc.Pick("line.bond"), 1f));
                    AudioManager.Play(Sfx.Sparkle, 0.8f);
                    Haptics.Success();
                    var bg = GameManager.Instance.GetSelectedGecko();
                    if (bg != null) RefreshBondLabel(bg);
                    if (_bondText != null) StartCoroutine(Pulse(_bondText.transform, 1.3f));
                    break;
            }
        }
        else
        {
            AudioManager.Play(e.type == GeckoEventType.MoltFail ? Sfx.MoltFail : Sfx.Chime, 0.7f);
        }

        ShowResult(EventMessage(e));
        yield return new WaitForSecondsRealtime(RESULT_DISPLAY_SECONDS + 0.4f);

        if (e.type == GeckoEventType.GrowthUp && e.growthStage >= GeckoManager.ADULT_STAGE)
        {
            yield return SuggestNewFriend(isSelected);
            yield return AnnounceUnlockedDecor(e.adultsRaised);
        }
    }

    // 새로 달성한 업적 — 알림 + 게코 탭 통통 (보상은 게코 목록의 도감 창에서 받는다). 알렸으면 true
    private bool AnnounceAchievements()
    {
        var list = _reward != null ? _reward.TakeNewlyAchieved() : null;
        if (list == null || list.Count == 0) return false;

        var names = new System.Collections.Generic.List<string>();
        foreach (var a in list) names.Add(Loc.Get(a.NameKey));
        ShowResult(Loc.Format("achieve.done", string.Join(", ", names)));
        AudioManager.Play(Sfx.Chime, 0.6f);
        Haptics.Light();
        if (_geckoListButton != null) StartCoroutine(PulseTab(_geckoListButton.transform));
        return true;
    }

    // 어덜트 수로 새로 열린 장식 — 알림 + 꾸미기 탭 통통
    private IEnumerator AnnounceUnlockedDecor(int adultsRaised)
    {
        if (adultsRaised <= 0) yield break;
        var unlocked = TerrariumManager.NewlyUnlocked(DecorCatalog.All, adultsRaised - 1, adultsRaised);
        if (unlocked.Count == 0) yield break;

        var names = new System.Collections.Generic.List<string>();
        foreach (var item in unlocked) names.Add(Loc.DecorName(item));
        ShowResult(Loc.Format("terrarium.unlocked", string.Join(", ", names)));
        AudioManager.Play(Sfx.Sparkle, 0.7f);
        if (_terrariumButton != null) yield return PulseTab(_terrariumButton.transform);
        yield return new WaitForSecondsRealtime(RESULT_DISPLAY_SECONDS);
    }

    private const int TAB_PULSES = 3;   // 하단 탭이 통통 튀는 횟수

    // 다 자란 뒤 — 새 게코를 들이도록 권한다 (보상 코인이면 가고일도 분양 가능)
    private IEnumerator SuggestNewFriend(bool say)
    {
        if (say) Fx()?.Say(Loc.Get("line.new_friend"), 2.5f);
        if (_geckoListButton == null) yield break;
        AudioManager.Play(Sfx.Pop, 0.6f);
        yield return PulseTab(_geckoListButton.transform);
    }

    // 하단 탭을 몇 번 통통 — 눌러 보라는 신호
    private static IEnumerator PulseTab(Transform tab)
    {
        for (int i = 0; i < TAB_PULSES; i++)
        {
            yield return Pulse(tab, 1.25f);
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    // ── 오늘의 돌봄 목표 ──────────────────────────────────────

    private RewardManager _reward;

    // 목표 하나를 채우면 짧은 알림, 모두 채우면 보상 탭을 통통 (카드 숫자는 DailyGoalCard가 직접 갱신)
    private void OnGoalProgress(CareKind kind, int now, int target, bool allDone)
    {
        if (allDone)
        {
            ShowResult(Loc.Get("goal.all"));
            AudioManager.Play(Sfx.Chime, 0.6f);
            if (_rewardButton != null) StartCoroutine(PulseTab(_rewardButton.transform));
        }
        else if (now >= target)
        {
            ShowResult(Loc.Format("goal.done", Loc.Format(DailyGoalCard.RowKey(kind), now, target)));
        }
    }

    private static string EventMessage(GeckoEvent e)
    {
        switch (e.type)
        {
            case GeckoEventType.GrowthUp:
                if (e.growthStage >= GeckoManager.ADULT_STAGE)
                    return e.rewardGem > 0
                        ? Loc.Format("event.adult", Loc.Subject(e.geckoName), e.rewardCoin, e.rewardGem)
                        : Loc.Format("event.adult_coin", Loc.Subject(e.geckoName), e.rewardCoin);
                return Loc.Format("event.growth", Loc.Subject(e.geckoName),
                                  Loc.StageName(e.growthStage - 1), Loc.StageName(e.growthStage));
            case GeckoEventType.MoltSuccess:
                return Loc.Format("event.molt_success", Loc.Subject(e.geckoName), e.moltCount);
            case GeckoEventType.BondUp:
                return BondUpMessage(e);
            case GeckoEventType.MorphReveal:
                return MorphMessage(e);
            default:
                return Loc.Get("event.molt_fail");
        }
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    private void Refresh(GeckoData g)
    {
        if (g == null) return;

        // 시간 진행(30초 주기·앱 시작)은 **모든 게코**에 OnStateChanged를 보낸다 —
        // 홈은 선택한 게코만 그린다 (예전에는 목록 마지막 게코가 이름·게이지·유대·선물을 덮어썼다)
        var data = GameManager.Instance != null ? GameManager.Instance.GetPlayerData() : null;
        if (data != null && !IsHomeGecko(g, data.selectedGeckoId)) return;

        SetGauge(0, g.hunger);
        SetGauge(1, g.thirst);
        SetGauge(2, g.mood);
        SetGauge(3, g.health);
        SetGauge(4, g.cleanliness);

        if (_moltBadge != null)
            _moltBadge.SetActive(g.moltProgress >= 80f);

        if (_moltProgressFill != null)
            _moltProgressFill.fillAmount = g.moltProgress / 100f;

        RefreshGrowthInfo(g);
        RefreshFeedButton();
        RefreshGift(g);
    }

    // ── 어덜트의 선물 (RewardManager.CanGift / ClaimGift) ─────
    // 잘 지내는 어덜트가 하루 한 번 바닥에 선물 상자를 남긴다. 누르면 코인(가끔 먹이).
    // 상태가 50 아래로 떨어지면 다시 사라진다 — Refresh(상태 변화 · 30초 시간 진행)마다 확인

    private static readonly Color   GIFT_COLOR    = new Color(1f, 0.80f, 0.30f);
    private static readonly Vector2 GIFT_SIZE     = new Vector2(120f, 120f);
    private const float             GIFT_MIN_Y    = 440f;    // 바닥 앞쪽 (발 높이)
    private const float             GIFT_MAX_Y    = 640f;
    private const float             GIFT_EDGE     = 140f;    // 화면 끝 여백
    private const float             GIFT_AVOID    = 200f;    // 게코·바닥 장식과 떨어뜨리는 거리
    private static readonly System.Random s_giftRng = new System.Random();

    private RectTransform _gift;
    private Transform     _giftGlow, _giftBox;
    private string        _giftGeckoId;

    private void RefreshGift(GeckoData g)
    {
        bool show = g != null && !_hatchPending && RewardManager.CanGift(g);
        if (!show)
        {
            if (_gift != null) _gift.gameObject.SetActive(false);
            return;
        }
        if (_gift != null && _gift.gameObject.activeSelf && _giftGeckoId == g.id) return;

        EnsureGift();
        if (_gift == null) return;
        _giftGeckoId = g.id;
        _gift.anchoredPosition = GiftSpot();
        _gift.SetAsLastSibling();   // 게코 터치 영역보다 위 — 게코 옆에 있어도 눌리게
        _gift.gameObject.SetActive(true);
        AudioManager.Play(Sfx.Sparkle, 0.4f);
    }

    private void EnsureGift()
    {
        if (_gift != null) return;
        var area = _geckoAnimator != null ? _geckoAnimator.transform.parent as RectTransform : null;
        if (area == null) return;

        var go = new GameObject("AdultGift", typeof(RectTransform));
        _gift = (RectTransform)go.transform;
        _gift.SetParent(area, false);
        _gift.anchorMin = _gift.anchorMax = new Vector2(0.5f, 0f);   // 장식·게코와 같은 좌표 (아래 가운데)
        _gift.pivot     = new Vector2(0.5f, 0f);
        _gift.sizeDelta = GIFT_SIZE;

        // 뒤에서 도는 반짝이 → 상자
        var glow = new GameObject("Glow", typeof(RectTransform)).AddComponent<Image>();
        glow.transform.SetParent(_gift, false);
        glow.rectTransform.sizeDelta        = GIFT_SIZE * 1.6f;
        glow.rectTransform.anchoredPosition = new Vector2(0f, GIFT_SIZE.y * 0.1f);
        glow.sprite        = FxSprites.Sparkle;
        glow.color         = new Color(1f, 0.95f, 0.6f, 0.75f);
        glow.raycastTarget = false;
        _giftGlow = glow.transform;

        var box = new GameObject("Box", typeof(RectTransform)).AddComponent<Image>();
        box.transform.SetParent(_gift, false);
        box.rectTransform.sizeDelta = GIFT_SIZE;
        box.sprite = FxSprites.Gift;
        box.color  = GIFT_COLOR;
        _giftBox   = box.transform;

        var button = box.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OnGiftClicked);
        UIPressScale.Ensure(button);

        go.SetActive(false);
    }

    // 바닥 앞쪽 빈 곳 — 게코와 바닥 장식에서 떨어진 자리를 몇 번 찾아본다
    private Vector2 GiftSpot()
    {
        var area  = (RectTransform)_gift.parent;
        float half = Mathf.Max(0f, area.rect.width * 0.5f - GIFT_EDGE);
        var geckoRt = _geckoAnimator != null ? _geckoAnimator.transform as RectTransform : null;
        Vector2 gecko = geckoRt != null ? geckoRt.anchoredPosition : new Vector2(0f, -9999f);
        var data = _terrarium != null ? _terrarium.GetData() : null;

        Vector2 best = new Vector2(0f, GIFT_MIN_Y);
        for (int i = 0; i < 12; i++)
        {
            var p = new Vector2(Random.Range(-half, half), Random.Range(GIFT_MIN_Y, GIFT_MAX_Y));
            best = p;
            if ((p - gecko).magnitude < GIFT_AVOID) continue;
            bool near = false;
            for (int s = 0; s < TerrariumLayout.SlotCount && data != null; s++)
            {
                if (TerrariumLayout.PlacementOf(s) != DecorPlacement.Floor) continue;
                if (data.decorSlots == null || s >= data.decorSlots.Length || string.IsNullOrEmpty(data.decorSlots[s])) continue;
                if ((p - TerrariumLayout.AnchorOf(data, s)).magnitude < GIFT_AVOID) near = true;
            }
            if (!near) break;
        }
        return best;
    }

    private void OnGiftClicked()
    {
        if (_decorEditing || SceneRouter.IsTransitioning || GameManager.Instance == null) return;
        var g = GameManager.Instance.GetSelectedGecko();
        bool ok = g != null && g.id == _giftGeckoId && _reward.ClaimGift(g.id, s_giftRng, out var gift)
                  && ShowGift(g, gift);
        if (_gift != null) _gift.gameObject.SetActive(false);
        if (!ok) AudioManager.Play(Sfx.Pop, 0.6f);
    }

    private bool ShowGift(GeckoData g, RewardManager.Gift gift)
    {
        AudioManager.Play(Sfx.Sparkle, 0.9f);
        Haptics.Success();
        var fx = Fx();
        fx?.Sparkles();
        fx?.Say(Loc.Pick("line.gift"));
        var anim = Anim;
        if (anim != null && !anim.IsBusy) anim.TriggerAction(GeckoAction.Happy_LookUp);

        var food = gift.foodId != null ? Resources.Load<ItemSO>($"Items/{gift.foodId}") : null;
        ShowResult(food != null
            ? Loc.Format("gift.coin_food", g.name, gift.coin, Loc.ItemName(food))
            : Loc.Format("gift.coin", g.name, gift.coin));
        RefreshFeedButton();
        return true;
    }

    private void RefreshGrowthInfo(GeckoData g)
    {
        if (_geckoNameText != null)
            _geckoNameText.text = g.name;

        int stage = Mathf.Clamp(_stageOverride >= 0 ? _stageOverride : g.growthStage, 0, 4);

        if (_growthStageText != null)
            _growthStageText.text = Loc.StageName(stage);

        RefreshBondLabel(g);

        if (_growthStageIcon != null && _growthStageSprites != null && stage < _growthStageSprites.Length)
            _growthStageIcon.sprite = _growthStageSprites[stage];
    }

    // ── 유대 레벨 (GeckoBond) ─────────────────────────────────
    // 성장 단계 글자 오른쪽 "유대 3"(누르면 말풍선) · Lv.1 인사 · Lv.2 하트 더 · Lv.3 부르기 · Lv.4 재롱 · Lv.5 손바닥

    private const float BOND_TRICK_CHANCE = 0.25f;   // [TBD] 쓰다듬을 때 재롱 확률
    private const float BOND_LABEL_GAP    = 28f;
    private const float BOND_INFO_HOLD    = 4f;
    private const float PALM_SIZE         = 380f;    // 손 그림 크기 (다 자란 게코 기준, 원근 적용)
    private const float PALM_LIFT         = 140f;    // 손바닥이 게코를 들어 올리는 높이
    private const float PALM_STAY         = 3f;      // [TBD] 손 위에 머무는 시간
    private static readonly Color PALM_COLOR = new Color(1f, 0.86f, 0.74f);

    private TextMeshProUGUI _bondText;
    private Button          _bondButton;
    private FloorTapCatcher _floorCatcher;
    private Image           _palm;
    private bool            _palmRide;
    private bool            _greeted;

    private void RefreshBondLabel(GeckoData g)
    {
        if (_growthStageText == null || g == null) return;
        if (_bondText == null)
        {
            var go = new GameObject("BondText", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_growthStageText.transform.parent, false);
            var src = _growthStageText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(260f, src.rect.height);

            _bondText = go.AddComponent<TextMeshProUGUI>();
            _bondText.font               = _growthStageText.font;
            _bondText.fontSharedMaterial = _growthStageText.fontSharedMaterial;   // 배경 위 그림자 (ApplyHudReadability)
            _bondText.fontSize           = _growthStageText.fontSize * 0.85f;
            _bondText.fontStyle          = FontStyles.Bold;
            _bondText.color              = GAUGE_MOOD;                             // 하트 핑크
            _bondText.alignment          = TextAlignmentOptions.MidlineLeft;
            _bondText.textWrappingMode   = TextWrappingModes.NoWrap;
            SceneTextLocalizer.Ignore(_bondText);

            _bondButton = go.AddComponent<Button>();
            _bondButton.transition = Selectable.Transition.None;
            _bondButton.onClick.AddListener(OnBondInfoClicked);
            UIPressScale.Ensure(_bondButton);
        }

        _bondText.text = Loc.Format("bond.label", GeckoBond.Level(g));
        // 성장 단계 글자가 끝나는 곳 바로 오른쪽 (글자 길이가 언어·단계마다 다르다)
        var stage = _growthStageText.rectTransform;
        float stageWidth = Mathf.Min(_growthStageText.GetPreferredValues(_growthStageText.text).x, stage.rect.width);
        float left = stage.anchoredPosition.x + stage.anchorMin.x * ((RectTransform)stage.parent).rect.width;
        ((RectTransform)_bondText.transform).anchoredPosition = new Vector2(left + stageWidth + BOND_LABEL_GAP, stage.anchoredPosition.y);
        _bondText.transform.SetAsLastSibling();   // 성장 단계 글자 버튼보다 위 (누르기)
    }

    private void OnBondInfoClicked()
    {
        var g = GameManager.Instance != null ? GameManager.Instance.GetSelectedGecko() : null;
        if (g == null) return;
        Fx()?.Say(DescribeBond(g, RewardManager.TodayNumber()), BOND_INFO_HOLD);
    }

    /// <summary>"유대 Lv.3 / 다음 Lv.4 (120/180) / 풀린 것: 인사, 쓰다듬기, 부르기" (+ 오늘 한도)</summary>
    public static string DescribeBond(GeckoData g, int today)
    {
        int level = GeckoBond.Level(g);
        var sb = new System.Text.StringBuilder(Loc.Format("bond.info_title", level));
        float next = GeckoBond.NextPoints(level);
        sb.Append('\n').Append(next < 0f
            ? Loc.Get("bond.info_max")
            : Loc.Format("bond.info_next", level + 1, Mathf.FloorToInt(GeckoBond.Points(g)), Mathf.RoundToInt(next)));

        if (level == 0) sb.Append('\n').Append(Loc.Get("bond.info_none"));
        else
        {
            var perks = new System.Collections.Generic.List<string>();
            for (int lv = 1; lv <= level; lv++) perks.Add(Loc.Get("bond.perk." + lv));
            sb.Append('\n').Append(Loc.Format("bond.info_perks", string.Join(", ", perks)));
        }
        if (g.affection >= 100f && GeckoBond.TodayFull(g, today) && next >= 0f)
            sb.Append('\n').Append(Loc.Get("bond.info_today_full"));
        return sb.ToString();
    }

    /// <summary>"하코의 무늬가 드러났어요! / 할리퀸 (희귀)  새 모프! 코인 +150"</summary>
    public static string MorphMessage(GeckoEvent e)
    {
        var m = GeckoMorph.Find(e.morphId);
        string reward = e.rewardGem > 0 ? Loc.Format("achieve.reward_gem", e.rewardGem)
                      : e.rewardCoin > 0 ? Loc.Format("achieve.reward_coin", e.rewardCoin) : "";
        string tail = e.morphFirst ? Loc.Format("morph.new", reward).Trim() : "";
        return Loc.Format("event.morph", e.geckoName, Loc.Get(m.NameKey), Loc.Get(RarityKey(m.rarity)), tail).TrimEnd();
    }

    public static string RarityKey(MorphRarity r) => "morph.rarity." + (int)r;

    private static string BondUpMessage(GeckoEvent e)
    {
        string reward = e.rewardGem > 0 ? Loc.Format("achieve.reward_gem", e.rewardGem)
                      : e.rewardCoin > 0 ? Loc.Format("achieve.reward_coin", e.rewardCoin) : "";
        return Loc.Format("event.bond", Loc.Subject(e.geckoName), e.bondLevel, Loc.Get("bond.perk_desc." + e.bondLevel), reward);
    }

    // Lv.1 — 홈에 들어오면 한 번 인사 (사건 연출이 없을 때)
    private bool TryGreet()
    {
        if (_greeted) return false;
        _greeted = true;
        var g = GameManager.Instance != null ? GameManager.Instance.GetSelectedGecko() : null;
        var anim = Anim;
        if (g == null || anim == null || !GeckoBond.Has(g, BondPerk.Greet)) return false;
        anim.TriggerAction(GeckoAction.Wave);
        Fx()?.Say(Loc.Pick("line.greet"));
        AudioManager.PlayVaried(Sfx.Pop, 0.6f);
        return true;
    }

    // Lv.3 — 바닥의 빈 곳(게코가 다니는 높이까지)을 덮는 투명 판. 장식·게코보다 뒤
    private void EnsureFloorCatcher()
    {
        var move = _geckoMovement != null ? _geckoMovement : null;
        if (_floorCatcher != null || _geckoAnimator == null || move == null) return;
        var area = _geckoAnimator.transform.parent as RectTransform;
        if (area == null) return;

        var go = new GameObject("FloorTapCatcher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(area, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, move.GroundTop + 80f);   // 위쪽 이름·성장 단계 글자는 덮지 않는다
        rt.anchoredPosition = Vector2.zero;
        UpdateDepthOrder();
        rt.SetSiblingIndex(DepthGroupFirstIndex());
        go.GetComponent<Image>().color = Color.clear;

        _floorCatcher = go.AddComponent<FloorTapCatcher>();
        _floorCatcher.DoubleTapped = OnFloorDoubleTapped;
    }

    private void OnFloorDoubleTapped(UnityEngine.EventSystems.PointerEventData e)
    {
        if (_decorEditing || _hatchPending || _palmRide || SceneRouter.IsTransitioning) return;
        var g    = GameManager.Instance != null ? GameManager.Instance.GetSelectedGecko() : null;
        var move = _geckoMovement != null ? _geckoMovement : null;
        if (g == null || move == null) return;
        if (!GeckoBond.Has(g, BondPerk.Come)) return;   // 아직 부를 수 없다 — 조용히

        var area = (RectTransform)_floorCatcher.transform.parent;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out Vector2 local)) return;
        var point = new Vector2(local.x, local.y - area.rect.yMin);   // 아래 가운데 기준 (장식·게코 좌표)
        if (!move.CallTo(point)) return;

        Fx()?.Sparkles();
        AudioManager.PlayVaried(Sfx.Tap, 0.7f);
        Haptics.Light();
    }

    private void OnGeckoArrived()
    {
        var anim = Anim;
        if (anim == null) return;
        anim.TriggerAction(GeckoAction.Happy_LookUp);
        Fx()?.Say(Loc.Pick("line.come"));
    }

    // Lv.5 — 게코를 길게 누르면 아래에서 손이 올라와 게코를 들어 올린다
    private void OnGeckoLongPressed(Vector3 world)
    {
        if (_palmRide || _decorEditing || _hatchPending || SceneRouter.IsTransitioning) return;
        var g    = GameManager.Instance != null ? GameManager.Instance.GetSelectedGecko() : null;
        var move = _geckoMovement != null ? _geckoMovement : null;
        var anim = Anim;
        if (g == null || move == null || anim == null || !GeckoBond.Has(g, BondPerk.Palm)) return;
        if (anim.IsBusy || !move.Hold()) return;
        StartCoroutine(PalmRide(move, anim));
    }

    private IEnumerator PalmRide(GeckoMovementAI move, GeckoAnimatorController anim)
    {
        _palmRide = true;
        var gecko = (RectTransform)_geckoAnimator.transform;
        Vector2 home = gecko.anchoredPosition;
        float   size = PALM_SIZE * move.DepthScaleFor(home.y);

        EnsurePalm(gecko.parent);
        var palm = _palm.rectTransform;
        palm.sizeDelta = new Vector2(size, size);
        palm.gameObject.SetActive(true);
        Vector2 below = home + new Vector2(0f, -size * 0.9f);
        UpdateDepthOrder();   // 손은 게코 바로 뒤

        // ① 아래에서 손이 올라온다
        yield return SlidePalm(palm, below, home, 0.35f, null);
        // ② 게코가 폴짝 — 뛰는 동안 손과 함께 들어 올린다
        anim.TriggerAction(GeckoAction.Jump);
        AudioManager.PlayVaried(Sfx.Boing, 0.6f);
        yield return new WaitForSeconds(0.3f);
        Vector2 up = home + new Vector2(0f, PALM_LIFT);
        yield return SlidePalm(palm, home, up, 0.5f, gecko);
        Fx()?.Hearts();
        Fx()?.Say(Loc.Pick("line.palm"));
        Haptics.Success();
        for (float t = 0f; t < PALM_STAY; t += Time.deltaTime)
        {
            if (!isActiveAndEnabled) yield break;
            float bob = Mathf.Sin(t * 2.2f) * 6f;
            palm.anchoredPosition  = up + new Vector2(0f, bob);
            gecko.anchoredPosition = up + new Vector2(0f, bob);
            yield return null;
        }
        // ③ 내려놓고 손이 내려간다
        yield return SlidePalm(palm, up, home, 0.5f, gecko);
        yield return SlidePalm(palm, home, below, 0.3f, null);
        EndPalmRide(move, home);
    }

    private IEnumerator SlidePalm(RectTransform palm, Vector2 from, Vector2 to, float time, RectTransform carry)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float u = t / time;
            u = u * u * (3f - 2f * u);
            var p = Vector2.Lerp(from, to, u);
            SetPalmPosition(palm, p);
            if (carry != null) carry.anchoredPosition = p;
            yield return null;
        }
        SetPalmPosition(palm, to);
        if (carry != null) carry.anchoredPosition = to;
    }

    // 손바닥 윗면이 p(게코 발 높이)에 오게
    private static void SetPalmPosition(RectTransform palm, Vector2 p) => palm.anchoredPosition = p;

    private void EndPalmRide(GeckoMovementAI move, Vector2 home)
    {
        if (_geckoAnimator != null) ((RectTransform)_geckoAnimator.transform).anchoredPosition = home;
        if (_palm != null) _palm.gameObject.SetActive(false);
        _palmRide = false;
        if (move != null) move.Release();
    }

    private void EnsurePalm(Transform area)
    {
        if (_palm != null) return;
        var go = new GameObject("BondPalm", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _palm = go.GetComponent<Image>();
        var rt = _palm.rectTransform;
        rt.SetParent(area, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);   // 게코와 같은 좌표
        rt.pivot     = new Vector2(0.5f, FxSprites.HAND_PALM_TOP);
        _palm.sprite        = FxSprites.Hand;
        _palm.color         = PALM_COLOR;
        _palm.raycastTarget = false;
        go.SetActive(false);
    }

    // ── 다음 성장 조건 ────────────────────────────────────────

    private const float GROWTH_INFO_HOLD = 4f;   // 말풍선 유지 시간 (여러 줄이라 평소보다 길게)

    private Button _growthInfoButton;

    // 성장 단계 글자를 누르면 다음 성장 조건을 말풍선으로 보여준다 — 왜 아직 안 크는지 알 수 있게
    private void EnsureGrowthInfoButton()
    {
        if (_growthInfoButton != null || _growthStageText == null) return;

        _growthStageText.raycastTarget = true;
        _growthInfoButton = _growthStageText.GetComponent<Button>();
        if (_growthInfoButton == null) _growthInfoButton = _growthStageText.gameObject.AddComponent<Button>();
        _growthInfoButton.transition = Selectable.Transition.None;
        _growthInfoButton.onClick.AddListener(OnGrowthInfoClicked);
        UIPressScale.Ensure(_growthInfoButton);
    }

    private void OnGrowthInfoClicked()
    {
        if (GameManager.Instance == null || _gecko == null) return;
        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;
        Fx()?.Say(DescribeGrowth(_gecko.GetGrowthCheck(g.id)), GROWTH_INFO_HOLD);
    }

    /// <summary>"다음 성장: 서브어덜트 / 나이 7일 - 충족 / 건강 50 - 부족 (지금 10)" — 그 단계에 있는 조건만</summary>
    public static string DescribeGrowth(GrowthCheck c)
    {
        if (c.IsAdult) return Loc.Get("growth.adult");

        var sb = new System.Text.StringBuilder(Loc.Format("growth.next", Loc.StageName(c.nextStage)));
        AppendRequirement(sb, Loc.Format("growth.req_age", Mathf.RoundToInt(c.needDays)), c.DaysMet, Mathf.FloorToInt(c.ageDays));
        if (c.needMolts > 0)
            AppendRequirement(sb, Loc.Format("growth.req_molt", c.needMolts), c.MoltsMet, c.moltCount);
        if (c.needHealth > 0f)
            AppendRequirement(sb, Loc.Format("growth.req_health", Mathf.RoundToInt(c.needHealth)), c.HealthMet, Mathf.FloorToInt(c.health));
        if (c.needAffection > 0f)
            AppendRequirement(sb, Loc.Format("growth.req_affection", Mathf.RoundToInt(c.needAffection)), c.AffectionMet, Mathf.FloorToInt(c.affection));
        return sb.ToString();
    }

    // 지금 값은 내림 — 49.6을 "부족 (지금 50)"으로 보이지 않게
    private static void AppendRequirement(System.Text.StringBuilder sb, string label, bool met, int now)
        => sb.Append('\n').Append(met ? Loc.Format("growth.met", label) : Loc.Format("growth.unmet", label, now));

    // 홈 화면 글자에 쓰는 TMP 글꼴 (한글이 들어 있는 글꼴) — 게이지 숫자·말풍선이 같이 쓴다
    private TMP_FontAsset HomeFont
        => _geckoNameText != null ? _geckoNameText.font : (_resultText != null ? _resultText.font : null);

    private void InitViews()
    {
        if (_gauges == null)
        {
            var font = HomeFont;
            _gauges = new[]
            {
                new GaugeView(_hungerFill,      WARNING_THRESHOLD, GAUGE_HUNGER, font),
                new GaugeView(_thirstFill,      WARNING_THRESHOLD, GAUGE_THIRST, font),
                new GaugeView(_moodFill,        WARNING_THRESHOLD, GAUGE_MOOD,   font),
                new GaugeView(_healthFill,      20f,               GAUGE_HEALTH, font),
                new GaugeView(_cleanlinessFill, 20f,               GAUGE_CLEAN,  font),
            };
        }
        if (_coinView == null) _coinView = new CountView(_coinText, "hud.coin", playSound: true);
        if (_gemView  == null) _gemView  = new CountView(_gemText,  "hud.gem",  playSound: true);
    }

    // 홈에 들어온 순간에는 애니메이션 없이 바로 현재 값을 보여준다
    private void SnapViews()
    {
        if (_gauges != null)
            foreach (var g in _gauges) g.Snap();

        var data = GameManager.Instance.GetPlayerData();
        _coinView?.Snap(data.coin);
        _gemView?.Snap(data.gem);
    }

    private void SetGauge(int index, float value)
    {
        if (_gauges == null) InitViews();
        _gauges[index].SetTarget(value);
    }

    // 먹이 버튼 — 선반에 나오는 목록과 같은 기준으로 보여 준다 (맨 앞 = 마지막으로 준 먹이)
    private void RefreshFeedButton()
    {
        var g       = GameManager.Instance != null ? GameManager.Instance.GetSelectedGecko() : null;
        var options = OwnedFoods(GameManager.Instance != null ? GameManager.Instance.GetPlayerData() : null, g);
        bool hasFeed = options.Count > 0;

        // 투명도
        var group = _feedButton.GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = hasFeed ? 1f : 0.5f;

        // 아이템 이름 + 수량 텍스트
        if (_feedItemText != null)
            _feedItemText.text = hasFeed
                ? $"{Loc.ItemName(options[0].item)} x{options[0].count}"
                : Loc.Get("home.no_food");
    }

    // ── 결과 알림 ─────────────────────────────────────────────

    private void ShowResult(string message)
    {
        if (_resultPanel == null || _resultText == null) return;

        if (_resultCoroutine != null)
            StopCoroutine(_resultCoroutine);

        _resultText.text = message;
        _resultPanel.SetActive(true);
        _resultCoroutine = StartCoroutine(HideResultAfterDelay());
    }

    private IEnumerator HideResultAfterDelay()
    {
        // 톡 튀어나오며 등장
        var t = _resultPanel.transform;
        for (float s = 0f; s < 0.25f; s += Time.unscaledDeltaTime)
        {
            float u = s / 0.25f;
            t.localScale = Vector3.one * EaseOutBack(u);
            yield return null;
        }
        t.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(RESULT_DISPLAY_SECONDS);

        for (float s = 0f; s < 0.15f; s += Time.unscaledDeltaTime)
        {
            t.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, s / 0.15f);
            yield return null;
        }
        t.localScale = Vector3.one;

        if (_resultPanel != null)
            _resultPanel.SetActive(false);
        _resultCoroutine = null;
    }

    // ── 테라리움 비주얼 갱신 ──────────────────────────────────

    private void RefreshTerrarium()
    {
        if (_terrarium == null) return;
        var data = _terrarium.GetData();

        ApplyDecorSprite(_backgroundImage, data.backgroundId);   // 테마 — 뒷벽과 바닥이 한 장

        // 테마는 불투명하게 (2026-09-21) — 씬의 Background 이미지가 알파 0.59라 뒤의 카메라 하늘색(파랑)이 41% 비쳐,
        // 정글 잎은 어둡고 푸르게, 흙 바닥은 보랏빛 회색으로 보였다. 어둡게 하고 싶으면 알파가 아니라 색(검정 쪽)으로 한다
        if (_backgroundImage != null) _backgroundImage.color = Color.white;

        // 바닥 띠는 쓰지 않는다 (2026-09-21) — 높이 150짜리가 하단 탭(120)·돌봄 버튼에 가려 거의 안 보였고,
        // 게코가 걷는 발 높이 380~950은 테마 그림의 바닥 부분이다
        if (_floorImage != null) _floorImage.gameObject.SetActive(false);

        // 장식 칸 — 자리·크기를 정하고, 게코가 쓸 수 있는 구조물을 이동 AI에 넘긴다
        var structures = new System.Collections.Generic.List<GeckoMovementAI.Structure>();
        if (_decorImages != null)
        {
            for (int i = 0; i < _decorImages.Length; i++)
            {
                var    image   = _decorImages[i];
                string slotId  = data.decorSlots != null && i < data.decorSlots.Length ? data.decorSlots[i] : null;
                bool   hasItem = !string.IsNullOrEmpty(slotId);
                if (image == null) continue;

                image.gameObject.SetActive(hasItem);
                if (!hasItem) continue;

                ApplyDecorSprite(image, slotId);
                var item = FindDecor(slotId);
                if (item == null || i >= TerrariumLayout.SlotCount) continue;

                Vector2 anchor = TerrariumLayout.AnchorOf(data, i);   // 옮겼으면 저장된 위치
                PlaceDecorImage(image, item, anchor);
                EnsureDecorInput(image, i);
                if (item.use != DecorUse.None && TerrariumManager.Fits(item, i))
                    structures.Add(new GeckoMovementAI.Structure { slot = i, use = item.use, anchor = anchor });
            }
        }

        var move = _geckoMovement != null ? _geckoMovement : null;
        if (move != null) move.SetStructures(structures);
        if (_decorEditing) SetDecorHighlight(true);   // 편집 중 새로 켜진 그림에도 테두리
    }

    // ── 꾸미기 구조물 (TerrariumLayout) ───────────────────────

    private bool[] _decorInputReady;

    // 씬의 DecorSlot0~3 뒤에 늘어난 칸(바닥 4·5, 뒷벽 6)의 그림을 같은 종류 칸을 복제해 만든다.
    // 입력·테두리가 붙기 전(첫 RefreshTerrarium 전)에 불러야 복제본에 딸려 가지 않는다
    private void EnsureDecorImages()
    {
        if (_decorImages == null || _decorImages.Length >= TerrariumLayout.SlotCount) return;

        var images = new Image[TerrariumLayout.SlotCount];
        System.Array.Copy(_decorImages, images, _decorImages.Length);
        for (int i = _decorImages.Length; i < images.Length; i++)
        {
            Image template = null;
            for (int j = 0; j < _decorImages.Length && template == null; j++)
                if (_decorImages[j] != null && TerrariumLayout.PlacementOf(j) == TerrariumLayout.PlacementOf(i))
                    template = _decorImages[j];
            if (template == null) continue;

            var copy = Instantiate(template, template.transform.parent);
            copy.name = "DecorSlot" + i;
            copy.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);   // 순서는 UpdateDepthOrder가 다시 정한다
            copy.gameObject.SetActive(false);
            images[i] = copy;
        }
        _decorImages = images;
    }

    // ── 화면 분위기 (2026-09-18) ──────────────────────────────
    // 비네트·먼지·앞 잎사귀는 TerrariumAtmosphere, 공기 원근 색은 이동 AI의 설정(farTint)을 같이 쓴다

    [Header("화면 분위기 연출")]
    [Tooltip("끄면 비네트·먼지·앞 잎사귀가 나오지 않는다 (공기 원근은 이동 AI의 Far Tint)")]
    [SerializeField] private bool _atmosphere = true;

    private TerrariumAtmosphere _atmosphereFx;

    private void EnsureAtmosphere()
    {
        if (_atmosphereFx != null || !_atmosphere || _geckoAnimator == null) return;
        var area = _geckoAnimator.transform.parent as RectTransform;
        if (area == null) return;
        UpdateDepthOrder();
        _atmosphereFx = TerrariumAtmosphere.Create(area, DepthGroupFirstIndex());   // 비네트는 장식·게코 뒤
    }

    /// <summary>발 높이에 따른 공기 원근 색 — 게코와 같은 규칙 (이동 AI가 없으면 그대로)</summary>
    private Color HazeTint(float footY)
    {
        var move = _geckoMovement != null ? _geckoMovement : null;
        return move != null ? move.DepthTintFor(footY) : Color.white;
    }

    // ── 앞뒤 겹침 순서 ────────────────────────────────────────
    // 뒷벽 구조물은 늘 맨 뒤, 바닥 장식과 게코는 발 높이(y)가 클수록(= 뒤쪽) 먼저 그린다.
    // 게코가 숨은 은신처는 게코 바로 앞 (꼬리만 삐죽). 게코 터치 영역은 게코 바로 뒤 순서라
    // 게코 앞에 놓인 장식을 누르면 장식이 눌린다. 순서가 바뀔 때만 SetSiblingIndex

    private struct DepthEntry
    {
        public Transform t;
        public int       band;    // 0 = 뒷벽, 1 = 바닥·게코
        public float     y;       // 클수록 뒤
        public int       order;   // 같은 y일 때 — 칸 번호, 게코는 뒤
    }

    private readonly System.Collections.Generic.List<DepthEntry> _depth = new System.Collections.Generic.List<DepthEntry>();
    private readonly System.Collections.Generic.List<Transform>  _depthOrder = new System.Collections.Generic.List<Transform>();

    private static readonly System.Comparison<DepthEntry> DEPTH_COMPARE = (a, b) =>
        a.band != b.band ? a.band.CompareTo(b.band)
        : a.y != b.y ? b.y.CompareTo(a.y)
        : a.order.CompareTo(b.order);

    private void LateUpdate()
    {
        UpdateDepthOrder();
    }

    private void UpdateDepthOrder()
    {
        if (_decorImages == null || _geckoAnimator == null) return;
        var gecko  = (RectTransform)_geckoAnimator.transform;
        var parent = gecko.parent;
        var move   = _geckoMovement != null ? _geckoMovement : null;
        int hidden = move != null ? move.HiddenSlot : -1;

        _depth.Clear();
        _depth.Add(new DepthEntry { t = gecko, band = 1, y = gecko.anchoredPosition.y, order = 100 });
        for (int i = 0; i < _decorImages.Length; i++)
        {
            var image = _decorImages[i];
            if (image == null || image.transform.parent != parent || !image.gameObject.activeSelf) continue;
            bool wall = TerrariumLayout.PlacementOf(i) == DecorPlacement.Wall;
            float y   = image.rectTransform.anchoredPosition.y;          // 바닥 장식 피벗 = 바닥에 닿는 곳 (PlaceDecorImage)
            if (i == hidden) y = gecko.anchoredPosition.y - 0.5f;          // 숨은 게코를 가린다
            _depth.Add(new DepthEntry { t = image.transform, band = wall ? 0 : 1, y = y, order = i });
        }
        _depth.Sort(DEPTH_COMPARE);

        _depthOrder.Clear();
        foreach (var e in _depth)
        {
            if (e.t == gecko && _palm != null && _palm.gameObject.activeSelf && _palm.transform.parent == parent)
                _depthOrder.Add(_palm.transform);   // 손바닥은 게코 바로 뒤
            _depthOrder.Add(e.t);
            if (e.t == gecko && _geckoTouch != null && _geckoTouch.transform.parent == parent)
                _depthOrder.Add(_geckoTouch.transform);
        }

        int first = int.MaxValue;
        foreach (var t in _depthOrder) first = Mathf.Min(first, t.GetSiblingIndex());
        for (int i = 0; i < _depthOrder.Count; i++)
            if (_depthOrder[i].GetSiblingIndex() != first + i) _depthOrder[i].SetSiblingIndex(first + i);
    }

    // 장식·게코 무리의 맨 앞 순서 (편집 판을 그 바로 뒤에 깐다)
    private int DepthGroupFirstIndex()
    {
        int first = _geckoAnimator != null ? _geckoAnimator.transform.GetSiblingIndex() : int.MaxValue;
        if (_decorImages != null)
            foreach (var image in _decorImages)
                if (image != null) first = Mathf.Min(first, image.transform.GetSiblingIndex());
        return first;
    }

    private DecorItemSO FindDecor(string itemId)
    {
        if (_allDecorItems != null)
            foreach (var item in _allDecorItems)
                if (item != null && item.itemId == itemId) return item;
        return DecorCatalog.Find(itemId);
    }

    private DecorItemSO DecorAt(int slot)
    {
        var slots = _terrarium != null ? _terrarium.GetData().decorSlots : null;
        return slots != null && slot >= 0 && slot < slots.Length ? FindDecor(slots[slot]) : null;
    }

    // 발 높이의 원근 크기 — 게코와 같은 규칙 (이동 AI가 없으면 1)
    private float DecorDepth(float footY)
    {
        var move = _geckoMovement != null ? _geckoMovement : null;
        return move != null ? move.DepthScaleFor(footY) : 1f;
    }

    // 기준점에 맞춰 자리·크기·피벗. 바닥 장식은 뒤로 갈수록 작아진다
    private void PlaceDecorImage(Image image, DecorItemSO item, Vector2 anchor)
    {
        TerrariumLayout.ImagePlacement(item, anchor, out Vector2 position, out Vector2 pivot, out bool flipX);
        float scale = item.placement == DecorPlacement.Floor ? DecorDepth(anchor.y) : 1f;
        var rt = image.rectTransform;
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot            = pivot;
        rt.sizeDelta        = TerrariumLayout.ImageSize(item.use);
        rt.anchoredPosition = position;
        rt.localScale       = new Vector3(flipX ? -scale : scale, scale, 1f);
        image.color          = HazeTint(item.placement == DecorPlacement.Floor ? anchor.y : float.MaxValue);   // 공기 원근
        image.preserveAspect = true;
        image.raycastTarget  = true;   // 모든 장식을 길게 눌러 옮길 수 있게 (게코 터치 영역은 장식보다 위 순서라 가려지지 않는다)
    }

    // 누르기 입력 — 길게 누르기·끌기(모든 장식), 짧게 누르기(바닥 장식: 숨은 게코 불러내기)
    private void EnsureDecorInput(Image image, int slot)
    {
        _decorInputReady ??= new bool[TerrariumLayout.SlotCount];
        if (_decorInputReady[slot]) return;
        _decorInputReady[slot] = true;

        var handle = image.GetComponent<DecorDragHandle>();
        if (handle == null) handle = image.gameObject.AddComponent<DecorDragHandle>();
        handle.Slot        = slot;
        handle.LongPressed = OnDecorLongPressed;
        handle.CanDrag     = _ => _decorEditing;
        handle.DragBegan   = OnDecorDragBegan;
        handle.Dragged     = OnDecorDragged;
        handle.DragEnded   = OnDecorDragEnded;

        if (TerrariumLayout.PlacementOf(slot) != DecorPlacement.Floor) return;
        var button = image.GetComponent<Button>();
        if (button == null)
        {
            button = image.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
        }
        int captured = slot;
        button.onClick.AddListener(() => OnDecorTouched(captured));
    }

    // ── 장식 옮기기 (편집 모드) ───────────────────────────────
    // 장식을 길게 누르면 시작 — 게코가 멈추고 장식에 테두리, 끌어서 범위 안으로 옮긴다. 빈 곳을 누르면 끝.
    // 옮길 수 있는 범위·간격은 TerrariumLayout.ClampAnchor · TooClose, 저장은 TerrariumManager.SetDecorPosition

    private static readonly Color   DECOR_EDIT_OUTLINE      = new Color(1f, 0.88f, 0.35f, 0.95f);
    private static readonly Vector2 DECOR_EDIT_OUTLINE_SIZE = new Vector2(5f, -5f);
    private static readonly Color   DECOR_EDIT_DIM          = new Color(0f, 0f, 0f, 0.12f);   // 편집 중 배경을 살짝 어둡게

    private bool       _decorEditing;
    private GameObject _decorEditBlocker;
    private int        _dragSlot = -1;
    private Vector2    _dragStartAnchor, _dragStartLocal, _dragAnchor;

    private void OnDecorLongPressed(int slot)
    {
        if (_decorEditing || _hatchPending || _palmRide || SceneRouter.IsTransitioning) return;
        EnterDecorEdit();
    }

    private void EnterDecorEdit()
    {
        _decorEditing = true;
        var move = _geckoMovement != null ? _geckoMovement : null;
        if (move != null) move.enabled = false;                            // 게코는 멈춘다 (집·벽에 있었으면 바닥으로)
        if (_geckoTouch != null) _geckoTouch.gameObject.SetActive(false);  // 게코에 가린 장식도 잡을 수 있게
        if (_floorCatcher != null) _floorCatcher.gameObject.SetActive(false);
        EnsureDecorEditBlocker();
        if (_decorEditBlocker != null) _decorEditBlocker.SetActive(true);
        SetDecorHighlight(true);
        ShowResult(Loc.Get("terrarium.edit_hint"));
        AudioManager.Play(Sfx.Pop, 0.8f);
        Haptics.Medium();
    }

    private void ExitDecorEdit()
    {
        if (!_decorEditing) return;
        _decorEditing = false;
        _dragSlot     = -1;
        if (_decorEditBlocker != null) _decorEditBlocker.SetActive(false);
        SetDecorHighlight(false);
        if (_geckoTouch != null) _geckoTouch.gameObject.SetActive(true);
        if (_floorCatcher != null) _floorCatcher.gameObject.SetActive(true);
        var move = _geckoMovement != null ? _geckoMovement : null;
        if (move != null) move.enabled = !_hatchPending;
        RefreshTerrarium();   // 옮긴 위치로 게코 경로도 새로
        ShowResult(Loc.Get("terrarium.edit_saved"));
        AudioManager.Play(Sfx.Sparkle, 0.6f);
        Haptics.Light();
    }

    // 빈 곳 누르기 = 편집 끝 — 장식들 바로 뒤(배경 위)에 깔리는 투명한 판
    private void EnsureDecorEditBlocker()
    {
        if (_decorEditBlocker != null || _decorImages == null) return;

        Transform area = null;
        foreach (var image in _decorImages)
            if (image != null) area = image.transform.parent;
        if (area == null) return;
        UpdateDepthOrder();                    // 장식·게코를 한데 모은 뒤
        int first = DepthGroupFirstIndex();    // 게코가 맨 뒤에 있어도 판이 무리 전체의 뒤에 깔리게

        var go = new GameObject("DecorEditBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(area, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.SetSiblingIndex(first);
        go.GetComponent<Image>().color = DECOR_EDIT_DIM;

        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(ExitDecorEdit);
        go.SetActive(false);
        _decorEditBlocker = go;
    }

    private void SetDecorHighlight(bool on)
    {
        if (_decorImages == null) return;
        foreach (var image in _decorImages)
        {
            if (image == null) continue;
            var outline = image.GetComponent<Outline>();
            if (outline == null)
            {
                if (!on) continue;
                outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor    = DECOR_EDIT_OUTLINE;
                outline.effectDistance = DECOR_EDIT_OUTLINE_SIZE;
            }
            outline.enabled = on;
        }
    }

    private void OnDecorDragBegan(int slot, UnityEngine.EventSystems.PointerEventData e)
    {
        if (!_decorEditing || e == null || _terrarium == null || _decorImages == null || _decorImages[slot] == null) return;
        var area = _decorImages[slot].rectTransform.parent as RectTransform;
        if (area == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out _dragStartLocal)) return;

        _dragSlot        = slot;
        _dragStartAnchor = _dragAnchor = TerrariumLayout.AnchorOf(_terrarium.GetData(), slot);
        AudioManager.Play(Sfx.Tap, 0.5f);
    }

    private void OnDecorDragged(int slot, UnityEngine.EventSystems.PointerEventData e)
    {
        if (slot != _dragSlot || e == null) return;
        var image = _decorImages[slot];
        var area  = image != null ? image.rectTransform.parent as RectTransform : null;
        var item  = DecorAt(slot);
        if (area == null || item == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out Vector2 local)) return;

        Vector2 want = TerrariumLayout.ClampAnchor(item, _dragStartAnchor + (local - _dragStartLocal), area.rect.width, DecorDepth);
        if (TooCloseToOtherDecor(slot, item, want)) return;   // 다른 장식에 너무 붙으면 더 가지 않는다

        _dragAnchor = want;
        PlaceDecorImage(image, item, want);
    }

    private void OnDecorDragEnded(int slot, UnityEngine.EventSystems.PointerEventData e)
    {
        if (slot != _dragSlot) return;
        _dragSlot = -1;
        if (_terrarium != null) _terrarium.SetDecorPosition(slot, _dragAnchor);   // 저장 → 다시 그림 (OnTerrariumChanged)
        if (!isActiveAndEnabled) return;
        AudioManager.Play(Sfx.Pop, 0.6f);
        Haptics.Light();
    }

    private bool TooCloseToOtherDecor(int slot, DecorItemSO item, Vector2 anchor)
    {
        var data = _terrarium.GetData();
        for (int i = 0; i < TerrariumLayout.SlotCount; i++)
        {
            if (i == slot || data.decorSlots == null || i >= data.decorSlots.Length || string.IsNullOrEmpty(data.decorSlots[i])) continue;
            if (TerrariumLayout.PlacementOf(i) != item.placement) continue;
            if (TerrariumLayout.TooClose(item.placement, anchor, TerrariumLayout.AnchorOf(data, i))) return true;
        }
        return false;
    }

    // 은신처를 누르면 — 숨어 있던 게코가 "누구야?" 하고 나온다
    private void OnDecorTouched(int slot)
    {
        var move = _geckoMovement != null ? _geckoMovement : null;
        if (_decorEditing || move == null || move.HiddenSlot != slot || SceneRouter.IsTransitioning || _hatchPending) return;
        move.ComeOut();
        Fx()?.Say(Loc.Pick("line.peek"));
        AudioManager.PlayVaried(Sfx.Pop, 0.8f);
        Haptics.Light();
    }

    // 은신처에 들어가면 집 그림을 게코·터치 영역 바로 앞으로 (꼬리만 삐죽, 집을 누를 수 있게) — 순서는 UpdateDepthOrder
    private void OnGeckoHideChanged(int slot, bool inside) => UpdateDepthOrder();

    private void OnGeckoPerched()
    {
        if (Random.value < 0.5f) Fx()?.Say(Loc.Pick("line.perch"));
    }

    // 돌봄 버튼 — 은신처에 있으면 먼저 나온다 (반응 동작은 바로, 걸어 나오기는 동작이 끝난 뒤)
    private void CallOutOfHide()
    {
        var move = _geckoMovement != null ? _geckoMovement : null;
        if (move != null && move.HiddenSlot >= 0) move.ComeOut();
    }

    private void ApplyDecorSprite(Image target, string itemId)
    {
        if (target == null) return;
        if (string.IsNullOrEmpty(itemId))
        {
            target.gameObject.SetActive(false);
            return;
        }

        // Inspector 배열 우선 탐색, 없으면 Resources 폴더에서 자동 로드
        DecorItemSO found = null;
        if (_allDecorItems != null)
            foreach (var item in _allDecorItems)
                if (item != null && item.itemId == itemId) { found = item; break; }

        if (found == null)
            found = Resources.Load<DecorItemSO>($"Decor/{itemId}");

        if (found == null)
        {
            Debug.LogWarning($"[HomeUIController] DecorItemSO를 찾을 수 없음 — {itemId}");
            target.gameObject.SetActive(false);
            return;
        }

        Sprite sprite = found.previewSprite != null ? found.previewSprite : found.icon;
        target.sprite = sprite;
        target.gameObject.SetActive(sprite != null);
    }

    // ── 돌봄 버튼 아이콘 ──────────────────────────────────────

    private const float CARE_ICON_SIZE = 64f;

    /// <summary>
    /// 둥근 돌봄 버튼 위쪽에 아이콘을 붙인다 (아래쪽은 글자). 씬에 오브젝트를 늘리지 않으려고 실행 중에 한 번만 만든다.
    /// 그림 교체 = Inspector의 _careButtonIcons 교체.
    /// </summary>
    private void EnsureCareButtonIcons()
    {
        var buttons = new[] { _feedButton, _waterButton, _petButton, _cleanButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (button == null || _careButtonIcons == null || i >= _careButtonIcons.Length || _careButtonIcons[i] == null) continue;
            if (button.transform.Find("Icon") != null) continue;

            var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(button.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.66f);
            rt.sizeDelta = new Vector2(CARE_ICON_SIZE, CARE_ICON_SIZE);

            var image = go.GetComponent<Image>();
            image.sprite         = _careButtonIcons[i];
            image.preserveAspect = true;
            image.raycastTarget  = false;
        }
    }


    // ── 하단 탭 아이콘 ────────────────────────────────────────

    private const float NAV_ICON_SIZE = 38f;   // 씬 Emoji 칸(LayoutElement preferredHeight)과 같게

    /// <summary>
    /// 하단 탭 5개의 빈 `Emoji` 칸에 아이콘 그림을 넣는다 (아래는 글자 그대로).
    /// 이모지·기호는 글꼴 아틀라스에 없어 □로 나오기 때문에 그림으로 넣는다 (2026-09-21).
    /// 그림 교체 = `Resources/Icons/tab_*.png`를 같은 크기로 바꿔 끼우기 (`NavIconArt`가 임시 그림을 만든다).
    /// </summary>
    public  static readonly string[] NAV_ICONS = { "tab_store", "tab_gecko", "tab_terrarium", "tab_reward", "tab_settings" };

    private void EnsureNavButtonIcons()
    {
        var buttons = new[] { _storeButton, _geckoListButton, _terrariumButton, _rewardButton, _settingsButton };
        for (int i = 0; i < buttons.Length && i < NAV_ICONS.Length; i++)
        {
            var button = buttons[i];
            if (button == null) continue;

            // 씬의 빈 글자 칸 안에 넣는다 — 세로 배치(글자가 아래)를 그대로 쓰려고
            var slot = button.transform.Find("Emoji");
            var host = slot != null ? slot : button.transform;
            if (host.Find("Icon") != null) continue;

            var sprite = Resources.Load<Sprite>($"Icons/{NAV_ICONS[i]}");
            if (sprite == null) continue;   // 그림이 아직 없으면 지금처럼 글자만

            var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(host, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(NAV_ICON_SIZE, NAV_ICON_SIZE);

            var image = go.GetComponent<Image>();
            image.sprite         = sprite;
            image.preserveAspect = true;
            image.raycastTarget  = false;
        }
    }
    // ── 배경 위 글자 읽기 쉽게 ────────────────────────────────

    // 부드러운 어두운 그림자(TMP underlay) 수치 — 글꼴 SDF 단위 [TBD]
    private static readonly Color HUD_SHADOW_COLOR = new Color(0f, 0f, 0f, 0.75f);
    private const float HUD_SHADOW_OFFSET   = 0.4f;
    private const float HUD_SHADOW_DILATE   = 0.4f;
    private const float HUD_SHADOW_SOFTNESS = 0.5f;

    private static Material s_hudTextMaterial;

    /// <summary>
    /// 정글 배경 그림 위에 바로 놓인 흰 글자(윗줄 재화·이름·성장 단계·게이지 이름과 숫자)에 그림자를 넣는다.
    /// 판을 깔 수 없는 자리라서 글자 자체를 읽기 쉽게 한다. 버튼 글자는 어두운 바탕이 있어 그대로 둔다.
    /// </summary>
    private void ApplyHudReadability()
    {
        ReadableText(_coinText);
        ReadableText(_gemText);
        ReadableText(_geckoNameText);
        ReadableText(_growthStageText);

        // 게이지 이름·숫자 — 숫자는 InitViews가 만든 뒤라야 함께 바뀐다
        Transform row   = _hungerFill != null ? _hungerFill.transform.parent : null;
        Transform panel = row != null ? row.parent : null;
        if (panel != null)
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
                ReadableText(text);
    }

    // 같은 글꼴이면 머티리얼 하나를 같이 쓴다 (글자마다 복사하지 않음)
    private static void ReadableText(TMP_Text text)
    {
        if (text == null) return;
        var baseMaterial = text.fontSharedMaterial;
        if (baseMaterial == null || baseMaterial == s_hudTextMaterial) return;

        if (s_hudTextMaterial == null || s_hudTextMaterial.mainTexture != baseMaterial.mainTexture)
        {
            ShaderUtilities.GetShaderPropertyIDs();
            s_hudTextMaterial = new Material(baseMaterial) { name = baseMaterial.name + " (HUD shadow)" };
            s_hudTextMaterial.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            s_hudTextMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, HUD_SHADOW_COLOR);
            s_hudTextMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, HUD_SHADOW_OFFSET);
            s_hudTextMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -HUD_SHADOW_OFFSET);
            s_hudTextMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, HUD_SHADOW_DILATE);
            s_hudTextMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, HUD_SHADOW_SOFTNESS);
            s_hudTextMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;   // 씬 전환 시 정리 대상에서 제외
        }
        text.fontSharedMaterial = s_hudTextMaterial;
    }

    // ── 연출 헬퍼 ─────────────────────────────────────────────

    // Unity 오브젝트는 ?. 가 '파괴됨/미연결'을 null로 보지 않는다 → 진짜 null로 바꿔서 돌려준다
    private GeckoAnimatorController Anim => _geckoAnimator != null ? _geckoAnimator : null;

    private GeckoFx Fx()
    {
        EnsureFx();
        return _fx != null ? _fx : null;
    }

    private void EnsureFx()
    {
        if (_fx != null || _geckoAnimator == null) return;

        var area = _geckoAnimator.transform.parent as RectTransform;
        if (area == null) return;

        var motor = _geckoAnimator.Motor != null ? _geckoAnimator.Motor : _geckoAnimator.GetComponent<GeckoMotor>();
        _fx = GeckoFx.Create(area, motor, HomeFont);

        // 허물 배지는 연출보다 위에
        if (_moltBadge != null && _moltBadge.transform.parent == area) _moltBadge.transform.SetAsLastSibling();
    }

    private IEnumerator SayLater(string line, float delay)
    {
        yield return new WaitForSeconds(delay);
        Fx()?.Say(line);
    }

    private static IEnumerator Pulse(Transform t, float peak)
    {
        Vector3 baseScale = Vector3.one;
        for (float s = 0f; s < 0.35f; s += Time.unscaledDeltaTime)
        {
            float u = s / 0.35f;
            t.localScale = baseScale * (1f + (peak - 1f) * Mathf.Sin(u * Mathf.PI));
            yield return null;
        }
        t.localScale = baseScale;
    }


    private static float EaseOutBack(float x)
    {
        const float C1 = 1.7f, C3 = C1 + 1f;
        float t = Mathf.Clamp01(x) - 1f;
        return 1f + C3 * t * t * t + C1 * t * t;
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────

    /// <summary>
    /// 홈이 이 게코를 그려야 하는가 — 시간 진행은 모든 게코에 상태 변화를 알리므로 선택한 게코만 그린다.
    /// 선택이 비어 있으면(저장 손상) 화면이 비지 않게 그대로 그린다. 자가 검사가 같이 쓴다.
    /// </summary>
    public static bool IsHomeGecko(GeckoData g, string selectedGeckoId)
        => g != null && (string.IsNullOrEmpty(selectedGeckoId) || g.id == selectedGeckoId);

    // ── 게이지 · 숫자 표시 ────────────────────────────────────

    private static Sprite s_whiteSprite;

    /// <summary>
    /// 채움 막대로 쓸 수 있게 만든다. fillAmount는 Filled 타입이면서 **스프라이트가 있을 때만** 화면에 반영된다 —
    /// 스프라이트가 비어 있으면 uGUI(Image.OnPopulateMesh)가 채움을 무시하고 사각형 전체를 그린다.
    /// 예전 게이지·허물 막대가 값과 상관없이 가득 차 보인 원인. 씬 설정이 틀려도 동작하게 실행 시 보정한다.
    /// </summary>
    private static void MakeFillable(Image image)
    {
        if (image == null) return;

        if (image.sprite == null)
        {
            if (s_whiteSprite == null)
            {
                s_whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
                s_whiteSprite.hideFlags = HideFlags.DontUnloadUnusedAsset;   // 씬 전환 시 정리 대상에서 제외
            }
            image.sprite = s_whiteSprite;
        }

        if (image.type != Image.Type.Filled)
        {
            image.type       = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
    }

    /// <summary>
    /// 게이지 — 목표값으로 부드럽게 차오르고, 오를 때 살짝 부풀며, 위험 구간에서는 은은하게 깜빡인다.
    /// 막대 오른쪽 위에 현재 값(0~100)을 숫자로 보여준다.
    /// </summary>
    private class GaugeView
    {
        private const float VALUE_FONT_SIZE = 26f;
        private const float VALUE_GAP       = 2f;    // 막대 윗변과 숫자 사이

        private readonly Image    _fill;
        private readonly TMP_Text _value;
        private readonly float    _warning;
        private readonly Color    _normal;
        private readonly Vector3  _baseScale;
        private float _target, _shown, _bump, _time;
        private int   _lastValue = int.MinValue;

        public GaugeView(Image fill, float warning, Color normal, TMP_FontAsset font)
        {
            _fill      = fill;
            _warning   = warning;
            _normal    = normal;
            _baseScale = fill != null ? fill.transform.localScale : Vector3.one;
            if (fill == null) return;

            MakeFillable(fill);

            _value = CreateValueText(fill, font);
        }

        // 막대 위치를 기준으로 붙이므로 씬에서 막대를 옮기거나 크기를 바꿔도 숫자가 따라간다
        private static TMP_Text CreateValueText(Image fill, TMP_FontAsset font)
        {
            var fillRt = fill.rectTransform;
            var go = new GameObject("Value", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(fillRt.parent, false);
            rt.anchorMin = fillRt.anchorMin;
            rt.anchorMax = fillRt.anchorMax;
            rt.pivot     = new Vector2(1f, 0f);
            Vector2 size = fillRt.rect.size;
            rt.anchoredPosition = fillRt.anchoredPosition
                                + new Vector2(size.x * (1f - fillRt.pivot.x), size.y * (1f - fillRt.pivot.y) + VALUE_GAP);
            rt.sizeDelta = new Vector2(90f, 36f);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize      = VALUE_FONT_SIZE;
            text.alignment     = TextAlignmentOptions.BottomRight;
            text.color         = Color.white;
            text.raycastTarget = false;
            return text;
        }

        public void SetTarget(float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);
            if (value > _target + 0.5f) _bump = 1f;   // 눈에 띄게 올랐을 때만
            _target = value;
        }

        public void Snap()
        {
            _shown = _target;
            _bump  = 0f;
            Apply();
        }

        public void Tick(float dt)
        {
            if (_fill == null) return;
            _time += dt;
            _shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-GAUGE_SPEED * dt));
            _bump  = Mathf.Max(0f, _bump - dt * 3f);
            Apply();
        }

        private void Apply()
        {
            if (_fill == null) return;
            _fill.fillAmount = _shown / 100f;

            bool warn = _target <= _warning;
            _fill.color = warn
                ? Color.Lerp(COLOR_WARNING, COLOR_WARNING_SOFT, 0.5f + 0.5f * Mathf.Sin(_time * 3.5f))
                : _normal;

            float s = 1f + 0.12f * Mathf.Sin(_bump * Mathf.PI);
            _fill.transform.localScale = new Vector3(_baseScale.x, _baseScale.y * s, _baseScale.z);

            if (_value != null)
            {
                int v = Mathf.RoundToInt(_shown);
                if (v != _lastValue)   // 값이 바뀔 때만 텍스트 갱신 (메시 재생성 최소화)
                {
                    _lastValue  = v;
                    _value.text = v.ToString();
                }
            }
        }
    }

    /// <summary>코인·젬 숫자 — 늘어나면 또르르 올라가며 톡 커지고 '띵'.</summary>
    private class CountView
    {
        private readonly TMP_Text _text;
        private readonly string   _labelKey;   // 번역표 키 — "코인 {0}" / "Coins {0}"
        private readonly bool     _sound;
        private readonly Vector3  _baseScale;
        private float _shown, _from;
        private int   _target, _last = int.MinValue;
        private float _t = 1f, _pop;

        public CountView(TMP_Text text, string labelKey, bool playSound)
        {
            _text      = text;
            _labelKey  = labelKey;
            _sound     = playSound;
            _baseScale = text != null ? text.transform.localScale : Vector3.one;

            // 이름표가 붙어 길어져도 두 줄로 꺾이지 않게 ("Coins 1,250")
            if (text != null) text.textWrappingMode = TextWrappingModes.NoWrap;
        }

        public void Snap(int value)
        {
            _target = value;
            _shown  = value;
            _t      = 1f;
            _pop    = 0f;
            Write();
        }

        public void Tick(int value, float dt)
        {
            if (_text == null) return;

            if (value != _target)
            {
                if (value > _target && _sound) AudioManager.Play(Sfx.Coin, 0.8f);
                if (value > _target) _pop = 1f;
                _from   = _shown;
                _target = value;
                _t      = 0f;
            }

            if (_t < 1f)
            {
                _t = Mathf.Min(1f, _t + dt / COUNT_UP_TIME);
                float u = 1f - (1f - _t) * (1f - _t);   // 끝에서 천천히
                _shown = Mathf.Lerp(_from, _target, u);
            }

            _pop = Mathf.Max(0f, _pop - dt * 2.5f);
            _text.transform.localScale = _baseScale * (1f + 0.18f * Mathf.Sin(_pop * Mathf.PI));
            Write();
        }

        private void Write()
        {
            if (_text == null) return;
            int v = Mathf.RoundToInt(_shown);
            if (v == _last) return;   // 값이 바뀔 때만 텍스트 갱신 (메시 재생성 최소화)
            _last = v;
            _text.text = Loc.Format(_labelKey, v.ToString("N0"));
        }
    }
}
