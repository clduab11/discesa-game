using UnityEngine;

/// <summary>
/// Floating resource pickup. Cell restores 25 energy; missile adds 4 heavy rounds.
/// Bobs, spins, glows, and respawns after twenty seconds. Collection is a proximity
/// check against the ship so it works with the ship's trigger hull and in tests.
/// </summary>
public class Pickup : MonoBehaviour
{
    public enum Kind { Cell, Missile }

    public Kind kind = Kind.Cell;
    public ShipStatus status;
    public Transform ship;
    public float respawnSeconds = 20f;
    public float collectRadius = 1.4f;
    public string Message { get; private set; }

    bool _available = true;
    float _timer;
    Vector3 _home;
    float _phase;
    Transform _visual;
    Renderer[] _renderers;
    Light _light;
    Collider[] _colliders;

    public bool IsAvailable => _available;

    public void Build(Kind pickupKind, ShipStatus shipStatus, Transform shipTransform,
                      Vector3 home, Transform visualRoot, Light glowLight)
    {
        kind = pickupKind;
        status = shipStatus;
        ship = shipTransform;
        _home = home;
        transform.position = home;
        _visual = visualRoot != null ? visualRoot : transform;
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _light = glowLight;
        _phase = kind == Kind.Cell ? 0.3f : 1.1f;
        _available = true;
        _timer = 0f;
        SetVisible(true);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (_available)
        {
            _phase += dt;
            if (_visual != null)
            {
                _visual.localPosition = Vector3.up * (Mathf.Sin(_phase * 1.8f) * 0.18f);
                _visual.Rotate(Vector3.up, 90f * dt, Space.World);
            }

            if (ship != null)
            {
                var shipPos = ship.position;
                if ((shipPos - transform.position).sqrMagnitude <= collectRadius * collectRadius)
                    Collect();
            }
            return;
        }

        _timer -= dt;
        if (_timer <= 0f) Respawn();
    }

    void Collect()
    {
        if (status == null) return;

        if (kind == Kind.Cell)
        {
            float added = status.AddEnergy(25f);
            Message = added > 0f ? $"+{Mathf.RoundToInt(added)} ENERGY" : "ENERGY FULL";
        }
        else
        {
            int added = status.AddMissiles(4);
            Message = added > 0 ? $"+{added} MISSILES" : "MISSILES FULL";
        }

        Vfx.Impact(transform.position, kind == Kind.Cell ? DiscesaMaterials.PickupCell : DiscesaMaterials.PickupMissile, 6);
        if (GameManager.Instance != null) GameManager.Instance.HudMessage(Message);
        SetVisible(false);
        _available = false;
        _timer = respawnSeconds;
    }

    public void Respawn()
    {
        _available = true;
        _timer = 0f;
        transform.position = _home;
        if (_visual != null) _visual.localPosition = Vector3.zero;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        if (_renderers != null) foreach (var r in _renderers) if (r != null) r.enabled = visible;
        if (_colliders != null) foreach (var c in _colliders) if (c != null) c.enabled = visible;
        if (_light != null) _light.enabled = visible;
    }

    public void ResetPickup()
    {
        SetVisible(true);
        Respawn();
    }

    public float TimeUntilRespawn => _available ? 0f : Mathf.Max(_timer, 0f);
}
