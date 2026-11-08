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
    [Tooltip("Linear drag in 1/s. Terminal speed = maxThrust / (mass * drag).")]
    public float drag = 0.8f;

    [Tooltip("Angular lag in 1/s: how quickly rotation approaches the commanded rate.")]
    public float rotDrag = 8f;

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
    /// Terminal speeds here: forward ≈ 20 m/s, slide ≈ 15 m/s.
    /// </summary>
    public static ShipPhysics CreateDefault()
    {
        var p = CreateInstance<ShipPhysics>();
        p.name = "ShipPhysics (runtime default)";
        p.mass = 100f;
        p.maxThrust = 1600f;
        p.maxSlideThrust = 1200f;
        p.drag = 0.8f;
        p.rotDrag = 8f;
        p.rotthrust = 2.4f;
        p.rollRate = 2.6f;
        p.radius = 0.55f;
        p.maxSpeed = 55f;
        return p;
    }
}
