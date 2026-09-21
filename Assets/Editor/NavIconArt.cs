using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 하단 탭 아이콘 5종(상점·게코·꾸미기·보상·설정) PNG를 만든다. 메뉴 없이 배치 실행 전용:
/// Unity.exe -batchmode -nographics -projectPath (프로젝트) -executeMethod NavIconArt.GenerateBatch -logFile (로그)
///
/// - 탭 바가 어두운 반투명(0.1, 0.13, 0.1 · 59%)이고 글자가 흰색이라 **흰 실루엣**으로 그린다
/// - 이미 있는 PNG는 덮어쓰지 않는다 (최종 그림 보호). 다시 그리려면 PNG와 .meta를 지우고 실행
/// - 최종 그림은 같은 이름·같은 크기(96x96, 투명 배경)로 바꿔 끼우면 된다.
///   HomeUIController.EnsureNavButtonIcons가 Resources/Icons에서 이름으로 찾는다
/// </summary>
public static class NavIconArt
{
    public const  string DIR  = "Assets/_Game/Resources/Icons";
    private const int    SIZE = 96;

    /// <summary>탭 순서 — HomeUIController.NAV_ICONS와 같아야 한다</summary>
    public static readonly string[] NAMES = { "tab_store", "tab_gecko", "tab_terrarium", "tab_reward", "tab_settings" };

    public static void GenerateBatch()
    {
        try
        {
            Generate();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[NavIconArt] 실패: " + e);
            EditorApplication.Exit(1);
        }
    }

    public static void Generate()
    {
        Directory.CreateDirectory(DIR);
        MakeIcon(NAMES[0], Store);
        MakeIcon(NAMES[1], Gecko);
        MakeIcon(NAMES[2], Terrarium);
        MakeIcon(NAMES[3], Reward);
        MakeIcon(NAMES[4], Settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"[NavIconArt] 하단 탭 아이콘 {NAMES.Length}종 완료 — {DIR}");
    }

    // ── 파일 ─────────────────────────────────────────────────

    // draw(u, v) = 그 자리의 흰색 농도 0~1 (u·v는 0~1, 왼쪽 아래가 0,0)
    private static void MakeIcon(string name, Func<float, float, float> draw)
    {
        string path = $"{DIR}/{name}.png";
        if (!File.Exists(path))
        {
            var px = new Color[SIZE * SIZE];
            for (int y = 0; y < SIZE; y++)
                for (int x = 0; x < SIZE; x++)
                {
                    float a = Mathf.Clamp01(draw((x + 0.5f) / SIZE, (y + 0.5f) / SIZE));
                    px[x + y * SIZE] = new Color(1f, 1f, 1f, a);
                }

            var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[NavIconArt] 그림 생성 — {path} ({SIZE}x{SIZE})");
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || !importer.alphaIsTransparency)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.maxTextureSize      = 512;
            importer.SaveAndReimport();
        }
    }

    // ── 도형 (부호 있는 거리 — 0보다 작으면 안쪽) ─────────────

    private const float AA = 1.6f / SIZE;   // 경계 부드럽게

    private static float Fill(float d) => Mathf.Clamp01(0.5f - d / AA);
    private static float Or(float a, float b)  => Mathf.Min(a, b);
    private static float Cut(float a, float b) => Mathf.Max(a, -b);      // a에서 b를 판다
    private static float And(float a, float b) => Mathf.Max(a, b);
    private static float Outline(float d, float half) => Mathf.Abs(d) - half;   // 테두리만 남긴다

    private static float Circle(float u, float v, float cx, float cy, float r)
        => Mathf.Sqrt((u - cx) * (u - cx) + (v - cy) * (v - cy)) - r;

    private static float Ellipse(float u, float v, float cx, float cy, float rx, float ry)
    {
        float dx = (u - cx) / rx, dy = (v - cy) / ry;
        return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
    }

    private static float Box(float u, float v, float cx, float cy, float hw, float hh, float round = 0f)
    {
        float dx = Mathf.Abs(u - cx) - hw + round;
        float dy = Mathf.Abs(v - cy) - hh + round;
        float out2 = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
        return out2 + Mathf.Min(Mathf.Max(dx, dy), 0f) - round;
    }

    // 기울인 타원 — deg만큼 돌린 자리에서 잰다
    private static float EllipseRot(float u, float v, float cx, float cy, float rx, float ry, float deg)
    {
        float a = deg * Mathf.Deg2Rad;
        float dx = u - cx, dy = v - cy;
        float x =  dx * Mathf.Cos(a) + dy * Mathf.Sin(a);
        float y = -dx * Mathf.Sin(a) + dy * Mathf.Cos(a);
        return Ellipse(x + cx, y + cy, cx, cy, rx, ry);
    }

    // 잎 — 타원 두 개를 위아래로 어긋나게 겹쳐 양 끝을 뾰족하게. deg 방향으로 뻗는다
    private static float Leaf(float u, float v, float cx, float cy, float len, float wide, float deg)
    {
        float a = deg * Mathf.Deg2Rad;
        float dx = u - cx, dy = v - cy;
        float x =  dx * Mathf.Cos(a) + dy * Mathf.Sin(a);
        float y = -dx * Mathf.Sin(a) + dy * Mathf.Cos(a);
        return And(Ellipse(x, y, 0f,  wide, len, wide * 2.2f),
                   Ellipse(x, y, 0f, -wide, len, wide * 2.2f));
    }

    // 굵기 있는 선분 (양 끝은 둥글다)
    private static float Bar(float u, float v, float ax, float ay, float bx, float by, float r)
    {
        float vx = bx - ax, vy = by - ay;
        float wx = u - ax,  wy = v - ay;
        float len2 = vx * vx + vy * vy;
        float t = len2 <= 1e-6f ? 0f : Mathf.Clamp01((wx * vx + wy * vy) / len2);
        float dx = wx - vx * t, dy = wy - vy * t;
        return Mathf.Sqrt(dx * dx + dy * dy) - r;
    }

    // ── 아이콘 ───────────────────────────────────────────────

    // 장바구니 — 손잡이 아치 + 사다리꼴 몸통에 세로 홈 두 줄
    private static float Store(float u, float v)
    {
        float handle = And(Outline(Circle(u, v, 0.5f, 0.60f, 0.17f), 0.035f), -(v - 0.60f));

        // 사다리꼴: 아래로 갈수록 좁아진다 — 기울어진 옆면 두 개로 자른다
        float body = Box(u, v, 0.5f, 0.42f, 0.36f, 0.17f, 0.05f);
        body = And(body,  (Mathf.Abs(u - 0.5f) - 0.36f) + (0.59f - v) * 0.42f);

        float slot1 = Box(u, v, 0.38f, 0.42f, 0.022f, 0.10f, 0.02f);
        float slot2 = Box(u, v, 0.62f, 0.42f, 0.022f, 0.10f, 0.02f);
        body = Cut(Cut(body, slot1), slot2);

        return Fill(Or(handle, body));
    }

    // 게코 — **옆에서 본** 실루엣 (오른쪽을 본다. 게코 그림 규칙과 같은 방향)
    // 위에서 본 모습은 좌우대칭이라 팔다리 벌린 사람으로 읽힌다 (세 번 고쳐 보고 옆모습으로 바꿨다).
    // 몸통이 가로로 눕고 꼬리가 위로 말리면 사람과 겹칠 수가 없다.
    private static float Gecko(float u, float v)
    {
        float body = Ellipse(u, v, 0.450f, 0.470f, 0.195f, 0.080f);
        float head = Ellipse(u, v, 0.695f, 0.525f, 0.108f, 0.072f);
        float neck = Bar(u, v, 0.590f, 0.485f, 0.670f, 0.515f, 0.062f);

        // 꼬리 — 몸통 왼쪽 끝에서 아래로 빠졌다가 위로 말린다
        float tail = Or(Or(Or(Bar(u, v, 0.275f, 0.470f, 0.175f, 0.515f, 0.052f),
                              Bar(u, v, 0.175f, 0.515f, 0.115f, 0.605f, 0.038f)),
                           Or(Bar(u, v, 0.115f, 0.605f, 0.135f, 0.700f, 0.028f),
                              Bar(u, v, 0.135f, 0.700f, 0.225f, 0.748f, 0.019f))),
                        Bar(u, v, 0.225f, 0.748f, 0.302f, 0.722f, 0.012f));

        // 가까운 쪽 다리 둘 — 아래로 내려가 발이 바깥으로
        float near = Or(Or(Bar(u, v, 0.615f, 0.430f, 0.628f, 0.318f, 0.030f),
                           Bar(u, v, 0.628f, 0.318f, 0.700f, 0.298f, 0.024f)),
                        Or(Bar(u, v, 0.322f, 0.430f, 0.302f, 0.318f, 0.030f),
                           Bar(u, v, 0.302f, 0.318f, 0.230f, 0.298f, 0.024f)));

        // 먼 쪽 다리 둘 — 가늘고 짧게 (네 다리가 있다는 것만 보이게)
        float far = Or(Or(Bar(u, v, 0.545f, 0.425f, 0.548f, 0.352f, 0.021f),
                          Bar(u, v, 0.548f, 0.352f, 0.602f, 0.336f, 0.017f)),
                       Or(Bar(u, v, 0.392f, 0.425f, 0.389f, 0.352f, 0.021f),
                          Bar(u, v, 0.389f, 0.352f, 0.335f, 0.336f, 0.017f)));

        float d = Or(Or(Or(body, head), neck), Or(tail, Or(near, far)));
        return Fill(Cut(d, Circle(u, v, 0.728f, 0.548f, 0.026f)));   // 눈
    }

    // 꾸미기 — 화분에 심은 잎 두 장
    // (옆면을 기울이면 화분이 **아래 화살표**로 보였다 — 옆면은 곧게, 위 테두리로 화분을 만든다.
    //  가운데 잎을 더하면 화살 깃처럼 보여서 뺐다)
    private static float Terrarium(float u, float v)
    {
        float pot = Box(u, v, 0.5f, 0.235f, 0.165f, 0.125f, 0.028f);   // 옆면 곧게
        float rim = Box(u, v, 0.5f, 0.375f, 0.220f, 0.050f, 0.022f);

        float stem  = Bar(u, v, 0.5f, 0.38f, 0.5f, 0.60f, 0.022f);
        float leafL = Leaf(u, v, 0.352f, 0.640f, 0.165f, 0.060f,  30f);
        float leafR = Leaf(u, v, 0.648f, 0.715f, 0.165f, 0.060f, -30f);

        return Fill(Or(Or(pot, rim), Or(stem, Or(leafL, leafR))));
    }

    // 보상 — 선물 상자: 상자 + 뚜껑 + 세로 리본 홈 + 기울인 고리 두 개
    private static float Reward(float u, float v)
    {
        float d = Or(Box(u, v, 0.5f, 0.325f, 0.285f, 0.185f, 0.035f),    // 상자
                     Box(u, v, 0.5f, 0.545f, 0.330f, 0.070f, 0.030f));   // 뚜껑

        // 리본은 그리는 게 아니라 **양옆을 파서** 남긴다 (같은 흰색이라 더해서는 안 보인다)
        d = Cut(d, Box(u, v, 0.5f - 0.072f, 0.42f, 0.016f, 0.31f));
        d = Cut(d, Box(u, v, 0.5f + 0.072f, 0.42f, 0.016f, 0.31f));

        // 고리 두 개 — 바깥으로 기울여야 귀가 아니라 리본으로 읽힌다
        float bow = Or(Outline(EllipseRot(u, v, 0.392f, 0.705f, 0.090f, 0.052f, -28f), 0.024f),
                       Outline(EllipseRot(u, v, 0.608f, 0.705f, 0.090f, 0.052f,  28f), 0.024f));
        float knot = Circle(u, v, 0.5f, 0.665f, 0.040f);

        return Fill(Or(d, Or(bow, knot)));
    }

    // 설정 — 톱니바퀴
    private static float Settings(float u, float v)
    {
        float d = Circle(u, v, 0.5f, 0.5f, 0.235f);

        const int TEETH = 8;
        for (int i = 0; i < TEETH; i++)
        {
            float a  = i * Mathf.PI * 2f / TEETH;
            float cx = 0.5f + Mathf.Cos(a) * 0.285f;
            float cy = 0.5f + Mathf.Sin(a) * 0.285f;
            d = Or(d, Bar(u, v, 0.5f + Mathf.Cos(a) * 0.19f, 0.5f + Mathf.Sin(a) * 0.19f, cx, cy, 0.062f));
        }

        d = Cut(d, Circle(u, v, 0.5f, 0.5f, 0.095f));   // 가운데 구멍
        return Fill(d);
    }
}
