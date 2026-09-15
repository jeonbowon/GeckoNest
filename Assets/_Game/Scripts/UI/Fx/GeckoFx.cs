using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게코 주변 연출 — 먹이 낚아채기, 분무, 하트, 반짝이, 허물 조각, 성장 빛, 말풍선.
/// HomeUIController가 GeckoArea 위에 만들고, 돌봄 결과·성장·허물 사건에 맞춰 부른다.
///
/// 위치는 GeckoRig에서 그 순간의 파츠 위치(머리·입·혀끝)를 읽어 정한다 → 그림이 바뀌어도 맞는다.
/// 효과음도 연출과 박자를 맞추기 위해 여기서 낸다 (진동은 UI 쪽에서).
/// </summary>
[DisallowMultipleComponent]
public class GeckoFx : MonoBehaviour
{
    private const float FOOD_SIZE = 150f;   // 먹이 크기 (스킨 픽셀) — 게코 크기에 따라 함께 줄어든다

    // 색 — 따뜻하고 부드러운 파스텔 (ART_GUIDE.md 팔레트)
    private static readonly Color PINK    = new Color(1.00f, 0.56f, 0.64f);   // #FF8FA3
    private static readonly Color PINK_B  = new Color(1.00f, 0.72f, 0.77f);   // #FFB8C4
    private static readonly Color WATER   = new Color(0.56f, 0.83f, 1.00f, 0.9f);
    private static readonly Color WATER_B = new Color(0.80f, 0.93f, 1.00f, 0.9f);
    private static readonly Color GOLD    = new Color(1.00f, 0.91f, 0.62f);   // #FFE89E
    private static readonly Color WHITE   = new Color(1.00f, 1.00f, 1.00f);
    private static readonly Color FLAKE   = new Color(0.96f, 0.94f, 0.90f, 0.95f);
    private static readonly Color PUFF    = new Color(0.90f, 0.87f, 0.84f, 0.9f);
    private static readonly Color CRUMB   = new Color(0.66f, 0.44f, 0.24f);

    private RectTransform _rt;
    private UIParticles   _particles;
    private RectTransform _food;
    private Image         _foodImg;
    private SpeechBubble  _bubble;
    private GeckoMotor    _motor;
    private GeckoRig      _rig;
    private Coroutine     _feedRoutine;

    // ── 생성 ──────────────────────────────────────────────────

    /// <summary>area(GeckoArea) 위에 연출 레이어를 만든다. 이미 있으면 그것을 쓴다.</summary>
    public static GeckoFx Create(RectTransform area, GeckoMotor motor, TMP_FontAsset font)
    {
        if (area == null) return null;

        var existing = area.Find("GeckoFx");
        var fx = existing != null ? existing.GetComponent<GeckoFx>() : null;
        if (fx == null)
        {
            var go = new GameObject("GeckoFx", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(area, false);
            Stretch(rt);

            // 하위 캔버스 — 파티클이 매 프레임 움직여도 홈 화면 전체를 다시 계산하지 않게
            go.AddComponent<Canvas>();

            fx = go.AddComponent<GeckoFx>();
            fx._rt = rt;

            var pGo = new GameObject("Particles", typeof(RectTransform));
            var pRt = (RectTransform)pGo.transform;
            pRt.SetParent(rt, false);
            Stretch(pRt);
            fx._particles = pGo.AddComponent<UIParticles>();

            var foodGo = new GameObject("Food", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fx._food = (RectTransform)foodGo.transform;
            fx._food.SetParent(rt, false);
            fx._food.anchorMin = fx._food.anchorMax = new Vector2(0.5f, 0.5f);
            fx._foodImg = foodGo.GetComponent<Image>();
            fx._foodImg.raycastTarget  = false;
            fx._foodImg.preserveAspect = true;
            foodGo.SetActive(false);

            fx._bubble = SpeechBubble.Create(rt, font);
        }

        fx.transform.SetAsLastSibling();   // 게코보다 앞
        fx.Bind(motor);
        return fx;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void Bind(GeckoMotor motor)
    {
        if (_motor != null) _motor.ActionStarted -= OnActionStarted;
        _motor = motor;
        _rig   = motor != null ? motor.Rig : null;
        if (_motor != null) _motor.ActionStarted += OnActionStarted;
    }

    private void OnDestroy()
    {
        if (_motor != null) _motor.ActionStarted -= OnActionStarted;
    }

    // ── 위치 ──────────────────────────────────────────────────

    private bool HasRig => _rig != null && _rig.Skin != null;

    private Vector3 HeadTopWorld   => HasRig ? _rig.PartWorldPoint(GeckoPartId.Head,  new Vector2(0.55f, 0.92f)) : Fallback(260f);
    private Vector3 MouthWorld     => HasRig ? _rig.PartWorldPoint(GeckoPartId.Mouth, new Vector2(0.5f, 0.5f))   : Fallback(160f);
    private Vector3 TongueTipWorld => HasRig ? _rig.PartWorldPoint(GeckoPartId.Tongue2, new Vector2(0.85f, 0.5f)) : MouthWorld;
    private Vector3 BodyWorld      => HasRig ? _rig.PartWorldPoint(GeckoPartId.Body,  new Vector2(0.5f, 0.55f))  : Fallback(120f);

    private Vector3 Fallback(float up)
    {
        var t = _motor != null ? _motor.transform : transform;
        return t.TransformPoint(new Vector3(0f, up, 0f));
    }

    /// <summary>연출 크기 배율 — 어린 게코일수록, 멀리 있을수록 작게</summary>
    private float K => HasRig ? Mathf.Clamp(_rig.StageScale * _rig.DepthScale, 0.55f, 1f) : 1f;

    private float FacingSign => HasRig && !_rig.FacingRight ? -1f : 1f;

    private Vector2 Local(Vector3 world) => _particles.ToLocal(world);

    // ── 돌봄 연출 ─────────────────────────────────────────────

    /// <summary>먹이가 톡 떨어지고, 혀로 낚아채 오물오물. GeckoAnimatorController.TriggerFeedCatch와 동시에 부른다.</summary>
    /// <remarks>sizeScale — 큰 먹이(두비아·슈퍼밀웜)는 조금 크게 떨어진다. 받아먹는 박자는 같다 (Tongue_FeedBig도 앞부분은 같은 동작)</remarks>
    public void FeedDrop(Sprite icon, float sizeScale = 1f)
    {
        if (_feedRoutine != null) StopCoroutine(_feedRoutine);
        _feedRoutine = StartCoroutine(FeedRoutine(icon != null ? icon : FxSprites.Bug, sizeScale));
    }

    private IEnumerator FeedRoutine(Sprite icon, float sizeScale)
    {
        float dur      = GeckoMotor.DurationOf(GeckoAction.Tongue_FeedCatch);
        float tCatch   = dur * GeckoMotor.FEED_SHOOT_PEAK;   // 혀가 가장 멀리 뻗는 순간
        float tSwallow = dur * 0.42f;                         // 혀가 입으로 돌아온 순간 (동작 곡선과 같은 값)
        float k        = K;

        _foodImg.sprite = icon;
        float px = HasRig ? _rig.UIPerSkinPixel : 0.43f;
        float size = Mathf.Clamp(FOOD_SIZE * px, 40f, 90f) * sizeScale;
        _food.sizeDelta = new Vector2(size, size);
        _food.gameObject.SetActive(true);
        _food.SetAsLastSibling();
        _bubble.transform.SetAsLastSibling();

        Vector2 catchL = HasRig
            ? Local(_rig.PredictTongueTipWorld(GeckoMotor.FEED_TONGUE_AIM + GeckoMotor.FEED_HEAD_LEAN, GeckoMotor.FEED_TONGUE_EXT))
            : Local(MouthWorld);
        float dir = FacingSign;
        Vector2 startL = catchL + new Vector2(dir * 70f, 300f) * k;

        AudioManager.Play(Sfx.Pop, 0.6f);

        // 1) 떨어진다 — 점점 빨라지게 (중력 느낌), 살랑살랑 흔들리며
        float t = 0f;
        while (t < tCatch)
        {
            float u = t / tCatch;
            Vector2 p = Vector2.Lerp(startL, catchL, u * u);
            p.x += Mathf.Sin(u * Mathf.PI) * -dir * 18f * k;
            _food.localPosition = p;
            _food.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 14f) * 14f);
            float pop = Mathf.Clamp01(t / 0.12f);
            _food.localScale = Vector3.one * EaseOutBack(pop);
            t += Step();
            yield return null;
        }

        // 2) 혀끝에 붙어 입으로 끌려간다
        AudioManager.PlayVaried(Sfx.Lick, 0.9f);
        while (t < tSwallow)
        {
            float u = Mathf.InverseLerp(tCatch, tSwallow, t);
            _food.localPosition = Local(TongueTipWorld);
            _food.localScale    = Vector3.one * Mathf.Lerp(1f, 0.55f, u);
            t += Step();
            yield return null;
        }

        // 3) 쏙 — 입속으로
        Vector2 from = _food.localPosition;
        for (float s = 0f; s < 0.08f; s += Step())
        {
            float u = s / 0.08f;
            _food.localPosition = Vector2.Lerp(from, Local(MouthWorld), u);
            _food.localScale    = Vector3.one * Mathf.Lerp(0.55f, 0f, u);
            yield return null;
        }
        _food.gameObject.SetActive(false);

        AudioManager.PlayVaried(Sfx.Crunch, 0.8f);
        _particles.Emit(Local(MouthWorld), new UIParticles.Burst
        {
            sprite = FxSprites.Dot, color = CRUMB, colorB = CRUMB, count = 4,
            speed = new Vector2(60f, 140f), angle = 90f, spread = 70f,
            size = new Vector2(8f, 13f) * k, life = new Vector2(0.3f, 0.45f), gravity = 700f,
        });
        _feedRoutine = null;
    }

    /// <summary>영양제·성장촉진제 — 머리 위에서 가루가 솔솔 내려앉는다. 촉진제(sparkle)는 반짝이도</summary>
    public void Dust(bool sparkle)
    {
        float k = K;
        AudioManager.Play(Sfx.Spray, 0.5f);
        _particles.Emit(Local(HeadTopWorld) + new Vector2(0f, 180f * k), new UIParticles.Burst
        {
            sprite = FxSprites.Dot, color = WHITE, colorB = GOLD, count = 22,
            speed = new Vector2(10f, 50f), angle = -90f, spread = 50f,
            size = new Vector2(8f, 16f) * k, life = new Vector2(0.8f, 1.2f),
            gravity = 260f, drag = 1.2f, area = new Vector2(160f, 30f) * k, delay = 0.2f,
        });

        if (!sparkle) return;
        StartCoroutine(DelayedSfx(0.7f, Sfx.Chime, 0.6f));
        _particles.Emit(Local(BodyWorld), new UIParticles.Burst
        {
            sprite = FxSprites.Sparkle, color = GOLD, colorB = WHITE, count = 10,
            speed = new Vector2(20f, 60f), angle = 90f, spread = 180f,
            size = new Vector2(24f, 42f) * k, life = new Vector2(0.6f, 1.0f),
            drag = 1f, spin = 90f, area = new Vector2(220f, 110f) * k, delay = 0.7f,
        });
    }

    /// <summary>분무 — 위에서 물방울이 흩날리고, 게코가 할짝일 때마다 혀끝에서 똑.</summary>
    public void Mist()
    {
        float k = K;
        AudioManager.Play(Sfx.Spray, 0.9f);
        _particles.Emit(Local(HeadTopWorld) + new Vector2(0f, 260f * k), new UIParticles.Burst
        {
            sprite = FxSprites.Drop, color = WATER, colorB = WATER_B, count = 26,
            speed = new Vector2(20f, 80f), angle = -90f, spread = 40f,
            size = new Vector2(12f, 22f) * k, life = new Vector2(0.7f, 1.2f),
            gravity = 520f, drag = 0.6f, area = new Vector2(240f, 60f) * k, delay = 0.35f,
        });
        StartCoroutine(DrinkDrips());
    }

    private IEnumerator DrinkDrips()
    {
        float dur  = GeckoMotor.DurationOf(GeckoAction.Tongue_Drink);
        float span = GeckoMotor.DRINK_LAP_END - GeckoMotor.DRINK_LAP_START;
        float prev = 0f;
        for (int i = 0; i < GeckoMotor.DRINK_LAPS; i++)
        {
            // 할짝 한 번의 가장 멀리 뻗은 순간
            float at = (GeckoMotor.DRINK_LAP_START + (i + 0.5f) / GeckoMotor.DRINK_LAPS * span) * dur;
            yield return Wait(at - prev);
            prev = at;

            AudioManager.PlayVaried(Sfx.Drip, 0.7f, 0.1f);
            _particles.Emit(Local(TongueTipWorld), new UIParticles.Burst
            {
                sprite = FxSprites.Drop, color = WATER, colorB = WATER_B, count = 3,
                speed = new Vector2(60f, 140f), angle = 90f, spread = 60f,
                size = new Vector2(8f, 14f) * K, life = new Vector2(0.35f, 0.5f), gravity = 600f,
            });
        }
    }

    /// <summary>쓰다듬기 — 하트가 둥실둥실</summary>
    public void Hearts()
    {
        float k = K;
        AudioManager.PlayVaried(Sfx.Heart, 0.9f, 0.04f);
        _particles.Emit(Local(HeadTopWorld), new UIParticles.Burst
        {
            sprite = FxSprites.Heart, color = PINK, colorB = PINK_B, count = 5,
            speed = new Vector2(90f, 160f), angle = 90f, spread = 35f,
            size = new Vector2(38f, 58f) * k, life = new Vector2(1.0f, 1.4f),
            gravity = -20f, drag = 1.2f, spin = 30f, sway = 10f, delay = 0.25f,
        });
    }

    /// <summary>청소 — 게코 둘레로 반짝반짝</summary>
    public void Sparkles()
    {
        float k = K;
        AudioManager.Play(Sfx.Sparkle, 0.9f);
        _particles.Emit(Local(BodyWorld), new UIParticles.Burst
        {
            sprite = FxSprites.Sparkle, color = GOLD, colorB = WHITE, count = 12,
            speed = new Vector2(0f, 30f), angle = 90f, spread = 180f,
            size = new Vector2(26f, 48f) * k, life = new Vector2(0.6f, 1.0f),
            drag = 1f, spin = 90f, area = new Vector2(260f, 120f) * k, delay = 0.5f,
        });
        _particles.Emit(Local(BodyWorld), new UIParticles.Burst
        {
            sprite = FxSprites.Dot, color = WHITE, colorB = GOLD, count = 8,
            speed = new Vector2(10f, 40f), angle = 90f, spread = 180f,
            size = new Vector2(14f, 26f) * k, life = new Vector2(0.5f, 0.9f),
            area = new Vector2(280f, 140f) * k, delay = 0.6f,
        });
    }

    /// <summary>거절 — 입 앞으로 작은 '흥' 김</summary>
    public void Refuse()
    {
        AudioManager.Play(Sfx.Refuse, 0.9f);
        EmitPuffs(Local(MouthWorld) + new Vector2(FacingSign * 30f * K, 10f), 2, FacingSign > 0f ? 20f : 160f);
    }

    /// <summary>삐짐 — 머리 위로 뭉게뭉게</summary>
    public void Annoyed()
    {
        AudioManager.Play(Sfx.Annoyed, 0.9f);
        EmitPuffs(Local(HeadTopWorld), 3, 90f);
    }

    private void EmitPuffs(Vector2 at, int count, float angle)
    {
        float k = K;
        _particles.Emit(at, new UIParticles.Burst
        {
            sprite = FxSprites.Puff, color = PUFF, colorB = WHITE, count = count,
            speed = new Vector2(50f, 110f), angle = angle, spread = 30f,
            size = new Vector2(30f, 48f) * k, life = new Vector2(0.5f, 0.8f),
            drag = 2.5f, growTo = 1.4f, gravity = -30f,
        });
    }

    // ── 사건 연출 ─────────────────────────────────────────────

    /// <summary>성장 — 빛의 고리가 퍼지고 반짝이가 사방으로</summary>
    public void GrowthBurst()
    {
        StartCoroutine(GrowthRoutine());
    }

    private IEnumerator GrowthRoutine()
    {
        AudioManager.Play(Sfx.Chime);
        yield return Wait(0.18f);   // LevelUp_Pulse가 가장 커지는 순간에 맞춘다

        Vector2 c = Local(BodyWorld);
        _particles.Emit(c, new UIParticles.Burst
        {
            sprite = FxSprites.Ring, color = GOLD, colorB = WHITE, count = 1,
            size = new Vector2(160f, 160f), life = new Vector2(0.7f, 0.7f), growTo = 3.2f,
        });
        _particles.Emit(c, new UIParticles.Burst
        {
            sprite = FxSprites.Sparkle, color = GOLD, colorB = WHITE, count = 16,
            speed = new Vector2(220f, 380f), angle = 90f, spread = 180f,
            size = new Vector2(28f, 50f), life = new Vector2(0.8f, 1.2f), drag = 2.5f, spin = 120f,
        });
    }

    /// <summary>허물 — 성공하면 껍질 조각이 우수수 + 반짝, 실패하면 조각 몇 개만</summary>
    public void MoltFlakes(bool success)
    {
        StartCoroutine(MoltRoutine(success));
    }

    private IEnumerator MoltRoutine(bool success)
    {
        AudioManager.Play(success ? Sfx.MoltSuccess : Sfx.MoltFail, 0.9f);
        yield return Wait(success ? 0.35f : 0.3f);   // 껍질이 벗겨지기 시작하는 순간

        float k = K;
        _particles.Emit(Local(BodyWorld), new UIParticles.Burst
        {
            sprite = FxSprites.Flake, color = FLAKE, colorB = WHITE, count = success ? 12 : 5,
            speed = new Vector2(40f, 160f), angle = 90f, spread = 70f,
            size = new Vector2(22f, 40f) * k, life = new Vector2(1.1f, 1.7f),
            gravity = 380f, drag = 0.8f, spin = 240f, area = new Vector2(120f, 40f) * k,
        });

        if (!success) yield break;
        yield return Wait(0.55f);
        _particles.Emit(Local(BodyWorld), new UIParticles.Burst
        {
            sprite = FxSprites.Sparkle, color = GOLD, colorB = WHITE, count = 8,
            speed = new Vector2(20f, 60f), angle = 90f, spread = 180f,
            size = new Vector2(26f, 44f) * k, life = new Vector2(0.6f, 0.9f),
            drag = 1f, spin = 90f, area = new Vector2(200f, 100f) * k,
        });
    }

    /// <summary>머리 위 말풍선 한마디</summary>
    public void Say(string message, float hold = 1.5f)
    {
        if (_bubble != null) _bubble.Show(message, () => HeadTopWorld, hold);
    }

    // ── 자동 동작 소리 ────────────────────────────────────────

    // 버튼으로 시작한 동작(먹이·물 …)의 소리는 위 연출이 낸다. 여기서는 게코가 스스로 하는 동작만.
    private void OnActionStarted(GeckoAction action)
    {
        switch (action)
        {
            case GeckoAction.Tongue_Lick:
                StartCoroutine(DelayedSfx(0.18f, Sfx.Lick, 0.35f));
                break;
            case GeckoAction.Tongue_EyeLick:
                StartCoroutine(DelayedSfx(0.45f, Sfx.Lick, 0.4f));
                break;
            case GeckoAction.Jump:
                StartCoroutine(DelayedSfx(0.22f, Sfx.Boing, 0.45f));
                break;
        }
    }

    private IEnumerator DelayedSfx(float delay, Sfx sfx, float volume)
    {
        yield return Wait(delay);
        AudioManager.PlayVaried(sfx, volume);
    }

    // ── 시간 ──────────────────────────────────────────────────

    // GeckoMotor와 같은 시간 흐름(최대 0.05초 단위)을 써야 연출이 동작과 어긋나지 않는다
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
