using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GeckoList 게코 슬롯 프리팹에 부착.
/// GeckoListUIController.RefreshGeckoList() 에서 Setup() 호출로 초기화된다.
///
/// 프리팹 구조:
///   GeckoSlot (GeckoSlotUI)
///     NameText       — TMP_Text  (게코 이름)
///     StageText      — TMP_Text  (성장 단계명)
///     SelectButton   — Button    (탭 시 이 게코를 선택하고 홈으로)
/// </summary>
public class GeckoSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _stageText;
    [SerializeField] private Button   _selectButton;

    private GeckoData         _gecko;
    private Action<GeckoData> _onSelect;

    private static readonly string[] STAGE_NAMES =
        { "Hatchling", "Baby", "Juvenile", "Sub-Adult", "Adult" };

    private void Awake()
    {
        if (_selectButton != null)
            _selectButton.onClick.AddListener(OnSelectClicked);
        else
            Debug.LogWarning("[GeckoSlotUI] _selectButton이 연결되지 않았습니다.", this);
    }

    private void OnDestroy()
    {
        if (_selectButton != null)
            _selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    public void Setup(GeckoData gecko, Action<GeckoData> onSelect)
    {
        _gecko    = gecko;
        _onSelect = onSelect;

        if (_nameText  != null) _nameText.text  = gecko.name;
        if (_stageText != null)
        {
            int stage = Mathf.Clamp(gecko.growthStage, 0, STAGE_NAMES.Length - 1);
            _stageText.text = STAGE_NAMES[stage];
        }
    }

    private void OnSelectClicked()
    {
        _onSelect?.Invoke(_gecko);
    }
}
