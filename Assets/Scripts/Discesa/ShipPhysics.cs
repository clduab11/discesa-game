using UnityEngine;

/// <summary>
/// Ship tuning in SI units (m, kg, N, s, rad/s). Kept as plain data so the motion
/// model can be ported to another engine without carrying engine types with it.
/// </summary>
[CreateAssetMenu(menuName = "Discesa/Ship Physics", fileName = "ShipPhysics")]
public class ShipPhysics : ScriptableObject
{
    [Header("Mass and thrust")]
    [Tooltip("Ship mass in kg. Scales thrust into acceleration.")]
    public float mass = 100f;

    [Tooltip("Forward/reverse thrust in newtons.")]
    public float maxThrust = 1600f;

    [Tooltip("Lateral and vertical (slide) thrust in newtons.")]
    public float maxSlideThrust = 1200f;

    [Header("Damping")]
    // PHYSICS.C:450,647-666: FT = F1_0/64 = fix 1024 (0x400),
    // converted to 0.015625f seconds. Each reference tick adds thrust/mass,
    // then multiplies velocity by (1-d). In SI units that tick impulse is a*FT:
    // a reference per-tick thrust value corresponds to 64 times that value in N.
    // Our existing drag is an exponential rate, so d64 = 1-exp(-drag/64).
    // vInf = (thrust/mass)*FT*(1-d64)/d64, not exactly a/drag.
    [Tooltip("Linear decay rate in 1/s. At 64 Hz terminal speed is (thrust/mass)*dt/(exp(drag*dt)-1).")]
    public float drag = 0.8f;

    // PHYSICS.C:478: rotational per-tick loss = (linear fix drag * 5)/2.
    // The original integer ratio 5/2 = 2.5f (equivalent 16.16 value 0x28000).
    // At our chosen drag=0.8/s: d64=0.0124221995, angular retention=0.9689445012,
    // rotDrag=-64*ln(1-2.5*d64)=2.01906835f /s. Lerp's (1-exp(-rotDrag*dt))
    // therefore has the reference's release/response pole at exactly 64 Hz.
    // This preserves our SI tuning, NOT an assertion of original ship-table values
    // or bit-identical integer rounding. Custom assets can still override either rate.
    [Tooltip("Angular lag in 1/s: how quickly rotation approaches the commanded rate.")]
    public float rotDrag = 2.01906835f;

    // Reference banking, extracted but not enabled in this free-6DoF increment:
    // ROLL_RATE=0x2000 (8192/65536 turns/s) -> 0.785398163f rad/s,
    // or 0.0122718463f rad per 64 Hz tick; DAMP_ANG=0x400 -> 0.0981747704f rad.
    // TURNROLL_SCALE=(0x4ec4/2)=10082 -> 0.153839111f; target bank=-yawRate*scale.
    // do_physics_align_object chooses the segment side nearest the current up axis,
    // skips |dot(up,forward)|>=F1_0/2 (fix 32768 -> 0.5f), and rate-limits roll.
    // Segment-relative leveling must not be replaced with world-up auto-leveling.

    [Header("Rotation rates (rad/s)")]
    public float rotthrust = 2.4f;
    public float rollRate = 2.6f;

    [Header("Collision")]
    [Tooltip("Sweep half-extent for the ship volume, in metres.")]
    public float radius = 0.55f;

    [Tooltip("Hard speed clamp in m/s. Safety rail, not a design target.")]
    public float maxSpeed = 55f;

    /// <summary>
    /// Runtime fallback used when no ShipPhysics asset is assigned.
    /// Exact 64 Hz terminal speeds: forward 19.87526 m/s, slide 14.90645 m/s.
    /// </summary>
    public static ShipPhysics CreateDefault()
    {
        var p = CreateInstance<ShipPhysics>();
        p.name = "ShipPhysics (runtime default)";
        p.mass = 100f;
        p.maxThrust = 1600f;
        p.maxSlideThrust = 1200f;
        p.drag = 0.8f;
        p.rotthrust = 2.4f;
        p.rollRate = 2.6f;
        p.radius = 0.55f;
        p.maxSpeed = 55f;
        return p;
    }
}
