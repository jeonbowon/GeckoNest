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
///     PriceText   — TMP_Text  ("15 C" / "5 G" / 무료 · 보유 · 빼기)
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
        {
            _selectButton.onClick.AddListener(OnSelectClicked);
            UIPressScale.Ensure(_selectButton);   // 나중에 생성되는 버튼이라 직접 붙인다
        }
        else
            Debug.LogWarning("[DecorSlotUI] _selectButton이 연결되지 않았습니다.", this);
    }

    private void OnDestroy()
    {
        if (_selectButton != null)
            _selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    private const float LOCKED_ICON_ALPHA = 0.35f;

    /// <summary>canRemove = 이미 놓은 장식 (다시 누르면 빼낸다) · locked = 어덜트 수가 모자라 잠김 (가격 대신 조건)</summary>
    public void Setup(DecorItemSO item, Action<DecorItemSO> onSelect, bool isSelected = false, bool owned = false,
                      bool canRemove = false, bool locked = false)
    {
        _item     = item;
        _onSelect = onSelect;

        if (_iconImage  != null)
        {
            _iconImage.sprite         = item.icon;
            _iconImage.preserveAspect = true;   // 세로로 긴 구조물(뒤판·덩굴) 그림이 찌그러지지 않게
            var c = _iconImage.color;
            c.a = locked ? LOCKED_ICON_ALPHA : 1f;
            _iconImage.color = c;
        }
        if (_nameText   != null) _nameText.text    = Loc.DecorName(item);
        if (_priceText  != null)
        {
            bool paid = item.gemPrice > 0 || item.coinPrice > 0;
            if (locked && !canRemove)
                _priceText.text = Loc.Format("terrarium.locked", item.requiredAdults);   // "어덜트 2마리"
            else if (canRemove)
                _priceText.text = Loc.Get("common.remove");
            else if (owned && paid)
                _priceText.text = Loc.Get("common.owned");
            else if (item.gemPrice > 0)
                _priceText.text = $"{item.gemPrice} G";
            else if (item.coinPrice > 0)
                _priceText.text = $"{item.coinPrice} C";
            else
                _priceText.text = Loc.Get("common.free");
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

    private void OnSelectClicked()
    {
        Debug.Log($"[DecorSlotUI] 클릭됨 — item={(_item != null ? _item.itemId : "null")}, onSelect={(object)_onSelect ?? "null"}");
        _onSelect?.Invoke(_item);
    }
}
