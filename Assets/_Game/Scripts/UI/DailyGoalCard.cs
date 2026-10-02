using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 오늘의 돌봄 목표 카드 — 일일 보상 팝업 카드 아래 빈 자리에 붙는다.
/// RewardPanelUI가 실행 중에 만든다 (씬에 오브젝트를 두지 않음). 색·모양은 보상 카드와 받기 버튼을 따라 한다.
///
/// 목표 4줄(먹이·물·쓰다듬기·청소 n/목표) + 받기 버튼. 진행은 RewardManager.OnGoalProgress로 바로 갱신한다.
/// </summary>
public class DailyGoalCard : MonoBehaviour
{
    private static readonly Color CARD_COLOR   = new Color(0.14f, 0.18f, 0.14f, 0.97f);
    private static readonly Color BUTTON_COLOR = new Color(0.22f, 0.55f, 0.28f, 1f);
    private static readonly Color TODO_COLOR   = new Color(0.93f, 0.97f, 0.90f);
    private static readonly Color DONE_COLOR   = new Color(0.62f, 0.90f, 0.55f);

    // 카드 위치 — 보상 카드(세로 0.28~0.72) 아래
    private static readonly Vector2 CARD_MIN = new Vector2(0.08f, 0.05f);
    private static readonly Vector2 CARD_MAX = new Vector2(0.92f, 0.26f);

    private static readonly CareKind[] ROWS = { CareKind.Feed, CareKind.Water, CareKind.Pet, CareKind.Clean };

    private RewardManager _reward;
    private TMP_Text[]    _rows;
    private Button        _claim;
    private TMP_Text      _claimText;
    private Button        _adButton;
    private TMP_Text      _adText;
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
        var go = new GameObject("DailyGoalCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(panel, false);
        rt.anchorMin = CARD_MIN;
        rt.anchorMax = CARD_MAX;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        CopyStyle(go.GetComponent<Image>(), cardStyle, CARD_COLOR);

        var card = go.AddComponent<DailyGoalCard>();

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
        buttonRt.SetParent(rt, false);
        buttonRt.anchorMin = new Vector2(0.06f, 0.24f);
        buttonRt.anchorMax = new Vector2(0.94f, 0.40f);
        buttonRt.offsetMin = buttonRt.offsetMax = Vector2.zero;
        var buttonImage = buttonGo.GetComponent<Image>();
        CopyStyle(buttonImage, buttonStyle, BUTTON_COLOR);

        card._claim = buttonGo.GetComponent<Button>();
        card._claim.targetGraphic = buttonImage;   // 못 받을 때는 기본 비활성 색으로 흐려진다
        card._claim.onClick.AddListener(card.OnClaim);
        UIPressScale.Ensure(card._claim);

        card._claimText = NewText(buttonRt, "Text", Vector2.zero, Vector2.one, 28f, font, TextAlignmentOptions.Center);
        card._claimText.fontStyle = FontStyles.Bold;

        var adGo = new GameObject("RewardedAdButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var adRt = (RectTransform)adGo.transform;
        adRt.SetParent(rt, false);
        adRt.anchorMin = new Vector2(0.06f, 0.04f);
        adRt.anchorMax = new Vector2(0.94f, 0.20f);
        adRt.offsetMin = adRt.offsetMax = Vector2.zero;
        var adImage = adGo.GetComponent<Image>();
        CopyStyle(adImage, buttonStyle, new Color(0.24f, 0.38f, 0.52f, 1f));
        adImage.color = new Color(0.24f, 0.38f, 0.52f, 1f);   // 돌봄 보상과 구분되는 차분한 파랑
        card._adButton = adGo.GetComponent<Button>();
        card._adButton.targetGraphic = adImage;
        card._adButton.onClick.AddListener(card.OnRewardedAd);
        UIPressScale.Ensure(card._adButton);
        card._adText = NewText(adRt, "Text", Vector2.zero, Vector2.one, 27f, font, TextAlignmentOptions.Center);
        card._adText.fontStyle = FontStyles.Bold;

        card.Bind();
        return card;
    }

    // ── 생명주기 ──────────────────────────────────────────────

    // AddComponent 직후에도 OnEnable이 불리므로 칸이 다 만들어진 뒤(Create 끝) 한 번 더 Bind한다
    private void OnEnable() => Bind();

    private void OnDisable()
    {
        if (_reward != null) _reward.OnGoalProgress -= OnProgress;
        if (_reward != null) _reward.OnAdRewardChanged -= Refresh;
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
            _reward.OnAdRewardChanged += Refresh;
        }
        if (_ads == null && AdMobService.Instance != null)
        {
            _ads = AdMobService.Instance;
            _ads.StateChanged += RefreshAd;
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
        if (_reward == null || !_reward.CanClaimDailyAd()) return;
        if (_ads == null)
        {
            RefreshAd();
            return;
        }

        _adButton.interactable = false;
        _adText.text = Loc.Get("ad.showing");
        _ads.ShowRewarded(
            onEarned: () =>
            {
                int coin = _reward != null ? _reward.ClaimDailyAd() : 0;
                if (coin > 0)
                {
                    AudioManager.Play(Sfx.Sparkle, 0.7f);
                    Haptics.Success();
                    if (_adText != null) _adText.text = Loc.Format("ad.rewarded", coin);
                }
                Refresh();
            },
            onClosed: RefreshAd,
            onUnavailable: RefreshAd);
    }

    private void RefreshAd()
    {
        if (_adButton == null || _adText == null || _reward == null) return;

        int count = _reward.DailyAdCount;
        if (count >= RewardManager.DAILY_AD_LIMIT)
        {
            _adButton.interactable = false;
            _adText.text = Loc.Get("ad.daily_done");
            return;
        }

        _ads = _ads != null ? _ads : AdMobService.Instance;
        bool ready = _ads != null && _ads.IsReady;
        _adButton.interactable = ready;
        if (ready)
            _adText.text = Loc.Format("ad.daily_watch", RewardManager.DAILY_AD_REWARD_COIN, count, RewardManager.DAILY_AD_LIMIT);
        else if (_ads != null && _ads.State == AdMobService.AdState.Unavailable)
            _adText.text = Loc.Get("ad.unavailable");
        else
            _adText.text = Loc.Get("ad.loading");
    }

    // ── 도구 ──────────────────────────────────────────────────

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
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
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
