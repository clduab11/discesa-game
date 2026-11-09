using UnityEngine;

/// <summary>
/// Pooled projectile. Movement and hit resolution are done manually with a sphere cast,
/// which keeps tunnelling impossible at 70 m/s and costs one query per active bolt.
///
/// Damage routing:
///   player bolts -> turret / walker / core, splash on heavy
///   enemy bolts  -> ship shields
/// The projectile never owns a Rigidbody or collider, so nothing can push the ship.
/// </summary>
public class Projectile : MonoBehaviour
{
    public bool IsActive { get; private set; }
    public int Damage { get; private set; }
    public bool FromPlayer { get; private set; }
    public float SplashRadius { get; private set; }
    public float Radius { get; private set; }

    Vector3 _velocity;
    float _life;
    float _lifetime;
    Material _material;
    TrailRenderer _trail;
    ProjectilePool _owner;

    public void Bind(ProjectilePool owner, Renderer renderer, TrailRenderer trail)
    {
        _owner = owner;
        _trail = trail;
        _material = renderer != null ? renderer.sharedMaterial : null;
    }

    public void Launch(Vector3 position, Vector3 velocity, int damage, bool fromPlayer,
                       float lifetime, float radius, float splashRadius, Material material)
    {
        transform.position = position;
        _velocity = velocity;
        Damage = damage;
        FromPlayer = fromPlayer;
        _lifetime = lifetime;
        _life = 0f;
        Radius = Mathf.Max(radius, 0.05f);
        SplashRadius = splashRadius;
        IsActive = true;
        _material = material;

        transform.localScale = Vector3.one * (Radius * 2f);

        var renderer = GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        if (_trail != null)
        {
            _trail.sharedMaterial = DiscesaMaterials.Trail;
            _trail.startColor = material != null ? WithAlpha(material.color, 0.85f) : Color.white;
            _trail.endColor = new Color(1f, 1f, 1f, 0f);
            _trail.Clear();
            _trail.emitting = true;
        }

        gameObject.SetActive(true);
    }

    public void Release()
    {
        if (!IsActive) return;
        IsActive = false;
        if (_trail != null)
        {
            _trail.emitting = false;
            _trail.Clear();
        }
        gameObject.SetActive(false);
        if (_owner != null) _owner.Return(this);
    }

    void Update()
    {
        if (!IsActive) return;

        float dt = Time.deltaTime;
        float distance = _velocity.magnitude * dt;
        if (distance <= 0.0001f) return;

        Vector3 dir = _velocity / _velocity.magnitude;
        float castRadius = Mathf.Max(Radius, 0.05f);

        RaycastHit hit;
        bool blocked = Physics.SphereCast(transform.position, castRadius, dir, out hit, distance,
            ~0, QueryTriggerInteraction.Collide);

        if (blocked)
        {
            transform.position = hit.point;
            ResolveHit(hit.collider, hit.point);
            return;
        }

        transform.position += dir * distance;

                _life += dt;
        if (_lifetime > 0f && _life >= _lifetime) Release();
    }

    void ResolveHit
(Collider col, Vector3 point)
    {
        if (col == null)
        {
            Impact(point);
            return;
        }

        var core = col.GetComponentInParent<CoreObjective>();
        var turret = col.GetComponentInParent<Turret>();
        var walker = col.GetComponentInParent<Walker>();
        var ship = col.GetComponentInParent<ShipController>();

        if (FromPlayer)
        {
            if (core != null)
            {
                core.Damage(Damage);
                Impact(point);
                return;
            }
            if (turret != null && !turret.IsDead)
            {
                turret.Damage(Damage);
                Impact(point);
                ApplySplash(point);
                return;
            }
            if (walker != null && !walker.IsDead)
            {
                walker.Damage(Damage);
                Impact(point);
                ApplySplash(point);
                return;
            }
            if (ship != null)
            {
                // Owner hull: never damage ourselves; let the bolt die quietly.
                Impact(point);
                return;
            }

            Impact(point);
            ApplySplash(point);
            return;
        }

        // Enemy bolt
        if (ship != null)
        {
            if (GameManager.Instance != null) GameManager.Instance.DamageShip(Damage);
            Impact(point);
            return;
        }

        Impact(point);
    }

    void ApplySplash(Vector3 centre)
    {
        if (SplashRadius <= 0f) return;

        var hits = Physics.OverlapSphere(centre, SplashRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var c in hits)
        {
            if (c == null) continue;
            var turret = c.GetComponentInParent<Turret>();
            if (turret != null && !turret.IsDead)
            {
                turret.Damage(Mathf.RoundToInt(Damage * 0.5f));
                continue;
            }
            var walker = c.GetComponentInParent<Walker>();
            if (walker != null && !walker.IsDead)
            {
                walker.Damage(Mathf.RoundToInt(Damage * 0.5f));
                continue;
            }
            var core = c.GetComponentInParent<CoreObjective>();
            if (core != null) core.Damage(Mathf.RoundToInt(Damage * 0.5f));
            // The ship is deliberately never damaged by splash, player or enemy.
        }
    }

    void Impact(Vector3 point)
    {
        Vfx.Impact(point, _material != null ? _material : DiscesaMaterials.ProjectileGreen, 4);
        Release();
    }

    static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
}
