using UnityEngine;

/// <summary>
/// Fast projectile. Resolves its target by component rather than by tag, so the project
/// needs no custom tag definitions. Despawns on first hit.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public int damage = 15;
    public float lifetime = 4f;

    Rigidbody _body;

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.useGravity = false;
        _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    /// <summary>Runtime factory; slice 1 ships no prefab assets.</summary>
    public static Projectile Spawn(Vector3 position, Quaternion rotation, Vector3 velocity,
                                   Material material, int damage)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Projectile";
        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = Vector3.one * 0.25f;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        var projectile = go.AddComponent<Projectile>();
        projectile.damage = damage;
        projectile._body.linearVelocity = velocity;

        Destroy(go, projectile.lifetime);
        return projectile;
    }

    void OnTriggerEnter(Collider other)
    {
        // The ship hull is a trigger collider (see GameManager.BuildShip), so hull hits
        // arrive here instead of through OnCollisionEnter.
        if (other.GetComponentInParent<ShipController>() == null) return;

        if (GameManager.Instance != null) GameManager.Instance.DamageShip(damage);
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        var turret = collision.collider.GetComponentInParent<Turret>();
        if (turret != null)
        {
            turret.Damage(damage);
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
