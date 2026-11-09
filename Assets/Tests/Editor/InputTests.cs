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

    [Test]
    public void LatchedInput_ReplaysOneSampleToEverySubstep()
    {
        var latched = new LatchedShipInput();
        latched.Latch(new ShipInputState(0f, 0f, 0f, 1f, 0f, 0f, true));
        Assert.That(latched.Sample(1f / 64f), Is.EqualTo(latched.Sample(1f / 64f)));
        Assert.That(latched.Sample(1f / 64f).Thrust, Is.EqualTo(1f));
        latched.Latch(ShipInputState.Zero);
        Assert.That(latched.Sample(1f / 64f), Is.EqualTo(ShipInputState.Zero));
    }
}
