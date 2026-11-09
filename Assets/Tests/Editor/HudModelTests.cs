using NUnit.Framework;
using UnityEngine;

public class HudModelTests
{
    [Test]
    public void HudModel_ClampsShieldEnergyAndAmmo()
    {
        var m = new HudModel(150f, -20f, 99, 0, 200f, 150f, false, false);
        Assert.That(m.Shield, Is.EqualTo(100f));
        Assert.That(m.Energy, Is.EqualTo(0f));
        Assert.That(m.Missiles, Is.EqualTo(12));
        Assert.That(m.CoreHp, Is.EqualTo(150f));

        var low = new HudModel(-5f, 200f, -3, 0, -10f, 150f, false, false, 2f);
        Assert.That(low.Shield, Is.EqualTo(0f));
        Assert.That(low.Energy, Is.EqualTo(100f));
        Assert.That(low.Missiles, Is.EqualTo(0));
        Assert.That(low.CoreHp, Is.EqualTo(0f));
        Assert.That(low.DamageFlash, Is.EqualTo(1f));
    }

    [Test]
    public void HudModel_MapsWeaponAndAmmoLabels()
    {
        var pulse = new HudModel(100f, 100f, 6, 0, 150f, 150f, false, false);
        Assert.That(pulse.WeaponName, Is.EqualTo("PULSE"));
        Assert.That(pulse.WeaponSlotText, Is.EqualTo("PRIMARY"));
        Assert.That(pulse.CostText, Is.EqualTo("ENERGY 2"));
        Assert.That(pulse.MissileText, Is.EqualTo("MISSILES 06"));

        var spread = new HudModel(100f, 100f, 6, 1, 150f, 150f, false, false);
        Assert.That(spread.WeaponName, Is.EqualTo("SPREAD"));
        Assert.That(spread.CostText, Is.EqualTo("ENERGY 6"));

        var heavy = new HudModel(100f, 100f, 4, 2, 150f, 150f, false, false);
        Assert.That(heavy.WeaponName, Is.EqualTo("HEAVY"));
        Assert.That(heavy.WeaponSlotText, Is.EqualTo("SECONDARY"));
        Assert.That(heavy.CostText, Is.EqualTo("AMMO 1"));
        Assert.That(heavy.MissileText, Is.EqualTo("MISSILES 04"));
    }

    [Test]
    public void HudModel_ReportsLowResourcesAndOutcomeText()
    {
        var low = new HudModel(20f, 15f, 6, 0, 150f, 150f, false, false);
        Assert.That(low.LowShield, Is.True);
        Assert.That(low.LowEnergy, Is.True);

        var fine = new HudModel(60f, 60f, 6, 0, 150f, 150f, false, false);
        Assert.That(fine.LowShield, Is.False);
        Assert.That(fine.LowEnergy, Is.False);

        var win = new HudModel(100f, 100f, 6, 0, 0f, 150f, true, false);
        Assert.That(win.BannerText, Does.Contain("CORE DESTROYED"));

        var lose = new HudModel(0f, 100f, 6, 0, 150f, 150f, false, true);
        Assert.That(lose.BannerText, Does.Contain("SHIP DESTROYED"));
    }

    [Test]
    public void HudModel_FormatsIntegersAsCeil()
    {
        var m = new HudModel(40.2f, 65.1f, 6, 0, 149.2f, 150f, false, false);
        Assert.That(m.ShieldText, Is.EqualTo("41"));
        Assert.That(m.EnergyText, Is.EqualTo("66"));
        Assert.That(m.ObjectiveText, Is.EqualTo("CORE 150"));
    }
}

public class ShipStatusTests
{
    static ShipStatus NewStatus()
    {
        var go = new GameObject("StatusTest");
        return go.AddComponent<ShipStatus>();
    }

    [Test]
    public void AddEnergy_ClampsAtMaximum()
    {
        var status = NewStatus();
        status.SetResources(100f, 90f, 6);
        Assert.That(status.AddEnergy(25f), Is.EqualTo(10f));
        Assert.That(status.Energy, Is.EqualTo(100f));
        Object.DestroyImmediate(status.gameObject);
    }

    [Test]
    public void AddMissiles_ClampsAtMaximum()
    {
        var status = NewStatus();
        status.SetResources(100f, 100f, 10);
        Assert.That(status.AddMissiles(4), Is.EqualTo(2));
        Assert.That(status.Missiles, Is.EqualTo(ShipStatus.MaxMissiles));
        Object.DestroyImmediate(status.gameObject);
    }

    [Test]
    public void Consume_FailsWhenEmptyAndClamps()
    {
        var status = NewStatus();
        status.SetResources(100f, 1f, 1);
        Assert.That(status.TryConsumeEnergy(2f), Is.False);
        Assert.That(status.Energy, Is.EqualTo(1f));
        Assert.That(status.TryConsumeMissile(), Is.True);
        Assert.That(status.Missiles, Is.EqualTo(0));
        Assert.That(status.TryConsumeMissile(), Is.False);

        status.Damage(100f);
        Assert.That(status.Shield, Is.EqualTo(0f));
        Assert.That(status.IsDead, Is.True);
        Object.DestroyImmediate(status.gameObject);
    }

    [Test]
    public void WeaponSelection_ClampsAndCycles()
    {
        var status = NewStatus();
        status.SelectWeapon(99);
        Assert.That(status.WeaponIndex, Is.EqualTo(WeaponDefinition.Count - 1));
        status.CycleWeapon();
        Assert.That(status.WeaponIndex, Is.EqualTo(0));
        status.SelectWeapon(-5);
        Assert.That(status.WeaponIndex, Is.EqualTo(0));
        Object.DestroyImmediate(status.gameObject);
    }
}
