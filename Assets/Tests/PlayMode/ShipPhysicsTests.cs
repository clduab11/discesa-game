using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ShipPhysicsTests
{
    GameObject _go;
    ShipController _ship;
    ShipPhysics _physics;

    [SetUp]
    public void Setup()
    {
        // Arena so collision casts have something to hit.
        ArenaBuilder.Build(Vector3.zero, 40f, 20f, 40f);
        _physics = ScriptableObject.CreateInstance<ShipPhysics>();
        _physics.mass = 100f; _physics.maxThrust = 1000f; _physics.drag = 2f;
        _physics.rotthrust = 2f; _physics.rollRate = 2f; _physics.rotDrag = 8f;
        _physics.radius = 0.4f; _physics.maxSpeed = 60f;

        _go = new GameObject("Ship");
        _ship = _go.AddComponent<ShipController>();
        _ship.physics = _physics;
        _ship.fixedDt = 1f / 64f;
        _ship.ResetShip(new Vector3(-19f, 0, 0), Quaternion.identity);
        _go.transform.position = new Vector3(-19f, 0, 0);
        _go.transform.rotation = Quaternion.identity;
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        var arena = GameObject.Find("Arena");
        if (arena != null) Object.DestroyImmediate(arena);
        Object.DestroyImmediate(_physics);
    }

    [UnityTest]
    public IEnumerator TerminalVelocity_UnderConstantThrust_ReachesExpected()
    {
        _ship.SetInput(new DebugScriptedInput(new ShipInputState(0, 0, 0, 1, 0, 0, false)));
        float dt = 1f / 64f;
        for (int i = 0; i < 200; i++) _ship.Step(_ship.input.Sample(dt), dt);
        // Terminal v ≈ maxThrust/(mass*drag) = 1000/(100*2) = 5 m/s
        Assert.That(_ship.velocity.magnitude, Is.InRange(4f, 6f),
            $"Expected terminal ~5 m/s, got {_ship.velocity}");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Drag_DecaysExponentially()
    {
        _ship.ResetShip(new Vector3(0, 0, 0), Quaternion.identity);
        // Give initial velocity by stepping with a thrust, then zero input.
        float dt = 1f / 64f;
        var thrust = new ShipInputState(0, 0, 0, 1, 0, 0, false);
        for (int i = 0; i < 30; i++) _ship.Step(thrust, dt);
        float v0 = _ship.velocity.magnitude;
        Assert.Greater(v0, 0.5f, $"setup failed: no velocity after thrust");
        var zero = ShipInputState.Zero;
        for (int i = 0; i < 64; i++) _ship.Step(zero, dt); // 1 second
        float expected = v0 * Mathf.Exp(-_physics.drag * 1f);
        Assert.That(_ship.velocity.magnitude, Is.InRange(expected * 0.7f, expected * 1.4f),
            $"Expected ~{expected:F2}, got {_ship.velocity.magnitude:F2}");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Rotation_UsesLocalAxes()
    {
        _ship.ResetShip(new Vector3(0, 0, 0), Quaternion.identity);
        float dt = 1f / 64f;
        // Pitch only: should rotate about local X.
        var pitch = new ShipInputState(1f, 0, 0, 0, 0, 0, false);
        Quaternion q0 = _ship.transform.rotation;
        for (int i = 0; i < 64; i++) _ship.Step(pitch, dt);
        Quaternion q1 = _ship.transform.rotation;
        float angle = Quaternion.Angle(q0, q1);
        Assert.Greater(angle, 0.5f, $"Pitch produced almost no rotation ({angle} deg)");
        Vector3 fwd = q1 * Vector3.forward;
        Assert.Less(Mathf.Abs(fwd.x), 0.2f, $"Pitch should not yaw much, got fwd={fwd}");
        yield return null;
    }

    [UnityTest]
    public IEnumerator NoTunneling_AtMaxSpeedTowardThinWall()
    {
        _ship.ResetShip(new Vector3(0, 0, 0), Quaternion.identity);
        _go.transform.position = new Vector3(0, 0, 0);
        _go.transform.rotation = Quaternion.identity;
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0, 0, 2f);
        wall.transform.localScale = new Vector3(40f, 20f, 0.2f);
        yield return new WaitForFixedUpdate();
        _physics.maxSpeed = 40f;
        var thrust = new ShipInputState(0, 0, 0, 1, 0, 0, false);
        float dt = 1f / 64f;
        // Thin wall thickness 0.2 at z=2 → front face at 1.9. Ship half-extent 0.4.
        // No-tunnel criterion: the ship center must never cross z=2.0 (the wall slab).
        for (int i = 0; i < 300; i++)
        {
            _ship.Step(thrust, dt);
            float z = _go.transform.position.z;
            Assert.Less(z, 2.0f,
                $"Tunneled through thin wall at step {i}: z={z} (wall slab z≥1.9)");
        }
        Object.DestroyImmediate(wall);
        yield return null;
    }
}
