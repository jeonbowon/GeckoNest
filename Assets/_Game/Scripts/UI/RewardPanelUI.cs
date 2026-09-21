using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일일 보상 팝업 패널. MainHome.unity 씬 안에 비활성 패널로 배치.
/// HomeUIController가 OnEnable 시 CanClaim() 여부에 따라 SetActive(true).
///
/// 패널 구조:
///   RewardPanel
///     Overlay          — 반투명 배경 (전체 화면 덮기)
///     PopupBox         — 중앙 카드
///       TitleText      — "일일 보상"
///       StreakText      — "연속 N일"
///       RewardText     — "코인 +100" / "코인 +250  젬 +3"
///       ClaimButton    — "받기" / "내일 다시"
///         ClaimBtnText
///       CloseButton    — X 버튼
/// </summary>
public class RewardPanelUI : MonoBehaviour
{
    [Header("텍스트")]
    [SerializeField] private TMP_Text _streakText;
    [SerializeField] private TMP_Text _rewardText;

    [Header("버튼")]
    [SerializeField] private Button   _claimButton;
    [SerializeField] private TMP_Text _claimButtonText;
    [SerializeField] private Button   _closeButton;

    [Header("결과 알림 (선택)")]
    [SerializeField] private TMP_Text _resultText;   // 수령 후 잠깐 표시

    private RewardManager _reward;

    // ── 생명주기 ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;

        _reward = GameManager.Instance.Reward;
        _claimButton?.onClick.AddListener(OnClaimClicked);
        _closeButton?.onClick.AddListener(OnCloseClicked);

        if (_resultText != null) _resultText.gameObject.SetActive(false);
        Refresh();
        EnsureGoalCard();
    }

    // 오늘의 돌봄 목표 카드 — 보상 카드 아래에 실행 중에 붙인다 (보상 카드·받기 버튼 모양을 따라 한다)
    private DailyGoalCard _goalCard;

    private void EnsureGoalCard()
    {
        if (_goalCard != null)
        {
            _goalCard.Refresh();
            return;
        }
        var popup       = _claimButton != null ? _claimButton.transform.parent : null;
        var cardStyle   = popup != null ? popup.GetComponent<Image>() : null;
        var buttonStyle = _claimButton != null ? _claimButton.targetGraphic as Image : null;
        var font        = _streakText != null ? _streakText.font : null;
        _goalCard = DailyGoalCard.Create((RectTransform)transform, cardStyle, buttonStyle, font);
    }

    private void OnDisable()
    {
        _claimButton?.onClick.RemoveListener(OnClaimClicked);
        _closeButton?.onClick.RemoveListener(OnCloseClicked);
    }

    // ── UI 갱신 ───────────────────────────────────────────────

    private void Refresh()
    {
        bool canClaim      = _reward.CanClaim();
        int  streak        = _reward.GetStreak();
        var (coin, gem)    = _reward.PeekReward();

        if (_streakText != null)
            _streakText.text = Loc.Format("reward.streak", streak);

        if (_rewardText != null)
            _rewardText.text = gem > 0 ? Loc.Format("reward.coin_gem", coin, gem) : Loc.Format("reward.coin", coin);

        if (_claimButton != null)
            _claimButton.interactable = canClaim;

        if (_claimButtonText != null)
            _claimButtonText.text = Loc.Get(canClaim ? "reward.claim" : "reward.tomorrow");

        EnsureResetNote();
        if (_resetNote != null)
            _resetNote.text = Loc.Format("reward.reset", RewardManager.LocalResetTimeText());
    }


    // ── 새로 고침 안내 ────────────────────────────────────────
    // 하루 기준이 UTC 자정이라 한국에서는 오전 9시에 바뀐다 (2026-09-21 결정).
    // 언제 바뀌는지 모르면 "어제 받았는데 왜 또 안 되지?"가 되므로 보상 금액 아래에 작게 적는다.
    // 씬에 오브젝트를 늘리지 않으려고 보상 글자의 자식으로 실행 중에 한 번만 만든다.

    private static readonly Color RESET_NOTE_COLOR = new Color(1f, 1f, 1f, 0.55f);
    private const float           RESET_NOTE_SCALE = 0.62f;   // 보상 글자 대비 크기

    private TMP_Text _resetNote;

    private void EnsureResetNote()
    {
        if (_resetNote != null || _rewardText == null) return;

        var go = new GameObject("ResetNote", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(_rewardText.transform, false);
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -4f);
        rt.sizeDelta        = new Vector2(0f, _rewardText.fontSize * RESET_NOTE_SCALE * 1.4f);

        _resetNote = go.AddComponent<TextMeshProUGUI>();
        if (_rewardText.font != null) _resetNote.font = _rewardText.font;
        _resetNote.fontSize      = _rewardText.fontSize * RESET_NOTE_SCALE;
        _resetNote.color         = RESET_NOTE_COLOR;
        _resetNote.alignment     = TextAlignmentOptions.Center;
        _resetNote.raycastTarget = false;
        SceneTextLocalizer.Ignore(_resetNote);   // 시각이 들어간 문구라 번역표 원문과 겹치지 않게
    }
    // ── 버튼 핸들러 ───────────────────────────────────────────

    private void OnClaimClicked()
    {
        var (coin, gem) = _reward.ClaimReward();
        if (coin == 0 && gem == 0) return;

        // 코인 '띵' 소리는 홈 상단 숫자가 올라가며 낸다 (HomeUIController)
        AudioManager.Play(Sfx.Sparkle, 0.7f);
        Haptics.Success();

        if (_resultText != null)
        {
            _resultText.text = gem > 0
                ? Loc.Format("reward.got_coin_gem", coin, gem)
                : Loc.Format("reward.got_coin", coin);
            _resultText.gameObject.SetActive(true);
        }

        Refresh();
    }

    private void OnCloseClicked() => gameObject.SetActive(false);
}
