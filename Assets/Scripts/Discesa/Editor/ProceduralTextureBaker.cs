using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Deterministic, original texture baking for the slice. Generates panel/grate/hazard
/// maps in code so the arena reads as an industrial mine instead of flat primitives,
/// without spending Unity AI points and without importing third-party imagery.
///
/// Output: Assets/Art/GeneratedFallback/Textures/*.png (tracked).
/// Run: Discesa > Bake Procedural Textures (also invoked by MaterialBaker.Bake).
/// </summary>
public static class ProceduralTextureBaker
{
    public const string Folder = "Assets/Art/GeneratedFallback/Textures";

    static string AbsoluteFolder => Path.Combine(Application.dataPath, "Art/GeneratedFallback/Textures");

    [MenuItem("Discesa/Bake Procedural Textures")]
    public static void BakeAll()
    {
        Directory.CreateDirectory(AbsoluteFolder);

        Write("wall_panel", 512, 512, WallPanel);
        Write("floor_grate", 512, 512, FloorGrate);
        Write("ceiling_panel", 256, 256, CeilingPanel);
        Write("hazard_stripe", 256, 256, HazardStripe);
        Write("metal_trim", 256, 256, MetalTrim);
        Write("core_trim", 256, 256, CoreTrim);
        Write("prop_metal", 256, 256, PropMetal);

        AssetDatabase.Refresh();
        ArtImportSettings.ApplyAll();
        Debug.Log("[Discesa] Procedural textures baked into " + Folder);
    }

    static void Write(string name, int w, int h, System.Func<int, int, Color> pixel)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = pixel(x, y);

        // Ping-pong the edges so Repeat tiling has no visible seam.
        for (int y = 0; y < h; y++)
        {
            Color edge = px[y * w];
            px[y * w + w - 1] = Color.Lerp(px[y * w + w - 1], edge, 0.5f);
        }
        for (int x = 0; x < w; x++)
        {
            Color edge = px[x];
            px[(h - 1) * w + x] = Color.Lerp(px[(h - 1) * w + x], edge, 0.5f);
        }

        tex.SetPixels(px);
        tex.Apply(false, false);

        string path = Path.Combine(AbsoluteFolder, name + ".png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    // ---------------------------------------------------------------- patterns

    static Color WallPanel(int x, int y)
    {
        int cx = x / 128;
        int cy = y / 256;
        int lx = x % 128;
        int ly = y % 256;

        bool seam = lx < 4 || ly < 4 || lx > 123 || ly > 251;
        float n = Hash(cx * 17 + lx / 8, cy * 31 + ly / 8, 11) * 0.10f - 0.05f;

        float v = 0.42f + n;
        v += (1f - ly / 256f) * 0.05f;                 // top of each panel catches light
        if (ly < 12 || lx < 10) v += 0.10f;            // bevel
        if (seam) v = 0.16f + n * 0.5f;

        // Rivets near panel corners.
        float rx = Mathf.Min(lx, 128 - lx);
        float ry = Mathf.Min(ly, 256 - ly);
        if (rx < 12 && ry < 12 && rx > 4 && ry > 4) v += 0.16f;

        // Sparse scratches and stains.
        if (Hash(x / 2, y / 2, 71) > 0.986f) v += 0.18f;
        if (Hash(x / 16, y / 16, 91) > 0.93f) v -= 0.06f;

        v = Mathf.Clamp01(v);
        return new Color(v * 0.90f, v * 0.95f, v * 1.06f);
    }

    static Color FloorGrate(int x, int y)
    {
        int bx = x % 32;
        int by = y % 32;
        bool bar = bx < 11 || by < 11;
        float n = Hash(x, y, 5) * 0.04f;

        float v = bar ? 0.34f + n : 0.10f + n;
        if (bar && (bx < 2 || by < 2)) v += 0.12f;
        if (!bar && (bx > 27 || by > 27)) v += 0.03f;

        return new Color(v * 0.86f, v * 0.90f, v * 1.0f);
    }

    static Color CeilingPanel(int x, int y)
    {
        int lx = x % 128;
        int ly = y % 128;
        bool seam = lx < 4 || ly < 4;
        float n = Hash(x / 8, y / 8, 3) * 0.08f - 0.04f;

        float v = seam ? 0.18f : 0.40f + n;
        if (!seam && (ly < 10 || lx < 10)) v += 0.08f;
        return new Color(v * 0.93f, v * 0.96f, v * 1.02f);
    }

    static Color HazardStripe(int x, int y)
    {
        int band = (x + y) % 72;
        bool yellow = band < 36;
        float wear = Hash(x / 3, y / 3, 19);

        Color c = yellow ? new Color(0.95f, 0.70f, 0.10f) : new Color(0.10f, 0.10f, 0.11f);
        float k = 0.85f + wear * 0.25f;
        if (wear > 0.95f) k *= 0.6f; // chipped paint
        return new Color(c.r * k, c.g * k, c.b * k);
    }

    static Color MetalTrim(int x, int y)
    {
        float brush = Hash(x, y / 6, 23) * 0.10f - 0.05f;
        float broad = Hash(x / 32, y / 32, 29) * 0.06f;
        float v = 0.46f + brush + broad;
        return new Color(v * 0.96f, v * 0.99f, v * 1.04f);
    }

    static Color CoreTrim(int x, int y)
    {
        int lx = x % 64;
        int ly = y % 64;
        bool seam = lx < 3 || ly < 3;
        float n = Hash(x / 4, y / 4, 37) * 0.08f;
        float v = seam ? 0.16f : 0.30f + n;
        if (!seam && (ly < 8 || lx < 8)) v += 0.08f;
        return new Color(v * 0.92f, v * 0.97f, v * 1.08f);
    }

    static Color PropMetal(int x, int y)
    {
        float n = Hash(x / 2, y / 2, 41) * 0.10f - 0.05f;
        float v = 0.44f + n;
        if (Hash(x / 24, y / 24, 43) > 0.90f) v -= 0.08f; // dents
        return new Color(v * 0.95f, v * 1.0f, v * 1.05f);
    }

    // ---------------------------------------------------------------- noise

    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 69069);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777215f;
        }
    }
}
