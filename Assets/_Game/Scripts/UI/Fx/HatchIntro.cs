using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 첫 실행 부화 연출 — 새 게임 첫 홈 화면에서 한 번만.
/// 게코 자리에 알이 놓이고, 화면을 톡톡 3번 두드리면 금이 가다가 깨지며 게코가 튀어나와 인사한다.
/// 8초 동안 안 누르면 스스로 금이 가며 부화한다. 연출 중에는 화면 전체를 덮어 다른 버튼을 막는다.
///
/// HomeUIController가 실행 중에 만든다 (씬에 오브젝트를 두지 않음). 끝나면 onHatched를 부르고 스스로 사라진다.
/// 봤다는 기록은 HomeUIController → GeckoManager.CompleteHatchIntro가 남긴다.
/// 알 그림: Resources/Fx/egg 가 있으면 그것, 없으면 해츨링 단계 아이콘(알 그림)을 받는다.
/// </summary>
public class HatchIntro : MonoBehaviour
{
    private const int   TAPS_TO_HATCH  = 3;      // [TBD]
    private const float AUTO_HATCH_SEC = 8f;     // [TBD] 이 시간 동안 안 누르면 스스로 금이 간다
    private const float AUTO_TAP_GAP   = 0.7f;   // 스스로 금이 갈 때 간격

    // 알 그림 상자 — 지금 그림(512px)은 가운데 약 40% 폭에 알이 있고 위아래로 여백이 있다
    private const float EGG_SIZE     = 460f;     // [TBD] UI 단위 (1080 폭 기준)
    private const float EGG_BOTTOM   = 0.21f;    // 상자 아래에서 알 아래 끝까지 (비율) — 여기를 발 위치에 맞춘다
    private const float EGG_CENTER_Y = 0.50f;    // 알 가운데 (비율)
    private const float EGG_TOP      = 0.78f;    // 알 위 끝 (비율) — 말풍선 위치
    private const float CRACK_LENGTH = 0.20f;    // 금 한 줄 길이 (상자 비율)

    private const float ENTER_TIME     = 0.45f;
    private const float HINT_DELAY     = 0.8f;
    private const float BREAK_WAIT     = 0.4f;   // 마지막 금 → 깨지기까지 (크게 흔들리는 시간)
    private const float GECKO_POP_TIME = 0.4f;
    private const float HELLO_HOLD     = 2.2f;
    private const float END_WAIT       = 1.4f;

    private static readonly Color SHELL   = new Color(0.84f, 0.70f, 0.42f);
    private static readonly Color SHELL_B = new Color(0.98f, 0.92f, 0.74f);
    private static readonly Color CRACK   = new Color(0.28f, 0.18f, 0.08f, 0.92f);
    private static readonly Color GOLD    = new Color(1.00f, 0.91f, 0.62f);
    private static readonly Color WHITE   = Color.white;
    private static readonly Color SHADOW  = new Color(0f, 0f, 0f, 0.28f);

    // 금 3줄 — (알 가운데 기준 x, y는 상자 비율, z = 회전 도). 두드린 순서대로 생긴다
    private static readonly Vector3[] CRACKS =
    {
        new Vector3(-0.03f,  0.04f,  12f),
        new Vector3( 0.06f, -0.08f, -28f),
        new Vector3(-0.07f,  0.13f,  38f),
    };

    private RectTransform   _layer;
    private RectTransform   _egg;
    private Image           _shadow;
    private Image[]         _cracks;
    private UIParticles     _particles;
    private SpeechBubble    _hint;
    private RectTransform   _gecko;
    private GeckoMotor      _motor;
    private GeckoMovementAI _movement;
    private GeckoFx         _fx;
    private Action          _onHatched;

    private int   _taps;
    private float _idle;       // 마지막 금 이후 시간
    private float _shake;      // 두드린 순간 크게 흔들리고 줄어든다
    private float _time;
    private bool  _ready;      // 알이 다 나타난 뒤부터 두드릴 수 있다
    private bool  _breaking;

    // ── 생성 ──────────────────────────────────────────────────

    /// <summary>
    /// root = 홈 화면 Canvas, gecko = GeckoObject (발밑이 피벗). 알 그림이나 게코가 없으면 null — 호출한 쪽이 연출 없이 넘어간다.
    /// </summary>
    public static HatchIntro Create(RectTransform root, RectTransform gecko, GeckoMotor motor, GeckoMovementAI movement,
                                    GeckoFx fx, Sprite fallbackEgg, TMP_FontAsset font, Action onHatched)
    {
        var eggSprite = Resources.Load<Sprite>("Fx/egg");
        if (eggSprite == null) eggSprite = fallbackEgg;
        if (root == null || gecko == null || eggSprite == null) return null;

        // 화면 전체 — 투명. 어디를 눌러도 알을 두드린 것으로 치고, 다른 버튼은 막는다
        var go = new GameObject("HatchIntro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        Stretch(rt);
        rt.SetAsLastSibling();
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        var intro = go.AddComponent<HatchIntro>();
        intro._gecko     = gecko;
        intro._motor     = motor;
        intro._movement  = movement;
        intro._fx        = fx;
        intro._onHatched = onHatched;

        var tap = go.GetComponent<Button>();
        tap.transition = Selectable.Transition.None;
        tap.onClick.AddListener(intro.OnTap);

        // 그림 층 — 하위 캔버스 (파티클이 매 프레임 움직여도 홈 화면 전체를 다시 계산하지 않게). 터치는 위 판이 받는다
        var layerGo = new GameObject("Layer", typeof(RectTransform));
        intro._layer = (RectTransform)layerGo.transform;
        intro._layer.SetParent(rt, false);
        Stretch(intro._layer);
        layerGo.AddComponent<Canvas>();

        intro._shadow = NewImage(intro._layer, "Shadow", FxSprites.Dot, SHADOW,
                                 new Vector2(EGG_SIZE * 0.45f, EGG_SIZE * 0.1f), new Vector2(0.5f, 0.5f));

        var egg = NewImage(intro._layer, "Egg", eggSprite, WHITE, new Vector2(EGG_SIZE, EGG_SIZE), new Vector2(0.5f, EGG_BOTTOM));
        egg.preserveAspect = true;
        intro._egg = egg.rectTransform;

        intro._cracks = new Image[CRACKS.Length];
        for (int i = 0; i < CRACKS.Length; i++)
        {
            var crack = NewImage(intro._egg, "Crack" + i, FxSprites.Crack, CRACK,
                                 new Vector2(EGG_SIZE * CRACK_LENGTH * 0.5f, EGG_SIZE * CRACK_LENGTH), new Vector2(0.5f, 0.5f));
            var crt = crack.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, EGG_CENTER_Y);
            crt.anchoredPosition = new Vector2(CRACKS[i].x, CRACKS[i].y) * EGG_SIZE;
            crt.localRotation    = Quaternion.Euler(0f, 0f, CRACKS[i].z);
            crack.gameObject.SetActive(false);
            intro._cracks[i] = crack;
        }

        var particlesGo = new GameObject("Particles", typeof(RectTransform));
        var particlesRt = (RectTransform)particlesGo.transform;
        particlesRt.SetParent(intro._layer, false);
        Stretch(particlesRt);
        intro._particles = particlesGo.AddComponent<UIParticles>();

        intro._hint = SpeechBubble.Create(intro._layer, font);

        SetGeckoVisible(gecko, false);
        if (movement != null) movement.enabled = false;

        intro.StartCoroutine(intro.Run());
        return intro;
    }

    /// <summary>게코를 숨기거나 다시 보이게 한다 (부화 전에는 알만 보여야 한다). 그리는 것만 끄고 동작·자세 계산은 그대로.</summary>
    public static void SetGeckoVisible(Component gecko, bool visible)
    {
        if (gecko == null) return;
        var group = gecko.GetComponent<CanvasGroup>();
        if (group == null)
        {
            if (visible) return;
            group = gecko.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable   = false;
        }
        group.alpha = visible ? 1f : 0f;
    }

    // ── 진행 ──────────────────────────────────────────────────

    private IEnumerator Run()
    {
        _egg.localScale = Vector3.zero;
        _shadow.rectTransform.localScale = Vector3.zero;
        yield return null;   // 첫 프레임에는 레이아웃 크기가 확정되지 않았을 수 있다

        // 1) 알이 톡 나타난다
        AudioManager.Play(Sfx.Pop, 0.8f);
        for (float t = 0f; t < ENTER_TIME; t += Step())
        {
            float u = t / ENTER_TIME;
            float s = EaseOutBack(u);
            _egg.localScale = new Vector3(s, s, 1f);
            _shadow.rectTransform.localScale = Vector3.one * u;
            yield return null;
        }
        _egg.localScale = Vector3.one;
        _shadow.rectTransform.localScale = Vector3.one;
        _ready = true;

        yield return Wait(HINT_DELAY);
        if (_taps == 0 && _hint != null) _hint.Show(Loc.Get("hatch.hint"), EggTopWorld, AUTO_HATCH_SEC);

        // 2) 톡톡 — 안 누르면 스스로 금이 간다
        while (_taps < TAPS_TO_HATCH)
        {
            _idle += Step();
            if (_idle >= AUTO_HATCH_SEC)
            {
                Crack();
                _idle = AUTO_HATCH_SEC - AUTO_TAP_GAP;
            }
            yield return null;
        }

        // 3) 크게 흔들리다가 깨진다
        _breaking = true;
        _shake = 1.6f;
        yield return Wait(BREAK_WAIT);
        Break();

        // 4) 게코가 튀어나온다
        SetGeckoVisible(_gecko, true);
        if (_motor != null) _motor.Play(GeckoAction.Surprise);
        for (float t = 0f; t < GECKO_POP_TIME; t += Step())
        {
            float s = Mathf.LerpUnclamped(0.3f, 1f, EaseOutBack(t / GECKO_POP_TIME));
            _gecko.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        _gecko.localScale = Vector3.one;

        // 5) 기뻐하며 인사
        for (float t = 0f; t < 1.2f && _motor != null && _motor.IsBusy; t += Step())
            yield return null;
        if (_motor != null) _motor.Play(GeckoAction.Happy_LookUp);
        if (_fx != null)
        {
            _fx.Hearts();
            _fx.Say(Loc.Get("hatch.hello"), HELLO_HOLD);
        }
        Haptics.Light();
        yield return Wait(END_WAIT);

        Finish();
    }

    private void Finish()
    {
        SetGeckoVisible(_gecko, true);
        if (_movement != null) _movement.enabled = true;

        var done = _onHatched;
        _onHatched = null;
        done?.Invoke();
        Destroy(gameObject);
    }

    private void OnTap()
    {
        if (!_ready || _breaking || _taps >= TAPS_TO_HATCH) return;
        _idle = 0f;
        Crack();
    }

    private void Crack()
    {
        if (_taps >= TAPS_TO_HATCH) return;
        int i = _taps++;

        if (_hint != null) _hint.gameObject.SetActive(false);
        _shake = 1f;
        if (i < _cracks.Length) StartCoroutine(PopIn(_cracks[i]));

        AudioManager.PlayVaried(i == 0 ? Sfx.Tap : Sfx.Pop, 0.8f);
        Haptics.Light();

        if (i == 0) return;   // 두 번째부터 껍질 부스러기
        _particles.Emit(EggCenterLocal, new UIParticles.Burst
        {
            sprite = FxSprites.Flake, color = SHELL, colorB = SHELL_B, count = 2 + i * 2,
            speed = new Vector2(80f, 200f), angle = 90f, spread = 60f,
            size = new Vector2(14f, 26f), life = new Vector2(0.5f, 0.8f),
            gravity = 900f, spin = 300f, area = new Vector2(40f, 40f),
        });
    }

    private void Break()
    {
        Vector2 c = EggCenterLocal;
        AudioManager.Play(Sfx.Chime);
        AudioManager.PlayVaried(Sfx.Pop, 1f);
        Haptics.Success();

        _particles.Emit(c, new UIParticles.Burst
        {
            sprite = FxSprites.Ring, color = GOLD, colorB = WHITE, count = 1,
            size = new Vector2(180f, 180f), life = new Vector2(0.6f, 0.6f), growTo = 3.4f,
        });
        _particles.Emit(c, new UIParticles.Burst
        {
            sprite = FxSprites.Flake, color = SHELL, colorB = SHELL_B, count = 16,
            speed = new Vector2(260f, 560f), angle = 90f, spread = 180f,
            size = new Vector2(26f, 54f), life = new Vector2(0.9f, 1.3f),
            gravity = 1100f, drag = 0.4f, spin = 420f, area = new Vector2(50f, 60f),
        });
        _particles.Emit(c, new UIParticles.Burst
        {
            sprite = FxSprites.Sparkle, color = GOLD, colorB = WHITE, count = 14,
            speed = new Vector2(200f, 380f), angle = 90f, spread = 180f,
            size = new Vector2(26f, 48f), life = new Vector2(0.7f, 1.1f), drag = 2.5f, spin = 120f,
        });

        _egg.gameObject.SetActive(false);
        _shadow.gameObject.SetActive(false);
    }

    private IEnumerator PopIn(Image crack)
    {
        crack.gameObject.SetActive(true);
        var t = crack.rectTransform;
        for (float s = 0f; s < 0.15f; s += Step())
        {
            t.localScale = new Vector3(1f, EaseOutBack(s / 0.15f), 1f);   // 금이 위아래로 쭉 뻗는다
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ── 매 프레임: 알 위치 · 흔들림 ───────────────────────────

    private void Update()
    {
        if (_gecko == null || _egg == null) return;

        // 게코 발밑(피벗)에 알 아래 끝을 맞춘다
        Vector2 foot = _layer.InverseTransformPoint(_gecko.position);
        _egg.anchoredPosition = foot;
        _shadow.rectTransform.anchoredPosition = foot;

        // 가만히 있어도 살짝 기우뚱, 금이 갈수록 더, 두드린 순간 크게 (피벗이 알 아래 끝이라 오뚝이처럼 흔들린다)
        float dt = Step();
        _time += dt;
        _shake = Mathf.MoveTowards(_shake, 0f, dt * 2.5f);
        float calm = Mathf.Sin(_time * 2.4f) * (2.5f + _taps * 1.5f);
        float jolt = Mathf.Sin(_time * 38f) * 14f * _shake;
        _egg.localRotation = Quaternion.Euler(0f, 0f, calm + jolt);
    }

    // ── 위치 ──────────────────────────────────────────────────

    private Vector3 EggTopWorld()
        => _egg != null ? _egg.TransformPoint(new Vector3(0f, (EGG_TOP - EGG_BOTTOM) * EGG_SIZE, 0f)) : transform.position;

    private Vector2 EggCenterLocal
        => _particles.ToLocal(_egg.TransformPoint(new Vector3(0f, (EGG_CENTER_Y - EGG_BOTTOM) * EGG_SIZE, 0f)));

    // ── 도구 ──────────────────────────────────────────────────

    private static Image NewImage(RectTransform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = pivot;
        rt.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.sprite        = sprite;
        image.color         = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // GeckoMotor와 같은 시간 흐름(최대 0.05초 단위) — 게코 동작과 박자가 어긋나지 않게
    private static float Step() => Mathf.Min(Time.deltaTime, 0.05f);

    private static IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Step()) yield return null;
    }

    private static float EaseOutBack(float x)
    {
        const float C1 = 1.9f, C3 = C1 + 1f;
        float t = x - 1f;
        return 1f + C3 * t * t * t + C1 * t * t;
    }
}
