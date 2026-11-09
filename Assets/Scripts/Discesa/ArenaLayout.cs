using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Axis-aligned volume for a room or corridor. Data only; the builder and the
/// containment safety net both read the same definitions so they cannot drift.
/// </summary>
public readonly struct ArenaVolume
{
    public readonly string Name;
    public readonly Vector3 Min;
    public readonly Vector3 Max;

    public ArenaVolume(string name, Vector3 min, Vector3 max)
    {
        Name = name;
        Min = new Vector3(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Min(min.z, max.z));
        Max = new Vector3(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y), Mathf.Max(min.z, max.z));
    }

    public float FloorY => Min.y;
    public float CeilingY => Max.y;
    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;

    public bool Contains(Vector3 p) =>
        p.x >= Min.x && p.x <= Max.x &&
        p.y >= Min.y && p.y <= Max.y &&
        p.z >= Min.z && p.z <= Max.z;

    /// <summary>Horizontal containment, used by ground actors that ignore height.</summary>
    public bool ContainsXZ(Vector3 p) =>
        p.x >= Min.x && p.x <= Max.x && p.z >= Min.z && p.z <= Max.z;
}

/// <summary>A rectangular opening cut into a wall.</summary>
public readonly struct Doorway
{
    public readonly string Name;
    public readonly string From;
    public readonly string To;
    public readonly Vector3 Min;
    public readonly Vector3 Max;

    public Doorway(string name, string from, string to, Vector3 a, Vector3 b)
    {
        Name = name;
        From = from;
        To = to;
        Min = new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));
        Max = new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
    }

    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 Size => Max - Min;
}

/// <summary>
/// The slice layout: three rooms plus two corridors and one alcove bay.
/// All floors sit at y = -6 so the ground walker can patrol every room; the ship
/// spawns at y = 0 in the Entry Hall and must descend to pass the corridor lintels.
///
/// Original layout. Not a tile-for-tile port of any reference level.
/// </summary>
public sealed class ArenaLayout
{
    public readonly List<ArenaVolume> Rooms = new List<ArenaVolume>();
    public readonly List<ArenaVolume> Corridors = new List<ArenaVolume>();
    public readonly List<ArenaVolume> Bays = new List<ArenaVolume>();
    public readonly List<Doorway> Doorways = new List<Doorway>();

    public Vector3 ShipSpawn = new Vector3(-32f, 0f, 0f);
    public Quaternion ShipRotation = Quaternion.identity;

    public Vector3 TurretAnchor = new Vector3(26.5f, 0.4f, 5.8f);
    public Vector3 CoreAnchor = new Vector3(31f, 2.0f, 8f);
    public Vector3 WalkerAnchor = new Vector3(2f, -5.3f, 8f);

    public Vector3 CellPickupAnchor = new Vector3(2f, -2.4f, 20f);
    public Vector3 MissilePickupAnchor = new Vector3(6.5f, -2.4f, 13f);

    public Vector3[] WalkerWaypoints = new Vector3[]
    {
        new Vector3(-3f, -5.3f, 4f),
        new Vector3(6f, -5.3f, 4f),
        new Vector3(6f, -5.3f, 15f),
        new Vector3(-3f, -5.3f, 15f),
        new Vector3(14f, -5.3f, 8f)
    };

    public static ArenaLayout CreateDefault()
    {
        var l = new ArenaLayout();

        l.Rooms.Add(new ArenaVolume("EntryHall", new Vector3(-35f, -6f, -8f), new Vector3(-17f, 6f, 8f)));
        l.Rooms.Add(new ArenaVolume("RelayGallery", new Vector3(-6f, -6f, -2f), new Vector3(10f, 6f, 18f)));
        l.Rooms.Add(new ArenaVolume("CoreChamber", new Vector3(18f, -6f, -3f), new Vector3(40f, 8f, 19f)));

        l.Corridors.Add(new ArenaVolume("CorridorA", new Vector3(-17f, -6f, -3f), new Vector3(-6f, 0f, 3f)));
        l.Corridors.Add(new ArenaVolume("CorridorB", new Vector3(10f, -6f, 5f), new Vector3(18f, 0f, 11f)));

        l.Bays.Add(new ArenaVolume("ServiceAlcove", new Vector3(-1f, -6f, 18f), new Vector3(5f, 0f, 22f)));

        // Doorways are the shared planes between volumes.
        l.Doorways.Add(new Doorway("EntryToCorridorA", "EntryHall", "CorridorA",
            new Vector3(-17f, -6f, -3f), new Vector3(-17f, 0f, 3f)));
        l.Doorways.Add(new Doorway("CorridorAToGallery", "CorridorA", "RelayGallery",
            new Vector3(-6f, -6f, -2f), new Vector3(-6f, 0f, 3f)));
        l.Doorways.Add(new Doorway("GalleryToAlcove", "RelayGallery", "ServiceAlcove",
            new Vector3(-1f, -6f, 18f), new Vector3(5f, 0f, 18f)));
        l.Doorways.Add(new Doorway("GalleryToCorridorB", "RelayGallery", "CorridorB",
            new Vector3(10f, -6f, 5f), new Vector3(10f, 0f, 11f)));
        l.Doorways.Add(new Doorway("CorridorBToCore", "CorridorB", "CoreChamber",
            new Vector3(18f, -6f, 5f), new Vector3(18f, 0f, 11f)));

        return l;
    }

    public IEnumerable<ArenaVolume> AllVolumes
    {
        get
        {
            foreach (var v in Rooms) yield return v;
            foreach (var v in Corridors) yield return v;
            foreach (var v in Bays) yield return v;
        }
    }

    public ArenaVolume FindVolume(string name)
    {
        foreach (var v in AllVolumes)
            if (v.Name == name) return v;
        return default;
    }

    public bool HasVolume(string name) => FindVolume(name).Name != null;

    public bool Contains(Vector3 p)
    {
        foreach (var v in AllVolumes)
            if (v.Contains(p)) return true;
        return false;
    }

    /// <summary>True when two volumes share a doorway (topology test helper).</summary>
    public bool DoorwayConnects(string a, string b)
    {
        foreach (var d in Doorways)
            if ((d.From == a && d.To == b) || (d.From == b && d.To == a)) return true;
        return false;
    }
}
