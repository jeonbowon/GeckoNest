using System;
using UnityEngine;

/// <summary>
/// 연출용 작은 그림을 코드로 만든다 (하트·물방울·반짝이·허물 조각·말풍선 …).
/// 흰색 바탕에 은은한 명암만 넣어 두고, 색은 Image.color로 입힌다.
/// 한 번 만들면 앱이 끝날 때까지 재사용한다. 그림 파일로 바꾸고 싶으면 Resources/Fx/{이름} 에 넣는다.
/// </summary>
public static class FxSprites
{
    private const int SIZE = 128;

    private static Sprite s_heart, s_drop, s_sparkle, s_dot, s_flake, s_puff, s_ring, s_bug, s_bubble, s_bubbleTail, s_crack, s_gift;

    /// <summary>알 껍질의 지그재그 금 (부화 연출) — 세로로 긴 꺾인 선, 가운데가 굵다</summary>
    public static Sprite Crack      => s_crack      ??= Load("crack")       ?? Make("fx_crack",   SIZE, CrackShape,   0f);

    /// <summary>어덜트의 선물 — 리본 틈이 있는 상자와 위의 나비매듭 (색은 Image.color로)</summary>
    public static Sprite Gift       => s_gift       ??= Load("gift")        ?? Make("fx_gift",    SIZE, GiftShape,    0.18f);
    public static Sprite Heart      => s_heart      ??= Load("heart")       ?? Make("fx_heart",   SIZE, HeartShape,   0.10f);
    public static Sprite Drop       => s_drop       ??= Load("drop")        ?? Make("fx_drop",    SIZE, DropShape,    0.14f);
    public static Sprite Sparkle    => s_sparkle    ??= Load("sparkle")     ?? Make("fx_sparkle", SIZE, SparkleShape, 0f);
    public static Sprite Dot        => s_dot        ??= Load("dot")         ?? MakeSoftDot();
    public static Sprite Flake      => s_flake      ??= Load("flake")       ?? Make("fx_flake",   SIZE, FlakeShape,   0.08f);
    public static Sprite Puff       => s_puff       ??= Load("puff")        ?? Make("fx_puff",    SIZE, PuffShape,    0.12f);
    public static Sprite Ring       => s_ring       ??= Load("ring")        ?? Make("fx_ring",    SIZE, RingShape,    0f);
    public static Sprite Bug        => s_bug        ??= Load("bug")         ?? MakeBug();
    public static Sprite Bubble     => s_bubble     ??= Load("bubble")      ?? MakeBubble();
    public static Sprite BubbleTail => s_bubbleTail ??= Load("bubble_tail") ?? Make("fx_bubble_tail", 64, TailShape, 0f);

    private static Sprite Load(string name) => Resources.Load<Sprite>("Fx/" + name);

    // ── 모양 (좌표: 가운데 (0,0), 가장자리 ±1. 음수 = 안쪽) ────

    private static float HeartShape(Vector2 p)
    {
        // Inigo Quilez 하트 거리 함수. 원래 도형은 아래 꼭짓점 y=0, 위쪽 끝 y≈1.1 → 텍스처 가운데에 80% 크기로 맞춘다
        const float K = 0.72f;
        Vector2 q = new Vector2(Mathf.Abs(p.x) * K, p.y * K + 0.55f);
        float d;
        if (q.y + q.x > 1f)
            d = (q - new Vector2(0.25f, 0.75f)).magnitude - Mathf.Sqrt(2f) / 4f;
        else
        {
            float a = (q - new Vector2(0f, 1f)).sqrMagnitude;
            float b = (q - 0.5f * Mathf.Max(q.x + q.y, 0f) * Vector2.one).sqrMagnitude;
            d = Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(q.x - q.y);
        }
        return d / K;
    }

    private static float DropShape(Vector2 p)
    {
        // 아래는 원, 위로 갈수록 가늘어져 뾰족해지는 물방울 (원 ∪ 원뿔)
        const float CY = -0.25f, R = 0.55f, TOP = 0.9f;
        float circle = (p - new Vector2(0f, CY)).magnitude - R;
        if (p.y <= CY) return circle;
        float u    = Mathf.Clamp01((TOP - p.y) / (TOP - CY));
        float cone = Mathf.Max(Mathf.Abs(p.x) - R * Mathf.Pow(u, 0.85f), p.y - TOP);
        return Mathf.Min(circle, cone);
    }

    private static float SparkleShape(Vector2 p)
    {
        // 네 갈래 반짝이 (오목한 별)
        float x = Mathf.Abs(p.x), y = Mathf.Abs(p.y);
        float s = Mathf.Sqrt(x) + Mathf.Sqrt(y);
        return s * s - 0.85f;
    }

    private static float FlakeShape(Vector2 p)
    {
        // 불규칙한 허물 조각 — 각도에 따라 반지름이 흔들리는 도형
        float a = Mathf.Atan2(p.y, p.x);
        float r = 0.62f + 0.12f * Mathf.Sin(a * 3f + 0.7f) + 0.07f * Mathf.Sin(a * 5f + 2.1f) + 0.05f * Mathf.Sin(a * 9f);
        return p.magnitude - r;
    }

    private static float PuffShape(Vector2 p)
    {
        float d = Circle(p, new Vector2(-0.32f, -0.12f), 0.42f);
        d = Mathf.Min(d, Circle(p, new Vector2(0.30f, -0.14f), 0.40f));
        d = Mathf.Min(d, Circle(p, new Vector2(0.02f,  0.22f), 0.48f));
        return d;
    }

    private static float RingShape(Vector2 p) => Mathf.Abs(p.magnitude - 0.78f) - 0.1f;

    // 선물 상자 — 몸통·뚜껑 사이와 가운데 세로에 리본 틈, 위에 고리 두 개
    private static float GiftShape(Vector2 p)
    {
        float body = RoundBox(p - new Vector2(0f, -0.40f), new Vector2(0.58f, 0.40f), 0.06f);
        float lid  = RoundBox(p - new Vector2(0f,  0.12f), new Vector2(0.68f, 0.12f), 0.05f);
        float box  = Mathf.Min(body, lid);
        float gapV = 0.05f - Mathf.Abs(p.x);                   // 세로 리본 틈
        float gapH = 0.035f - Mathf.Abs(p.y + 0.015f);         // 뚜껑 아래 틈
        box = Mathf.Max(box, Mathf.Max(gapV, gapH));
        float bowL = Mathf.Abs((p - new Vector2(-0.20f, 0.46f)).magnitude - 0.17f) - 0.06f;
        float bowR = Mathf.Abs((p - new Vector2( 0.20f, 0.46f)).magnitude - 0.17f) - 0.06f;
        return Mathf.Min(box, Mathf.Min(bowL, bowR));
    }

    private static float RoundBox(Vector2 p, Vector2 half, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(r, r);
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    private static readonly Vector2[] CRACK_POINTS =
    {
        new Vector2(-0.05f,  0.92f), new Vector2( 0.20f,  0.48f), new Vector2(-0.16f,  0.08f),
        new Vector2( 0.22f, -0.34f), new Vector2(-0.02f, -0.90f),
    };

    private static float CrackShape(Vector2 p)
    {
        // 지그재그 금 — 꺾인 선까지의 거리. 가운데가 굵고 위아래 끝으로 갈수록 가늘다
        float best = float.MaxValue;
        for (int i = 0; i < CRACK_POINTS.Length - 1; i++)
        {
            Vector2 a = CRACK_POINTS[i], ab = CRACK_POINTS[i + 1] - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            Vector2 q = a + ab * t;
            float half = Mathf.Lerp(0.08f, 0.025f, Mathf.Abs(q.y));
            best = Mathf.Min(best, (p - q).magnitude - half);
        }
        return best;
    }

    private static float TailShape(Vector2 p)
    {
        // 말풍선 꼬리 — 아래를 향한 둥근 삼각형
        Vector2 a = new Vector2(-0.9f, 1f), b = new Vector2(0.9f, 1f), c = new Vector2(0f, -0.8f);
        return Triangle(p, a, b, c) - 0.08f;
    }

    private static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

    private static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
        Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
        Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
        Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
        Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
        float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
        Vector2 d = Vector2.Min(Vector2.Min(
            new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
            new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
            new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
        return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
    }

    // ── 굽기 ─────────────────────────────────────────────────

    /// <summary>
    /// 모양 함수를 텍스처로. 경계는 수치 기울기로 거리를 보정해 1픽셀 안티에일리어싱.
    /// shade = 아래쪽을 살짝 어둡게 (입체감). 위쪽에는 작은 하이라이트가 들어간다.
    /// </summary>
    private static Sprite Make(string name, int size, Func<Vector2, float> shape, float shade)
    {
        var px = new Color32[size * size];
        float pix = 2f / size;   // 한 픽셀의 도형 좌표 크기
        float h = pix * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                float f = shape(p);

                float gx = (shape(p + new Vector2(h, 0f)) - shape(p - new Vector2(h, 0f))) / (2f * h);
                float gy = (shape(p + new Vector2(0f, h)) - shape(p - new Vector2(0f, h))) / (2f * h);
                float g  = Mathf.Max(0.2f, Mathf.Sqrt(gx * gx + gy * gy));
                float cov = Mathf.Clamp01(0.5f - f / g / pix);
                if (cov <= 0f) continue;

                float lum = 1f - shade * Mathf.Clamp01(0.5f - p.y * 0.5f);
                // 위쪽 왼편에 작은 하이라이트
                float hl = Mathf.Clamp01(1f - (p - new Vector2(-0.28f, 0.34f)).magnitude / 0.22f);
                lum = Mathf.Min(1f, lum + hl * shade * 1.5f);

                byte v = (byte)(lum * 255f);
                px[x + y * size] = new Color32(v, v, v, (byte)(cov * 255f));
            }
        }
        return ToSprite(name, size, size, px, Vector4.zero);
    }

    private static Sprite MakeSoftDot()
    {
        const int s = 64;
        var px = new Color32[s * s];
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float r = new Vector2((x + 0.5f) / s * 2f - 1f, (y + 0.5f) / s * 2f - 1f).magnitude;
            float a = 1f - Mathf.SmoothStep(0.15f, 1f, r);
            px[x + y * s] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
        }
        return ToSprite("fx_dot", s, s, px, Vector4.zero);
    }

    // 먹이 아이콘이 없을 때 쓰는 작은 귀뚜라미
    private static Sprite MakeBug()
    {
        Func<Vector2, float> body = p =>
        {
            float d = Ellipse(p, new Vector2(0.05f, 0f), new Vector2(0.62f, 0.3f));
            d = Mathf.Min(d, Ellipse(p, new Vector2(-0.62f, 0.06f), new Vector2(0.22f, 0.2f)));   // 머리
            return d;
        };
        var sprite = Make("fx_bug", SIZE, body, 0.25f);
        // 색은 따뜻한 갈색으로 굽는다 (먹이는 틴트하지 않는다)
        var tex = sprite.texture;
        var px = tex.GetPixels32();
        for (int i = 0; i < px.Length; i++)
        {
            float l = px[i].r / 255f;
            px[i] = new Color32((byte)(168 * l), (byte)(112 * l), (byte)(62 * l), px[i].a);
        }
        tex.SetPixels32(px);
        tex.Apply(false, false);
        return sprite;
    }

    private static float Ellipse(Vector2 p, Vector2 c, Vector2 r)
    {
        Vector2 q = p - c;
        float k0 = new Vector2(q.x / r.x, q.y / r.y).magnitude;
        float k1 = new Vector2(q.x / (r.x * r.x), q.y / (r.y * r.y)).magnitude;
        if (k1 < 1e-6f) return -Mathf.Min(r.x, r.y);
        return k0 * (k0 - 1f) / k1;
    }

    // 둥근 사각형 9-slice 말풍선 — 흰 바탕, 따뜻한 회색 테두리
    private static Sprite MakeBubble()
    {
        const int s = 96;
        const float R = 40f, LINE = 3f;
        var px = new Color32[s * s];
        var fill = new Color(1f, 1f, 1f, 1f);
        var line = new Color(0.90f, 0.85f, 0.80f, 1f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float qx = Mathf.Abs(x + 0.5f - s * 0.5f) - (s * 0.5f - R);
            float qy = Mathf.Abs(y + 0.5f - s * 0.5f) - (s * 0.5f - R);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            float d = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - R;   // 둥근 사각형 거리
            float cov = Mathf.Clamp01(0.5f - d);
            if (cov <= 0f) continue;
            float edge = Mathf.Clamp01(0.5f - (Mathf.Abs(d + LINE * 0.5f) - LINE * 0.5f));
            Color c = Color.Lerp(fill, line, edge);
            px[x + y * s] = new Color(c.r, c.g, c.b, cov);
        }
        return ToSprite("fx_bubble", s, s, px, new Vector4(R + 4f, R + 4f, R + 4f, R + 4f));
    }

    private static Sprite ToSprite(string name, int w, int h, Color32[] px, Vector4 border)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name       = name,
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
            hideFlags  = HideFlags.DontUnloadUnusedAsset,   // 씬 전환 시 자동 정리 대상에서 제외
        };
        tex.SetPixels32(px);
        tex.Apply(false, false);
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        sprite.name      = name;
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }
}
