using UnityEngine;

/// <summary>
/// Slow tracking turret. Fires a visible projectile at the ship.
/// Target resolution uses components rather than tags so no custom tags are needed.
/// </summary>
public class Turret : MonoBehaviour
{
    public int maxHp = 50;
    public int hp { get; private set; }
    public float fireInterval = 1.2f;
    public float projectileSpeed = 42f;
    public int projectileDamage = 12;
    public float turnRate = 3f;

    public Transform muzzle;
    public Transform target;

    float _cooldown;
    Renderer _renderer;
    Color _baseColor;

    public bool IsDead => hp <= 0;

    void Awake()
    {
        hp = maxHp;
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _baseColor = _renderer.material.color;
    }

    void Update()
    {
        if (IsDead || target == null) return;

        Vector3 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(toTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                look, 1f - Mathf.Exp(-turnRate * Time.deltaTime));
        }

        _cooldown -= Time.deltaTime;
        if (_cooldown <= 0f)
        {
            Fire();
            _cooldown = fireInterval;
        }
    }

    void Fire()
    {
        Vector3 origin = muzzle != null ? muzzle.position : transform.position;
        Vector3 dir = muzzle != null ? muzzle.forward : transform.forward;

        Projectile.Spawn(origin, Quaternion.LookRotation(dir),
            dir * projectileSpeed, DiscesaMaterials.ProjectileRed, projectileDamage);
    }

    public void Damage(int amount)
    {
        if (IsDead) return;
        hp = Mathf.Max(hp - amount, 0);
        if (_renderer != null)
            _renderer.material.color = Color.Lerp(_baseColor, Color.white, 0.7f);

        if (hp <= 0) Die();
        else Invoke(nameof(RestoreColor), 0.08f);
    }

    void RestoreColor()
    {
        if (_renderer != null) _renderer.material.color = _baseColor;
    }

    void Die()
    {
        if (_renderer != null) _renderer.material.color = Color.black;
        enabled = false;
        if (GameManager.Instance != null) GameManager.Instance.OnTurretKilled();
    }

    public void ResetTurret()
    {
        hp = maxHp;
        enabled = true;
        _cooldown = 0.75f;
        if (_renderer != null) _renderer.material.color = _baseColor;
    }
}
