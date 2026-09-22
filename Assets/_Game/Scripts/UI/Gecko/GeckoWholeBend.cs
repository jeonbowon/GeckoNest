using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전신 그림 한 장을 뼈대로 휜다 (2026-09-22). 전신 스킨(GeckoSkin.wholeBody)이면 GeckoRig가 몸통 Image에 붙인다.
///
/// 전신 그림은 머리·몸·꼬리·다리가 한 장이라 파츠를 따로 돌릴 수 없다 → 그림을 촘촘한 격자로 나누고
/// 꼭짓점마다 어느 뼈에 얼마나 붙어 있는지(0~1)를 구해 그 관절을 중심으로 그만큼 돌린다 (2D 스키닝).
///   머리  — 목을 중심으로 머리 각도 × 배율(wholeHeadGain) + 목 빼기(offset). 영역은 가로 × 세로 (머리 아래 앞다리가 따라 돌지 않게)
///   꼬리  — 뿌리부터 끝까지 마디 사슬(wholeTailChain). 모터의 꼬리 굽힘(12마디 물결)을 마디마다 받아 끝에서부터 차례로 돌린다
///   다리 4 — 어깨·엉덩이를 중심으로 다리 각도(걸음 = 대각선 두 발씩) + 발 들기(offset). 영역은 다리 상자, 관절 쪽은 부드럽게
/// 경계 없이 이어지므로 목·꼬리 뿌리·어깨가 잘려 보이지 않는다.
///
/// 좌표: 설정은 그림 안 비율(uv, 0,0 = 왼쪽 아래, 오른쪽을 보는 그림 기준), 자세 값은 스킨 픽셀(= 이 그림의 로컬 픽셀).
/// 좌우 반전은 부모(_visual) 크기로 하므로 신경 쓰지 않는다.
/// 예전(첫 판)에는 꼬리 굽힘을 **모두 더해** 한 각도로 돌렸는데, 물결이 마디마다 어긋나 있어 합이 거의 0 → 꼬리가 멈춰 보였다. 다리는 움직이지 않았다
/// </summary>
[RequireComponent(typeof(Graphic))]
public class GeckoWholeBend : BaseMeshEffect
{
    public const int    COLUMNS   = 64;   // 격자 (미리보기도 같은 격자로 그린다)
    public const int    ROWS      = 36;
    private const float HEAD_MAX  = 20f;     // [TBD] 머리 회전 한계 (배율을 곱한 뒤)
    private const float TAIL_GAIN = 1f;      // [TBD] 꼬리 굽힘을 받는 몫
    private const float LEG_SOFT  = 0.035f;  // 다리 캡슐 밖으로 옅어지는 폭 (그림 폭 비율)
    private const float LEG_ROOT  = 0.22f;   // 뼈 길이 중 관절 쪽 이만큼은 몸에서 다리로 넘어가는 구간 — 어깨·엉덩이 살이 늘어나듯
    private const float FAR_LEG_ROOT = 0.6f; // [TBD] 먼 다리는 옆 가까운 어깨를 중심으로 돌아 자기 어깨 자리에서도 많이 움직인다 → 몸에 넘어가는 구간을 길게 (짧으면 어깨가 톱니처럼 꺾였다)

    public static readonly GeckoPartId[] LEGS =
    {
        GeckoPartId.LegFrontNear, GeckoPartId.LegFrontFar, GeckoPartId.LegBackNear, GeckoPartId.LegBackFar,
    };

    /// <summary>이 다리(LEGS 번호)가 따라 도는 다리 — 먼 다리는 옆 가까운 다리의 관절·각도를 그대로 쓴다 (붙어 있는 발가락이 한 덩어리로 움직여 늘어나지 않게)</summary>
    public static int PivotOf(int leg) => leg == 1 ? 0 : leg == 3 ? 2 : leg;

    // ── 설정 (스킨) ──────────────────────────────────────────
    private Vector2   _headPivot = new Vector2(0.75f, 0.5f);
    private Vector4   _headZone  = new Vector4(0.66f, 0.76f, 0.46f, 0.56f);   // 가로 0→1, 세로 0→1
    private float     _headGain  = 1f;
    private Vector2[] _tailChain = { new Vector2(0.33f, 0.28f), new Vector2(0.02f, 0.28f) };
    private Vector2   _tailZone  = new Vector2(0.33f, 0.22f);                 // 가로 0→1
    private readonly Vector2[] _legJoint  = new Vector2[4];
    private readonly Vector2[] _legFoot   = new Vector2[4];
    private readonly Vector2[] _legRadius = new Vector2[4];   // x = 관절 쪽, y = 발 쪽 (그림 폭 비율)
    private readonly float[]   _legW = new float[4], _legQ = new float[4];   // 계산 버퍼
    private const float LEG_BAND = 0.6f;    // [TBD] 두 발을 섞는 구간 (폭 대비 거리 차이) — 0.12로 좁혔더니 붙어 있는 먼 다리 가장자리가 톱니처럼 찢어졌다
    private int _legCount;

    // ── 자세 ─────────────────────────────────────────────────
    private float     _headAngle, _tailRoot;
    private Vector2   _headOffset;
    private float[]   _tailJoint = new float[1];   // 사슬 관절마다 각도 (도, + = 반시계)
    private readonly float[]   _legAngle  = new float[4];
    private readonly Vector2[] _legOffset = new Vector2[4];
    private bool _moved;

    public float HeadAngle => _headAngle;
    public int   TailJoints => _tailJoint.Length;

    /// <summary>스킨의 뼈대 (ApplySkin 때 한 번). 다리는 LEGS 순서 — 관절·발은 uv, 폭은 그림 폭 비율 (관절 쪽, 발 쪽)</summary>
    public void Configure(Vector2 headPivot, Vector4 headZone, float headGain, Vector2[] tailChain, Vector2 tailZone,
                          GeckoWholeLimb[] legs)
    {
        _headPivot = headPivot;
        _headZone  = headZone;
        _headGain  = headGain > 0f ? headGain : 1f;
        _tailZone  = tailZone;
        if (tailChain != null && tailChain.Length >= 2) _tailChain = (Vector2[])tailChain.Clone();
        if (_tailJoint.Length != _tailChain.Length - 1) _tailJoint = new float[_tailChain.Length - 1];

        _legCount = 0;
        for (int i = 0; i < LEGS.Length && legs != null && i < legs.Length; i++)
        {
            var l = legs[i];
            if (l == null || l.radius.y <= 0f) { _legRadius[i] = Vector2.zero; _legJoint[i] = _legFoot[i] = Vector2.zero; continue; }
            _legJoint[i] = l.joint; _legFoot[i] = l.foot; _legRadius[i] = l.radius;
            _legCount = i + 1;
        }
        if (graphic != null) graphic.SetVerticesDirty();
    }

    /// <summary>한 프레임 자세 — 머리·꼬리·다리는 몸통 대비 값이라 그대로 이 그림 공간이다</summary>
    public void SetPose(GeckoPose pose)
    {
        if (pose == null) return;
        bool changed = false;

        ref var head = ref pose.parts[(int)GeckoPartId.Head];
        changed |= Set(ref _headAngle, Mathf.Clamp(head.angle * _headGain, -HEAD_MAX, HEAD_MAX));
        changed |= Set(ref _headOffset, head.offset);
        changed |= Set(ref _tailRoot, pose.parts[(int)GeckoPartId.Tail].angle);

        // 모터 꼬리 굽힘(뿌리 → 끝, n마디) → 사슬 관절(m개): 마디 가운데가 들어가는 관절에 더한다. + = 위로 말림 → 왼쪽으로 뻗은 꼬리는 시계 방향
        int m = _tailJoint.Length, n = pose.tailBend.Length;
        for (int j = 0; j < m; j++)
        {
            float a = 0f;
            for (int k = 0; k < n; k++)
                if (Mathf.Min(m - 1, (int)((k + 0.5f) / n * m)) == j) a += pose.tailBend[k];
            changed |= Set(ref _tailJoint[j], -a * TAIL_GAIN);
        }

        // 먼 다리는 바로 옆 가까운 다리를 따라간다 (PivotOf) — 그림에서 두 발의 발가락이 붙어 있어,
        // 걸음처럼 반대 박자로 움직이면 맞닿은 발가락이 늘어났다. 먼 다리는 대부분 가려져 박자 차이가 거의 안 보인다
        for (int i = 0; i < 4; i++)
        {
            ref var leg = ref pose.parts[(int)LEGS[PivotOf(i)]];
            changed |= Set(ref _legAngle[i], leg.angle);
            changed |= Set(ref _legOffset[i], leg.offset);
        }

        _moved = Mathf.Abs(_headAngle) > 0.01f || _headOffset.sqrMagnitude > 0.01f || Mathf.Abs(_tailRoot) > 0.01f;
        for (int j = 0; j < m && !_moved; j++) _moved = Mathf.Abs(_tailJoint[j]) > 0.01f;
        for (int i = 0; i < 4 && !_moved; i++) _moved = Mathf.Abs(_legAngle[i]) > 0.01f || _legOffset[i].sqrMagnitude > 0.01f;

        if (changed && graphic != null) graphic.SetVerticesDirty();
    }

    /// <summary>쉬는 자세로 (스킨을 바꿀 때)</summary>
    public void ResetPose()
    {
        _headAngle = _tailRoot = 0f;
        _headOffset = Vector2.zero;
        for (int j = 0; j < _tailJoint.Length; j++) _tailJoint[j] = 0f;
        for (int i = 0; i < 4; i++) { _legAngle[i] = 0f; _legOffset[i] = Vector2.zero; }
        _moved = false;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    private static bool Set(ref float field, float value)
    {
        if (Mathf.Abs(field - value) < 0.01f) return false;
        field = value;
        return true;
    }

    private static bool Set(ref Vector2 field, Vector2 value)
    {
        if ((field - value).sqrMagnitude < 0.0001f) return false;
        field = value;
        return true;
    }

    // ── 붙는 정도 (uv만으로 정해진다) ─────────────────────────

    /// <summary>그림 안 위치 → 머리를 따르는 정도 (0 = 몸통 그대로, 1 = 머리 그대로)</summary>
    public static float HeadWeight(Vector2 uv, Vector4 zone) => Ramp(zone.x, zone.y, uv.x) * Ramp(zone.z, zone.w, uv.y);

    /// <summary>그림 안 위치 → 꼬리를 따르는 정도</summary>
    public static float TailWeight(Vector2 uv, Vector2 zone) => Ramp(zone.x, zone.y, uv.x);

    /// <summary>
    /// 한 점(픽셀) → 다리를 따르는 정도. 다리 = 관절(어깨·엉덩이) → 발 가운데 뼈, 폭은 관절 rTop에서 발 rFoot로 넓어지는 캡슐(발가락까지 덮게).
    /// 캡슐 밖 soft만큼 옅어지고, 관절 쪽(뼈의 앞 LEG_ROOT)은 몸에 붙어 있다 — 어깨·엉덩이 살이 늘어나듯
    /// </summary>
    public static float LegWeight(Vector2 p, Vector2 joint, Vector2 foot, float rTop, float rFoot, float soft)
        => LegWeight(p, joint, foot, rTop, rFoot, soft, out _);

    /// <summary>q = 뼈까지 거리 ÷ 그 자리 폭 (1 = 캡슐 가장자리) — 맞닿은 두 발 중 어느 쪽에 더 가까운가</summary>
    public static float LegWeight(Vector2 p, Vector2 joint, Vector2 foot, float rTop, float rFoot, float soft, out float q, float root = LEG_ROOT)
    {
        Vector2 bone = foot - joint;
        float len2 = Mathf.Max(1e-6f, bone.sqrMagnitude);
        float tRaw = Vector2.Dot(p - joint, bone) / len2;
        float t = Mathf.Clamp01(tRaw);
        float d = (joint + bone * t - p).magnitude;
        float r = Mathf.Max(1e-3f, Mathf.Lerp(rTop, rFoot, t));
        q = d / r;
        return (1f - Ramp(r, r + soft, d)) * Ramp(0f, root, tRaw);
    }

    /// <summary>a에서 0, b에서 1인 부드러운 경사 (a > b도 된다)</summary>
    private static float Ramp(float a, float b, float t)
    {
        if (Mathf.Approximately(a, b)) return t >= b ? 1f : 0f;
        float k = Mathf.Clamp01((t - a) / (b - a));
        return k * k * (3f - 2f * k);
    }

    // ── 휘기 ─────────────────────────────────────────────────

    /// <summary>그림 안 한 점(uv)이 지금 자세에서 가는 곳 (로컬 좌표, 그림 사각형 min~max). 격자 꼭짓점과 자가 검사가 같이 쓴다</summary>
    public Vector2 Deform(Vector2 uv, Vector2 min, Vector2 max)
    {
        Vector2 size = max - min;
        Vector2 L(Vector2 u) => min + Vector2.Scale(size, u);
        Vector2 p = L(uv);
        if (!_moved) return p;

        // 다리 — 뼈(관절 → 발) 캡슐 무게. 붙어 있는 두 발 사이는 무게를 나눠 섞는다 (한쪽으로 찢어지거나 발가락이 비스듬히 잘리지 않게)
        // 두 발이 맞닿은 곳은 부드럽게 섞되 더 가까운 발(폭 대비 거리가 작은 쪽)을 조금 더 따른다 — 날카롭게 나누면 먼 다리가 찢어진다
        if (_legCount > 0)
        {
            float qMin = float.MaxValue;
            for (int i = 0; i < _legCount; i++)
            {
                _legW[i] = 0f;
                if (_legRadius[i].y <= 0f) continue;   // 스킨에 없는 다리
                _legW[i] = LegWeight(p, L(_legJoint[i]), L(_legFoot[i]), _legRadius[i].x * size.x, _legRadius[i].y * size.x,
                                     LEG_SOFT * size.x, out _legQ[i], PivotOf(i) == i ? LEG_ROOT : FAR_LEG_ROOT);
                if (_legW[i] > 0f) qMin = Mathf.Min(qMin, _legQ[i]);
            }

            float sum = 0f;
            Vector2 move = Vector2.zero;
            for (int i = 0; i < _legCount; i++)
            {
                float w = _legW[i];
                if (w <= 0f) continue;
                w *= 1f - Ramp(0f, LEG_BAND, _legQ[i] - qMin);
                if (w <= 0f) continue;
                // 먼 다리도 옆 가까운 다리의 관절·각도로 **똑같이** 돈다 — 붙어 있는 발가락이 한 덩어리로 움직인다.
                // (가까운 발이 움직인 만큼 평행 이동만 시키면 먼 다리 윗부분이 옆으로 밀려 찢어졌다. 먼 앞발은 딛는 박자에 조금 들린다)
                Vector2 j = L(_legJoint[PivotOf(i)]);
                move += w * (Rotate(p, j, _legAngle[i]) + _legOffset[i] - p);
                sum  += w;
            }
            if (sum > 1f) move /= sum;
            p += move;
        }

        // 꼬리 — 끝 쪽 관절부터 뿌리 쪽으로 (쉬는 자리 관절을 중심으로 돌리면 정기구학과 같다)
        float wt = TailWeight(uv, _tailZone);
        if (wt > 0f)
        {
            float s = ChainParam(uv);
            for (int j = _tailJoint.Length - 1; j >= 0; j--)
            {
                float a = _tailJoint[j] + (j == 0 ? _tailRoot : 0f);
                if (Mathf.Abs(a) < 0.001f) continue;
                float f = Ramp(j - 0.5f, j + 0.5f, s);   // 관절 앞뒤로 반 마디씩 부드럽게
                if (j == 0) f = Ramp(0f, 0.5f, s);        // 뿌리는 몸 쪽으로 넘어가지 않는다
                if (f > 0f) p = Rotate(p, L(_tailChain[j]), a * f * wt);
            }
        }

        // 머리
        float wh = HeadWeight(uv, _headZone);
        if (wh > 0f) p = Rotate(p, L(_headPivot), _headAngle * wh) + _headOffset * wh;
        return p;
    }

    /// <summary>꼬리 사슬 위 위치 (0 = 뿌리 관절, 1 = 다음 관절 …). 가장 가까운 마디에 투영한다</summary>
    private float ChainParam(Vector2 uv)
    {
        float best = float.MaxValue, s = 0f;
        for (int j = 0; j < _tailChain.Length - 1; j++)
        {
            Vector2 a = _tailChain[j], b = _tailChain[j + 1], ab = b - a;
            float len2 = Mathf.Max(1e-8f, ab.sqrMagnitude);
            float t = Mathf.Clamp01(Vector2.Dot(uv - a, ab) / len2);
            float d = (a + ab * t - uv).sqrMagnitude;
            if (d < best) { best = d; s = j + t; }
        }
        return s;
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        // 기본 Image(Simple)는 사각형 하나 = 꼭짓점 4개. 그 밖의 모양은 건드리지 않는다
        if (!IsActive() || vh.currentVertCount != 4 || !_moved) return;

        UIVertex v = default;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
        Vector2 uvMin = min, uvMax = max;
        Color32 color = default;
        for (int i = 0; i < 4; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            if (i == 0) color = v.color;
            min   = Vector2.Min(min, v.position);
            max   = Vector2.Max(max, v.position);
            uvMin = Vector2.Min(uvMin, v.uv0);
            uvMax = Vector2.Max(uvMax, v.uv0);
        }

        vh.Clear();
        for (int r = 0; r <= ROWS; r++)
        {
            float fv = (float)r / ROWS;
            for (int c = 0; c <= COLUMNS; c++)
            {
                float fu = (float)c / COLUMNS;
                Vector2 p = Deform(new Vector2(fu, fv), min, max);
                vh.AddVert(new Vector3(p.x, p.y, 0f), color,
                           new Vector2(Mathf.Lerp(uvMin.x, uvMax.x, fu), Mathf.Lerp(uvMin.y, uvMax.y, fv)));
            }
        }

        int stride = COLUMNS + 1;
        for (int r = 0; r < ROWS; r++)
        for (int c = 0; c < COLUMNS; c++)
        {
            int a = r * stride + c, b = a + 1, d = a + stride, e = d + 1;
            vh.AddTriangle(a, d, e);
            vh.AddTriangle(a, e, b);
        }
    }

    private static Vector2 Rotate(Vector2 p, Vector2 pivot, float deg)
    {
        if (Mathf.Abs(deg) < 0.0001f) return p;
        float rad = deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
        Vector2 d = p - pivot;
        return pivot + new Vector2(d.x * cs - d.y * sn, d.x * sn + d.y * cs);
    }
}
