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

        _storeButton?.onClick.AddListener(OnStoreClicked);
        _geckoListButton?.onClick.AddListener(OnGeckoListClicked);
        _terrariumButton?.onClick.AddListener(OnTerrariumClicked);
        _rewardButton?.onClick.AddListener(OnRewardClicked);
        _settingsButton?.onClick.AddListener(OnSettingsClicked);

        if (_rewardPanel != null)   _rewardPanel.SetActive(false);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);

        // 새 게임 첫 홈이면 알이 깨지는 연출부터 (Start에서 시작) — 보상 팝업은 부화가 끝난 뒤 OnHatched에서
        _hatchPending = _gecko.NeedsHatchIntro();

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
        EnsureGrowthInfoButton();          // 성장 단계 글자 누르기 → 다음 성장 조건
        SceneTextLocalizer.Ignore(_geckoNameText);   // 게코 이름은 번역하지 않는다 ("하코"가 영어에서 "Hako"로 바뀌지 않게)
        InitViews();
        ApplyHudReadability();              // 게이지 숫자가 만들어진 뒤 — 배경 위 글자에 그림자
        Refresh(selected);
        SnapViews();
        RefreshTerrarium();

        _presenter = StartCoroutine(EventPresenter());
    }

    private void Start()
    {
        // 게코 오브젝트의 Awake가 끝난 뒤에 연출 레이어를 붙인다 (OnEnable 순서는 오브젝트끼리 보장되지 않음)
        EnsureFx();
        if (_hatchPending) StartHatchIntro();   // 인사 말풍선·하트에 연출 레이어가 필요해서 Start에서
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

    // 결과 알림이 사라진 뒤 일일 보상 팝업 (첫 실행은 부화 연출 때문에 미뤄 두었다)
    private IEnumerator OpenRewardAfterResult()
    {
        while (_resultCoroutine != null) yield return null;
        if (GameManager.Instance != null && GameManager.Instance.Reward.CanClaim() && _rewardPanel != null)
            _rewardPanel.SetActive(true);
    }

    private void OnDisable()
    {
        if (_gecko != null)
            _gecko.OnStateChanged -= Refresh;

        if (_terrarium != null)
            _terrarium.OnTerrariumChanged -= RefreshTerrarium;

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

    /// <summary>가진 먹이 목록 — 마지막으로 준 먹이를 맨 앞에</summary>
    private System.Collections.Generic.List<FoodTray.Option> OwnedFoods(GeckoData g)
    {
        var data = GameManager.Instance.GetPlayerData();
        var list = new System.Collections.Generic.List<FoodTray.Option>();
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

        switch (_gecko.Pet(g.id))
        {
            case CareResult.Done:
                Anim?.TriggerPet();
                Fx()?.Hearts();
                Haptics.Light();
                if (Random.value < PET_LINE_CHANCE) Fx()?.Say(Loc.Pick("line.pet"));
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
            }
        }
        else
        {
            AudioManager.Play(e.type == GeckoEventType.MoltFail ? Sfx.MoltFail : Sfx.Chime, 0.7f);
        }

        ShowResult(EventMessage(e));
        yield return new WaitForSecondsRealtime(RESULT_DISPLAY_SECONDS + 0.4f);

        if (e.type == GeckoEventType.GrowthUp && e.growthStage >= GeckoManager.ADULT_STAGE)
            yield return SuggestNewFriend(isSelected);
    }

    private const int NEW_FRIEND_PULSES = 3;   // 게코 탭이 통통 튀는 횟수

    // 다 자란 뒤 — 새 게코를 들이도록 권한다 (보상 코인이면 가고일도 분양 가능)
    private IEnumerator SuggestNewFriend(bool say)
    {
        if (say) Fx()?.Say(Loc.Get("line.new_friend"), 2.5f);
        if (_geckoListButton == null) yield break;
        AudioManager.Play(Sfx.Pop, 0.6f);
        for (int i = 0; i < NEW_FRIEND_PULSES; i++)
        {
            yield return Pulse(_geckoListButton.transform, 1.25f);
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private static string EventMessage(GeckoEvent e)
    {
        switch (e.type)
        {
            case GeckoEventType.GrowthUp:
                if (e.growthStage >= GeckoManager.ADULT_STAGE)
                    return Loc.Format("event.adult", Loc.Subject(e.geckoName),
                                      GeckoManager.ADULT_REWARD_COIN, GeckoManager.ADULT_REWARD_GEM);
                return Loc.Format("event.growth", Loc.Subject(e.geckoName),
                                  Loc.StageName(e.growthStage - 1), Loc.StageName(e.growthStage));
            case GeckoEventType.MoltSuccess:
                return Loc.Format("event.molt_success", Loc.Subject(e.geckoName), e.moltCount);
            default:
                return Loc.Get("event.molt_fail");
        }
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    private void Refresh(GeckoData g)
    {
        if (g == null) return;

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
    }

    private void RefreshGrowthInfo(GeckoData g)
    {
        if (_geckoNameText != null)
            _geckoNameText.text = g.name;

        int stage = Mathf.Clamp(_stageOverride >= 0 ? _stageOverride : g.growthStage, 0, 4);

        if (_growthStageText != null)
            _growthStageText.text = Loc.StageName(stage);

        if (_growthStageIcon != null && _growthStageSprites != null && stage < _growthStageSprites.Length)
            _growthStageIcon.sprite = _growthStageSprites[stage];
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

    private void RefreshFeedButton()
    {
        var item    = GetFirstFoodItem();
        bool hasFeed = item != null;

        // 투명도
        var group = _feedButton.GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = hasFeed ? 1f : 0.5f;

        // 아이템 이름 + 수량 텍스트
        if (_feedItemText != null)
        {
            if (hasFeed)
            {
                int count = GameManager.Instance.GetPlayerData()
                    .inventory.Find(s => s.itemId == item.itemId)?.count ?? 0;
                _feedItemText.text = $"{Loc.ItemName(item)} x{count}";
            }
            else
            {
                _feedItemText.text = Loc.Get("home.no_food");
            }
        }
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
        if (_allDecorItems == null) return;

        var data = _terrarium.GetData();

        ApplyDecorSprite(_backgroundImage, data.backgroundId);
        ApplyDecorSprite(_floorImage,      data.floorId);

        if (_decorImages != null)
        {
            for (int i = 0; i < _decorImages.Length; i++)
            {
                string slotId  = i < data.decorSlots.Length ? data.decorSlots[i] : null;
                bool   hasItem = !string.IsNullOrEmpty(slotId);
                if (_decorImages[i] != null)
                {
                    _decorImages[i].gameObject.SetActive(hasItem);
                    if (hasItem)
                        ApplyDecorSprite(_decorImages[i], slotId);
                }
            }
        }
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
    /// MVP: inventory에서 수량이 남은 첫 번째 먹이 아이템을 자동 선택.
    /// 종류 선택 UI는 2차 MVP에서 구현.
    /// </summary>
    private ItemSO GetFirstFoodItem()
    {
        var data = GameManager.Instance.GetPlayerData();
        foreach (var stack in data.inventory)
        {
            if (stack.count <= 0) continue;
            var item = Resources.Load<ItemSO>($"Items/{stack.itemId}");
            if (item != null && item.hungerRestore > 0f)
                return item;
        }
        return null;
    }

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
