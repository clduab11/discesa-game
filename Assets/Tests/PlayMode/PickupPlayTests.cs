using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PickupPlayTests
{
    readonly System.Collections.Generic.List<GameObject> _objects = new System.Collections.Generic.List<GameObject>();

    GameObject Track(GameObject go)
    {
        _objects.Add(go);
        return go;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in _objects)
            if (go != null) Object.Destroy(go);
        _objects.Clear();
    }

    Pickup MakePickup(Pickup.Kind kind, ShipStatus status, Transform ship, Vector3 at)
    {
        var go = Track(new GameObject(kind + "Pickup"));
        var visual = Track(new GameObject("Visual"));
        visual.transform.SetParent(go.transform, false);

        var lightGo = Track(new GameObject("Glow"));
        lightGo.transform.SetParent(go.transform, false);
        var light = lightGo.AddComponent<Light>();

        var pickup = go.AddComponent<Pickup>();
        pickup.respawnSeconds = 0.05f;
        pickup.Build(kind, status, ship, at, visual.transform, light);
        return pickup;
    }

    [UnityTest]
    public IEnumerator Cell_RestoresEnergy_Clamps_AndRespawns()
    {
        var shipGo = Track(new GameObject("Ship"));
        shipGo.transform.position = Vector3.zero;
        var status = shipGo.AddComponent<ShipStatus>();
        status.SetResources(100f, 50f, 6);

        var pickup = MakePickup(Pickup.Kind.Cell, status, shipGo.transform, Vector3.zero);

        yield return null;
        yield return null;

        Assert.That(status.Energy, Is.EqualTo(75f), "Cell must add 25 energy");
        Assert.That(pickup.IsAvailable, Is.False, "Pickup must be consumed on contact");
        Assert.That(pickup.Message, Does.Contain("ENERGY"));

        // Fly away before the timer expires, otherwise the pickup would be re-collected
        // the moment it respawns (which is correct behaviour, just not what we measure).
        shipGo.transform.position = new Vector3(12f, 0f, 0f);
        yield return new WaitForSeconds(0.2f);

        Assert.That(pickup.IsAvailable, Is.True, "Cell must respawn after its timer");

        // Collected at full energy: the cell clamps at 100 and still respawns.
        status.SetResources(100f, 100f, 6);
        shipGo.transform.position = Vector3.zero;
        yield return null;
        yield return null;
        Assert.That(status.Energy, Is.EqualTo(100f));
    }

    [UnityTest]
    public IEnumerator Missile_AddsFourRounds_AndClampsAtMaximum()
    {
        var shipGo = Track(new GameObject("Ship"));
        shipGo.transform.position = Vector3.zero;
        var status = shipGo.AddComponent<ShipStatus>();
        status.SetResources(100f, 100f, 10);

        var pickup = MakePickup(Pickup.Kind.Missile, status, shipGo.transform, Vector3.zero);

        yield return null;
        yield return null;

        Assert.That(status.Missiles, Is.EqualTo(ShipStatus.MaxMissiles), "Missile pickup clamps at 12");
        Assert.That(pickup.Message, Does.Contain("MISSILES"));
    }
}
