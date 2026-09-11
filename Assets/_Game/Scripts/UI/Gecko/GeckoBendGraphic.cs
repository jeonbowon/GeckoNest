using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>
/// 휘어지는 UI 그림 — 꼬리 전용.
/// 스프라이트를 길이 방향으로 잘게 나눈 띠 메시로 그리고, 관절마다 각도를 줘서 구부린다.
///
/// 규칙
/// - RectTransform 피벗 = 꼬리 뿌리(관절). 피벗에서 더 먼 가장자리가 꼬리 끝.
/// - 피벗 뒤쪽(몸통 속으로 파고드는 겹침 부분)은 굽히지 않는다.
/// - SetBend 각도 +는 "위로 말림". 꼬리가 왼쪽을 향하든 오른쪽을 향하든 같다.
/// 스프라이트만 바꾸면 그대로 동작하므로 프록시 → 최종 아트 교체에 영향이 없다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class GeckoBendGraphic : MaskableGraphic
{
    [SerializeField] private Sprite _sprite;
    [SerializeField, Range(2, 32)] private int _segments = 12;

    private float[] _bend;

    public int Segments => _segments;

    public Sprite sprite
    {
        get => _sprite;
        set
        {
            if (_sprite == value) return;
            _sprite = value;
            SetAllDirty();
        }
    }

    public override Texture mainTexture => _sprite != null ? _sprite.texture : s_WhiteTexture;

    /// <summary>관절별 굽힘 각도(도). 길이가 모자라면 남은 관절은 0.</summary>
    public void SetBend(float[] anglesDeg)
    {
        if (anglesDeg == null) return;
        EnsureBuffer();

        bool changed = false;
        for (int i = 0; i < _bend.Length; i++)
        {
            float a = i < anglesDeg.Length ? anglesDeg[i] : 0f;
            if (Mathf.Abs(_bend[i] - a) > 0.01f)
            {
                _bend[i] = a;
                changed = true;
            }
        }
        if (changed) SetVerticesDirty();
    }

    private void EnsureBuffer()
    {
        if (_bend == null || _bend.Length != _segments) _bend = new float[_segments];
    }

    private readonly List<Vector3> _pos = new List<Vector3>();
    private readonly List<Vector2> _uv  = new List<Vector2>();
    private readonly List<int>     _idx = new List<int>();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_sprite == null) return;

        Rect r = rectTransform.rect;
        if (r.width <= 0f || r.height <= 0f) return;
        EnsureBuffer();

        BuildStrip(r, DataUtility.GetOuterUV(_sprite), _bend, _pos, _uv, _idx);

        Color32 col = color;
        for (int i = 0; i < _pos.Count; i++) vh.AddVert(_pos[i], col, _uv[i]);
        for (int i = 0; i + 2 < _idx.Count; i += 3) vh.AddTriangle(_idx[i], _idx[i + 1], _idx[i + 2]);
    }

    /// <summary>
    /// 굽은 띠 메시 계산 (순수 함수 — 테스트용으로 분리).
    /// r = 피벗 기준 로컬 사각형, uv = (uMin, vMin, uMax, vMax), bend = 관절별 각도(도)
    /// </summary>
    public static void BuildStrip(Rect r, Vector4 uv, float[] bend, List<Vector3> pos, List<Vector2> uvs, List<int> idx)
    {
        pos.Clear();
        uvs.Clear();
        idx.Clear();
        int n = bend != null ? bend.Length : 0;
        if (n == 0) return;

        float s      = (-r.xMin > r.xMax) ? -1f : 1f;       // 꼬리 끝 방향
        float length = s < 0f ? -r.xMin : r.xMax;
        float back   = s < 0f ?  r.xMax : -r.xMin;
        float seg    = length / n;

        // 관절 k에서의 누적 각도 Φk (라디안)
        // 방향 = (s·cosΦ, s·sinΦ), 단면 = (-y·sinΦ, y·cosΦ) → 굽혀도 "위"가 위로 유지된다
        Vector2 p = Vector2.zero;
        float phiPrev = 0f;
        float phi = 0f;

        for (int k = 0; k <= n; k++)
        {
            if (k < n) phi = phiPrev + s * bend[k] * Mathf.Deg2Rad;

            // 단면 각도 = 앞뒤 구간의 평균 → 이음매가 벌어지거나 겹치지 않는다
            float psi = (k < n) ? (phiPrev + phi) * 0.5f : phiPrev;
            float sin = Mathf.Sin(psi);
            float cos = Mathf.Cos(psi);

            float u = Mathf.Lerp(uv.x, uv.z, Mathf.InverseLerp(r.xMin, r.xMax, s * seg * k));
            pos.Add(new Vector3(p.x - r.yMin * sin, p.y + r.yMin * cos));
            uvs.Add(new Vector2(u, uv.y));
            pos.Add(new Vector3(p.x - r.yMax * sin, p.y + r.yMax * cos));
            uvs.Add(new Vector2(u, uv.w));

            if (k < n)
            {
                p += new Vector2(s * Mathf.Cos(phi), s * Mathf.Sin(phi)) * seg;
                phiPrev = phi;
            }
        }

        for (int k = 0; k < n; k++)
        {
            int b0 = k * 2, t0 = b0 + 1, b1 = b0 + 2, t1 = b0 + 3;
            idx.Add(b0); idx.Add(t0); idx.Add(t1);
            idx.Add(t1); idx.Add(b1); idx.Add(b0);
        }

        // 피벗 뒤쪽 겹침 부분 — 뿌리 단면에 이어 붙인다
        if (back > 0.5f)
        {
            float xb = -s * back;
            float ub = Mathf.Lerp(uv.x, uv.z, Mathf.InverseLerp(r.xMin, r.xMax, xb));
            int bi = pos.Count;
            pos.Add(new Vector3(xb, r.yMin)); uvs.Add(new Vector2(ub, uv.y));
            pos.Add(new Vector3(xb, r.yMax)); uvs.Add(new Vector2(ub, uv.w));
            idx.Add(bi); idx.Add(bi + 1); idx.Add(1);
            idx.Add(1);  idx.Add(0);      idx.Add(bi);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        _segments = Mathf.Clamp(_segments, 2, 32);
        EnsureBuffer();
        SetVerticesDirty();
    }
#endif
}
