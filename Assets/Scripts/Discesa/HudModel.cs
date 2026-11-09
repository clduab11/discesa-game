using UnityEngine;

/// <summary>
/// Immutable, already-clamped snapshot of everything the cockpit HUD draws.
/// Pure data: no UnityEngine.UI, no scene references, so the layout can be tested
/// without entering Play mode. The UI reads this; nothing writes back into the model.
/// </summary>
public readonly struct HudModel
{
    public readonly float Shield;
    public readonly float Energy;
    public readonly int Missiles;
    public readonly int WeaponIndex;
    public readonly float CoreHp;
    public readonly float CoreMaxHp;
    public readonly bool IsWin;
    public readonly bool IsLose;
    public readonly float DamageFlash; // 0..1, decays in the presenter

    public HudModel(float shield, float energy, int missiles, int weaponIndex,
                    float coreHp, float coreMaxHp, bool isWin, bool isLose, float damageFlash = 0f)
    {
        Shield = Mathf.Clamp(shield, 0f, ShipStatus.MaxShield);
        Energy = Mathf.Clamp(energy, 0f, ShipStatus.MaxEnergy);
        Missiles = Mathf.Clamp(missiles, 0, ShipStatus.MaxMissiles);
        WeaponIndex = Mathf.Clamp(weaponIndex, 0, WeaponDefinition.Count - 1);
        CoreMaxHp = Mathf.Max(coreMaxHp, 0f);
        CoreHp = Mathf.Clamp(coreHp, 0f, CoreMaxHp);
        IsWin = isWin;
        IsLose = isLose;
        DamageFlash = Mathf.Clamp01(damageFlash);
    }

    public static HudModel From(ShipStatus status, CoreObjective core, bool isWin, bool isLose, float damageFlash = 0f)
    {
        if (status == null)
            return new HudModel(0f, 0f, 0, 0, 0f, 0f, isWin, isLose, damageFlash);

        return new HudModel(
            status.Shield, status.Energy, status.Missiles, status.WeaponIndex,
            core != null ? core.Hp : 0f,
            core != null ? core.MaxHp : 0f,
            isWin, isLose, damageFlash);
    }

    public float ShieldFraction => Shield / ShipStatus.MaxShield;
    public float EnergyFraction => Energy / ShipStatus.MaxEnergy;
    public float CoreFraction => CoreMaxHp <= 0f ? 0f : CoreHp / CoreMaxHp;

    public WeaponDefinition Weapon => WeaponDefinition.Get(WeaponIndex);

    public string WeaponName => Weapon.DisplayName;

    public string WeaponSlotText =>
        Weapon.Slot == WeaponSlot.Primary ? "PRIMARY" : "SECONDARY";

    public string ShieldText => ((int)Mathf.Ceil(Shield)).ToString("00");
    public string EnergyText => ((int)Mathf.Ceil(Energy)).ToString("00");

    /// <summary>Bottom-right line: heavy missiles always, plus the active weapon's cost.</summary>
    public string MissileText => $"MISSILES {Missiles:00}";

    public string CostText =>
        Weapon.UsesAmmo ? $"AMMO {Weapon.AmmoCost}" :
        $"ENERGY {Weapon.EnergyCost:0}";

    public string ObjectiveText =>
        IsWin ? "CORE DESTROYED" :
        IsLose ? "SHIP DESTROYED" :
        CoreMaxHp > 0f ? $"CORE {((int)Mathf.Ceil(CoreHp)):000}" : "CORE ----";

    public string BannerText =>
        IsWin ? "CORE DESTROYED\nPress R to restart" :
        IsLose ? "SHIP DESTROYED\nPress R to restart" : string.Empty;

    public bool LowShield => Shield > 0f && Shield <= 25f;
    public bool LowEnergy => Energy > 0f && Energy <= 20f;

    public static HudModel Empty => new HudModel(0f, 0f, 0, 0, 0f, 0f, false, false);
}
