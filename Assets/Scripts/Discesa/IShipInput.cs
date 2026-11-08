using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Ship input abstraction. Keyboard/mouse for play, scripted input for tests.
/// The ship controller never touches an input API directly, so a touch or gyro
/// implementation can be swapped in for the later mobile experiment.
/// </summary>
public interface IShipInput
{
    ShipInputState Sample(float deltaTime);
}

/// <summary>
/// Keyboard + mouse through the Input System package (this project is New-Input-only,
/// so legacy UnityEngine.Input is not available). Null-safe: with no devices attached
/// it simply reports zero input.
/// </summary>
public sealed class KeyboardMouseShipInput : IShipInput
{
    public ShipInputState Sample(float dt)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return ShipInputState.Zero;

        float pitch = 0f;
        if (kb.wKey.isPressed) pitch += 1f;      // nose up
        if (kb.sKey.isPressed) pitch -= 1f;
        if (kb.upArrowKey.isPressed) pitch = 1f;
        if (kb.downArrowKey.isPressed) pitch = -1f;

        float yaw = 0f;
        if (kb.aKey.isPressed) yaw -= 1f;        // nose left
        if (kb.dKey.isPressed) yaw += 1f;

        float roll = 0f;
        if (kb.qKey.isPressed) roll -= 1f;
        if (kb.eKey.isPressed) roll += 1f;

        float thrust = 0f;
        if (kb.iKey.isPressed) thrust += 1f;
        if (kb.kKey.isPressed) thrust -= 1f;

        float strafe = 0f;
        if (kb.jKey.isPressed) strafe -= 1f;
        if (kb.lKey.isPressed) strafe += 1f;

        float vertical = 0f;
        if (kb.pageUpKey.isPressed) vertical += 1f;
        if (kb.pageDownKey.isPressed) vertical -= 1f;

        // Shift sidesteps instead of yawing (classic 6DoF strafe toggle).
        if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
        {
            strafe = yaw;
            yaw = 0f;
        }

        bool fire = kb.spaceKey.isPressed;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed) fire = true;

        return new ShipInputState(pitch, yaw, roll, thrust, strafe, vertical, fire);
#else
        return ShipInputState.Zero;
#endif
    }
}

/// <summary>Replays a scripted sequence of input states; used by tests and demos.</summary>
public sealed class DebugScriptedInput : IShipInput
{
    readonly Queue<ShipInputState> _queue = new Queue<ShipInputState>();
    readonly ShipInputState _fallback;

    public DebugScriptedInput(ShipInputState fallback = default) { _fallback = fallback; }

    public void Enqueue(ShipInputState s) => _queue.Enqueue(s);

    public int Remaining => _queue.Count;

    public ShipInputState Sample(float dt) =>
        _queue.Count > 0 ? _queue.Dequeue() : _fallback;
}

/// <summary>Zero input — used when the game is over or paused.</summary>
public sealed class NullShipInput : IShipInput
{
    public ShipInputState Sample(float dt) => ShipInputState.Zero;
}
