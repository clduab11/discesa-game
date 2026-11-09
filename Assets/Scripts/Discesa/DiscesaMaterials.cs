using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime palette. Materials are real assets under Resources/Discesa (baked by
/// Editor/MaterialBaker) so shaders survive the build's shader stripping.
/// Each name resolves once and is cached, so arena pieces and props share a single
/// material instance and batching stays intact.
/// </summary>
public static class DiscesaMaterials
{
    const string Root = "Discesa/";
    static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

    public static Material WallPanel => Get("WallPanel", new Color(0.26f, 0.30f, 0.36f), 0.18f, 0.35f);
    public static Material WallTrim => Get("WallTrim", new Color(0.38f, 0.43f, 0.50f), 0.30f, 0.55f);
    public static Material FloorGrate => Get("FloorGrate", new Color(0.22f, 0.24f, 0.28f), 0.22f, 0.40f);
    public static Material CeilingPanel => Get("CeilingPanel", new Color(0.32f, 0.35f, 0.40f), 0.12f, 0.25f);
    public static Material HazardStripe => Get("HazardStripe", new Color(0.82f, 0.62f, 0.10f), 0.30f, 0.10f,
        new Color(0.55f, 0.38f, 0.03f) * 1.4f);
    public static Material CoreTrim => Get("CoreTrim", new Color(0.34f, 0.39f, 0.46f), 0.35f, 0.60f);
    public static Material PropMetal => Get("PropMetal", new Color(0.50f, 0.53f, 0.58f), 0.35f, 0.45f);

    public static Material EmissivePanel => Get("EmissivePanel", new Color(0.72f, 0.84f, 1f), 0.10f, 0.0f,
        new Color(0.72f, 0.84f, 1f) * 3.2f);
    public static Material EmergencyPanel => Get("EmergencyPanel", new Color(0.80f, 0.20f, 0.16f), 0.10f, 0.0f,
        new Color(1f, 0.24f, 0.18f) * 3.0f);

    public static Material ArenaFloor => Get("ArenaFloor", new Color(0.55f, 0.60f, 0.70f), 0.15f, 0.15f);
    public static Material ArenaTrim => Get("ArenaTrim", new Color(0.25f, 0.28f, 0.35f), 0.10f, 0.20f);
    public static Material ShipHull => Get("ShipHull", new Color(0.95f, 0.75f, 0.25f), 0.60f, 0.50f);
    public static Material Turret => Get("Turret", new Color(0.85f, 0.25f, 0.25f), 0.40f, 0.40f);
    public static Material ObjectiveCore => Get("ObjectiveCore", new Color(0.20f, 0.90f, 1.00f), 0.30f, 0.00f,
        new Color(0.2f, 0.9f, 1f) * 2.5f);
    public static Material ProjectileGreen => Get("ProjectileGreen", new Color(0.50f, 1.00f, 0.40f), 0.30f, 0.00f,
        new Color(0.5f, 1f, 0.4f) * 2.2f);
    public static Material ProjectileRed => Get("ProjectileRed", new Color(1.00f, 0.35f, 0.20f), 0.30f, 0.00f,
        new Color(1f, 0.35f, 0.2f) * 2.2f);
    public static Material ProjectileAmber => Get("ProjectileAmber", new Color(1.00f, 0.62f, 0.20f), 0.30f, 0.00f,
        new Color(1f, 0.62f, 0.2f) * 2.2f);

    public static Material EnemyBody => Get("EnemyBody", new Color(0.62f, 0.22f, 0.20f), 0.35f, 0.45f);
    public static Material WalkerBody => Get("WalkerBody", new Color(0.48f, 0.44f, 0.34f), 0.25f, 0.35f);
    public static Material CoreReactor => Get("CoreReactor", new Color(0.15f, 0.62f, 0.92f), 0.20f, 0.0f, new Color(0.15f, 0.62f, 0.92f) * 1.1f);
    public static Material PickupCell => Get("PickupCell", new Color(0.20f, 0.95f, 0.55f), 0.20f, 0.0f, new Color(0.20f, 0.95f, 0.55f) * 1.5f);
    public static Material PickupMissile => Get("PickupMissile", new Color(1f, 0.66f, 0.22f), 0.20f, 0.0f, new Color(1f, 0.66f, 0.22f) * 1.6f);
    public static Material MuzzleFlash => Get("MuzzleFlash", new Color(1f, 0.92f, 0.55f), 0.10f, 0.0f,
        new Color(1f, 0.92f, 0.55f) * 4f);
    public static Material EngineGlow => Get("EngineGlow", new Color(0.30f, 0.85f, 1f), 0.10f, 0.0f, new Color(0.30f, 0.85f, 1f) * 1.8f);
    public static Material Trail => Trails();

    static void VerifyShaders()
    {
#if UNITY_EDITOR
        if (Shader.Find("Universal Render Pipeline/Lit") == null)
            Debug.LogWarning("[Discesa] URP Lit shader unavailable during asset build; run Discesa > Bake Runtime Materials.");
#endif
    }

    static Material Get(string name, Color fallbackColor, float smoothness, float metallic, Color? emission = null)
    {
        if (Cache.TryGetValue(name, out var cached) && cached != null) return cached;

        var source = Resources.Load<Material>(Root + name);
        Material mat;
        if (source != null)
        {
            mat = new Material(source) { name = name };
        }
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = new Material(shader) { name = name };
            mat.color = fallbackColor;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            Debug.LogWarning($"[Discesa] Material '{name}' missing from Resources; using fallback. " +
                             "Run Discesa > Bake Runtime Materials.");
        }

        Cache[name] = mat;
        return mat;
    }

    static Material Trails()
    {
        const string name = "ProjectileTrail";
        if (Cache.TryGetValue(name, out var cached) && cached != null) return cached;

        var source = Resources.Load<Material>(Root + name);
        if (source != null)
        {
            Cache[name] = new Material(source) { name = name };
            return Cache[name];
        }

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        mat.color = Color.white;
        Cache[name] = mat;
        return mat;
    }

    /// <summary>Clears cached instances (used by tests and after a domain reload).</summary>
    public static void ClearCache() => Cache.Clear();
}
