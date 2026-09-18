using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// AI로 만든 게코 그림 한 장(`Textures/Gecko/Source/gecko_base.png`)을 파츠로 잘라 스킨을 만든다.
///
///   ① 흰 배경 제거 (가장자리부터 흰색을 따라 들어가며 지우고, 경계의 흰 기운을 뺀다)
///   ② 파츠별 사각형으로 잘라 1600×900 캔버스에 같은 자리로 저장 → 관절은 GeckoSkinImporter가 자동으로 잡는다
///   ③ 머리에서 눈·입을 지우고(주변 살빛으로 메움) 그 그림을 눈·입 파츠로 따로 저장
///   ④ 나머지 표정·혀·그림자는 코드로 그린다 (없는 표정은 GeckoSkin이 기본 표정으로 대신한다)
///   ⑤ 스킨 에셋 생성 + MainHome 게코에 적용
///
/// 배치 실행: Unity.exe -batchmode -executeMethod GeckoArtCutter.CutBatch
/// 자른 자리가 어색하면 아래 RECTS·CUT_PARTS 숫자(원본 픽셀, 왼쪽 위 기준)만 고치면 된다.
/// 몸통·다리는 서로 겹쳐 있어 사각형으로 자르면 옆 발가락이 섞이고 네모난 자른 자리가 걸을 때 드러나므로,
/// CUT_PARTS(지울 구역 + 씨앗점 + 관절)로 **그림의 외곽선을 따라** 잘라 낸다.
/// </summary>
public static class GeckoArtCutter
{
    private const string SRC_PATH = "Assets/_Game/Textures/Gecko/Source/gecko_base.png";
    private const string OUT_DIR  = "Assets/_Game/Textures/Gecko/Final";
    // 스킨 에셋 이름은 GeckoSkinImporter가 폴더 이름으로 정한다 (Final → GeckoSkin_Final)

    private const int CANVAS_W = 1600, CANVAS_H = 900;
    private const float EYE_COVER = 1.45f;   // 표정 판이 그려진 눈을 덮는 배율 (Expand와 같게)

    // 원본에서 사각형 그대로 잘라 낼 파츠 (원본 픽셀, 왼쪽 위가 (0,0))
    private static readonly (string name, int x0, int y0, int x1, int y1)[] RECTS =
    {
        ("tail",            55, 320,  470, 485),
        ("head",           895, 170, 1275, 375),
    };

    /// <summary>
    /// 몸통·다리는 사각형으로 자르면 안 된다 — 그림 한 장에서 서로 겹쳐 있어 옆 다리 발가락·배 아랫면이 같이 담기고,
    /// 무엇보다 **네모난 자른 자리가 걸을 때 드러난다**(다리가 관절을 중심으로 ±18° 돌기 때문).
    /// 그래서 파츠마다 ① 넉넉한 사각형 ② 지울 구역(다각형) ③ 안쪽 씨앗점을 주고,
    /// 씨앗에서 **어두운 외곽선을 벽으로 삼아** 살만 채운다 → 그림에 그려진 그 파츠의 윤곽대로 잘린다.
    /// grow(발가락 잇기) · keep(외곽선 두께만큼 넓히기)은 TraceLimb 참고.
    /// joint = 관절(어깨·허벅지) 자리, 원본 픽셀 — 걸을 때 이 점을 중심으로 돈다.
    /// </summary>
    private struct CutPart
    {
        public string name;
        public RectInt rect;            // 원본에서 잘라 올 자리 (왼쪽 위 기준)
        public Vector2Int seed;         // 파츠 안(살)의 한 점
        public Vector2Int[][] cuts;     // 지울 구역 (원본 좌표 다각형) — 외곽선이 없어 살이 이어진 곳을 막는다
        public Vector2 joint;           // 관절 자리 (원본 좌표)
        public int rounds;              // 외곽선을 넘어가며 이어 붙이는 횟수 (발가락용, 몸통은 0)
        public int keep;                // 마지막에 넓히는 픽셀 (몸통은 크게 — 다리 뒤를 메운다)
    }

    private static Vector2Int P(int x, int y) => new Vector2Int(x, y);

    // 가까운 쪽 다리는 몸통 앞에 그려지므로 그림의 윤곽대로 잘라야 하고,
    // 먼 쪽 다리는 몸통 뒤라서 위쪽을 넉넉히 담아 둔다 (몸통에 가려지고, 돌 때 빈틈을 메운다)
    private static readonly CutPart[] CUT_PARTS =
    {
        new CutPart
        {
            name   = "body",
            rect   = new RectInt(395, 230, 770, 270),        // 목·가슴 아랫면까지 (먼 앞다리 위 빈틈 방지)
            seed   = P(700, 330),
            cuts   = new[]
            {
                // 머리 그림과 겹치는 턱·볼 — 머리는 따로 움직이므로 몸통에 남기지 않는다
                new[] { P(996, 222), P(1172, 222), P(1172, 372), P(996, 372) },
            },
            joint  = new Vector2(697.5f, 362.5f),            // 예전 몸통 중심 그대로 (회전·숨쉬기 기준)
            rounds = 0,
            keep   = 22,                                     // 다리 뒤로 조금 넉넉히 — 다리가 돌 때 빈틈이 안 생긴다
        },
        new CutPart
        {
            name   = "leg_back_far",
            rect   = new RectInt(610, 456, 190, 94),      // 위쪽 여유는 몸통에 가려질 만큼만 (벽에서 다리를 벌리면 삐져나온다)
            seed   = P(700, 512),
            cuts   = new[] { new[] { P(788, 470), P(830, 470), P(830, 570), P(788, 570) } },   // 가까운 앞다리 (발가락 끝 785 뒤에서 자른다)
            joint  = new Vector2(660, 470),
            rounds = 4,
            keep   = 5,
        },
        new CutPart
        {
            name   = "leg_front_far",
            rect   = new RectInt(952, 428, 198, 154),     // 위쪽 여유는 몸통에 가려질 만큼만
            seed   = P(1050, 540),
            cuts   = new[] { new[] { P(920, 548), P(980, 548), P(980, 624), P(920, 624) } },   // 가까운 앞발 발가락만
            joint  = new Vector2(1000, 445),
            rounds = 4,
            keep   = 5,
        },
        new CutPart
        {
            name   = "leg_back_near",
            rect   = new RectInt(428, 352, 212, 264),
            seed   = P(530, 580),
            cuts   = new[]
            {
                // 허벅지 앞쪽 바깥 (옆구리·꼬리 밑동)
                new[] { P(410, 346), P(552, 352), P(505, 398), P(470, 470), P(470, 520), P(410, 520) },
                // 허벅지 위·뒤쪽 바깥 (등·배 아랫면) — 그림의 허벅지 외곽선을 따라간다
                new[]
                {
                    P(552, 352), P(660, 346), P(660, 528), P(580, 528), P(585, 515), P(591, 500),
                    P(603, 485), P(619, 470), P(617, 455), P(615, 425), P(603, 410), P(588, 395), P(570, 372),
                },
            },
            joint  = new Vector2(560, 392),
            rounds = 4,
            keep   = 5,
        },
        new CutPart
        {
            name   = "leg_front_near",
            rect   = new RectInt(780, 376, 200, 242),
            seed   = P(890, 585),
            cuts   = new[]
            {
                // 겨드랑이 위 (가슴·옆구리) — 외곽선이 없는 어깨는 겨드랑이에서 곧게 끊는다
                new[]
                {
                    P(776, 360), P(992, 360), P(992, 470), P(895, 470),
                    P(858, 406), P(832, 426), P(806, 452), P(788, 470), P(776, 470),
                },
                // 먼 쪽 앞다리 (발목 부근)
                new[] { P(966, 470), P(992, 470), P(992, 545), P(966, 545) },
                // 배 아랫면 외곽선 조각
                new[] { P(770, 462), P(800, 462), P(800, 500), P(770, 500) },
            },
            joint  = new Vector2(838, 432),
            rounds = 4,
            keep   = 5,
        },
    };

    private const int INK_LUM   = 108;   // 이 밝기보다 어두우면 외곽선 = 채우기를 막는 벽 [TBD]
    private const int GROW_STEP = 2;     // 한 번에 외곽선을 넘어가는 픽셀 [TBD]

    // 머리 안에서 지우고 따로 쓸 자리
    private static readonly RectInt EYE_RECT   = new RectInt(1025, 210, 110, 110);   // 가까운 쪽 눈
    private static readonly RectInt EYE_FAR    = new RectInt(1165, 205,  70,  70);   // 먼 쪽 눈 (반쯤 가림)
    private static readonly RectInt MOUTH_RECT = new RectInt(1075, 285, 175,  60);   // 입선

    private static readonly Color OUTLINE = new Color(0.42f, 0.26f, 0.19f);
    private static readonly Color TONGUE  = new Color(0.95f, 0.60f, 0.66f);

    // ── 실행 ─────────────────────────────────────────────────

    [MenuItem("Hako/Gecko/④ 그림 한 장 자르기 (Source/gecko_base.png)", priority = 4)]
    public static void CutMenu()
    {
        string report = Cut();
        EditorUtility.DisplayDialog("게코 그림 자르기", report, "확인");
    }

    public static void CutBatch()
    {
        try
        {
            Debug.Log("[GeckoArtCutter]\n" + Cut());
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[GeckoArtCutter] 실패: " + e);
            EditorApplication.Exit(1);
        }
    }

    /// <summary>배치 확인용 — MainHome 게코에 어떤 스킨이 붙어 있고 파츠가 몇 개인지 로그로 찍는다</summary>
    public static void CheckBatch()
    {
        try
        {
            EditorSceneManager.OpenScene(HOME_SCENE, OpenSceneMode.Single);
            var rig = UnityEngine.Object.FindFirstObjectByType<GeckoRig>(FindObjectsInactive.Include);
            var skin = rig != null ? new SerializedObject(rig).FindProperty("_skin").objectReferenceValue as GeckoSkin : null;
            Debug.Log(skin == null
                ? "[GeckoArtCutter] 확인 — 게코에 스킨이 붙어 있지 않습니다"
                : $"[GeckoArtCutter] 확인 — 스킨 {skin.name} · 파츠 {skin.parts.Count}개 · 눈 {skin.eyes.Count}종 · 입 {skin.mouths.Count}종 · 기준 폭 {skin.referenceWidth:0}");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[GeckoArtCutter] 확인 실패: " + e);
            EditorApplication.Exit(1);
        }
    }

    public static string Cut()
    {
        var log = new System.Text.StringBuilder();
        var src = LoadPng(SRC_PATH);
        if (src == null) return $"원본을 찾지 못했습니다: {SRC_PATH}";
        log.AppendLine($"원본 {src.width}×{src.height}");

        var px = src.GetPixels32();
        int w = src.width, h = src.height;
        FlipVertical(px, w, h);   // Unity 픽셀 배열은 아래가 0 → 그림처럼 위가 0이 되게 뒤집는다 (자를 자리도 위 기준)
        int removed = RemoveBackground(px, w, h);
        log.AppendLine($"배경 제거 {removed:N0} 픽셀");

        Directory.CreateDirectory(OUT_DIR);
        float scale = (float)CANVAS_W / w;                       // 원본 → 캔버스 배율
        float offY  = (CANVAS_H - h * scale) * 0.5f;             // 세로 가운데
        s_scale = scale; s_offY = offY;                          // 관절을 원본 좌표로 주려면 BuildSkin도 필요하다

        // 머리 그림은 눈·입이 그려진 채로 둔다 (지우면 외곽선까지 상한다).
        // 기본 표정은 빈 그림, 다른 표정은 살빛 판으로 덮고 그 위에 그린다.
        var eyePixels = Crop(px, w, h, EYE_RECT);
        Color eyeSkin   = SkinAround(px, w, h, EYE_RECT);
        Color mouthSkin = SkinAround(px, w, h, MOUTH_RECT);
        var eyeArea   = Expand(RectToCanvas(EYE_RECT, scale, offY), EYE_COVER);
        var mouthArea = Expand(RectToCanvas(MOUTH_RECT, scale, offY), 1.25f);
        SaveEmpty("eye_open", eyeArea);
        SaveEmpty("mouth_closed", mouthArea);

        foreach (var (name, x0, y0, x1, y1) in RECTS)
        {
            SaveCrop(px, w, h, new RectInt(x0, y0, x1 - x0, y1 - y0), scale, offY, name);
            log.AppendLine($"{name}  {x1 - x0}×{y1 - y0}");
        }

        foreach (var part in CUT_PARTS)
        {
            int kept = SaveTracedCrop(px, w, h, part, scale, offY);
            var r = s_placed.TryGetValue(part.name, out var pr) ? pr : new Rect();
            log.AppendLine($"{part.name}  {r.width:0}×{r.height:0} (남긴 픽셀 {kept:N0})");
        }

        // 코드로 그리는 파츠 — 눈 표정·입 모양·혀·그림자·허물
        var eyeCanvas   = eyeArea;
        var mouthCanvas = mouthArea;
        SaveEyeVariant("eye_closed",  eyeCanvas, Cover(eyeSkin, EyeArc(false)));
        SaveEyeVariant("eye_happy",   eyeCanvas, Cover(eyeSkin, EyeArc(true)));
        SaveEyeVariant("eye_sleepy",  eyeCanvas, Cover(eyeSkin, EyeLidded(eyePixels, EYE_RECT, scale, offY)));
        SaveEyeVariant("eye_sparkle", eyeCanvas, EyeSparkle(eyePixels, EYE_RECT, scale, offY));   // 그려진 눈에 딱 겹친다
        SaveMouth("mouth_smile",      mouthCanvas, MouthKind.Smile,     mouthSkin);
        SaveMouth("mouth_open_small", mouthCanvas, MouthKind.OpenSmall, mouthSkin);
        SaveMouth("mouth_open_wide",  mouthCanvas, MouthKind.OpenWide,  mouthSkin);
        SaveMouth("mouth_frown",      mouthCanvas, MouthKind.Frown,     mouthSkin);
        SaveTongues(mouthCanvas);
        SaveShadow(px, w, h, scale, offY);

        AssetDatabase.Refresh();
        ImportAsSprites();

        // 스킨 만들기 + 씬 적용 (파츠를 실제 크기로 잘랐으므로 관절은 여기서 직접 계산한다)
        var skin = BuildSkin(eyeArea, mouthArea, log);
        if (skin == null) { log.AppendLine("스킨 생성 실패"); return log.ToString(); }
        log.AppendLine(ApplyToScene(skin));
        return log.ToString();
    }

    // ── 배경 제거 ────────────────────────────────────────────

    /// <summary>가장자리에서 시작해 흰색이 이어지는 곳을 지우고, 경계의 흰 기운을 알파로 뺀다</summary>
    private static int RemoveBackground(Color32[] px, int w, int h)
    {
        const int WHITE = 190;      // 이 밝기 이상이고
        const int GRAY  = 20;       // 색 기운이 이만큼 안에 들면 배경 — 발밑 회색 그림자(밝기 212 안팎)까지 지운다.
                                    // 게코 몸에서 가장 옅은 곳도 색 기운이 40 이상이라 몸은 지워지지 않는다
        var outside = new bool[w * h];
        var stack = new Stack<int>();

        void Push(int i)
        {
            if (i < 0 || i >= px.Length || outside[i]) return;
            var c = px[i];
            int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            if (min < WHITE || max - min > GRAY) return;
            outside[i] = true;
            stack.Push(i);
        }

        for (int x = 0; x < w; x++) { Push(x); Push((h - 1) * w + x); }
        for (int y = 0; y < h; y++) { Push(y * w); Push(y * w + w - 1); }

        while (stack.Count > 0)
        {
            int i = stack.Pop();
            int x = i % w, y = i / w;
            if (x > 0)     Push(i - 1);
            if (x < w - 1) Push(i + 1);
            if (y > 0)     Push(i - w);
            if (y < h - 1) Push(i + w);
        }

        int removed = 0;
        for (int i = 0; i < px.Length; i++)
        {
            if (outside[i])
            {
                px[i] = new Color32(px[i].r, px[i].g, px[i].b, 0);
                removed++;
                continue;
            }
            // 배경과 맞닿은 밝은 픽셀 = 외곽 흐림 → 밝을수록 투명하게 (흰 테두리 제거)
            int x = i % w, y = i / w;
            bool nearOutside = (x > 0 && outside[i - 1]) || (x < w - 1 && outside[i + 1])
                            || (y > 0 && outside[i - w]) || (y < h - 1 && outside[i + w]);
            if (!nearOutside) continue;
            var c = px[i];
            float lum = (c.r + c.g + c.b) / 3f / 255f;
            float a = Mathf.Clamp01((0.96f - lum) / 0.20f);
            px[i] = new Color32(c.r, c.g, c.b, (byte)(a * 255f));
        }
        return removed;
    }

    // ── 자르기 · 저장 ────────────────────────────────────────

    private static Color32[] Crop(Color32[] px, int w, int h, RectInt r)
    {
        var outPx = new Color32[r.width * r.height];
        for (int y = 0; y < r.height; y++)
        for (int x = 0; x < r.width; x++)
        {
            int sx = r.x + x, sy = r.y + y;
            outPx[x + y * r.width] = (sx >= 0 && sx < w && sy >= 0 && sy < h) ? px[sx + sy * w] : new Color32(0, 0, 0, 0);
        }
        return outPx;
    }

    /// <summary>원본 사각형 → 캔버스 사각형 (둘 다 왼쪽 위 기준)</summary>
    private static Rect RectToCanvas(RectInt r, float scale, float offY)
        => new Rect(r.x * scale, r.y * scale + offY, r.width * scale, r.height * scale);

    private static void SaveCrop(Color32[] px, int w, int h, RectInt r, float scale, float offY, string name)
    {
        var canvas = NewCanvas();
        Rect dst = RectToCanvas(r, scale, offY);
        int x0 = Mathf.FloorToInt(dst.x), y0 = Mathf.FloorToInt(dst.y);
        int cw = Mathf.CeilToInt(dst.width), ch = Mathf.CeilToInt(dst.height);

        for (int y = 0; y < ch; y++)
        for (int x = 0; x < cw; x++)
        {
            int cx = x0 + x, cy = y0 + y;
            if (cx < 0 || cx >= CANVAS_W || cy < 0 || cy >= CANVAS_H) continue;
            canvas[cx + cy * CANVAS_W] = Sample(px, w, h, cx / scale, (cy - offY) / scale);   // 둘 다 위가 0
        }
        SaveTrimmed(canvas, name);
    }

    /// <summary>
    /// 몸통·다리 한 짝을 **그림에 그려진 윤곽대로** 잘라 저장한다.
    /// 사각형 안에서 지울 구역을 뺀 뒤, 씨앗에서 어두운 외곽선을 벽으로 삼아 살만 채운다 (TraceLimb).
    /// 돌려주는 값은 남긴 픽셀 수.
    /// </summary>
    private static int SaveTracedCrop(Color32[] px, int w, int h, CutPart part, float scale, float offY)
    {
        var r = part.rect;
        int rw = r.width, rh = r.height;
        var keep = TraceLimb(px, w, h, part, out int kept);
        if (kept == 0)
        {
            Debug.LogWarning($"[GeckoArtCutter] {part.name}: 씨앗점 ({part.seed.x},{part.seed.y})이 살이 아닙니다 — 자리를 확인하세요");
            return 0;
        }

        var canvas = NewCanvas();
        Rect dst = RectToCanvas(r, scale, offY);
        int cx0 = Mathf.FloorToInt(dst.x), cy0 = Mathf.FloorToInt(dst.y);
        int cw = Mathf.CeilToInt(dst.width), ch = Mathf.CeilToInt(dst.height);

        for (int y = 0; y < ch; y++)
        for (int x = 0; x < cw; x++)
        {
            int cx = cx0 + x, cy = cy0 + y;
            if (cx < 0 || cx >= CANVAS_W || cy < 0 || cy >= CANVAS_H) continue;
            int sx = Mathf.RoundToInt(cx / scale), sy = Mathf.RoundToInt((cy - offY) / scale);
            int lx = sx - r.x, ly = sy - r.y;
            if (lx < 0 || ly < 0 || lx >= rw || ly >= rh || !keep[lx + ly * rw]) continue;
            canvas[cx + cy * CANVAS_W] = Sample(px, w, h, cx / scale, (cy - offY) / scale);
        }
        SaveTrimmed(canvas, part.name);
        return kept;
    }

    /// <summary>
    /// 파츠 윤곽 찾기 — ① 씨앗에서 **외곽선을 벽으로** 살을 채우고 ② 선을 조금씩 넘어가며 다시 채워 발가락을 잇고
    /// ③ 마지막에 외곽선 두께만큼 넓힌다. 사각형을 넘지 않고, 지울 구역과 배경은 처음부터 뺀다.
    /// </summary>
    private static bool[] TraceLimb(Color32[] px, int w, int h, CutPart part, out int kept)
    {
        var r = part.rect;
        int rw = r.width, rh = r.height;
        var ok  = new bool[rw * rh];
        var ink = new bool[rw * rh];

        for (int y = 0; y < rh; y++)
        for (int x = 0; x < rw; x++)
        {
            int sx = r.x + x, sy = r.y + y;
            if (sx < 0 || sx >= w || sy < 0 || sy >= h) continue;
            var c = px[sx + sy * w];
            if (c.a <= 8) continue;                                  // 배경
            bool cut = false;
            foreach (var poly in part.cuts)
                if (InPolygon(poly, sx, sy)) { cut = true; break; }
            if (cut) continue;
            int j = x + y * rw;
            ok[j] = true;
            ink[j] = (c.r + c.g + c.b) / 3 < INK_LUM;
        }

        var core = new bool[rw * rh];
        int seedX = part.seed.x - r.x, seedY = part.seed.y - r.y;
        kept = 0;
        if (seedX < 0 || seedY < 0 || seedX >= rw || seedY >= rh) return core;
        int seed = seedX + seedY * rw;
        if (!ok[seed] || ink[seed]) return core;

        var stack = new Stack<int>();
        stack.Push(seed);
        core[seed] = true;
        SpreadFlesh(stack, core, ok, ink, rw, rh);

        for (int i = 0; i < part.rounds; i++)
        {
            Grow(core, ok, rw, rh, GROW_STEP);                       // 외곽선을 조금 넘어간다
            for (int j = 0; j < core.Length; j++) if (core[j]) stack.Push(j);
            SpreadFlesh(stack, core, ok, ink, rw, rh);               // 넘어간 자리에서 다시 살을 채운다 (발가락)
        }
        Grow(core, ok, rw, rh, part.keep);

        for (int j = 0; j < core.Length; j++) if (core[j]) kept++;
        return core;
    }

    /// <summary>외곽선(ink)을 넘지 않고 이어진 살을 채운다</summary>
    private static void SpreadFlesh(Stack<int> stack, bool[] core, bool[] ok, bool[] ink, int rw, int rh)
    {
        while (stack.Count > 0)
        {
            int i = stack.Pop();
            int x = i % rw, y = i / rw;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= rw || ny >= rh) continue;
                int q = nx + ny * rw;
                if (core[q] || !ok[q] || ink[q]) continue;
                core[q] = true;
                stack.Push(q);
            }
        }
    }

    /// <summary>채운 자리를 px 픽셀만큼 넓힌다 (사각형·배경·지울 구역 밖으로는 못 나간다)</summary>
    private static void Grow(bool[] core, bool[] ok, int rw, int rh, int px)
    {
        var add = new List<int>();
        for (int k = 0; k < px; k++)
        {
            add.Clear();
            for (int y = 0; y < rh; y++)
            for (int x = 0; x < rw; x++)
            {
                int j = x + y * rw;
                if (core[j] || !ok[j]) continue;
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= rw || ny >= rh) continue;
                    if (core[nx + ny * rw]) { near = true; break; }
                }
                if (near) add.Add(j);
            }
            foreach (int j in add) core[j] = true;
        }
    }

    /// <summary>점이 다각형 안인지 (원본 좌표)</summary>
    private static bool InPolygon(Vector2Int[] poly, int x, int y)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > y) == (poly[j].y > y)) continue;
            float t = (float)(y - poly[i].y) / (poly[j].y - poly[i].y);
            if (x < poly[i].x + t * (poly[j].x - poly[i].x)) inside = !inside;
        }
        return inside;
    }

    private static Color32 Sample(Color32[] px, int w, int h, float sx, float sy)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(sx), 0, w - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(sy), 0, h - 1);
        return px[x + y * w];
    }

    /// <summary>
    /// 타원 안을 지우고 **그 바깥 살빛**으로 메운다. expand = 타원을 이만큼 더 키워 지운다(눈·입 외곽선까지 지우려고).
    /// 각 픽셀에서 여덟 방향으로 나가 처음 만나는 성한 색을 거리 반비례로 섞는다 — 지운 색이 다시 번지지 않는다.
    /// </summary>
    private static void Inpaint(Color32[] px, int w, int h, RectInt r, float expand)
    {
        var center = r.center;
        float rx = r.width * 0.5f * expand, ry = r.height * 0.5f * expand;

        bool InHole(int x, int y)
        {
            float u = (x - center.x) / rx, v = (y - center.y) / ry;
            return u * u + v * v <= 1f;
        }

        var hole = new List<int>();
        for (int y = Mathf.Max(0, Mathf.FloorToInt(center.y - ry)); y <= Mathf.Min(h - 1, Mathf.CeilToInt(center.y + ry)); y++)
        for (int x = Mathf.Max(0, Mathf.FloorToInt(center.x - rx)); x <= Mathf.Min(w - 1, Mathf.CeilToInt(center.x + rx)); x++)
            if (InHole(x, y)) hole.Add(x + y * w);

        if (hole.Count == 0) return;
        var inHole = new bool[px.Length];
        foreach (int i in hole) inHole[i] = true;

        // 구멍에 닿은 짙은 선(눈 테두리·입선)까지 함께 지운다 — 남으면 메운 색이 어두워진다
        const int DARK = 165;
        float reach = Mathf.Max(rx, ry) * 1.9f;
        var grow = new Queue<int>(hole);
        while (grow.Count > 0)
        {
            int i = grow.Dequeue();
            int x = i % w, y = i / w;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                int j = nx + ny * w;
                if (inHole[j]) continue;
                var c = px[j];
                if (c.a < 200) continue;
                if ((c.r + c.g + c.b) / 3 >= DARK) continue;                                  // 살빛이면 여기까지
                if (Vector2.Distance(new Vector2(nx, ny), center) > reach) continue;           // 너무 멀리 번지지 않게
                inHole[j] = true;
                hole.Add(j);
                grow.Enqueue(j);
            }
        }

        // 가장자리(구멍 바로 바깥)의 평균 살빛으로 먼저 채운 뒤, 여러 번 평균을 내어 매끄럽게 잇는다
        double sr = 0, sg = 0, sb = 0, sa = 0;
        int n = 0;
        foreach (int i in hole)
        {
            int x = i % w, y = i / w;
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                int j = (x + dx) + (y + dy) * w;
                if (j < 0 || j >= px.Length || inHole[j]) continue;
                var c = px[j];
                if (c.a < 200) continue;
                sr += c.r; sg += c.g; sb += c.b; sa += c.a; n++;
            }
        }
        if (n == 0) return;
        var seed = new Color32((byte)(sr / n), (byte)(sg / n), (byte)(sb / n), (byte)(sa / n));
        foreach (int i in hole) px[i] = seed;

        var buf = new Color32[hole.Count];
        for (int pass = 0; pass < 220; pass++)
        {
            for (int k = 0; k < hole.Count; k++)
            {
                int i = hole[k], x = i % w, y = i / w;
                int rs = 0, gs = 0, bs = 0, asum = 0, cnt = 0;
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int j = (x + dx) + (y + dy) * w;
                    if (j < 0 || j >= px.Length) continue;
                    var c = px[j];
                    if (!inHole[j] && c.a < 200) continue;   // 그림 밖은 섞지 않는다
                    rs += c.r; gs += c.g; bs += c.b; asum += c.a; cnt++;
                }
                buf[k] = cnt == 0 ? px[i] : new Color32((byte)(rs / cnt), (byte)(gs / cnt), (byte)(bs / cnt), (byte)(asum / cnt));
            }
            for (int k = 0; k < hole.Count; k++) px[hole[k]] = buf[k];
        }
    }

    // ── 코드로 그리는 파츠 ────────────────────────────────────

    private delegate Color32 Painter(float u, float v);   // u,v = 그 사각형 안 0~1

    private static void SaveEyeVariant(string name, Rect area, Painter painter) => SavePainted(name, area, painter);
    private static void SaveMouth(string name, Rect area, MouthKind kind, Color skin)
        => SavePainted(name, area, Cover(skin, MouthPainter(kind)));

    private static Rect Expand(Rect r, float k)
        => new Rect(r.center.x - r.width * k * 0.5f, r.center.y - r.height * k * 0.5f, r.width * k, r.height * k);

    /// <summary>그려진 눈·입을 살빛 타원으로 덮고 그 위에 표정을 그린다 (가장자리는 부드럽게)</summary>
    private static Painter Cover(Color skin, Painter over) => (u, v) =>
    {
        float d = new Vector2((u - 0.5f) * 2f, (v - 0.5f) * 2f).magnitude;
        float patch = Mathf.Clamp01((1f - d) / 0.28f);
        var top = over(u, v);
        float ta = top.a / 255f;
        var baseColor = Rgba(skin, patch);
        if (ta <= 0f) return baseColor;
        var mixed = Color32.Lerp(baseColor, new Color32(top.r, top.g, top.b, 255), ta);
        mixed.a = (byte)(Mathf.Max(patch, ta) * 255f);
        return mixed;
    };

    /// <summary>그 부위 둘레의 살빛 평균 (짙은 선·어두운 곳은 빼고)</summary>
    private static Color SkinAround(Color32[] px, int w, int h, RectInt r)
    {
        double sr = 0, sg = 0, sb = 0;
        int n = 0;
        var center = r.center;
        float rx = r.width * 0.9f, ry = r.height * 0.9f;
        for (int y = Mathf.Max(0, (int)(center.y - ry)); y < Mathf.Min(h, (int)(center.y + ry)); y++)
        for (int x = Mathf.Max(0, (int)(center.x - rx)); x < Mathf.Min(w, (int)(center.x + rx)); x++)
        {
            float u = (x - center.x) / rx, v = (y - center.y) / ry;
            float d = u * u + v * v;
            if (d < 0.75f || d > 1f) continue;              // 부위 바로 바깥 띠만
            var c = px[x + y * w];
            if (c.a < 200 || (c.r + c.g + c.b) / 3 < 170) continue;
            sr += c.r; sg += c.g; sb += c.b; n++;
        }
        return n == 0 ? new Color(0.90f, 0.72f, 0.52f) : new Color((float)(sr / n / 255.0), (float)(sg / n / 255.0), (float)(sb / n / 255.0));
    }

    private static void SaveEmpty(string name, Rect area) => SaveFixed(NewCanvas(), name, area);

    private static void SavePainted(string name, Rect area, Painter painter)
    {
        var canvas = NewCanvas();
        int x0 = Mathf.FloorToInt(area.x), y0 = Mathf.FloorToInt(area.y);
        int cw = Mathf.CeilToInt(area.width), ch = Mathf.CeilToInt(area.height);
        for (int y = 0; y < ch; y++)
        for (int x = 0; x < cw; x++)
        {
            int cx = x0 + x, cy = y0 + y;
            if (cx < 0 || cx >= CANVAS_W || cy < 0 || cy >= CANVAS_H) continue;
            canvas[cx + cy * CANVAS_W] = painter((x + 0.5f) / cw, (y + 0.5f) / ch);
        }
        SaveFixed(canvas, name, area);   // 표정·혀는 정해진 자리로 (바뀔 때 어긋나지 않게)
    }

    // 감은 눈·웃는 눈 — 아래/위로 볼록한 호
    private static Painter EyeArc(bool happy) => (u, v) =>
    {
        float t = (u - 0.5f) * 2f;                       // -1~1
        float curve = happy ? -0.12f * (1f - t * t) : 0.10f * (1f - t * t);   // v는 아래로 커진다 — 웃는 눈은 가운데가 위로
        float line = Mathf.Abs(v - (0.5f + curve));
        float width = 0.055f * (1f - 0.5f * Mathf.Abs(t));
        float a = Mathf.Clamp01((width - line) / 0.02f) * (Mathf.Abs(t) < 0.92f ? 1f : 0f);
        return Rgba(OUTLINE, a);
    };

    // 졸린 눈 — 원래 눈 위에 눈꺼풀을 덮는다
    private static Painter EyeLidded(Color32[] eye, RectInt src, float scale, float offY) => (u, v) =>
    {
        var c = SampleUV(eye, src.width, src.height, (u - 0.5f) * EYE_COVER + 0.5f, 1f - ((v - 0.5f) * EYE_COVER + 0.5f));
        if (v > 0.52f) return Rgba(OUTLINE, c.a / 255f * 0.95f);   // 위쪽을 눈꺼풀로 덮는다
        return c;
    };

    // 반짝이는 눈 — 원래 눈 + 흰 점 두 개
    private static Painter EyeSparkle(Color32[] eye, RectInt src, float scale, float offY) => (u, v) =>
    {
        var c = SampleUV(eye, src.width, src.height, (u - 0.5f) * EYE_COVER + 0.5f, 1f - ((v - 0.5f) * EYE_COVER + 0.5f));
        float d1 = Vector2.Distance(new Vector2(u, v), new Vector2(0.62f, 0.68f));
        float d2 = Vector2.Distance(new Vector2(u, v), new Vector2(0.40f, 0.40f));
        float s = Mathf.Max(Mathf.Clamp01((0.10f - d1) / 0.05f), Mathf.Clamp01((0.06f - d2) / 0.04f));
        if (s <= 0f || c.a < 40) return c;
        return Color32.Lerp(c, new Color32(255, 255, 255, c.a), s);
    };

    private enum MouthKind { Smile, OpenSmall, OpenWide, Frown }

    private static Painter MouthPainter(MouthKind kind) => (u, v) =>
    {
        float t = (u - 0.5f) * 2f;
        switch (kind)
        {
            case MouthKind.OpenSmall:
            case MouthKind.OpenWide:
            {
                float ry = kind == MouthKind.OpenWide ? 0.42f : 0.24f;
                float rx = kind == MouthKind.OpenWide ? 0.62f : 0.55f;
                float e = new Vector2(t / rx, (v - 0.5f) / ry).magnitude;
                if (e > 1.05f) return Rgba(OUTLINE, 0f);
                if (e > 0.88f) return Rgba(OUTLINE, 1f);                                   // 테두리
                return Rgba(new Color(0.35f, 0.16f, 0.16f), 1f);                            // 입 안
            }
            case MouthKind.Frown:
            {
                float line = Mathf.Abs(v - (0.5f - 0.10f * (1f - t * t)));
                return Rgba(OUTLINE, Mathf.Clamp01((0.05f - line) / 0.02f) * (Mathf.Abs(t) < 0.9f ? 1f : 0f));
            }
            default:   // Smile
            {
                float line = Mathf.Abs(v - (0.5f + 0.12f * (1f - t * t)));
                return Rgba(OUTLINE, Mathf.Clamp01((0.05f - line) / 0.02f) * (Mathf.Abs(t) < 0.9f ? 1f : 0f));
            }
        }
    };

    // 혀 두 마디 — 입 자리에서 오른쪽으로 뻗는 분홍 막대
    private static void SaveTongues(Rect mouth)
    {
        float len = mouth.width * 0.55f, thick = mouth.height * 0.42f;
        var a = new Rect(mouth.center.x, mouth.center.y - thick * 0.5f, len, thick);
        var b = new Rect(a.xMax, a.y, len * 0.9f, thick * 0.85f);
        SavePainted("tongue_01", a, TonguePainter());
        SavePainted("tongue_02", b, TonguePainter());
    }

    private static Painter TonguePainter() => (u, v) =>
    {
        float d = Mathf.Abs(v - 0.5f) * 2f;
        float a = Mathf.Clamp01((0.92f - d) / 0.25f);
        return Rgba(Color.Lerp(TONGUE, new Color(0.72f, 0.38f, 0.44f), d * 0.5f), a);
    };

    // 발밑 그림자 — 그림에서 가장 아래 발 높이에 맞춰 부드러운 타원
    private static void SaveShadow(Color32[] px, int w, int h, float scale, float offY)
    {
        int bottom = 0;   // 그림에서 가장 아래(발끝) — 위가 0인 좌표
        for (int y = h - 1; y >= 0 && bottom == 0; y--)
            for (int x = 0; x < w; x++)
                if (px[x + y * w].a > 40) { bottom = y; break; }

        float cy = bottom * scale + offY;
        var area = new Rect(CANVAS_W * 0.5f - 330f, cy - 45f, 660f, 90f);
        SavePainted("shadow_contact", area, (u, v) =>
        {
            float d = new Vector2((u - 0.5f) * 2f, (v - 0.5f) * 2f).magnitude;
            return Rgba(Color.black, Mathf.Clamp01(1f - d) * 0.33f);
        });
    }

    // ── 도구 ─────────────────────────────────────────────────

    private static Color32 Rgba(Color c, float a)
        => new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(Mathf.Clamp01(a) * 255f));

    private static Color32 SampleUV(Color32[] px, int w, int h, float u, float v)
    {
        if (u < 0f || u > 1f || v < 0f || v > 1f) return new Color32(0, 0, 0, 0);   // 그림 밖
        int x = Mathf.Clamp(Mathf.RoundToInt(u * (w - 1)), 0, w - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(v * (h - 1)), 0, h - 1);
        return px[x + y * w];
    }

    private static Color32[] NewCanvas()
    {
        var px = new Color32[CANVAS_W * CANVAS_H];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 0);
        return px;
    }

    private static void FlipVertical(Color32[] px, int w, int h)
    {
        for (int y = 0; y < h / 2; y++)
        for (int x = 0; x < w; x++)
        {
            int a = x + y * w, b = x + (h - 1 - y) * w;
            (px[a], px[b]) = (px[b], px[a]);
        }
    }

    /// <summary>이 실행에서 저장한 파츠가 캔버스에서 차지한 자리 (왼쪽 위 기준) — 관절 계산용</summary>
    private static readonly Dictionary<string, Rect> s_placed = new Dictionary<string, Rect>();

    /// <summary>
    /// 캔버스에서 **실제로 그려진 부분만 잘라** PNG로 저장한다 (통짜로 저장하면 꼬리 휘는 메시가 캔버스 전체를 휘고
    /// 메모리도 장당 5MB가 넘는다). 저장한 자리를 s_placed에 기록한다.
    /// </summary>
    private static void SaveTrimmed(Color32[] canvas, string name)
    {
        int minX = CANVAS_W, minY = CANVAS_H, maxX = -1, maxY = -1;
        for (int y = 0; y < CANVAS_H; y++)
        for (int x = 0; x < CANVAS_W; x++)
        {
            if (canvas[x + y * CANVAS_W].a <= 3) continue;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        if (maxX < minX)   // 아무것도 안 그려졌다 (기본 표정용 빈 그림)
        {
            SavePngRaw(new Color32[4 * 4], 4, 4, name);
            return;
        }

        int tw = maxX - minX + 1, th = maxY - minY + 1;
        var px = new Color32[tw * th];
        for (int y = 0; y < th; y++)
        for (int x = 0; x < tw; x++)
            px[x + y * tw] = canvas[(minX + x) + (minY + y) * CANVAS_W];

        s_placed[name] = new Rect(minX, minY, tw, th);
        SavePngRaw(px, tw, th, name);
    }

    /// <summary>
    /// 정해진 사각형 그대로 잘라 저장한다 — 표정(눈·입)은 바뀔 때 크기·자리가 같아야 하므로 실제 그린 범위로 줄이지 않는다.
    /// </summary>
    private static void SaveFixed(Color32[] canvas, string name, Rect area)
    {
        int x0 = Mathf.Clamp(Mathf.FloorToInt(area.x), 0, CANVAS_W - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(area.y), 0, CANVAS_H - 1);
        int tw = Mathf.Clamp(Mathf.CeilToInt(area.width),  1, CANVAS_W - x0);
        int th = Mathf.Clamp(Mathf.CeilToInt(area.height), 1, CANVAS_H - y0);

        var px = new Color32[tw * th];
        for (int y = 0; y < th; y++)
        for (int x = 0; x < tw; x++)
            px[x + y * tw] = canvas[(x0 + x) + (y0 + y) * CANVAS_W];

        s_placed[name] = new Rect(x0, y0, tw, th);
        SavePngRaw(px, tw, th, name);
    }

    private static void SavePngRaw(Color32[] px, int w, int h, string name)
    {
        var flipped = (Color32[])px.Clone();
        FlipVertical(flipped, w, h);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(flipped);
        tex.Apply();
        File.WriteAllBytes($"{OUT_DIR}/{name}.png", tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static void SavePng(Color32[] canvas, string name)
    {
        var flipped = (Color32[])canvas.Clone();
        FlipVertical(flipped, CANVAS_W, CANVAS_H);   // 위가 0인 캔버스를 Unity가 쓰는 아래가 0으로
        var tex = new Texture2D(CANVAS_W, CANVAS_H, TextureFormat.RGBA32, false);
        tex.SetPixels32(flipped);
        tex.Apply();
        File.WriteAllBytes($"{OUT_DIR}/{name}.png", tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    private static Texture2D LoadPng(string path)
    {
        if (!File.Exists(path)) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        return tex;
    }

    private static void ImportAsSprites()
    {
        foreach (var file in Directory.GetFiles(OUT_DIR, "*.png"))
        {
            string path = file.Replace('\\', '/');
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is not TextureImporter imp) continue;
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.isReadable          = true;   // 관절을 잡으려면 픽셀을 읽을 수 있어야 한다 (GeckoSkinImporter.OpaqueBounds)
            imp.mipmapEnabled       = true;
            imp.maxTextureSize      = 2048;
            imp.SaveAndReimport();
        }
    }

    // ── 스킨 만들기 ──────────────────────────────────────────
    // 파츠를 실제 크기로 잘랐기 때문에 관절을 직접 계산한다.
    //   원점 = 네 다리 아래 끝 가운데 (발밑)  ·  관절 = 파츠 안에서 몸통 쪽 끝 (아래 표)
    //   jointPosition = 원점 기준 스킨 픽셀 (y는 위로 +)  ·  jointPivot = 파츠 안 0~1 (y는 아래에서 위로)

    private const string SKIN_PATH = "Assets/_Game/GeckoSkins/GeckoSkin_Painted.asset";
    private static readonly Color FAR_TINT = new Color(0.84f, 0.84f, 0.84f);   // 먼 쪽 다리는 살짝 어둡게 (ART_GUIDE)

    private static float s_scale = 1f, s_offY;   // 원본 → 캔버스 (Cut에서 채운다)

    private static GeckoSkin BuildSkin(Rect eyeArea, Rect mouthArea, System.Text.StringBuilder log)
    {
        Sprite Load(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{OUT_DIR}/{n}.png");

        // 원점 — 다리 네 개의 아래 끝 가운데
        float bottom = 0f, cx = 0f;
        int legs = 0;
        foreach (var name in new[] { "leg_back_near", "leg_back_far", "leg_front_near", "leg_front_far" })
        {
            if (!s_placed.TryGetValue(name, out var lr)) continue;
            bottom = Mathf.Max(bottom, lr.yMax);
            cx += lr.center.x;
            legs++;
        }
        var origin = legs > 0 ? new Vector2(cx / legs, bottom) : new Vector2(CANVAS_W * 0.5f, CANVAS_H * 0.75f);

        var skin = ScriptableObject.CreateInstance<GeckoSkin>();

        // 화면 크기 기준 = 꼬리 끝 ~ 주둥이 끝
        float minX = CANVAS_W, maxX = 0f;
        foreach (var name in new[] { "tail", "body", "head" })
            if (s_placed.TryGetValue(name, out var r)) { minX = Mathf.Min(minX, r.xMin); maxX = Mathf.Max(maxX, r.xMax); }
        skin.referenceWidth = Mathf.Max(200f, maxX - minX);

        void AddPart(GeckoPartId id, string name, Vector2 anchor01, Color tint)
        {
            var sp = Load(name);
            if (sp == null || !s_placed.TryGetValue(name, out var r))
            {
                log.AppendLine("파츠 없음: " + name);
                return;
            }
            var joint = new Vector2(r.xMin + r.width * anchor01.x, r.yMin + r.height * anchor01.y);
            skin.parts.Add(new GeckoPartArt
            {
                id            = id,
                sprite        = sp,
                jointPosition = new Vector2(joint.x - origin.x, origin.y - joint.y),   // y는 위로 +
                jointPivot    = new Vector2(anchor01.x, 1f - anchor01.y),              // 스프라이트 피벗은 아래가 0
                scale         = Vector2.one,
                tint          = tint,
            });
        }

        // 관절은 CUT_PARTS의 joint(원본 좌표) — 실제 어깨·허벅지 자리라야 걸을 때 발이 미끄러지지 않는다
        void AddLeg(GeckoPartId id, string name, Color tint)
        {
            foreach (var leg in CUT_PARTS)
            {
                if (leg.name != name) continue;
                if (!s_placed.TryGetValue(name, out var r)) break;
                var jc = new Vector2(leg.joint.x * s_scale, leg.joint.y * s_scale + s_offY);
                AddPart(id, name, new Vector2((jc.x - r.xMin) / r.width, (jc.y - r.yMin) / r.height), tint);
                return;
            }
            log.AppendLine("파츠 없음: " + name);
        }

        AddPart(GeckoPartId.Shadow,       "shadow_contact", new Vector2(0.50f, 0.50f), Color.white);
        AddPart(GeckoPartId.Tail,         "tail",           new Vector2(0.97f, 0.45f), Color.white);   // 뿌리 = 오른쪽 끝
        AddLeg(GeckoPartId.LegBackFar,    "leg_back_far",   FAR_TINT);
        AddLeg(GeckoPartId.LegFrontFar,   "leg_front_far",  FAR_TINT);
        AddLeg(GeckoPartId.Body,          "body",           Color.white);
        AddLeg(GeckoPartId.LegBackNear,   "leg_back_near",  Color.white);
        AddLeg(GeckoPartId.LegFrontNear,  "leg_front_near", Color.white);
        AddPart(GeckoPartId.Head,         "head",           new Vector2(0.07f, 0.60f), Color.white);   // 목 = 왼쪽
        AddPart(GeckoPartId.EyeL,         "eye_open",       new Vector2(0.50f, 0.50f), Color.white);
        AddPart(GeckoPartId.EyeR,         "eye_open",       new Vector2(0.50f, 0.50f), Color.white);
        AddPart(GeckoPartId.Mouth,        "mouth_closed",   new Vector2(0.50f, 0.50f), Color.white);
        AddPart(GeckoPartId.Tongue1,      "tongue_01",      new Vector2(0.05f, 0.50f), Color.white);   // 혀 뿌리 = 왼쪽
        AddPart(GeckoPartId.Tongue2,      "tongue_02",      new Vector2(0.05f, 0.50f), Color.white);

        void AddEye(GeckoEye state, string name)
        {
            var sp = Load(name);
            if (sp != null) skin.eyes.Add(new GeckoEyeArt { state = state, left = sp });
        }
        AddEye(GeckoEye.Open,    "eye_open");
        AddEye(GeckoEye.Closed,  "eye_closed");
        AddEye(GeckoEye.Sleepy,  "eye_sleepy");
        AddEye(GeckoEye.Happy,   "eye_happy");
        AddEye(GeckoEye.Sparkle, "eye_sparkle");

        void AddMouth(GeckoMouth state, string name)
        {
            var sp = Load(name);
            if (sp != null) skin.mouths.Add(new GeckoMouthArt { state = state, sprite = sp });
        }
        AddMouth(GeckoMouth.Closed,    "mouth_closed");
        AddMouth(GeckoMouth.Smile,     "mouth_smile");
        AddMouth(GeckoMouth.OpenSmall, "mouth_open_small");
        AddMouth(GeckoMouth.OpenWide,  "mouth_open_wide");
        AddMouth(GeckoMouth.Frown,     "mouth_frown");

        var existing = AssetDatabase.LoadAssetAtPath<GeckoSkin>(SKIN_PATH);
        if (existing != null)
        {
            EditorUtility.CopySerialized(skin, existing);
            existing.name = "GeckoSkin_Painted";
            UnityEngine.Object.DestroyImmediate(skin);
            skin = existing;
            EditorUtility.SetDirty(skin);
        }
        else
        {
            skin.name = "GeckoSkin_Painted";
            AssetDatabase.CreateAsset(skin, SKIN_PATH);
        }
        AssetDatabase.SaveAssets();

        log.AppendLine($"스킨 {SKIN_PATH} — 파츠 {skin.parts.Count}개 · 눈 {skin.eyes.Count}종 · 입 {skin.mouths.Count}종");
        log.AppendLine($"기준 폭 {skin.referenceWidth:0} px · 발밑 원점 ({origin.x:0}, {origin.y:0})");
        return skin;
    }

    private const string HOME_SCENE = "Assets/_Game/Scenes/MainHome.unity";

    private static string ApplyToScene(GeckoSkin skin)
    {
        // 이미 MainHome을 열어 두었으면 그 씬에 그대로 적용한다 (다시 열면 편집 중이던 내용이 사라진다)
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != HOME_SCENE)
        {
            if (Application.isPlaying) return "플레이 중에는 씬에 적용하지 못합니다 — 정지한 뒤 다시 실행해 주십시오.";
            scene = EditorSceneManager.OpenScene(HOME_SCENE, OpenSceneMode.Single);
        }
        var rig = UnityEngine.Object.FindFirstObjectByType<GeckoRig>(FindObjectsInactive.Include);
        if (rig == null) return "MainHome에서 GeckoRig를 찾지 못했습니다 — 메뉴 ③으로 직접 적용해 주십시오.";

        var so = new SerializedObject(rig);
        so.FindProperty("_skin").objectReferenceValue = skin;
        var stages = so.FindProperty("_stageSkins");
        for (int i = 0; i < stages.arraySize; i++) stages.GetArrayElementAtIndex(i).objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);
        var applied = new SerializedObject(rig).FindProperty("_skin").objectReferenceValue;
        return applied == skin
            ? $"MainHome 게코에 적용했습니다 (씬 저장 {(saved ? "완료" : "실패 — Ctrl+S로 저장해 주십시오")})."
            : "▲ 게코에 스킨이 붙지 않았습니다 — 메뉴 ③으로 직접 적용해 주십시오.";
    }
}
