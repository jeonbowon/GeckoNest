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
public class GeckoTouch : MonoBehaviour, IPointerClickHandler
{
    private const float HEIGHT_RATIO   = 0.55f;   // [TBD] 터치 영역 높이 = 게코 폭 × 이 값 (발밑부터)
    private const float TAIL_TIP_SPLIT = 0.55f;   // 꼬리 그림에서 이 비율(왼쪽 끝 0)보다 오른쪽은 뿌리 [TBD]

    // 판정 순서 — (파츠, 부위, 여유 비율: 그림 가로·세로를 각각 이만큼 넓혀서 본다)
    // 여유는 이웃 부위를 덮지 않을 만큼만 — 프록시 그림 기준 눈 여유 45%면 입 가운데까지 눈이 되고,
    // 꼬리 여유 20%면 몸통 뒤쪽 1/3이 꼬리가 된다 (2026-09-17 그림 크기로 계산해 조정)
    private static readonly (GeckoPartId part, GeckoTouchZone zone, float pad)[] ORDER =
    {
        (GeckoPartId.EyeL,         GeckoTouchZone.Eye,      0.20f),
        (GeckoPartId.EyeR,         GeckoTouchZone.Eye,      0.20f),
        (GeckoPartId.Mouth,        GeckoTouchZone.Mouth,    0.25f),
        (GeckoPartId.LegFrontNear, GeckoTouchZone.FrontLeg, 0.15f),
        (GeckoPartId.LegFrontFar,  GeckoTouchZone.FrontLeg, 0.15f),
        (GeckoPartId.LegBackNear,  GeckoTouchZone.BackLeg,  0.15f),
        (GeckoPartId.LegBackFar,   GeckoTouchZone.BackLeg,  0.15f),
        (GeckoPartId.Head,         GeckoTouchZone.Head,     0f),
        (GeckoPartId.Tail,         GeckoTouchZone.TailTip,  0.05f),  // 뿌리·끝은 TailZone으로 나눈다
        (GeckoPartId.Body,         GeckoTouchZone.Body,     0f),
    };

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

    private void LateUpdate() => Follow();

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

    public void OnPointerClick(PointerEventData e)
    {
        if (_onTouch == null || _rig == null) return;
        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(_rt, e.position, e.pressEventCamera, out Vector3 world)) return;
        _onTouch(ZoneAt(_rig, world), world);
    }

    // ── 판정 (자가 검사가 직접 부른다) ────────────────────────

    /// <summary>월드 좌표가 게코의 어느 부위인가. 어느 그림에도 안 걸리면 가장 가까운 큰 부위(머리·몸통·꼬리 끝)</summary>
    public static GeckoTouchZone ZoneAt(GeckoRig rig, Vector3 world)
    {
        foreach (var (part, zone, pad) in ORDER)
        {
            if (!rig.TryPartLocal(part, world, pad, out Vector2 uv)) continue;
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

    /// <summary>판정 순서 (작을수록 먼저). 순서표에 없는 파츠는 -1</summary>
    public static int PriorityOf(GeckoPartId part)
    {
        for (int i = 0; i < ORDER.Length; i++)
            if (ORDER[i].part == part) return i;
        return -1;
    }
}
