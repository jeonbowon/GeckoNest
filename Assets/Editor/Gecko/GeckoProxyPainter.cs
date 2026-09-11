using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프록시(임시) 게코 그림을 코드로 그린다.
/// 거리 함수(SDF)로 모양을 정의하고 1픽셀 안티에일리어싱으로 칠한다.
/// 순수 계산만 하므로(Texture2D 미사용) Unity 밖에서도 검증할 수 있다.
///
/// 좌표: 텍스처 픽셀, 왼쪽 아래 (0,0), 위가 +y. 게코는 오른쪽을 본다.
/// </summary>
internal static class GeckoProxyPainter
{
    // ── 색 (회색 프록시 + 눈·입·혀만 구분용 색) ───────────────
    private static readonly Color C_BODY       = Hex("9E9B96");
    private static readonly Color C_HEAD       = Hex("A7A49F");
    private static readonly Color C_LEG        = Hex("9B9892");
    private static readonly Color C_TAIL       = Hex("989590");
    private static readonly Color C_BELLY      = Hex("C3C0BA");
    private static readonly Color C_SNOUT      = Hex("B8B5AF");
    private static readonly Color C_SPOT       = Hex("86827C");
    private static readonly Color C_LINE       = Hex("4A4743");
    private static readonly Color C_LINE_SOFT  = Hex("6E6B66");
    private static readonly Color C_PAD        = Hex("CDC9C2");
    private static readonly Color C_SOCKET     = Hex("7E7B76");
    private static readonly Color C_IRIS       = Hex("DDD5C3");
    private static readonly Color C_IRIS_RING  = Hex("B9B09C");
    private static readonly Color C_PUPIL      = Hex("1C1A18");
    private static readonly Color C_MOUTH_IN   = Hex("5A3036");
    private static readonly Color C_TONGUE     = Hex("CD909C");
    private static readonly Color C_TONGUE_LN  = Hex("8C5863");
    private static readonly Color C_SHED       = new Color(0.96f, 0.95f, 0.92f, 0.72f);
    private static readonly Color C_SHED_LINE  = Hex("D6D2CA");
    private static readonly Color C_CLEAR      = new Color(0f, 0f, 0f, 0f);
    private static readonly Color C_WHITE      = Color.white;

    // ── 캔버스 ───────────────────────────────────────────────

    public sealed class Canvas
    {
        public readonly int W, H;
        public readonly Color[] Px;
        public Canvas(int w, int h)
        {
            W = w;
            H = h;
            Px = new Color[w * h];
        }
    }

    public delegate float Sdf(Vector2 p);

    /// <summary>모양 채우기. d &lt; 0 이 안쪽. lineW &gt; 0 이면 안쪽 가장자리에 외곽선.</summary>
    public static void Fill(Canvas c, Sdf shape, Color fill, Color line, float lineW, float shade = 0f, Sdf clip = null)
    {
        for (int y = 0; y < c.H; y++)
        {
            for (int x = 0; x < c.W; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = shape(p);
                if (d > 1f) continue;

                float cov = Mathf.Clamp01(0.5f - d);
                if (clip != null) cov *= Mathf.Clamp01(0.5f - clip(p));
                if (cov <= 0f) continue;

                Color col = fill;
                if (shade != 0f)
                {
                    float k = 1f + shade * (p.y / c.H - 0.5f) * 2f;   // 위가 밝고 아래가 어둡다
                    col = new Color(col.r * k, col.g * k, col.b * k, col.a);
                }
                if (lineW > 0f)
                {
                    float o = Mathf.Clamp01(0.5f - (Mathf.Abs(d + lineW * 0.5f) - lineW * 0.5f));
                    col = Color.Lerp(col, new Color(line.r, line.g, line.b, col.a), o * line.a);
                }
                Blend(c, x + y * c.W, col, cov * col.a);
            }
        }
    }

    /// <summary>선 긋기 (모양의 경계를 width 두께로)</summary>
    public static void Stroke(Canvas c, Sdf shape, Color color, float width, Sdf clip = null)
        => Fill(c, p => Mathf.Abs(shape(p)) - width * 0.5f, color, C_CLEAR, 0f, 0f, clip);

    private static void Blend(Canvas c, int i, Color src, float a)
    {
        if (a <= 0f) return;
        Color dst = c.Px[i];
        float outA = a + dst.a * (1f - a);
        if (outA <= 0f) return;
        float k = dst.a * (1f - a);
        c.Px[i] = new Color(
            (src.r * a + dst.r * k) / outA,
            (src.g * a + dst.g * k) / outA,
            (src.b * a + dst.b * k) / outA,
            outA);
    }

    // ── 거리 함수 ────────────────────────────────────────────

    public static float Circle(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;

    public static float Ellipse(Vector2 p, Vector2 c, Vector2 r)
    {
        Vector2 q = p - c;
        float k0 = new Vector2(q.x / r.x, q.y / r.y).magnitude;
        float k1 = new Vector2(q.x / (r.x * r.x), q.y / (r.y * r.y)).magnitude;
        if (k1 < 1e-6f) return -Mathf.Min(r.x, r.y);
        return k0 * (k0 - 1f) / k1;
    }

    private static float SegmentDist(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        Vector2 pa = p - a, ba = b - a;
        t = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-6f, Vector2.Dot(ba, ba)));
        return (pa - ba * t).magnitude;
    }

    public static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r) => SegmentDist(p, a, b, out _) - r;

    /// <summary>굵기가 a→b로 변하는 캡슐</summary>
    public static float Taper(Vector2 p, Vector2 a, Vector2 b, float ra, float rb)
    {
        float d = SegmentDist(p, a, b, out float t);
        return d - Mathf.Lerp(ra, rb, t);
    }

    public static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 e0 = b - a, e1 = c - b, e2 = a - c;
        Vector2 v0 = p - a, v1 = p - b, v2 = p - c;
        Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
        Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
        Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
        float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
        Vector2 d0 = new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x));
        Vector2 d1 = new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x));
        Vector2 d2 = new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x));
        Vector2 d = Vector2.Min(Vector2.Min(d0, d1), d2);
        return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
    }

    public static float SmoothMin(float a, float b, float k)
    {
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(b, a, h) - k * h * (1f - h);
    }

    public static float Polyline(Vector2 p, IReadOnlyList<Vector2> pts)
    {
        float d = float.MaxValue;
        for (int i = 0; i + 1 < pts.Count; i++) d = Mathf.Min(d, SegmentDist(p, pts[i], pts[i + 1], out _));
        return d;
    }

    /// <summary>x0~x1 사이 포물선 호. bend &lt; 0 이면 가운데가 아래로(‿), &gt; 0 이면 위로(⌒)</summary>
    public static List<Vector2> Arc(float x0, float x1, float y, float bend, int n = 24)
    {
        var pts = new List<Vector2>(n);
        float mid = (x0 + x1) * 0.5f, half = (x1 - x0) * 0.5f;
        for (int i = 0; i < n; i++)
        {
            float x = Mathf.Lerp(x0, x1, i / (n - 1f));
            float u = (x - mid) / half;
            pts.Add(new Vector2(x, y + bend * (1f - u * u)));
        }
        return pts;
    }

    // ── 몸통 ─────────────────────────────────────────────────

    private static readonly Vector2 BODY_C = new Vector2(360f, 172f);
    private static readonly Vector2 BODY_R = new Vector2(330f, 132f);

    private static float Torso(Vector2 p) => Ellipse(p, BODY_C, BODY_R);

    public static Canvas Body()
    {
        var c = new Canvas(720, 380);
        var spikes = new List<Vector2[]>();
        for (float x = 84f; x <= 640f; x += 34f)
        {
            float u = (x - BODY_C.x) / BODY_R.x;
            float top = BODY_C.y + BODY_R.y * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
            float h = 24f * (0.55f + 0.45f * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)));
            spikes.Add(new[] { new Vector2(x - 13f, top - 10f), new Vector2(x + 13f, top - 10f), new Vector2(x - 6f, top + h) });
        }

        Fill(c, p =>
        {
            float d = Torso(p);
            foreach (var t in spikes) d = Mathf.Min(d, Triangle(p, t[0], t[1], t[2]));
            return d;
        }, C_BODY, C_LINE, 4f, 0.14f);

        Fill(c, p => Ellipse(p, new Vector2(360f, 118f), new Vector2(272f, 62f)), C_BELLY, C_CLEAR, 0f, 0.05f,
             p => Torso(p) + 6f);

        var spots = new[]
        {
            new Vector3(170, 212, 18), new Vector3(250, 242, 22), new Vector3(340, 252, 20), new Vector3(430, 247, 24),
            new Vector3(520, 232, 19), new Vector3(598, 206, 15), new Vector3(212, 172, 14), new Vector3(300, 196, 16),
            new Vector3(398, 202, 15), new Vector3(482, 186, 17), new Vector3(560, 170, 13),
        };
        foreach (var s in spots)
            Fill(c, p => Circle(p, new Vector2(s.x, s.y), s.z), C_SPOT, C_CLEAR, 0f, 0f, p => Torso(p) + 6f);

        return c;
    }

    public static Canvas ShedPatch()
    {
        var c = new Canvas(720, 380);
        var blobs = new[] { new Vector3(228, 236, 46), new Vector3(382, 266, 52), new Vector3(506, 214, 44), new Vector3(300, 160, 40) };

        Fill(c, p =>
        {
            float d = float.MaxValue;
            for (int b = 0; b < blobs.Length; b++)
            {
                for (int k = 0; k < 5; k++)
                {
                    float a  = Hash(b * 7 + k) * Mathf.PI * 2f;
                    float rr = blobs[b].z * (0.45f + 0.35f * Hash(b * 13 + k + 3));
                    Vector2 cc = new Vector2(blobs[b].x, blobs[b].y) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * blobs[b].z * 0.45f;
                    d = Mathf.Min(d, Circle(p, cc, rr));
                }
            }
            return d;
        }, C_SHED, C_SHED_LINE, 2.5f, 0f, p => Torso(p) + 3f);

        return c;
    }

    // ── 머리 ─────────────────────────────────────────────────

    private static readonly Vector2 HEAD_C = new Vector2(250f, 215f);
    private static readonly Vector2 HEAD_R = new Vector2(205f, 168f);
    private static readonly Vector2 JAW_C  = new Vector2(250f, 150f);
    private static readonly Vector2 JAW_R  = new Vector2(186f, 112f);

    public static readonly Vector2 EYE_L_ON_HEAD = new Vector2(175f, 232f);
    public static readonly Vector2 EYE_R_ON_HEAD = new Vector2(332f, 238f);
    public static readonly Vector2 MOUTH_ON_HEAD = new Vector2(250f, 112f);

    private static float Skull(Vector2 p) => SmoothMin(Ellipse(p, HEAD_C, HEAD_R), Ellipse(p, JAW_C, JAW_R), 30f);

    public static Canvas Head()
    {
        var c = new Canvas(500, 440);
        var spikes = new List<Vector2[]>();
        for (float deg = 22f; deg <= 158f; deg += 12f)
        {
            float a = deg * Mathf.Deg2Rad;
            Vector2 b = HEAD_C + new Vector2(HEAD_R.x * Mathf.Cos(a), HEAD_R.y * Mathf.Sin(a));
            Vector2 n = new Vector2(Mathf.Cos(a) / HEAD_R.x, Mathf.Sin(a) / HEAD_R.y).normalized;
            Vector2 t = new Vector2(-n.y, n.x);
            spikes.Add(new[] { b - t * 13f - n * 8f, b + t * 13f - n * 8f, b + n * 30f });
        }

        Fill(c, p =>
        {
            float d = Skull(p);
            foreach (var s in spikes) d = Mathf.Min(d, Triangle(p, s[0], s[1], s[2]));
            return d;
        }, C_HEAD, C_LINE, 4f, 0.12f);

        Fill(c, p => Ellipse(p, new Vector2(250f, 122f), new Vector2(150f, 72f)), C_SNOUT, C_CLEAR, 0f, 0f, p => Skull(p) + 6f);

        var spots = new[] { new Vector3(170, 330, 13), new Vector3(250, 352, 15), new Vector3(330, 328, 12), new Vector3(118, 280, 10), new Vector3(384, 290, 11) };
        foreach (var s in spots)
            Fill(c, p => Circle(p, new Vector2(s.x, s.y), s.z), C_SPOT, C_CLEAR, 0f, 0f, p => Skull(p) + 6f);

        // 눈이 앉을 자리 — 눈 파츠보다 살짝 커서 테두리가 그늘처럼 보인다
        Fill(c, p => Circle(p, EYE_L_ON_HEAD, 64f), C_SOCKET, C_CLEAR, 0f);
        Fill(c, p => Circle(p, EYE_R_ON_HEAD, 60f), C_SOCKET, C_CLEAR, 0f);

        Fill(c, p => Circle(p, new Vector2(228f, 160f), 5f), C_LINE, C_CLEAR, 0f);
        Fill(c, p => Circle(p, new Vector2(272f, 160f), 5f), C_LINE, C_CLEAR, 0f);
        return c;
    }

    // ── 눈 ───────────────────────────────────────────────────

    public static Canvas Eye(GeckoEye state)
    {
        var c = new Canvas(140, 140);
        Vector2 o = new Vector2(70f, 70f);
        const float R = 58f;

        if (state == GeckoEye.Closed || state == GeckoEye.Happy)
        {
            Fill(c, p => Circle(p, o, R), C_HEAD, C_LINE_SOFT, 2.5f, 0.06f);
            var arc = state == GeckoEye.Closed ? Arc(26f, 114f, 66f, -14f) : Arc(26f, 114f, 56f, 18f);
            Stroke(c, p => Polyline(p, arc), C_LINE, state == GeckoEye.Closed ? 5f : 6f);
            return c;
        }

        Fill(c, p => Circle(p, o, R), C_IRIS, C_LINE, 4f, 0.08f);
        Stroke(c, p => Circle(p, o, 44f), C_IRIS_RING, 3f, p => Circle(p, o, R - 4f));

        Vector2 pc = o;
        Vector2 pr = new Vector2(11f, 40f);
        switch (state)
        {
            case GeckoEye.LookLeft:  pc = new Vector2(50f, 70f); break;
            case GeckoEye.LookRight: pc = new Vector2(90f, 70f); break;
            case GeckoEye.LookUp:    pc = new Vector2(70f, 86f); pr = new Vector2(11f, 32f); break;
            case GeckoEye.Surprised: pr = new Vector2(9f, 9f); break;
            case GeckoEye.Sparkle:   pr = new Vector2(14f, 40f); break;
            case GeckoEye.Sleepy:    pr = new Vector2(7f, 34f); break;
        }
        Fill(c, p => Ellipse(p, pc, pr), C_PUPIL, C_CLEAR, 0f, 0f, p => Circle(p, o, R - 3f));

        if (state == GeckoEye.Sparkle)
        {
            Star(c, new Vector2(88f, 92f), 20f);
            Star(c, new Vector2(52f, 54f), 10f);
            Fill(c, p => Circle(p, new Vector2(100f, 68f), 4f), C_WHITE, C_CLEAR, 0f);
        }
        else
        {
            float big = state == GeckoEye.Surprised ? 13f : 11f;
            Fill(c, p => Circle(p, new Vector2(90f, 94f), big), C_WHITE, C_CLEAR, 0f);
            Fill(c, p => Circle(p, new Vector2(55f, 50f), 5f), new Color(1f, 1f, 1f, 0.9f), C_CLEAR, 0f);
        }

        if (state == GeckoEye.Sleepy)
        {
            // 위쪽을 눈꺼풀이 덮는다 (게임적 허용 — 실제 크레스티드는 눈꺼풀이 없다)
            Fill(c, p => Mathf.Max(Circle(p, o, R + 1f), 76f + 8f * (1f - Sq((p.x - 70f) / 58f)) - p.y),
                 C_HEAD, C_LINE, 4f, 0.05f);
        }
        return c;
    }

    private static void Star(Canvas c, Vector2 at, float size)
    {
        Fill(c, p => Mathf.Min(Ellipse(p, at, new Vector2(size, size * 0.22f)), Ellipse(p, at, new Vector2(size * 0.22f, size))),
             C_WHITE, C_CLEAR, 0f);
    }

    // ── 입 ───────────────────────────────────────────────────

    public static Canvas Mouth(GeckoMouth state)
    {
        var c = new Canvas(200, 120);
        switch (state)
        {
            case GeckoMouth.Closed:
                Stroke(c, p => Polyline(p, Arc(38f, 162f, 64f, -9f)), C_LINE, 5f);
                break;

            case GeckoMouth.Smile:
                Stroke(c, p => Polyline(p, Arc(36f, 164f, 68f, -18f)), C_LINE, 6f);
                Stroke(c, p => Capsule(p, new Vector2(34f, 70f), new Vector2(28f, 80f), 0f), C_LINE, 5f);
                Stroke(c, p => Capsule(p, new Vector2(166f, 70f), new Vector2(172f, 80f), 0f), C_LINE, 5f);
                break;

            case GeckoMouth.OpenSmall:
            case GeckoMouth.Drink:
            {
                Vector2 r = state == GeckoMouth.Drink ? new Vector2(20f, 13f) : new Vector2(30f, 17f);
                Sdf m = p => Ellipse(p, new Vector2(100f, 56f), r);
                Fill(c, m, C_MOUTH_IN, C_LINE, 4f);
                Fill(c, p => Ellipse(p, new Vector2(100f, 47f), new Vector2(r.x * 0.6f, r.y * 0.45f)), C_TONGUE, C_CLEAR, 0f, 0f, p => m(p) + 3f);
                break;
            }

            case GeckoMouth.OpenWide:
            {
                Sdf m = p => Mathf.Max(Ellipse(p, new Vector2(100f, 62f), new Vector2(62f, 44f)), p.y - 70f);
                Fill(c, m, C_MOUTH_IN, C_LINE, 5f);
                Fill(c, p => Ellipse(p, new Vector2(100f, 36f), new Vector2(34f, 16f)), C_TONGUE, C_CLEAR, 0f, 0f, p => m(p) + 4f);
                break;
            }

            case GeckoMouth.Chew:
            {
                var pts = new List<Vector2> { new Vector2(68f, 62f), new Vector2(84f, 55f), new Vector2(100f, 63f), new Vector2(116f, 55f), new Vector2(132f, 62f) };
                Stroke(c, p => Polyline(p, pts), C_LINE, 5f);
                break;
            }

            case GeckoMouth.Surprised:
                Fill(c, p => Ellipse(p, new Vector2(100f, 56f), new Vector2(20f, 26f)), C_MOUTH_IN, C_LINE, 4f);
                break;

            case GeckoMouth.Frown:
                Stroke(c, p => Polyline(p, Arc(45f, 155f, 50f, 12f)), C_LINE, 5f);
                break;
        }
        return c;
    }

    // ── 혀 ───────────────────────────────────────────────────

    public static Canvas Tongue1()
    {
        var c = new Canvas(100, 40);
        Fill(c, p => Taper(p, new Vector2(15f, 20f), new Vector2(92f, 20f), 14f, 13f), C_TONGUE, C_TONGUE_LN, 2.5f, 0.1f);
        Stroke(c, p => Capsule(p, new Vector2(24f, 20f), new Vector2(84f, 20f), 0f), new Color(C_TONGUE_LN.r, C_TONGUE_LN.g, C_TONGUE_LN.b, 0.5f), 2f);
        return c;
    }

    public static Canvas Tongue2()
    {
        var c = new Canvas(96, 44);
        Fill(c, p => SmoothMin(Taper(p, new Vector2(12f, 22f), new Vector2(70f, 22f), 13f, 12f), Circle(p, new Vector2(74f, 22f), 17f), 8f),
             C_TONGUE, C_TONGUE_LN, 2.5f, 0.1f);
        return c;
    }

    // ── 꼬리 ─────────────────────────────────────────────────

    private static Vector2 TailAt(float u) => new Vector2(600f - 550f * u, 130f + 102f * Mathf.Pow(u, 1.8f));
    private static float TailR(float u) => Mathf.Lerp(60f, 17f, Mathf.Pow(u, 0.9f));

    public static Canvas Tail()
    {
        var c = new Canvas(640, 300);
        const int N = 64;
        var pts = new Vector2[N];
        var rad = new float[N];
        for (int i = 0; i < N; i++)
        {
            float u = i / (N - 1f);
            pts[i] = TailAt(u);
            rad[i] = TailR(u);
        }

        float Tube(Vector2 p)
        {
            float d = float.MaxValue;
            for (int i = 0; i < N; i++) d = Mathf.Min(d, (p - pts[i]).magnitude - rad[i]);
            return d;
        }

        var spikes = new List<Vector2[]>();
        for (float u = 0.06f; u <= 0.56f; u += 0.05f)
        {
            Vector2 tan = (TailAt(u + 0.01f) - TailAt(u)).normalized;   // 꼬리 끝 방향
            Vector2 up  = new Vector2(tan.y, -tan.x);
            if (up.y < 0f) up = -up;
            Vector2 top = TailAt(u) + up * TailR(u);
            float s = Mathf.Lerp(12f, 6f, u / 0.56f);
            spikes.Add(new[] { top - tan * s * 0.9f - up * 6f, top + tan * s * 0.9f - up * 6f, top + up * s * 1.6f + tan * 3f });
        }

        Fill(c, p =>
        {
            float d = Tube(p);
            foreach (var s in spikes) d = Mathf.Min(d, Triangle(p, s[0], s[1], s[2]));
            return d;
        }, C_TAIL, C_LINE, 4f, 0.12f);

        foreach (float u in new[] { 0.12f, 0.24f, 0.36f, 0.48f, 0.6f, 0.72f })
        {
            Vector2 tan = (TailAt(u + 0.01f) - TailAt(u)).normalized;
            Vector2 up  = new Vector2(tan.y, -tan.x);
            if (up.y < 0f) up = -up;
            Vector2 cc = TailAt(u) + up * TailR(u) * 0.25f;
            float rr = TailR(u) * 0.33f;
            Fill(c, p => Circle(p, cc, rr), C_SPOT, C_CLEAR, 0f, 0f, p => Tube(p) + 5f);
        }

        // 꼬리 끝 접착 패드 — 크레스티드 게코의 특징
        Fill(c, p => Circle(p, TailAt(1f) + new Vector2(-4f, 2f), 20f), C_PAD, C_LINE, 3f, 0.08f);
        return c;
    }

    // ── 다리 ─────────────────────────────────────────────────

    // 게코 다리는 짧고 굵으며 옆으로 벌어진다 — 팔꿈치·무릎을 굽힌 형태
    public static Canvas LegFront()
    {
        var c = new Canvas(150, 220);
        Vector2 sh = new Vector2(66f, 192f), el = new Vector2(96f, 116f), wr = new Vector2(100f, 44f), ft = new Vector2(106f, 26f);
        var toes = new[] { new Vector2(144f, 12f), new Vector2(149f, 32f), new Vector2(135f, 50f), new Vector2(82f, 10f) };
        Limb(c, sh, 32f, el, 26f, 24f, wr, 19f, ft, toes, new[] { new Vector3(70, 166, 10), new Vector3(92, 128, 8) });
        return c;
    }

    public static Canvas LegBack()
    {
        var c = new Canvas(180, 220);
        Vector2 hip = new Vector2(84f, 192f), knee = new Vector2(40f, 118f), ank = new Vector2(84f, 44f), ft = new Vector2(96f, 26f);
        var toes = new[] { new Vector2(136f, 12f), new Vector2(142f, 32f), new Vector2(128f, 50f), new Vector2(72f, 10f) };
        Limb(c, hip, 38f, knee, 29f, 26f, ank, 19f, ft, toes, new[] { new Vector3(74, 168, 11), new Vector3(52, 136, 9) });
        return c;
    }

    private static void Limb(Canvas c, Vector2 a, float ra, Vector2 b, float rb1, float rb2, Vector2 w, float rw,
                             Vector2 foot, Vector2[] toes, Vector3[] spots)
    {
        Sdf limb = p =>
        {
            float d = Taper(p, a, b, ra, rb1);
            d = SmoothMin(d, Taper(p, b, w, rb2, rw), 10f);
            d = SmoothMin(d, Ellipse(p, foot, new Vector2(34f, 14f)), 10f);
            foreach (var t in toes) d = SmoothMin(d, Taper(p, foot, t, 9f, 6f), 6f);
            return d;
        };
        Fill(c, limb, C_LEG, C_LINE, 4f, 0.1f);
        foreach (var t in toes) Fill(c, p => Circle(p, t, 9f), C_PAD, C_LINE, 2.5f);   // 발가락 끝 패드
        foreach (var s in spots) Fill(c, p => Circle(p, new Vector2(s.x, s.y), s.z), C_SPOT, C_CLEAR, 0f, 0f, p => limb(p) + 5f);
    }

    // ── 그림자 ───────────────────────────────────────────────

    public static Canvas Shadow()
    {
        var c = new Canvas(512, 64);
        var center = new Vector2(256f, 32f);
        var r = new Vector2(240f, 24f);
        for (int y = 0; y < c.H; y++)
        {
            for (int x = 0; x < c.W; x++)
            {
                Vector2 q = new Vector2((x + 0.5f - center.x) / r.x, (y + 0.5f - center.y) / r.y);
                float k = q.magnitude;
                float a = 0.33f * (1f - SmoothStep(0.35f, 1f, k));
                if (a > 0f) c.Px[x + y * c.W] = new Color(0f, 0f, 0f, a);
            }
        }
        return c;
    }

    // ── 유틸 ─────────────────────────────────────────────────

    private static float Sq(float x) => x * x;

    private static float SmoothStep(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    private static float Hash(int n)
    {
        float s = Mathf.Sin(n * 12.9898f + 78.233f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    private static Color Hex(string h)
    {
        int v = Convert.ToInt32(h, 16);
        return new Color(((v >> 16) & 0xFF) / 255f, ((v >> 8) & 0xFF) / 255f, (v & 0xFF) / 255f, 1f);
    }
}

/// <summary>
/// 프록시 게코의 조립 규격 — 각 파츠의 관절 위치와 관절 비율.
/// 게코 발밑 중앙 (0,0) 기준 스킨 픽셀. 텍스처 크기와 함께 GeckoProxyPainter의 그림과 맞물린다.
/// </summary>
internal static class GeckoProxyLayout
{
    public const float ReferenceWidth = 1480f;

    public struct Part
    {
        public GeckoPartId id;
        public string  sprite;   // 텍스처 파일 이름 (확장자 제외)
        public Vector2 joint;    // 관절 위치
        public Vector2 pivot;    // 스프라이트 안 관절 비율 (0~1)
        public Vector2 scale;
        public float   shade;    // 1 = 원래 색, 작을수록 어둡게
    }

    private static Part P(GeckoPartId id, string sprite, float jx, float jy, float pxX, float pxY, float w, float h,
                          float scale = 1f, float shade = 1f, float scaleY = -1f)
        => new Part
        {
            id = id, sprite = sprite,
            joint = new Vector2(jx, jy),
            pivot = new Vector2(pxX / w, pxY / h),
            scale = new Vector2(scale, scaleY > 0f ? scaleY : scale),
            shade = shade,
        };

    public static readonly Part[] Parts =
    {
        P(GeckoPartId.Shadow,       "shadow_contact",  50f,   6f, 256f,  32f, 512f,  64f, 1.75f, 1f, 1.3f),
        P(GeckoPartId.Tail,         "tail",          -300f, 258f, 585f, 130f, 640f, 300f),
        P(GeckoPartId.LegBackFar,   "leg_back",      -118f, 204f,  84f, 192f, 180f, 220f, 0.93f, 0.84f),
        P(GeckoPartId.LegFrontFar,  "leg_front",      292f, 200f,  66f, 192f, 150f, 220f, 0.93f, 0.84f),
        P(GeckoPartId.Body,         "body",             0f, 245f, 360f, 171f, 720f, 380f),
        P(GeckoPartId.ShedPatch,    "shed_patch",       0f, 245f, 360f, 171f, 720f, 380f),
        P(GeckoPartId.LegBackNear,  "leg_back",      -200f, 192f,  84f, 192f, 180f, 220f),
        P(GeckoPartId.LegFrontNear, "leg_front",      205f, 192f,  66f, 192f, 150f, 220f),
        P(GeckoPartId.Head,         "head",           262f, 318f, 110f,  70f, 500f, 440f),
        P(GeckoPartId.EyeL,         "eye_open",       327f, 480f,  70f,  70f, 140f, 140f),
        P(GeckoPartId.EyeR,         "eye_open",       484f, 486f,  70f,  70f, 140f, 140f, 0.92f),
        P(GeckoPartId.Mouth,        "mouth_closed",   402f, 360f, 100f,  60f, 200f, 120f),
        P(GeckoPartId.Tongue1,      "tongue_01",      400f, 363f,  12f,  20f, 100f,  40f),
        P(GeckoPartId.Tongue2,      "tongue_02",      478f, 363f,  10f,  22f,  96f,  44f),
    };

    public static Part Get(GeckoPartId id)
    {
        foreach (var p in Parts) if (p.id == id) return p;
        return default;
    }
}
