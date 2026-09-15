using UnityEngine;

public class SettingsManager
{
    private readonly PlayerRepository _repo;

    public SettingsManager(PlayerRepository repo)
    {
        _repo = repo;
    }

    public SettingsData GetSettings() => _repo.GetPlayerData().settings;

    // ── 설정 변경 ─────────────────────────────────────────────

    public void SetBgm(bool on)
    {
        _repo.GetPlayerData().settings.bgmOn = on;
        _repo.Save();
        ApplyBgm(on);
    }

    public void SetSfx(bool on)
    {
        _repo.GetPlayerData().settings.sfxOn = on;
        _repo.Save();
        AudioManager.SetSfxEnabled(on);
    }

    public void SetVibration(bool on)
    {
        _repo.GetPlayerData().settings.vibrationOn = on;
        _repo.Save();
    }

    // 알림 권한 요청·예약 취소는 UI에서 (SettingsPanelUI) — Domain은 플랫폼 기능을 모른다
    public void SetNotification(bool on)
    {
        _repo.GetPlayerData().settings.notificationOn = on;
        _repo.Save();
    }

    /// <summary>"ko" · "en" · ""(기기 언어). 이미 떠 있는 화면 글자는 씬을 다시 열어야 바뀐다.</summary>
    public void SetLanguage(string language)
    {
        language ??= SettingsData.LANGUAGE_AUTO;
        _repo.GetPlayerData().settings.language = language;
        _repo.Save();
        Loc.Init(language);
    }

    /// <summary>앱 시작 시 AppBootstrap에서 호출 — 저장된 설정을 즉시 적용.</summary>
    public void ApplyAll()
    {
        var s = GetSettings();
        ApplyBgm(s.bgmOn);
        AudioManager.SetSfxEnabled(s.sfxOn);
        // 진동은 재생 시점에 설정값 참조 (Haptics)
    }

    // ── 내부 적용 ─────────────────────────────────────────────

    // 예전에는 AudioListener.volume을 0으로 만들어 효과음까지 같이 꺼졌다. 이제 배경음만 끈다.
    private static void ApplyBgm(bool on) => AudioManager.SetBgmEnabled(on);
}
