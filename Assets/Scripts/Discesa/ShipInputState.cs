using System;
using UnityEngine;

// Value object: one sampled frame of ship input. No Unity Input API lives here.
public readonly struct ShipInputState : IEquatable<ShipInputState>
{
    public readonly float Pitch;      // + = nose up (rotation about local X), -1..1
    public readonly float Yaw;        // + = nose right (rotation about local Y), -1..1
    public readonly float Roll;       // + = roll right (rotation about local Z), -1..1
    public readonly float Thrust;     // + = forward, -1 = reverse
    public readonly float Strafe;     // + = right
    public readonly float Vertical;   // + = up
    public readonly bool Fire;

    public ShipInputState(float pitch, float yaw, float roll, float thrust, float strafe, float vertical, bool fire)
    {
        Pitch = pitch; Yaw = yaw; Roll = roll; Thrust = thrust; Strafe = strafe; Vertical = vertical; Fire = fire;
    }

    public static ShipInputState Zero => new ShipInputState(0f, 0f, 0f, 0f, 0f, 0f, false);

    public bool Equals(ShipInputState other) =>
        Pitch == other.Pitch && Yaw == other.Yaw && Roll == other.Roll &&
        Thrust == other.Thrust && Strafe == other.Strafe && Vertical == other.Vertical && Fire == other.Fire;

    public override bool Equals(object obj) => obj is ShipInputState s && Equals(s);

    public override int GetHashCode() => HashCode.Combine(Pitch, Yaw, Roll, Thrust, Strafe, Vertical, Fire);

    public override string ToString() =>
        $"Pitch={Pitch:F2} Yaw={Yaw:F2} Roll={Roll:F2} Thrust={Thrust:F2} Strafe={Strafe:F2} Vert={Vertical:F2} Fire={Fire}";
}
