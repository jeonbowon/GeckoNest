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
            _streakText.text = $"연속 {streak}일";

        if (_rewardText != null)
            _rewardText.text = gem > 0 ? $"코인 +{coin}  젬 +{gem}" : $"코인 +{coin}";

        if (_claimButton != null)
            _claimButton.interactable = canClaim;

        if (_claimButtonText != null)
            _claimButtonText.text = canClaim ? "받기" : "내일 다시";
    }

    // ── 버튼 핸들러 ───────────────────────────────────────────

    private void OnClaimClicked()
    {
        var (coin, gem) = _reward.ClaimReward();
        if (coin == 0 && gem == 0) return;

        if (_resultText != null)
        {
            _resultText.text = gem > 0
                ? $"코인 +{coin}  젬 +{gem} 수령!"
                : $"코인 +{coin} 수령!";
            _resultText.gameObject.SetActive(true);
        }

        Refresh();
    }

    private void OnCloseClicked() => gameObject.SetActive(false);
}
