using NUnit.Framework;
using UnityEngine;

public class WeaponTests
{
    [Test]
    public void WeaponDefinitions_MatchSliceTuning()
    {
        var pulse = WeaponDefinition.Pulse;
        Assert.That(pulse.EnergyCost, Is.EqualTo(2f));
        Assert.That(pulse.Cooldown, Is.EqualTo(0.12f));
        Assert.That(pulse.Pellets, Is.EqualTo(1));
        Assert.That(pulse.Damage, Is.EqualTo(10));
        Assert.That(pulse.Speed, Is.EqualTo(70f));

        var spread = WeaponDefinition.Spread;
        Assert.That(spread.EnergyCost, Is.EqualTo(6f));
        Assert.That(spread.Cooldown, Is.EqualTo(0.35f));
        Assert.That(spread.Pellets, Is.EqualTo(3));
        Assert.That(spread.SpreadDegrees, Is.EqualTo(6f));
        Assert.That(spread.Damage, Is.EqualTo(6));

        var heavy = WeaponDefinition.Heavy;
        Assert.That(heavy.Slot, Is.EqualTo(WeaponSlot.Secondary));
        Assert.That(heavy.AmmoCost, Is.EqualTo(1));
        Assert.That(heavy.Cooldown, Is.EqualTo(1f));
        Assert.That(heavy.Speed, Is.EqualTo(25f));
        Assert.That(heavy.Damage, Is.EqualTo(40));
        Assert.That(heavy.SplashRadius, Is.GreaterThan(0f));
    }

    [Test]
    public void WeaponIndex_MapsInOrder()
    {
        Assert.That(WeaponDefinition.Count, Is.EqualTo(3));
        Assert.That(WeaponDefinition.Get(0).Id, Is.EqualTo("pulse"));
        Assert.That(WeaponDefinition.Get(1).Id, Is.EqualTo("spread"));
        Assert.That(WeaponDefinition.Get(2).Id, Is.EqualTo("heavy"));

        // Out-of-range indices clamp instead of throwing.
        Assert.That(WeaponDefinition.Get(-1).Id, Is.EqualTo("pulse"));
        Assert.That(WeaponDefinition.Get(99).Id, Is.EqualTo("heavy"));
    }

    [Test]
    public void OnlyHeavyUsesAmmo()
    {
        Assert.That(WeaponDefinition.Pulse.UsesEnergy, Is.True);
        Assert.That(WeaponDefinition.Pulse.UsesAmmo, Is.False);
        Assert.That(WeaponDefinition.Spread.UsesAmmo, Is.False);
        Assert.That(WeaponDefinition.Heavy.UsesEnergy, Is.False);
        Assert.That(WeaponDefinition.Heavy.UsesAmmo, Is.True);
    }
}
