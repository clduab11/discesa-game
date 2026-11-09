using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pool of projectiles. Pre-creates a small fleet with trail renderers so firing never
/// instantiates or destroys objects. Pool growth is capped; when the cap is reached the
/// oldest bolt is recycled.
/// </summary>
public class ProjectilePool : MonoBehaviour
{
    const int InitialSize = 24;
    const int MaxSize = 64;

    readonly List<Projectile> _all = new List<Projectile>();
    readonly Stack<Projectile> _free = new Stack<Projectile>();
    int _active;

    public int ActiveCount => _active;
    public int TotalCount => _all.Count;
    public int SpawnCount { get; private set; }

    public void Build()
    {
        for (int i = 0; i < InitialSize; i++) CreateOne();
    }

    Projectile CreateOne()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Projectile";
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * 0.2f;

        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = DiscesaMaterials.ProjectileGreen;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.28f;
        trail.widthMultiplier = 0.22f;
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 2;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.material = DiscesaMaterials.Trail;
        trail.emitting = false;
        trail.startColor = new Color(0.5f, 1f, 0.55f, 0.9f);
        trail.endColor = new Color(1f, 1f, 1f, 0f);

        var projectile = go.AddComponent<Projectile>();
        projectile.Bind(this, renderer, trail);
        go.SetActive(false);

        _all.Add(projectile);
        _free.Push(projectile);
        return projectile;
    }

    public Projectile Spawn(Vector3 position, Vector3 velocity, int damage, bool fromPlayer,
                            float lifetime, float radius, float splashRadius, Material material)
    {
        Projectile p;
        if (_free.Count > 0)
        {
            p = _free.Pop();
        }
        else if (_all.Count < MaxSize)
        {
            p = CreateOne();
            _free.Pop(); // take the one just created
        }
        else
        {
            // Recycle the oldest active bolt.
            p = _all[0];
            if (p.IsActive) p.Release();
            if (!_free.Contains(p)) _free.Push(p);
            p = _free.Pop();
        }

        _active++;
        SpawnCount++;
        p.Launch(position, velocity, damage, fromPlayer, lifetime, radius, splashRadius, material);
        return p;
    }

    public void Return(Projectile p)
    {
        if (p == null) return;
        _active = Mathf.Max(0, _active - 1);
        if (!_free.Contains(p)) _free.Push(p);
    }

    public void ReleaseAll()
    {
        foreach (var p in _all)
            if (p != null && p.IsActive) p.Release();
        _active = 0;
    }

    /// <summary>Test helper: find an active projectile travelling roughly along a direction.</summary>
    public Projectile FindActive()
    {
        foreach (var p in _all)
            if (p != null && p.IsActive) return p;
        return null;
    }
}
