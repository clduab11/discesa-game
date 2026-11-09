using NUnit.Framework;
using UnityEngine;

public class KinematicRegressionTests
{
    const float Dt = 1f / 64f;
    static readonly Vector3 Origin = new Vector3(1000f, 1000f, 1000f);
    GameObject _root;
    ShipController _ship;
    ShipPhysics _physics;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("Kinematic regression fixture");
        var body = new GameObject("Ship");
        body.transform.SetParent(_root.transform);
        body.transform.position = Origin;
        _ship = body.AddComponent<ShipController>();
        Object.DestroyImmediate(_ship.physics);
        _physics = ShipPhysics.CreateDefault();
        _physics.mass = 1f;
        _physics.drag = 0f;
        _physics.radius = 0.5f;
        _physics.maxSpeed = 200f;
        _ship.physics = _physics;
        _ship.enabled = false; // These tests advance only explicit 64 Hz steps.
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_physics);
    }

    BoxCollider Wall(Vector3 position, Vector3 size, Quaternion rotation)
    {
        var wall = new GameObject("Wall");
        wall.transform.SetParent(_root.transform);
        wall.transform.SetPositionAndRotation(Origin + position, rotation);
        var box = wall.AddComponent<BoxCollider>();
        box.size = size;
        return box;
    }

    void Kick(Vector3 velocity)
    {
        _physics.maxThrust = velocity.z / Dt;
        _physics.maxSlideThrust = 1f / Dt;
        _ship.Step(new ShipInputState(0, 0, 0, 1, velocity.x, velocity.y, false), Dt);
        _physics.maxThrust = _physics.maxSlideThrust = 0f;
    }

    ArenaContainment Net(float grace = 0f)
    {
        var layout = new ArenaLayout();
        layout.ShipSpawn = Origin;
        layout.Rooms.Add(new ArenaVolume("Test room", Origin - Vector3.one * 5f,
            Origin + Vector3.one * 5f));
        return new ArenaContainment(layout, _ship.transform, _physics.radius)
        {
            escapeGrace = grace,
            safeUpdateInterval = 0.25f
        };
    }

    [Test]
    public void InitialOverlap_UsesRealSeparationNormal_NotSyntheticSweepNormal()
    {
        var wall = Wall(Vector3.zero, new Vector3(0.2f, 20f, 20f), Quaternion.identity);
        _ship.transform.position = Origin + Vector3.right * 0.45f;
        Physics.SyncTransforms();
        var hits = Physics.BoxCastAll(_ship.transform.position, Vector3.one * 0.5f,
            Vector3.forward, Quaternion.identity, 1f, ~0, QueryTriggerInteraction.Ignore);
        var hit = System.Array.Find(hits, h => h.collider == wall);
        Assert.That(hit.collider, Is.EqualTo(wall), "Fixture must begin overlapping the wall");
        Assert.That(hit.distance, Is.Zero);
        Assert.That(Vector3.Dot(hit.normal, Vector3.back), Is.GreaterThan(0.999f));

        Kick(Vector3.forward);

        Assert.That(_ship.transform.position.x - Origin.x, Is.GreaterThanOrEqualTo(0.599f));
        Assert.That(_ship.transform.position.z - Origin.z, Is.EqualTo(Dt).Within(0.001f));
        Assert.That(_ship.velocity.z, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void StationaryRotatedHull_RecoversOverlapWithoutThrust()
    {
        Wall(Vector3.zero, new Vector3(0.2f, 20f, 20f), Quaternion.identity);
        _ship.transform.SetPositionAndRotation(Origin + Vector3.right * 0.7f,
            Quaternion.Euler(0f, 45f, 0f));
        Physics.SyncTransforms();

        _ship.Step(ShipInputState.Zero, Dt);

        Assert.That(_ship.transform.position.x - Origin.x, Is.GreaterThan(0.806f));
        Assert.That(_ship.velocity, Is.EqualTo(Vector3.zero));
    }

    [Test]
    public void WallSlide_PreservesTangent_ConsumesOnlyRemainingDisplacement()
    {
        Wall(Vector3.right, new Vector3(0.2f, 20f, 20f), Quaternion.identity);
        Physics.SyncTransforms();

        Kick(new Vector3(64f, 0f, 64f));

        Assert.That(_ship.transform.position.x - Origin.x, Is.InRange(0.37f, 0.401f));
        Assert.That(_ship.transform.position.z - Origin.z, Is.EqualTo(1f).Within(0.015f));
        Assert.That(_ship.velocity.z, Is.EqualTo(64f).Within(0.001f));
        Assert.That(_ship.velocity.x, Is.EqualTo(0f).Within(0.001f));
    }

    [TestCase(30f)]
    [TestCase(45f)]
    public void AcuteCrevice_PreservesCreaseMomentum_WithoutBacktracking(float angle)
    {
        Vector3 n1 = Vector3.right;
        Vector3 n2 = new Vector3(-Mathf.Cos(angle * Mathf.Deg2Rad), 0f,
            Mathf.Sin(angle * Mathf.Deg2Rad));
        Wall(-n1 * 0.1f, new Vector3(0.2f, 40f, 40f), Quaternion.identity);
        Wall(-n2 * 0.1f, new Vector3(0.2f, 40f, 40f),
            Quaternion.FromToRotation(Vector3.right, n2));
        _ship.transform.position = Origin + new Vector3(0.65f, 0f,
            0.65f / Mathf.Tan(angle * 0.5f * Mathf.Deg2Rad));
        Physics.SyncTransforms();
        Kick(new Vector3(0f, 3f, -4f));
        float y = _ship.transform.position.y;
        for (int i = 0; i < 128; i++)
        {
            float previousZ = _ship.transform.position.z;
            _ship.Step(ShipInputState.Zero, Dt);
            Assert.That(_ship.transform.position.z, Is.LessThanOrEqualTo(previousZ + 0.002f),
                $"Backtracking at step {i}, angle {angle}");
            Assert.That(_ship.velocity.y, Is.EqualTo(3f).Within(0.001f),
                $"Lost crease momentum at step {i}, angle {angle}");
        }
        Assert.That(_ship.transform.position.y - y, Is.EqualTo(6f).Within(0.01f));
        Vector3 local = _ship.transform.position - Origin;
        Assert.That(Vector3.Dot(local, n1), Is.GreaterThanOrEqualTo(0.499f));
        float support = 0.5f * (Mathf.Abs(n2.x) + Mathf.Abs(n2.z));
        Assert.That(Vector3.Dot(local, n2), Is.GreaterThanOrEqualTo(support - 0.001f));
    }

    [Test]
    public void Sweep_IgnoresCollidersAttachedToOwnBody()
    {
        _ship.gameObject.AddComponent<BoxCollider>().size = Vector3.one;
        Physics.SyncTransforms();

        Kick(Vector3.forward * 2f);

        Assert.That(_ship.transform.position.z - Origin.z, Is.EqualTo(2f * Dt).Within(0.001f));
        Assert.That(_ship.velocity.z, Is.EqualTo(2f).Within(0.001f));
    }

    [Test]
    public void Containment_ExpiredEscapeIsRecoveredBeforeMovementCanHideIt()
    {
        Kick(Vector3.left * 128f);
        _ship.transform.position = Origin;
        _ship.containment = Net();
        _ship.transform.position = Origin + Vector3.right * 6f;

        _ship.Step(ShipInputState.Zero, Dt);

        Assert.That(_ship.transform.position, Is.EqualTo(Origin));
        Assert.That(_ship.velocity, Is.EqualTo(Vector3.zero));
        Assert.That(_ship.containment.RespawnCount, Is.EqualTo(1));
    }

    [Test]
    public void Containment_ExteriorToleranceCannotPoisonLastSafe()
    {
        var net = Net();
        _ship.transform.position = Origin + Vector3.right * 5.25f;

        Assert.That(net.Tick(_ship.transform.position, 0.3f), Is.True);
        Assert.That(net.LastSafe, Is.EqualTo(Origin));
        Assert.That(_ship.transform.position, Is.EqualTo(Origin));
    }

    [Test]
    public void Containment_SafeDwellMustBeContinuous()
    {
        var net = Net(10f);
        net.Tick(Origin + Vector3.right, 0.2f);
        net.Tick(Origin + Vector3.right * 6f, 0.1f);
        net.Tick(Origin + Vector3.right * 2f, 0.06f);

        Assert.That(net.LastSafe, Is.EqualTo(Origin));
    }

    [Test]
    public void Containment_DoesNotCacheAHullIntersectingAWall()
    {
        Wall(Vector3.right * 2f, new Vector3(0.2f, 20f, 20f), Quaternion.identity);
        Physics.SyncTransforms();
        var net = Net();
        _ship.transform.position = Origin + Vector3.right * 2f;

        net.Tick(_ship.transform.position, 0.3f);

        Assert.That(net.LastSafe, Is.EqualTo(Origin));
    }

    [Test]
    public void InitialOverlap_MotionAwayFromWallKeepsSeparatingMomentum()
    {
        Wall(Vector3.zero, new Vector3(0.2f, 20f, 20f), Quaternion.identity);
        _ship.transform.position = Origin + Vector3.right * 0.45f;
        Physics.SyncTransforms();

        Kick(Vector3.right * 4f);

        Assert.That(_ship.transform.position.x - Origin.x, Is.GreaterThan(0.66f));
        Assert.That(_ship.velocity.x, Is.EqualTo(4f).Within(0.001f));
    }

    [Test]
    public void ThreePlaneCorner_StopsWithoutPenetrationOrEnergyGain()
    {
        Wall(Vector3.right, new Vector3(0.2f, 20f, 20f), Quaternion.identity);
        Wall(Vector3.up, new Vector3(20f, 0.2f, 20f), Quaternion.identity);
        Wall(Vector3.forward, new Vector3(20f, 20f, 0.2f), Quaternion.identity);
        Physics.SyncTransforms();

        Kick(Vector3.one * 64f);
        for (int i = 0; i < 64; i++) _ship.Step(ShipInputState.Zero, Dt);

        Vector3 local = _ship.transform.position - Origin;
        Assert.That(local.x, Is.InRange(0.37f, 0.401f));
        Assert.That(local.y, Is.InRange(0.37f, 0.401f));
        Assert.That(local.z, Is.InRange(0.37f, 0.401f));
        Assert.That(_ship.velocity.sqrMagnitude, Is.LessThan(0.000001f));
    }

    [Test]
    public void Containment_GraceAdvancesOnlyOncePerSubstep()
    {
        _ship.containment = Net(2f * Dt);
        _ship.transform.position = Origin + Vector3.right * 6f;

        _ship.Step(ShipInputState.Zero, Dt);
        Assert.That(_ship.containment.RespawnCount, Is.Zero);
        Assert.That(_ship.containment.OutsideTime, Is.EqualTo(Dt).Within(0.000001f));
        _ship.Step(ShipInputState.Zero, Dt);

        Assert.That(_ship.containment.RespawnCount, Is.EqualTo(1));
        Assert.That(_ship.transform.position, Is.EqualTo(Origin));
    }

    [Test]
    public void Containment_RotatedHullOutsideUnionCannotBecomeLastSafe()
    {
        var net = Net();
        _ship.transform.SetPositionAndRotation(Origin + Vector3.right * 4.4f,
            Quaternion.Euler(0f, 45f, 0f));

        net.Tick(_ship.transform.position, 0.3f);

        Assert.That(net.LastSafe, Is.EqualTo(Origin));
    }

    [Test]
    public void DefaultDamping_MatchesReferenceFiveHalvesPerTickRatio()
    {
        var tuning = ShipPhysics.CreateDefault();
        try
        {
            float linearLoss = 1f - Mathf.Exp(-tuning.drag * Dt);
            float angularLoss = 1f - Mathf.Exp(-tuning.rotDrag * Dt);
            Assert.That(angularLoss, Is.EqualTo(2.5f * linearLoss).Within(0.000001f));
        }
        finally { Object.DestroyImmediate(tuning); }
    }

    [Test]
    public void DefaultAngularCoast_MatchesReferenceRetentionOverOneSecond()
    {
        var tuning = ShipPhysics.CreateDefault();
        try
        {
            _physics.rotDrag = tuning.rotDrag;
            _ship.Step(new ShipInputState(0f, 1f, 0f, 0f, 0f, 0f, false), Dt);
            float initial = _ship.angularVelocity.y;
            for (int i = 0; i < 64; i++) _ship.Step(ShipInputState.Zero, Dt);
            float retention = 1f - 2.5f * (1f - Mathf.Exp(-tuning.drag * Dt));
            Assert.That(_ship.angularVelocity.y,
                Is.EqualTo(initial * Mathf.Pow(retention, 64f)).Within(0.000001f));
        }
        finally { Object.DestroyImmediate(tuning); }
    }
}
