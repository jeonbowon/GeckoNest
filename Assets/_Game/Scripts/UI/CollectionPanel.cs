using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게코 도감 · 업적 창 (게코 목록 화면, 실행 중 생성). GeckoListUIController가 "도감" 버튼으로 연다.
///
///   도감 탭 — 종마다 그림 · 이름(만나기 전에는 ???) · 도장 "만남" "어덜트", 맨 아래 모든 종 어덜트 완성 보상
///   업적 탭 — RewardManager.ACHIEVEMENTS 목록, 진행(37/100) · 받기 버튼
///
/// 데이터·보상은 모두 RewardManager (UI는 읽고 Claim만 부른다). 화면을 덮어 뒤 버튼을 막는다.
/// </summary>
public class CollectionPanel : MonoBehaviour
{
    private static readonly Color BG        = new Color(0.07f, 0.08f, 0.11f, 0.98f);
    private static readonly Color CARD      = new Color(0.16f, 0.18f, 0.24f);
    private static readonly Color TAB_ON    = new Color(0.18f, 0.55f, 0.30f);
    private static readonly Color TAB_OFF   = new Color(0.22f, 0.25f, 0.32f);
    private static readonly Color GOLD      = new Color(1.00f, 0.80f, 0.30f);
    private static readonly Color STAMP_OFF = new Color(0.28f, 0.31f, 0.38f);
    private static readonly Color SUB_TEXT  = new Color(0.72f, 0.75f, 0.82f);
    private static readonly Color DARK_TEXT = new Color(0.16f, 0.12f, 0.05f);
    private static readonly Color CLAIM     = new Color(0.18f, 0.60f, 0.30f);
    private static readonly Color DONE      = new Color(0.30f, 0.33f, 0.40f);

    private const float HEADER_H   = 130f;
    private const float TAB_H      = 96f;
    private const float PAD        = 32f;
    private const float ROW_GAP    = 16f;
    private const float SPECIES_H  = 180f;
    private const float COMPLETE_H = 150f;
    private const float ACHIEVE_H  = 150f;

    private RewardManager _reward;
    private TMP_FontAsset _font;
    private Sprite        _round;
    private Action        _onChanged;   // 재화·배지 갱신 (게코 목록 화면)

    private RectTransform _content;
    private Image         _bookTab, _achieveTab;
    private bool          _showAchievements;

    public bool IsOpen => gameObject.activeSelf;

    /// <summary>parent 전체를 덮는 창을 만든다 (처음에는 닫힘). round = 버튼·카드용 둥근 스프라이트</summary>
    public static CollectionPanel Create(RectTransform parent, RewardManager reward, TMP_FontAsset font, Sprite round, Action onChanged)
    {
        var go = new GameObject("CollectionPanel", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        Stretch(rt);
        go.GetComponent<Image>().color = BG;   // 뒤 화면 누르기를 막는다

        var panel = go.AddComponent<CollectionPanel>();
        panel._reward    = reward;
        panel._font      = font;
        panel._round     = round;
        panel._onChanged = onChanged;
        panel.Build(rt);
        go.SetActive(false);
        return panel;
    }

    public void Open(bool achievements)
    {
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        ShowTab(achievements);
    }

    public void Close() => gameObject.SetActive(false);

    // ── 뼈대 ──────────────────────────────────────────────────

    private void Build(RectTransform root)
    {
        // 제목 + 닫기
        var title = MakeText(root, Loc.Get("book.title"), 52f, Color.white, TextAlignmentOptions.Center, bold: true);
        Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -HEADER_H * 0.5f), new Vector2(-440f, HEADER_H));   // 닫기 버튼과 겹치지 않게

        var close = MakeButton(root, Loc.Get("book.close"), TAB_OFF, Close, out _);
        Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-PAD - 90f, -HEADER_H * 0.5f), new Vector2(180f, 86f));

        // 탭
        var bookTab = MakeButton(root, Loc.Get("book.tab_book"), TAB_ON, () => ShowTab(false), out _);
        Place((RectTransform)bookTab.transform, new Vector2(0f, 1f), new Vector2(0.5f, 1f),
              new Vector2(PAD * 0.5f, -HEADER_H - TAB_H * 0.5f), new Vector2(-PAD * 1.5f, TAB_H));
        var achieveTab = MakeButton(root, Loc.Get("book.tab_achieve"), TAB_OFF, () => ShowTab(true), out _);
        Place((RectTransform)achieveTab.transform, new Vector2(0.5f, 1f), new Vector2(1f, 1f),
              new Vector2(-PAD * 0.5f, -HEADER_H - TAB_H * 0.5f), new Vector2(-PAD * 1.5f, TAB_H));
        _bookTab    = bookTab.image;
        _achieveTab = achieveTab.image;

        // 목록 (세로 스크롤)
        var view = NewRect("List", root);
        view.anchorMin = Vector2.zero;
        view.anchorMax = Vector2.one;
        view.offsetMin = new Vector2(PAD, PAD);
        view.offsetMax = new Vector2(-PAD, -(HEADER_H + TAB_H + 24f));
        view.gameObject.AddComponent<RectMask2D>();
        var hit = view.gameObject.AddComponent<Image>();   // 빈 곳을 끌어도 스크롤되게
        hit.color = Color.clear;

        _content = NewRect("Content", view);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot     = new Vector2(0.5f, 1f);
        _content.sizeDelta = Vector2.zero;
        var layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing                = ROW_GAP;
        layout.childControlWidth      = true;
        layout.childControlHeight     = true;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;
        _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content      = _content;
        scroll.viewport     = view;
        scroll.horizontal   = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    private void ShowTab(bool achievements)
    {
        _showAchievements = achievements;
        if (_bookTab != null)    _bookTab.color    = achievements ? TAB_OFF : TAB_ON;
        if (_achieveTab != null) _achieveTab.color = achievements ? TAB_ON  : TAB_OFF;
        Rebuild();
    }

    private void Rebuild()
    {
        if (_content == null || _reward == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            var child = _content.GetChild(i).gameObject;
            child.SetActive(false);   // Destroy는 프레임 끝이라 레이아웃에서 먼저 뺀다
            Destroy(child);
        }
        _content.anchoredPosition = Vector2.zero;

        if (_showAchievements)
            foreach (var a in RewardManager.ACHIEVEMENTS) AchievementRow(a);
        else
        {
            foreach (var s in SpeciesCatalog.All) SpeciesRow(s);
            CompleteRow();
        }
    }

    // ── 도감 줄 ───────────────────────────────────────────────

    private void SpeciesRow(GeckoSpeciesSO species)
    {
        bool met   = _reward.HasMet(species.speciesId);
        bool adult = _reward.HasRaisedAdult(species.speciesId);
        var row = Row(SPECIES_H);

        var thumb = NewRect("Thumb", row).gameObject.AddComponent<Image>();
        thumb.sprite         = species.thumbnailSprite;
        thumb.preserveAspect = true;
        thumb.color          = species.thumbnailSprite == null ? Color.clear
                             : met ? Color.white : new Color(0f, 0f, 0f, 0.75f);   // 만나기 전에는 그림자만
        thumb.raycastTarget  = false;
        Place(thumb.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f + 70f, 0f), new Vector2(140f, 140f));

        var name = MakeText(row, met ? Loc.SpeciesName(species) : Loc.Get("book.unknown"), 40f, Color.white, TextAlignmentOptions.MidlineLeft, bold: true);
        Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(100f, 34f), new Vector2(-240f, 56f));

        Stamp(row, Loc.Get("book.stamp_met"),    met,   new Vector2(220f + 80f,  -36f));   // 이름 왼쪽 끝(220)에 맞춤
        Stamp(row, Loc.StageName(GeckoManager.ADULT_STAGE), adult, new Vector2(220f + 260f, -36f));
    }

    private void Stamp(RectTransform row, string label, bool on, Vector2 center)
    {
        var bg = NewRect("Stamp", row).gameObject.AddComponent<Image>();
        bg.sprite        = _round;
        bg.type          = Image.Type.Sliced;
        bg.color         = on ? GOLD : STAMP_OFF;
        bg.raycastTarget = false;
        Place(bg.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), center, new Vector2(160f, 54f));
        var t = MakeText(bg.rectTransform, label, 28f, on ? DARK_TEXT : SUB_TEXT, TextAlignmentOptions.Center, bold: true);
        Stretch(t.rectTransform);
    }

    private void CompleteRow()
    {
        var species = SpeciesCatalog.All;
        int done = _reward.BookAdults(species, out int total);
        var row = Row(COMPLETE_H);

        var label = MakeText(row, Loc.Format("book.complete", done, total), 32f, Color.white, TextAlignmentOptions.MidlineLeft, bold: true);
        Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(24f - 150f, 0f), new Vector2(-348f, 120f));

        bool claimed  = _reward.BookRewardClaimed;
        bool canClaim = _reward.CanClaimBook(species);
        string gem  = Loc.Format("achieve.reward_gem", RewardManager.BOOK_COMPLETE_GEM);
        string text = claimed ? Loc.Get("book.claimed") : canClaim ? Loc.Format("achieve.claim", gem) : gem;
        var button = MakeButton(row, text, canClaim ? CLAIM : DONE, OnClaimBook, out _);
        button.interactable = canClaim;
        Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f - 130f, 0f), new Vector2(260f, 96f));
    }

    private void OnClaimBook()
    {
        if (_reward.ClaimBook(SpeciesCatalog.All) <= 0) return;
        Celebrate();
    }

    // ── 업적 줄 ───────────────────────────────────────────────

    private void AchievementRow(AchievementDef def)
    {
        var row = Row(ACHIEVE_H);
        bool claimed  = _reward.IsClaimed(def.id);
        bool canClaim = _reward.CanClaimAchievement(def);

        var name = MakeText(row, Loc.Get(def.NameKey), 38f, claimed ? SUB_TEXT : Color.white, TextAlignmentOptions.MidlineLeft, bold: true);
        Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(24f - 150f, 28f), new Vector2(-348f, 52f));

        string desc = Loc.Format(def.DescKey, def.target) + "  " + Loc.Format("achieve.progress", _reward.AchievementProgress(def), def.target);
        var sub = MakeText(row, desc, 28f, SUB_TEXT, TextAlignmentOptions.MidlineLeft, bold: false);
        Place(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(24f - 150f, -30f), new Vector2(-348f, 44f));

        string reward = RewardText(def);
        string label  = claimed ? Loc.Get("book.claimed")
                      : canClaim ? Loc.Format("achieve.claim", reward)
                      : reward;
        var button = MakeButton(row, label, canClaim ? CLAIM : DONE, () => OnClaimAchievement(def.id), out _);
        button.interactable = canClaim;
        Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f - 130f, 0f), new Vector2(260f, 96f));
    }

    public static string RewardText(AchievementDef def)
    {
        if (def.coin > 0 && def.gem > 0) return Loc.Format("achieve.reward_coin", def.coin) + " " + Loc.Format("achieve.reward_gem", def.gem);
        return def.gem > 0 ? Loc.Format("achieve.reward_gem", def.gem) : Loc.Format("achieve.reward_coin", def.coin);
    }

    private void OnClaimAchievement(string id)
    {
        if (!_reward.ClaimAchievement(id, out _, out _)) return;
        Celebrate();
    }

    private void Celebrate()
    {
        AudioManager.Play(Sfx.Coin, 0.9f);
        AudioManager.Play(Sfx.Sparkle, 0.6f);
        Haptics.Success();
        _onChanged?.Invoke();
        Rebuild();
    }

    // ── 조립 도구 ─────────────────────────────────────────────

    private RectTransform Row(float height)
    {
        var bg = NewRect("Row", _content).gameObject.AddComponent<Image>();
        bg.sprite = _round;
        bg.type   = Image.Type.Sliced;
        bg.color  = CARD;
        bg.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return bg.rectTransform;
    }

    private TextMeshProUGUI MakeText(RectTransform parent, string text, float size, Color color, TextAlignmentOptions align, bool bold)
    {
        var t = NewRect("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text             = text;
        t.fontSize         = size;
        t.enableAutoSizing = true;
        t.fontSizeMin      = Mathf.Min(20f, size);
        t.fontSizeMax      = size;
        t.color            = color;
        t.alignment        = align;
        t.fontStyle        = bold ? FontStyles.Bold : FontStyles.Normal;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget    = false;
        SceneTextLocalizer.Ignore(t);   // 이미 번역된 글자
        return t;
    }

    private Button MakeButton(RectTransform parent, string label, Color color, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI text)
    {
        var img = NewRect("Button", parent).gameObject.AddComponent<Image>();
        img.sprite = _round;
        img.type   = Image.Type.Sliced;
        img.color  = color;
        var button = img.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        UIPressScale.Ensure(button);
        text = MakeText(img.rectTransform, label, 30f, Color.white, TextAlignmentOptions.Center, bold: true);
        Stretch(text.rectTransform, 8f);
        return button;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt, float pad = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }

    // 앵커 두 점 + 위치 + 크기 (앵커가 벌어진 방향은 sizeDelta가 늘이는 양)
    private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
    }
}
