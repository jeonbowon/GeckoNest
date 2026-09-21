using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 머리 그림을 목에서 휘게 한다 (2026-09-21). GeckoRig가 머리 Image에 붙이고 매 프레임 머리 각도를 넣는다.
///
/// 머리는 목(관절)을 중심으로 **통째로** 돌아서, 고개를 들면 머리 그림 아래의 곧게 잘린 선이 몸통에서 떨어져
/// 턱 밑에 틈이 보였고("목이 잘린 것 같다"), 숙이면 뒷머리 등 돌기가 몸통 돌기와 두 겹으로 겹쳤다.
/// → 머리 오브젝트는 그대로 머리 각도만큼 돌리되(눈·입·혀가 따라가게), **그림의 목 쪽은 되돌려** 몸통에 붙여 둔다.
///    그림 왼쪽(목) NECK_START까지는 몸통 각도 그대로, FACE_START부터는 머리 각도 그대로, 그 사이는 부드럽게 휜다.
///
/// 오른쪽을 보는 그림 기준(목이 왼쪽). 좌우 반전은 부모(_visual) 크기로 하므로 여기서는 신경 쓰지 않는다.
/// 최종 그림으로 머리만 돌려 합성해 보고 정한 값이다 — 그림을 바꾸면 눈 자리가 FACE_START보다 오른쪽인지 확인할 것.
/// </summary>
[RequireComponent(typeof(Graphic))]
public class GeckoNeckBend : BaseMeshEffect
{
    public const float NECK_START = 0.12f;   // [TBD] 그림 가로 비율 — 여기까지는 몸통에 붙어 있다
    public const float FACE_START = 0.46f;   // [TBD] 여기부터 얼굴 전체가 머리 각도대로 (눈 53% · 입 76%가 표정 그림과 어긋나지 않게)
    private const int  COLUMNS    = 18;      // 휘는 세로 띠 수

    private float _angle;   // 몸통 대비 머리 각도 (도)

    /// <summary>몸통 대비 머리 각도 (GeckoPose 머리의 angle) — 바뀌었을 때만 그림을 다시 만든다</summary>
    public void SetAngle(float degrees)
    {
        if (Mathf.Abs(degrees - _angle) < 0.01f) return;
        _angle = degrees;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    /// <summary>그림 가로 위치(0 = 목 쪽 끝, 1 = 주둥이) → 머리 각도를 얼마나 따르는가 (0 = 몸통 그대로, 1 = 머리 각도 그대로)</summary>
    public static float FollowAt(float u)
    {
        float t = Mathf.Clamp01((u - NECK_START) / (FACE_START - NECK_START));
        return t * t * (3f - 2f * t);
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        // 기본 Image(Simple)는 사각형 하나 = 꼭짓점 4개. 그 밖의 모양은 건드리지 않는다
        if (!IsActive() || Mathf.Abs(_angle) < 0.01f || vh.currentVertCount != 4) return;

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
        for (int c = 0; c <= COLUMNS; c++)
        {
            float u  = (float)c / COLUMNS;
            float x  = Mathf.Lerp(min.x, max.x, u);
            float ux = Mathf.Lerp(uvMin.x, uvMax.x, u);

            // 머리 전체가 _angle만큼 돌아 있으니 목 쪽은 그만큼 되돌린다 — 회전 중심은 관절(= 이 그림의 피벗, 로컬 원점)
            var back = Quaternion.Euler(0f, 0f, -_angle * (1f - FollowAt(u)));
            vh.AddVert(back * new Vector3(x, min.y), color, new Vector2(ux, uvMin.y));
            vh.AddVert(back * new Vector3(x, max.y), color, new Vector2(ux, uvMax.y));
        }
        for (int c = 0; c < COLUMNS; c++)
        {
            int b = c * 2;
            vh.AddTriangle(b, b + 1, b + 3);
            vh.AddTriangle(b, b + 3, b + 2);
        }
    }
}
