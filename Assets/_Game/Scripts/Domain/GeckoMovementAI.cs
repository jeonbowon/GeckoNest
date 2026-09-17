using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게코가 테라리움을 돌아다니는 AI (UI 좌표).
///
/// - 바닥: 발 높이 groundBand(가까운 쪽 ~ 먼 쪽) 안에서 앞뒤·좌우·대각선으로 다닌다.
///   발 높이가 높을수록(멀리 있을수록) 작고 느리게 그려 원근감을 준다 (바닥 장식도 DepthScaleFor로 같은 원근)
/// - 구조물(꾸미기 장식, TerrariumLayout — 옮긴 위치 기준): 은신처에 들어가 쉬고, 코르크 뒤판·덩굴을 오르내리고,
///   나뭇가지를 타고 올라가 위쪽 가로 부분에서 엎드려 쉰다
/// - 빈 유리벽: 뒷벽 구조물이 없으면 가끔 제자리에서 벽을 탄다
/// - 벽·구조물을 타는 동안 게코 오브젝트를 발밑 기준으로 돌려 가는 방향에 머리를 맞추고(경로 따라가기),
///   크기는 출발한 바닥 높이 기준으로 고정한다
/// - 먹이·쓰다듬기 등 동작 중이면 멈춰서 기다린다
/// - Flee(꼬리·연타) — 바닥에서는 누른 곳 반대쪽으로, 벽에서는 더 위로 달아나거나 후다닥 내려오고, 집에서는 튀어나온다
///
/// 피벗이 발밑이므로 회전도 발밑을 중심으로 한다 (GeckoTouch가 같은 각도로 따라 돈다).
/// 꾸미기 편집 모드에서는 홈 화면이 이 컴포넌트를 끈다 — 꺼질 때 벽·집에 있었으면 바닥으로 되돌린다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GeckoMotor))]
public class GeckoMovementAI : MonoBehaviour
{
    /// <summary>게코가 쓸 수 있는 장식 한 칸 (HomeUIController가 꾸미기 상태로 채운다)</summary>
    public struct Structure
    {
        public int      slot;
        public DecorUse use;
        public Vector2  anchor;   // TerrariumLayout.AnchorOf — 은신처 안 발 위치 / 벽 구조물 밑동
    }

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

    [Header("구조물 (꾸미기 장식)")]
    [Tooltip("쉬고 난 뒤 걷는 대신 구조물로 갈 확률 (구조물이 있을 때)")]
    [SerializeField, Range(0f, 1f)] private float structureChance = 0.45f;   // [TBD]
    [Tooltip("은신처에 머무는 시간 (최소·최대, 초)")]
    [SerializeField] private Vector2 hideStay       = new Vector2(5f, 12f);  // [TBD]
    [Tooltip("졸릴 때 은신처에 머무는 시간")]
    [SerializeField] private Vector2 hideStaySleepy = new Vector2(12f, 25f); // [TBD]
    [Tooltip("은신처 문 앞 — 집 가운데에서 화면 가운데 쪽으로 이만큼")]
    [SerializeField] private float   hideDoorOffset = 300f;                  // [TBD]
    [Tooltip("나뭇가지 위에서 엎드려 쉬는 시간")]
    [SerializeField] private Vector2 perchRest      = new Vector2(3f, 6f);   // [TBD]

    [Header("빈 유리벽 타기 (뒷벽 구조물이 없을 때)")]
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
    [Tooltip("90° 도는 데 걸리는 시간 (초). 더 많이/적게 돌면 비례")]
    [SerializeField] private float   climbTurnTime   = 0.45f;                  // [TBD]

    [Header("도망 (꼬리를 만졌을 때)")]
    [Tooltip("평소 걸음의 몇 배로 달아나는가")]
    [SerializeField] private float   fleeSpeedScale = 2.5f;                     // [TBD]
    [Tooltip("바닥에서 달아나는 거리 (최소·최대, UI 단위)")]
    [SerializeField] private Vector2 fleeDistance   = new Vector2(300f, 400f);  // [TBD]
    [Tooltip("달아나며 앞뒤(발 높이)로 비껴가는 범위 — + 는 뒤쪽(멀리)")]
    [SerializeField] private Vector2 fleeDepthShift = new Vector2(-150f, 250f); // [TBD]
    [Tooltip("빈 벽에서 더 위로 달아나는 높이 (최소·최대)")]
    [SerializeField] private Vector2 fleeClimbRise  = new Vector2(200f, 320f);  // [TBD]
    [Tooltip("멈춘 뒤 뒤돌아보는 시간 (최소·최대, 초)")]
    [SerializeField] private Vector2 fleeLookBack   = new Vector2(1f, 2f);      // [TBD]

    private const float ACCEL           = 260f;   // UI 단위/초²
    private const float DECEL           = 220f;
    private const float FLEE_MIN_TRAVEL = 80f;    // 바닥에서 이보다 짧게밖에 못 가면 (화면 끝) 반대쪽으로 달아난다
    private const float CLIMB_MIN_RISE  = 150f;   // 빈 벽에서 이보다 낮게밖에 못 오르면 타지 않는다
    private const float TILT_CLIMBING   = 20f;    // 이보다 기울면 벽 타는 자세 (다리 벌림·그림자 숨김)

    private RectTransform _rt;
    private GeckoMotor    _motor;
    private GeckoRig      _rig;
    private Coroutine     _loop;

    private float   _groundY;        // 원근 크기 기준 발 높이 (벽·구조물을 타는 동안은 출발한 높이로 고정)
    private float   _angle;          // 게코 오브젝트 회전 (도, -180~180). 0 = 바닥
    private bool    _climbing;       // 벽·구조물 위 (바닥에서 떠난 뒤 다시 눕힐 때까지)
    private bool    _freeClimb;      // 빈 유리벽을 타는 중 (구조물 아님)
    private Vector2 _hideAnchor;     // 들어가 있는 은신처의 안쪽 발 위치
    private readonly List<Vector2>   _route      = new List<Vector2>();     // 올라온 길 (밑동부터) — 내려갈 때 거꾸로
    private readonly List<Structure> _structures = new List<Structure>();

    /// <summary>꼬리를 만져 달아나는 중 (뒤돌아보는 시간 포함)</summary>
    public bool IsFleeing { get; private set; }

    /// <summary>벽·구조물 위에 있다 (몸을 세우고 눕히는 시간 포함)</summary>
    public bool IsClimbing => _climbing;

    /// <summary>들어가 있는 은신처 칸 (-1 = 밖)</summary>
    public int HiddenSlot { get; private set; } = -1;

    /// <summary>은신처에 들어가거나 나왔다 — (칸, 들어감). 홈 화면이 집 그림을 게코 앞/뒤로 옮긴다</summary>
    public event Action<int, bool> HideChanged;

    /// <summary>나뭇가지 위에 올라가 엎드렸다 (말풍선용)</summary>
    public event Action Perched;

    // ── 계산식 (자가 검사가 확인한다) ─────────────────────────

    /// <summary>발 높이에 따른 원근 크기 — 가까운 쪽 1, 먼 쪽 far, 범위 밖은 끝 값</summary>
    public static float DepthScaleAt(float footY, Vector2 band, float far)
        => Mathf.Lerp(1f, far, Mathf.InverseLerp(band.x, band.y, footY));

    /// <summary>이 게코의 원근 설정으로 본 크기 — 바닥 장식도 같은 원근으로 그린다</summary>
    public float DepthScaleFor(float footY) => DepthScaleAt(footY, groundBand, farScale);

    /// <summary>벽을 탈 때 발(피벗)이 올라갈 수 있는 가장 높은 곳 — 몸이 위 여백 안에 들어오게</summary>
    public static float ClimbTopY(float areaHeight, float topMargin, float bodyReach)
        => areaHeight - topMargin - bodyReach;

    /// <summary>
    /// dir 방향으로 갈 때 게코 오브젝트 회전각 (-180~180) — 머리가 가는 방향을 향한다.
    /// 오른쪽을 보는 게코는 머리가 +x, 왼쪽을 보는 게코는 -x (그림 좌우 반전) 이므로 180° 차이
    /// </summary>
    public static float SegmentAngle(Vector2 dir, bool facingRight)
    {
        float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (!facingRight) a -= 180f;
        return Mathf.DeltaAngle(0f, a);
    }

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
        IsHeld    = false;

        // 벽·집에 있던 채 꺼지면 (부화 연출·꾸미기 편집·씬 이동) 바닥으로 되돌려 둔다
        var p = _rt.anchoredPosition;
        if (_climbing && _route.Count > 0) p = _route[0];
        if (HiddenSlot >= 0) p = TerrariumLayout.HideDoor(_hideAnchor, hideDoorOffset);
        _rt.anchoredPosition = ClampToBand(p);

        _route.Clear();
        _climbing = _freeClimb = false;
        SetHidden(-1);
        SetAngle(0f);
        if (_motor != null)
        {
            _motor.SetWalking(false);
            _motor.SetClimbing(false);
            _motor.SetResting(false);
        }
    }

    private void Update()
    {
        if (_rig == null) return;
        if (!_climbing && !IsHeld) _groundY = _rt.anchoredPosition.y;   // 손바닥에 들려 있으면 크기는 그대로
        _rig.DepthScale = DepthScaleFor(_groundY);
    }

    // ── 구조물 ────────────────────────────────────────────────

    /// <summary>게코가 쓸 수 있는 장식 칸 (꾸미기 상태가 바뀔 때마다 홈 화면이 넘긴다)</summary>
    public void SetStructures(IList<Structure> list)
    {
        _structures.Clear();
        if (list != null) _structures.AddRange(list);
    }

    private bool TryFindStructure(DecorUse use, out Structure found)
    {
        foreach (var s in _structures)
        {
            if (s.use != use) continue;
            found = s;
            return true;
        }
        found = default;
        return false;
    }

    private bool HasWallStructure()
    {
        foreach (var s in _structures)
            if (s.use != DecorUse.Hide && s.use != DecorUse.None) return true;
        return false;
    }

    // ── 외부 요청 ─────────────────────────────────────────────

    /// <summary>
    /// 꼬리를 만졌다(또는 연타에 삐졌다) — 바닥에서는 touchWorld(누른 곳) 반대쪽으로 빠르게,
    /// 벽에서는 더 위로 달아나거나 후다닥 내려오고, 집에서는 튀어나온다. 꺼져 있거나 이미 달아나는 중이면 무시.
    /// </summary>
    public void Flee(Vector3 touchWorld)
    {
        if (!isActiveAndEnabled || IsFleeing || IsHeld || _rig == null) return;
        StopLoop();

        IEnumerator body;
        if (HiddenSlot >= 0)  body = LeaveHide(fleeSpeedScale);
        else if (_climbing)   body = ClimbEscape();
        else                  body = FleeOnGround(touchWorld.x < _rt.position.x);   // 왼쪽을 만졌으면 오른쪽으로
        _loop = StartCoroutine(Escape(body));
    }

    /// <summary>은신처에서 나오게 한다 (집을 누름 · 돌봄 버튼). 집에 없으면 무시</summary>
    public void ComeOut()
    {
        if (!isActiveAndEnabled || HiddenSlot < 0 || IsFleeing || IsHeld) return;
        StopLoop();
        _loop = StartCoroutine(ThenLoop(LeaveHide(1f)));
    }

    // ── 유대 (부르기 · 손바닥) ────────────────────────────────

    [Header("유대 — 부르기")]
    [Tooltip("불렀을 때 평소 걸음의 몇 배로 오는가")]
    [SerializeField] private float comeSpeedScale = 1.6f;   // [TBD]

    /// <summary>불러서 도착했다 (홈 화면이 올려다보기·말풍선)</summary>
    public event Action Arrived;

    /// <summary>게코가 다니는 바닥의 가장 먼 발 높이 — 빈 바닥 누르기 영역 높이</summary>
    public float GroundTop => groundBand.y;

    /// <summary>손바닥에 올라가 있어 스스로 움직이지 않는다 (Hold ~ Release)</summary>
    public bool IsHeld { get; private set; }

    /// <summary>
    /// 부르기 — point(바닥 좌표)로 다가온다. 집에 있으면 나오고, 벽에 있으면 내려온 뒤.
    /// 꺼져 있거나 달아나는 중·손바닥 위면 무시. 받아들였으면 true
    /// </summary>
    public bool CallTo(Vector2 point)
    {
        if (!isActiveAndEnabled || IsFleeing || IsHeld || _rig == null) return false;
        StopLoop();
        _loop = StartCoroutine(ThenLoop(ComeTo(point)));
        return true;
    }

    private IEnumerator ComeTo(Vector2 point)
    {
        if (HiddenSlot >= 0)  yield return LeaveHide(comeSpeedScale);
        else if (_climbing)   yield return Descend(comeSpeedScale);
        else                  yield return RotateTo(0f);

        GetWalkableX(out float minX, out float maxX);
        var target = new Vector2(Mathf.Clamp(point.x, minX, maxX), Mathf.Clamp(point.y, groundBand.x, groundBand.y));
        yield return WalkTo(target, comeSpeedScale, mayPause: false);
        Arrived?.Invoke();
        yield return Pause(1.5f);   // 올려다보는 동안은 다시 걷지 않는다
    }

    /// <summary>
    /// 손바닥 — 바닥에 서 있을 때만 붙잡는다 (벽·집·도망 중이면 false). 붙잡힌 동안은 홈 화면이 위치를 옮긴다
    /// </summary>
    public bool Hold()
    {
        if (!isActiveAndEnabled || IsFleeing || IsHeld || _climbing || HiddenSlot >= 0 || _rig == null) return false;
        StopLoop();
        SetAngle(0f);
        IsHeld = true;
        return true;
    }

    public void Release()
    {
        if (!IsHeld) return;
        IsHeld = false;
        ClampIntoBand();
        if (isActiveAndEnabled && _loop == null) _loop = StartCoroutine(Loop());
    }

    private void StopLoop()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;
        _motor.SetWalking(false);
        _motor.SetResting(false);
    }

    private IEnumerator Escape(IEnumerator body)
    {
        IsFleeing = true;
        yield return body;
        IsFleeing = false;
        _loop = StartCoroutine(Loop());
    }

    private IEnumerator ThenLoop(IEnumerator body)
    {
        yield return body;
        _loop = StartCoroutine(Loop());
    }

    // ── 행동 루프 ────────────────────────────────────────────

    private IEnumerator Loop()
    {
        yield return null;   // 첫 프레임에는 레이아웃 크기가 확정되지 않았을 수 있다

        while (true)
        {
            float wait = UnityEngine.Random.Range(waitTimeMin, waitTimeMax);
            while (wait > 0f)
            {
                if (!_motor.IsBusy) wait -= Time.deltaTime;
                yield return null;
            }

            // 졸리면 제자리 — 은신처가 있으면 들어가 잔다
            if (_motor.Mood == GeckoMood.Sleepy)
            {
                if (TryFindStructure(DecorUse.Hide, out var bed) && UnityEngine.Random.value < 0.6f)
                    yield return GoHide(bed, sleepy: true);
                continue;
            }

            if (_structures.Count > 0 && UnityEngine.Random.value < structureChance)
            {
                var s = _structures[UnityEngine.Random.Range(0, _structures.Count)];
                if (s.use == DecorUse.Hide)
                {
                    yield return GoHide(s, sleepy: false);
                }
                else
                {
                    var path = TerrariumLayout.ClimbPath(s.use, s.anchor, ClimbTop(), UnityEngine.Random.Range(0.5f, 1f));
                    yield return ClimbRoute(path, free: false, perch: s.use == DecorUse.Branch);
                }
                continue;
            }

            if (!HasWallStructure() && UnityEngine.Random.value < climbChance)
            {
                yield return FreeClimb();
                continue;
            }

            yield return WalkTo(PickTarget());
            if (UnityEngine.Random.value < 0.3f) _motor.TryPlayIdle(GeckoAction.Tongue_Lick);
        }
    }

    // ── 은신처 ────────────────────────────────────────────────

    private IEnumerator GoHide(Structure house, bool sleepy)
    {
        Vector2 inside = ClampToBand(house.anchor);
        Vector2 door   = ClampToBand(TerrariumLayout.HideDoor(inside, hideDoorOffset));

        yield return WalkTo(door);
        yield return WalkTo(inside, 0.6f, mayPause: false);   // 집 쪽을 보고 천천히 들어간다
        _hideAnchor = inside;
        SetHidden(house.slot);                                // 집 그림이 게코 앞으로 — 꼬리만 삐죽
        if (sleepy) _motor.SetResting(true);

        yield return Pause(Rand(sleepy ? hideStaySleepy : hideStay));
        yield return LeaveHide(1f);
    }

    // 돌아서서 문 앞으로 나온다. 돌아서면 집 그림을 다시 게코 뒤로
    private IEnumerator LeaveHide(float speedScale)
    {
        _motor.SetResting(false);
        if (HiddenSlot < 0) yield break;

        Vector2 door = ClampToBand(TerrariumLayout.HideDoor(_hideAnchor, hideDoorOffset));
        yield return Face(door.x > _rt.anchoredPosition.x);
        SetHidden(-1);
        yield return Travel(door, speedScale, 2f);
    }

    private void SetHidden(int slot)
    {
        if (HiddenSlot == slot) return;
        int prev = HiddenSlot;
        HiddenSlot = slot;
        if (prev >= 0) HideChanged?.Invoke(prev, false);
        if (slot >= 0) HideChanged?.Invoke(slot, true);
    }

    // ── 벽 · 구조물 타기 ─────────────────────────────────────

    // 빈 유리벽 — 지금 자리에서 곧장 위로
    private IEnumerator FreeClimb()
    {
        Vector2 p = _rt.anchoredPosition;
        float target = Mathf.Min(ClimbTop(), p.y + Rand(climbHeight));
        if (target - p.y < CLIMB_MIN_RISE) yield break;   // 오를 자리가 없다
        yield return ClimbRoute(new[] { p, new Vector2(p.x, target) }, free: true, perch: false);
    }

    /// <summary>
    /// path[0](바닥)까지 걸어간 뒤 점들을 차례로 따라 오르고, 꼭대기에서 쉬었다가 온 길을 되짚어 내려와 눕는다.
    /// perch = 꼭대기에서 엎드려 쉰다 (나뭇가지)
    /// </summary>
    private IEnumerator ClimbRoute(Vector2[] path, bool free, bool perch)
    {
        if (path == null || path.Length < 2) yield break;

        yield return WalkTo(ClampToBand(path[0]));
        _climbing  = true;          // 이제부터 원근 크기는 이 바닥 높이 기준으로 고정 (Update)
        _freeClimb = free;
        _route.Clear();
        _route.Add(_rt.anchoredPosition);

        for (int i = 1; i < path.Length; i++)
        {
            yield return Segment(path[i], climbSpeedScale);
            _route.Add(path[i]);
        }

        if (perch)
        {
            _motor.SetResting(true);
            Perched?.Invoke();
            yield return Pause(Rand(perchRest));
            _motor.SetResting(false);
        }
        else
        {
            if (UnityEngine.Random.value < 0.5f) _motor.TryPlayIdle(GeckoAction.Tongue_Lick);
            yield return Pause(Rand(climbHang));
        }

        yield return Descend(climbSpeedScale);
    }

    // 올라온 길을 거꾸로 내려와 바닥에서 몸을 눕힌다
    private IEnumerator Descend(float speedScale)
    {
        _motor.SetResting(false);
        for (int i = _route.Count - 1; i >= 0; i--)
            yield return Segment(_route[i], speedScale);
        yield return RotateTo(0f);

        _route.Clear();
        _climbing = _freeClimb = false;
        _motor.SetClimbing(false);
    }

    // 벽·구조물에서 놀랐다 — 빈 벽이면 위로 더 달아날 수 있고, 아니면 후다닥 내려간다
    private IEnumerator ClimbEscape()
    {
        if (_freeClimb)
        {
            Vector2 p = _rt.anchoredPosition;
            float rise = Rand(fleeClimbRise);
            float top  = ClimbTop();
            if (top - p.y >= rise * 0.6f)
            {
                var up = new Vector2(p.x, Mathf.Min(top, p.y + rise));
                yield return Segment(up, fleeSpeedScale);
                _route.Add(up);
                yield return Pause(Rand(fleeLookBack));
                yield return Descend(climbSpeedScale);
                yield break;
            }
        }
        yield return Descend(fleeSpeedScale);
    }

    /// <summary>지금 자리에서 target까지 한 구간 — 가는 쪽으로 돌아서고 몸을 기울인 뒤 이동</summary>
    private IEnumerator Segment(Vector2 target, float speedScale)
    {
        Vector2 dir = target - _rt.anchoredPosition;
        if (dir.magnitude < 2f) yield break;

        bool right = Mathf.Abs(dir.x) < 1f ? _rig.FacingRight : dir.x > 0f;   // 위아래로만 가면 방향 유지
        yield return Face(right);

        float angle = SegmentAngle(dir, right);
        _motor.SetClimbing(Mathf.Abs(angle) > TILT_CLIMBING);
        yield return RotateTo(angle);
        yield return Travel(target, speedScale, 2f);
    }

    // 몸 전체가 위 여백 안에 들어오는 발 높이 — 머리를 아래로 돌리면 꼬리가 위로 가므로 긴 쪽 기준
    private float ClimbTop()
    {
        _rig.GetExtents(out float left, out float right);
        return ClimbTopY(ParentHeight(), climbTopMargin, Mathf.Max(left, right));
    }

    // angle까지 부드럽게 돈다 — 도는 양에 비례한 시간 (+90 → -90 은 0을 지나 몸을 뒤집는다)
    private IEnumerator RotateTo(float angle)
    {
        float from  = _angle;
        float delta = Mathf.Abs(angle - from);
        if (delta < 0.5f)
        {
            SetAngle(angle);
            yield break;
        }

        _motor.SetWalking(false);
        float time = climbTurnTime * Mathf.Clamp(delta / 90f, 0.4f, 2f);
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

    // 오른쪽/왼쪽을 보게 돌아선다 (그림 좌우 반전)
    private IEnumerator Face(bool right)
    {
        if (right == _rig.FacingRight) yield break;
        _rig.SetFacing(right);
        _motor.SetWalking(false);
        float limit = 0.5f;   // 그림이 없어 회전이 진행되지 않는 경우 대비
        while (_rig.IsTurning && limit > 0f)
        {
            limit -= Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>바닥 걷기 — 가는 쪽으로 돌아선 뒤 이동. speedScale = 평소 걸음의 배수, mayPause = 도중에 가끔 멈춰 둘러보기</summary>
    private IEnumerator WalkTo(Vector2 target, float speedScale = 1f, bool mayPause = true)
    {
        Vector2 start = _rt.anchoredPosition;
        if (Vector2.Distance(start, target) < 4f) yield break;

        if (Mathf.Abs(target.x - start.x) > 2f)   // 앞뒤로만 가면 방향 그대로
            yield return Face(target.x > start.x);

        float pauseAt = mayPause && UnityEngine.Random.value < pauseMidwayChance ? UnityEngine.Random.Range(0.35f, 0.65f) : 2f;
        yield return Travel(target, speedScale, pauseAt);
    }

    private IEnumerator FleeOnGround(bool toRight)
    {
        yield return RotateTo(0f);   // 혹시 기울어 있었다면 먼저 눕힌다

        GetWalkableX(out float minX, out float maxX);
        Vector2 start = _rt.anchoredPosition;
        float   dist  = Rand(fleeDistance);

        float x = Mathf.Clamp(start.x + (toRight ? dist : -dist), minX, maxX);
        if (Mathf.Abs(x - start.x) < FLEE_MIN_TRAVEL)   // 화면 끝에 몰렸으면 반대쪽으로
        {
            toRight = !toRight;
            x = Mathf.Clamp(start.x + (toRight ? dist : -dist), minX, maxX);
        }
        float y = Mathf.Clamp(start.y + Rand(fleeDepthShift), groundBand.x, groundBand.y);

        yield return WalkTo(new Vector2(x, y), fleeSpeedScale, mayPause: false);

        yield return Face(!toRight);   // 뒤를 한 번 돌아본다 — 따라오나?
        yield return Pause(Rand(fleeLookBack));
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
                yield return Pause(UnityEngine.Random.Range(0.8f, 1.6f));
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

    private static float Rand(Vector2 range) => UnityEngine.Random.Range(range.x, range.y);

    // ── 범위 ─────────────────────────────────────────────────

    private Vector2 PickTarget()
    {
        GetWalkableX(out float minX, out float maxX);
        float x = minX < maxX ? UnityEngine.Random.Range(minX, maxX) : 0f;
        float y = UnityEngine.Random.Range(groundBand.x, groundBand.y);   // 앞뒤로도 고른다 (원근)
        return new Vector2(x, y);
    }

    private Vector2 ClampToBand(Vector2 p) => new Vector2(p.x, Mathf.Clamp(p.y, groundBand.x, groundBand.y));

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
        _rt.anchoredPosition = ClampToBand(_rt.anchoredPosition);
    }
}
