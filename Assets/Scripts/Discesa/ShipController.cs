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

    [Tooltip("Half-extents of the enclosed arena the ship is clamped inside.")]
    public Vector3 arenaHalfExtents = new Vector3(20f, 10f, 20f);

    [Header("Input")]
    public IShipInput input;
    public Vector3 velocity { get; private set; }
    public Vector3 angularVelocity { get; private set; } // rad/s, local space (x pitch, y yaw, z roll)
    public bool IsDead { get; set; }

    public event Action OnDeath;

    Rigidbody _body;
    float _stepAccumulator;

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.isKinematic = true;   // the controller owns motion; nothing else may move us
        _body.useGravity = false;

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

        StepRotation(inp, dt);
        StepTranslation(inp, dt);
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
    /// Swept move with slide response. Two layers:
    /// 1) hard clamp inside the arena envelope, so the ship can never leave the level;
    /// 2) a swept box against other colliders (pillars, turret), stopping at the contact
    ///    and keeping the tangential component so the player slides instead of sticking.
    /// </summary>
    void MoveWithSlide(Vector3 displacement)
    {
        if (displacement.sqrMagnitude <= 1e-10f) return;

        Vector3 start = transform.position;
        Vector3 desired = start + displacement;

        Vector3 clamped = new Vector3(
            Mathf.Clamp(desired.x, -arenaHalfExtents.x + physics.radius, arenaHalfExtents.x - physics.radius),
            Mathf.Clamp(desired.y, -arenaHalfExtents.y + physics.radius, arenaHalfExtents.y - physics.radius),
            Mathf.Clamp(desired.z, -arenaHalfExtents.z + physics.radius, arenaHalfExtents.z - physics.radius));

        if ((clamped - desired).sqrMagnitude > 1e-10f)
            RemoveVelocityAlong((clamped - desired).normalized);

        Vector3 step = clamped - start;
        float stepLen = step.magnitude;
        if (stepLen <= 1e-6f) return;

        Vector3 dir = step / stepLen;
        Vector3 half = Vector3.one * physics.radius;

        RaycastHit hit;
        if (!Physics.BoxCast(start, half, dir, out hit, transform.rotation, stepLen, ~0, QueryTriggerInteraction.Ignore))
        {
            transform.position = clamped;
            return;
        }

        // Contact centre is origin + direction * distance: unambiguous for sweeps and
        // immune to normal-sign mistakes. Stop a hair short so the next cast starts free.
        float travel = Mathf.Max(hit.distance - 0.01f, 0f);
        Vector3 contactCentre = start + dir * travel;
        Vector3 slide = step - hit.normal * Vector3.Dot(step, hit.normal);

        if (slide.sqrMagnitude > 1e-8f)
        {
            Vector3 slideDir = slide.normalized;
            RaycastHit slideHit;
            if (!Physics.BoxCast(contactCentre, half, slideDir, out slideHit, transform.rotation, slide.magnitude, ~0, QueryTriggerInteraction.Ignore))
                contactCentre += slide;
        }

        contactCentre.x = Mathf.Clamp(contactCentre.x, -arenaHalfExtents.x + physics.radius, arenaHalfExtents.x - physics.radius);
        contactCentre.y = Mathf.Clamp(contactCentre.y, -arenaHalfExtents.y + physics.radius, arenaHalfExtents.y - physics.radius);
        contactCentre.z = Mathf.Clamp(contactCentre.z, -arenaHalfExtents.z + physics.radius, arenaHalfExtents.z - physics.radius);

        transform.position = contactCentre;
        RemoveVelocityAlong(hit.normal);
        velocity *= 0.9f;   // light impact friction; slice 1 has no bounce
    }

    void RemoveVelocityAlong(Vector3 normal)
    {
        velocity -= normal * Vector3.Dot(velocity, normal);
    }
}
