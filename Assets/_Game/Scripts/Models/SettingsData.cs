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
}
