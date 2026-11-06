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
        _onBuy?.Invoke(_item);
        RefreshCount();
    }
}
