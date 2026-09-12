using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 소리 찾기. 파일이 있으면 파일, 없으면 코드로 합성한 임시 소리.
///
/// 소리 교체: Resources/Audio/Sfx/{이름}.wav|ogg  (이름 = Sfx 값 소문자. 예: chime, moltsuccess)
/// 배경음 교체: Resources/Audio/Bgm/home.wav|ogg  (없으면 테라리움 환경음을 합성)
/// </summary>
public static class SfxLibrary
{
    private const string SFX_PATH = "Audio/Sfx/";
    private const string BGM_PATH = "Audio/Bgm/home";

    private static readonly Dictionary<Sfx, AudioClip> s_cache = new Dictionary<Sfx, AudioClip>();

    public static string FileName(Sfx id) => id.ToString().ToLowerInvariant();

    public static AudioClip Get(Sfx id)
    {
        if (s_cache.TryGetValue(id, out var clip) && clip != null) return clip;

        clip = Resources.Load<AudioClip>(SFX_PATH + FileName(id));
        if (clip == null) clip = MakeClip("sfx_" + FileName(id), SfxSynth.Render(id), SfxSynth.SFX_RATE);
        s_cache[id] = clip;
        return clip;
    }

    public static AudioClip GetBgm()
    {
        var clip = Resources.Load<AudioClip>(BGM_PATH);
        return clip != null ? clip : MakeClip("amb_terrarium", SfxSynth.RenderAmbience(), SfxSynth.AMB_RATE);
    }

    public static void Warmup()
    {
        foreach (Sfx id in Enum.GetValues(typeof(Sfx))) Get(id);
    }

    private static AudioClip MakeClip(string name, float[] data, int rate)
    {
        var clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        clip.hideFlags = HideFlags.DontUnloadUnusedAsset;   // 씬 전환 시 자동 정리 대상에서 제외
        return clip;
    }
}
