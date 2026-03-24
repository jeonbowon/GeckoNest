using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정 패널. MainHome.unity 씬 안에 비활성 패널로 배치.
/// HomeUIController의 설정 버튼 클릭 시 SetActive(true).
///
/// 패널 구조:
///   SettingsPanel
///     Overlay
///     PopupBox
///       TitleText        — "설정"
///       BgmRow / SfxRow / VibrationRow / NotificationRow
///       PrivacyButton    — 개인정보 처리방침 (브라우저 오픈)
///       VersionText      — "v{Application.version}"
///       CloseButton
/// </summary>
public class SettingsPanelUI : MonoBehaviour
{
    // URL 교체 시 이 한 줄만 수정
    private const string PRIVACY_POLICY_URL = "https://www.tnb-soft.com/hako-privacy";

    [Header("토글")]
    [SerializeField] private Toggle _bgmToggle;
    [SerializeField] private Toggle _sfxToggle;
    [SerializeField] private Toggle _vibrationToggle;
    [SerializeField] private Toggle _notificationToggle;

    [Header("기타")]
    [SerializeField] private Button   _privacyButton;
    [SerializeField] private TMP_Text _versionText;
    [SerializeField] private Button   _closeButton;

    private SettingsManager _settings;

    // ── 생명주기 ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;

        _settings = GameManager.Instance.Settings;
        var data = _settings.GetSettings();

        // 현재 설정값을 이벤트 없이 반영
        _bgmToggle?.SetIsOnWithoutNotify(data.bgmOn);
        _sfxToggle?.SetIsOnWithoutNotify(data.sfxOn);
        _vibrationToggle?.SetIsOnWithoutNotify(data.vibrationOn);
        _notificationToggle?.SetIsOnWithoutNotify(data.notificationOn);

        _bgmToggle?.onValueChanged.AddListener(OnBgmChanged);
        _sfxToggle?.onValueChanged.AddListener(OnSfxChanged);
        _vibrationToggle?.onValueChanged.AddListener(OnVibrationChanged);
        _notificationToggle?.onValueChanged.AddListener(OnNotificationChanged);
        _privacyButton?.onClick.AddListener(OnPrivacyClicked);
        _closeButton?.onClick.AddListener(OnCloseClicked);

        if (_versionText != null)
            _versionText.text = $"v{UnityEngine.Application.version}";
    }

    private void OnDisable()
    {
        _bgmToggle?.onValueChanged.RemoveListener(OnBgmChanged);
        _sfxToggle?.onValueChanged.RemoveListener(OnSfxChanged);
        _vibrationToggle?.onValueChanged.RemoveListener(OnVibrationChanged);
        _notificationToggle?.onValueChanged.RemoveListener(OnNotificationChanged);
        _privacyButton?.onClick.RemoveListener(OnPrivacyClicked);
        _closeButton?.onClick.RemoveListener(OnCloseClicked);
    }

    // ── 토글 핸들러 ───────────────────────────────────────────

    private void OnBgmChanged(bool on)          => _settings.SetBgm(on);
    private void OnSfxChanged(bool on)          => _settings.SetSfx(on);
    private void OnVibrationChanged(bool on)    => _settings.SetVibration(on);
    private void OnNotificationChanged(bool on) => _settings.SetNotification(on);

    private void OnPrivacyClicked()
    {
        Application.OpenURL(PRIVACY_POLICY_URL);
        Debug.Log($"[SettingsPanelUI] 개인정보 처리방침 열기 — {PRIVACY_POLICY_URL}");
    }

    private void OnCloseClicked() => gameObject.SetActive(false);
}
