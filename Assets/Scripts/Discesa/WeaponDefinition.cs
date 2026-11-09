using UnityEngine;

public enum WeaponSlot
{
    Primary,
    Secondary
}

/// <summary>
/// Original weapon tuning for the slice. Names are our own (pulse / spread / heavy);
/// no reference weapon names, art or values are carried over.
/// </summary>
public sealed class WeaponDefinition
{
    public string Id { get; }
    public string DisplayName { get; }
    public WeaponSlot Slot { get; }
    public float EnergyCost { get; }
    public int AmmoCost { get; }
    public float Cooldown { get; }
    public int Pellets { get; }
    public float SpreadDegrees { get; }
    public float Speed { get; }
    public int Damage { get; }
    public float SplashRadius { get; }
    public float Lifetime { get; }
    public float BoltRadius { get; }
    public Color BoltColor { get; }

    WeaponDefinition(string id, string displayName, WeaponSlot slot, float energyCost, int ammoCost,
        float cooldown, int pellets, float spreadDegrees, float speed, int damage,
        float splashRadius, float lifetime, float boltRadius, Color boltColor)
    {
        Id = id;
        DisplayName = displayName;
        Slot = slot;
        EnergyCost = energyCost;
        AmmoCost = ammoCost;
        Cooldown = cooldown;
        Pellets = pellets;
        SpreadDegrees = spreadDegrees;
        Speed = speed;
        Damage = damage;
        SplashRadius = splashRadius;
        Lifetime = lifetime;
        BoltRadius = boltRadius;
        BoltColor = boltColor;
    }

    public static readonly WeaponDefinition Pulse = new WeaponDefinition(
        "pulse", "PULSE", WeaponSlot.Primary,
        energyCost: 2f, ammoCost: 0, cooldown: 0.12f, pellets: 1, spreadDegrees: 0f,
        speed: 70f, damage: 10, splashRadius: 0f, lifetime: 2.5f, boltRadius: 0.10f,
        boltColor: new Color(0.45f, 1f, 0.55f));

    public static readonly WeaponDefinition Spread = new WeaponDefinition(
        "spread", "SPREAD", WeaponSlot.Primary,
        energyCost: 6f, ammoCost: 0, cooldown: 0.35f, pellets: 3, spreadDegrees: 6f,
        speed: 55f, damage: 6, splashRadius: 0f, lifetime: 2.5f, boltRadius: 0.09f,
        boltColor: new Color(0.45f, 0.85f, 1f));

    public static readonly WeaponDefinition Heavy = new WeaponDefinition(
        "heavy", "HEAVY", WeaponSlot.Secondary,
        energyCost: 0f, ammoCost: 1, cooldown: 1.00f, pellets: 1, spreadDegrees: 0f,
        speed: 25f, damage: 40, splashRadius: 1.5f, lifetime: 5f, boltRadius: 0.16f,
        boltColor: new Color(1f, 0.62f, 0.20f));

    public static readonly WeaponDefinition[] All = { Pulse, Spread, Heavy };

    public static int Count => All.Length;

    public static WeaponDefinition Get(int index)
    {
        index = Mathf.Clamp(index, 0, All.Length - 1);
        return All[index];
    }

    public bool UsesEnergy => EnergyCost > 0f;
    public bool UsesAmmo => AmmoCost > 0;
}
