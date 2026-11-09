using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the multi-room arena from <see cref="ArenaLayout"/>. Walls are assembled from
/// box pieces around explicit doorway openings (no CSG), which keeps colliders exact and
/// the runtime cost trivial. Every piece is a solid box: the ship's swept controller
/// treats them as authoritative, and ArenaContainment is only a safety net.
///
/// Original level geometry. No reference level data is used.
/// </summary>
public static class ArenaBuilder
{
    public const float WallThickness = 0.3f;
    public const float DoorHeight = 6f;

    struct Opening
    {
        public float A0, A1, B0, B1; // along-wall span, vertical span
        public Opening(float a0, float a1, float b0, float b1) { A0 = a0; A1 = a1; B0 = b0; B1 = b1; }
    }

    public static GameObject Build(ArenaLayout layout, bool includeProps = true)
    {
        if (layout == null) layout = ArenaLayout.CreateDefault();

        var root = new GameObject("Arena");
        var wall = DiscesaMaterials.WallPanel;
        var trim = DiscesaMaterials.WallTrim;
        var floor = DiscesaMaterials.FloorGrate;
        var ceil = DiscesaMaterials.CeilingPanel;

        foreach (var v in layout.Rooms) BuildShell(root.transform, v, floor, ceil);
        foreach (var v in layout.Corridors) BuildShell(root.transform, v, floor, ceil);
        foreach (var v in layout.Bays) BuildShell(root.transform, v, floor, ceil);

        // ---- Entry Hall
        var entry = layout.FindVolume("EntryHall");
        AddWallZ(root.transform, "EntryWest", entry.Min.x, entry.Min.z, entry.Max.z, entry.Min.y, entry.Max.y, wall);
        AddWallX(root.transform, "EntrySouth", entry.Min.z, entry.Min.x, entry.Max.x, entry.Min.y, entry.Max.y, wall);
        AddWallX(root.transform, "EntryNorth", entry.Max.z, entry.Min.x, entry.Max.x, entry.Min.y, entry.Max.y, wall);
        var entryDoor = new List<Opening> { new Opening(-3f, 3f, -6f, 0f) };
        AddWallZ(root.transform, "EntryEast", entry.Max.x, entry.Min.z, entry.Max.z, entry.Min.y, entry.Max.y, wall, entryDoor);

        // ---- Corridor A
        var corA = layout.FindVolume("CorridorA");
        AddWallX(root.transform, "CorridorASouth", corA.Min.z, corA.Min.x, corA.Max.x, corA.Min.y, corA.Max.y, trim);
        AddWallX(root.transform, "CorridorANorth", corA.Max.z, corA.Min.x, corA.Max.x, corA.Min.y, corA.Max.y, trim);
        // The corridor's east end meets the Gallery wall at z -2..3; the remaining
        // sliver (z -3..-2) is sealed here so there is no gap into the void.
        AddBox(root.transform, "CorridorASeal", new Vector3(corA.Max.x, (corA.Min.y + corA.Max.y) * 0.5f, -2.5f),
            new Vector3(WallThickness, corA.Max.y - corA.Min.y, 1f), trim);

        // ---- Relay Gallery
        var gallery = layout.FindVolume("RelayGallery");
        AddWallX(root.transform, "GallerySouth", gallery.Min.z, gallery.Min.x, gallery.Max.x, gallery.Min.y, gallery.Max.y, wall);
        var galleryNorthOpenings = new List<Opening> { new Opening(-1f, 5f, -6f, 0f) };
        AddWallX(root.transform, "GalleryNorth", gallery.Max.z, gallery.Min.x, gallery.Max.x, gallery.Min.y, gallery.Max.y, wall, galleryNorthOpenings);
        var galleryWestOpenings = new List<Opening> { new Opening(-2f, 3f, -6f, 0f) };
        AddWallZ(root.transform, "GalleryWest", gallery.Min.x, gallery.Min.z, gallery.Max.z, gallery.Min.y, gallery.Max.y, wall, galleryWestOpenings);
        var galleryEastOpenings = new List<Opening> { new Opening(5f, 11f, -6f, 0f) };
        AddWallZ(root.transform, "GalleryEast", gallery.Max.x, gallery.Min.z, gallery.Max.z, gallery.Min.y, gallery.Max.y, wall, galleryEastOpenings);

        // ---- Service Alcove (branch bay, not a fourth room)
        var alcove = layout.FindVolume("ServiceAlcove");
        AddWallX(root.transform, "AlcoveNorth", alcove.Max.z, alcove.Min.x, alcove.Max.x, alcove.Min.y, alcove.Max.y, trim);
        AddWallZ(root.transform, "AlcoveWest", alcove.Min.x, alcove.Min.z, alcove.Max.z, alcove.Min.y, alcove.Max.y, trim);
        AddWallZ(root.transform, "AlcoveEast", alcove.Max.x, alcove.Min.z, alcove.Max.z, alcove.Min.y, alcove.Max.y, trim);

        // ---- Corridor B
        var corB = layout.FindVolume("CorridorB");
        AddWallX(root.transform, "CorridorBSouth", corB.Min.z, corB.Min.x, corB.Max.x, corB.Min.y, corB.Max.y, trim);
        AddWallX(root.transform, "CorridorBNorth", corB.Max.z, corB.Min.x, corB.Max.x, corB.Min.y, corB.Max.y, trim);

        // ---- Core Chamber
        var core = layout.FindVolume("CoreChamber");
        AddWallX(root.transform, "CoreSouth", core.Min.z, core.Min.x, core.Max.x, core.Min.y, core.Max.y, wall);
        AddWallX(root.transform, "CoreNorth", core.Max.z, core.Min.x, core.Max.x, core.Min.y, core.Max.y, wall);
        AddWallZ(root.transform, "CoreEast", core.Max.x, core.Min.z, core.Max.z, core.Min.y, core.Max.y, wall);
        var coreWestOpenings = new List<Opening> { new Opening(5f, 11f, -6f, 0f) };
        AddWallZ(root.transform, "CoreWest", core.Min.x, core.Min.z, core.Max.z, core.Min.y, core.Max.y, wall, coreWestOpenings);

        BuildCorePlatform(root.transform, layout);
        BuildDoorwayTrim(root.transform, layout);
        BuildWallAccents(root.transform, layout);
        BuildLighting(root.transform, layout);
        if (includeProps) BuildProps(root.transform, layout);

        ConfigureEnvironment();
        return root;
    }

    // ---------------------------------------------------------------- volume shell

    static void BuildShell(Transform parent, ArenaVolume v, Material floorMat, Material ceilMat)
    {
        float t = WallThickness;
        var size = v.Size;

        AddBox(parent, v.Name + "Floor", new Vector3(v.Center.x, v.Min.y - t * 0.5f, v.Center.z),
            new Vector3(size.x, t, size.z), floorMat);
        AddBox(parent, v.Name + "Ceiling", new Vector3(v.Center.x, v.Max.y + t * 0.5f, v.Center.z),
            new Vector3(size.x, t, size.z), ceilMat);
    }

    // ---------------------------------------------------------------- walls

    /// <summary>Wall on the plane x = constant, running along Z.</summary>
    static void AddWallZ(Transform parent, string name, float x, float z0, float z1, float y0, float y1,
                         Material mat, List<Opening> openings = null)
    {
        float t = WallThickness;
        var pending = new List<Opening>();
        if (openings != null) foreach (var o in openings) if (o.A1 > z0 && o.A0 < z1) pending.Add(o);
        pending.Sort((a, b) => a.A0.CompareTo(b.A0));

        float cursor = z0;
        foreach (var o in pending)
        {
            float a0 = Mathf.Max(o.A0, z0);
            float a1 = Mathf.Min(o.A1, z1);
            if (a1 <= a0) continue;

            if (a0 > cursor)
                AddBox(parent, name + $"_{cursor:0.0}", new Vector3(x, (y0 + y1) * 0.5f, (cursor + a0) * 0.5f),
                       new Vector3(t, y1 - y0, a0 - cursor), mat);

            if (o.B0 > y0)
                AddBox(parent, name + "_Sill", new Vector3(x, (y0 + o.B0) * 0.5f, (a0 + a1) * 0.5f),
                       new Vector3(t, o.B0 - y0, a1 - a0), mat);
            if (o.B1 < y1)
                AddBox(parent, name + "_Lintel", new Vector3(x, (o.B1 + y1) * 0.5f, (a0 + a1) * 0.5f),
                       new Vector3(t, y1 - o.B1, a1 - a0), mat);

            cursor = Mathf.Max(cursor, a1);
        }

        if (cursor < z1)
            AddBox(parent, name + $"_end", new Vector3(x, (y0 + y1) * 0.5f, (cursor + z1) * 0.5f),
                   new Vector3(t, y1 - y0, z1 - cursor), mat);
    }

    /// <summary>Wall on the plane z = constant, running along X.</summary>
    static void AddWallX(Transform parent, string name, float z, float x0, float x1, float y0, float y1,
                         Material mat, List<Opening> openings = null)
    {
        float t = WallThickness;
        var pending = new List<Opening>();
        if (openings != null) foreach (var o in openings) if (o.A1 > x0 && o.A0 < x1) pending.Add(o);
        pending.Sort((a, b) => a.A0.CompareTo(b.A0));

        float cursor = x0;
        foreach (var o in pending)
        {
            float a0 = Mathf.Max(o.A0, x0);
            float a1 = Mathf.Min(o.A1, x1);
            if (a1 <= a0) continue;

            if (a0 > cursor)
                AddBox(parent, name + $"_{cursor:0.0}", new Vector3((cursor + a0) * 0.5f, (y0 + y1) * 0.5f, z),
                       new Vector3(a0 - cursor, y1 - y0, t), mat);

            if (o.B0 > y0)
                AddBox(parent, name + "_Sill", new Vector3((a0 + a1) * 0.5f, (y0 + o.B0) * 0.5f, z),
                       new Vector3(a1 - a0, o.B0 - y0, t), mat);
            if (o.B1 < y1)
                AddBox(parent, name + "_Lintel", new Vector3((a0 + a1) * 0.5f, (o.B1 + y1) * 0.5f, z),
                       new Vector3(a1 - a0, y1 - o.B1, t), mat);

            cursor = Mathf.Max(cursor, a1);
        }

        if (cursor < x1)
            AddBox(parent, name + $"_end", new Vector3((cursor + x1) * 0.5f, (y0 + y1) * 0.5f, z),
                   new Vector3(x1 - cursor, y1 - y0, t), mat);
    }

    // ---------------------------------------------------------------- platform, lights, props

    static void BuildCorePlatform(Transform parent, ArenaLayout layout)
    {
        var trim = DiscesaMaterials.CoreTrim;
        var hazard = DiscesaMaterials.HazardStripe;

        AddBox(parent, "CorePlatform", new Vector3(29f, -4f, 8f), new Vector3(8f, 4f, 8f), trim);

        // Hazard band around the platform rim.
        AddBox(parent, "HazardN", new Vector3(29f, -1.96f, 11.8f), new Vector3(8f, 0.08f, 0.4f), hazard);
        AddBox(parent, "HazardS", new Vector3(29f, -1.96f, 4.2f), new Vector3(8f, 0.08f, 0.4f), hazard);
        AddBox(parent, "HazardE", new Vector3(32.8f, -1.96f, 8f), new Vector3(0.4f, 0.08f, 8f), hazard);
        AddBox(parent, "HazardW", new Vector3(25.2f, -1.96f, 8f), new Vector3(0.4f, 0.08f, 8f), hazard);

        // A low collar marks the turret mount without blocking the core shot.
        AddBox(parent, "TurretMount", new Vector3(29f, -1.8f, 8f), new Vector3(2.2f, 0.4f, 2.2f), trim);
    }

    static void BuildDoorwayTrim(Transform parent, ArenaLayout layout)
    {
        var hazard = DiscesaMaterials.HazardStripe;
        float t = WallThickness + 0.08f;

        foreach (var d in layout.Doorways)
        {
            bool onX = d.Size.x < 0.6f;
            if (onX)
            {
                float x = d.Center.x;
                AddBox(parent, d.Name + "_JambA", new Vector3(x, -3f, d.Min.z + 0.12f),
                    new Vector3(t, 5.7f, 0.24f), hazard);
                AddBox(parent, d.Name + "_JambB", new Vector3(x, -3f, d.Max.z - 0.12f),
                    new Vector3(t, 5.7f, 0.24f), hazard);
                AddBox(parent, d.Name + "_Head", new Vector3(x, -0.12f, d.Center.z),
                    new Vector3(t, 0.24f, d.Size.z - 0.4f), hazard);
            }
            else
            {
                float z = d.Center.z;
                AddBox(parent, d.Name + "_JambA", new Vector3(d.Min.x + 0.12f, -3f, z),
                    new Vector3(0.24f, 5.7f, t), hazard);
                AddBox(parent, d.Name + "_JambB", new Vector3(d.Max.x - 0.12f, -3f, z),
                    new Vector3(0.24f, 5.7f, t), hazard);
                AddBox(parent, d.Name + "_Head", new Vector3(d.Center.x, -0.12f, z),
                    new Vector3(d.Size.x - 0.4f, 0.24f, t), hazard);
            }
        }
    }

    static void BuildWallAccents(Transform parent, ArenaLayout layout)
    {
        var cool = DiscesaMaterials.EmissivePanel;
        var warm = DiscesaMaterials.HazardStripe;

        // Corridor guide strips at floor level, echoing the reference's lit channel edges.
        AddAccent(parent, "CorridorAStripS", new Vector3(-11.5f, -5.72f, -2.82f), new Vector3(9.6f, 0.07f, 0.07f), cool);
        AddAccent(parent, "CorridorAStripN", new Vector3(-11.5f, -5.72f, 2.82f), new Vector3(9.6f, 0.07f, 0.07f), cool);
        AddAccent(parent, "CorridorBStripS", new Vector3(14f, -5.72f, 5.18f), new Vector3(7.6f, 0.07f, 0.07f), warm);
        AddAccent(parent, "CorridorBStripN", new Vector3(14f, -5.72f, 10.82f), new Vector3(7.6f, 0.07f, 0.07f), warm);

        // Entry Hall floor lane and gallery wall bands.
        AddAccent(parent, "EntryLaneStrip", new Vector3(-26f, -5.72f, 0f), new Vector3(17f, 0.07f, 0.07f), cool);
        AddAccent(parent, "GalleryBandS", new Vector3(2f, 2.6f, -1.82f), new Vector3(15.4f, 0.10f, 0.10f), warm);
        AddAccent(parent, "GalleryBandW", new Vector3(-5.82f, 2.6f, 8f), new Vector3(0.10f, 0.10f, 19.4f), warm);

        // Core chamber hazard ring marks the objective approach.
        AddAccent(parent, "CoreApproachStrip", new Vector3(21.5f, -5.72f, 8f), new Vector3(0.10f, 0.10f, 10.5f), warm);
    }

    static void BuildLighting(Transform parent, ArenaLayout layout)
    {
        var panel = DiscesaMaterials.EmissivePanel;
        var red = DiscesaMaterials.EmergencyPanel;
        int shadowed = 0;

        Fixture(parent, "LightEntryA", new Vector3(-30f, 5.75f, 0f), panel, new Color(0.70f, 0.82f, 1f), 26f, 16f, ref shadowed, false);
        Fixture(parent, "LightEntryB", new Vector3(-22f, 5.75f, -4f), panel, new Color(0.70f, 0.82f, 1f), 24f, 14f, ref shadowed, false);
        Fixture(parent, "LightEntryC", new Vector3(-22f, 5.75f, 4f), panel, new Color(0.70f, 0.82f, 1f), 24f, 14f, ref shadowed, false);

        Fixture(parent, "LightCorridorA1", new Vector3(-14f, -0.25f, 0f), panel, new Color(1f, 0.86f, 0.62f), 16f, 11f, ref shadowed, false);
        Fixture(parent, "LightCorridorA2", new Vector3(-9f, -0.25f, 0f), panel, new Color(1f, 0.86f, 0.62f), 16f, 11f, ref shadowed, false);

        Fixture(parent, "LightGalleryA", new Vector3(-2f, 5.75f, 4f), panel, new Color(0.72f, 0.86f, 1f), 28f, 18f, ref shadowed, true);
        Fixture(parent, "LightGalleryB", new Vector3(6f, 5.75f, 12f), panel, new Color(0.72f, 0.86f, 1f), 28f, 16f, ref shadowed, false);
        Fixture(parent, "LightAlcove", new Vector3(2f, -0.25f, 20f), panel, new Color(0.62f, 0.88f, 0.82f), 10f, 14f, ref shadowed, false);

        Fixture(parent, "LightCorridorB", new Vector3(14f, -0.25f, 8f), panel, new Color(1f, 0.78f, 0.55f), 18f, 13f, ref shadowed, false);

        Fixture(parent, "LightCoreA", new Vector3(23f, 7.75f, 8f), red, new Color(1f, 0.30f, 0.22f), 30f, 22f, ref shadowed, false);
        Fixture(parent, "LightCoreB", new Vector3(35f, 7.75f, 8f), red, new Color(1f, 0.30f, 0.22f), 30f, 18f, ref shadowed, false);

        // Cool fill so vertical walls stay readable away from the point lights.
        var fillGo = new GameObject("FillLight");
        fillGo.transform.SetParent(parent, false);
        fillGo.transform.rotation = Quaternion.Euler(58f, -34f, 0f);
        var fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.55f, 0.65f, 0.85f);
        fill.intensity = 0.55f;
        fill.shadows = LightShadows.None;
    }

    static void Fixture(Transform parent, string name, Vector3 position, Material panelMaterial, Color lightColor,
                        float range, float intensity, ref int shadowed, bool castShadows)
    {
        var go = AddBox(parent, name, position, new Vector3(2.2f, 0.12f, 0.7f), panelMaterial);
        Kill(go.GetComponent<Collider>());

        var lightGo = new GameObject(name + "_Light");
        lightGo.transform.SetParent(parent, false);
        lightGo.transform.position = position - Vector3.up * 0.6f;
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = lightColor;
        light.range = range;
        light.intensity = intensity;

        bool useShadow = castShadows && shadowed < 5;
        light.shadows = useShadow ? LightShadows.Soft : LightShadows.None;
        if (useShadow) shadowed++;
    }

    static void BuildProps(Transform parent, ArenaLayout layout)
    {
        var metal = DiscesaMaterials.PropMetal;

        // Crate visuals: the SimReady asset when present, authored boxes as cover.
        var crateA = SimReadyPropLoader.TryInstantiate();
        if (crateA != null)
        {
            crateA.name = "SimReadyCrateA";
            crateA.transform.SetParent(parent, true);
            crateA.transform.position = new Vector3(-2.5f, -5.5f, 6.5f);
            crateA.transform.rotation = Quaternion.Euler(0f, 24f, 0f);
        }

        var crateB = SimReadyPropLoader.TryInstantiate();
        if (crateB != null)
        {
            crateB.name = "SimReadyCrateB";
            crateB.transform.SetParent(parent, true);
            crateB.transform.position = new Vector3(4.5f, -5.5f, 15.5f);
            crateB.transform.rotation = Quaternion.Euler(0f, -38f, 0f);
        }

        AddCover(parent, "CoverA", new Vector3(-4.5f, -5.2f, 2.2f), new Vector3(1.6f, 1.6f, 1.6f), metal);
        AddCover(parent, "CoverB", new Vector3(7.2f, -5.2f, 3.4f), new Vector3(1.4f, 1.4f, 1.4f), metal);
        AddCover(parent, "CoverAlcove", new Vector3(3.4f, -5.2f, 20.4f), new Vector3(1.5f, 1.5f, 1.5f), metal);
        AddCover(parent, "CoverCore", new Vector3(24.5f, -5.2f, 2.5f), new Vector3(1.8f, 1.6f, 1.8f), metal);
    }

    static void AddAccent(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var go = AddBox(parent, name, position, size, material);
        Kill(go.GetComponent<Collider>());
    }

    static void AddCover(Transform parent, string name, Vector3 position, Vector3 size, Material material)
    {
        var go = AddBox(parent, name, position, size, material);
        // Solid collider: gameplay cover, and the swept ship slides off it.
        go.GetComponent<BoxCollider>().isTrigger = false;
    }

    static void ConfigureEnvironment()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.23f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.07f, 0.09f, 0.12f);
        RenderSettings.fogStartDistance = 26f;
        RenderSettings.fogEndDistance = 170f;
    }

    static void Kill(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Object.Destroy(obj);
        else Object.DestroyImmediate(obj);
    }

    // ---------------------------------------------------------------- primitives

    public static GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = size;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        return go;
    }
}
