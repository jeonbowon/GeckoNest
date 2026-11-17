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
    private TMP_Text _actionText;
    private TMP_Text _previewHint;

    private void Awake()
    {
        if (_iconImage != null)
        {
            _iconImage.raycastTarget = true;
            var previewButton = _iconImage.GetComponent<Button>();
            if (previewButton == null) previewButton = _iconImage.gameObject.AddComponent<Button>();
            previewButton.targetGraphic = _iconImage;
            previewButton.onClick.AddListener(ShowPreview);
        }
        if (_selectButton != null)
        {
            _selectButton.onClick.AddListener(OnSelectClicked);
            UIPressScale.Ensure(_selectButton);   // 나중에 생성되는 버튼이라 직접 붙인다
            // 기존 카드 전체의 투명 버튼을 하단의 명시적인 동작 버튼으로 제한한다.
            Region((RectTransform)_selectButton.transform, .04f, .01f, .96f, .15f);
            if (_selectButton.targetGraphic != null)
                _selectButton.targetGraphic.color = new Color(.22f, .43f, .28f, 1f);
            _actionText = CreateLabel("ActionLabel", _selectButton.transform, 36);
            Region(_actionText.rectTransform, 0, 0, 1, 1);
        }
        else
            Debug.LogWarning("[DecorSlotUI] _selectButton이 연결되지 않았습니다.", this);

        _previewHint = CreateLabel("PreviewHint", transform, 30);
        Region(_previewHint.rectTransform, .04f, .46f, .96f, .51f);
    }

    private TMP_Text CreateLabel(string name, Transform parent, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        if (_nameText != null) label.font = _nameText.font;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private void OnDestroy()
    {
        if (_selectButton != null)
            _selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    private static void Region(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = new Vector2(12, 4); rt.offsetMax = new Vector2(-12, -4);
    }
    private GameObject _preview;

    private void ShowPreview()
    {
        if (_item == null || _item.icon == null || _preview != null) return;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        _preview = new GameObject("DecorPreview", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)_preview.transform;
        rt.SetParent(canvas.rootCanvas.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _preview.GetComponent<Image>().color = new Color(.04f, .06f, .03f, .96f);
        _preview.GetComponent<Button>().onClick.AddListener(ClosePreview);
        var picture = new GameObject("Picture", typeof(RectTransform), typeof(Image));
        var pictureRt = (RectTransform)picture.transform;
        pictureRt.SetParent(rt, false);
        pictureRt.anchorMin = new Vector2(.06f, .18f); pictureRt.anchorMax = new Vector2(.94f, .9f);
        pictureRt.offsetMin = pictureRt.offsetMax = Vector2.zero;
        var image = picture.GetComponent<Image>();
        image.sprite = _item.icon; image.preserveAspect = true; image.raycastTarget = false;
        var label = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = (RectTransform)label.transform;
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = new Vector2(.05f, .04f); labelRt.anchorMax = new Vector2(.95f, .16f);
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        var text = label.GetComponent<TextMeshProUGUI>();
        if (_nameText != null) text.font = _nameText.font;
        text.fontSize = 48;
        text.text = Loc.DecorName(_item) + "\n" + Loc.Get("common.close");
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
    }

    private void ClosePreview() { if (_preview != null) Destroy(_preview); _preview = null; }
    private void OnDisable() { ClosePreview(); }

    private const float LOCKED_ICON_ALPHA = 0.35f;

    /// <summary>canRemove = 이미 놓은 장식 (다시 누르면 빼낸다) · locked = 어덜트 수가 모자라 잠김 (가격 대신 조건)</summary>
    public void Setup(DecorItemSO item, Action<DecorItemSO> onSelect, bool isSelected = false, bool owned = false,
                      bool canRemove = false, bool locked = false)
    {
        if (item == null) return;
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
                _priceText.text = Loc.Get("terrarium.placed");
            else if (owned && paid)
                _priceText.text = Loc.Get("common.owned");
            else if (item.gemPrice > 0)
                _priceText.text = $"{item.gemPrice} G";
            else if (item.coinPrice > 0)
                _priceText.text = $"{item.coinPrice} C";
            else
                _priceText.text = Loc.Get("common.free");

            // 효과 한 줄 — 가격 밑에 작게 (가격 칸 높이 68에 두 줄이 들어간다). 사기 전에도, 놓은 뒤에도 보인다
            string perk = PerkLabel(item.perk);
            if (!string.IsNullOrEmpty(perk))
                _priceText.text += $"\n<size={PERK_SIZE}%><color={PERK_COLOR}>{perk}</color></size>";
        }

        if (_iconImage != null) Region(_iconImage.rectTransform, .04f, .51f, .96f, .97f);
        if (_nameText != null) Region(_nameText.rectTransform, 0, .36f, 1, .46f);
        if (_priceText != null) Region(_priceText.rectTransform, 0, .16f, 1, .36f);
        if (_previewHint != null) _previewHint.text = Loc.Get("terrarium.preview");
        bool theme = item.category == DecorCategory.Background;
        bool needsPurchase = !owned && (item.coinPrice > 0 || item.gemPrice > 0);
        string actionKey = canRemove ? "terrarium.remove_action"
            : locked ? "terrarium.locked_action"
            : theme && isSelected ? "terrarium.applied"
            : needsPurchase ? (theme ? "terrarium.buy_apply" : "terrarium.buy_place")
            : theme ? "terrarium.apply" : "terrarium.place";
        if (_actionText != null) _actionText.text = Loc.Get(actionKey);
        if (_selectButton != null)
            _selectButton.interactable = canRemove || (!locked && !(theme && isSelected));
        ItemSlotUI.ReadableText(_nameText, 48);
        ItemSlotUI.ReadableText(_priceText, 44);
        SetSelected(isSelected);
    }

    private const int    PERK_SIZE  = 92;          // 가격 글자 대비 %
    private const string PERK_COLOR = "#A8DDA0";   // 연두 — 가격(노랑)과 구분

    /// <summary>장식 효과를 한 줄로 ("허물 성공 +10%"). 효과가 없으면 null — 수치는 DecorPerks에서 그대로 가져온다</summary>
    public static string PerkLabel(DecorPerk perk) => perk switch
    {
        DecorPerk.MoltRub  => Loc.Format("perk.moltrub",  Mathf.RoundToInt(DecorPerks.MOLT_RUB_BONUS * 100f)),
        DecorPerk.Droplets => Loc.Format("perk.droplets", Mathf.RoundToInt(DecorPerks.DROPLET_WATER_BONUS)),
        DecorPerk.Basking  => Loc.Format("perk.basking",  Mathf.RoundToInt(DecorPerks.BASK_HEALTH_REGEN / GeckoManager.HEALTH_REGEN * 100f)),
        DecorPerk.Shelter  => Loc.Format("perk.shelter",  Mathf.RoundToInt((1f - DecorPerks.SHELTER_MOOD_MUL) * 100f)),
        DecorPerk.Play     => Loc.Format("perk.play",     Mathf.RoundToInt(DecorPerks.PLAY_PET_AFFECTION)),
        _                  => null,
    };

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
        if (_item == null || _selectButton == null || !_selectButton.interactable) return;
        Debug.Log($"[DecorSlotUI] 클릭됨 — item={(_item != null ? _item.itemId : "null")}, onSelect={(object)_onSelect ?? "null"}");
        _onSelect?.Invoke(_item);
    }
}
