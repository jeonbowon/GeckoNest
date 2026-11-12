using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Store 아이템 슬롯 프리팹에 부착.
/// StoreUIController.BuildItemList() 에서 Setup() 호출로 초기화된다.
///
/// 프리팹 구조:
///   ItemSlot (ItemSlotUI)
///     IconImage      — Image
///     NameText       — TMP_Text
///     PriceText      — TMP_Text  (예: "15 C" / "5 G")
///     CountText      — TMP_Text  (보유 수량)
///     BuyButton      — Button
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image    _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private Button   _buyButton;

    private ItemSO            _item;
    private Action<ItemSO>    _onBuy;
    private Action _onExchange;

    public void SetupGemExchange(Action onExchange, bool used)
    {
        _onExchange = onExchange;
        BuildExchangeIcon();
        if (_nameText != null)
        {
            _nameText.text = Loc.Get("exchange.title");
            Place(_nameText.rectTransform, 0, 1, 288, 104, -312, 72);
        }
        if (_priceText != null)
        {
            _priceText.text = Loc.Format("exchange.price", StoreManager.GEM_EXCHANGE_COIN, StoreManager.GEM_EXCHANGE_AMOUNT);
            Place(_priceText.rectTransform, 0, 1, 288, 24, -568, 60);
            _priceText.enableAutoSizing = true;
            _priceText.fontSizeMin = 28;
            _priceText.fontSizeMax = 46;
        }
        if (_countText != null)
        {
            Place(_countText.rectTransform, 0, 1, 288, -52, -568, 60);
            _countText.enableAutoSizing = true;
            _countText.fontSizeMin = 28;
            _countText.fontSizeMax = 42;
        }
        if (_buyButton != null)
            foreach (var text in _buyButton.GetComponentsInChildren<TMP_Text>(true)) text.text = Loc.Get("exchange.action");
        RefreshGemExchange(used);
    }

    private void BuildExchangeIcon()
    {
        if (_iconImage == null) return;
        _iconImage.gameObject.SetActive(true);
        // Keep the prefab's icon rectangle; decorative children never intercept taps.
        _iconImage.sprite = FxSprites.Dot;
        _iconImage.overrideSprite = null;
        _iconImage.color = new Color(.98f, .89f, .69f, .16f);
        _iconImage.raycastTarget = false;
        if (_iconImage.transform.Find("ExchangeArt") != null) return;
        var art = new GameObject("ExchangeArt", typeof(RectTransform));
        var root = (RectTransform)art.transform;
        root.SetParent(_iconImage.transform, false);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
        root.sizeDelta = new Vector2(240, 240);

        var brown = new Color(.36f, .23f, .16f);
        var cream = new Color(1f, .96f, .85f);
        ExchangePart(root, "CoinRim", FxSprites.Dot, new Color(.78f, .48f, .16f), -62, 35, 110, 110);
        ExchangePart(root, "Coin", FxSprites.Dot, new Color(1f, .77f, .32f), -62, 39, 94, 94);
        ExchangePart(root, "CoinShine", FxSprites.Dot, cream, -82, 65, 15, 10);
        ExchangeFace(root, -62, 35, brown);

        ExchangePart(root, "GemRim", null, new Color(.64f, .32f, .53f), 61, -27, 75, 75, 45);
        ExchangePart(root, "Gem", null, new Color(.97f, .57f, .72f), 61, -23, 65, 65, 45);
        ExchangePart(root, "GemShine", FxSprites.Sparkle, cream, 47, 2, 24, 24);
        ExchangeFace(root, 61, -30, brown);

        // A drawn arrow avoids unsupported font symbols and clearly shows one-way exchange.
        ExchangePart(root, "ArrowStem", null, cream, -5, 14, 32, 8);
        ExchangePart(root, "ArrowUpper", null, cream, 8, 20, 23, 8, -40);
        ExchangePart(root, "ArrowLower", null, cream, 8, 8, 23, 8, 40);
        ExchangePart(root, "SparkleTop", FxSprites.Sparkle, new Color(1f, .87f, .52f), 79, 77, 29, 29);
        ExchangePart(root, "SparkleBottom", FxSprites.Sparkle, cream, -78, -72, 19, 19);
    }

    private static void ExchangeFace(RectTransform root, float x, float y, Color ink)
    {
        ExchangePart(root, "Eye", FxSprites.Dot, ink, x - 13, y + 2, 9, 13);
        ExchangePart(root, "Eye", FxSprites.Dot, ink, x + 13, y + 2, 9, 13);
        var blush = new Color(1f, .49f, .51f, .75f);
        ExchangePart(root, "Cheek", FxSprites.Dot, blush, x - 23, y - 8, 15, 8);
        ExchangePart(root, "Cheek", FxSprites.Dot, blush, x + 23, y - 8, 15, 8);
        ExchangePart(root, "SmileLeft", null, ink, x - 4, y - 13, 10, 3, -28);
        ExchangePart(root, "SmileRight", null, ink, x + 4, y - 13, 10, 3, 28);
    }

    private static void ExchangePart(RectTransform parent, string name, Sprite sprite, Color color,
        float x, float y, float width, float height, float angle = 0)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        var rt = image.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, height);
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
    }

    public void RefreshGemExchange(bool used)
    {
        if (_countText != null) _countText.text = Loc.Get(used ? "exchange.used" : "exchange.limit");
        if (_buyButton != null) _buyButton.interactable = !used;
    }

    private void Awake()
    {
        if (_buyButton != null)
        {
            _buyButton.onClick.AddListener(OnBuyClicked);
            UIPressScale.Ensure(_buyButton);   // 나중에 생성되는 버튼이라 직접 붙인다
        }
        else
            Debug.LogWarning("[ItemSlotUI] _buyButton이 연결되지 않았습니다.", this);

        var root = (RectTransform)transform;
        root.sizeDelta = new Vector2(root.sizeDelta.x, 320f);
        if (_iconImage != null) { Place(_iconImage.rectTransform, 0, 0, 24, 0, 240, 240); _iconImage.preserveAspect = true; }
        if (_nameText != null) Place(_nameText.rectTransform, 0, 1, 288, 104, -312, 72);
        if (_priceText != null) Place(_priceText.rectTransform, 0, 0, 288, 24, 280, 60);
        if (_countText != null) Place(_countText.rectTransform, 0, 0, 288, -52, 280, 60);
        ReadableText(_nameText, 50); ReadableText(_priceText, 46); ReadableText(_countText, 42);
        if (_buyButton != null)
        {
            var rt = (RectTransform)_buyButton.transform;
            Place(rt, 1, 1, -24, -40, 240, 144); rt.pivot = new Vector2(1, .5f);
            foreach (var text in _buyButton.GetComponentsInChildren<TMP_Text>(true)) ReadableText(text, 46);
        }
        SceneTextLocalizer.LocalizeUnder(gameObject);   // 프리팹의 고정 글자 ("구매")
    }

    internal static void Place(RectTransform rt, float left, float right, float x, float y, float width, float height)
    {
        rt.anchorMin = new Vector2(left, .5f); rt.anchorMax = new Vector2(right, .5f);
        rt.pivot = new Vector2(0, .5f); rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(width, height);
    }
    internal static void ReadableText(TMP_Text text, float size)
    {
        if (text == null) return;
        text.fontSize = size; text.enableAutoSizing = false; text.overflowMode = TextOverflowModes.Ellipsis;
    }
    private void OnDestroy()
    {
        if (_buyButton != null)
            _buyButton.onClick.RemoveListener(OnBuyClicked);
    }

    public void Setup(ItemSO item, Action<ItemSO> onBuy)
    {
        _item  = item;
        _onBuy = onBuy;

        if (_iconImage != null) _iconImage.sprite = item.icon;
        if (_nameText  != null) _nameText.text    = Loc.ItemName(item);
        if (_priceText != null)
        {
            _priceText.text = item.gemPrice > 0
                ? $"{item.gemPrice} G"
                : $"{item.coinPrice} C";
        }

        RefreshCount();
    }

    /// <summary>보유 수량 표시 갱신. StoreUIController에서 필요 시 호출 가능.</summary>
    public void RefreshCount()
    {
        if (_countText == null || _item == null) return;
        int count = GameManager.Instance != null
            ? GameManager.Instance.GetPlayerData().inventory
                .Find(s => s.itemId == _item.itemId)?.count ?? 0
            : 0;
        _countText.text = $"x{count}";
    }

    private void OnBuyClicked()
    {
        if (_onExchange != null) { _onExchange(); return; }
        _onBuy?.Invoke(_item);
        RefreshCount();
    }
}
