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
    }

    public void SetVibration(bool on)
    {
        _repo.GetPlayerData().settings.vibrationOn = on;
        _repo.Save();
    }

    public void SetNotification(bool on)
    {
        _repo.GetPlayerData().settings.notificationOn = on;
        _repo.Save();
        // TODO: Mobile Notifications 패키지 연동 시 여기서 알림 권한 요청/취소
    }

    /// <summary>앱 시작 시 AppBootstrap에서 호출 — 저장된 설정을 즉시 적용.</summary>
    public void ApplyAll()
    {
        var s = GetSettings();
        ApplyBgm(s.bgmOn);
        // SFX, 진동은 재생 시점에 설정값 참조
    }

    // ── 내부 적용 ─────────────────────────────────────────────

    private static void ApplyBgm(bool on)
    {
        // TODO: AudioManager 연동 시 BGM AudioSource.mute = !on 으로 교체
        AudioListener.volume = on ? 1f : 0f;
    }
}
