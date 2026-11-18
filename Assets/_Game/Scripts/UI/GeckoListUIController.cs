using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
    private GameObject _deletePanel;
    private TMP_Text _deleteName;
    private TMP_Text _deleteDetails;
    private Button _cancelDeleteButton;
    private string _deleteGeckoId;
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
        if (_speciesDropdown != null)
            _speciesDropdown.onValueChanged.AddListener(UpdateAdoptSelection);

        if (_adoptPanel != null)   _adoptPanel.SetActive(false);
        if (_errorPanel != null)   _errorPanel.SetActive(false);

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
        CloseDeleteDialog();
        if (_store != null)
        {
            _store.OnGeckoPurchased -= OnGeckoPurchasedHandler;
            _store.OnPurchaseFailed -= OnPurchaseFailedHandler;
        }
        _adoptButton.onClick.RemoveListener(OpenAdoptPanel);
        _confirmAdoptButton.onClick.RemoveListener(OnConfirmAdopt);
        _cancelAdoptButton.onClick.RemoveListener(CloseAdoptPanel);
        _backButton.onClick.RemoveListener(OnBackClicked);
        if (_speciesDropdown != null)
            _speciesDropdown.onValueChanged.RemoveListener(UpdateAdoptSelection);
    }

    // ── 게코 목록 ─────────────────────────────────────────────

    private void RefreshGeckoList()
    {
        if (_geckoListContent == null || _geckoSlotPrefab == null) return;

        foreach (Transform child in _geckoListContent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        var data = GameManager.Instance.GetPlayerData();
        foreach (var gecko in data.geckos)
        {
            var go   = Instantiate(_geckoSlotPrefab, _geckoListContent);
            var slot = go.GetComponent<GeckoSlotUI>();
            if (slot != null)
                slot.Setup(gecko, gecko.id == data.selectedGeckoId, OnGeckoSlotClicked, OpenDeleteDialog);
        }
    }

    private void OnGeckoSlotClicked(GeckoData gecko)
    {
        if (SceneRouter.IsTransitioning) return;
        AudioManager.Play(Sfx.Pop, 0.8f);
        GameManager.Instance.SetSelectedGecko(gecko.id);
        SceneRouter.GoToHome();
    }

    private void OpenDeleteDialog(GeckoData gecko)
    {
        if (SceneRouter.IsTransitioning || gecko == null) return;

        var geckos = GameManager.Instance.GetPlayerData().geckos;
        int index = geckos.IndexOf(gecko);
        if (index < 0) return;
        if (geckos.Count <= 1)
        {
            ShowError(Loc.Get("geckolist.delete_last"));
            return;
        }

        if (_deletePanel == null)
        {
            var canvas = _geckoListContent.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            BuildDeleteDialog((RectTransform)canvas.transform);
        }
        _deleteGeckoId = gecko.id;
        _deleteName.text = gecko.name;
        _deleteDetails.text = Loc.Format("geckolist.delete_details", index + 1,
            Loc.StageName(gecko.growthStage), GeckoBond.Level(gecko));
        _deletePanel.transform.SetAsLastSibling();
        _deletePanel.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_cancelDeleteButton.gameObject);
    }

    private void ConfirmDelete()
    {
        if (string.IsNullOrEmpty(_deleteGeckoId) || SceneRouter.IsTransitioning) return;

        string id = _deleteGeckoId;
        CloseDeleteDialog();
        var game = GameManager.Instance;
        if (!game.Gecko.TryDeleteGecko(id))
        {
            ShowError(Loc.Get(game.GetPlayerData().geckos.Count <= 1
                ? "geckolist.delete_last" : "geckolist.delete_unavailable"));
            return;
        }

        RefreshGeckoList();
        RefreshBookBadge();
    }

    private void CloseDeleteDialog()
    {
        _deleteGeckoId = null;
        if (_deletePanel == null || !_deletePanel.activeSelf) return;

        var events = EventSystem.current;
        if (events != null && events.currentSelectedGameObject != null
            && events.currentSelectedGameObject.transform.IsChildOf(_deletePanel.transform))
            events.SetSelectedGameObject(null);
        _deletePanel.SetActive(false);
    }

    private void BuildDeleteDialog(RectTransform canvas)
    {
        var overlay = CreateDeleteRect(canvas, "GeckoDeleteDialog", Vector2.zero, Vector2.one);
        _deletePanel = overlay.gameObject;
        _deletePanel.SetActive(false);
        overlay.gameObject.AddComponent<Image>().color = new Color32(0, 0, 0, 185);

        var card = CreateDeleteRect(overlay, "Card", new Vector2(.08f, .5f), new Vector2(.92f, .5f));
        card.offsetMin = new Vector2(0f, -310f);
        card.offsetMax = new Vector2(0f, 310f);
        card.gameObject.AddComponent<Image>().color = new Color32(37, 40, 55, 255);

        var title = CreateDeleteLabel(card, "Title", new Vector2(.08f, .83f), new Vector2(.92f, .95f), 46f);
        title.text = Loc.Get("geckolist.delete_title");
        title.fontStyle = FontStyles.Bold;
        _deleteName = CreateDeleteLabel(card, "Name", new Vector2(.08f, .71f), new Vector2(.92f, .82f), 40f);
        _deleteName.fontStyle = FontStyles.Bold;
        _deleteName.textWrappingMode = TextWrappingModes.NoWrap;
        _deleteName.overflowMode = TextOverflowModes.Truncate;
        _deleteDetails = CreateDeleteLabel(card, "Details", new Vector2(.08f, .61f), new Vector2(.92f, .70f), 32f);
        _deleteDetails.color = new Color32(199, 207, 225, 255);
        var warning = CreateDeleteLabel(card, "Warning", new Vector2(.08f, .38f), new Vector2(.92f, .58f), 36f);
        warning.text = Loc.Get("geckolist.delete_warning");
        var irreversible = CreateDeleteLabel(card, "Irreversible", new Vector2(.08f, .26f), new Vector2(.92f, .36f), 34f);
        irreversible.text = Loc.Get("geckolist.delete_irreversible");
        irreversible.fontStyle = FontStyles.Bold;
        irreversible.color = new Color32(255, 207, 166, 255);

        _cancelDeleteButton = CreateDeleteButton(card, "Cancel", new Vector2(.08f, .07f), new Vector2(.48f, .23f),
            new Color32(67, 74, 94, 255), Loc.Get("common.cancel"), CloseDeleteDialog);
        var confirm = CreateDeleteButton(card, "Delete", new Vector2(.52f, .07f), new Vector2(.92f, .23f),
            new Color32(155, 53, 58, 255), Loc.Get("geckolist.delete_confirm"), ConfirmDelete);
        _cancelDeleteButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = confirm };
        confirm.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = _cancelDeleteButton };
    }

    private Button CreateDeleteButton(RectTransform parent, string name, Vector2 min, Vector2 max,
        Color color, string label, UnityAction onClick)
    {
        var rect = CreateDeleteRect(parent, name, min, max);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        UIPressScale.Ensure(button);
        CreateDeleteLabel(rect, "Label", new Vector2(.05f, .05f), new Vector2(.95f, .95f), 36f).text = label;
        return button;
    }

    private TMP_Text CreateDeleteLabel(RectTransform parent, string name, Vector2 min, Vector2 max, float size)
    {
        var label = CreateDeleteRect(parent, name, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        if (_coinText != null && _coinText.font != null) label.font = _coinText.font;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 23f;
        label.fontSizeMax = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Masking;
        label.richText = false;
        label.raycastTarget = false;
        SceneTextLocalizer.Ignore(label);
        return label;
    }

    private static RectTransform CreateDeleteRect(RectTransform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    // ── 분양 패널 ─────────────────────────────────────────────

    private const float SPECIES_ROW_HEIGHT = 112f;

    private void EnsureSpeciesDropdown()
    {
        if (_speciesDropdown.template != null) return;

        var root = (RectTransform)_speciesDropdown.transform;
        var font = _coinText != null ? _coinText.font : TMP_Settings.defaultFontAsset;
        var caption = CreateSpeciesLabel(root, "Caption", font, 80f);
        var placeholder = CreateSpeciesLabel(root, "Placeholder", font, 80f);
        placeholder.color = new Color32(209, 209, 219, 255);

        // 화살표는 글꼴에 없는 기호 대신 두 개의 Image로 표시한다.
        for (int side = -1; side <= 1; side += 2)
        {
            var stroke = new GameObject("Arrow Stroke", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)stroke.transform;
            rect.SetParent(root, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-39f + side * 8f, 0f);
            rect.sizeDelta = new Vector2(22f, 5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, side * 40f);
            stroke.GetComponent<Image>().raycastTarget = false;
        }

        var menu = new GameObject("Species Menu", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        menu.SetActive(false);
        var template = (RectTransform)menu.transform;
        template.SetParent(root, false);
        template.anchorMin = Vector2.zero;
        template.anchorMax = Vector2.right;
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, -8f);
        template.sizeDelta = new Vector2(0f, 4f * SPECIES_ROW_HEIGHT);
        menu.GetComponent<Image>().color = new Color32(46, 46, 57, 255);

        var viewport = (RectTransform)new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)).transform;
        viewport.SetParent(template, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;

        var content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
        content.SetParent(viewport, false);
        content.anchorMin = Vector2.up;
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, SPECIES_ROW_HEIGHT);

        var row = new GameObject("Species Item", typeof(RectTransform), typeof(Image), typeof(Toggle));
        var rowRect = (RectTransform)row.transform;
        rowRect.SetParent(content, false);
        rowRect.anchorMin = new Vector2(0f, 0.5f);
        rowRect.anchorMax = new Vector2(1f, 0.5f);
        rowRect.anchoredPosition = Vector2.zero;
        rowRect.sizeDelta = new Vector2(0f, SPECIES_ROW_HEIGHT);
        var rowImage = row.GetComponent<Image>();
        rowImage.color = new Color32(55, 55, 69, 255);
        var toggle = row.GetComponent<Toggle>();
        toggle.targetGraphic = rowImage;
        var colors = toggle.colors;
        colors.pressedColor = new Color(0.72f, 0.83f, 0.93f);
        toggle.colors = colors;

        var scroll = menu.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        _speciesDropdown.template = template;
        _speciesDropdown.captionText = caption;
        _speciesDropdown.placeholder = placeholder;
        _speciesDropdown.itemText = CreateSpeciesLabel(rowRect, "Item Label", font, 28f);
    }

    private static TMP_Text CreateSpeciesLabel(RectTransform parent, string name, TMP_FontAsset font, float rightInset)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var rect = label.rectTransform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(28f, 8f);
        rect.offsetMax = new Vector2(-rightInset, -8f);
        label.font = font;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = 28f;
        label.fontSizeMax = 46f;
        label.raycastTarget = false;
        return label;
    }

    private void BuildSpeciesDropdown()
    {
        _dropdownSpecies.Clear();
        UpdateAdoptSelection(-1);
        if (_speciesDropdown == null) return;

        EnsureSpeciesDropdown();
        if (_speciesDropdown.placeholder is TMP_Text placeholder)
            placeholder.text = Loc.Get("geckolist.species_placeholder");

        var data    = GameManager.Instance.GetPlayerData();
        var options = new System.Collections.Generic.List<string>();
        foreach (var species in _speciesForSale ?? System.Array.Empty<GeckoSpeciesSO>())
        {
            if (species == null) continue;
            _dropdownSpecies.Add(species);
            string name = Loc.SpeciesName(species);
            string label = StoreManager.IsFreeFor(data, species)
                ? Loc.Format("geckolist.option_free", name)
                : Loc.Format("geckolist.option_price", name, species.coinPrice);
            options.Add(label);
        }

        _speciesDropdown.ClearOptions();
        _speciesDropdown.AddOptions(options);
        _speciesDropdown.interactable = options.Count > 0;
    }

    private void UpdateAdoptSelection(int index) =>
        _confirmAdoptButton.interactable = index >= 0 && index < _dropdownSpecies.Count;

    private void OpenAdoptPanel()
    {
        if (!StoreManager.CanAdoptMore(GameManager.Instance.GetPlayerData()))
        {
            OnPurchaseFailedHandler(Loc.Format("geckolist.full", StoreManager.MAX_GECKOS));
            return;
        }
        BuildSpeciesDropdown();
        if (_adoptPanel != null) _adoptPanel.SetActive(true);
        if (_nameInputField != null) _nameInputField.text = "";
    }

    private void CloseAdoptPanel()
    {
        if (_adoptPanel != null) _adoptPanel.SetActive(false);
    }

    private void OnConfirmAdopt()
    {
        if (_speciesDropdown == null) return;
        int index = _speciesDropdown.value;
        if (index < 0 || index >= _dropdownSpecies.Count) return;

        var species = _dropdownSpecies[index];
        string name = _nameInputField != null ? _nameInputField.text.Trim() : "";

        _store.BuyGecko(species, name);
        CloseAdoptPanel();
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    private void OnGeckoPurchasedHandler(GeckoData gecko)
    {
        RefreshCurrency();
        RefreshGeckoList();
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
