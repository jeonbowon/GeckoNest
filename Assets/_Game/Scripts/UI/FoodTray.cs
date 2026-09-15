using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 먹이 선반 — 먹이 버튼을 누르면 돌봄 버튼 줄 위에 가진 먹이를 펼쳐 보여 주고, 고른 먹이를 준다.
/// HomeUIController가 실행 중에 만든다 (씬에 오브젝트를 두지 않음). 선반 밖을 누르면 닫힌다.
///
/// 칸: 아이콘 · 이름 · 개수 · 효과(배고픔·성장·기분·건강·허물) · 좋아하는 먹이 표시.
/// 효과 설명(DescribeEffects)은 먹은 뒤 말풍선도 같이 쓴다.
/// </summary>
public class FoodTray : MonoBehaviour
{
    public struct Option
    {
        public ItemSO item;
        public int    count;
        public bool   favorite;
    }

    private const float BOTTOM      = 320f;   // 돌봄 버튼 줄(아래 136~308) 바로 위
    private const float HEIGHT      = 212f;
    private const float SIDE        = 48f;
    private const float SLOT_WIDTH  = 128f;   // 7칸이 1080 폭 안에 들어가는 크기
    private const float SLOT_HEIGHT = 188f;
    private const float POP_TIME    = 0.14f;

    private static readonly Color PANEL_COLOR    = new Color(0.08f, 0.1f, 0.08f, 0.88f);
    private static readonly Color SLOT_COLOR     = new Color(0.2f, 0.27f, 0.2f, 0.95f);
    private static readonly Color TEXT_COLOR     = new Color(0.93f, 0.97f, 0.9f);
    private static readonly Color EFFECT_COLOR   = new Color(0.78f, 0.92f, 0.72f);
    private static readonly Color FAVORITE_COLOR = new Color(1f, 0.56f, 0.64f);   // #FF8FA3 하트 핑크

    private RectTransform  _panel;
    private GameObject     _blocker;
    private TMP_FontAsset  _font;
    private Sprite         _roundSprite;
    private Action<ItemSO> _onPick;
    private float          _pop = 1f;

    public bool IsOpen => _panel != null && _panel.gameObject.activeSelf;

    // ── 생성 ──────────────────────────────────────────────────

    /// <summary>root = 홈 화면 Canvas. roundSprite = 둥근 판 그림 (9-slice)</summary>
    public static FoodTray Create(RectTransform root, TMP_FontAsset font, Sprite roundSprite)
    {
        var go = new GameObject("FoodTray", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        Stretch(rt);

        var tray = go.AddComponent<FoodTray>();
        tray._font        = font;
        tray._roundSprite = roundSprite;

        // 선반 밖 — 투명한 화면 전체. 누르면 닫는다
        var blocker = new GameObject("Blocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var blockerRt = (RectTransform)blocker.transform;
        blockerRt.SetParent(rt, false);
        Stretch(blockerRt);
        blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        var blockerButton = blocker.GetComponent<Button>();
        blockerButton.transition = Selectable.Transition.None;
        blockerButton.onClick.AddListener(tray.Close);
        tray._blocker = blocker;

        // 선반 — 돌봄 버튼 줄 바로 위, 칸을 가로로 나란히
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        var panelRt = (RectTransform)panel.transform;
        panelRt.SetParent(rt, false);
        panelRt.anchorMin        = new Vector2(0f, 0f);
        panelRt.anchorMax        = new Vector2(1f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, BOTTOM);
        panelRt.sizeDelta        = new Vector2(-SIDE * 2f, HEIGHT);
        SetRoundImage(panel.GetComponent<Image>(), roundSprite, PANEL_COLOR);   // 판 빈 곳을 눌러도 닫히지 않게 터치를 받는다

        var layout = panel.GetComponent<HorizontalLayoutGroup>();
        layout.padding                = new RectOffset(12, 12, 12, 12);
        layout.spacing                = 10f;
        layout.childAlignment         = TextAnchor.MiddleCenter;
        layout.childControlWidth      = false;
        layout.childControlHeight     = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;
        tray._panel = panelRt;

        tray.SetVisible(false);
        return tray;
    }

    // ── 열기 · 닫기 ───────────────────────────────────────────

    public void Open(IList<Option> options, Action<ItemSO> onPick)
    {
        _onPick = onPick;

        foreach (Transform child in _panel)
        {
            child.gameObject.SetActive(false);   // Destroy는 프레임 끝에 지워진다 — 이번 배치에서 빼려고 먼저 끈다
            Destroy(child.gameObject);
        }
        foreach (var option in options)
            if (option.item != null) CreateSlot(option);

        transform.SetAsLastSibling();
        SetVisible(true);
        _pop = 0f;
        _panel.localScale = new Vector3(1f, 0f, 1f);
        AudioManager.Play(Sfx.Pop, 0.6f);
    }

    public void Close()
    {
        if (!IsOpen) return;
        SetVisible(false);
        _onPick = null;
    }

    private void OnDisable()
    {
        if (_panel != null) SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        _blocker.SetActive(visible);
        _panel.gameObject.SetActive(visible);
    }

    private void Pick(ItemSO item)
    {
        var onPick = _onPick;
        Close();
        onPick?.Invoke(item);
    }

    private void Update()
    {
        if (_pop >= 1f || _panel == null) return;
        _pop = Mathf.Min(1f, _pop + Time.unscaledDeltaTime / POP_TIME);
        _panel.localScale = new Vector3(1f, EaseOutBack(_pop), 1f);   // 아래(피벗)에서 위로 톡 펼쳐진다
    }

    // ── 칸 ────────────────────────────────────────────────────

    private void CreateSlot(Option option)
    {
        var item = option.item;
        var slot = new GameObject("Slot_" + item.itemId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var slotRt = (RectTransform)slot.transform;
        slotRt.SetParent(_panel, false);
        slotRt.sizeDelta = new Vector2(SLOT_WIDTH, SLOT_HEIGHT);
        SetRoundImage(slot.GetComponent<Image>(), _roundSprite, SLOT_COLOR);

        var button = slot.GetComponent<Button>();
        button.onClick.AddListener(() => Pick(item));
        UIPressScale.Ensure(button);

        var icon = NewChild<Image>(slotRt, "Icon", new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(64f, 64f));
        icon.sprite         = item.icon;
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        icon.enabled        = item.icon != null;

        var name = NewText(slotRt, "Name", new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.56f), 18f, TEXT_COLOR, TextAlignmentOptions.Center);
        name.text             = Loc.ItemName(item);
        name.fontStyle        = FontStyles.Bold;
        name.enableAutoSizing = true;   // "칼슘+비타민 더스팅"처럼 긴 이름은 자동으로 작게
        name.fontSizeMin      = 11f;
        name.fontSizeMax      = 18f;

        var effects = NewText(slotRt, "Effects", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.42f), 14f, EFFECT_COLOR, TextAlignmentOptions.Top);
        effects.text             = DescribeItem(item, option.favorite, 3, "\n");
        effects.enableAutoSizing = true;
        effects.fontSizeMin      = 10f;
        effects.fontSizeMax      = 14f;

        var count = NewText(slotRt, "Count", new Vector2(0.5f, 0.86f), new Vector2(0.95f, 0.99f), 16f, TEXT_COLOR, TextAlignmentOptions.TopRight);
        count.text = "x" + option.count;

        if (option.favorite)
        {
            var favorite = NewText(slotRt, "Favorite", new Vector2(0.05f, 0.86f), new Vector2(0.6f, 0.99f), 14f, FAVORITE_COLOR, TextAlignmentOptions.TopLeft);
            favorite.text      = Loc.Get("food.favorite");
            favorite.fontStyle = FontStyles.Bold;
        }
    }

    // ── 효과 설명 (선반 · 말풍선 공용) ─────────────────────────

    /// <summary>먹이 에셋 기준 예상 효과 — 좋아하는 먹이면 기분 보너스를 더한다</summary>
    public static string DescribeItem(ItemSO item, bool favorite, int maxLines, string separator)
    {
        if (item == null) return "";
        return DescribeEffects(item.hungerRestore, item.growthExpGain,
                               item.moodBonus + (favorite ? GeckoManager.FAVORITE_MOOD_BONUS : 0f),
                               item.healthRestore, item.moltBonus, maxLines, separator);
    }

    /// <summary>0보다 큰 효과만 "배고픔 +32" 형태로. moltBonus는 0.1 = 10%</summary>
    public static string DescribeEffects(float hunger, float growthExp, float mood, float health, float moltBonus, int maxLines, string separator)
    {
        var parts = new List<string>();
        void Add(string key, float value)
        {
            int n = Mathf.RoundToInt(value);
            if (n > 0 && parts.Count < maxLines) parts.Add(Loc.Format(key, n));
        }
        Add("food.hunger", hunger);
        Add("food.growth", growthExp);
        Add("food.mood",   mood);
        Add("food.health", health);
        Add("food.molt",   moltBonus * 100f);
        return string.Join(separator, parts);
    }

    // ── 도구 ──────────────────────────────────────────────────

    private T NewChild<T>(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size) where T : Graphic
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        return go.AddComponent<T>();
    }

    private TMP_Text NewText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float size, Color color, TextAlignmentOptions alignment)
    {
        var text = NewChild<TextMeshProUGUI>(parent, name, anchorMin, anchorMax, Vector2.zero);
        if (_font != null) text.font = _font;
        text.fontSize      = size;
        text.color         = color;
        text.alignment     = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRoundImage(Image image, Sprite sprite, Color color)
    {
        image.sprite = sprite;
        image.type   = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.color  = color;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static float EaseOutBack(float x)
    {
        const float C1 = 1.6f, C3 = C1 + 1f;
        float t = x - 1f;
        return 1f + C3 * t * t * t + C1 * t * t;
    }
}
