using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 프록시 게코 그림을 PNG 에셋으로 저장하고, 그 그림으로 GeckoSkin_Proxy 에셋을 만든다.
/// 다시 실행하면 같은 파일을 덮어쓴다 (GUID 유지 → 씬 연결이 끊기지 않는다).
/// </summary>
internal static class GeckoProxyArt
{
    public const string TEX_DIR   = "Assets/_Game/Textures/Gecko/Proxy";
    public const string SKIN_DIR  = "Assets/_Game/GeckoSkins";
    public const string SKIN_PATH = SKIN_DIR + "/GeckoSkin_Proxy.asset";
    public const string SKIN_HATCHLING_PATH = SKIN_DIR + "/GeckoSkin_Proxy_Hatchling.asset";
    public const string SKIN_JUVENILE_PATH  = SKIN_DIR + "/GeckoSkin_Proxy_Juvenile.asset";

    // 아기 비율 — 어릴수록 머리와 눈이 크고 꼬리가 짧다 (귀여움 공식, ART_GUIDE.md)
    private const float HATCH_HEAD = 1.22f, HATCH_EYE = 1.12f, HATCH_TAIL = 0.88f;
    private const float JUV_HEAD   = 1.10f, JUV_EYE   = 1.05f, JUV_TAIL   = 0.95f;

    /// <summary>
    /// 성장 단계별 그림 (GeckoRig._stageSkins에 넣는다). 0·1 = 해츨링 비율, 2 = 주버나일 비율, 3·4 = 기본(null).
    /// 기본 스킨에서 머리 관절을 중심으로 머리·눈·입·혀를 키워 만든다 — 그림을 새로 그리지 않는다.
    /// </summary>
    public static GeckoSkin[] GenerateStageSkins(GeckoSkin baseSkin)
    {
        if (baseSkin == null) return new GeckoSkin[5];
        var hatch = Derive(baseSkin, SKIN_HATCHLING_PATH, HATCH_HEAD, HATCH_EYE, HATCH_TAIL);
        var juv   = Derive(baseSkin, SKIN_JUVENILE_PATH,  JUV_HEAD,   JUV_EYE,   JUV_TAIL);
        AssetDatabase.SaveAssets();
        return new[] { hatch, hatch, juv, null, null };
    }

    private static GeckoSkin Derive(GeckoSkin src, string path, float head, float eye, float tail)
    {
        var skin = AssetDatabase.LoadAssetAtPath<GeckoSkin>(path);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<GeckoSkin>();
            AssetDatabase.CreateAsset(skin, path);
        }

        Undo.RecordObject(skin, "Generate Stage Skin");
        skin.referenceWidth = src.referenceWidth;   // 전체 크기는 GeckoRig 단계 배율이 정한다
        skin.parts.Clear();
        skin.eyes.Clear();
        skin.mouths.Clear();

        var headArt = src.GetPart(GeckoPartId.Head);
        Vector2 hj  = headArt != null ? headArt.jointPosition : Vector2.zero;

        foreach (var p in src.parts)
        {
            if (p == null) continue;
            var a = new GeckoPartArt
            {
                id = p.id, sprite = p.sprite, jointPosition = p.jointPosition,
                jointPivot = p.jointPivot, scale = p.scale, tint = p.tint,
            };

            switch (p.id)
            {
                case GeckoPartId.Head:
                    a.scale *= head;
                    break;
                case GeckoPartId.EyeL:
                case GeckoPartId.EyeR:
                    a.jointPosition = hj + (p.jointPosition - hj) * head;
                    a.scale *= head * eye;
                    break;
                case GeckoPartId.Mouth:
                case GeckoPartId.Tongue1:
                case GeckoPartId.Tongue2:
                    a.jointPosition = hj + (p.jointPosition - hj) * head;
                    a.scale *= head;
                    break;
                case GeckoPartId.Tail:
                    a.scale = Vector2.Scale(a.scale, new Vector2(tail, Mathf.Lerp(1f, tail, 0.5f)));
                    break;
            }
            skin.parts.Add(a);
        }
        foreach (var e in src.eyes)   if (e != null) skin.eyes.Add(new GeckoEyeArt { state = e.state, left = e.left, right = e.right });
        foreach (var m in src.mouths) if (m != null) skin.mouths.Add(new GeckoMouthArt { state = m.state, sprite = m.sprite });

        EditorUtility.SetDirty(skin);
        return skin;
    }

    public static GeckoSkin Generate()
    {
        EnsureFolder(TEX_DIR);
        EnsureFolder(SKIN_DIR);

        // 1) 그림 → PNG 파일
        var canvases = new Dictionary<string, GeckoProxyPainter.Canvas>
        {
            ["body"]           = GeckoProxyPainter.Body(),
            ["shed_patch"]     = GeckoProxyPainter.ShedPatch(),
            ["head"]           = GeckoProxyPainter.Head(),
            ["tail"]           = GeckoProxyPainter.Tail(),
            ["leg_front"]      = GeckoProxyPainter.LegFront(),
            ["leg_back"]       = GeckoProxyPainter.LegBack(),
            ["shadow_contact"] = GeckoProxyPainter.Shadow(),
            ["tongue_01"]      = GeckoProxyPainter.Tongue1(),
            ["tongue_02"]      = GeckoProxyPainter.Tongue2(),
        };
        foreach (GeckoEye e in System.Enum.GetValues(typeof(GeckoEye)))
            canvases[GeckoParts.EyeSpriteName(e)] = GeckoProxyPainter.Eye(e);
        foreach (GeckoMouth m in System.Enum.GetValues(typeof(GeckoMouth)))
            canvases[GeckoParts.MouthSpriteName(m)] = GeckoProxyPainter.Mouth(m);

        var paths = new List<string>();
        foreach (var kv in canvases)
        {
            string path = $"{TEX_DIR}/{kv.Key}.png";
            WritePng(kv.Value, path);
            paths.Add(path);
        }

        // 2) 스프라이트로 가져오기
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var path in paths)
            {
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                ti.textureType         = TextureImporterType.Sprite;
                ti.spriteImportMode    = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled       = false;
                ti.filterMode          = FilterMode.Bilinear;
                ti.wrapMode            = TextureWrapMode.Clamp;
                ti.textureCompression  = TextureImporterCompression.Compressed;   // 임시 그림 26장 — 압축해도 충분
                ti.maxTextureSize      = 1024;
                ti.spritePixelsPerUnit = 100f;
                ti.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        AssetDatabase.Refresh();

        // 3) 스킨 에셋
        var skin = AssetDatabase.LoadAssetAtPath<GeckoSkin>(SKIN_PATH);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<GeckoSkin>();
            AssetDatabase.CreateAsset(skin, SKIN_PATH);
        }

        Undo.RecordObject(skin, "Generate Proxy Skin");
        skin.referenceWidth = GeckoProxyLayout.ReferenceWidth;
        skin.parts.Clear();
        skin.eyes.Clear();
        skin.mouths.Clear();

        var missing = new List<string>();
        foreach (var p in GeckoProxyLayout.Parts)
        {
            var sprite = LoadSprite(p.sprite, missing);
            skin.parts.Add(new GeckoPartArt
            {
                id            = p.id,
                sprite        = sprite,
                jointPosition = p.joint,
                jointPivot    = p.pivot,
                scale         = p.scale,
                tint          = new Color(p.shade, p.shade, p.shade, 1f),
            });
        }
        foreach (GeckoEye e in System.Enum.GetValues(typeof(GeckoEye)))
            skin.eyes.Add(new GeckoEyeArt { state = e, left = LoadSprite(GeckoParts.EyeSpriteName(e), missing) });
        foreach (GeckoMouth m in System.Enum.GetValues(typeof(GeckoMouth)))
            skin.mouths.Add(new GeckoMouthArt { state = m, sprite = LoadSprite(GeckoParts.MouthSpriteName(m), missing) });

        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();

        if (missing.Count > 0)
            Debug.LogWarning("[GeckoProxyArt] 스프라이트를 불러오지 못함: " + string.Join(", ", missing));
        else
            Debug.Log($"[GeckoProxyArt] 프록시 그림 {paths.Count}장 + {SKIN_PATH} 생성 완료");

        return skin;
    }

    private static Sprite LoadSprite(string name, List<string> missing)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{TEX_DIR}/{name}.png");
        if (s == null && !missing.Contains(name)) missing.Add(name);
        return s;
    }

    private static void WritePng(GeckoProxyPainter.Canvas c, string assetPath)
    {
        var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false);
        try
        {
            tex.SetPixels(c.Px);
            tex.Apply(false);
            File.WriteAllBytes(Path.GetFullPath(assetPath), tex.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(tex);
        }
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
