using UnityEngine;

/// <summary>
/// Runtime palette. Materials are real assets under Resources/Discesa (baked by
/// Editor/MaterialBaker) so shaders survive the build's shader stripping.
/// Falls back to a plain unlit material if an asset is missing, so the game still runs.
/// </summary>
public static class DiscesaMaterials
{
    const string Root = "Discesa/";

    public static Material ArenaFloor => Load("ArenaFloor", new Color(0.55f, 0.60f, 0.70f));
    public static Material ArenaTrim => Load("ArenaTrim", new Color(0.25f, 0.28f, 0.35f));
    public static Material ShipHull => Load("ShipHull", new Color(0.95f, 0.75f, 0.25f));
    public static Material Turret => Load("Turret", new Color(0.85f, 0.25f, 0.25f));
    public static Material ObjectiveCore => Load("ObjectiveCore", new Color(0.20f, 0.90f, 1.00f));
    public static Material ProjectileGreen => Load("ProjectileGreen", new Color(0.50f, 1.00f, 0.40f));
    public static Material ProjectileRed => Load("ProjectileRed", new Color(1.00f, 0.35f, 0.20f));

    static Material Load(string name, Color fallbackColor)
    {
        var mat = Resources.Load<Material>(Root + name);
        if (mat != null) return new Material(mat);   // instance so per-object tints are safe

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var fallback = new Material(shader) { color = fallbackColor };
        Debug.LogWarning($"[Discesa] Material '{name}' missing from Resources; using fallback. " +
                         "Run Discesa > Bake Runtime Materials.");
        return fallback;
    }
}
