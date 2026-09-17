using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 꾸미기 구조물 임시 그림(코르크 뒤판·덩굴·나뭇가지) PNG와 장식 에셋을 만든다. 메뉴 없이 배치 실행 전용:
/// Unity.exe -batchmode -nographics -projectPath (프로젝트) -executeMethod DecorProxyArt.GenerateBatch -logFile (로그)
///
/// - 기존 장식(은신처·바위·화분)에는 놓는 곳·쓰임·아래 여백만 채운다
/// - 이미 있는 PNG는 덮어쓰지 않는다 (최종 그림 보호). 다시 그리려면 PNG를 지우고 실행
/// - 최종 그림은 Textures/Decor의 같은 이름 PNG를 바꿔 끼우면 된다 — 크기·나뭇가지 선 위치는 TerrariumLayout과 같게
/// </summary>
public static class DecorProxyArt
{
    private const string TEX_DIR   = "Assets/_Game/Textures/Decor";
    private const string ASSET_DIR = "Assets/_Game/Resources/Decor";

    private static readonly Color CORK_DARK  = new Color(0.50f, 0.34f, 0.20f);
    private static readonly Color CORK_LIGHT = new Color(0.72f, 0.54f, 0.34f);
    private static readonly Color CORK_EDGE  = new Color(0.30f, 0.19f, 0.10f);
    private static readonly Color STEM       = new Color(0.24f, 0.38f, 0.16f);
    private static readonly Color LEAF_DARK  = new Color(0.22f, 0.48f, 0.20f);
    private static readonly Color LEAF_LIGHT = new Color(0.42f, 0.68f, 0.32f);
    private static readonly Color BARK       = new Color(0.44f, 0.29f, 0.17f);
    private static readonly Color BARK_LINE  = new Color(0.28f, 0.17f, 0.09f);
    private static readonly Color OUTLINE    = new Color(0.18f, 0.12f, 0.07f);

    public static void GenerateBatch()
    {
        try
        {
            Generate();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[DecorProxyArt] 실패: " + e);
            EditorApplication.Exit(1);
        }
    }

    public static void Generate()
    {
        Directory.CreateDirectory(TEX_DIR);
        var cork   = MakeSprite("decor_cork",   TerrariumLayout.ImageSize(DecorUse.ClimbPanel), DrawCork);
        var vine   = MakeSprite("decor_vine",   TerrariumLayout.ImageSize(DecorUse.Vine),       DrawVine);
        var branch = MakeSprite("decor_branch", TerrariumLayout.BRANCH_SIZE,                    DrawBranch);

        // 어덜트 전용 (requiredAdults = 키운 어덜트 수)
        var moss  = MakeSprite("decor_moss_rock", TerrariumLayout.ImageSize(DecorUse.None), DrawMossRock);
        var cave  = MakeSprite("decor_cave",      TerrariumLayout.ImageSize(DecorUse.Hide), DrawCave);
        var drift = MakeSprite("decor_driftwood", TerrariumLayout.BRANCH_SIZE,              DrawDriftwood);

        Upsert("decor_cork",   "코르크 뒤판", cork,   60, DecorUse.ClimbPanel);   // [TBD] 가격
        Upsert("decor_vine",   "덩굴",        vine,   40, DecorUse.Vine);
        Upsert("decor_branch", "나뭇가지",    branch, 80, DecorUse.Branch);
        Upsert("decor_moss_rock", "이끼 바위", moss,  60,  DecorUse.None,   DecorPlacement.Floor, MOSS_BASE, 1);   // [TBD] 가격·조건
        Upsert("decor_cave",      "동굴",      cave,  120, DecorUse.Hide,   DecorPlacement.Floor, CAVE_BASE, 2);
        Upsert("decor_driftwood", "큰 유목",   drift, 150, DecorUse.Branch, DecorPlacement.Wall,  0f,        3);

        SetKind("decor_hide",  DecorPlacement.Floor, DecorUse.Hide, 0.22f);   // 그림 아래 여백 — 지금 그림 기준
        SetKind("decor_rock",  DecorPlacement.Floor, DecorUse.None, 0.20f);
        SetKind("decor_plant", DecorPlacement.Floor, DecorUse.None, 0.08f);

        AssetDatabase.SaveAssets();
        Debug.Log("[DecorProxyArt] 구조물 임시 그림 6장 + 장식 에셋 완료");
    }

    // ── 파일 ─────────────────────────────────────────────────

    private static Sprite MakeSprite(string name, Vector2 size, Func<float, float, int, int, Color> draw)
    {
        string path = $"{TEX_DIR}/{name}.png";
        if (!File.Exists(path))
        {
            int w = Mathf.RoundToInt(size.x), h = Mathf.RoundToInt(size.y);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[x + y * w] = draw(x + 0.5f, y + 0.5f, w, h);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[DecorProxyArt] 그림 생성 — {path} ({w}x{h})");
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || !importer.alphaIsTransparency)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.maxTextureSize      = 2048;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Upsert(string id, string displayName, Sprite sprite, int coinPrice, DecorUse use,
                               DecorPlacement placement = DecorPlacement.Wall, float baseline = 0f, int requiredAdults = 0)
    {
        string path = $"{ASSET_DIR}/{id}.asset";
        var item = AssetDatabase.LoadAssetAtPath<DecorItemSO>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<DecorItemSO>();
            item.itemId      = id;
            item.displayName = displayName;
            item.category    = DecorCategory.Decoration;
            item.coinPrice   = coinPrice;
            AssetDatabase.CreateAsset(item, path);
            Debug.Log($"[DecorProxyArt] 장식 에셋 생성 — {path}");
        }
        if (item.icon == null)          item.icon          = sprite;
        if (item.previewSprite == null) item.previewSprite = sprite;
        item.placement      = placement;
        item.use            = use;
        item.baseline       = baseline;
        item.requiredAdults = requiredAdults;
        EditorUtility.SetDirty(item);
    }

    private static void SetKind(string id, DecorPlacement placement, DecorUse use, float baseline)
    {
        var item = AssetDatabase.LoadAssetAtPath<DecorItemSO>($"{ASSET_DIR}/{id}.asset");
        if (item == null)
        {
            Debug.LogWarning($"[DecorProxyArt] 장식 에셋 없음 — {id}");
            return;
        }
        item.placement = placement;
        item.use       = use;
        item.baseline  = baseline;
        EditorUtility.SetDirty(item);
    }

    // ── 그림 (좌표: 왼쪽 아래 원점, 픽셀 가운데) ────────────────

    // 코르크 뒤판 — 가장자리가 울퉁불퉁한 둥근 판, 얼룩덜룩한 코르크 알갱이
    private static Color DrawCork(float x, float y, int w, int h)
    {
        const float MARGIN = 14f, R = 40f;
        float qx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - MARGIN - R);
        float qy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - MARGIN - R);
        float d  = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - R;
        d += 5f * Mathf.PerlinNoise(x * 0.04f, y * 0.04f) - 2.5f;   // 울퉁불퉁한 가장자리

        float a = Mathf.Clamp01(0.5f - d);
        if (a <= 0f) return Color.clear;

        float n = Mathf.PerlinNoise(x * 0.08f + 3.1f, y * 0.08f + 7.3f);
        Color c = Color.Lerp(CORK_DARK, CORK_LIGHT, n);
        float h1 = Hash((int)(x / 3f), (int)(y / 3f));
        if (h1 > 0.86f)      c *= 0.78f;   // 어두운 알갱이
        else if (h1 < 0.06f) c *= 1.15f;   // 밝은 알갱이
        c = Color.Lerp(c, CORK_EDGE, Mathf.Clamp01((d + 12f) / 12f) * 0.85f);   // 가장자리 어둡게
        c.a = a;
        return c;
    }

    // 덩굴 — 구불구불한 줄기와 좌우 번갈아 달린 잎
    private static Color DrawVine(float x, float y, int w, int h)
    {
        const float STEP = 90f;
        float best = Mathf.Abs(x - VineX(y, w)) - (7f + 2f * Mathf.Sin(y * 0.05f));
        if (y < 12f || y > h - 12f) best = Mathf.Max(best, Mathf.Max(12f - y, y - (h - 12f)));   // 위아래 끝을 닫는다
        bool  leaf  = false;
        float shade = 0f;

        int k = Mathf.RoundToInt(y / STEP);
        for (int j = k - 1; j <= k + 1; j++)
        {
            float ly = j * STEP;
            if (j < 1 || ly > h - 50f) continue;
            float side = (j % 2 == 0) ? 1f : -1f;
            Vector2 p  = new Vector2(x, y) - new Vector2(VineX(ly, w) + side * 36f, ly + 14f);
            float ang  = side * 0.5f;
            var q = new Vector2(p.x * Mathf.Cos(ang) + p.y * Mathf.Sin(ang), -p.x * Mathf.Sin(ang) + p.y * Mathf.Cos(ang));
            float e = (new Vector2(q.x / 36f, q.y / 16f).magnitude - 1f) * 14f;
            if (e < best)
            {
                best  = e;
                leaf  = true;
                shade = Mathf.Clamp01(0.5f + q.y / 32f);
            }
        }

        float a = Mathf.Clamp01(0.5f - best);
        if (a <= 0f) return Color.clear;
        Color c = leaf ? Color.Lerp(LEAF_DARK, LEAF_LIGHT, shade) : STEM;
        if (best > -2.5f) c = Color.Lerp(c, OUTLINE, 0.6f);   // 외곽선
        c.a = a;
        return c;
    }

    private static float VineX(float y, int w) => w * 0.5f + 34f * Mathf.Sin(y * 0.011f) + 10f * Mathf.Sin(y * 0.037f);

    // ── 어덜트 전용 ──────────────────────────────────────────

    private const float MOSS_BASE = 0.12f;   // 그림 아래 투명 여백 (에셋 baseline과 같게)
    private const float CAVE_BASE = 0.10f;

    private static readonly Color ROCK_DARK  = new Color(0.36f, 0.36f, 0.38f);
    private static readonly Color ROCK_LIGHT = new Color(0.60f, 0.59f, 0.57f);
    private static readonly Color MOSS_DARK  = new Color(0.20f, 0.42f, 0.16f);
    private static readonly Color MOSS_LIGHT = new Color(0.44f, 0.66f, 0.26f);
    private static readonly Color CAVE_IN    = new Color(0.08f, 0.07f, 0.06f);
    private static readonly Color DRIFT      = new Color(0.74f, 0.68f, 0.58f);
    private static readonly Color DRIFT_LINE = new Color(0.50f, 0.43f, 0.34f);

    // 이끼 바위 — 울퉁불퉁한 둥근 바위, 윗부분에 이끼
    private static Color DrawMossRock(float x, float y, int w, int h)
    {
        float baseY = h * MOSS_BASE;
        var c0 = new Vector2(w * 0.5f, baseY + 100f);
        Vector2 q = new Vector2(x, y) - c0;
        float d = (new Vector2(q.x / 135f, q.y / 105f).magnitude - 1f) * 100f;
        d += 8f * Mathf.PerlinNoise(x * 0.03f, y * 0.03f) - 4f;
        d = Mathf.Max(d, baseY - y);   // 바닥은 평평하게
        float a = Mathf.Clamp01(0.5f - d);
        if (a <= 0f) return Color.clear;

        Color c = Color.Lerp(ROCK_DARK, ROCK_LIGHT, Mathf.Clamp01(0.35f + q.y / 180f + 0.3f * Mathf.PerlinNoise(x * 0.07f, y * 0.07f)));
        float mossLine = c0.y + 20f + 25f * Mathf.PerlinNoise(x * 0.04f + 5f, 1.3f);   // 이 높이 위는 이끼
        if (y > mossLine)
            c = Color.Lerp(MOSS_DARK, MOSS_LIGHT, Mathf.PerlinNoise(x * 0.12f, y * 0.12f));
        if (d > -3f) c = Color.Lerp(c, OUTLINE, 0.6f);
        c.a = a;
        return c;
    }

    // 동굴 — 둥근 바위 언덕 가운데 아래에 어두운 입구 (은신처처럼 게코를 가린다)
    private static Color DrawCave(float x, float y, int w, int h)
    {
        float baseY = h * CAVE_BASE;
        Vector2 q = new Vector2(x - w * 0.5f, y - baseY);
        float d = (new Vector2(q.x / 218f, q.y / 330f).magnitude - 1f) * 200f;
        d += 10f * Mathf.PerlinNoise(x * 0.025f, y * 0.025f) - 5f;
        d = Mathf.Max(d, -q.y);
        float a = Mathf.Clamp01(0.5f - d);
        if (a <= 0f) return Color.clear;

        float door = (new Vector2(q.x / 105f, q.y / 150f).magnitude - 1f) * 100f;   // 입구
        Color c;
        if (door < 0f)
        {
            c = Color.Lerp(ROCK_DARK * 0.45f, CAVE_IN, Mathf.Clamp01(-door / 25f));   // 가장자리만 살짝 밝게
        }
        else
        {
            float n = Mathf.PerlinNoise(x * 0.05f + 2f, y * 0.05f);
            c = Color.Lerp(ROCK_DARK, ROCK_LIGHT, Mathf.Clamp01(0.25f + q.y / 500f + 0.4f * n));
            if (Hash((int)(x / 18f), (int)(y / 14f)) > 0.8f) c *= 0.85f;                  // 돌 무늬
            if (q.y > 250f + 30f * Mathf.PerlinNoise(x * 0.03f, 4.2f))                      // 꼭대기 이끼
                c = Color.Lerp(MOSS_DARK, MOSS_LIGHT, Mathf.PerlinNoise(x * 0.1f, y * 0.1f));
            if (door < 6f) c = Color.Lerp(c, OUTLINE, 0.7f);                                  // 입구 테두리
        }
        if (d > -3f) c = Color.Lerp(c, OUTLINE, 0.6f);
        c.a = a;
        return c;
    }

    // 큰 유목 — 나뭇가지와 같은 선(BRANCH_LINE), 굵고 하얗게 바랜 나무, 잎 없음
    private static Color DrawDriftwood(float x, float y, int w, int h)
        => DrawBranchStyle(x, y, DRIFT, DRIFT_LINE, leaves: false);   // 굵기는 같게 — 게코 발이 가지 위에 놓이도록

    // 나뭇가지 — TerrariumLayout.BRANCH_LINE을 따라 끝으로 갈수록 가늘어지는 가지 + 잔가지 + 잎
    private static Color DrawBranch(float x, float y, int w, int h)
        => DrawBranchStyle(x, y, BARK, BARK_LINE, leaves: true);

    private static Color DrawBranchStyle(float x, float y, Color bark, Color barkLine, bool leaves)
    {
        var line  = TerrariumLayout.BRANCH_LINE;
        var p     = new Vector2(x, y);
        float best = float.MaxValue, along = 0f, total = 0f;
        int segs = line.Length - 1;

        for (int i = 0; i < segs; i++)
        {
            Vector2 a = line[i], ab = line[i + 1] - a;
            float t    = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float half = TerrariumLayout.BRANCH_THICK * 0.5f * Mathf.Lerp(1f, 0.7f, (i + t) / segs);
            float d    = (p - (a + ab * t)).magnitude - half;
            if (d < best)
            {
                best  = d;
                along = total + t * ab.magnitude;
            }
            total += ab.magnitude;
        }

        // 잔가지 — 첫 구간 중간에서 위쪽으로
        Vector2 t0 = Vector2.Lerp(line[0], line[1], 0.55f), t1 = t0 + new Vector2(-70f, 70f);
        float twig = SegDist(p, t0, t1) - 8f;
        bool leaf = false;
        float shade = 0f;
        if (twig < best) best = twig;

        // 잎 — 잔가지 끝과 가지 끝
        var leafSpots = leaves
            ? new[] { (t1 + new Vector2(-10f, 16f), 0.6f), (line[2] + new Vector2(-8f, 30f), -0.4f), (line[1] + new Vector2(-6f, 34f), 0.2f) }
            : new (Vector2, float)[0];
        foreach (var (c0, ang) in leafSpots)
        {
            Vector2 q0 = p - c0;
            var q = new Vector2(q0.x * Mathf.Cos(ang) + q0.y * Mathf.Sin(ang), -q0.x * Mathf.Sin(ang) + q0.y * Mathf.Cos(ang));
            float e = (new Vector2(q.x / 34f, q.y / 15f).magnitude - 1f) * 14f;
            if (e < best)
            {
                best  = e;
                leaf  = true;
                shade = Mathf.Clamp01(0.5f + q.y / 30f);
            }
        }

        float alpha = Mathf.Clamp01(0.5f - best);
        if (alpha <= 0f) return Color.clear;

        Color c;
        if (leaf)
        {
            c = Color.Lerp(LEAF_DARK, LEAF_LIGHT, shade);
        }
        else
        {
            float stripe = Mathf.Sin(along * 0.22f + 6f * Mathf.PerlinNoise(x * 0.05f, y * 0.05f));
            c = Color.Lerp(bark, barkLine, Mathf.Clamp01(stripe * 0.6f));
            c *= 0.9f + 0.2f * Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
        }
        if (best > -3f) c = Color.Lerp(c, OUTLINE, 0.65f);   // 외곽선
        c.a = alpha;
        return c;
    }

    private static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }

    private static float Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
        }
    }
}
