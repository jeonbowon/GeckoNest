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
        public bool   grown;      // 다 자란 게코 — 효과에 "성장 +N"을 넣지 않는다
        public bool   useless;    // 다 자란 게코에게 쓸모없는 먹이 (성장촉진제) — 효과 대신 "필요 없음"
    }

    private const float BOTTOM      = 320f;   // 돌봄 버튼 줄(아래 136~308) 바로 위
    private const float HEIGHT      = 660f;
    private const float SIDE        = 48f;
    private const float SLOT_WIDTH  = 264f;   // 그림 크기를 유지하고 나머지 먹이는 가로 스크롤
    private const float SLOT_HEIGHT = 400f;
    private const float POP_TIME    = 0.14f;

    private static readonly Color PANEL_COLOR    = new Color(0.08f, 0.1f, 0.08f, 0.88f);
    private static readonly Color SLOT_COLOR     = new Color(0.2f, 0.27f, 0.2f, 0.95f);
    private static readonly Color TEXT_COLOR     = new Color(0.93f, 0.97f, 0.9f);
    private static readonly Color EFFECT_COLOR   = new Color(0.78f, 0.92f, 0.72f);
    private static readonly Color FAVORITE_COLOR = new Color(1f, 0.56f, 0.64f);   // #FF8FA3 하트 핑크

    private RectTransform  _panel;
    private RectTransform  _content;
    private ScrollRect     _scroll;
    private TMP_Text       _hint;
    private GameObject     _blocker;
    private TMP_FontAsset  _font;
    private Sprite         _roundSprite;
    private Action<ItemSO> _onPick;
    private Action<ItemSO> _onPlay;
    private bool _playMode;
    private Button _playButton;
    private Button _directButton;
    private GameObject _newBadge;
    private const string PLAY_USED_KEY = "hako.ui.foodPlayUsed";
    private static bool _sessionPlayMode = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionMode()
    {
        _sessionPlayMode = true;
    }
    public static void MarkPlayUsed() { PlayerPrefs.SetInt(PLAY_USED_KEY, 1); PlayerPrefs.Save(); }
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
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var panelRt = (RectTransform)panel.transform;
        panelRt.SetParent(rt, false);
        panelRt.anchorMin        = new Vector2(0f, 0f);
        panelRt.anchorMax        = new Vector2(1f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, BOTTOM);
        panelRt.sizeDelta        = new Vector2(-SIDE * 2f, HEIGHT);
        SetRoundImage(panel.GetComponent<Image>(), roundSprite, PANEL_COLOR);   // 판 빈 곳을 눌러도 닫히지 않게 터치를 받는다

        tray._directButton = tray.ModeButton(panelRt, false, .03f, .49f);
        tray._playButton = tray.ModeButton(panelRt, true, .51f, .97f);
        tray._hint = tray.NewText(panelRt, "Hint", new Vector2(.03f, .65f), new Vector2(.97f, .77f), 29f, TEXT_COLOR, TextAlignmentOptions.Center);
        var badge = tray.NewText(panelRt, "NewPlay", new Vector2(.72f, .94f), new Vector2(.97f, 1f), 25f, new Color(1f, .85f, .35f), TextAlignmentOptions.Right);
        badge.text = Loc.Get("foodplay.new");
        tray._newBadge = badge.gameObject;

        // 헤더 버튼의 작은 손 움직임을 목록 드래그로 처리하지 않도록 스크롤은 카드 영역에만 둔다.
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        var viewportRt = (RectTransform)viewport.transform;
        viewportRt.SetParent(panelRt, false);
        Stretch(viewportRt);
        viewportRt.offsetMin = new Vector2(12f, 12f);
        viewportRt.offsetMax = new Vector2(-12f, -240f);
        viewport.GetComponent<Image>().color = Color.clear;

        var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        var contentRt = (RectTransform)content.transform;
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = new Vector2(0f, 1f);
        contentRt.pivot = new Vector2(0f, 0.5f);
        contentRt.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.padding                = new RectOffset(6, 6, 6, 6);
        layout.spacing                = 16f;
        layout.childAlignment         = TextAnchor.MiddleLeft;
        layout.childControlWidth      = false;
        layout.childControlHeight     = false;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;
        tray._panel = panelRt;
        tray._content = contentRt;
        content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        tray._scroll = viewport.GetComponent<ScrollRect>();
        tray._scroll.viewport = viewportRt;
        tray._scroll.content = contentRt;
        tray._scroll.horizontal = true;
        tray._scroll.vertical = false;
        tray._scroll.movementType = ScrollRect.MovementType.Clamped;

        tray.SetVisible(false);
        return tray;
    }

    // ── 열기 · 닫기 ───────────────────────────────────────────

    public void Open(IList<Option> options, Action<ItemSO> onPick, Action<ItemSO> onPlay = null)
    {
        _onPick = onPick;
        _onPlay = onPlay;
        _playButton.gameObject.SetActive(onPlay != null);
        _newBadge.SetActive(onPlay != null && PlayerPrefs.GetInt(PLAY_USED_KEY, 0) == 0);
        SetMode(_sessionPlayMode);

        foreach (Transform child in _content)
        {
            child.gameObject.SetActive(false);   // Destroy는 프레임 끝에 지워진다 — 이번 배치에서 빼려고 먼저 끈다
            Destroy(child.gameObject);
        }
        foreach (var option in options)
            if (option.item != null) CreateSlot(option);

        transform.SetAsLastSibling();
        SetVisible(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        _scroll.StopMovement();
        _content.anchoredPosition = Vector2.zero;
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

    private Button ModeButton(RectTransform parent, bool play, float left, float right)
    {
        var image = NewChild<Image>(parent, play ? "PlayMode" : "DirectMode", new Vector2(left, .78f), new Vector2(right, .94f), Vector2.zero);
        SetRoundImage(image, _roundSprite, SLOT_COLOR);
        var button = image.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        var outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, .87f, .48f); outline.effectDistance = new Vector2(3f, -3f);
        var icon = NewChild<Image>(image.rectTransform, "FoodIcon", new Vector2(.02f, .2f), new Vector2(.17f, .8f), Vector2.zero);
        icon.sprite = HomeButtonArt.Get(0); icon.preserveAspect = true; icon.raycastTarget = false;
        if (play)
        {
            var hand = NewChild<Image>(image.rectTransform, "HandIcon", new Vector2(.12f, .02f), new Vector2(.27f, .62f), Vector2.zero);
            hand.sprite = HomeButtonArt.Get(2); hand.preserveAspect = true; hand.raycastTarget = false;
        }
        var label = NewText(image.rectTransform, "Label", new Vector2(.28f, .08f), new Vector2(.97f, .92f), 34f, TEXT_COLOR, TextAlignmentOptions.Center);
        label.text = Loc.Get(play ? "foodplay.start" : "foodplay.direct");
        label.enableAutoSizing = true; label.fontSizeMin = 24f; label.fontSizeMax = 34f;
        button.onClick.AddListener(() => SelectMode(play));
        return button;
    }

    private void SelectMode(bool play)
    {
        if (play && _onPlay == null) return;
        SetMode(play);
        // 실행 중 마지막 선택만 유지한다. 창 재생성이나 일시적인 놀이 불가는 선택을 바꾸지 않는다.
        _sessionPlayMode = play;
    }

    private void SetMode(bool play)
    {
        _playMode = play && _onPlay != null;
        foreach (var button in new[] { _directButton, _playButton })
        {
            bool selected = (button == _playButton) == _playMode;
            button.GetComponent<Image>().color = selected ? new Color(.25f, .48f, .29f, 1f) : SLOT_COLOR;
            button.GetComponent<Outline>().enabled = selected;
        }
        _hint.text = Loc.Get(_playMode ? "foodplay.choose" : (_onPlay != null ? "foodplay.explain" : "food.choose_hint"));
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
        var onPick = _playMode ? _onPlay : _onPick;
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
        slotRt.SetParent(_content, false);
        slotRt.sizeDelta = new Vector2(SLOT_WIDTH, SLOT_HEIGHT);
        SetRoundImage(slot.GetComponent<Image>(), _roundSprite, SLOT_COLOR);

        var button = slot.GetComponent<Button>();
        button.onClick.AddListener(() => Pick(item));
        UIPressScale.Ensure(button);

        var icon = NewChild<Image>(slotRt, "Icon", new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(192f, 192f));
        icon.sprite         = item.icon;
        icon.preserveAspect = true;
        icon.raycastTarget  = false;
        icon.enabled        = item.icon != null;

        var name = NewText(slotRt, "Name", new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.44f), 30f, TEXT_COLOR, TextAlignmentOptions.Center);
        name.text             = Loc.ItemName(item);
        name.fontStyle        = FontStyles.Bold;
        name.enableAutoSizing = true;   // "칼슘+비타민 더스팅"처럼 긴 이름은 자동으로 작게
        name.fontSizeMin      = 26f;
        name.fontSizeMax      = 30f;

        var effects = NewText(slotRt, "Effects", new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.27f), 26f, EFFECT_COLOR, TextAlignmentOptions.Top);
        effects.text             = option.useless
            ? Loc.Get("food.useless")
            : DescribeItem(item, option.favorite, 3, "\n", includeGrowth: !option.grown);
        if (option.useless) icon.color = new Color(1f, 1f, 1f, 0.45f);   // 쓸모없는 먹이는 흐리게 (누르면 게코가 거절한다)
        effects.enableAutoSizing = true;
        effects.fontSizeMin      = 24f;
        effects.fontSizeMax      = 26f;

        var count = NewText(slotRt, "Count", new Vector2(0.58f, 0.92f), new Vector2(0.95f, 0.99f), 26f, TEXT_COLOR, TextAlignmentOptions.TopRight);
        count.text = "x" + option.count;

        if (option.favorite)
        {
            var favorite = NewText(slotRt, "Favorite", new Vector2(0.05f, 0.92f), new Vector2(0.57f, 0.99f), 26f, FAVORITE_COLOR, TextAlignmentOptions.TopLeft);
            favorite.text      = Loc.Get("food.favorite");
            favorite.fontStyle = FontStyles.Bold;
        }
    }

    // ── 효과 설명 (선반 · 말풍선 공용) ─────────────────────────

    /// <summary>먹이 에셋 기준 예상 효과 — 좋아하는 먹이면 기분 보너스를 더한다. includeGrowth = false면 성장을 빼고 (다 자란 게코)</summary>
    public static string DescribeItem(ItemSO item, bool favorite, int maxLines, string separator, bool includeGrowth = true)
    {
        if (item == null) return "";
        return DescribeEffects(item.hungerRestore, includeGrowth ? item.growthExpGain : 0f,
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
