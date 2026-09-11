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
    public const string SKIN_DIR  = "Assets/_Game/ScriptableObjects/GeckoSkins";
    public const string SKIN_PATH = SKIN_DIR + "/GeckoSkin_Proxy.asset";

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
                ti.textureCompression  = TextureImporterCompression.Uncompressed;
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
