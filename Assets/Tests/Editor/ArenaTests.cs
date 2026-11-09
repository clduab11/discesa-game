using NUnit.Framework;
using UnityEngine;

public class ArenaTests
{
    [Test]
    public void ArenaLayout_HasThreeRoomsAndTwoCorridors()
    {
        var layout = ArenaLayout.CreateDefault();
        Assert.That(layout.Rooms.Count, Is.EqualTo(3), "Expected three rooms");
        Assert.That(layout.Corridors.Count, Is.EqualTo(2), "Expected two corridors");
        Assert.That(layout.Bays.Count, Is.EqualTo(1), "Expected one branch alcove");

        Assert.That(layout.HasVolume("EntryHall"), Is.True);
        Assert.That(layout.HasVolume("RelayGallery"), Is.True);
        Assert.That(layout.HasVolume("CoreChamber"), Is.True);
        Assert.That(layout.HasVolume("CorridorA"), Is.True);
        Assert.That(layout.HasVolume("CorridorB"), Is.True);
        Assert.That(layout.HasVolume("ServiceAlcove"), Is.True);
    }

    [Test]
    public void ArenaLayout_DoorwaysConnectRooms()
    {
        var layout = ArenaLayout.CreateDefault();
        Assert.That(layout.Doorways.Count, Is.EqualTo(5));

        Assert.That(layout.DoorwayConnects("EntryHall", "CorridorA"), Is.True);
        Assert.That(layout.DoorwayConnects("CorridorA", "RelayGallery"), Is.True);
        Assert.That(layout.DoorwayConnects("RelayGallery", "CorridorB"), Is.True);
        Assert.That(layout.DoorwayConnects("CorridorB", "CoreChamber"), Is.True);
        Assert.That(layout.DoorwayConnects("RelayGallery", "ServiceAlcove"), Is.True);

        foreach (var d in layout.Doorways)
        {
            Assert.That(layout.HasVolume(d.From), Is.True, $"{d.Name}: missing {d.From}");
            Assert.That(layout.HasVolume(d.To), Is.True, $"{d.Name}: missing {d.To}");
            Assert.That(d.Center.x, Is.InRange(-35f, 40f));
            Assert.That(d.Center.y, Is.InRange(-6f, 0f));
        }
    }

    [Test]
    public void ArenaLayout_VolumesDoNotOverlapRoomInteriors()
    {
        var layout = ArenaLayout.CreateDefault();
        foreach (var room in layout.Rooms)
        {
            foreach (var corridor in layout.Corridors)
            {
                // Corridors may only touch a room at the shared doorway plane.
                float overlapX = Mathf.Min(room.Max.x, corridor.Max.x) - Mathf.Max(room.Min.x, corridor.Min.x);
                float overlapZ = Mathf.Min(room.Max.z, corridor.Max.z) - Mathf.Max(room.Min.z, corridor.Min.z);
                Assert.That(overlapX <= 0.001f || overlapZ <= 0.001f, Is.True,
                    $"{room.Name} and {corridor.Name} overlap by ({overlapX}, {overlapZ})");
            }
        }
    }

    [Test]
    public void ArenaContainment_ContainsSpawnAndCore()
    {
        var layout = ArenaLayout.CreateDefault();
        Assert.That(layout.Contains(layout.ShipSpawn), Is.True, "Ship spawn must be inside the union");
        Assert.That(layout.Contains(layout.CoreAnchor), Is.True, "Core anchor must be inside the Core Chamber");
        Assert.That(layout.Contains(layout.TurretAnchor), Is.True);
        Assert.That(layout.Contains(new Vector3(100f, 0f, 0f)), Is.False, "Outside the union must be rejected");
    }

    [Test]
    public void ArenaContainment_RespawnsEscapedShip()
    {
        var layout = ArenaLayout.CreateDefault();
        var go = new GameObject("ContainmentBody");
        go.transform.position = layout.ShipSpawn;
        var containment = new ArenaContainment(layout, go.transform, 0.55f);
        containment.Reset(layout.ShipSpawn);

        go.transform.position = new Vector3(200f, 0f, 0f);
        bool respawned = false;
        for (int i = 0; i < 20 && !respawned; i++)
            respawned = containment.Tick(go.transform.position, 0.1f);

        Assert.That(respawned, Is.True, "Escaped ship should be returned to the last-safe point");
        Assert.That(go.transform.position, Is.EqualTo(layout.ShipSpawn));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ArenaBuilder_BuildsNoGaps()
    {
        var layout = ArenaLayout.CreateDefault();
        var arena = ArenaBuilder.Build(layout, includeProps: false);
        try
        {
            Physics.SyncTransforms();

            var colliders = arena.GetComponentsInChildren<Collider>();
            Assert.That(colliders.Length, Is.GreaterThan(30), $"Expected a multi-room shell, got {colliders.Length} colliders");

            foreach (var d in layout.Doorways)
            {
                var c = d.Center;
                float offset = Mathf.Max(d.Size.x, d.Size.z) * 0.25f;
                bool alongX = d.Size.x > d.Size.z;

                CheckFree(c);
                CheckFree(alongX ? new Vector3(c.x + offset, c.y, c.z) : new Vector3(c.x, c.y, c.z + offset));
                CheckFree(alongX ? new Vector3(c.x - offset, c.y, c.z) : new Vector3(c.x, c.y, c.z - offset));
            }
        }
        finally
        {
            Object.DestroyImmediate(arena);
        }
    }

    static void CheckFree(Vector3 point)
    {
        Assert.That(Physics.CheckSphere(point, 0.3f, ~0, QueryTriggerInteraction.Collide), Is.False,
            $"Doorway aperture blocked at {point}");
    }
}
