using NUnit.Framework;
using UnityEngine;

public class InputTests
{
    [Test]
    public void ShipInputState_EqualityWorks()
    {
        var a = new ShipInputState(1, 2, 3, 4, 5, 6, true);
        var b = new ShipInputState(1, 2, 3, 4, 5, 6, true);
        var c = new ShipInputState(1, 2, 3, 4, 5, 6, false);
        Assert.That(a.Equals(b), Is.True);
        Assert.That(a.Equals(c), Is.False);
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        Assert.That(a.ToString(), Does.Contain("Pitch"));
    }

    [Test]
    public void DebugScriptedInput_ReplaysQueue()
    {
        var inp = new DebugScriptedInput(new ShipInputState(9, 9, 9, 9, 9, 9, true));
        inp.Enqueue(ShipInputState.Zero);
        inp.Enqueue(new ShipInputState(0.5f, 0, 0, 0, 0, 0, false));
        Assert.That(inp.Sample(0.01f), Is.EqualTo(ShipInputState.Zero));
        var second = inp.Sample(0.01f);
        Assert.That(second.Pitch, Is.EqualTo(0.5f));
        var fallback = inp.Sample(0.01f);
        Assert.That(fallback.Pitch, Is.EqualTo(9f));
    }

    [Test]
    public void KeyboardMouseShipInput_SamplesWithoutError()
    {
        var inp = new KeyboardMouseShipInput();
        var s = inp.Sample(0.016f);
        Assert.That(s.Pitch, Is.InRange(-1f, 1f));
        Assert.That(s.Thrust, Is.InRange(-1f, 1f));
    }
}

public class CollisionTests
{
    [Test]
    public void ArenaBuilder_CreatesSixWallsAndTwoPillars()
    {
        var arena = ArenaBuilder.Build(Vector3.zero, 40f, 20f, 40f);
        var colliders = arena.GetComponentsInChildren<Collider>();

        // 6 walls + 2 pillars.
        Assert.That(colliders.Length, Is.EqualTo(8), $"Expected 8 colliders, got {colliders.Length}");

        foreach (var name in new[] { "Ceiling", "Floor", "WallWest", "WallEast", "WallNorth", "WallSouth", "PillarSW", "PillarNE" })
            Assert.IsNotNull(arena.transform.Find(name), $"Missing arena part: {name}");

        foreach (var c in colliders)
            Assert.That(c.isTrigger, Is.False, $"{c.name} must be a solid collider for the ship sweep");

        Object.DestroyImmediate(arena);
    }

    [Test]
    public void ArenaBuilder_IsEnclosing()
    {
        var arena = ArenaBuilder.Build(Vector3.zero, 40f, 20f, 40f);

        foreach (var c in arena.GetComponentsInChildren<Collider>())
        {
            var b = c.bounds;
            Assert.That(b.min.x <= 20f && b.max.x >= -20f, $"Part {c.name} does not span the arena");
            Assert.That(b.min.y <= 10f && b.max.y >= -10f, $"Part {c.name} does not span arena height");
        }

        Object.DestroyImmediate(arena);
    }
}
