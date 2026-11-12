using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Store.unity 에 부착. StoreManager를 통해 아이템/게코 구매를 처리한다.
///
/// 씬 구성 (Unity Editor에서 직접 배치):
///   Canvas
///     TopBar           — 코인/젬 표시
///     ItemListPanel    — 아이템 목록 (ScrollView > Content 에 ItemSlot 프리팹 배치)
///     BackButton       — 홈으로 돌아가기
///     ErrorPanel       — 구매 실패 알림 (2.5초 자동 숨김)
///       ErrorText
/// </summary>
public class StoreUIController : MonoBehaviour
{
    [Header("상단 바")]
    [SerializeField] private TMP_Text _coinText;
    [SerializeField] private TMP_Text _gemText;

    [Header("아이템 슬롯 루트 (ScrollView > Viewport > Content)")]
    [SerializeField] private Transform _itemListContent;
    [SerializeField] private GameObject _itemSlotPrefab; // ItemSlot 프리팹

    [Header("판매 아이템 목록 (Inspector에서 드래그)")]
    [SerializeField] private ItemSO[] _itemsForSale;

    [Header("오류 알림")]
    [SerializeField] private GameObject _errorPanel;
    [SerializeField] private TMP_Text   _errorText;
    private const float ERROR_DISPLAY_SECONDS = 2.5f;
    private Coroutine   _errorCoroutine;

    [Header("뒤로가기")]
    [SerializeField] private Button _backButton;

    private StoreManager _store;
    private ItemSlotUI _exchangeSlot;
    private GameObject _exchangeDialog;
    private float _exchangeRefresh;

    // ── 생명주기 ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[StoreUIController] GameManager가 아직 없습니다 — AppBootstrap이 초기화한 뒤 이 씬을 다시 엽니다.");
            return;
        }

        _store = GameManager.Instance.Store;
        _store.OnItemPurchased  += OnItemPurchasedHandler;
        _store.OnPurchaseFailed += OnPurchaseFailedHandler;

        _backButton.onClick.AddListener(OnBackClicked);

        if (_errorPanel != null)
            _errorPanel.SetActive(false);

        RefreshCurrency();
        BuildItemList();
    }

    private void OnDisable()
    {
        CloseExchangeDialog();
        if (_store != null)
        {
            _store.OnItemPurchased  -= OnItemPurchasedHandler;
            _store.OnPurchaseFailed -= OnPurchaseFailedHandler;
        }
        _backButton.onClick.RemoveListener(OnBackClicked);
    }

    // ── 아이템 목록 생성 ──────────────────────────────────────

    private void BuildItemList()
    {
        if (_itemListContent == null || _itemSlotPrefab == null || _itemsForSale == null) return;

        var layout = _itemListContent.GetComponent<VerticalLayoutGroup>();
        if (layout != null) { layout.childControlWidth = true; layout.childForceExpandWidth = true; layout.childControlHeight = false; layout.childForceExpandHeight = false; layout.spacing = 24f; }
        // 기존 슬롯 제거 후 재생성
        foreach (Transform child in _itemListContent)
            Destroy(child.gameObject);

        var exchange = Instantiate(_itemSlotPrefab, _itemListContent);
        _exchangeSlot = exchange.GetComponent<ItemSlotUI>();
        if (_exchangeSlot != null) _exchangeSlot.SetupGemExchange(OpenExchangeDialog, _store.GemExchangeUsedToday);

        foreach (var item in _itemsForSale)
        {
            if (item == null) continue;
            var go = Instantiate(_itemSlotPrefab, _itemListContent);
            var slot = go.GetComponent<ItemSlotUI>();
            if (slot != null)
                slot.Setup(item, OnBuyItemClicked);
        }
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────

    private void Update()
    {
        if (_store == null || _exchangeSlot == null || Time.unscaledTime < _exchangeRefresh) return;
        _exchangeRefresh = Time.unscaledTime + 1f;
        _exchangeSlot.RefreshGemExchange(_store.GemExchangeUsedToday);
    }

    private void CloseExchangeDialog()
    {
        if (_exchangeDialog != null)
        {
            _exchangeDialog.SetActive(false);
            Destroy(_exchangeDialog);
        }
        _exchangeDialog = null;
    }

    private void OpenExchangeDialog()
    {
        if (_store == null || _exchangeDialog != null) return;
        _exchangeDialog = new GameObject("GemExchangeConfirmation", typeof(RectTransform), typeof(Image));
        var overlay = (RectTransform)_exchangeDialog.transform;
        FitExchangeRect(overlay, transform, Vector2.zero, Vector2.one);
        overlay.SetAsLastSibling();
        _exchangeDialog.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        FitExchangeRect(card, overlay, new Vector2(.06f, .35f), new Vector2(.94f, .65f));
        card.GetComponent<Image>().color = new Color(.12f, .19f, .09f, 1);
        ExchangeLabel(card, "Message", Loc.Format("exchange.confirm", StoreManager.GEM_EXCHANGE_COIN,
            StoreManager.GEM_EXCHANGE_AMOUNT), new Vector2(.05f, .45f), new Vector2(.95f, .95f));
        ExchangeButton(card, "Confirm", Loc.Get("exchange.action"), .05f, .48f, () =>
        {
            // Closing first also makes a second click on this dialog impossible.
            CloseExchangeDialog();
            bool accepted = _store.TryExchangeGem(out string message);
            RefreshCurrency();
            if (_exchangeSlot != null) _exchangeSlot.RefreshGemExchange(_store.GemExchangeUsedToday);
            AudioManager.Play(accepted ? Sfx.Coin : Sfx.Error, .8f);
            if (accepted) Haptics.Light();
            ShowError(message);
        });
        ExchangeButton(card, "Cancel", Loc.Get("exchange.cancel"), .52f, .95f, CloseExchangeDialog);
    }

    private static void FitExchangeRect(RectTransform rect, Transform parent, Vector2 min, Vector2 max)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void ExchangeLabel(Transform parent, string name, string text, Vector2 min, Vector2 max)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        FitExchangeRect(label.rectTransform, parent, min, max);
        label.font = _coinText != null ? _coinText.font : TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = 38; label.enableAutoSizing = true; label.fontSizeMin = 26; label.fontSizeMax = 38;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.97f, .95f, .85f);
        label.raycastTarget = false;
    }

    private void ExchangeButton(Transform parent, string name, string text, float min, float max,
        UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        FitExchangeRect((RectTransform)go.transform, parent, new Vector2(min, .08f), new Vector2(max, .35f));
        var image = go.GetComponent<Image>();
        image.color = new Color(.30f, .42f, .22f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        UIPressScale.Ensure(button);
        ExchangeLabel(go.transform, "Label", text, new Vector2(.03f, .03f), new Vector2(.97f, .97f));
    }

    private void OnBuyItemClicked(ItemSO item)
    {
        _store.BuyItem(item, 1);
    }

    private void OnBackClicked()
    {
        SceneRouter.GoToHome();
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    private void OnItemPurchasedHandler(string itemId, int newCount)
    {
        RefreshCurrency();
        AudioManager.Play(Sfx.Coin, 0.8f);
        Haptics.Light();
        Debug.Log($"[StoreUIController] 구매 완료 — {itemId} (보유: {newCount})");
    }

    private void OnPurchaseFailedHandler(string reason)
    {
        AudioManager.Play(Sfx.Error, 0.8f);
        ShowError(reason);
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    private void RefreshCurrency()
    {
        var data = GameManager.Instance.GetPlayerData();
        if (_coinText != null) _coinText.text = Loc.Format("hud.coin", data.coin.ToString("N0"));
        if (_gemText  != null) _gemText.text  = Loc.Format("hud.gem",  data.gem.ToString("N0"));
    }

    private void ShowError(string message)
    {
        if (_errorPanel == null || _errorText == null) return;
        if (_errorCoroutine != null) StopCoroutine(_errorCoroutine);
        _errorText.text = message;
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
