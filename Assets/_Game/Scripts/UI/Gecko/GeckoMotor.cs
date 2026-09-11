using UnityEngine;

/// <summary>
/// 게코를 살아 움직이게 하는 절차적 애니메이션.
/// 매 프레임 층을 차례로 쌓아 GeckoPose를 만들고 GeckoRig에 넘긴다.
///
///   ① 호흡  ② 기분 자세  ③ 걷기  ④ 머리 미세 움직임·둘러보기
///   ⑤ 동작(액션)  ⑥ 그림자·접지  ⑦ 꼬리 물리  ⑧ 표정
///
/// 키프레임 클립이 아니라 코드로 계산하므로 그림을 바꿔도 움직임이 그대로 유지된다.
/// 수치는 모두 Inspector에서 조정 가능하며 [TBD] 표시는 플레이 테스트 후 확정한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GeckoRig))]
public class GeckoMotor : MonoBehaviour
{
    private const float FADE = 0.12f;   // 동작끼리 넘어가는 시간

    // ── 설정 ─────────────────────────────────────────────────

    [Header("종 특성")]
    [Tooltip("눈꺼풀이 있는 종(레오파드 등)만 켭니다. 크레스티드 게코는 눈을 깜빡일 수 없어 혀로 눈을 닦습니다.")]
    [SerializeField] private bool _canBlink = false;

    [Header("호흡")]
    [SerializeField] private float _breathPeriod = 2.8f;    // 초 [TBD]
    [SerializeField] private float _breathAmount = 0.022f;  // 몸통 세로 크기 변화 [TBD]

    [Header("꼬리")]
    [Tooltip("평소 꼬리 흔들림 크기 (꼬리 끝 기준, 도)")]
    [SerializeField] private float _tailSway = 16f;          // [TBD]
    [SerializeField] private float _tailSwayPeriod = 3.6f;   // 초 [TBD]
    [Tooltip("클수록 꼬리가 몸을 빨리 따라온다")]
    [SerializeField] private float _tailStiffness = 80f;
    [Tooltip("클수록 꼬리 출렁임이 빨리 멎는다")]
    [SerializeField] private float _tailDamping = 10f;

    [Header("걷기")]
    [SerializeField] private float _legSwing = 18f;          // 다리 흔드는 각도 [TBD]
    [SerializeField] private float _legLift  = 12f;          // 발 드는 높이, 스킨 픽셀 [TBD]

    [Header("자동 동작 간격 (초)")]
    [SerializeField] private Vector2 _lickInterval       = new Vector2(4f, 8f);    // CLAUDE.md 4~8초 [TBD]
    [Tooltip("혀 내밀기 중 눈 핥기(시그니처)로 바뀔 확률")]
    [SerializeField, Range(0f, 1f)] private float _eyeLickChance = 0.3f;         // [TBD]
    [SerializeField] private Vector2 _lookInterval       = new Vector2(3f, 6f);    // [TBD]
    [SerializeField] private Vector2 _blinkInterval      = new Vector2(3f, 7f);    // CLAUDE.md 3~7초
    [SerializeField] private Vector2 _moodActionInterval = new Vector2(9f, 18f);   // [TBD]

    [Header("미리보기 (플레이 모드에서 기분 강제)")]
    [SerializeField] private bool      _overrideMood;
    [SerializeField] private GeckoMood _previewMood = GeckoMood.Normal;
    [SerializeField] private bool      _previewMolting;
    [Tooltip("-1 = 실제 게코 데이터를 따름. 0~4 = 해당 성장 단계 크기로 고정")]
    [SerializeField, Range(-1, 4)] private int _previewStage = -1;

    // ── 실행 상태 ────────────────────────────────────────────

    private struct ActionSlot
    {
        public GeckoAction action;
        public float time;
        public float duration;
        public float weight;
        public bool  rightSide;
        public bool  Active => action != GeckoAction.None;
    }

    private GeckoRig  _rig;
    private GeckoPose _pose;

    private GeckoMood _mood = GeckoMood.Normal;
    private bool  _molting;
    private float _wHappy, _wSleepy, _wAngry, _wMolt;

    private bool  _walking;
    private float _walkWeight;
    private float _walkPhase;

    private float _time;
    private float _breathPhase;
    private float _prevBodyY;

    private ActionSlot _cur, _prev;

    private float _lickTimer, _lookTimer, _blinkTimer, _moodTimer, _sparkleTimer, _dozeTimer;
    private float _lookLeft, _lookTotal;
    private GeckoEye _lookEye;
    private float _blinkLeft, _sparkleLeft, _dozeLeft;
    private float _doze;

    private float[] _tailAngle, _tailVel;

    // 한 프레임 동안 층들이 쌓는 값
    private float _tailCurl, _tailRoot, _tailFlick, _tailWaveBoost;
    private bool  _faceOn, _faceEyeSet, _faceMouthSet;
    private GeckoEye   _faceEyeL, _faceEyeR;
    private GeckoMouth _faceMouth;
    private float _w;   // 지금 계산 중인 동작의 가중치

    // ── 공개 API ──────────────────────────────────────────────

    // 같은 오브젝트의 다른 컴포넌트(GeckoAnimatorController 등)가 이 컴포넌트의 Awake보다 먼저
    // 공개 API를 부를 수 있으므로, 리그는 필요할 때 찾는다.
    private GeckoRig RigRef => _rig != null ? _rig : (_rig = GetComponent<GeckoRig>());

    public GeckoRig    Rig           => RigRef;
    public GeckoMood   Mood          => _overrideMood ? _previewMood : _mood;
    public bool        IsMolting     => _overrideMood ? _previewMolting : _molting;
    public bool        IsBusy        => _cur.Active;
    public GeckoAction CurrentAction => _cur.action;

    public bool CanBlink
    {
        get => _canBlink;
        set => _canBlink = value;
    }

    public void SetMood(GeckoMood mood)   => _mood = mood;
    public void SetMolting(bool molting)  => _molting = molting;
    public void SetWalking(bool walking)  => _walking = walking;

    public void SetGrowthStage(int stage, bool immediate)
    {
        if (_previewStage >= 0) stage = _previewStage;
        var rig = RigRef;
        if (rig != null) rig.SetGrowthStage(stage, immediate);
    }

    /// <summary>미리보기용 성장 단계 고정. -1이면 해제.</summary>
    public int PreviewStage
    {
        get => _previewStage;
        set
        {
            _previewStage = Mathf.Clamp(value, -1, 4);
            var rig = RigRef;
            if (_previewStage >= 0 && rig != null) rig.SetGrowthStage(_previewStage, false);
        }
    }

    /// <summary>이동 AI가 한 프레임 동안 움직인 거리(UI 단위). 발이 미끄러지지 않게 걸음을 맞춘다.</summary>
    public void AddTravel(float uiDistance)
    {
        var rig = RigRef;
        if (rig == null) return;
        float k = rig.UIPerSkinPixel;
        if (k <= 0f) return;
        _walkPhase += uiDistance / k / StrideLength() * Mathf.PI * 2f;
    }

    /// <summary>동작 재생. 진행 중인 동작이 있으면 부드럽게 넘어간다.</summary>
    public void Play(GeckoAction action)
    {
        if (action == GeckoAction.None || RigRef == null) return;
        if (action == GeckoAction.Tongue_Lick && Random.value < _eyeLickChance)
            action = GeckoAction.Tongue_EyeLick;

        if (_cur.Active) _prev = _cur;
        _cur = new ActionSlot
        {
            action    = action,
            time      = 0f,
            duration  = DurationOf(action),
            weight    = 0f,
            rightSide = Random.value < 0.5f,
        };

        // 동작 직후 자동 동작이 바로 겹치지 않게
        _lickTimer = Mathf.Max(_lickTimer, 2.5f);
        _lookLeft  = 0f;
        _dozeLeft  = 0f;
    }

    /// <summary>아무 동작도 안 하고 있을 때만 재생 (자동 동작용)</summary>
    public bool TryPlayIdle(GeckoAction action)
    {
        if (_cur.Active) return false;
        Play(action);
        return true;
    }

    public static float DurationOf(GeckoAction a)
    {
        switch (a)
        {
            case GeckoAction.Tongue_Lick:      return 0.55f;
            case GeckoAction.Tongue_EyeLick:   return 1.5f;
            case GeckoAction.Tongue_FeedCatch: return 1.4f;
            case GeckoAction.Tongue_Drink:     return 1.9f;
            case GeckoAction.Happy_LookUp:     return 1.3f;
            case GeckoAction.Angry_TailFlick:  return 1.0f;
            case GeckoAction.Molt_Start:       return 1.3f;
            case GeckoAction.Molt_Finish:      return 1.8f;
            case GeckoAction.LevelUp_Pulse:    return 1.1f;
            case GeckoAction.Pet_Reaction:     return 1.6f;
            case GeckoAction.Surprise:         return 0.8f;
            case GeckoAction.Blink_Short:      return 0.16f;
            case GeckoAction.Jump:             return 0.95f;
            default:                           return 0f;
        }
    }

    // ── 생명주기 ──────────────────────────────────────────────

    private void Awake()
    {
        _rig = GetComponent<GeckoRig>();
        int n = _rig != null ? _rig.TailSegments : 12;
        _pose      = new GeckoPose(n);
        _tailAngle = new float[n];
        _tailVel   = new float[n];

        _lickTimer    = Rand(_lickInterval);
        _lookTimer    = Rand(_lookInterval);
        _blinkTimer   = Rand(_blinkInterval);
        _moodTimer    = Rand(_moodActionInterval);
        _sparkleTimer = Random.Range(4f, 8f);
        _dozeTimer    = Random.Range(5f, 10f);
        _breathPhase  = Random.value * Mathf.PI * 2f;
    }

    private void LateUpdate()
    {
        if (_rig == null || _rig.Skin == null) return;

        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (dt <= 0f) return;
        _time += dt;

        var mood = Mood;
        UpdateWeights(mood, IsMolting, dt);
        UpdateActions(dt);
        UpdateIdle(mood, dt);

        _pose.Reset();
        _tailCurl = _tailRoot = _tailFlick = _tailWaveBoost = 0f;
        _faceEyeSet = _faceMouthSet = false;
        _pose[GeckoPartId.Tongue1].scale = new Vector2(0f, 1f);
        _pose[GeckoPartId.Tongue2].scale = new Vector2(0f, 1f);
        _pose[GeckoPartId.ShedPatch].alpha = 0f;

        LayerBreath(dt);
        LayerMood();
        LayerWalk();
        LayerHead();

        if (_prev.Active) { _w = _prev.weight; _faceOn = false; EvaluateAction(ref _prev); }
        if (_cur.Active)  { _w = _cur.weight;  _faceOn = true;  EvaluateAction(ref _cur); }

        LayerGround();
        SimulateTail(dt);
        ResolveFace(mood);

        _rig.Solve(_pose, dt);
    }

    // ── 상태 갱신 ────────────────────────────────────────────

    private void UpdateWeights(GeckoMood mood, bool molting, float dt)
    {
        float k = dt * 1.5f;
        _wHappy  = Mathf.MoveTowards(_wHappy,  mood == GeckoMood.Happy  ? 1f : 0f, k);
        _wSleepy = Mathf.MoveTowards(_wSleepy, mood == GeckoMood.Sleepy ? 1f : 0f, k);
        _wAngry  = Mathf.MoveTowards(_wAngry,  mood == GeckoMood.Angry  ? 1f : 0f, k);
        _wMolt   = Mathf.MoveTowards(_wMolt,   molting ? 1f : 0f, dt);
        _walkWeight = Mathf.MoveTowards(_walkWeight, _walking && !_cur.Active ? 1f : 0f, dt * 4f);
    }

    private void UpdateActions(float dt)
    {
        if (_prev.Active)
        {
            _prev.time   += dt;
            _prev.weight -= dt / FADE;
            if (_prev.weight <= 0f || _prev.time >= _prev.duration) _prev = default;
        }
        if (_cur.Active)
        {
            _cur.time  += dt;
            _cur.weight = Mathf.Min(1f, _cur.weight + dt / FADE);
            if (_cur.time >= _cur.duration) _cur = default;
        }
    }

    private void UpdateIdle(GeckoMood mood, float dt)
    {
        _lookLeft    = Mathf.Max(0f, _lookLeft - dt);
        _blinkLeft   = Mathf.Max(0f, _blinkLeft - dt);
        _sparkleLeft = Mathf.Max(0f, _sparkleLeft - dt);
        _dozeLeft    = Mathf.Max(0f, _dozeLeft - dt);
        if (mood != GeckoMood.Sleepy) _dozeLeft = 0f;
        _doze = Mathf.MoveTowards(_doze, _dozeLeft > 0f ? 1f : 0f, dt * 1.2f);

        // 깜빡임 — 눈꺼풀 있는 종만
        if (_canBlink)
        {
            _blinkTimer -= dt;
            if (_blinkTimer <= 0f)
            {
                _blinkLeft  = 0.13f;
                _blinkTimer = Rand(_blinkInterval);
            }
        }

        bool idle = !_cur.Active && _walkWeight < 0.05f;
        if (!idle) return;

        _lickTimer    -= dt;
        _lookTimer    -= dt;
        _moodTimer    -= dt;
        _sparkleTimer -= dt;
        if (mood == GeckoMood.Sleepy) _dozeTimer -= dt;

        if (mood == GeckoMood.Sleepy && _dozeTimer <= 0f)
        {
            _dozeLeft  = Random.Range(2.5f, 5f);
            _dozeTimer = Random.Range(6f, 12f);
            return;
        }
        if (_dozeLeft > 0f) return;   // 조는 동안은 가만히

        if (_lickTimer <= 0f)
        {
            _lickTimer = Rand(_lickInterval) * (mood == GeckoMood.Sleepy ? 2f : 1f);
            Play(GeckoAction.Tongue_Lick);
            return;
        }

        if (_moodTimer <= 0f)
        {
            _moodTimer = Rand(_moodActionInterval) * (mood == GeckoMood.Angry ? 0.5f : 1f);
            var a = MoodAction(mood);
            if (a != GeckoAction.None)
            {
                Play(a);
                return;
            }
        }

        if (_lookTimer <= 0f)
        {
            _lookTimer = Rand(_lookInterval);
            _lookTotal = _lookLeft = Random.Range(0.8f, 1.6f);
            float r = Random.value;
            _lookEye = r < 0.4f ? GeckoEye.LookLeft : (r < 0.8f ? GeckoEye.LookRight : GeckoEye.LookUp);
        }

        if (mood == GeckoMood.Happy && _sparkleTimer <= 0f)
        {
            _sparkleTimer = Random.Range(5f, 9f);
            _sparkleLeft  = 1.1f;
        }
    }

    private static GeckoAction MoodAction(GeckoMood mood)
    {
        switch (mood)
        {
            case GeckoMood.Happy: return Random.value < 0.7f ? GeckoAction.Happy_LookUp : GeckoAction.Jump;
            case GeckoMood.Angry: return GeckoAction.Angry_TailFlick;
            default:              return GeckoAction.None;
        }
    }

    // ── ① 호흡 ───────────────────────────────────────────────

    private void LayerBreath(float dt)
    {
        float period = _breathPeriod * Mathf.Lerp(1f, 1.6f, _wSleepy) * Mathf.Lerp(1f, 0.75f, _wAngry);
        _breathPhase += dt / Mathf.Max(0.3f, period) * Mathf.PI * 2f;

        float amount = _breathAmount
                     * (1f + 0.4f * _wSleepy)
                     * (1f - 0.3f * _wAngry)
                     * (1f - 0.5f * _walkWeight);
        float b = Mathf.Sin(_breathPhase);

        _pose[GeckoPartId.Body].scale += new Vector2(amount * 0.35f * b, amount * b);
        _pose[GeckoPartId.Head].angle += 0.8f * b;
    }

    // ── ② 기분 자세 ──────────────────────────────────────────

    private void LayerMood()
    {
        _pose[GeckoPartId.Head].angle    += 4f * _wHappy - 9f * _wSleepy - 3f * _wAngry - 7f * _doze;
        _pose[GeckoPartId.Body].offset.y -= 3f * _wSleepy + 2f * _doze;
        _pose[GeckoPartId.Body].scale.y  -= 0.015f * _wAngry;   // 화나면 몸에 힘이 들어감

        _tailCurl      += 8f * _wHappy - 12f * _wSleepy - 4f * _doze;
        _tailWaveBoost += 0.5f * _wHappy - 0.6f * _wSleepy + 0.4f * _wAngry;

        // 허물 벗을 준비 중 — 몸에 들뜬 껍질이 보인다
        float molt = 0.85f * _wMolt * (0.92f + 0.08f * Mathf.Sin(_time * 2f));
        _pose[GeckoPartId.ShedPatch].alpha = Mathf.Max(_pose[GeckoPartId.ShedPatch].alpha, molt);
    }

    // ── ③ 걷기 ───────────────────────────────────────────────

    private void LayerWalk()
    {
        float w = _walkWeight;
        if (w <= 0.001f) return;

        float ph = _walkPhase;
        // 도마뱀 걸음: 대각선 다리끼리 같이 움직인다 (앞-가까운 + 뒤-먼 / 앞-먼 + 뒤-가까운)
        LegStep(GeckoPartId.LegFrontNear, ph,            w);
        LegStep(GeckoPartId.LegBackFar,   ph,            w);
        LegStep(GeckoPartId.LegFrontFar,  ph + Mathf.PI, w);
        LegStep(GeckoPartId.LegBackNear,  ph + Mathf.PI, w);

        _pose[GeckoPartId.Body].offset.y += w * 3f * (0.5f + 0.5f * Mathf.Cos(2f * ph));
        _pose[GeckoPartId.Body].angle    += w * 1.6f * Mathf.Sin(ph);
        _pose[GeckoPartId.Head].angle    -= w * 1.4f * Mathf.Sin(ph);   // 머리는 흔들림을 상쇄해 시선이 안정된다
        _tailWaveBoost += 0.8f * w;
    }

    private void LegStep(GeckoPartId id, float phase, float w)
    {
        ref var leg = ref _pose[id];
        leg.angle    += w * _legSwing * Mathf.Sin(phase);                // + = 발이 앞으로
        leg.offset.y += w * _legLift * Mathf.Max(0f, Mathf.Cos(phase));  // 앞으로 옮기는 동안만 발을 든다
    }

    private float StrideLength()
    {
        float leg = _rig != null ? _rig.RestLengthDown(GeckoPartId.LegFrontNear) : 0f;
        if (leg < 20f) leg = 220f;
        return Mathf.Max(60f, 2f * Mathf.PI * leg * Mathf.Sin(_legSwing * Mathf.Deg2Rad) * 0.9f);
    }

    // ── ④ 머리 ───────────────────────────────────────────────

    private void LayerHead()
    {
        float still = 1f - _walkWeight;
        float n = Mathf.PerlinNoise(_time * 0.35f, 0.37f) - 0.5f;
        float m = Mathf.PerlinNoise(0.71f, _time * 0.27f) - 0.5f;

        ref var head = ref _pose[GeckoPartId.Head];
        head.angle  += still * 4.4f * n;
        head.offset += still * new Vector2(2f * m, 1.5f * n);

        if (_lookLeft > 0f && !_cur.Active)
        {
            float e = Smooth(Mathf.Min(_lookTotal - _lookLeft, _lookLeft) / 0.2f);
            if (_lookEye == GeckoEye.LookUp) head.angle += 5f * e;
            else head.angle += (_lookEye == GeckoEye.LookLeft ? 2.5f : -2.5f) * e;
        }
    }

    // ── ⑥ 그림자 · 접지 ──────────────────────────────────────

    private void LayerGround()
    {
        ref var body = ref _pose[GeckoPartId.Body];

        // 몸이 낮아질 때 발이 바닥 아래로 파고들지 않게
        float sink = Mathf.Min(0f, body.offset.y);
        if (sink < 0f)
        {
            _pose[GeckoPartId.LegFrontNear].offset.y -= sink;
            _pose[GeckoPartId.LegFrontFar].offset.y  -= sink;
            _pose[GeckoPartId.LegBackNear].offset.y  -= sink;
            _pose[GeckoPartId.LegBackFar].offset.y   -= sink;
        }

        // 몸이 뜨면 그림자가 작고 옅어진다
        float k = Mathf.Clamp01(Mathf.Max(0f, body.offset.y) / 140f);
        ref var shadow = ref _pose[GeckoPartId.Shadow];
        shadow.scale *= new Vector2(1f - 0.4f * k, 1f - 0.3f * k);
        shadow.alpha *= 1f - 0.5f * k;

        // 허물은 몸통 크기(호흡)를 따라간다
        ref var shed = ref _pose[GeckoPartId.ShedPatch];
        shed.scale = Vector2.Scale(shed.scale, body.scale);
    }

    // ── ⑦ 꼬리 물리 ──────────────────────────────────────────

    private void SimulateTail(float dt)
    {
        int n = _tailAngle.Length;
        float bodyY = _pose[GeckoPartId.Body].offset.y;
        float vy = (bodyY - _prevBodyY) / dt;
        _prevBodyY = bodyY;

        float swayAmp  = _tailSway * Mathf.Max(0.1f, 1f + _tailWaveBoost);
        float swayFreq = Mathf.PI * 2f / Mathf.Max(0.5f, _tailSwayPeriod)
                       * (1f + 0.8f * _wAngry - 0.4f * _wSleepy);

        float wsum = 0f;
        for (int k = 0; k < n; k++) wsum += 0.35f + (k + 1f) / n;

        for (int k = 0; k < n; k++)
        {
            float f     = (k + 1f) / n;              // 0 = 뿌리, 1 = 끝
            float share = (0.35f + f) / wsum;        // 끝으로 갈수록 많이 굽는다
            float wave  = Mathf.Sin(_time * swayFreq - k * 0.42f);
            float walk  = Mathf.Sin(_walkPhase - k * 0.5f);

            float target = share * (_tailCurl
                                  + swayAmp * wave * 1.6f
                                  + 16f * _walkWeight * walk
                                  + _tailFlick * (0.3f + f) * 1.8f);

            float stiff = _tailStiffness * (1.2f - 0.6f * f);
            float damp  = _tailDamping   * (1.1f - 0.4f * f);
            _tailVel[k]   += (stiff * (target - _tailAngle[k]) - damp * _tailVel[k]) * dt;
            _tailVel[k]   -= vy * 0.05f * f;         // 몸이 튀어 오르면 꼬리는 한 박자 늦게 따라온다
            _tailAngle[k]  = Mathf.Clamp(_tailAngle[k] + _tailVel[k] * dt, -40f, 40f);
            _pose.tailBend[k] = _tailAngle[k];
        }

        _pose[GeckoPartId.Tail].angle += _tailRoot;
    }

    // ── ⑧ 표정 ───────────────────────────────────────────────

    private void ResolveFace(GeckoMood mood)
    {
        GeckoEye   eye   = GeckoEye.Open;
        GeckoMouth mouth = GeckoMouth.Closed;

        switch (mood)
        {
            case GeckoMood.Happy:
                mouth = GeckoMouth.Smile;
                if (_sparkleLeft > 0f) eye = GeckoEye.Sparkle;
                break;
            case GeckoMood.Sleepy:
                eye = GeckoEye.Sleepy;
                break;
            case GeckoMood.Angry:
                mouth = GeckoMouth.Frown;
                break;
        }

        if (mood != GeckoMood.Sleepy)
        {
            if (_lookLeft > 0f && !_cur.Active) eye = _lookEye;
            if (_walkWeight > 0.5f) eye = GeckoEye.LookRight;   // 가는 방향을 본다 (좌우 반전 시 자동으로 따라감)
        }
        if (_doze > 0.5f) eye = GeckoEye.Closed;

        GeckoEye eyeL = eye, eyeR = eye;
        if (_blinkLeft > 0f) eyeL = eyeR = GeckoEye.Closed;

        if (_faceEyeSet)
        {
            eyeL = _faceEyeL;
            eyeR = _faceEyeR;
        }
        if (_faceMouthSet) mouth = _faceMouth;

        _pose.eyeL  = eyeL;
        _pose.eyeR  = eyeR;
        _pose.mouth = mouth;
    }

    // ── ⑤ 동작 ───────────────────────────────────────────────

    private void EvaluateAction(ref ActionSlot a)
    {
        float t   = a.duration > 0f ? Mathf.Clamp01(a.time / a.duration) : 1f;
        float sec = a.time;

        switch (a.action)
        {
            case GeckoAction.Tongue_Lick:      ActLipLick(t);                break;
            case GeckoAction.Tongue_EyeLick:   ActEyeLick(t, sec, a.rightSide); break;
            case GeckoAction.Tongue_FeedCatch: ActFeedCatch(t, sec);         break;
            case GeckoAction.Tongue_Drink:     ActDrink(t);                  break;
            case GeckoAction.Happy_LookUp:     ActHappyLookUp(t);            break;
            case GeckoAction.Angry_TailFlick:  ActTailFlick(t, sec);         break;
            case GeckoAction.Molt_Start:       ActMoltStart(t, sec);         break;
            case GeckoAction.Molt_Finish:      ActMoltFinish(t, sec);        break;
            case GeckoAction.LevelUp_Pulse:    ActLevelUp(t);                break;
            case GeckoAction.Pet_Reaction:     ActPet(t, sec);               break;
            case GeckoAction.Surprise:         ActSurprise(t);               break;
            case GeckoAction.Blink_Short:      SetEyes(GeckoEye.Closed, GeckoEye.Closed); break;
            case GeckoAction.Jump:             ActJump(t);                   break;
        }
    }

    // 혀 내밀기 — 입술 핥기
    private void ActLipLick(float t)
    {
        float e = Bell(t, 0.05f, 0.45f, 0.95f);
        TongueForward(-6f, 24f, 0.55f * e);
        Rot(GeckoPartId.Head, 2f * e);
        if (e > 0.04f) SetMouth(GeckoMouth.OpenSmall);
    }

    // 혀로 눈 닦기 — 시그니처. 크레스티드 게코는 눈꺼풀이 없어 이렇게 눈을 닦는다
    private void ActEyeLick(float t, float sec, bool right)
    {
        float tilt = Bell(t, 0f, 0.25f, 0.95f);
        Rot(GeckoPartId.Head, (right ? -3.5f : 3.5f) * tilt);   // 닦을 눈 쪽으로 고개를 기울인다

        float reach = Hold(t, 0.12f, 0.32f, 0.72f, 0.9f);
        float hold  = Hold(t, 0.30f, 0.36f, 0.66f, 0.72f);
        float wipe  = Mathf.Sin(sec * Mathf.PI * 2f * 2.2f) * 12f * hold;
        TongueToEye(right, reach, wipe);

        if (reach > 0.03f) SetMouth(GeckoMouth.OpenSmall);
        if (t > 0.88f) SetEyes(right ? GeckoEye.Open : GeckoEye.Sparkle, right ? GeckoEye.Sparkle : GeckoEye.Open);
        else           SetEyes(GeckoEye.Open, GeckoEye.Open);
    }

    // 먹이 받아먹기 — 먹이를 먼저 보고, 0.3초 뒤 혀 발사, 오물오물
    private void ActFeedCatch(float t, float sec)
    {
        float lean = Hold(t, 0f, 0.2f, 0.45f, 0.65f);
        Rot(GeckoPartId.Head, -7f * lean);
        Move(GeckoPartId.Body, 6f * lean, 0f);

        float shoot = Bell(t, 0.22f, 0.30f, 0.42f);
        TongueForward(-24f, -10f, 1.25f * shoot);

        float chew = Hold(t, 0.44f, 0.5f, 0.9f, 1f);
        Rot(GeckoPartId.Head, 1.5f * Mathf.Sin(sec * Mathf.PI * 2f * 3.5f) * chew);
        _tailCurl += 10f * Bell(t, 0.4f, 0.6f, 1f) * _w;

        SetEyes(t < 0.44f ? GeckoEye.LookRight : GeckoEye.Happy);
        if (t > 0.18f && t < 0.44f)      SetMouth(GeckoMouth.OpenWide);
        else if (t >= 0.44f && t < 0.9f) SetMouth(((int)(sec * 7f) & 1) == 0 ? GeckoMouth.Chew : GeckoMouth.Closed);
        else if (t >= 0.9f)              SetMouth(GeckoMouth.Smile);
    }

    // 물 마시기 — 고개 숙여 세 번 할짝
    private void ActDrink(float t)
    {
        float down = Hold(t, 0f, 0.15f, 0.85f, 1f);
        Rot(GeckoPartId.Head, -14f * down);
        Move(GeckoPartId.Body, 0f, -3f * down);

        float laps = Win(t, 0.2f, 0.8f) * 3f;
        float lap  = t > 0.2f && t < 0.8f ? Mathf.Sin((laps - Mathf.Floor(laps)) * Mathf.PI) : 0f;
        TongueForward(-72f, 20f, 0.5f * lap);

        SetEyes(t > 0.85f ? GeckoEye.Happy : GeckoEye.Open);
        SetMouth(down > 0.3f ? GeckoMouth.Drink : GeckoMouth.Closed);
    }

    // 기뻐서 올려다보기
    private void ActHappyLookUp(float t)
    {
        float e   = Bell(t, 0f, 0.3f, 1f);
        float hop = Bell(t, 0.15f, 0.3f, 0.5f);
        Rot(GeckoPartId.Head, 16f * e);
        Rot(GeckoPartId.Body, 3f * e);
        Move(GeckoPartId.Body, 0f, 12f * hop);
        Rot(GeckoPartId.LegFrontNear, 10f * hop);
        Rot(GeckoPartId.LegFrontFar,  10f * hop);
        _tailCurl += 26f * e * _w;

        SetEyes(t < 0.75f ? GeckoEye.Happy : GeckoEye.Sparkle);
        SetMouth(e > 0.5f ? GeckoMouth.OpenWide : GeckoMouth.Smile);
    }

    // 화나서 꼬리 튕기기
    private void ActTailFlick(float t, float sec)
    {
        float e   = Hold(t, 0f, 0.1f, 0.75f, 1f);
        float osc = Mathf.Sin(sec * Mathf.PI * 2f * 2.6f);
        _tailFlick += 40f * osc * e * _w;
        _tailRoot  += 9f * osc * e * _w;
        Grow(GeckoPartId.Body, 0f, -0.03f * e);
        Rot(GeckoPartId.Head, -4f * e);

        SetEyes(GeckoEye.Open);
        SetMouth(GeckoMouth.Frown);
    }

    // 허물 시작 (실패 시에도 재생) — 몸을 부르르 떨고 껍질이 들뜬다
    private void ActMoltStart(float t, float sec)
    {
        float e      = Hold(t, 0f, 0.1f, 0.7f, 1f);
        float shiver = Mathf.Sin(sec * Mathf.PI * 2f * 13f) * e;
        Move(GeckoPartId.Body, 2.6f * shiver, 0f);
        Rot(GeckoPartId.Head, 1.5f * shiver);

        float shed = Hold(t, 0f, 0.25f, 0.75f, 1f);
        Fade(GeckoPartId.ShedPatch, Mathf.Max(_pose[GeckoPartId.ShedPatch].alpha, shed));

        SetEyes(t < 0.35f ? GeckoEye.Surprised : GeckoEye.Open);
        SetMouth(t < 0.35f ? GeckoMouth.Surprised : (t > 0.5f ? GeckoMouth.Frown : GeckoMouth.Closed));
    }

    // 허물 성공 — 몸을 흔들어 껍질을 털어내고 반짝
    private void ActMoltFinish(float t, float sec)
    {
        float wiggle = Hold(t, 0f, 0.08f, 0.4f, 0.5f);
        Rot(GeckoPartId.Body, 5f * Mathf.Sin(sec * Mathf.PI * 2f * 4f) * wiggle);

        float peel = Smooth(Win(t, 0.12f, 0.65f));
        Fade(GeckoPartId.ShedPatch, 1f - peel);
        Move(GeckoPartId.ShedPatch, -10f * peel, -48f * peel);
        Grow(GeckoPartId.ShedPatch, 0.12f * peel, 0.12f * peel);

        _pose.rootScale += 0.08f * Bell(t, 0.5f, 0.62f, 0.82f) * _w;
        _tailCurl += 16f * Bell(t, 0.45f, 0.65f, 1f) * _w;

        SetEyes(t < 0.45f ? GeckoEye.Open : GeckoEye.Sparkle);
        if (t >= 0.5f && t < 0.8f) SetMouth(GeckoMouth.OpenWide);
        else SetMouth(t >= 0.8f ? GeckoMouth.Smile : GeckoMouth.Closed);
    }

    // 성장 단계 상승
    private void ActLevelUp(float t)
    {
        _pose.rootScale += (0.14f * Bell(t, 0f, 0.22f, 0.5f) - 0.03f * Bell(t, 0.45f, 0.6f, 0.8f)) * _w;
        Move(GeckoPartId.Body, 0f, 14f * Bell(t, 0.05f, 0.2f, 0.42f));
        _tailCurl += 20f * Bell(t, 0f, 0.3f, 1f) * _w;

        SetEyes(GeckoEye.Sparkle);
        SetMouth(t < 0.6f ? GeckoMouth.OpenWide : GeckoMouth.Smile);
    }

    // 쓰다듬기 반응 — 손에 머리를 부비고 꼬리를 흔든다
    private void ActPet(float t, float sec)
    {
        float e = Hold(t, 0f, 0.15f, 0.75f, 1f);
        Rot(GeckoPartId.Head, (8f + 3f * Mathf.Sin(sec * Mathf.PI * 2f * 1.3f)) * e);
        Move(GeckoPartId.Head, -4f * e, 0f);
        Rot(GeckoPartId.Body, -2f * e);
        Grow(GeckoPartId.Body, 0.015f * e, 0.02f * e);
        _tailCurl      += 12f * e * _w;
        _tailWaveBoost += 1.0f * e * _w;

        SetEyes(GeckoEye.Happy);
        SetMouth(GeckoMouth.Smile);
    }

    // 놀람 — 폴짝
    private void ActSurprise(float t)
    {
        float hop = Bell(t, 0f, 0.18f, 0.45f);
        float e   = Hold(t, 0f, 0.08f, 0.7f, 1f);
        Move(GeckoPartId.Body, 0f, 24f * hop);
        Rot(GeckoPartId.Head, 5f * e);
        Rot(GeckoPartId.LegFrontNear,  10f * hop);
        Rot(GeckoPartId.LegFrontFar,   10f * hop);
        Rot(GeckoPartId.LegBackNear,  -10f * hop);
        Rot(GeckoPartId.LegBackFar,   -10f * hop);
        _tailCurl += 22f * e * _w;

        SetEyes(GeckoEye.Surprised);
        SetMouth(t < 0.75f ? GeckoMouth.Surprised : GeckoMouth.Closed);
    }

    // 점프 — 웅크렸다 뛰고 착지
    private void ActJump(float t)
    {
        float crouch = Bell(t, 0f, 0.2f, 0.3f);
        Move(GeckoPartId.Body, 0f, -8f * crouch);
        Grow(GeckoPartId.Body, 0.02f * crouch, -0.06f * crouch);

        float h = t > 0.24f && t < 0.72f ? Mathf.Sin(Win(t, 0.24f, 0.72f) * Mathf.PI) : 0f;
        Move(GeckoPartId.Body, 0f, 62f * h);
        Rot(GeckoPartId.LegFrontNear,  16f * h);
        Rot(GeckoPartId.LegFrontFar,   16f * h);
        Rot(GeckoPartId.LegBackNear,  -16f * h);
        Rot(GeckoPartId.LegBackFar,   -16f * h);

        float land = Bell(t, 0.7f, 0.78f, 0.95f);
        Move(GeckoPartId.Body, 0f, -6f * land);
        Grow(GeckoPartId.Body, 0.03f * land, -0.07f * land);

        SetEyes(h > 0.2f ? GeckoEye.Happy : GeckoEye.Open);
        SetMouth(GeckoMouth.Smile);
    }

    // ── 혀 ───────────────────────────────────────────────────

    /// <summary>혀를 aim 방향(머리 기준 도)으로 ext 비율만큼 뻗는다. curl = 혀끝 추가 굽힘.</summary>
    private void TongueForward(float aimDeg, float curlDeg, float ext)
    {
        if (ext <= 0.0001f) return;
        Vector2 root = _rig.RestPosition(GeckoPartId.Tongue1);
        float a1 = Mathf.Max(1f, (_rig.RestPosition(GeckoPartId.Tongue2) - root).magnitude);
        float l2 = Mathf.Max(1f, _rig.RestLengthForward(GeckoPartId.Tongue2));
        TongueRaw(aimDeg, curlDeg, ext, a1, l2);
    }

    /// <summary>혀끝이 정확히 눈 한가운데에 닿도록 각도와 길이를 계산한다. 그림이 바뀌어도 자동으로 맞는다.</summary>
    private void TongueToEye(bool right, float amount, float wipeDeg)
    {
        if (amount <= 0.0001f) return;
        Vector2 root = _rig.RestPosition(GeckoPartId.Tongue1);
        Vector2 eye  = _rig.RestPosition(right ? GeckoPartId.EyeR : GeckoPartId.EyeL);
        Vector2 v = eye - root;
        float dist = v.magnitude;
        if (dist < 1f) return;

        float a1 = Mathf.Max(1f, (_rig.RestPosition(GeckoPartId.Tongue2) - root).magnitude);
        float l2 = Mathf.Max(1f, _rig.RestLengthForward(GeckoPartId.Tongue2));

        float curl  = (right ? 1f : -1f) * (38f + wipeDeg);   // 뺨 바깥쪽으로 휘어 들어간다
        float c     = curl * Mathf.Deg2Rad;
        float chord = Mathf.Sqrt(a1 * a1 + l2 * l2 + 2f * a1 * l2 * Mathf.Cos(c));
        float ext   = Mathf.Clamp(dist / Mathf.Max(1f, chord), 0.3f, 1.35f) * amount;
        float aim   = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        TongueRaw(aim, curl, ext, a1, l2);
    }

    // 두 마디 혀: 첫 마디 방향 = 목표 방향 - 보정각, 둘째 마디 = +curl → 끝이 정확히 목표 방향에 온다
    private void TongueRaw(float aimDeg, float curlDeg, float ext, float a1, float l2)
    {
        float c = curlDeg * Mathf.Deg2Rad;
        float delta = Mathf.Atan2(l2 * Mathf.Sin(c), a1 + l2 * Mathf.Cos(c)) * Mathf.Rad2Deg;
        Rot(GeckoPartId.Tongue1, aimDeg - delta);
        Rot(GeckoPartId.Tongue2, curlDeg);
        Grow(GeckoPartId.Tongue1, ext, 0f);
        Grow(GeckoPartId.Tongue2, ext, 0f);
    }

    // ── 동작 헬퍼 (현재 가중치 _w 반영) ──────────────────────

    private void Rot(GeckoPartId id, float deg)          => _pose[id].angle  += deg * _w;
    private void Move(GeckoPartId id, float x, float y)  => _pose[id].offset += new Vector2(x, y) * _w;
    private void Grow(GeckoPartId id, float x, float y)  => _pose[id].scale  += new Vector2(x, y) * _w;
    private void Fade(GeckoPartId id, float alpha)       => _pose[id].alpha   = Mathf.Lerp(_pose[id].alpha, alpha, _w);

    private void SetEyes(GeckoEye both) => SetEyes(both, both);

    private void SetEyes(GeckoEye left, GeckoEye right)
    {
        if (!_faceOn) return;
        _faceEyeSet = true;
        _faceEyeL   = left;
        _faceEyeR   = right;
    }

    private void SetMouth(GeckoMouth mouth)
    {
        if (!_faceOn) return;
        _faceMouthSet = true;
        _faceMouth    = mouth;
    }

    // ── 수학 ─────────────────────────────────────────────────

    private static float Rand(Vector2 range) => Random.Range(range.x, range.y);

    private static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private static float Win(float t, float a, float b) => Mathf.Clamp01((t - a) / Mathf.Max(0.0001f, b - a));

    // a에서 올라가 peak에서 최고, b에서 다시 0
    private static float Bell(float t, float a, float peak, float b)
        => t < peak ? Smooth(Win(t, a, peak)) : 1f - Smooth(Win(t, peak, b));

    // a→b 올라가서 유지, c→d 내려옴
    private static float Hold(float t, float a, float b, float c, float d)
        => Smooth(Win(t, a, b)) * (1f - Smooth(Win(t, c, d)));
}
