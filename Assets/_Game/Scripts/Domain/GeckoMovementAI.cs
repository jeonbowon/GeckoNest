using System.Collections;
using UnityEngine;

/// <summary>
/// 게코가 테라리움을 돌아다니는 AI (UI 좌표).
///
/// - 바닥: 발 높이 groundBand(가까운 쪽 ~ 먼 쪽) 안에서 앞뒤·좌우·대각선으로 다닌다.
///   발 높이가 높을수록(멀리 있을수록) 작고 느리게 그려 원근감을 준다
/// - 기어오르기: 가끔 몸을 세워(머리가 위로 오게 90° 회전) 벽을 타고 올라가 잠깐 매달렸다가,
///   머리를 아래로 돌려 내려오고 바닥에서 몸을 눕힌다. 오르는 동안 크기는 출발한 바닥 높이 기준으로 고정
/// - 먹이·쓰다듬기 등 동작 중이면 멈춰서 기다리고, 졸린 기분이면 다니지 않는다
/// - 꼬리를 만지면 `Flee` — 바닥에서는 누른 곳 반대쪽으로, 벽에서는 더 위로 달아나거나 후다닥 내려온다
///
/// 게코 오브젝트의 피벗은 발밑이므로 회전도 발밑을 중심으로 한다 (GeckoTouch가 같은 각도로 따라 돈다).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GeckoMotor))]
public class GeckoMovementAI : MonoBehaviour
{
    [Header("걷기")]
    [Tooltip("다 자란 게코 기준 최고 속도 (UI 단위/초)")]
    [SerializeField] private float moveSpeed   = 90f;    // [TBD]
    [SerializeField] private float waitTimeMin = 2.5f;   // [TBD]
    [SerializeField] private float waitTimeMax = 6f;     // [TBD]
    [SerializeField, Range(0f, 1f)] private float pauseMidwayChance = 0.2f;

    [Header("다니는 범위 · 원근")]
    [Tooltip("발이 닿는 높이 범위 (부모 영역 아래 끝 기준, UI 단위). x=가까운 쪽(아래), y=먼 쪽(위)")]
    [SerializeField] private Vector2 groundBand = new Vector2(380f, 950f);   // [TBD] 돌봄 버튼(아래 308) 위 ~ 화면 중간
    [Tooltip("화면 좌우 끝에서 띄울 여백")]
    [SerializeField] private float sideMargin = 30f;
    [Tooltip("가장 먼 쪽(groundBand.y)에서의 크기")]
    [SerializeField, Range(0.4f, 1f)] private float farScale = 0.62f;        // [TBD]

    [Header("기어오르기")]
    [Tooltip("한 번 쉬고 난 뒤 걷는 대신 벽을 탈 확률")]
    [SerializeField, Range(0f, 1f)] private float climbChance = 0.3f;        // [TBD]
    [Tooltip("오르는 높이 (최소·최대, UI 단위)")]
    [SerializeField] private Vector2 climbHeight    = new Vector2(400f, 800f); // [TBD]
    [Tooltip("부모 영역 위 끝에서 이만큼 아래까지만 오른다 — 위쪽 상태 띠(위에서 340)에 닿지 않게")]
    [SerializeField] private float   climbTopMargin = 420f;                    // [TBD]
    [Tooltip("매달려 있는 시간 (최소·최대, 초)")]
    [SerializeField] private Vector2 climbHang      = new Vector2(2f, 4f);     // [TBD]
    [Tooltip("평소 걸음 대비 벽 타는 속도")]
    [SerializeField] private float   climbSpeedScale = 0.75f;                  // [TBD]
    [Tooltip("몸을 세우거나 눕히는 시간 (초). 위→아래로 돌아설 때는 1.4배")]
    [SerializeField] private float   climbTurnTime   = 0.45f;                  // [TBD]

    [Header("도망 (꼬리를 만졌을 때)")]
    [Tooltip("평소 걸음의 몇 배로 달아나는가")]
    [SerializeField] private float   fleeSpeedScale = 2.5f;                     // [TBD]
    [Tooltip("바닥에서 달아나는 거리 (최소·최대, UI 단위)")]
    [SerializeField] private Vector2 fleeDistance   = new Vector2(300f, 400f);  // [TBD]
    [Tooltip("달아나며 앞뒤(발 높이)로 비껴가는 범위 — + 는 뒤쪽(멀리)")]
    [SerializeField] private Vector2 fleeDepthShift = new Vector2(-150f, 250f); // [TBD]
    [Tooltip("벽에서 더 위로 달아나는 높이 (최소·최대)")]
    [SerializeField] private Vector2 fleeClimbRise  = new Vector2(200f, 320f);  // [TBD]
    [Tooltip("멈춘 뒤 뒤돌아보는 시간 (최소·최대, 초)")]
    [SerializeField] private Vector2 fleeLookBack   = new Vector2(1f, 2f);      // [TBD]

    private const float ACCEL           = 260f;   // UI 단위/초²
    private const float DECEL           = 220f;
    private const float FLEE_MIN_TRAVEL = 80f;    // 바닥에서 이보다 짧게밖에 못 가면 (화면 끝) 반대쪽으로 달아난다
    private const float CLIMB_MIN_RISE  = 150f;   // 이보다 낮게밖에 못 오르면 벽을 타지 않는다
    private const float TURN_DOWN_SCALE = 1.4f;   // 위 → 아래로 돌아서는 시간 배율 (180°)

    private RectTransform _rt;
    private GeckoMotor    _motor;
    private GeckoRig      _rig;
    private Coroutine     _loop;

    private float _groundY;       // 원근 크기 기준 발 높이 (벽을 타는 동안은 출발한 높이로 고정)
    private float _angle;         // 게코 오브젝트 회전 (도). 0 = 바닥, ±90 = 벽
    private bool  _climbing;
    private float _climbBaseY;    // 벽을 타기 시작한 발 높이 — 여기로 내려온다
    private float _climbUp;       // 머리가 위로 향하는 각도 (+90 = 오른쪽을 보던 게코, -90 = 왼쪽)

    /// <summary>꼬리를 만져 달아나는 중 (뒤돌아보는 시간 포함)</summary>
    public bool IsFleeing { get; private set; }

    /// <summary>벽을 타는 중 (몸을 세우고 눕히는 시간 포함)</summary>
    public bool IsClimbing => _climbing;

    // ── 계산식 (자가 검사가 확인한다) ─────────────────────────

    /// <summary>발 높이에 따른 원근 크기 — 가까운 쪽 1, 먼 쪽 far, 범위 밖은 끝 값</summary>
    public static float DepthScaleAt(float footY, Vector2 band, float far)
        => Mathf.Lerp(1f, far, Mathf.InverseLerp(band.x, band.y, footY));

    /// <summary>벽을 탈 때 발(피벗)이 올라갈 수 있는 가장 높은 곳 — 몸이 위 여백 안에 들어오게</summary>
    public static float ClimbTopY(float areaHeight, float topMargin, float bodyReach)
        => areaHeight - topMargin - bodyReach;

    // ── 생명주기 ──────────────────────────────────────────────

    private void Awake()
    {
        _rt    = (RectTransform)transform;
        _motor = GetComponent<GeckoMotor>();
        _rig   = GetComponent<GeckoRig>();
        _groundY = _rt.anchoredPosition.y;
    }

    private void OnEnable()
    {
        ClampIntoBand();
        _groundY = _rt.anchoredPosition.y;
        _loop = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;
        IsFleeing = false;

        // 벽에 붙은 채 꺼지면 (부화 연출 등) 바닥으로 되돌려 둔다
        if (_climbing)
        {
            var p = _rt.anchoredPosition;
            p.y = _climbBaseY;
            _rt.anchoredPosition = p;
            _climbing = false;
        }
        SetAngle(0f);
        if (_motor != null)
        {
            _motor.SetWalking(false);
            _motor.SetClimbing(false);
        }
    }

    private void Update()
    {
        if (_rig == null) return;
        if (!_climbing) _groundY = _rt.anchoredPosition.y;
        _rig.DepthScale = DepthScaleAt(_groundY, groundBand, farScale);
    }

    // ── 도망 ─────────────────────────────────────────────────

    /// <summary>
    /// 꼬리를 만졌다(또는 연타에 삐졌다) — 바닥에서는 touchWorld(누른 곳) 반대쪽으로 빠르게,
    /// 벽에서는 더 위로 달아나거나 후다닥 내려온다. 꺼져 있거나 이미 달아나는 중이면 무시.
    /// </summary>
    public void Flee(Vector3 touchWorld)
    {
        if (!isActiveAndEnabled || IsFleeing || _rig == null) return;

        if (_loop != null) StopCoroutine(_loop);
        _motor.SetWalking(false);
        _loop = _climbing
            ? StartCoroutine(ClimbEscape())
            : StartCoroutine(FleeRoutine(touchWorld.x < _rt.position.x));   // 왼쪽을 만졌으면 오른쪽으로
    }

    private IEnumerator FleeRoutine(bool toRight)
    {
        IsFleeing = true;
        yield return RotateTo(0f, climbTurnTime);   // 혹시 기울어 있었다면 먼저 눕힌다

        GetWalkableX(out float minX, out float maxX);
        Vector2 start = _rt.anchoredPosition;
        float   dist  = Random.Range(fleeDistance.x, fleeDistance.y);

        float x = Mathf.Clamp(start.x + (toRight ? dist : -dist), minX, maxX);
        if (Mathf.Abs(x - start.x) < FLEE_MIN_TRAVEL)   // 화면 끝에 몰렸으면 반대쪽으로
        {
            toRight = !toRight;
            x = Mathf.Clamp(start.x + (toRight ? dist : -dist), minX, maxX);
        }
        float y = Mathf.Clamp(start.y + Random.Range(fleeDepthShift.x, fleeDepthShift.y), groundBand.x, groundBand.y);

        yield return WalkTo(new Vector2(x, y), fleeSpeedScale, mayPause: false);

        // 뒤를 한 번 돌아본다 — 따라오나?
        _rig.SetFacing(!toRight);
        yield return Pause(Random.Range(fleeLookBack.x, fleeLookBack.y));

        IsFleeing = false;
        _loop = StartCoroutine(Loop());
    }

    private IEnumerator ClimbEscape()
    {
        IsFleeing = true;
        yield return RotateTo(_climbUp, climbTurnTime * 0.5f);   // 내려오던 중이었으면 다시 머리를 위로

        float y    = _rt.anchoredPosition.y;
        float top  = ClimbTop();
        float rise = Random.Range(fleeClimbRise.x, fleeClimbRise.y);
        if (top - y >= rise * 0.6f)
        {
            // 위로 더 달아날 자리가 있다
            yield return ClimbTo(Mathf.Min(top, y + rise), fleeSpeedScale);
            yield return Pause(Random.Range(fleeLookBack.x, fleeLookBack.y));
            yield return ClimbDown(climbSpeedScale);
        }
        else
        {
            // 이미 꼭대기 — 후다닥 내려간다
            yield return ClimbDown(fleeSpeedScale);
        }

        IsFleeing = false;
        _loop = StartCoroutine(Loop());
    }

    // ── 행동 루프 ────────────────────────────────────────────

    private IEnumerator Loop()
    {
        yield return null;   // 첫 프레임에는 레이아웃 크기가 확정되지 않았을 수 있다

        while (true)
        {
            float wait = Random.Range(waitTimeMin, waitTimeMax);
            while (wait > 0f)
            {
                if (!_motor.IsBusy) wait -= Time.deltaTime;
                yield return null;
            }

            if (_motor.Mood == GeckoMood.Sleepy) continue;   // 졸리면 제자리

            if (Random.value < climbChance)
            {
                yield return Climb();
                continue;
            }

            yield return WalkTo(PickTarget());
            if (Random.value < 0.3f) _motor.TryPlayIdle(GeckoAction.Tongue_Lick);
        }
    }

    // ── 벽 타기 ──────────────────────────────────────────────

    private IEnumerator Climb()
    {
        float baseY  = _rt.anchoredPosition.y;
        float target = Mathf.Min(ClimbTop(), baseY + Random.Range(climbHeight.x, climbHeight.y));
        if (target - baseY < CLIMB_MIN_RISE) yield break;   // 오를 자리가 없다

        _climbing   = true;
        _climbBaseY = baseY;
        _climbUp    = _rig.FacingRight ? 90f : -90f;
        _motor.SetClimbing(true);

        yield return RotateTo(_climbUp, climbTurnTime);   // 몸을 세운다 (머리가 위로)
        yield return ClimbTo(target, climbSpeedScale);

        // 매달려서 잠깐 쉰다 — 가끔 혀를 날름
        if (Random.value < 0.5f) _motor.TryPlayIdle(GeckoAction.Tongue_Lick);
        yield return Pause(Random.Range(climbHang.x, climbHang.y));

        yield return ClimbDown(climbSpeedScale);
    }

    // 머리를 아래로 돌려 출발한 높이까지 내려오고, 바닥에서 몸을 눕힌다
    private IEnumerator ClimbDown(float speedScale)
    {
        yield return RotateTo(-_climbUp, climbTurnTime * TURN_DOWN_SCALE);
        yield return ClimbTo(_climbBaseY, speedScale);
        yield return RotateTo(0f, climbTurnTime);
        _climbing = false;
        _motor.SetClimbing(false);
    }

    private IEnumerator ClimbTo(float targetY, float speedScale)
    {
        var p = _rt.anchoredPosition;
        yield return Travel(new Vector2(p.x, targetY), speedScale, 2f);
    }

    // 몸 전체가 위 여백 안에 들어오는 발 높이 — 머리를 아래로 돌리면 꼬리가 위로 가므로 긴 쪽 기준
    private float ClimbTop()
    {
        _rig.GetExtents(out float left, out float right);
        return ClimbTopY(ParentHeight(), climbTopMargin, Mathf.Max(left, right));
    }

    // angle까지 부드럽게 돈다 (+90 → -90 은 0을 지나 몸을 뒤집는다)
    private IEnumerator RotateTo(float angle, float time)
    {
        float from = _angle;
        if (Mathf.Abs(angle - from) < 0.5f)
        {
            SetAngle(angle);
            yield break;
        }

        _motor.SetWalking(false);
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float u = Mathf.Clamp01(t / time);
            SetAngle(Mathf.Lerp(from, angle, u * u * (3f - 2f * u)));
            yield return null;
        }
        SetAngle(angle);
    }

    private void SetAngle(float angle)
    {
        _angle = angle;
        if (_rt != null) _rt.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ── 걷기 ─────────────────────────────────────────────────

    /// <summary>바닥 걷기 — 가는 쪽으로 돌아선 뒤 이동. speedScale = 평소 걸음의 배수, mayPause = 도중에 가끔 멈춰 둘러보기</summary>
    private IEnumerator WalkTo(Vector2 target, float speedScale = 1f, bool mayPause = true)
    {
        Vector2 start = _rt.anchoredPosition;
        if (Vector2.Distance(start, target) < 4f) yield break;

        // 돌아서기 (위아래로만 가는 경우는 방향 그대로)
        if (Mathf.Abs(target.x - start.x) > 2f)
        {
            bool right = target.x > start.x;
            if (right != _rig.FacingRight)
            {
                _rig.SetFacing(right);
                float limit = 0.5f;   // 그림이 없어 회전이 진행되지 않는 경우 대비
                while (_rig.IsTurning && limit > 0f)
                {
                    limit -= Time.deltaTime;
                    yield return null;
                }
            }
        }

        float pauseAt = mayPause && Random.value < pauseMidwayChance ? Random.Range(0.35f, 0.65f) : 2f;
        yield return Travel(target, speedScale, pauseAt);
    }

    /// <summary>
    /// target까지 가속·감속하며 이동 (바닥·벽 공용). pauseAt = 전체 거리 중 이 비율에서 한 번 멈춰 둘러본다 (1 넘으면 안 멈춤).
    /// 걸음은 AddTravel이 이동 거리에 맞추므로 빨리 가면 발도 빨라진다.
    /// </summary>
    private IEnumerator Travel(Vector2 target, float speedScale, float pauseAt)
    {
        float total = Vector2.Distance(_rt.anchoredPosition, target);
        if (total < 1f) yield break;

        bool  paused = false;
        float speed  = 0f;

        while (true)
        {
            // 먹이·쓰다듬기 등 동작 중이면 멈춰서 기다린다
            if (_motor.IsBusy)
            {
                _motor.SetWalking(false);
                speed = 0f;
                yield return null;
                continue;
            }

            Vector2 pos = _rt.anchoredPosition;
            Vector2 to  = target - pos;
            float dist  = to.magnitude;
            if (dist < 1f) break;

            float maxSpeed = moveSpeed * speedScale * Mathf.Lerp(0.7f, 1f, _rig.StageScale) * _rig.DepthScale;
            float desired  = Mathf.Min(maxSpeed, Mathf.Sqrt(2f * DECEL * speedScale * dist));
            speed = Mathf.MoveTowards(speed, desired, ACCEL * speedScale * Time.deltaTime);

            float step = Mathf.Min(dist, speed * Time.deltaTime);
            _rt.anchoredPosition = pos + to / dist * step;
            _motor.SetWalking(speed > 5f);
            _motor.AddTravel(step);

            if (!paused && 1f - dist / total >= pauseAt)
            {
                paused = true;
                _motor.SetWalking(false);
                yield return Pause(Random.Range(0.8f, 1.6f));
                speed = 0f;
            }

            yield return null;
        }

        _motor.SetWalking(false);
    }

    private static IEnumerator Pause(float seconds)
    {
        while (seconds > 0f)
        {
            seconds -= Time.deltaTime;
            yield return null;
        }
    }

    // ── 범위 ─────────────────────────────────────────────────

    private Vector2 PickTarget()
    {
        GetWalkableX(out float minX, out float maxX);
        float x = minX < maxX ? Random.Range(minX, maxX) : 0f;
        float y = Random.Range(groundBand.x, groundBand.y);   // 앞뒤로도 고른다 (원근)
        return new Vector2(x, y);
    }

    // 목표 방향으로 돌아선 뒤의 폭까지 고려해 양쪽 모두 화면 안에 들어가는 발 위치 범위
    private void GetWalkableX(out float minX, out float maxX)
    {
        float halfW = ParentSize().x * 0.5f;
        _rig.GetExtents(out float left, out float right);
        float reach = Mathf.Max(left, right);
        minX = -halfW + sideMargin + reach;
        maxX =  halfW - sideMargin - reach;
        if (minX > maxX) minX = maxX = 0f;
    }

    private float ParentHeight() => ParentSize().y;

    private Vector2 ParentSize()
    {
        var parent = _rt.parent as RectTransform;
        return parent != null ? parent.rect.size : new Vector2(1080f, 2400f);
    }

    private void ClampIntoBand()
    {
        if (_rt == null) return;
        var p = _rt.anchoredPosition;
        p.y = Mathf.Clamp(p.y, groundBand.x, groundBand.y);
        _rt.anchoredPosition = p;
    }
}
