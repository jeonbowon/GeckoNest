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
///     SelectButton   — Button    (카드 배경 · 짙은 색, 탭 시 이 게코를 선택하고 홈으로) — 맨 앞이어야 글자를 덮지 않는다
///     NameText       — TMP_Text  (게코 이름, 흰색 · 누르기 받지 않음)
///     StageText      — TMP_Text  (성장 단계명, 연회색 · 누르기 받지 않음)
///     StatusText · HomeText — 실행 중 생성 (오른쪽 상태 · "홈에 있어요"), 홈 게코는 카드에 초록 Outline
/// </summary>
public class GeckoSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _stageText;
    [SerializeField] private Button   _selectButton;

    private GeckoData         _gecko;
    private Action<GeckoData> _onSelect;

    private void Awake()
    {
        SceneTextLocalizer.Ignore(_nameText);   // 게코 이름은 사용자 데이터 — 번역표 원문과 같아도 그대로

        if (_selectButton != null)
        {
            _selectButton.onClick.AddListener(OnSelectClicked);
            UIPressScale.Ensure(_selectButton);   // 나중에 생성되는 버튼이라 직접 붙인다
            _selectButton.GetComponent<UIPressScale>().SetTarget(transform);   // 버튼은 카드 배경 — 글자와 함께 눌리게
        }
        else
            Debug.LogWarning("[GeckoSlotUI] _selectButton이 연결되지 않았습니다.", this);
    }

    private void OnDestroy()
    {
        if (_selectButton != null)
            _selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    public void Setup(GeckoData gecko, bool atHome, Action<GeckoData> onSelect)
    {
        _gecko    = gecko;
        _onSelect = onSelect;

        if (_nameText  != null) _nameText.text  = gecko.name;
        if (_stageText != null)
            _stageText.text = GeckoManager.IsAdult(gecko)
                ? Loc.Format("geckolist.grown", Loc.StageName(gecko.growthStage))   // "어덜트 - 다 자람"
                : Loc.StageName(gecko.growthStage);

        // 오른쪽: 돌봄 필요 표시 (위) · 지금 홈에 있는 게코 (아래)
        var alert = GeckoManager.AlertOf(gecko);
        var status = EnsureSideText(ref _statusText, "StatusText", 16f, 28f);
        if (status != null)
        {
            bool gift = alert == GeckoAlert.None && RewardManager.CanGift(gecko);   // 급한 일이 없을 때만
            status.text  = Loc.Get(gift ? "geckolist.status_gift" : AlertKey(alert));
            status.color = gift ? COLOR_GIFT : AlertColor(alert);
        }
        var home = EnsureSideText(ref _homeText, "HomeText", -20f, 22f);
        if (home != null)
        {
            home.gameObject.SetActive(atHome);
            home.text  = Loc.Get("geckolist.here");
            home.color = COLOR_SUB;
        }

        // 홈에 있는 게코는 카드에 초록 테두리
        if (_selectButton != null && _selectButton.targetGraphic != null)
        {
            var outline = _selectButton.targetGraphic.GetComponent<Outline>();
            if (outline == null && atHome)
            {
                outline = _selectButton.targetGraphic.gameObject.AddComponent<Outline>();
                outline.effectColor    = COLOR_OK;
                outline.effectDistance = new Vector2(4f, -4f);
            }
            if (outline != null) outline.enabled = atHome;
        }
    }

    // ── 오른쪽 글자 (실행 중에 만든다) ──

    private TMP_Text _statusText;
    private TMP_Text _homeText;

    private static readonly Color COLOR_SICK  = new Color(0.90f, 0.25f, 0.25f);
    private static readonly Color COLOR_NEED  = new Color(0.95f, 0.55f, 0.15f);
    private static readonly Color COLOR_DIRTY = new Color(0.85f, 0.70f, 0.10f);
    private static readonly Color COLOR_OK    = new Color(0.30f, 0.72f, 0.35f);
    private static readonly Color COLOR_GIFT  = new Color(1.00f, 0.80f, 0.30f);   // 홈 선물 상자와 같은 금색
    private static readonly Color COLOR_SUB  = new Color(0.72f, 0.75f, 0.82f);   // 단계 글자와 같은 연회색 (프리팹)

    public static string AlertKey(GeckoAlert alert)
    {
        switch (alert)
        {
            case GeckoAlert.Sick:    return "geckolist.status_sick";
            case GeckoAlert.Hungry:  return "geckolist.status_hungry";
            case GeckoAlert.Thirsty: return "geckolist.status_thirsty";
            case GeckoAlert.Dirty:   return "geckolist.status_dirty";
            default:                 return "geckolist.status_ok";
        }
    }

    private static Color AlertColor(GeckoAlert alert)
    {
        switch (alert)
        {
            case GeckoAlert.Sick:    return COLOR_SICK;
            case GeckoAlert.Hungry:
            case GeckoAlert.Thirsty: return COLOR_NEED;
            case GeckoAlert.Dirty:   return COLOR_DIRTY;
            default:                 return COLOR_OK;
        }
    }

    // 슬롯 오른쪽 끝에 붙는 한 줄 — 이름 글자의 글꼴·색을 따른다
    private TMP_Text EnsureSideText(ref TMP_Text text, string objName, float y, float size)
    {
        if (text != null) return text;
        if (_nameText == null) return null;

        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot            = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-24f, y);
        rt.sizeDelta        = new Vector2(220f, 34f);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.font               = _nameText.font;
        t.fontSharedMaterial = _nameText.fontSharedMaterial;
        t.color              = _nameText.color;
        t.fontSize           = size;
        t.fontStyle          = FontStyles.Bold;
        t.alignment          = TextAlignmentOptions.MidlineRight;
        t.textWrappingMode   = TextWrappingModes.NoWrap;
        t.overflowMode       = TextOverflowModes.Ellipsis;
        t.raycastTarget      = false;   // 슬롯 버튼 누르기를 막지 않게
        SceneTextLocalizer.Ignore(t);   // 이미 번역된 글자를 넣는다
        go.transform.SetAsLastSibling();
        text = t;
        return t;
    }

    private void OnSelectClicked()
    {
        _onSelect?.Invoke(_gecko);
    }
}
