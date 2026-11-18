using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일일 보상 팝업에 붙는 광고 보상 카드와 오늘의 돌봄 카드.
/// RewardPanelUI가 실행 중에 만들며 각 카드의 진행 상태는 RewardManager에서 읽는다.
/// </summary>
public class DailyGoalCard : MonoBehaviour
{
    private static readonly Color CARD_COLOR   = new Color(0.14f, 0.18f, 0.14f, 0.97f);
    private static readonly Color BUTTON_COLOR = new Color(0.22f, 0.55f, 0.28f, 1f);
    private static readonly Color TODO_COLOR   = new Color(0.93f, 0.97f, 0.90f);
    private static readonly Color DONE_COLOR   = new Color(0.62f, 0.90f, 0.55f);
    private static readonly Color AD_CARD_COLOR      = new Color32(30, 46, 48, 255);
    private static readonly Color AD_BUTTON_COLOR    = new Color32(239, 186, 87, 255);
    private static readonly Color AD_DISABLED_COLOR  = new Color32(49, 66, 68, 255);
    private static readonly Color AD_BUTTON_TEXT     = new Color32(37, 29, 15, 255);
    private static readonly Color AD_COUNT_COLOR     = new Color32(250, 218, 151, 255);

    // MainHome의 일일 보상 카드(세로 0.44~0.72) 아래에 위에서부터 광고 보상, 돌봄을 배치한다.
    private static readonly Vector2 AD_CARD_MIN = new Vector2(0.08f, 0.27f);
    private static readonly Vector2 AD_CARD_MAX = new Vector2(0.92f, 0.425f);
    private static readonly Vector2 GOAL_CARD_MIN = new Vector2(0.08f, 0.075f);
    private static readonly Vector2 GOAL_CARD_MAX = new Vector2(0.92f, 0.255f);

    private static readonly CareKind[] ROWS = { CareKind.Feed, CareKind.Water, CareKind.Pet, CareKind.Clean };

    private RewardManager _reward;
    private TMP_Text[]    _rows;
    private Button        _claim;
    private TMP_Text      _claimText;
    private Button        _adButton;
    private TMP_Text      _adText;
    private TMP_Text      _adRemainingText;
    private AdMobService  _ads;

    /// <summary>"먹이 1/2" 같은 목표 한 줄의 번역 키</summary>
    public static string RowKey(CareKind kind)
    {
        switch (kind)
        {
            case CareKind.Feed:  return "goal.feed";
            case CareKind.Water: return "goal.water";
            case CareKind.Pet:   return "goal.pet";
            default:             return "goal.clean";
        }
    }

    // ── 생성 ──────────────────────────────────────────────────

    /// <summary>panel = 보상 팝업 전체(RewardPanel). cardStyle·buttonStyle = 따라 할 그림 (없으면 기본색)</summary>
    public static DailyGoalCard Create(RectTransform panel, Image cardStyle, Image buttonStyle, TMP_FontAsset font)
    {
        var root = new GameObject("SupplementalRewards", typeof(RectTransform));
        var container = (RectTransform)root.transform;
        Place(container, panel, Vector2.zero, Vector2.one);
        var card = root.AddComponent<DailyGoalCard>();
        card.CreateAdCard(container, cardStyle, buttonStyle, font);

        var goal = new GameObject("DailyGoalCard", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)goal.transform;
        Place(rt, container, GOAL_CARD_MIN, GOAL_CARD_MAX);
        CopyStyle(goal.GetComponent<Image>(), cardStyle, CARD_COLOR);

        var title = NewText(rt, "Title", new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.97f), 34f, font, TextAlignmentOptions.Center);
        title.text      = Loc.Get("goal.title");
        title.fontStyle = FontStyles.Bold;

        // 목표 4줄 — 2칸 × 2줄
        card._rows = new TMP_Text[ROWS.Length];
        for (int i = 0; i < ROWS.Length; i++)
        {
            float xMin = 0.08f + (i % 2) * 0.46f;
            float yMax = 0.80f - (i / 2) * 0.19f;
            card._rows[i] = NewText(rt, "Goal" + i, new Vector2(xMin, yMax - 0.17f), new Vector2(xMin + 0.42f, yMax),
                                    28f, font, TextAlignmentOptions.Left);
        }

        var buttonGo = new GameObject("ClaimButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var buttonRt = (RectTransform)buttonGo.transform;
        Place(buttonRt, rt, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.35f));
        var buttonImage = buttonGo.GetComponent<Image>();
        CopyStyle(buttonImage, buttonStyle, BUTTON_COLOR);

        card._claim = buttonGo.GetComponent<Button>();
        card._claim.targetGraphic = buttonImage;   // 못 받을 때는 기본 비활성 색으로 흐려진다
        card._claim.onClick.AddListener(card.OnClaim);
        UIPressScale.Ensure(card._claim);

        card._claimText = NewText(buttonRt, "Text", Vector2.zero, Vector2.one, 28f, font, TextAlignmentOptions.Center);
        card._claimText.fontStyle = FontStyles.Bold;

        card.Bind();
        return card;
    }

    private void CreateAdCard(RectTransform parent, Image cardStyle, Image buttonStyle, TMP_FontAsset font)
    {
        var card = new GameObject("DailyAdRewardCard", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)card.transform;
        Place(rt, parent, AD_CARD_MIN, AD_CARD_MAX);
        var background = card.GetComponent<Image>();
        CopyStyle(background, cardStyle, AD_CARD_COLOR);
        background.color = AD_CARD_COLOR;

        var title = NewText(rt, "Title", new Vector2(0.06f, 0.66f), new Vector2(0.55f, 0.93f),
                            38f, font, TextAlignmentOptions.MidlineLeft);
        title.text = Loc.Get("ad.daily_title");
        title.fontStyle = FontStyles.Bold;

        _adRemainingText = NewText(rt, "Remaining", new Vector2(0.55f, 0.66f), new Vector2(0.94f, 0.93f),
                                   32f, font, TextAlignmentOptions.MidlineRight);
        _adRemainingText.color = AD_COUNT_COLOR;

        var adGo = new GameObject("RewardedAdButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var adRt = (RectTransform)adGo.transform;
        Place(adRt, rt, new Vector2(0.06f, 0.11f), new Vector2(0.94f, 0.64f));
        var adImage = adGo.GetComponent<Image>();
        CopyStyle(adImage, buttonStyle, AD_DISABLED_COLOR);
        adImage.color = AD_DISABLED_COLOR;
        _adButton = adGo.GetComponent<Button>();
        _adButton.targetGraphic = adImage;
        var colors = _adButton.colors;
        colors.disabledColor = Color.white;  // 비활성 배경과 글자 대비는 RefreshAd가 지정한다.
        _adButton.colors = colors;
        _adButton.interactable = false;
        _adButton.onClick.AddListener(OnRewardedAd);
        UIPressScale.Ensure(_adButton);

        _adText = NewText(adRt, "Text", new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.93f),
                          44f, font, TextAlignmentOptions.Center);
        _adText.fontStyle = FontStyles.Bold;
    }

    // ── 생명주기 ──────────────────────────────────────────────

    // AddComponent 직후에도 OnEnable이 불리므로 칸이 다 만들어진 뒤(Create 끝) 한 번 더 Bind한다
    private void OnEnable() => Bind();

    private void OnDisable()
    {
        if (_reward != null) _reward.OnGoalProgress -= OnProgress;
        if (_reward != null) _reward.OnAdRewardChanged -= RefreshAd;
        if (_ads != null) _ads.StateChanged -= RefreshAd;
        _reward = null;
        _ads = null;
    }

    private void Bind()
    {
        if (_rows == null || GameManager.Instance == null) return;
        if (_reward == null)
        {
            _reward = GameManager.Instance.Reward;
            _reward.OnGoalProgress += OnProgress;
            _reward.OnAdRewardChanged += RefreshAd;
        }
        Refresh();
    }

    private void OnProgress(CareKind kind, int now, int target, bool allDone) => Refresh();

    public void Refresh()
    {
        if (_reward == null || _rows == null) return;

        for (int i = 0; i < ROWS.Length; i++)
        {
            int now    = _reward.GoalCount(ROWS[i]);
            int target = RewardManager.GoalTarget(ROWS[i]);
            _rows[i].text  = Loc.Format(RowKey(ROWS[i]), now, target);
            _rows[i].color = now >= target ? DONE_COLOR : TODO_COLOR;
        }

        bool canClaim = _reward.CanClaimGoals();
        _claim.interactable = canClaim;
        _claimText.text = _reward.GoalsClaimed ? Loc.Get("goal.claimed")
                        : canClaim            ? Loc.Format("goal.claim",  RewardManager.GOAL_REWARD_COIN)
                                              : Loc.Format("goal.locked", RewardManager.GOAL_REWARD_COIN);
        RefreshAd();
    }

    private void OnClaim()
    {
        if (_reward == null || _reward.ClaimGoals() <= 0) return;
        // 코인 '띵' 소리는 홈 상단 숫자가 올라가며 낸다 (HomeUIController)
        AudioManager.Play(Sfx.Sparkle, 0.7f);
        Haptics.Success();
        Refresh();
    }

    private void OnRewardedAd()
    {
        var reward = _reward;
        if (reward == null || !reward.CanClaimDailyAd() || _ads == null || !_ads.IsReady)
        {
            RefreshAd();
            return;
        }

        _ads.ShowRewarded(
            onEarned: () =>
            {
                if (reward.ClaimDailyAd() <= 0) return;
                AudioManager.Play(Sfx.Sparkle, 0.7f);
                Haptics.Success();
            },
            onClosed: RefreshAd,
            onUnavailable: RefreshAd);
    }

    private void RefreshAd()
    {
        if (_adButton == null || _reward == null) return;

        if (_ads == null && AdMobService.Instance != null)
        {
            _ads = AdMobService.Instance;
            _ads.StateChanged += RefreshAd;
        }

        int remaining = _reward.DailyAdRemaining;
        _adRemainingText.text = Loc.Format("ad.daily_remaining", remaining);

        bool ready = remaining > 0 && _ads != null && _ads.IsReady;
        _adButton.interactable = ready;
        _adButton.image.color = ready ? AD_BUTTON_COLOR : AD_DISABLED_COLOR;
        _adText.color = ready ? AD_BUTTON_TEXT : TODO_COLOR;
        _adText.fontSizeMax = ready ? 44f : 34f;
        _adText.fontSizeMin = ready ? 27f : 21f;

        if (ready)
            _adText.text = Loc.Format("ad.daily_watch", RewardManager.DAILY_AD_REWARD_COIN);
        else if (remaining == 0)
            _adText.text = Loc.Get("ad.daily_done");
        else if (_ads != null && _ads.State == AdMobService.AdState.Showing)
            _adText.text = Loc.Get("ad.showing");
        else
            _adText.text = Loc.Get(_ads != null ? _ads.StatusTextKey : "ad.unavailable");
    }

    // ── 도구 ──────────────────────────────────────────────────

    private static void Place(RectTransform rt, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void CopyStyle(Image target, Image style, Color fallback)
    {
        if (style != null)
        {
            target.sprite = style.sprite;
            target.type   = style.type;
            target.color  = style.color;
        }
        else
        {
            target.color = fallback;
        }
    }

    private static TMP_Text NewText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                    float size, TMP_FontAsset font, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        Place(rt, parent, anchorMin, anchorMax);
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize         = size;
        text.enableAutoSizing = true;   // 영어 문구가 길어도 칸 안에
        text.fontSizeMin      = size * 0.6f;
        text.fontSizeMax      = size;
        text.color            = TODO_COLOR;
        text.alignment        = alignment;
        text.raycastTarget    = false;
        return text;
    }
}
