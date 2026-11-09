using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Importer policy for generated/authored art. The Unity AI Generators cannot set Unity
/// import settings, so this postprocessor owns them instead.
///
/// Rules:
///   albedo/emissive  : sRGB, mipmaps, Repeat, max 1024, BC7/High Quality
///   masks/normal maps: linear; normal maps use TextureImporterType.NormalMap
///   HUD sprites      : Sprite (2D and UI), no mipmaps, Clamp, Bilinear
/// </summary>
public class ArtImportSettings : AssetPostprocessor
{
    const string GeneratedRoot = "Assets/Art/Generated";
    const string FallbackRoot = "Assets/Art/GeneratedFallback";

    [MenuItem("Discesa/Apply Art Import Settings")]
    public static void ApplyAll()
    {
        int count = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { GeneratedRoot, FallbackRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Apply(path)) count++;
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Discesa] Art import settings applied to {count} texture(s).");
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(GeneratedRoot) && !assetPath.StartsWith(FallbackRoot)) return;
        ApplyToImporter((TextureImporter)assetImporter);
    }

    static bool Apply(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return false;

        ApplyToImporter(importer);
        importer.SaveAndReimport();
        return true;
    }

    static void ApplyToImporter(TextureImporter importer)
    {
        string lower = importer.assetPath.ToLowerInvariant();
        bool isNormal = lower.Contains("_normal") || lower.Contains("_nrm");
        bool isMask = lower.Contains("_mask") || lower.Contains("_rough") || lower.Contains("_metal");
        bool isHud = lower.Contains("/hud/") || lower.Contains("icon") || lower.Contains("sprite");

        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.filterMode = FilterMode.Bilinear;
        importer.anisoLevel = 2;

        if (isHud)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            return;
        }

        importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !isNormal && !isMask;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
    }
}
