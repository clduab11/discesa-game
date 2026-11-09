using System;
using UnityEngine;

/// <summary>
/// 6DoF ship controller. Original implementation of the Descent-style integration step:
/// fixed 64 Hz substeps, exponential linear drag, thrust composed from the ship's own axes,
/// then a swept move with slide-only response (no bounce in slice 1).
///
/// The motion model is plain math over plain data so it can be ported to another engine
/// without rewriting gameplay logic (see the engine bake-off note in docs/STATUS.md).
///
/// Behavioural reference (study only, no code copied): Descent 1 source release,
/// MAIN/PHYSICS.C do_physics_sim + MAIN/CONTROLS.C read_flying_controls.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Tuning")]
    public ShipPhysics physics;

    [Tooltip("Fixed integration step. 1/64 s mirrors the reference's fixed-step structure.")]
    public float fixedDt = 1f / 64f;

    [System.NonSerialized]
    [Tooltip("Union-of-volumes containment net. Walls stay authoritative; this catches escapes.")]
    public ArenaContainment containment;


    [Header("Input")]
    public IShipInput input;
    public Vector3 velocity { get; private set; }
    public Vector3 angularVelocity { get; private set; } // rad/s, local space (x pitch, y yaw, z roll)
    public bool IsDead { get; set; }

    public event Action OnDeath;

    Rigidbody _body;
    BoxCollider _sweepHull;
    const float Skin = 0.01f;
    const int MaxContacts = 8;
    readonly Vector3[] _contactNormals = new Vector3[MaxContacts];
    int _contactCount;
    float _stepAccumulator;

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.isKinematic = true;   // the controller owns motion; nothing else may move us
        _body.useGravity = false;

        // Query-only shape: never participates in contacts or gameplay trigger callbacks.
        _sweepHull = gameObject.AddComponent<BoxCollider>();
        _sweepHull.isTrigger = true;
        _sweepHull.enabled = false;

        if (physics == null) physics = ShipPhysics.CreateDefault();
        if (input == null) input = new KeyboardMouseShipInput();
    }

    public void SetInput(IShipInput newInput) => input = newInput;

    public void Kill()
    {
        if (IsDead) return;
        IsDead = true;
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;
        OnDeath?.Invoke();
    }

    public void ResetShip(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;
        IsDead = false;
        _stepAccumulator = 0f;
        containment?.Reset(position);
    }

    void FixedUpdate()
    {
        if (IsDead || physics == null || input == null) return;

        _stepAccumulator += Time.fixedDeltaTime;
        int guard = 0;
        while (_stepAccumulator >= fixedDt && guard++ < 8)
        {
            Step(input.Sample(fixedDt), fixedDt);
            _stepAccumulator -= fixedDt;
        }

        _body.position = transform.position;
        _body.rotation = transform.rotation;
    }

    /// <summary>One fixed substep. Deterministic in (position, rotation, velocity, input, dt).</summary>
    public void Step(ShipInputState inp, float dt)
    {
        if (physics == null || dt <= 0f) return;
        if (IsDead) inp = ShipInputState.Zero;

        if (containment != null && containment.Tick(transform.position, dt))
        {
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            return;
        }

        StepRotation(inp, dt);
        StepTranslation(inp, dt);

        // Validate the result too, but advance grace/dwell clocks only once per substep.
        if (containment != null && containment.Tick(transform.position, 0f))
        {
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
        }
    }

    void StepRotation(ShipInputState inp, float dt)
    {
        // Positive Pitch = nose up. Unity's +X rotation tips the nose down, hence the negation.
        var target = new Vector3(
            -inp.Pitch * physics.rotthrust,
             inp.Yaw * physics.rotthrust,
            -inp.Roll * physics.rollRate);

        // Angular response is a first-order lag, so release coasts briefly instead of snapping.
        float k = 1f - Mathf.Exp(-physics.rotDrag * dt);
        angularVelocity = Vector3.Lerp(angularVelocity, target, k);

        Quaternion q = transform.rotation;
        q *= Quaternion.AngleAxis(angularVelocity.x * dt * Mathf.Rad2Deg, Vector3.right);
        q *= Quaternion.AngleAxis(angularVelocity.y * dt * Mathf.Rad2Deg, Vector3.up);
        q *= Quaternion.AngleAxis(angularVelocity.z * dt * Mathf.Rad2Deg, Vector3.forward);
        transform.rotation = q;
    }

    void StepTranslation(ShipInputState inp, float dt)
    {
        Vector3 thrustWorld =
            transform.forward * (inp.Thrust * physics.maxThrust) +
            transform.right * (inp.Strafe * physics.maxSlideThrust) +
            transform.up * (inp.Vertical * physics.maxSlideThrust);

        velocity += (thrustWorld / physics.mass) * dt;
        velocity *= Mathf.Exp(-physics.drag * dt);

        if (velocity.sqrMagnitude > physics.maxSpeed * physics.maxSpeed)
            velocity = velocity.normalized * physics.maxSpeed;

        MoveWithSlide(velocity * dt);
    }

    /// <summary>
    /// Swept move with slide response. Walls are authoritative: the swept box stops at the
    /// contact and keeps the tangential component so the player slides instead of sticking.
    /// Arena containment is a separate safety net (see <see cref="containment"/>).
    /// </summary>
    void MoveWithSlide(Vector3 displacement)
    {
        // Step is also called explicitly after arena construction/teleports. A stale
        // broadphase can report distance-zero hits at a collider's previous pose.
        Physics.SyncTransforms();
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        Vector3 half = Vector3.one * physics.radius;
        Vector3 scale = transform.lossyScale;
        _sweepHull.size = new Vector3(2f * half.x / Mathf.Max(Mathf.Abs(scale.x), 1e-6f),
            2f * half.y / Mathf.Max(Mathf.Abs(scale.y), 1e-6f),
            2f * half.z / Mathf.Max(Mathf.Abs(scale.z), 1e-6f));
        _contactCount = 0;

        // A zero-distance BoxCast normal is synthetic (-direction). Recover a real
        // minimum separation first, even when a rotation-only step has zero displacement.
        // Unity requires an enabled shape for ComputePenetration. This synchronous
        // query finishes and disables it before the physics scene can simulate triggers.
        _sweepHull.enabled = true;
        bool separated = false;
        for (int pass = 0; pass < MaxContacts; pass++)
        {
            bool overlap = false;
            foreach (var other in Physics.OverlapBox(position, half, rotation, ~0,
                QueryTriggerInteraction.Ignore))
            {
                if (other.attachedRigidbody == _body || other.transform.IsChildOf(transform)) continue;
                if (!Physics.ComputePenetration(_sweepHull, position, rotation, other,
                    other.transform.position, other.transform.rotation, out var normal, out float depth)) continue;
                position += normal * (depth + Skin);
                RememberNormal(normal);
                overlap = true;
            }
            if (!overlap) { separated = true; break; }
        }
        _sweepHull.enabled = false;

        velocity = ConstrainToContacts(velocity);
        Vector3 remaining = ConstrainToContacts(displacement);
        for (int pass = 0; separated && pass < MaxContacts && remaining.sqrMagnitude > 1e-10f; pass++)
        {
            float length = remaining.magnitude;
            Vector3 direction = remaining / length;
            RaycastHit nearest = default;
            float distance = float.PositiveInfinity;
            foreach (var hit in Physics.BoxCastAll(position, half, direction, rotation,
                length, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.attachedRigidbody == _body || hit.collider.transform.IsChildOf(transform)) continue;
                if (Vector3.Dot(direction, hit.normal) >= -1e-6f || hit.distance >= distance) continue;
                nearest = hit;
                distance = hit.distance;
            }
            if (float.IsPositiveInfinity(distance)) { position += remaining; break; }

            // PHYSICS.C:875,938-946: consume moved time, remove only inward normal
            // momentum, then sweep the remainder. No artificial tangential friction.
            float travel = Mathf.Clamp(distance - Skin, 0f, length);
            position += direction * travel;
            remaining *= 1f - travel / length;
            RememberNormal(nearest.normal);
            remaining = ConstrainToContacts(remaining);
            velocity = ConstrainToContacts(velocity);
        }
        // Exhausting the bound never applies an unswept remainder.
        transform.position = position;
    }

    void RememberNormal(Vector3 normal)
    {
        for (int i = 0; i < _contactCount; i++)
            if (Vector3.Dot(_contactNormals[i], normal) > 0.9999f) return;
        if (_contactCount < MaxContacts) _contactNormals[_contactCount++] = normal;
    }

    Vector3 ConstrainToContacts(Vector3 value)
    {
        bool feasible = true;
        for (int i = 0; i < _contactCount; i++)
            if (Vector3.Dot(value, _contactNormals[i]) < -1e-6f) feasible = false;
        if (feasible) return value;

        // Project onto the feasible contact cone. One plane gives a slide; two
        // active planes give a crease. Sequential clipping alone can re-enter an
        // earlier plane in acute corners. Three independent planes can stop motion.
        Vector3 best = Vector3.zero;
        float bestError = value.sqrMagnitude;
        for (int i = 0; i < _contactCount; i++)
        for (int j = i; j < _contactCount; j++)
        {
            Vector3 candidate;
            if (i == j) candidate = Vector3.ProjectOnPlane(value, _contactNormals[i]);
            else
            {
                Vector3 crease = Vector3.Cross(_contactNormals[i], _contactNormals[j]);
                if (crease.sqrMagnitude < 1e-8f) continue;
                candidate = crease * (Vector3.Dot(value, crease) / crease.sqrMagnitude);
            }
            feasible = true;
            for (int k = 0; k < _contactCount; k++)
                if (Vector3.Dot(candidate, _contactNormals[k]) < -1e-6f) feasible = false;
            float error = (candidate - value).sqrMagnitude;
            if (feasible && error < bestError) { best = candidate; bestError = error; }
        }
        return best;
    }
}
