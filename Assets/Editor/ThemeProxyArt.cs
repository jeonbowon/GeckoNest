using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 임시 정글 테마 그림을 만든다 — 정글 배경(잎사귀 벽) 아래쪽에 코드로 그린 흙 바닥을 원근으로 깔아 한 장으로 합친다.
/// 메뉴 없이 배치 실행 전용:
/// Unity.exe -batchmode -nographics -projectPath (프로젝트) -executeMethod ThemeProxyArt.GenerateBatch -logFile (로그)
///
/// 왜 (2026-09-21): 배경·바닥을 "테마" 하나로 합쳤는데, 지금 정글 배경은 위에서 아래까지 잎사귀 벽이라
/// 게코·바위·동굴이 벽지 위에 떠 있는 것처럼 보였다. 최종 그림이 오기 전까지 쓰는 임시 합성이다.
///
/// - 결과: Textures/Backgrounds/theme_jungle.png (정글 배경과 같은 크기) → bg_jungle 테마 에셋이 이 그림을 쓴다
/// - **이미 있는 theme_jungle.png는 덮어쓰지 않는다** (최종 그림 보호). 다시 합치려면 PNG만 지우고 실행 (.meta를 두면 에셋 연결이 그대로다)
/// - 최종 그림 규격: 1080×2400(세로), 화면 아래 **발 높이 380~950이 땅 위**, 그 위는 뒷벽 — 땅 윗선(지평선)은 1060 근처
/// </summary>
public static class ThemeProxyArt
{
    private const string DIR      = "Assets/_Game/Textures/Backgrounds";
    private const string WALL     = DIR + "/bg_jungle.png";
    private const string OUT      = DIR + "/theme_jungle.png";
    private const string THEME_SO = "Assets/_Game/Resources/Decor/bg_jungle.asset";

    // 화면 좌표 (캔버스 1080×2400, 아래가 0) — 게코 발 높이 380~950보다 조금 위까지 땅
    private const float HORIZON     = 1060f;   // 땅 윗선
    private const float EDGE_SOFT   = 70f;     // 땅이 잎사귀 벽으로 녹아드는 폭
    private const float EDGE_WOBBLE = 38f;     // 땅 윗선의 들쭉날쭉
    private const float FAR_SCALE   = 0.42f;   // 지평선 쪽 흙 무늬 크기 (가까운 쪽 = 1)
    private const float CANVAS_H    = 2400f;

    private static readonly Color FAR_HAZE = new Color(0.40f, 0.45f, 0.33f);   // 먼 땅은 초록빛 안개로

    public static void GenerateBatch()
    {
        try
        {
            Generate();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("[ThemeProxyArt] 실패: " + e);
            EditorApplication.Exit(1);
        }
    }

    public static void Generate()
    {
        if (!File.Exists(OUT))
        {
            var wall = Load(WALL);
            try
            {
                var tex = Compose(wall);
                File.WriteAllBytes(OUT, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(OUT, ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"[ThemeProxyArt] 테마 그림 생성 — {OUT} ({wall.width}x{wall.height})");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wall);
            }
        }

        // 정글 배경과 같은 가져오기 설정 (세로 2411이라 최대 크기 4096)
        var importer = (TextureImporter)AssetImporter.GetAtPath(OUT);
        if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || importer.maxTextureSize < 4096)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled       = false;
            importer.maxTextureSize      = 4096;
            importer.SaveAndReimport();
        }

        // 정글 테마가 이 그림을 쓴다 (홈 배경 = previewSprite, 꾸미기 목록 = icon)
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OUT);
        var theme  = AssetDatabase.LoadAssetAtPath<DecorItemSO>(THEME_SO);
        if (sprite != null && theme != null)
        {
            theme.previewSprite = sprite;
            theme.icon          = sprite;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[ThemeProxyArt] bg_jungle 테마 → theme_jungle.png");
        }
    }

    private static Texture2D Load(string path)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(path))) throw new Exception("그림을 읽지 못함 — " + path);
        return tex;
    }

    // ── 합성 ─────────────────────────────────────────────────

    // 땅 색 — 가까운 흙은 짙고, 먼 흙은 밝고 흐리게
    private static readonly Color SOIL_NEAR = new Color(0.30f, 0.19f, 0.11f);
    private static readonly Color SOIL_FAR  = new Color(0.43f, 0.32f, 0.20f);
    private static readonly Color MOSS      = new Color(0.27f, 0.40f, 0.16f);
    private static readonly Color GRAVEL    = new Color(0.58f, 0.50f, 0.40f);
    private static readonly Color LITTER    = new Color(0.46f, 0.30f, 0.14f);   // 떨어진 잎·잔가지

    // 흙 그림을 되풀이하면 조약돌 줄이 300px마다 겹쳐 지층처럼 보였다 (2026-09-21) —
    // 땅을 코드로 그린다: 반복 없는 흙 결(잡음) · 벽 밑동 쪽 이끼 · 멀수록 작아지는 자갈 · 벽에서 늘어진 풀
    private static Texture2D Compose(Texture2D wall)
    {
        int w = wall.width, h = wall.height;
        var wallPx = wall.GetPixels32();

        float toTex   = h / CANVAS_H;           // 캔버스 단위 → 이 그림 픽셀
        float horizon = HORIZON * toTex;
        float soft    = EDGE_SOFT * toTex;
        float wobble  = EDGE_WOBBLE * toTex;

        // 땅 위 앞뒤 거리 — 먼 쪽일수록 한 줄이 멀리 간다 (원근). 되풀이가 없어 줄무늬가 생기지 않는다
        var depth = new float[h];
        float acc = 0f;
        for (int y = 0; y < h; y++)
        {
            acc += 1f / Mathf.Lerp(1f, FAR_SCALE, Mathf.Clamp01(y / horizon));
            depth[y] = acc;
        }

        var outPx = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float t     = Mathf.Clamp01(y / horizon);                 // 0 = 가까움, 1 = 지평선
            float scale = Mathf.Lerp(1f, FAR_SCALE, t);
            float gz    = depth[y];

            for (int x = 0; x < w; x++)
            {
                Color wallC = wallPx[x + y * w];

                // 땅 윗선 — 물결 + 곳곳에서 벽의 풀이 땅으로 늘어진다(윗선이 그만큼 내려감)
                float tuft  = Mathf.Pow(Mathf.Max(0f, Fbm(x * 0.012f, 7.3f, 2) - 0.52f) * 2.1f, 1.5f) * 95f * toTex;
                float edge  = horizon + Wobble(x, w) * wobble - tuft;
                float cover = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge - soft, edge + soft * 0.6f, y));
                if (cover <= 0f) { outPx[x + y * w] = wallC; continue; }

                float gx = (x - w * 0.5f) / scale;   // 땅 위 좌우 (먼 쪽은 넓게 = 무늬가 작게)

                // 흙 — 큰 얼룩 + 잔결
                float big  = Fbm(gx * 0.0055f, gz * 0.0055f, 4);
                float fine = Noise(gx * 0.09f, gz * 0.09f);
                Color c = Color.Lerp(SOIL_NEAR, SOIL_FAR, t * 0.85f);
                c *= 0.82f + big * 0.36f + (fine - 0.5f) * 0.10f;

                // 떨어진 잎 — 불그스름한 얼룩
                float litter = Fbm(gx * 0.021f + 31f, gz * 0.021f, 3);
                c = Color.Lerp(c, LITTER, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.60f, 0.72f, litter)) * 0.45f);

                // 이끼 — 벽 밑동(먼 쪽)에 모이고 앞쪽은 드문드문 (많으면 위장무늬처럼 보였다)
                float moss = Fbm(gx * 0.016f + 97f, gz * 0.016f, 4) + t * 0.18f;
                c = Color.Lerp(c, MOSS, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.70f, 0.86f, moss))
                                        * Mathf.Lerp(0.25f, 0.70f, t));

                // 자갈 — 잘게 박힌 밝은 점 (땅 좌표라 멀수록 작다)
                float grit = Noise(gx * 0.16f + 5f, gz * 0.16f);
                if (grit > 0.84f) c = Color.Lerp(c, GRAVEL, Mathf.InverseLerp(0.84f, 0.95f, grit) * 0.75f);

                // 먼 땅은 안개, 가까운 땅(하단 버튼 뒤)은 어둡게, 벽과 만나는 선 바로 아래는 그늘
                c = Color.Lerp(c, FAR_HAZE, Mathf.Pow(t, 1.8f) * 0.40f);
                c *= Mathf.Lerp(0.66f, 1f, Mathf.Clamp01(y / (h * 0.16f)));
                c *= Mathf.Lerp(1f, 0.70f, Mathf.Clamp01((y - (edge - soft * 2.4f)) / (soft * 2.4f)));
                c.a = 1f;

                outPx[x + y * w] = Color.Lerp(wallC, c, cover);
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(outPx);
        tex.Apply();
        return tex;
    }

    // ── 잡음 (값 잡음 + 겹쳐 쌓기) — 같은 입력이면 늘 같은 그림 ──

    private static float Hash(int x, int y)
    {
        unchecked
        {
            int n = x * 374761393 + y * 668265263;
            n = (n ^ (n >> 13)) * 1274126177;
            return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
        }
    }

    private static float Noise(float x, float y)
    {
        int   xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float tx = x - xi, ty = y - yi;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float a = Mathf.Lerp(Hash(xi, yi),     Hash(xi + 1, yi),     tx);
        float b = Mathf.Lerp(Hash(xi, yi + 1), Hash(xi + 1, yi + 1), tx);
        return Mathf.Lerp(a, b, ty);
    }

    private static float Fbm(float x, float y, int octaves)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum  += Noise(x, y) * amp;
            norm += amp;
            x *= 2.03f; y *= 2.03f; amp *= 0.5f;
        }
        return sum / norm;
    }

    // 가로 위치에 따른 -1~1 물결 (여러 주기를 섞어 규칙적으로 안 보이게)
    private static float Wobble(int x, int w)
    {
        float u = (float)x / w * Mathf.PI * 2f;
        return (Mathf.Sin(u * 3f + 0.7f) * 0.5f + Mathf.Sin(u * 7f + 2.1f) * 0.3f + Mathf.Sin(u * 13f + 4.4f) * 0.2f);
    }
}
