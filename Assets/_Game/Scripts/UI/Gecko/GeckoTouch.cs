using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>게코를 누른 곳</summary>
public enum GeckoTouchZone { Head, Eye, Mouth, FrontLeg, BackLeg, Body, TailBase, TailTip }

/// <summary>
/// 게코를 직접 누르기 — 보이지 않는 터치 영역을 게코 옆(GeckoArea 안)에 두고 게코 위치·크기·회전을 매 프레임 따라간다.
/// 누른 곳이 어느 파츠 그림 안인지로 부위를 정해 알려 준다 (반응은 HomeUIController).
///
/// - 작은 부위부터 본다: 눈 → 입 → 앞다리 → 뒷다리 → 머리 → 꼬리 → 몸통 (파츠마다 여유를 조금 둔다)
/// - 파츠의 실제 사각형(회전·좌우 반전·크기 반영)으로 판정하므로 벽을 타느라 돌아가 있어도 맞는다
/// - 꼬리는 그림 안 위치로 뿌리·끝을 나눈다 (지금 그림은 관절이 오른쪽 끝 — 오른쪽이 뿌리)
///
/// 게코 오브젝트는 하위 캔버스라 그 안의 그림은 홈 화면의 터치 판정에 잡히지 않는다 → 영역을 게코 밖(형제)에 둔다.
/// 먹이 선반·부화 연출·팝업은 이 영역보다 위에 있어서 열려 있으면 자연히 막힌다.
/// </summary>
[DisallowMultipleComponent]
public class GeckoTouch : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private const float HEIGHT_RATIO   = 0.55f;   // [TBD] 터치 영역 높이 = 게코 폭 × 이 값 (발밑부터)
    private const float TAIL_TIP_SPLIT = 0.55f;   // 꼬리 그림에서 이 비율(왼쪽 끝 0)보다 오른쪽은 뿌리 [TBD]

    // 판정 순서 — (파츠, 부위, 판정 박스 중심, 판정 박스 크기: 파츠 그림 사각형 대비 비율)
    //
    // **파츠 그림 사각형을 그대로 쓰지 않는다** (2026-09-20). 최종 그림에서 눈·입은 머리에 이미 그려져 있고
    // `eye_open`·`mouth_closed`는 다른 표정을 덮기 위한 **빈 판**이다 (눈 186×186 · 입 255×88, 불투명 픽셀 0개).
    // 그림 사각형으로 판정하면 눈 판(+여유)이 머리(408×210)보다 세로로 커져 머리 판정이 27%만 남았다.
    // 그래서 판정은 파츠 사각형 안의 박스로 한다 — 그림을 바꾸면 자가 검사 TestTouchAndMovement로 확인할 것.
    private static readonly (GeckoPartId part, GeckoTouchZone zone, Vector2 center, Vector2 size)[] ORDER =
    {
        (GeckoPartId.EyeL,         GeckoTouchZone.Eye,      Half, new Vector2(0.30f, 0.45f)),  // 빈 판 안의 실제 눈 [TBD]
        (GeckoPartId.EyeR,         GeckoTouchZone.Eye,      Half, new Vector2(0.30f, 0.45f)),
        (GeckoPartId.Mouth,        GeckoTouchZone.Mouth,    Half, new Vector2(1.00f, 1.10f)),
        (GeckoPartId.LegFrontNear, GeckoTouchZone.FrontLeg, Half, new Vector2(1.30f, 1.30f)),
        (GeckoPartId.LegFrontFar,  GeckoTouchZone.FrontLeg, Half, new Vector2(1.30f, 1.30f)),
        (GeckoPartId.LegBackNear,  GeckoTouchZone.BackLeg,  Half, new Vector2(1.30f, 1.30f)),
        (GeckoPartId.LegBackFar,   GeckoTouchZone.BackLeg,  Half, new Vector2(1.30f, 1.30f)),
        (GeckoPartId.Head,         GeckoTouchZone.Head,     Half, Vector2.one),
        (GeckoPartId.Tail,         GeckoTouchZone.TailTip,  Half, new Vector2(1.10f, 1.10f)),  // 뿌리·끝은 TailZone으로 나눈다
        (GeckoPartId.Body,         GeckoTouchZone.Body,     Half, Vector2.one),
    };

    private static Vector2 Half => new Vector2(0.5f, 0.5f);

    // 박스가 파츠 사각형보다 클 수 있으므로 uv는 넉넉히 받아 온 뒤(아래 ZoneAt) 박스로 판정한다
    private const float UV_MARGIN = 1f;

    private RectTransform                    _rt;
    private RectTransform                    _gecko;
    private GeckoRig                         _rig;
    private Action<GeckoTouchZone, Vector3>  _onTouch;   // (부위, 누른 곳 월드 좌표)

    /// <summary>gecko = GeckoObject (발밑 피벗). 게코나 그림 조립기가 없으면 null</summary>
    public static GeckoTouch Create(RectTransform gecko, GeckoRig rig, Action<GeckoTouchZone, Vector3> onTouch)
    {
        var area = gecko != null ? gecko.parent as RectTransform : null;
        if (area == null || rig == null) return null;

        var go = new GameObject("GeckoTouch", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(area, false);
        rt.anchorMin = gecko.anchorMin;
        rt.anchorMax = gecko.anchorMax;
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.SetSiblingIndex(gecko.GetSiblingIndex() + 1);
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // 투명해도 터치는 받는다

        var touch = go.AddComponent<GeckoTouch>();
        touch._rt      = rt;
        touch._gecko   = gecko;
        touch._rig     = rig;
        touch._onTouch = onTouch;
        touch.Follow();
        return touch;
    }

    private void LateUpdate()
    {
        Follow();
        CheckLongPress();
    }

    private void Follow()
    {
        if (_gecko == null || _rig == null) return;
        _rig.GetExtents(out float left, out float right);   // 발밑 기준 좌우 폭 (지금 방향·크기)
        float width = left + right;
        _rt.sizeDelta     = new Vector2(width, width * HEIGHT_RATIO);
        _rt.localRotation = _gecko.localRotation;             // 벽을 탈 때 함께 돈다 (피벗도 같은 발밑)
        Vector2 offset    = _gecko.localRotation * new Vector3((right - left) * 0.5f, 0f, 0f);
        _rt.anchoredPosition = _gecko.anchoredPosition + offset;
    }

    // ── 길게 누르기 (유대 Lv.5 손바닥) ────────────────────────

    public const float LONG_PRESS      = 0.6f;   // [TBD] 초
    public const float LONG_PRESS_SLOP = 24f;    // 누르는 동안 이만큼(화면 픽셀) 넘게 움직이면 취소

    /// <summary>게코를 길게 눌렀다 (누른 곳 월드 좌표). 불리면 이어지는 짧은 누르기 반응은 건너뛴다</summary>
    public Action<Vector3> LongPressed;

    private PointerEventData _press;
    private Vector2          _pressPos;
    private float            _pressTime;
    private bool             _longFired;

    public void OnPointerDown(PointerEventData e)
    {
        _press     = e;
        _pressPos  = e.position;
        _pressTime = Time.unscaledTime;
        _longFired = false;
    }

    public void OnPointerUp(PointerEventData e) => _press = null;

    private void CheckLongPress()
    {
        if (_press == null || LongPressed == null) return;
        if ((_press.position - _pressPos).magnitude > LONG_PRESS_SLOP) { _press = null; return; }
        if (Time.unscaledTime - _pressTime < LONG_PRESS) return;

        var e = _press;
        _press     = null;
        _longFired = true;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_rt, e.position, e.pressEventCamera, out Vector3 world))
            LongPressed(world);
    }

    private void OnDisable() => _press = null;

    public void OnPointerClick(PointerEventData e)
    {
        if (_longFired)   // 길게 누른 손을 뗀 것 — 부위 반응은 하지 않는다
        {
            _longFired = false;
            return;
        }
        if (_onTouch == null || _rig == null) return;
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(_rt, e.position, e.pressEventCamera, out Vector3 world)) return;
        _onTouch(ZoneAt(_rig, world), world);
    }

    // ── 판정 (자가 검사가 직접 부른다) ────────────────────────

    /// <summary>월드 좌표가 게코의 어느 부위인가. 어느 그림에도 안 걸리면 가장 가까운 큰 부위(머리·몸통·꼬리 끝)</summary>
    public static GeckoTouchZone ZoneAt(GeckoRig rig, Vector3 world)
    {
        foreach (var (part, zone, center, size) in ORDER)
        {
            if (!rig.TryPartLocal(part, world, UV_MARGIN, out Vector2 uv)) continue;   // uv만 받아 오고
            if (!InBox(uv, center, size)) continue;                                    // 판정은 박스로
            return part == GeckoPartId.Tail ? TailZone(uv.x) : zone;
        }

        float head = (rig.PartWorldPoint(GeckoPartId.Head, new Vector2(0.5f, 0.5f)) - world).sqrMagnitude;
        float body = (rig.PartWorldPoint(GeckoPartId.Body, new Vector2(0.5f, 0.5f)) - world).sqrMagnitude;
        float tail = (rig.PartWorldPoint(GeckoPartId.Tail, new Vector2(0.3f, 0.5f)) - world).sqrMagnitude;
        if (head <= body && head <= tail) return GeckoTouchZone.Head;
        return tail < body ? GeckoTouchZone.TailTip : GeckoTouchZone.Body;
    }

    /// <summary>꼬리 그림 안 가로 위치(0 = 왼쪽 끝, 오른쪽을 보는 그림 기준) → 뿌리·끝</summary>
    public static GeckoTouchZone TailZone(float u) => u >= TAIL_TIP_SPLIT ? GeckoTouchZone.TailBase : GeckoTouchZone.TailTip;

    /// <summary>파츠 그림 안 위치(uv)가 그 부위의 판정 박스 안인가</summary>
    private static bool InBox(Vector2 uv, Vector2 center, Vector2 size)
        => Mathf.Abs(uv.x - center.x) <= size.x * 0.5f && Mathf.Abs(uv.y - center.y) <= size.y * 0.5f;

    /// <summary>이 파츠의 판정 박스 (자가 검사용). 순서표에 없으면 false</summary>
    public static bool TryBoxOf(GeckoPartId part, out Vector2 center, out Vector2 size)
    {
        foreach (var o in ORDER)
            if (o.part == part) { center = o.center; size = o.size; return true; }
        center = Half;
        size   = Vector2.one;
        return false;
    }

    /// <summary>판정 순서 (작을수록 먼저). 순서표에 없는 파츠는 -1</summary>
    public static int PriorityOf(GeckoPartId part)
    {
        for (int i = 0; i < ORDER.Length; i++)
            if (ORDER[i].part == part) return i;
        return -1;
    }
}
