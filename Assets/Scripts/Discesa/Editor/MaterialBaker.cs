using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes the runtime graybox materials into Assets/Resources/Discesa.
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

        Create("ArenaFloor", new Color(0.55f, 0.60f, 0.70f), 0.15f, 0.15f, null);
        Create("ArenaTrim", new Color(0.25f, 0.28f, 0.35f), 0.10f, 0.20f, null);
        Create("ShipHull", new Color(0.95f, 0.75f, 0.25f), 0.60f, 0.50f, null);
        Create("Turret", new Color(0.85f, 0.25f, 0.25f), 0.40f, 0.40f, null);
        Create("ObjectiveCore", new Color(0.20f, 0.90f, 1.00f), 0.30f, 0.00f, new Color(0.2f, 0.9f, 1f) * 2.5f);
        Create("ProjectileGreen", new Color(0.50f, 1.00f, 0.40f), 0.30f, 0.00f, new Color(0.5f, 1f, 0.4f) * 2f);
        Create("ProjectileRed", new Color(1.00f, 0.35f, 0.20f), 0.30f, 0.00f, new Color(1f, 0.35f, 0.2f) * 2f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Discesa] Baked runtime materials into " + ResourceFolder);
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
}
