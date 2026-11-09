using UnityEngine;

/// <summary>Input adapter that samples the hardware once per frame and replays the same
/// state to every fixed substep, so fixed-step physics and weapon fire cannot read two
/// different frames of input.</summary>
public sealed class LatchedShipInput : IShipInput
{
    ShipInputState _state;

    public ShipInputState Current => _state;

    public void Latch(ShipInputState state) => _state = state;

    public ShipInputState Sample(float deltaTime) => _state;
}
