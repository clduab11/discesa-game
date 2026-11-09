using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ArenaPlayTests
{
    GameObject _arena;
    GameObject _shipGo;

    [TearDown]
    public void TearDown()
    {
        if (_shipGo != null) Object.Destroy(_shipGo);
        if (_arena != null) Object.Destroy(_arena);
    }

    [UnityTest]
    public IEnumerator Ship_CanTraverseEveryDoorway()
    {
        var layout = ArenaLayout.CreateDefault();
        _arena = ArenaBuilder.Build(layout, includeProps: false);
        Physics.SyncTransforms();
        yield return null;

        foreach (var d in layout.Doorways)
        {
            bool alongX = d.Size.x <= d.Size.z; // doorway plane is x = const
            Vector3 normal = alongX ? Vector3.right : Vector3.forward;
            Vector3 start = d.Center - normal * 2f;

            RaycastHit hit;
            bool blocked = Physics.BoxCast(start, Vector3.one * 0.4f, normal, out hit,
                Quaternion.identity, 4f, ~0, QueryTriggerInteraction.Ignore);

            Assert.That(blocked, Is.False, $"{d.Name} is not passable: hit {hit.collider?.name}");
        }
    }

    [UnityTest]
    public IEnumerator Ship_CannotEscapeAtMaxSpeed()
    {
        var layout = ArenaLayout.CreateDefault();
        _arena = ArenaBuilder.Build(layout, includeProps: false);
        Physics.SyncTransforms();
        yield return null;

        var physics = ScriptableObject.CreateInstance<ShipPhysics>();
        physics.mass = 100f;
        physics.maxThrust = 1600f;
        physics.drag = 0.8f;
        physics.maxSpeed = 55f;
        physics.radius = 0.55f;

        _shipGo = new GameObject("Ship");
        _shipGo.transform.position = new Vector3(-33f, -1f, 0f);
        _shipGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        var ship = _shipGo.AddComponent<ShipController>();
        ship.physics = physics;
        ship.ResetShip(new Vector3(-33f, -1f, 0f), Quaternion.Euler(0f, 90f, 0f));

        var containment = new ArenaContainment(layout, _shipGo.transform, physics.radius);
        containment.Reset(_shipGo.transform.position);
        ship.containment = containment;

        var input = new DebugScriptedInput(new ShipInputState(0f, 0f, 0f, 1f, 0f, 0f, false));
        float dt = 1f / 64f;

        for (int i = 0; i < 400; i++)
        {
            ship.Step(input.Sample(dt), dt);
            Physics.SyncTransforms();
            if (i % 20 == 0)
            {
                Assert.That(layout.Contains(_shipGo.transform.position), Is.True,
                    $"Ship escaped at step {i}: {_shipGo.transform.position}");
            }
        }

        Assert.That(layout.Contains(_shipGo.transform.position), Is.True,
            $"Ship escaped: {_shipGo.transform.position}");
        Assert.That(containment.RespawnCount, Is.EqualTo(0),
            "Walls alone must contain the ship; the safety net should not be needed");

        Object.DestroyImmediate(physics);
        yield return null;
    }
}
