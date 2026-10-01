using UnityEditor;
using UnityEngine;

/// <summary>Connects the transparent mealworm sprites while preserving the original source PNGs.</summary>
public static class HakoFoodArtFix
{
    private const string MealwormTexture = "Assets/_Game/Textures/mealworm_transparent_v2.png";
    private const string SuperwormTexture = "Assets/_Game/Textures/superworm_transparent_v2.png";

    [MenuItem("Hako/아트/먹이 투명 배경 연결")]
    public static void Build()
    {
        Connect("Assets/_Game/Resources/Items/mealworm.asset", MealwormTexture);
        Connect("Assets/_Game/Resources/Items/superworm.asset", SuperwormTexture);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HakoFoodArtFix] Transparent mealworm and superworm sprites connected.");
    }

    public static void BuildBatch()
    {
        Build();
        EditorApplication.Exit(0);
    }

    private static void Connect(string itemPath, string texturePath)
    {
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
            throw new System.InvalidOperationException("Texture importer missing: " + texturePath);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();

        var item = AssetDatabase.LoadAssetAtPath<ItemSO>(itemPath);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        if (item == null || sprite == null)
            throw new System.InvalidOperationException("Food item or sprite missing: " + itemPath);

        item.icon = sprite;
        EditorUtility.SetDirty(item);
    }
}
