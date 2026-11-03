using System;

[Serializable]
public class SettingsData
{
    public const string LANGUAGE_AUTO = "";   // 기기 언어를 따른다 (한국어 기기 → 한국어, 그 밖 → 영어)

    public bool   bgmOn          = true;
    public bool   sfxOn          = true;
    public bool   vibrationOn    = true;
    public bool   notificationOn = true;
    public string language       = LANGUAGE_AUTO;   // "" = 기기 언어 · "ko" · "en" (Loc.Resolve)
    // v12: exact age/birth date is never persisted. Zero means no choice yet.
    public int adAudienceVersion;
    public AdAudience adAudience;
}

public enum AdAudience { Unknown = 0, Minor = 1, Adult = 2 }
