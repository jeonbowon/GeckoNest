using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Terrarium.unity 장식 슬롯 프리팹에 부착.
/// TerrariumUIController.BuildDecorPanel() 에서 Setup() 호출로 초기화된다.
///
/// 프리팹 구조:
///   DecorSlot (DecorSlotUI)
///     IconImage   — Image
///     NameText    — TMP_Text
///     PriceText   — TMP_Text  ("15 C" / "5 G" / "Free")
///     SelectButton — Button
/// </summary>
public class DecorSlotUI : MonoBehaviour
{
    [SerializeField] private Image    _iconImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private Button   _selectButton;

    private DecorItemSO        _item;
    private Action<DecorItemSO> _onSelect;

    private void Awake()
    {
        if (_selectButton != null)
            _selectButton.onClick.AddListener(OnSelectClicked);
        else
            Debug.LogWarning("[DecorSlotUI] _selectButton이 연결되지 않았습니다.", this);
    }

    private void OnDestroy()
    {
        if (_selectButton != null)
            _selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    public void Setup(DecorItemSO item, Action<DecorItemSO> onSelect, bool isSelected = false)
    {
        _item     = item;
        _onSelect = onSelect;

        if (_iconImage  != null) _iconImage.sprite = item.icon;
        if (_nameText   != null) _nameText.text    = item.displayName;
        if (_priceText  != null)
        {
            if (item.gemPrice > 0)
                _priceText.text = $"{item.gemPrice} G";
            else if (item.coinPrice > 0)
                _priceText.text = $"{item.coinPrice} C";
            else
                _priceText.text = "Free";
        }

        SetSelected(isSelected);
    }

    public void SetSelected(bool selected)
    {
        if (_selectButton != null)
        {
            var colors = _selectButton.colors;
            colors.normalColor = selected ? new Color(0.6f, 0.9f, 0.6f) : Color.white;
            _selectButton.colors = colors;
        }
    }

    private void OnSelectClicked() => _onSelect?.Invoke(_item);
}
