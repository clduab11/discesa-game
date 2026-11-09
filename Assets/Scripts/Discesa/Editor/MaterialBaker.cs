using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Bakes the runtime materials into Assets/Resources/Discesa.
///
/// Why this exists: a build strips shaders that no asset references, so Shader.Find
/// ("Universal Render Pipeline/Lit") returns null at runtime and material creation throws.
/// Material assets kept under Resources are always included, so the built player can
/// colour its primitives without relying on shader lookup.
///
/// Run from the Editor: Discesa > Bake Runtime Materials
/// </summary>
public static class MaterialBaker
{
    const string ResourceFolder = "Assets/Resources/Discesa";

    [MenuItem("Discesa/Bake Runtime Materials")]
    public static void Bake()
    {
        Directory.CreateDirectory(ResourceFolder);
        AssetDatabase.Refresh();

        // Original industrial maps first; materials reference them below.
        ProceduralTextureBaker.BakeAll();

        // Slice 1 palette (kept so old scenes and tests still resolve).
        Create("ArenaFloor", new Color(0.55f, 0.60f, 0.70f), 0.15f, 0.15f, null);
        Create("ArenaTrim", new Color(0.25f, 0.28f, 0.35f), 0.10f, 0.20f, null);
        Create("ShipHull", new Color(0.95f, 0.75f, 0.25f), 0.60f, 0.50f, null);
        Create("Turret", new Color(0.85f, 0.25f, 0.25f), 0.40f, 0.40f, null);
        Create("ObjectiveCore", new Color(0.20f, 0.90f, 1.00f), 0.30f, 0.00f, new Color(0.2f, 0.9f, 1f) * 2.5f);
        Create("ProjectileGreen", new Color(0.50f, 1.00f, 0.40f), 0.30f, 0.00f, new Color(0.5f, 1f, 0.4f) * 2.2f);
        Create("ProjectileRed", new Color(1.00f, 0.35f, 0.20f), 0.30f, 0.00f, new Color(1f, 0.35f, 0.2f) * 2.2f);

        // M2 arena palette.
        Create("WallPanel", new Color(0.26f, 0.30f, 0.36f), 0.18f, 0.35f, null);
        Create("WallTrim", new Color(0.38f, 0.43f, 0.50f), 0.30f, 0.55f, null);
        Create("FloorGrate", new Color(0.22f, 0.24f, 0.28f), 0.22f, 0.40f, null);
        Create("CeilingPanel", new Color(0.32f, 0.35f, 0.40f), 0.12f, 0.25f, null);
        Create("HazardStripe", new Color(0.82f, 0.62f, 0.10f), 0.30f, 0.10f, new Color(0.55f, 0.38f, 0.03f) * 1.4f);
        Create("CoreTrim", new Color(0.34f, 0.39f, 0.46f), 0.35f, 0.60f, null);
        Create("PropMetal", new Color(0.50f, 0.53f, 0.58f), 0.35f, 0.45f, null);
        Create("EmissivePanel", new Color(0.72f, 0.84f, 1f), 0.10f, 0.0f, new Color(0.72f, 0.84f, 1f) * 3.2f);
        Create("EmergencyPanel", new Color(0.80f, 0.20f, 0.16f), 0.10f, 0.0f, new Color(1f, 0.24f, 0.18f) * 3.0f);

        // M3 combat palette.
        Create("ProjectileAmber", new Color(1.00f, 0.62f, 0.20f), 0.30f, 0.00f, new Color(1f, 0.62f, 0.2f) * 2.2f);
        Create("EnemyBody", new Color(0.62f, 0.22f, 0.20f), 0.35f, 0.45f, null);
        Create("WalkerBody", new Color(0.48f, 0.44f, 0.34f), 0.25f, 0.35f, null);
        Create("CoreReactor", new Color(0.15f, 0.62f, 0.92f), 0.20f, 0.0f, new Color(0.15f, 0.62f, 0.92f) * 1.1f);
        Create("PickupCell", new Color(0.20f, 0.95f, 0.55f), 0.20f, 0.0f, new Color(0.20f, 0.95f, 0.55f) * 1.5f);
        Create("PickupMissile", new Color(1f, 0.66f, 0.22f), 0.20f, 0.0f, new Color(1f, 0.66f, 0.22f) * 1.6f);
        Create("MuzzleFlash", new Color(1f, 0.92f, 0.55f), 0.10f, 0.0f, new Color(1f, 0.92f, 0.55f) * 4f);
        Create("EngineGlow", new Color(0.30f, 0.85f, 1f), 0.10f, 0.0f, new Color(0.30f, 0.85f, 1f) * 1.8f);

        CreateTrail("ProjectileTrail", new Color(0.72f, 1f, 0.85f));

        ApplyTextures();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Discesa] Baked runtime materials into " + ResourceFolder);
    }

    static void ApplyTextures()
    {
        Apply("WallPanel", "wall_panel", new Vector2(3f, 1.5f));
        Apply("WallTrim", "metal_trim", new Vector2(2f, 1f));
        Apply("FloorGrate", "floor_grate", new Vector2(6f, 6f));
        Apply("CeilingPanel", "ceiling_panel", new Vector2(4f, 4f));
        Apply("HazardStripe", "hazard_stripe", new Vector2(2f, 2f));
        Apply("CoreTrim", "core_trim", new Vector2(2f, 2f));
        Apply("PropMetal", "prop_metal", new Vector2(1f, 1f));
    }

    static void Apply(string materialName, string textureName, Vector2 tiling)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"{ResourceFolder}/{materialName}.mat");
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ProceduralTextureBaker.Folder}/{textureName}.png");
        if (mat == null || tex == null) return;

        mat.SetTexture("_BaseMap", tex);
        mat.SetTextureScale("_BaseMap", tiling);
        mat.SetTextureOffset("_BaseMap", Vector2.zero);
        mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
    }

    static void Create(string name, Color color, float smoothness, float metallic, Color? emission)
    {
        var path = $"{ResourceFolder}/{name}.mat";
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[Discesa] URP Lit shader not found; cannot bake materials.");
            return;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = mat == null;
        if (isNew) mat = new Material(shader) { name = name };

        mat.shader = shader;
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);

        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", emission.Value);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }

        if (isNew) AssetDatabase.CreateAsset(mat, path);
        else EditorUtility.SetDirty(mat);
    }

    /// <summary>Additive URP Unlit material for pooled projectile trails.</summary>
    static void CreateTrail(string name, Color color)
    {
        var path = $"{ResourceFolder}/{name}.mat";
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogError("[Discesa] URP Unlit shader not found; cannot bake trail material.");
            return;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = mat == null;
        if (isNew) mat = new Material(shader) { name = name };

        mat.shader = shader;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;

        mat.SetFloat("_Surface", 1f); // transparent
        mat.SetFloat("_Blend", 2f);   // additive
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.One);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;

        if (isNew) AssetDatabase.CreateAsset(mat, path);
        else EditorUtility.SetDirty(mat);
    }
}
