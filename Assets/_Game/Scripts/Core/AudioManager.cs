using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 효과음·배경음 재생. AppBootstrap이 만들고 씬이 바뀌어도 유지된다.
///
/// - 소리 파일이 없으면 SfxLibrary가 코드로 합성한 임시 소리를 쓴다
/// - 효과음과 배경음은 설정(sfxOn/bgmOn)을 따로 따른다
/// - Boot를 거치지 않고 씬을 바로 실행하면 조용히 아무 소리도 내지 않는다 (오류 없음)
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const int   VOICES          = 8;
    private const float SFX_VOLUME      = 0.8f;   // [TBD]
    private const float BGM_VOLUME      = 0.35f;  // [TBD]
    private const float BGM_FADE_TIME   = 1.2f;
    private const float SAME_SFX_GAP    = 0.035f; // 같은 소리가 한 프레임에 겹쳐 커지지 않게

    private static AudioManager s_instance;
    private static bool s_sfxOn = true;
    private static bool s_bgmOn = true;

    private AudioSource[] _voices;
    private AudioSource   _bgm;
    private readonly Dictionary<Sfx, float> _lastPlayed = new Dictionary<Sfx, float>();

    // ── 생성 ──────────────────────────────────────────────────

    public static void Create()
    {
        if (s_instance != null) return;
        var go = new GameObject("[AudioManager]");
        DontDestroyOnLoad(go);
        s_instance = go.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        _voices = new AudioSource[VOICES];
        for (int i = 0; i < VOICES; i++) _voices[i] = NewSource(false);

        _bgm = NewSource(true);
        _bgm.priority = 64;
        _bgm.clip     = SfxLibrary.GetBgm();
        _bgm.volume   = 0f;
        if (_bgm.clip != null) _bgm.Play();

        SfxLibrary.Warmup();   // 첫 재생 때 합성하느라 멈칫하지 않게 미리 만든다
    }

    private AudioSource NewSource(bool loop)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake  = false;
        src.loop         = loop;
        src.spatialBlend = 0f;
        return src;
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    // ── 공개 API ──────────────────────────────────────────────

    public static void Play(Sfx id, float volume = 1f, float pitch = 1f)
    {
        var a = s_instance;
        if (a == null || !s_sfxOn) return;
        a.PlayInternal(id, volume, pitch);
    }

    /// <summary>음높이를 조금씩 흔들어 같은 소리가 반복돼도 기계적으로 들리지 않게</summary>
    public static void PlayVaried(Sfx id, float volume = 1f, float variance = 0.06f)
        => Play(id, volume, 1f + Random.Range(-variance, variance));

    public static void SetSfxEnabled(bool on) => s_sfxOn = on;
    public static void SetBgmEnabled(bool on) => s_bgmOn = on;

    // ── 내부 ──────────────────────────────────────────────────

    private void PlayInternal(Sfx id, float volume, float pitch)
    {
        float now = Time.unscaledTime;
        if (_lastPlayed.TryGetValue(id, out float last) && now - last < SAME_SFX_GAP) return;
        _lastPlayed[id] = now;

        var clip = SfxLibrary.Get(id);
        if (clip == null) return;

        var src = PickVoice();
        src.clip   = clip;
        src.volume = Mathf.Clamp01(volume) * SFX_VOLUME;
        src.pitch  = Mathf.Clamp(pitch, 0.5f, 2f);
        src.Play();
    }

    // 쉬고 있는 채널 → 없으면 가장 많이 진행된 소리를 끊고 쓴다
    private AudioSource PickVoice()
    {
        AudioSource best = _voices[0];
        float bestProgress = -1f;
        for (int i = 0; i < _voices.Length; i++)
        {
            var v = _voices[i];
            if (!v.isPlaying) return v;
            float p = v.clip != null && v.clip.length > 0f ? v.time / v.clip.length : 1f;
            if (p > bestProgress)
            {
                bestProgress = p;
                best = v;
            }
        }
        return best;
    }

    private void Update()
    {
        if (_bgm == null || _bgm.clip == null) return;

        float target = s_bgmOn ? BGM_VOLUME : 0f;
        _bgm.volume = Mathf.MoveTowards(_bgm.volume, target, Time.unscaledDeltaTime / BGM_FADE_TIME * BGM_VOLUME);

        if (target <= 0f && _bgm.volume <= 0f && _bgm.isPlaying) _bgm.Pause();
        else if (target > 0f && !_bgm.isPlaying) _bgm.UnPause();
    }
}
