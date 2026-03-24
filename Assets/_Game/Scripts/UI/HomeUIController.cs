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
    [SerializeField] private Button _feedButton;
    [SerializeField] private Button _waterButton;
    [SerializeField] private Button _petButton;
    [SerializeField] private Button _cleanButton;

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

    // ── 성장 단계 ──────────────────────────────────────────────
    [Header("성장 단계 아이콘")]
    [SerializeField] private Image    _growthStageIcon;
    [SerializeField] private Sprite[] _growthStageSprites; // 0=Egg, 1=Baby, 2=Juvenile, 3=Sub-Adult, 4=Adult

    private static readonly string[] STAGE_NAMES =
        { "Hatchling", "Baby", "Juvenile", "Sub-Adult", "Adult" };

    private GeckoManager     _gecko;
    private TerrariumManager _terrarium;

    // ── 생명주기 ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[HomeUIController] GameManager.Instance가 null — Boot 씬부터 실행하세요.");
            return;
        }

        _gecko = GameManager.Instance.Gecko;
        _gecko.OnStateChanged += Refresh;
        _gecko.OnGrowthUp     += OnGrowthUpHandler;
        _gecko.OnMoltSuccess  += OnMoltSuccessHandler;
        _gecko.OnMoltFail     += OnMoltFailHandler;

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

        if (_resultPanel != null)
            _resultPanel.SetActive(false);

        Refresh(selected);
        RefreshCurrency();
        RefreshTerrarium();
    }

    private void OnDisable()
    {
        if (_gecko != null)
        {
            _gecko.OnStateChanged -= Refresh;
            _gecko.OnGrowthUp     -= OnGrowthUpHandler;
            _gecko.OnMoltSuccess  -= OnMoltSuccessHandler;
            _gecko.OnMoltFail     -= OnMoltFailHandler;
        }

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
        Debug.Log("[HomeUIController] 먹이 버튼 클릭됨");
        var g    = GameManager.Instance.GetSelectedGecko();
        var item = GetFirstFoodItem();

        if (item == null)
        {
            SceneRouter.GoToStore();
            return;
        }

        _gecko.FeedGecko(g.id, item);
        _geckoAnimator.TriggerFeedCatch();
        RefreshFeedButton();
    }

    private void OnWaterClicked()
    {
        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;

        _gecko.GiveWater(g.id);
        _geckoAnimator.TriggerDrink();
    }

    private void OnPetClicked()
    {
        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;
        _gecko.Pet(g.id);
    }

    private void OnCleanClicked()
    {
        var g = GameManager.Instance.GetSelectedGecko();
        if (g == null) return;
        _gecko.Clean(g.id);
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    private void OnGrowthUpHandler(GeckoData g)
    {
        RefreshGrowthInfo(g);
        ShowResult($"✦ {g.name} grew up!\n{STAGE_NAMES[Mathf.Clamp(g.growthStage - 1, 0, 4)]} -> {STAGE_NAMES[Mathf.Clamp(g.growthStage, 0, 4)]}");
    }

    private void OnMoltSuccessHandler(GeckoData g)
    {
        ShowResult($"Molt success! (x{g.moltCount})");
    }

    private void OnMoltFailHandler(GeckoData g)
    {
        ShowResult("Molt failed... try again.");
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    private void Refresh(GeckoData g)
    {
        if (g == null) return;

        SetGauge(_hungerFill,      g.hunger,      WARNING_THRESHOLD);
        SetGauge(_thirstFill,      g.thirst,      WARNING_THRESHOLD);
        SetGauge(_moodFill,        g.mood,        WARNING_THRESHOLD);
        SetGauge(_healthFill,      g.health,      20f);
        SetGauge(_cleanlinessFill, g.cleanliness, 20f);

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

        int stage = Mathf.Clamp(g.growthStage, 0, STAGE_NAMES.Length - 1);

        if (_growthStageText != null)
            _growthStageText.text = STAGE_NAMES[stage];

        if (_growthStageIcon != null && _growthStageSprites != null && stage < _growthStageSprites.Length)
            _growthStageIcon.sprite = _growthStageSprites[stage];
    }

    private void RefreshCurrency()
    {
        var data = GameManager.Instance.GetPlayerData();
        _coinText.text = data.coin.ToString("N0");
        _gemText.text  = data.gem.ToString("N0");
    }

    private void SetGauge(Image fill, float value, float warningThreshold)
    {
        fill.fillAmount = value / 100f;
        fill.color      = value <= warningThreshold ? COLOR_WARNING : COLOR_NORMAL;
    }

    private void RefreshFeedButton()
    {
        bool hasFeed = GetFirstFoodItem() != null;
        var group = _feedButton.GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = hasFeed ? 1f : 0.5f;
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
        yield return new WaitForSeconds(RESULT_DISPLAY_SECONDS);
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
                string slotId = i < data.decorSlots.Length ? data.decorSlots[i] : null;
                bool   hasItem = !string.IsNullOrEmpty(slotId);
                if (_decorImages[i] != null)
                {
                    _decorImages[i].gameObject.SetActive(hasItem);
                    if (hasItem) ApplyDecorSprite(_decorImages[i], slotId);
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

        foreach (var item in _allDecorItems)
        {
            if (item != null && item.itemId == itemId)
            {
                target.sprite = item.previewSprite;
                target.gameObject.SetActive(item.previewSprite != null);
                return;
            }
        }
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
}
