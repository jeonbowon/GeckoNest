using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GeckoList.unity 에 부착. 보유 게코 목록 표시 + 신규 분양 처리.
///
/// 씬 구성 (Unity Editor에서 직접 배치):
///   Canvas
///     TopBar               — 코인/젬 표시
///     GeckoListContent     — ScrollView > Viewport > Content (GeckoSlot 프리팹 배치)
///     AdoptPanel           — 분양 패널 (종 선택 + 이름 입력 + 확인 버튼)
///       SpeciesDropdown    — TMP_Dropdown
///       NameInputField     — TMP_InputField
///       ConfirmButton      — Button
///       CancelButton       — Button
///     AdoptButton          — 분양 패널 열기 버튼
///     BackButton           — 홈으로
///     ErrorPanel / ErrorText
/// </summary>
public class GeckoListUIController : MonoBehaviour
{
    [Header("상단 바")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _gemText;

    [Header("게코 목록 (ScrollView > Content)")]
    [SerializeField] private Transform  _geckoListContent;
    [SerializeField] private GameObject _geckoSlotPrefab;

    [Header("분양 패널")]
    [SerializeField] private GameObject       _adoptPanel;
    [SerializeField] private TMP_Dropdown     _speciesDropdown;
    [SerializeField] private TMP_InputField   _nameInputField;
    [SerializeField] private Button           _confirmAdoptButton;
    [SerializeField] private Button           _cancelAdoptButton;
    [SerializeField] private Button           _adoptButton;

    [Header("판매 종 목록 (Inspector에서 드래그)")]
    [SerializeField] private GeckoSpeciesSO[] _speciesForSale;

    [Header("오류 알림")]
    [SerializeField] private GameObject _errorPanel;
    [SerializeField] private TMP_Text   _errorText;
    private const float ERROR_DISPLAY_SECONDS = 2.5f;
    private Coroutine   _errorCoroutine;

    [Header("뒤로가기")]
    [SerializeField] private Button _backButton;

    private StoreManager _store;
    private readonly System.Collections.Generic.List<GeckoSpeciesSO> _dropdownSpecies
        = new System.Collections.Generic.List<GeckoSpeciesSO>();   // 드롭다운 항목과 같은 순서 (빈 칸 제외)

    // ── 생명주기 ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[GeckoListUIController] GameManager가 아직 없습니다 — AppBootstrap이 초기화한 뒤 이 씬을 다시 엽니다.");
            return;
        }

        _store = GameManager.Instance.Store;
        _store.OnGeckoPurchased += OnGeckoPurchasedHandler;
        _store.OnPurchaseFailed += OnPurchaseFailedHandler;

        _adoptButton.onClick.AddListener(OpenAdoptPanel);
        _confirmAdoptButton.onClick.AddListener(OnConfirmAdopt);
        _cancelAdoptButton.onClick.AddListener(CloseAdoptPanel);
        _backButton.onClick.AddListener(OnBackClicked);

        if (_adoptPanel != null)   _adoptPanel.SetActive(false);
        if (_errorPanel != null)   _errorPanel.SetActive(false);

        BuildSpeciesDropdown();
        RefreshCurrency();
        RefreshGeckoList();
        EnsureBookButton();
        RefreshBookBadge();
    }

    // ── 도감 · 업적 ──────────────────────────────────────────
    // "+ 분양" 버튼을 복제한 "도감" 버튼을 윗줄 아래에 두고(목록을 그만큼 내림), 누르면 CollectionPanel

    private const float BOOK_BAR_H     = 110f;   // 목록을 내리는 높이
    private const float BOOK_BUTTON_W  = 300f;
    private const float BOOK_BUTTON_H  = 88f;
    private const float BOOK_MARGIN    = 24f;
    private const float TOP_BAR_H      = 150f;   // 씬 TopBar 높이
    private static readonly Color NOTICE_COLOR = new Color(0.18f, 0.55f, 0.30f, 0.95f);

    private Button          _bookButton;
    private TMP_Text        _bookLabel;
    private CollectionPanel _book;

    private void EnsureBookButton()
    {
        if (_bookButton != null || _adoptButton == null || _geckoListContent == null) return;
        var scroll = _geckoListContent.GetComponentInParent<ScrollRect>();
        var area   = scroll != null ? scroll.transform.parent as RectTransform : null;
        if (area == null) return;

        // 목록을 내려 버튼 자리를 만든다
        var listRt = (RectTransform)scroll.transform;
        listRt.offsetMax -= new Vector2(0f, BOOK_BAR_H);

        _bookButton = Instantiate(_adoptButton, area);
        _bookButton.name = "BookButton";
        _bookButton.onClick.RemoveAllListeners();
        _bookButton.onClick.AddListener(OpenBook);
        var rt = (RectTransform)_bookButton.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.sizeDelta        = new Vector2(BOOK_BUTTON_W, BOOK_BUTTON_H);
        rt.anchoredPosition = new Vector2(-BOOK_MARGIN, -(TOP_BAR_H + (BOOK_BAR_H - BOOK_BUTTON_H) * 0.5f));
        _bookLabel = _bookButton.GetComponentInChildren<TMP_Text>(true);
        if (_bookLabel != null)
        {
            SceneTextLocalizer.Ignore(_bookLabel);
            _bookLabel.enableAutoSizing = true;
            _bookLabel.fontSizeMin      = 20f;
            _bookLabel.fontSizeMax      = 36f;
        }
        UIPressScale.Ensure(_bookButton);

        var font  = _coinText != null ? _coinText.font : (_bookLabel != null ? _bookLabel.font : null);
        var round = _adoptButton.image != null ? _adoptButton.image.sprite : null;
        _book = CollectionPanel.Create(area, GameManager.Instance.Reward, font, round, OnBookChanged);
    }

    private void RefreshBookBadge()
    {
        if (_bookLabel == null) return;
        int n = GameManager.Instance.Reward.ClaimableCount(SpeciesCatalog.All);
        _bookLabel.text = n > 0 ? Loc.Format("book.button_count", n) : Loc.Get("book.button");
    }

    private void OpenBook()
    {
        if (_book == null || SceneRouter.IsTransitioning) return;
        AudioManager.Play(Sfx.Pop, 0.8f);
        // 받을 업적이 있으면 업적 탭부터, 아니면 도감
        var reward = GameManager.Instance.Reward;
        bool achievements = false;
        foreach (var a in RewardManager.ACHIEVEMENTS)
            if (reward.CanClaimAchievement(a)) { achievements = true; break; }
        _book.Open(achievements);
    }

    private void OnBookChanged()
    {
        RefreshCurrency();
        RefreshBookBadge();
    }

    private void OnDisable()
    {
        if (_store != null)
        {
            _store.OnGeckoPurchased -= OnGeckoPurchasedHandler;
            _store.OnPurchaseFailed -= OnPurchaseFailedHandler;
        }
        _adoptButton.onClick.RemoveListener(OpenAdoptPanel);
        _confirmAdoptButton.onClick.RemoveListener(OnConfirmAdopt);
        _cancelAdoptButton.onClick.RemoveListener(CloseAdoptPanel);
        _backButton.onClick.RemoveListener(OnBackClicked);
    }

    // ── 게코 목록 ─────────────────────────────────────────────

    private void RefreshGeckoList()
    {
        if (_geckoListContent == null || _geckoSlotPrefab == null) return;

        foreach (Transform child in _geckoListContent)
            Destroy(child.gameObject);

        var data = GameManager.Instance.GetPlayerData();
        foreach (var gecko in data.geckos)
        {
            var go   = Instantiate(_geckoSlotPrefab, _geckoListContent);
            var slot = go.GetComponent<GeckoSlotUI>();
            if (slot != null)
                slot.Setup(gecko, gecko.id == data.selectedGeckoId, OnGeckoSlotClicked);
        }
    }

    private void OnGeckoSlotClicked(GeckoData gecko)
    {
        if (SceneRouter.IsTransitioning) return;
        AudioManager.Play(Sfx.Pop, 0.8f);
        GameManager.Instance.SetSelectedGecko(gecko.id);
        SceneRouter.GoToHome();
    }

    // ── 분양 패널 ─────────────────────────────────────────────

    private void BuildSpeciesDropdown()
    {
        _dropdownSpecies.Clear();
        if (_speciesForSale == null) return;

        var data    = GameManager.Instance.GetPlayerData();
        var options = new System.Collections.Generic.List<string>();
        foreach (var s in _speciesForSale)
        {
            if (s == null) continue;
            _dropdownSpecies.Add(s);
            string label = StoreManager.IsFreeFor(data, s)
                ? Loc.Format("geckolist.option_free", Loc.SpeciesName(s))
                : $"{Loc.SpeciesName(s)}  {s.coinPrice} C";
            options.Add(label);
        }

        if (_speciesDropdown == null) return;
        _speciesDropdown.ClearOptions();
        _speciesDropdown.AddOptions(options);
    }

    private void OpenAdoptPanel()
    {
        if (!StoreManager.CanAdoptMore(GameManager.Instance.GetPlayerData()))
        {
            OnPurchaseFailedHandler(Loc.Format("geckolist.full", StoreManager.MAX_GECKOS));
            return;
        }
        if (_adoptPanel != null) _adoptPanel.SetActive(true);
        if (_nameInputField != null) _nameInputField.text = "";
    }

    private void CloseAdoptPanel()
    {
        if (_adoptPanel != null) _adoptPanel.SetActive(false);
    }

    private void OnConfirmAdopt()
    {
        // 드롭다운은 빈 칸을 건너뛰고 만들었으므로, 원래 배열이 아니라 같은 순서로 모아 둔 목록에서 고른다
        if (_dropdownSpecies.Count == 0) return;

        int idx     = _speciesDropdown != null ? _speciesDropdown.value : 0;
        var species = _dropdownSpecies[Mathf.Clamp(idx, 0, _dropdownSpecies.Count - 1)];
        string name = _nameInputField != null ? _nameInputField.text.Trim() : "";

        _store.BuyGecko(species, name);
        CloseAdoptPanel();
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    private void OnGeckoPurchasedHandler(GeckoData gecko)
    {
        RefreshCurrency();
        RefreshGeckoList();
        BuildSpeciesDropdown();   // 무료였던 종이 유료로 바뀔 수 있다
        RefreshBookBadge();       // "북적이는 집" 같은 업적
        AudioManager.Play(Sfx.Chime, 0.8f);
        Haptics.Success();
        if (_store.LastMeetCoin > 0)   // 도감에 새 종 — 코인 보상
            ShowMessage(Loc.Format("book.met_reward", _store.LastMeetCoin), NOTICE_COLOR);
        Debug.Log($"[GeckoListUIController] 분양 완료 — {gecko.name}");
    }

    private void OnPurchaseFailedHandler(string reason)
    {
        AudioManager.Play(Sfx.Error, 0.8f);
        ShowError(reason);
    }

    private void OnBackClicked()
    {
        SceneRouter.GoToHome();
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    private void RefreshCurrency()
    {
        var data = GameManager.Instance.GetPlayerData();
        if (_coinText != null) _coinText.text = Loc.Format("hud.coin", data.coin.ToString("N0"));
        if (_gemText  != null) _gemText.text  = Loc.Format("hud.gem",  data.gem.ToString("N0"));
    }

    private Color? _errorBaseColor;

    private void ShowError(string message) => ShowMessage(message, null);

    // 오류 패널을 알림에도 쓴다 — color가 있으면 그 색(초록 = 좋은 소식), 없으면 씬의 원래 색
    private void ShowMessage(string message, Color? color)
    {
        if (_errorPanel == null || _errorText == null) return;
        var panelImage = _errorPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            _errorBaseColor ??= panelImage.color;
            panelImage.color = color ?? _errorBaseColor.Value;
        }
        if (_errorCoroutine != null) StopCoroutine(_errorCoroutine);
        _errorText.text = message;
        _errorPanel.transform.SetAsLastSibling();
        _errorPanel.SetActive(true);
        _errorCoroutine = StartCoroutine(HideErrorAfterDelay());
    }

    private System.Collections.IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(ERROR_DISPLAY_SECONDS);
        if (_errorPanel != null) _errorPanel.SetActive(false);
        _errorCoroutine = null;
    }
}
