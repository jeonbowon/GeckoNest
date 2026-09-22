using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 아이 그림풍 전신 게코(`Textures/Gecko/ChildArt/hako_child_base_v1.png`, 2026-09-22)로 전신 스킨을 만든다.
///
///   ① 그림 정리 → `hako_child_body.png` (원본은 그대로 둔다)
///      - 배경을 지울 때 남은 **흰 안개 테두리**(알파 120 아래)와 흩어진 잡티를 지우고, 윤곽(120~220)은 부드럽게 남긴다
///      - 몸 안쪽도 알파가 250 남짓이라 흙이 비쳐 보였다 → 220 이상은 완전히 불투명
///      - 가장 큰 덩어리(게코)만 남기고, 왼쪽을 보는 그림을 **오른쪽을 보게 뒤집는다** (스킨 규칙), 여백을 잘라 낸다
///      - **이미 있으면 다시 만들지 않는다** (손본 그림 보호 — 다시 만들려면 PNG만 지운다, .meta를 두면 연결 유지)
///   ② `GeckoSkins/GeckoSkin_Child.asset` — 몸통 파츠 = 그림 한 장(`wholeBody`), 나머지 파츠는 그림 없이 판정 자리(hitSize).
///      혀·그림자는 GeckoSkin_Painted의 그림을 쓴다 (혀는 화면 크기가 같게 키운다)
///   ③ 크레스티드 종(`Resources/Species/crested`)의 전용 그림으로 연결 — 하코가 이 그림으로 나온다. 다른 종은 씬 기본 그림
///
/// 배치 실행: Unity.exe -batchmode -executeMethod GeckoChildArt.BuildBatch
/// 부위 자리(아래 표)는 정리된 그림의 픽셀(왼쪽 위 0,0)이다 — 그림을 바꾸면 이 표를 다시 잰다.
/// </summary>
public static class GeckoChildArt
{
    private const string SRC_PATH     = "Assets/_Game/Textures/Gecko/ChildArt/hako_child_base_v1.png";
    private const string OUT_PATH     = "Assets/_Game/Textures/Gecko/ChildArt/hako_child_body.png";
    private const string SKIN_PATH    = "Assets/_Game/GeckoSkins/GeckoSkin_Child.asset";
    private const string PAINTED_PATH = "Assets/_Game/GeckoSkins/GeckoSkin_Painted.asset";
    private const string SPECIES_PATH = "Assets/_Game/Resources/Species/crested.asset";

    private const int ALPHA_LO = 120, ALPHA_HI = 220;   // 이 아래는 안개 테두리 · 이 위는 몸 안쪽
    private const int MARGIN   = 4;

    // 화면 크기 — 이 그림은 고개를 든 자세라 파츠 그림(길이 대비 키 0.37)보다 키가 크다(0.60).
    // 씬의 어덜트 폭 640 그대로면 키가 1.6배가 되어, 길이를 520으로 줄여 차지하는 넓이를 비슷하게 맞췄다
    private const float ADULT_WIDTH    = 640f;   // GeckoRig._adultWidth (씬 값)
    private const float DISPLAY_LENGTH = 520f;   // [TBD] 어덜트 화면 길이 (UI)

    private const int EXPECT_W = 1460, EXPECT_H = 883;   // 아래 표를 잰 그림 크기

    // 발밑 가운데 (가까운 뒷발·앞발 발끝 사이, 발끝 높이)
    private static readonly Vector2 ORIGIN = new Vector2(795f, 872f);

    // 판정 자리 — (파츠, 사각형 x0·y0·x1·y1, 관절 x·y), 정리된 그림 픽셀 (왼쪽 위 0,0)
    // 눈 판정 박스는 사각형의 30%×45%(GeckoTouch)라 사각형을 그만큼 크게 잡았다
    private static readonly (GeckoPartId id, float x0, float y0, float x1, float y1, float jx, float jy)[] GHOSTS =
    {
        (GeckoPartId.Tail,         0f,  540f,  500f, 850f,  480f, 640f),
        (GeckoPartId.LegBackFar,   685f, 655f,  815f, 785f,  760f, 660f),
        (GeckoPartId.LegFrontFar,  1140f, 605f, 1290f, 795f, 1170f, 610f),
        (GeckoPartId.LegBackNear,  500f, 605f,  680f, 855f,  630f, 610f),
        (GeckoPartId.LegFrontNear, 915f, 580f, 1095f, 840f,  990f, 580f),
        (GeckoPartId.Head,         960f,  20f, 1460f, 420f, 1090f, 420f),
        (GeckoPartId.EyeL,         945f,  33f, 1445f, 366f, 1195f, 200f),   // 가까운 눈 (그림 150×150)
        (GeckoPartId.EyeR,        1252f,  40f, 1452f, 240f, 1352f, 140f),   // 먼 눈 (60×90)
        (GeckoPartId.Mouth,       1240f, 240f, 1460f, 330f, 1350f, 285f),
    };

    private static readonly Vector2 BODY_JOINT  = new Vector2(800f, 560f);    // 몸통 가운데 — 몸 흔들림·호흡의 중심
    private static readonly Vector2 HEAD_PIVOT  = new Vector2(1090f, 420f);   // 목 (머리 관절과 같게)
    private static readonly Vector4 HEAD_ZONE   = new Vector4(964f, 1110f, 477f, 388f);   // 가로 0→1 · 세로 0→1 (턱 밑 목 피부가 늘어난다)
    private const float HEAD_GAIN = 1.5f;   // [TBD] 머리만 휘는 그림이라 같은 각도로는 움직임이 작게 보였다
    private static readonly Vector2 TAIL_ZONE   = new Vector2(482f, 321f);    // 가로 0→1 (뒷발 발끝 460에는 닿지 않게)

    // 꼬리 가운데 선 — 뿌리 → 끝 (마디 8개). 모터의 꼬리 물결을 마디마다 받는다
    private static readonly Vector2[] TAIL_CHAIN =
    {
        new Vector2(480f, 640f), new Vector2(380f, 635f), new Vector2(280f, 632f), new Vector2(190f, 628f),
        new Vector2(110f, 628f), new Vector2(45f, 665f),  new Vector2(55f, 745f),  new Vector2(110f, 800f),
        new Vector2(165f, 825f),
    };

    // 다리 뼈 — 관절은 GHOSTS의 다리 관절, 발 가운데(fx·fy), 폭(관절 쪽 · 발 쪽, 픽셀 — 발 쪽은 발가락 끝까지 덮게)
    // 네모 상자로 나눴더니 발이 다리보다 넓어 상자 가장자리가 발가락을 비스듬히 잘랐다 (2026-09-22)
    private static readonly (GeckoPartId id, float fx, float fy, float rTop, float rFoot)[] LEG_BONES =
    {
        (GeckoPartId.LegFrontNear, 1005f, 835f, 75f, 120f),
        (GeckoPartId.LegFrontFar,  1235f, 770f, 55f,  95f),
        (GeckoPartId.LegBackNear,   580f, 835f, 75f, 125f),
        (GeckoPartId.LegBackFar,    765f, 765f, 45f,  75f),
    };
    private static readonly Vector2 TONGUE_ROOT = new Vector2(1420f, 268f);   // 입 앞 끝
    private static readonly Vector2 SHADOW_AT   = new Vector2(870f, 835f);
    private static readonly Vector2 SHADOW_SCALE = new Vector2(1.4f, 1.3f);   // Painted 그림자(660×90) 기준 — 네 발과 배 밑을 덮게

    // ── 실행 ─────────────────────────────────────────────────

    [MenuItem("Hako/Gecko/⑤ 아이 그림 전신 스킨 만들기 (ChildArt)", priority = 5)]
    public static void BuildMenu()
    {
        EditorUtility.DisplayDialog("전신 스킨", Build(), "확인");
    }

    [MenuItem("Hako/Gecko/⑤ 아이 그림 전신 스킨 만들기 (ChildArt)", true)]
    private static bool BuildMenuValid() => !EditorApplication.isPlayingOrWillChangePlaymode;

    public static void BuildBatch()
    {
        try
        {
            Debug.Log("[GeckoChildArt]\n" + Build());
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[GeckoChildArt] 실패: " + e);
            EditorApplication.Exit(1);
        }
    }

    public static string Build()
    {
        var log = new System.Text.StringBuilder();

        if (File.Exists(OUT_PATH)) log.AppendLine($"정리된 그림이 이미 있어 그대로 씁니다: {OUT_PATH}");
        else
        {
            string r = CleanArt();
            if (r == null) return $"원본을 찾지 못했습니다: {SRC_PATH}";
            log.AppendLine(r);
            AssetDatabase.ImportAsset(OUT_PATH, ImportAssetOptions.ForceSynchronousImport);
        }
        ImportAsSprite(OUT_PATH);

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OUT_PATH);
        if (sprite == null) return log.Append("스프라이트를 읽지 못했습니다").ToString();
        int w = Mathf.RoundToInt(sprite.rect.width), h = Mathf.RoundToInt(sprite.rect.height);
        if (w != EXPECT_W || h != EXPECT_H)
            log.AppendLine($"주의: 그림 크기 {w}×{h} — 부위 표는 {EXPECT_W}×{EXPECT_H}에서 잰 값입니다. 자리가 어긋나면 표를 다시 잽니다");

        var painted = AssetDatabase.LoadAssetAtPath<GeckoSkin>(PAINTED_PATH);
        var skin = BuildSkin(sprite, painted, log);
        log.AppendLine(LinkSpecies(skin));
        AssetDatabase.SaveAssets();
        return log.ToString();
    }

    // ── 자세 미리보기 ────────────────────────────────────────
    // 배치 실행: -executeMethod GeckoChildArt.PreviewBatch → 프로젝트 Logs/child_pose_preview.png (git 제외)
    // 게임과 같은 GeckoWholeBend.Deform으로 그린다 — 걸음 두 박자 · 발 드는 순간 · 고개 들고 꼬리 흔들기 (모터 기본값으로 흉내)

    public static void PreviewBatch()
    {
        try
        {
            var skin = AssetDatabase.LoadAssetAtPath<GeckoSkin>(SKIN_PATH);
            var src  = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            src.LoadImage(File.ReadAllBytes(OUT_PATH));
            int W = src.width, H = src.height;
            var px = src.GetPixels32();
            UnityEngine.Object.DestroyImmediate(src);

            var go = new GameObject("PosePreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            var bend = go.AddComponent<GeckoWholeBend>();
            bend.Configure(skin.wholeHeadPivot, skin.wholeHeadZone, skin.wholeHeadGain, skin.wholeTailChain, skin.wholeTailZone,
                           GeckoRig.LegsInOrder(skin));

            float[] phases = { Mathf.PI * 0.5f, -Mathf.PI * 0.5f, 0f, -1f };   // -1 = 가만히 (고개 · 꼬리)
            int pad = 60, cellH = H + pad * 2;
            var outPx = new Color32[(W + pad * 2) * cellH * phases.Length];
            var bg = new Color32(70, 55, 40, 255);
            for (int i = 0; i < outPx.Length; i++) outPx[i] = bg;
            int OW = W + pad * 2, OH = cellH * phases.Length;

            for (int n = 0; n < phases.Length; n++)
            {
                bend.SetPose(PreviewPose(phases[n], n));
                int baseY = (phases.Length - 1 - n) * cellH + pad;   // 위에서부터 차례로
                var offset = new Vector2(pad, baseY);

                // 게임과 같은 격자 삼각형에 그림을 입힌다 (늘어난 곳도 구멍 없이 — 실제 화면과 같게)
                int C = GeckoWholeBend.COLUMNS, R = GeckoWholeBend.ROWS;
                var grid = new Vector2[(C + 1) * (R + 1)];
                var gridUv = new Vector2[grid.Length];
                for (int r = 0; r <= R; r++)
                for (int c = 0; c <= C; c++)
                {
                    var uv = new Vector2((float)c / C, (float)r / R);
                    gridUv[r * (C + 1) + c] = uv;
                    grid[r * (C + 1) + c]   = bend.Deform(uv, Vector2.zero, new Vector2(W, H)) + offset;
                }
                for (int r = 0; r < R; r++)
                for (int c = 0; c < C; c++)
                {
                    int a = r * (C + 1) + c, b = a + 1, d = a + C + 1, e = d + 1;
                    FillTriangle(outPx, OW, OH, px, W, H, grid, gridUv, a, d, e);
                    FillTriangle(outPx, OW, OH, px, W, H, grid, gridUv, a, e, b);
                }
            }
            UnityEngine.Object.DestroyImmediate(go);

            var tex = new Texture2D(OW, OH, TextureFormat.RGBA32, false);
            tex.SetPixels32(outPx);
            tex.Apply();
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "child_pose_preview.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log("[GeckoChildArt] 미리보기 " + path);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[GeckoChildArt] 미리보기 실패: " + e);
            EditorApplication.Exit(1);
        }
    }

    // 삼각형 하나를 칠한다 — 화면 픽셀마다 무게중심 좌표로 uv를 구해 그림에서 가져온다
    private static void FillTriangle(Color32[] dst, int DW, int DH, Color32[] src, int SW, int SH,
                                     Vector2[] p, Vector2[] uv, int i0, int i1, int i2)
    {
        Vector2 a = p[i0], b = p[i1], c = p[i2];
        float area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
        if (Mathf.Abs(area) < 1e-4f) return;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
        int x1 = Mathf.Min(DW - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
        int y1 = Mathf.Min(DH - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            var q = new Vector2(x + 0.5f, y + 0.5f);
            float w1 = ((q.x - a.x) * (c.y - a.y) - (c.x - a.x) * (q.y - a.y)) / area;
            float w2 = ((b.x - a.x) * (q.y - a.y) - (q.x - a.x) * (b.y - a.y)) / area;
            float w0 = 1f - w1 - w2;
            if (w0 < 0f || w1 < 0f || w2 < 0f) continue;
            Vector2 t = uv[i0] * w0 + uv[i1] * w1 + uv[i2] * w2;
            int sx = Mathf.Clamp((int)(t.x * SW), 0, SW - 1), sy = Mathf.Clamp((int)(t.y * SH), 0, SH - 1);
            var s = src[sy * SW + sx];
            if (s.a == 0) continue;
            int k = y * DW + x;
            dst[k] = Color32.Lerp(dst[k], new Color32(s.r, s.g, s.b, 255), s.a / 255f);
        }
    }

    // GeckoMotor 기본값 흉내 — 다리 흔들기 18° · 발 들기 12 · 꼬리 흔들기 16 (LayerWalk · SimulateTail)
    private static GeckoPose PreviewPose(float phase, int n)
    {
        var pose = new GeckoPose(12);
        bool walking = phase > -0.5f || phase < -1.5f;
        float w = walking ? 1f : 0f, ph = walking ? phase : 0f;
        void Leg(GeckoPartId id, float p)
        {
            pose[id].angle    += w * 18f * Mathf.Sin(p);
            pose[id].offset.y += w * 12f * Mathf.Max(0f, Mathf.Cos(p));
        }
        Leg(GeckoPartId.LegFrontNear, ph);
        Leg(GeckoPartId.LegBackFar,   ph);
        Leg(GeckoPartId.LegFrontFar,  ph + Mathf.PI);
        Leg(GeckoPartId.LegBackNear,  ph + Mathf.PI);
        if (!walking) pose[GeckoPartId.Head].angle = 7f;

        int k12 = pose.tailBend.Length;
        float wsum = 0f;
        for (int k = 0; k < k12; k++) wsum += 0.35f + (k + 1f) / k12;
        float time = 1.1f + n * 0.9f;
        for (int k = 0; k < k12; k++)
        {
            float f = (k + 1f) / k12, share = (0.35f + f) / wsum;
            pose.tailBend[k] = share * (16f * 1.6f * Mathf.Sin(time * 1.75f - k * 0.42f) + 16f * w * Mathf.Sin(ph - k * 0.5f));
        }
        return pose;
    }

    // ── ① 그림 정리 ──────────────────────────────────────────

    /// <summary>원본 → 정리된 그림 저장. 원본이 없으면 null</summary>
    private static string CleanArt()
    {
        if (!File.Exists(SRC_PATH)) return null;
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        src.LoadImage(File.ReadAllBytes(SRC_PATH));
        int W = src.width, H = src.height;
        var raw = src.GetPixels32();   // 아래가 0
        UnityEngine.Object.DestroyImmediate(src);

        // 위가 0인 배열로 옮기며 좌우 반전 + 알파 정리
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            var c = raw[(H - 1 - y) * W + x];
            c.a = CleanAlpha(c.a);
            px[y * W + (W - 1 - x)] = c;
        }

        int removed = KeepLargest(px, W, H, out RectInt box);
        int x0 = Mathf.Max(0, box.xMin - MARGIN), y0 = Mathf.Max(0, box.yMin - MARGIN);
        int x1 = Mathf.Min(W - 1, box.xMax - 1 + MARGIN), y1 = Mathf.Min(H - 1, box.yMax - 1 + MARGIN);
        int cw = x1 - x0 + 1, ch = y1 - y0 + 1;

        var outPx = new Color32[cw * ch];
        for (int y = 0; y < ch; y++)
        for (int x = 0; x < cw; x++)
        {
            var c = px[(y + y0) * W + (x + x0)];
            if (c.a == 0) c = new Color32(0, 0, 0, 0);
            outPx[(ch - 1 - y) * cw + x] = c;   // 다시 아래가 0으로
        }

        var tex = new Texture2D(cw, ch, TextureFormat.RGBA32, false);
        tex.SetPixels32(outPx);
        tex.Apply();
        File.WriteAllBytes(OUT_PATH, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        return $"그림 정리: 원본 {W}×{H} → {cw}×{ch} (좌우 반전, 떨어진 조각 {removed:N0}픽셀 지움)";
    }

    /// <summary>안개 테두리는 지우고, 윤곽은 부드럽게, 몸 안쪽은 불투명하게</summary>
    public static byte CleanAlpha(byte a)
    {
        if (a <= ALPHA_LO) return 0;
        if (a >= ALPHA_HI) return 255;
        float t = (a - ALPHA_LO) / (float)(ALPHA_HI - ALPHA_LO);
        return (byte)Mathf.RoundToInt(t * t * (3f - 2f * t) * 255f);
    }

    /// <summary>보이는 픽셀 덩어리(8방향) 중 가장 큰 것만 남긴다. 지운 픽셀 수, 남은 덩어리 범위</summary>
    private static int KeepLargest(Color32[] px, int W, int H, out RectInt box)
    {
        var label = new int[W * H];
        var stack = new int[W * H];
        int cur = 0, best = 0, bestCount = 0;
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a == 0 || label[i] != 0) continue;
            cur++;
            int count = 0, sp = 0;
            stack[sp++] = i;
            label[i] = cur;
            while (sp > 0)
            {
                int p = stack[--sp];
                count++;
                int x = p % W, y = p / W;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                    int q = ny * W + nx;
                    if (label[q] != 0 || px[q].a == 0) continue;
                    label[q] = cur;
                    stack[sp++] = q;
                }
            }
            if (count > bestCount) { bestCount = count; best = cur; }
        }

        int removed = 0, minX = W, minY = H, maxX = -1, maxY = -1;
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a == 0) continue;
            if (label[i] != best) { px[i].a = 0; removed++; continue; }
            int x = i % W, y = i / W;
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
        }
        box = maxX < 0 ? new RectInt(0, 0, W, H) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return removed;
    }

    private static void ImportAsSprite(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter imp) return;
        bool dirty = imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single
                     || !imp.alphaIsTransparency || !imp.mipmapEnabled || imp.maxTextureSize != 2048;
        if (!dirty) return;
        imp.textureType         = TextureImporterType.Sprite;
        imp.spriteImportMode    = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled       = true;   // 1460 픽셀 그림을 500 남짓으로 줄여 그린다
        imp.maxTextureSize      = 2048;
        imp.SaveAndReimport();
    }

    // ── ② 스킨 ───────────────────────────────────────────────

    private static GeckoSkin BuildSkin(Sprite sprite, GeckoSkin painted, System.Text.StringBuilder log)
    {
        var skin = AssetDatabase.LoadAssetAtPath<GeckoSkin>(SKIN_PATH);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<GeckoSkin>();
            AssetDatabase.CreateAsset(skin, SKIN_PATH);
        }

        float W = sprite.rect.width, H = sprite.rect.height;
        Vector2 Uv(Vector2 p) => new Vector2(p.x / W, (H - p.y) / H);
        Vector2 Skin(Vector2 p) => new Vector2(p.x - ORIGIN.x, ORIGIN.y - p.y);

        skin.referenceWidth = W * ADULT_WIDTH / DISPLAY_LENGTH;
        skin.wholeBody      = true;
        skin.wholeHeadPivot = Uv(HEAD_PIVOT);
        skin.wholeHeadZone  = new Vector4(HEAD_ZONE.x / W, HEAD_ZONE.y / W, (H - HEAD_ZONE.z) / H, (H - HEAD_ZONE.w) / H);
        skin.wholeHeadGain  = HEAD_GAIN;
        skin.wholeTailZone  = new Vector2(TAIL_ZONE.x / W, TAIL_ZONE.y / W);
        skin.wholeTailChain = Array.ConvertAll(TAIL_CHAIN, Uv);
        skin.wholeLegs.Clear();
        foreach (var (id, fx, fy, rTop, rFoot) in LEG_BONES)
        {
            Vector2 joint = Vector2.zero;
            foreach (var g in GHOSTS) if (g.id == id) joint = new Vector2(g.jx, g.jy);
            skin.wholeLegs.Add(new GeckoWholeLimb
            {
                id = id, joint = Uv(joint), foot = Uv(new Vector2(fx, fy)), radius = new Vector2(rTop / W, rFoot / W),
            });
        }
        skin.parts.Clear();
        skin.eyes.Clear();     // 표정은 그림에 그려진 그대로 (눈·입 판은 판정 자리만)
        skin.mouths.Clear();

        skin.parts.Add(new GeckoPartArt
        {
            id = GeckoPartId.Body, sprite = sprite,
            jointPosition = Skin(BODY_JOINT), jointPivot = Uv(BODY_JOINT),
        });

        foreach (var (id, x0, y0, x1, y1, jx, jy) in GHOSTS)
        {
            float pw = x1 - x0, ph = y1 - y0;
            skin.parts.Add(new GeckoPartArt
            {
                id = id,
                hitSize       = new Vector2(pw, ph),
                jointPosition = Skin(new Vector2(jx, jy)),
                jointPivot    = new Vector2((jx - x0) / pw, (y1 - jy) / ph),
            });
        }

        // 혀·그림자는 파츠 그림(Painted)에서 — 혀는 화면에서 같은 크기가 되게 (Painted 1픽셀 = 이 스킨 k픽셀)
        if (painted != null)
        {
            float k = skin.referenceWidth / Mathf.Max(1f, painted.referenceWidth);
            var t1 = painted.GetPart(GeckoPartId.Tongue1);
            var t2 = painted.GetPart(GeckoPartId.Tongue2);
            if (t1 != null && t2 != null)
            {
                Vector2 root = Skin(TONGUE_ROOT);
                skin.parts.Add(new GeckoPartArt
                {
                    id = GeckoPartId.Tongue1, sprite = t1.sprite, jointPosition = root,
                    jointPivot = t1.jointPivot, scale = t1.scale * k, tint = t1.tint,
                });
                skin.parts.Add(new GeckoPartArt
                {
                    id = GeckoPartId.Tongue2, sprite = t2.sprite,
                    jointPosition = root + (t2.jointPosition - t1.jointPosition) * k,
                    jointPivot = t2.jointPivot, scale = t2.scale * k, tint = t2.tint,
                });
            }
            var shadow = painted.GetPart(GeckoPartId.Shadow);
            if (shadow != null)
                skin.parts.Add(new GeckoPartArt
                {
                    id = GeckoPartId.Shadow, sprite = shadow.sprite, jointPosition = Skin(SHADOW_AT),
                    jointPivot = new Vector2(0.5f, 0.5f), scale = SHADOW_SCALE, tint = shadow.tint,
                });
        }
        else log.AppendLine($"주의: {PAINTED_PATH}가 없어 혀·그림자 없이 만들었습니다");

        EditorUtility.SetDirty(skin);
        log.AppendLine($"스킨 {SKIN_PATH} — 기준 폭 {skin.referenceWidth:0} (어덜트 길이 {DISPLAY_LENGTH:0}), 파츠 {skin.parts.Count}개");
        return skin;
    }

    // ── ③ 종 연결 ────────────────────────────────────────────

    private static string LinkSpecies(GeckoSkin skin)
    {
        var species = AssetDatabase.LoadAssetAtPath<GeckoSpeciesSO>(SPECIES_PATH);
        if (species == null) return $"주의: 종 에셋이 없습니다 ({SPECIES_PATH})";
        if (species.skin == skin) return "크레스티드 종에 이미 연결되어 있습니다";
        species.skin = skin;
        EditorUtility.SetDirty(species);
        return "크레스티드 종의 전용 그림으로 연결했습니다";
    }
}
