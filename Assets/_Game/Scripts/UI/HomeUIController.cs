using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeUIController : MonoBehaviour
{
    // ── 상단 바 ───────────────────────────────────────────────
    [Header("상단 바")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _gemText;

    // ── 상태 게이지 ───────────────────────────────────────────
    [Header("상태 게이지 (Image fillAmount)")]
    [SerializeField] private Image _hungerFill;
    [SerializeField] private Image _thirstFill;
    [SerializeField] private Image _moodFill;
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _cleanlinessFill;

    // ── 경고 색상 ──────────────────────────────────────────────
    private static readonly Color COLOR_NORMAL  = Color.white;
    private static readonly Color COLOR_WARNING = new Color(1f, 0.27f, 0.27f); // 빨강
    private const float WARNING_THRESHOLD = 30f;

    // ── 허물 배지 ──────────────────────────────────────────────
    [Header("허물 배지")]
    [SerializeField] private GameObject _moltBadge; // moltProgress 80+ 시 표시

    // ── 행동 버튼 ──────────────────────────────────────────────
    [Header("행동 버튼")]
    [SerializeField] private Button _feedButton;
    [SerializeField] private Button _waterButton;
    [SerializeField] private Button _petButton;
    [SerializeField] private Button _cleanButton;

    // ── 게코 영역 ──────────────────────────────────────────────
    [Header("게코")]
    [SerializeField] private GeckoAnimatorController _geckoAnimator;

    private GeckoManager _gecko;

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

        _feedButton.onClick.AddListener(OnFeedClicked);
        _waterButton.onClick.AddListener(OnWaterClicked);
        _petButton.onClick.AddListener(OnPetClicked);
        _cleanButton.onClick.AddListener(OnCleanClicked);

        var selected = GameManager.Instance.GetSelectedGecko();
        if (selected == null)
            Debug.LogError("[HomeUIController] GetSelectedGecko()가 null — selectedGeckoId 또는 geckos 목록을 확인하세요.");

        Refresh(selected);
        RefreshCurrency();
    }

    private void OnDisable()
    {
        if (_gecko != null)
            _gecko.OnStateChanged -= Refresh;

        _feedButton.onClick.RemoveListener(OnFeedClicked);
        _waterButton.onClick.RemoveListener(OnWaterClicked);
        _petButton.onClick.RemoveListener(OnPetClicked);
        _cleanButton.onClick.RemoveListener(OnCleanClicked);
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────

    private void OnFeedClicked()
    {
        Debug.Log("[HomeUIController] 먹이 버튼 클릭됨");
        var g    = GameManager.Instance.GetSelectedGecko();
        var item = GetFirstFoodItem();

        if (item == null)
        {
            // 먹이 없음 → 스토어로
            SceneRouter.GoToStore();
            return;
        }

        _gecko.FeedGecko(g.id, item);
        _geckoAnimator.TriggerFeedCatch();
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

        RefreshFeedButton();
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
        // 먹이 없으면 반투명 처리
        var group = _feedButton.GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = hasFeed ? 1f : 0.5f;
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────

    /// <summary>
    /// MVP: ownedItemIds에서 첫 번째 먹이 아이템을 자동 선택.
    /// 종류 선택 UI는 2차 MVP에서 구현.
    /// </summary>
    private ItemSO GetFirstFoodItem()
    {
        var data = GameManager.Instance.GetPlayerData();
        foreach (var itemId in data.ownedItemIds)
        {
            // Resources 폴더에서 itemId로 ItemSO 로드
            var item = Resources.Load<ItemSO>($"Items/{itemId}");
            if (item != null && item.hungerRestore > 0f)
                return item;
        }
        return null;
    }
}
