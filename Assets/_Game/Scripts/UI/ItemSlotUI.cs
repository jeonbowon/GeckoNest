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
            _buyButton.onClick.AddListener(OnBuyClicked);
        else
            Debug.LogWarning("[ItemSlotUI] _buyButton이 연결되지 않았습니다.", this);
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
        if (_nameText  != null) _nameText.text    = item.displayName;
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
