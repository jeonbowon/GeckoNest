using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Imports the approved elevated artwork without modifying any source PNG or saved game.</summary>
public static class HakoObliqueArt
{
    public const string Folder = "Assets/_Game/Textures/Oblique/";
    public const string SkinPath = "Assets/_Game/GeckoSkins/GeckoSkin_Oblique.asset";

    public static void BuildBatch()
    {
        try { Build(); HakoSelfTest.RunBatch(); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    [MenuItem("Hako/Gecko/사선 시점 아트 연결")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before linking artwork.");
        foreach (var path in Directory.GetFiles(Folder, "*.png"))
        {
            string normalized = path.Replace('\\', '/');
            AssetDatabase.ImportAsset(normalized, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(normalized);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        LinkDecor("bg_jungle", "theme_jungle_oblique_v1", 0f);
        LinkDecor("decor_cave", "cave_oblique_v1", 0.17f);
        LinkDecor("decor_moss_rock", "rock_oblique_v1", 0.13f);
        LinkDecor("decor_cork", "cork_oblique_v1", 0.025f);
        var branch = LinkDecor("decor_branch", "branch_oblique_v1", 0f);
        // Measured foot-contact line on the generated branch, in its 480 x 600 UI rect.
        branch.climbFootLine = new[] { new Vector2(60, 70), new Vector2(220, 295), new Vector2(310, 510), new Vector2(375, 510) };
        EditorUtility.SetDirty(branch);
        BuildSkin();
        AssetDatabase.SaveAssets();
        Debug.Log("[HakoObliqueArt] Elevated background, decor, skin and atlases linked. Original PNGs and skin preserved.");
    }

    private static DecorItemSO LinkDecor(string id, string art, float baseline)
    {
        var item = AssetDatabase.LoadAssetAtPath<DecorItemSO>("Assets/_Game/Resources/Decor/" + id + ".asset");
        if (item == null) throw new InvalidOperationException("Missing decor " + id);
        item.previewSprite = item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + art + ".png");
        item.baseline = baseline;
        // Oblique cave has a sloping entrance: legacy rectangular portal clipping would cut through rock.
        if (id == "decor_cave") item.doorRect = default;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static void BuildSkin()
    {
        var skin = AssetDatabase.LoadAssetAtPath<GeckoSkin>(SkinPath);
        if (skin == null) { skin = ScriptableObject.CreateInstance<GeckoSkin>(); AssetDatabase.CreateAsset(skin, SkinPath); }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "hako_oblique_v1.png");
        const float W = 1536f, H = 1024f;
        if (sprite == null || sprite.rect.width != W || sprite.rect.height != H)
            throw new InvalidOperationException("Oblique anatomy coordinates require the inspected 1536 x 1024 sprite.");
        var origin = new Vector2(900, 824);
        Vector2 Uv(float x, float y) => new Vector2(x / W, (H - y) / H);
        Vector2 Pos(float x, float y) => new Vector2(x - origin.x, origin.y - y);
        Rect Region(float x, float y, float w, float h) => new Rect(x / W, (H-y-h)/H, w/W, h/H);
        skin.referenceWidth = W * 640f / 510f; // [TBD] adult is ~485 visible UI units before depth scaling
        skin.wholeBody = true;
        skin.independentWholeLegs = true;
        skin.wholeHeadPivot = Uv(1160, 530);
        skin.wholeHeadZone = new Vector4(1110/W, 1270/W, (H-640)/H, (H-570)/H);
        skin.wholeHeadGain = 0.7f;
        skin.wholeTailZone = new Vector2(660/W, 530/W);
        skin.wholeTailChain = new[] { Uv(650,470), Uv(535,435), Uv(400,400), Uv(265,370), Uv(135,342), Uv(62,302) };
        skin.wholeEyeAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "hako_eyes_oblique_v1.png");
        skin.wholeMouthAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "hako_mouths_oblique_v1.png");
        skin.wholeEyeLeftRect = Region(1280, 462, 118, 121);
        skin.wholeEyeRightRect = default; // only a sliver of the far eye is visible in this angle
        skin.wholeMouthRect = Region(1238, 511, 252, 145);
        skin.wholeMouthAngle = 13f;
        skin.wholeFaceInsets = new Vector2(0.08f, 0.02f);
        skin.parts.Clear(); skin.wholeLegs.Clear(); skin.eyes.Clear(); skin.mouths.Clear();
        skin.parts.Add(new GeckoPartArt { id = GeckoPartId.Body, sprite = sprite,
            jointPosition = Pos(900,530), jointPivot = Uv(900,530) });
        void Ghost(GeckoPartId id, float x, float y, float width, float height, float jx, float jy)
        {
            skin.parts.Add(new GeckoPartArt { id = id, hitSize = new Vector2(width,height),
                jointPosition = Pos(jx,jy), jointPivot = new Vector2((jx-x)/width, (y+height-jy)/height) });
        }
        Ghost(GeckoPartId.Head, 1150,360,355,270,1160,530);
        Ghost(GeckoPartId.EyeL, 1274,455,130,138,1338,522);
        Ghost(GeckoPartId.EyeR, 1407,404,33,56,1422,432);
        Ghost(GeckoPartId.Mouth, 1250,553,245,60,1380,582);
        Ghost(GeckoPartId.Tail, 40,278,610,235,650,470);
        void Leg(GeckoPartId id, float jx, float jy, float fx, float fy, float top, float foot)
        {
            skin.wholeLegs.Add(new GeckoWholeLimb { id=id, joint=Uv(jx,jy), foot=Uv(fx,fy), radius=new Vector2(top/W,foot/W) });
            Ghost(id, fx-foot, fy-foot, foot*2, foot*2, jx,jy);
        }
        Leg(GeckoPartId.LegFrontNear,1115,605,1135,784,45,88);
        Leg(GeckoPartId.LegFrontFar,1060,445,1125,360,33,67);
        Leg(GeckoPartId.LegBackNear,678,511,663,674,44,81);
        Leg(GeckoPartId.LegBackFar,654,394,734,302,31,67);
        var old = AssetDatabase.LoadAssetAtPath<GeckoSkin>("Assets/_Game/GeckoSkins/GeckoSkin_Painted.asset");
        if (old != null)
        {
            float scale = skin.referenceWidth / old.referenceWidth;
            var t1 = old.GetPart(GeckoPartId.Tongue1); var t2 = old.GetPart(GeckoPartId.Tongue2);
            if (t1 != null && t2 != null)
            {
                Vector2 root = Pos(1465,555);
                skin.parts.Add(new GeckoPartArt { id=GeckoPartId.Tongue1, sprite=t1.sprite, jointPosition=root,
                    jointPivot=t1.jointPivot, scale=t1.scale*scale, tint=t1.tint });
                skin.parts.Add(new GeckoPartArt { id=GeckoPartId.Tongue2, sprite=t2.sprite,
                    jointPosition=root+(t2.jointPosition-t1.jointPosition)*scale, jointPivot=t2.jointPivot, scale=t2.scale*scale, tint=t2.tint });
            }
            var shadow = old.GetPart(GeckoPartId.Shadow);
            if (shadow != null) skin.parts.Add(new GeckoPartArt { id=GeckoPartId.Shadow, sprite=shadow.sprite,
                jointPosition=Pos(950,690), jointPivot=new Vector2(0.5f,0.5f), scale=new Vector2(1.65f,2.1f), tint=new Color(0.16f,0.13f,0.08f,0.3f) });
        }
        EditorUtility.SetDirty(skin);
        var species = AssetDatabase.LoadAssetAtPath<GeckoSpeciesSO>("Assets/_Game/Resources/Species/crested.asset");
        species.skin = skin;
        species.thumbnailSprite = sprite;
        EditorUtility.SetDirty(species);
    }
}
