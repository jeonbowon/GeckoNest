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
    private static readonly Color COLOR_NORMAL  = Color.white;
    private static readonly Color COLOR_WARNING = new Color(1f, 0.27f, 0.27f);
    private static readonly Color COLOR_WARNING_SOFT = new Color(1f, 0.55f, 0.55f);
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

    private static readonly string[] STAGE_NAMES =
        { "Hatchling", "Baby", "Juvenile", "Sub-Adult", "Adult" };

    // ── 게코 한마디 (글꼴에 있는 글자만: 한글·영문·숫자·기본 기호) ──
    private static readonly string[] LINES_FULL        = { "배불러요", "이미 배불러~" };
    private static readonly string[] LINES_NOT_THIRSTY = { "목 안 말라요", "물은 충분해!" };
    private static readonly string[] LINES_CLEAN       = { "이미 반짝반짝!", "깨끗해요~" };
    private static readonly string[] LINES_ANNOYED     = { "그만 만져~", "힝, 귀찮아", "잠깐만 쉴래" };
    private static readonly string[] LINES_PET         = { "좋아~", "헤헤", "더 해줘!" };
    private static readonly string[] LINES_FED         = { "냠냠", "맛있다!" };
    private static readonly string[] LINES_GROWTH      = { "쑥쑥!", "나 컸지?" };
    private static readonly string[] LINES_MOLT        = { "개운해!", "새 옷 입었다!" };
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

        // 일일 보상 자동 팝업 — 받을 수 있으면 앱 진입 시 표시
        if (GameManager.Instance.Reward.CanClaim() && _rewardPanel != null)
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
            _geckoMovement.enabled = true;

        InitViews();
        Refresh(selected);
        SnapViews();
        RefreshTerrarium();

        _presenter = StartCoroutine(EventPresenter());
    }

    private void Start()
    {
        // 게코 오브젝트의 Awake가 끝난 뒤에 연출 레이어를 붙인다 (OnEnable 순서는 오브젝트끼리 보장되지 않음)
        EnsureFx();
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

    private void OnFeedClicked()
    {
        if (SceneRouter.IsTransitioning) return;
        var g    = GameManager.Instance.GetSelectedGecko();
        var item = GetFirstFoodItem();

        if (item == null)
        {
            SceneRouter.GoToStore();
            return;
        }

        if (g == null) return;

        switch (_gecko.FeedGecko(g.id, item))
        {
            case CareResult.Done:
                Anim?.TriggerFeedCatch();
                Fx()?.FeedDrop(item.icon);
                Haptics.Light();
                if (Random.value < FED_LINE_CHANCE) StartCoroutine(SayLater(Pick(LINES_FED), 1.0f));
                break;
            case CareResult.Refused:
                Anim?.TriggerRefuse();
                Fx()?.Refuse();
                Fx()?.Say(Pick(LINES_FULL));
                Haptics.Light();
                break;
        }
        RefreshFeedButton();
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
                Fx()?.Say(Pick(LINES_NOT_THIRSTY));
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
                if (Random.value < PET_LINE_CHANCE) Fx()?.Say(Pick(LINES_PET));
                break;
            case CareResult.Annoyed:
                Anim?.TriggerAnnoyed();
                Fx()?.Annoyed();
                Fx()?.Say(Pick(LINES_ANNOYED));
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
                Fx()?.Say(Pick(LINES_CLEAN));
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
        if (_rewardPanel != null && _rewardPanel.activeInHierarchy) return false;
        if (_settingsPanel != null && _settingsPanel.activeInHierarchy) return false;
        if (_resultCoroutine != null) return false;
        if (_geckoAnimator != null && _geckoAnimator.IsBusy) return false;
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
                    StartCoroutine(SayLater(Pick(LINES_GROWTH), 1.2f));
                    break;
                case GeckoEventType.MoltSuccess:
                    fx?.MoltFlakes(true);
                    Haptics.Success();
                    StartCoroutine(SayLater(Pick(LINES_MOLT), 1.6f));
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
    }

    private static string EventMessage(GeckoEvent e)
    {
        switch (e.type)
        {
            case GeckoEventType.GrowthUp:
            {
                int to   = Mathf.Clamp(e.growthStage, 0, STAGE_NAMES.Length - 1);
                int prev = Mathf.Clamp(to - 1, 0, STAGE_NAMES.Length - 1);
                return $"{WithSubject(e.geckoName)} 자랐어요!\n{STAGE_NAMES[prev]} -> {STAGE_NAMES[to]}";
            }
            case GeckoEventType.MoltSuccess:
                return $"{WithSubject(e.geckoName)} 허물을 벗었어요!\n({e.moltCount}번째 허물)";
            default:
                return "허물이 잘 안 벗겨졌어요\n다음엔 꼭 성공할 거예요";
        }
    }

    private static string WithSubject(string name) => KoreanText.WithSubject(name);

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

        int stage = Mathf.Clamp(_stageOverride >= 0 ? _stageOverride : g.growthStage, 0, STAGE_NAMES.Length - 1);

        if (_growthStageText != null)
            _growthStageText.text = STAGE_NAMES[stage];

        if (_growthStageIcon != null && _growthStageSprites != null && stage < _growthStageSprites.Length)
            _growthStageIcon.sprite = _growthStageSprites[stage];
    }

    private void InitViews()
    {
        if (_gauges == null)
        {
            _gauges = new[]
            {
                new GaugeView(_hungerFill,      WARNING_THRESHOLD),
                new GaugeView(_thirstFill,      WARNING_THRESHOLD),
                new GaugeView(_moodFill,        WARNING_THRESHOLD),
                new GaugeView(_healthFill,      20f),
                new GaugeView(_cleanlinessFill, 20f),
            };
        }
        if (_coinView == null) _coinView = new CountView(_coinText, playSound: true);
        if (_gemView  == null) _gemView  = new CountView(_gemText,  playSound: true);
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
                _feedItemText.text = $"{item.displayName} x{count}";
            }
            else
            {
                _feedItemText.text = "먹이 없음";
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
        var font  = _geckoNameText != null ? _geckoNameText.font : (_resultText != null ? _resultText.font : null);
        _fx = GeckoFx.Create(area, motor, font);

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

    private static string Pick(string[] lines) => lines[Random.Range(0, lines.Length)];

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

    /// <summary>게이지 — 목표값으로 부드럽게 차오르고, 오를 때 살짝 부풀며, 위험 구간에서는 은은하게 깜빡인다.</summary>
    private class GaugeView
    {
        private readonly Image _fill;
        private readonly float _warning;
        private readonly Vector3 _baseScale;
        private float _target, _shown, _bump, _time;

        public GaugeView(Image fill, float warning)
        {
            _fill      = fill;
            _warning   = warning;
            _baseScale = fill != null ? fill.transform.localScale : Vector3.one;
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
                : COLOR_NORMAL;

            float s = 1f + 0.12f * Mathf.Sin(_bump * Mathf.PI);
            _fill.transform.localScale = new Vector3(_baseScale.x, _baseScale.y * s, _baseScale.z);
        }
    }

    /// <summary>코인·젬 숫자 — 늘어나면 또르르 올라가며 톡 커지고 '띵'.</summary>
    private class CountView
    {
        private readonly TMP_Text _text;
        private readonly bool     _sound;
        private readonly Vector3  _baseScale;
        private float _shown, _from;
        private int   _target, _last = int.MinValue;
        private float _t = 1f, _pop;

        public CountView(TMP_Text text, bool playSound)
        {
            _text      = text;
            _sound     = playSound;
            _baseScale = text != null ? text.transform.localScale : Vector3.one;
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
            _text.text = v.ToString("N0");
        }
    }
}
