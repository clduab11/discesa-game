using UnityEngine;

/// <summary>
/// Fires the selected weapon. Costs, cooldowns and pellet counts come from
/// <see cref="WeaponDefinition"/>; energy and missiles come from <see cref="ShipStatus"/>.
/// No allocation per shot: bolts come from the pooled <see cref="ProjectilePool"/>.
/// </summary>
public class WeaponSystem : MonoBehaviour
{
    public ShipStatus status;
    public ProjectilePool pool;
    public ShipController ship;
    public Transform muzzle;

    readonly float[] _cooldowns = new float[WeaponDefinition.Count];

    public int ActiveIndex => status != null ? status.WeaponIndex : 0;
    public WeaponDefinition Active => WeaponDefinition.Get(ActiveIndex);

    public void Build(ShipController shipController, ShipStatus shipStatus, ProjectilePool projectilePool)
    {
        ship = shipController;
        status = shipStatus;
        pool = projectilePool;
        for (int i = 0; i < _cooldowns.Length; i++) _cooldowns[i] = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _cooldowns.Length; i++)
            if (_cooldowns[i] > 0f) _cooldowns[i] = Mathf.Max(0f, _cooldowns[i] - dt);
    }

    public float CooldownRemaining(int index) =>
        _cooldowns[Mathf.Clamp(index, 0, _cooldowns.Length - 1)];

    public void Select(int index)
    {
        if (status != null) status.SelectWeapon(index);
    }

    public void Cycle()
    {
        if (status != null) status.CycleWeapon();
    }

    Vector3 MuzzlePosition
    {
        get
        {
            if (muzzle != null) return muzzle.position;
            if (ship != null) return ship.transform.position + ship.transform.forward * 1.3f;
            return transform.position;
        }
    }

    Vector3 ShipForward => ship != null ? ship.transform.forward : transform.forward;
    Vector3 ShipRight => ship != null ? ship.transform.right : transform.right;
    Vector3 ShipUp => ship != null ? ship.transform.up : transform.up;

    public bool TryFire()
    {
        if (status == null || pool == null || ship == null || ship.IsDead) return false;

        var def = Active;
        if (_cooldowns[status.WeaponIndex] > 0f) return false;
        if (def.UsesEnergy && !status.TryConsumeEnergy(def.EnergyCost)) return false;
        if (def.UsesAmmo && !status.TryConsumeMissile(def.AmmoCost)) return false;

        _cooldowns[status.WeaponIndex] = def.Cooldown;

        Vector3 origin = MuzzlePosition;
        Vector3 baseDir = ShipForward;
        Vector3 inherited = ship.velocity * 0.5f;

        int pellets = Mathf.Max(1, def.Pellets);
        for (int i = 0; i < pellets; i++)
        {
            Vector3 dir = baseDir;
            if (def.SpreadDegrees > 0f)
            {
                float yaw = Random.Range(-def.SpreadDegrees * 0.5f, def.SpreadDegrees * 0.5f);
                float pitch = Random.Range(-def.SpreadDegrees * 0.5f, def.SpreadDegrees * 0.5f);
                dir = (Quaternion.AngleAxis(yaw, ShipUp) * Quaternion.AngleAxis(pitch, ShipRight) * baseDir).normalized;
            }

            Vector3 velocity = dir * def.Speed + inherited * 0.5f;
            pool.Spawn(origin + dir * (i * 0.05f), velocity, def.Damage, true,
                def.Lifetime, def.BoltRadius, def.SplashRadius, BoltMaterial(def));
        }

        Vfx.Muzzle(muzzle != null ? muzzle : ship.transform, BoltMaterial(def));
        return true;
    }

    static Material BoltMaterial(WeaponDefinition def)
    {
        if (def.Slot == WeaponSlot.Secondary) return DiscesaMaterials.ProjectileAmber;
        return def.Id == "spread" ? DiscesaMaterials.ProjectileGreen : DiscesaMaterials.ProjectileGreen;
    }
}
