using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        public int       slot;
        public DecorUse  use;
        public Vector2   anchor;   // TerrariumLayout.AnchorOf — 은신처 안 발 위치 / 벽 구조물 밑동 / 바닥 장식이 바닥에 닿는 곳
        public DecorPerk perk;     // 찾아가서 하는 일 (바닥 장식 — 비비기·핥기·몸 데우기)
        public Vector2   size;     // 그림 크기 (원근 전) — 바닥 장식 옆에 설 자리
        public Rect      door;     // 은신처 문 (영역 좌표, 원근 반영). 폭 0 = 문 정보 없음 → 은신처 뒤로 숨는다
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
    [Tooltip("공기 원근 — 가장 먼 쪽에서 그림에 곱하는 색 (차갑고 살짝 어둡게). 흰색이면 끔")]
    [SerializeField] private Color farTint = new Color(0.90f, 0.94f, 1.00f); // [TBD]

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
    [Tooltip("좌우 유리벽 앞 — 걸을 수 있는 끝에서 이만큼 안쪽에서 오른다 (가운데서 오르면 허공에 붙은 것처럼 보인다)")]
    [SerializeField] private float   wallMargin     = 120f;                    // [TBD]
    [Tooltip("바닥에 내려서는 마지막 구간의 속도 배수")]
    [SerializeField, Range(0.3f, 1f)] private float climbLandSlow = 0.7f;      // [TBD]
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
    private const float ROUTE_REACHED   = 4f;     // 경로의 점에 이만큼 가까우면 지나온 것으로 본다
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

    /// <summary>발 높이의 거리감 (0 = 가장 앞, 1 = 가장 뒤)</summary>
    public float DepthAt(float footY) => Mathf.InverseLerp(groundBand.x, groundBand.y, footY);

    /// <summary>공기 원근 색 — 바닥 장식도 같은 값을 쓴다 (HomeUIController.PlaceDecorImage)</summary>
    public Color DepthTintFor(float footY) => Color.Lerp(Color.white, farTint, DepthAt(footY));

    /// <summary>벽을 탈 때 발(피벗)이 올라갈 수 있는 가장 높은 곳 — 몸이 위 여백 안에 들어오게</summary>
    public static float ClimbTopY(float areaHeight, float topMargin, float bodyReach)
        => areaHeight - topMargin - bodyReach;

    /// <summary>
    /// dir 방향으로 갈 때 게코 오브젝트 회전각 (-180~180) — 머리가 가는 방향을 향한다.
    /// 오른쪽을 보는 게코는 머리가 +x, 왼쪽을 보는 게코는 -x (그림 좌우 반전) 이므로 180° 차이
    /// </summary>
    /// <summary>
    /// 이 자세에서 발이 향하는 방향 — 그림의 발은 늘 아래(-y)이고 좌우 반전은 머리 방향만 바꾼다.
    /// 왼쪽 벽에 붙으려면 발이 -x(각도 -90), 오른쪽 벽이면 +x(각도 +90)를 향해야 한다.
    /// </summary>
    public static Vector2 FootDirection(float angle)
        => (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector3.down);

    /// <summary>벽을 탈 때 붙을 벽 — 지금 자리에서 가까운 쪽 (같으면 왼쪽)</summary>
    public static bool ClimbWallIsLeft(float x, float minX, float maxX)
        => Mathf.Abs(x - minX) <= Mathf.Abs(x - maxX);

    /// <summary>벽 앞에서 오를 발 위치 — 걸을 수 있는 끝에서 margin 안쪽</summary>
    public static float WallClimbX(bool leftWall, float minX, float maxX, float margin)
    {
        if (minX >= maxX) return 0f;
        return leftWall ? Mathf.Min(minX + margin, maxX) : Mathf.Max(maxX - margin, minX);
    }

    /// <summary>그 벽에 붙는 몸 각도 — 발이 벽을 향한다 (오르내릴 때 같은 값, 머리 방향만 바뀐다)</summary>
    public static float WallAngle(bool leftWall) => leftWall ? -90f : 90f;

    /// <summary>
    /// 내려가기 전 정리 — 지금 발 높이보다 위에 있는 점은 이미 지나온 곳이다.
    /// (내려오는 도중 다시 내려오라고 하면 꼭대기부터 되짚어 위로 올라가 보였다)
    /// </summary>
    public static void TrimRouteAbove(List<Vector2> route, float footY)
    {
        if (route == null) return;
        for (int i = route.Count - 1; i >= 1; i--)      // 0번(바닥 출발점)은 남긴다
            if (route[i].y > footY + ROUTE_REACHED) route.RemoveAt(i);
    }

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
        if (HiddenSlot >= 0) p = _hideDoor;
        _rt.anchoredPosition = ClampToBand(p);
        EndDoorway();   // 문 자르기·움츠림 해제

        _route.Clear();
        _climbing = _freeClimb = false;
        SetHidden(-1);
        SetAngle(0f);
        if (_motor != null)
        {
            _motor.SetWalking(false);
            _motor.SetClimbing(false);
            _motor.SetResting(false);
            _motor.SetBurrowed(false);
        }
    }

    private void Update()
    {
        if (_rig == null) return;
        if (!_climbing && !IsHeld) _groundY = _rt.anchoredPosition.y;   // 손바닥에 들려 있으면 크기는 그대로
        _rig.DepthScale = DepthScaleFor(_groundY);
        _rig.DepthTint  = DepthTintFor(_groundY);   // 뒤로 갈수록 차갑게 (공기 원근)

        _squash     = Vector2.MoveTowards(_squash, _squashTarget, Time.deltaTime * SQUASH_SPEED);
        _rig.Squash = _squash;
        UpdateDoorClip();
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

    private bool TryFindPerk(DecorPerk perk, out Structure found)
    {
        foreach (var s in _structures)
        {
            if (s.perk != perk || s.use != DecorUse.None) continue;
            found = s;
            return true;
        }
        found = default;
        return false;
    }

    private bool TryFindSlot(int slot, out Structure found)
    {
        foreach (var s in _structures)
        {
            if (s.slot != slot) continue;
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

    // ── 장식 찾아가기 (2026-09-21) ────────────────────────────
    // 바닥 장식 옆에 서서 그쪽을 본다 → 홈 화면이 비비기·핥기·몸 데우기를 보여 준다 (DecorVisited).
    // 효과(DecorPerks)는 놓여 있기만 하면 생기고, 찾아가는 것은 보여 주기다.

    [Header("장식 찾아가기")]
    [Tooltip("바위에 기대 몸을 데우는 시간 (초)")]
    [SerializeField] private Vector2 baskStay = new Vector2(6f, 10f);          // [TBD]
    [Tooltip("이끼 바위에 비비기 · 화분 잎 핥기에 머무는 시간 (초)")]
    [SerializeField] private float   visitStay = 2.8f;                          // [TBD]
    [Tooltip("허물 준비(80 이상)일 때 쉬고 나서 이끼 바위로 가는 확률")]
    [SerializeField, Range(0f, 1f)] private float moltRubChance = 0.5f;         // [TBD]
    [Tooltip("머리가 장식 가장자리에 닿게 — 게코 몸 폭(큰 쪽) 대비 얼마나 떨어져 서는가")]
    [SerializeField, Range(0.3f, 1f)] private float visitReach = 0.6f;          // [TBD]

    /// <summary>허물 준비 중 (80 이상) — 이끼 바위를 찾아간다. 홈 화면이 상태가 바뀔 때마다 넣는다</summary>
    public bool MoltReady { get; set; }

    /// <summary>바닥 장식 옆에 도착해 그쪽을 봤다 (칸, 효과) — 홈 화면이 동작·말풍선·연출</summary>
    public event Action<int, DecorPerk> DecorVisited;

    /// <summary>
    /// 장식을 눌렀다 — 그 장식으로 간다. 은신처는 들어가고, 벽 구조물은 타고, 바닥 장식은 옆에 서서 본다.
    /// 집·벽에 있으면 먼저 나온다. 꺼져 있거나 달아나는 중·손바닥 위·이미 그 집 안이면 무시. 받아들였으면 true
    /// </summary>
    public bool VisitDecor(int slot)
    {
        if (!isActiveAndEnabled || IsFleeing || IsHeld || _rig == null) return false;
        if (!TryFindSlot(slot, out var s)) return false;
        if (s.use == DecorUse.Hide && HiddenSlot == slot) return false;   // 이미 안에 있다 — 홈 화면이 불러낸다
        StopLoop();
        _loop = StartCoroutine(ThenLoop(GoToDecor(s)));
        return true;
    }

    private IEnumerator GoToDecor(Structure s)
    {
        if (HiddenSlot >= 0)  yield return LeaveHide(comeSpeedScale);
        else if (_climbing)   yield return Descend(comeSpeedScale);
        else                  yield return RotateTo(0f);

        switch (s.use)
        {
            case DecorUse.Hide:
                yield return GoHide(s, sleepy: false);
                break;
            case DecorUse.None:
                yield return Visit(s, asked: true);
                break;
            default:
                var path = TerrariumLayout.ClimbPath(s.use, s.anchor, ClimbTop(), UnityEngine.Random.Range(0.6f, 1f));
                yield return ClimbRoute(path, free: false, perch: s.use == DecorUse.Branch);
                break;
        }
    }

    /// <summary>화분 잎에 물방울이 맺혔다 (물을 줬다) — 화분이 있으면 가서 핥는다. 받아들였으면 true</summary>
    public bool VisitDroplets()
        => TryFindPerk(DecorPerk.Droplets, out var plant) && VisitDecor(plant.slot);

    // 바닥 장식 옆 — 화면 가운데 쪽(자리가 넉넉한 쪽)에 서서 장식을 본다
    private IEnumerator Visit(Structure s, bool asked)
    {
        yield return WalkTo(VisitSpot(s), asked ? comeSpeedScale : 1f, mayPause: !asked);
        yield return Face(s.anchor.x > _rt.anchoredPosition.x);
        DecorVisited?.Invoke(s.slot, s.perk);

        if (s.perk == DecorPerk.Basking)
        {
            _motor.SetResting(true);          // 엎드려 몸을 데운다
            yield return Pause(Rand(baskStay));
            _motor.SetResting(false);
            yield break;
        }

        yield return Pause(visitStay);        // 비비기·핥기 (홈 화면이 동작을 넣는다)
        while (_motor.IsBusy) yield return null;
    }

    private Vector2 VisitSpot(Structure s)
    {
        float half = s.size.x * 0.5f * DepthScaleFor(s.anchor.y);
        _rig.GetExtents(out float left, out float right);
        float gap = half + Mathf.Max(left, right) * visitReach;

        GetWalkableX(out float minX, out float maxX);

        // 장식보다 살짝 앞 — 겹침 순서(발 높이)에서 게코가 장식 앞에 그려진다
        return new Vector2(VisitX(s.anchor.x, gap, minX, maxX),
                           Mathf.Clamp(s.anchor.y - VISIT_IN_FRONT, groundBand.x, groundBand.y));
    }

    /// <summary>
    /// 바닥 장식 옆에 설 가로 위치 — 화면 가운데 쪽(자리가 넉넉한 쪽)이 먼저, 안 되면 바깥쪽, 둘 다 안 되면 다닐 수 있는 끝.
    /// gap = 장식 가운데에서 게코 발까지
    /// </summary>
    public static float VisitX(float anchorX, float gap, float minX, float maxX)
    {
        float toCenter = anchorX > 0f ? -1f : 1f;
        float a = anchorX + toCenter * gap;   // 가운데 쪽
        float b = anchorX - toCenter * gap;   // 바깥쪽
        return a >= minX && a <= maxX ? a
             : b >= minX && b <= maxX ? b
             : Mathf.Clamp(a, minX, maxX);
    }

    private const float VISIT_IN_FRONT = 30f;

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

            // 허물이 가까우면 이끼 바위에 가서 몸을 비빈다 (게코는 거친 곳에 비벼 허물을 벗는다)
            if (MoltReady && TryFindPerk(DecorPerk.MoltRub, out var mossRock) && UnityEngine.Random.value < moltRubChance)
            {
                yield return Visit(mossRock, asked: false);
                continue;
            }

            if (_structures.Count > 0 && UnityEngine.Random.value < structureChance)
            {
                var s = _structures[UnityEngine.Random.Range(0, _structures.Count)];
                if (s.use == DecorUse.Hide)
                {
                    yield return GoHide(s, sleepy: false);
                }
                else if (s.use == DecorUse.None)
                {
                    // 바닥 장식은 쓸 일이 있을 때만 — 바위는 몸 데우기, 이끼 바위는 허물 준비 중.
                    // 화분은 물을 준 뒤에 홈 화면이 부른다 (VisitDroplets). 쓸 일이 없으면 그냥 돌아다닌다
                    if (s.perk == DecorPerk.Basking || (s.perk == DecorPerk.MoltRub && MoltReady))
                        yield return Visit(s, asked: false);
                    else
                        yield return WalkTo(PickTarget());
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

    // 문으로 들어간다 (2026-09-21) — 문 앞에 서서 문을 보고, 문 가장자리를 넘는 부분부터 안 보이게(자르기) 기어 들어간다.
    // 문이 게코보다 작아 들어가면서 조금 작아지고 납작해진다 (게코도 좁은 틈에는 몸을 납작하게 해 들어간다).
    // 예전에는 은신처 그림을 게코 앞에 그려 가렸는데, 게코가 은신처보다 길어 머리가 반대편으로 삐져나와
    // 굴에 들어가는 게 아니라 바위 뒤에 숨은 것처럼 보였다. 문 정보가 없는 은신처는 예전 방식(GoHideBehind)
    private IEnumerator GoHide(Structure house, bool sleepy)
    {
        if (house.door.width <= 1f)
        {
            yield return GoHideBehind(house, sleepy);
            yield break;
        }

        GetWalkableX(out float minX, out float maxX);
        var plan = PlanDoor(house.door, house.anchor.x, house.size.x * DepthScaleFor(house.anchor.y),
                            _rig.FrontReach, _rig.RearReach, _rig.TopExtent, minX, maxX, doorFit, doorMinShrink, doorInside);
        float y = Mathf.Clamp(house.anchor.y - DOOR_IN_FRONT, groundBand.x, groundBand.y);

        yield return WalkTo(new Vector2(plan.standX, y));
        yield return Face(plan.insideRight);                  // 문을 본다
        yield return Pause(0.25f);

        _hideAnchor = house.anchor;
        _hideDoor   = new Vector2(plan.standX, y);
        BeginDoorway(plan);                                   // 문 가장자리 너머는 안 보인다
        SetHidden(house.slot);                                // 은신처 그림은 게코 뒤로 (홈 화면 겹침 순서)
        yield return WalkTo(new Vector2(plan.insideX, y), 0.5f, mayPause: false);
        _motor.SetResting(true);    // 안에서는 엎드린다 — 밖에 남은 꼬리가 바닥 쪽으로 (서 있으면 막대기처럼 떠 보였다)
        _motor.SetBurrowed(true);

        yield return Pause(Rand(sleepy ? hideStaySleepy : hideStay));
        yield return LeaveHide(1f);
    }

    // 예전 방식 — 은신처 그림이 게코 앞에 와서 가린다 (문 정보가 없는 은신처)
    private IEnumerator GoHideBehind(Structure house, bool sleepy)
    {
        Vector2 inside = ClampToBand(house.anchor);
        Vector2 door   = ClampToBand(TerrariumLayout.HideDoor(inside, hideDoorOffset));

        yield return WalkTo(door);
        yield return WalkTo(inside, 0.6f, mayPause: false);   // 집 쪽을 보고 천천히 들어간다
        _hideAnchor = inside;
        _hideDoor   = door;
        SetHidden(house.slot);                                // 집 그림이 게코 앞으로 — 꼬리만 삐죽
        if (sleepy) _motor.SetResting(true);

        yield return Pause(Rand(sleepy ? hideStaySleepy : hideStay));
        yield return LeaveHide(1f);
    }

    // 나온다 — 안에서 돌아서서(머리가 문 밖으로) 문 앞까지 걸어 나오고, 다 나오면 자르기를 끈다
    private IEnumerator LeaveHide(float speedScale)
    {
        _motor.SetResting(false);
        _motor.SetBurrowed(false);
        if (HiddenSlot < 0) yield break;

        Vector2 door = ClampToBand(_hideDoor);
        yield return Face(door.x > _rt.anchoredPosition.x);
        if (_doorway)
        {
            _squashTarget = Vector2.one;                      // 나오면서 원래 크기로
            yield return Travel(door, speedScale, 2f);
            EndDoorway();
            SetHidden(-1);
        }
        else
        {
            SetHidden(-1);
            yield return Travel(door, speedScale, 2f);
        }
    }

    // ── 문 자르기 · 움츠림 ────────────────────────────────────

    [Header("은신처 — 문으로 들어가기")]
    [Tooltip("움츠린 몸 높이 = 문 높이 × 이 값")]
    [SerializeField, Range(0.5f, 1f)] private float doorFit = 0.9f;          // [TBD]
    [Tooltip("움츠릴 때 작아지는 한계 — 나머지는 납작해져서 맞춘다 (한쪽만 쓰면 너무 작거나 판처럼 납작하다)")]
    [SerializeField, Range(0.3f, 1f)] private float doorMinShrink = 0.55f;   // [TBD]
    [Tooltip("다 들어갔을 때 몸길이의 몇 %가 문 안에 있는가 — 나머지(꼬리 쪽)는 밖에 삐죽")]
    [SerializeField, Range(0.3f, 0.95f)] private float doorInside = 0.68f;   // [TBD]
    [Tooltip("문 가장자리가 흐려지는 폭 (UI 단위) — 칼로 자른 선 대신 어둠 속으로 스며드는 것처럼")]
    [SerializeField] private int doorSoftness = 18;                           // [TBD]

    private const float DOOR_IN_FRONT = 12f;    // 문 앞 발 높이 — 은신처가 닿는 바닥보다 살짝 앞
    private const float STAND_GAP     = 8f;     // 문 앞에 설 때 주둥이와 문 가장자리 사이
    private const float SQUASH_SPEED  = 1.1f;   // 움츠림이 바뀌는 빠르기 (/초)

    private RectMask2D _doorClip;
    private bool    _doorway;              // 문으로 들어가 있는 중 (자르기 켜짐)
    private float   _doorEdgeX;            // 문 가장자리 (영역 좌표) — 넘은 부분은 안쪽이라 안 보인다
    private bool    _doorInsideRight;
    private Vector2 _hideDoor;             // 나오면 설 곳
    private Vector2 _squash = Vector2.one, _squashTarget = Vector2.one;

    /// <summary>문으로 들어가 있다 — 홈 화면은 은신처를 게코 뒤에 그리고, 게코의 안 보이는 쪽을 누르면 불러낸다</summary>
    public bool InDoorway => _doorway;

    /// <summary>은신처 문으로 들어가는 계획 (영역 좌표)</summary>
    public struct DoorPlan
    {
        public bool    insideRight;   // 문 안쪽이 오른쪽 (왼쪽에서 오른쪽으로 들어간다)
        public float   edgeX;         // 문 가장자리 — 이 선을 넘은 부분은 안 보인다
        public float   standX;        // 들어가기 전 선 곳 (주둥이가 문 가장자리 바로 앞)
        public float   insideX;       // 다 들어갔을 때 발 위치
        public Vector2 squash;        // 문 높이에 맞춘 움츠림 (가로 = 크기, 세로 = 크기 × 납작함)
    }

    /// <summary>
    /// 어느 쪽에서 · 어디까지 · 얼마나 움츠려 들어갈지. door = 문 사각형, decorX · decorWidth = 은신처 가운데·폭,
    /// front · rear · height = 게코 주둥이·꼬리 길이와 키 (움츠림 전), minX · maxX = 다닐 수 있는 발 범위
    /// </summary>
    public static DoorPlan PlanDoor(Rect door, float decorX, float decorWidth, float front, float rear, float height,
                                    float minX, float maxX, float fit, float minShrink, float inside)
    {
        // 문이 은신처 한쪽에 치우쳐 있으면 그쪽 바깥에서, 가운데면 화면 가운데 쪽에서 들어간다
        float rel     = door.center.x - decorX;
        bool fromLeft = Mathf.Abs(rel) > decorWidth * 0.03f ? rel < 0f : decorX > 0f;

        float standL = door.xMin - front - STAND_GAP, standR = door.xMax + front + STAND_GAP;
        if (fromLeft && standL < minX && standR <= maxX) fromLeft = false;        // 그쪽에 설 자리가 없으면 반대쪽에서
        else if (!fromLeft && standR > maxX && standL >= minX) fromLeft = true;

        // 움츠림 — 몸 높이를 문 높이 × fit에 맞춘다. 작아지기와 납작해지기를 반반(제곱근)
        float f      = height > 0.01f ? Mathf.Min(1f, door.height * fit / height) : 1f;
        float shrink = Mathf.Clamp(Mathf.Sqrt(f), minShrink, 1f);
        float flat   = Mathf.Clamp(f / shrink, 0.3f, 1f);

        float len = (front + rear) * shrink;
        var p = new DoorPlan
        {
            insideRight = fromLeft,
            edgeX       = fromLeft ? door.xMin : door.xMax,
            standX      = Mathf.Clamp(fromLeft ? standL : standR, minX, maxX),
            squash      = new Vector2(shrink, shrink * flat),
        };
        p.insideX = fromLeft ? p.edgeX + inside * len - front * shrink
                             : p.edgeX - inside * len + front * shrink;
        return p;
    }

    private void BeginDoorway(DoorPlan plan)
    {
        _doorway         = true;
        _doorEdgeX       = plan.edgeX;
        _doorInsideRight = plan.insideRight;
        _squashTarget    = plan.squash;
        UpdateDoorClip();
    }

    private void EndDoorway()
    {
        _doorway      = false;
        _squash       = _squashTarget = Vector2.one;
        if (_rig != null) _rig.Squash = Vector2.one;
        if (_doorClip != null) _doorClip.enabled = false;
    }

    // 게코 오브젝트에 RectMask2D — 문 가장자리 너머(안쪽)를 잘라 낸다. 바닥에서는 게코 오브젝트가 회전·크기 없이 움직이므로
    // 영역 좌표의 문 가장자리 = 게코 로컬 x (가장자리 − 발 위치). 나머지 세 방향은 넉넉히 열어 둔다
    private void UpdateDoorClip()
    {
        if (!_doorway)
        {
            if (_doorClip != null && _doorClip.enabled) _doorClip.enabled = false;
            return;
        }
        if (_doorClip == null)
        {
            _doorClip = GetComponent<RectMask2D>();
            if (_doorClip == null) _doorClip = gameObject.AddComponent<RectMask2D>();
        }
        const float OPEN = 5000f;
        Rect  r     = _rt.rect;
        float local = _doorEdgeX - _rt.anchoredPosition.x;
        _doorClip.padding  = _doorInsideRight
            ? new Vector4(-OPEN, -OPEN, r.xMax - local, -OPEN)
            : new Vector4(local - r.xMin, -OPEN, -OPEN, -OPEN);
        _doorClip.softness = new Vector2Int(doorSoftness, 0);
        if (!_doorClip.enabled) _doorClip.enabled = true;
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

    // 빈 유리벽 — 가까운 쪽 좌우 유리벽으로 걸어가 발이 벽을 향하게 세우고 오른다
    // (화면 가운데에서 그대로 오르면 붙을 곳이 없어 허공에 매달린 것처럼 보인다)
    private IEnumerator FreeClimb()
    {
        GetWalkableX(out float minX, out float maxX);
        if (minX >= maxX) yield break;

        bool  leftWall = ClimbWallIsLeft(_rt.anchoredPosition.x, minX, maxX);
        float wallX    = WallClimbX(leftWall, minX, maxX, wallMargin);
        Vector2 foot   = ClampToBand(new Vector2(wallX, _rt.anchoredPosition.y));

        yield return WalkTo(foot);

        float target = Mathf.Min(ClimbTop(), foot.y + Rand(climbHeight));
        if (target - foot.y < CLIMB_MIN_RISE) yield break;   // 오를 자리가 없다

        // 발이 벽을 향하도록 벽 쪽을 보고, 한 번 올려다본 뒤 오른다
        yield return Face(!leftWall);
        _motor.TryPlayIdle(GeckoAction.Happy_LookUp);
        while (_motor.IsBusy) yield return null;

        yield return ClimbRoute(new[] { foot, new Vector2(foot.x, target) }, free: true, perch: false);
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

    // 올라온 길을 거꾸로 내려와 바닥에서 몸을 눕힌다.
    // 내려오는 도중 다시 불릴 수 있으므로(만지면 도망 · 불러서 오기) **지나온 점은 지우고**,
    // 이미 지나친 위쪽 점은 먼저 버린다 — 예전에는 꼭대기부터 다시 훑어 위로 되올라갔다.
    private IEnumerator Descend(float speedScale)
    {
        _motor.SetResting(false);
        TrimRouteAbove(_route, _rt.anchoredPosition.y);
        if (DescentIsVertical()) yield return FlipOnWall();   // 벽에 붙은 채 머리를 아래로

        for (int i = _route.Count - 1; i >= 0; i--)
        {
            float sc = i == 0 ? speedScale * climbLandSlow : speedScale;   // 바닥에 내려서는 마지막 구간은 천천히
            yield return Segment(_route[i], sc);
            if (i < _route.Count) _route.RemoveAt(i);   // 지나온 점 (도중에 다시 내려오라고 해도 여기부터)
        }
        yield return RotateTo(0f);

        _route.Clear();
        _climbing = _freeClimb = false;
        _motor.SetClimbing(false);
    }

    // 내려갈 첫 구간이 거의 곧게 아래인가 (= 벽에 붙어 있다)
    private bool DescentIsVertical()
    {
        if (Mathf.Abs(_angle) < TILT_CLIMBING) return false;
        Vector2 p = _rt.anchoredPosition;
        for (int i = _route.Count - 1; i >= 0; i--)
        {
            Vector2 d = _route[i] - p;
            if (d.magnitude < 4f) continue;
            return Mathf.Abs(d.x) < 40f && d.y < -40f;
        }
        return false;
    }

    /// <summary>
    /// 벽에 붙은 채 제자리에서 머리를 아래로 돌린다 — **그림만 좌우 반전**하면 각도가 그대로라 발은 계속 벽을 짚는다.
    /// (예전에는 +90°에서 -90°로 0°를 지나 돌아, 벽에서 떨어져 한 바퀴 도는 것처럼 보였다)
    /// </summary>
    private IEnumerator FlipOnWall()
    {
        _motor.SetWalking(false);
        yield return Pause(0.15f);            // 아래를 살핀다
        yield return Face(!_rig.FacingRight);
        yield return Pause(0.12f);
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

        bool right;
        if (Mathf.Abs(_angle) > TILT_CLIMBING && Mathf.Abs(dir.x) < 40f && Mathf.Abs(dir.y) > 40f)
        {
            // 벽에 붙어 오르내리는 중 — 발이 짚은 벽은 그대로 두고 **그림만 뒤집어** 머리가 가는 쪽을 본다
            bool leftWall = _angle < 0f;
            bool headUp   = dir.y > 0f;
            right = leftWall ? !headUp : headUp;
        }
        else right = Mathf.Abs(dir.x) < 1f ? _rig.FacingRight : dir.x > 0f;   // 위아래로만 가면 방향 유지
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
