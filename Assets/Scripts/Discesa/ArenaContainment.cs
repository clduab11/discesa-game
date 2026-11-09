using UnityEngine;

/// <summary>
/// Union-of-volumes containment for the ship. Walls remain authoritative; this is a
/// safety net for the case where a swept move or a teleport leaves the level.
///
/// When the ship is outside every volume for more than <see cref="escapeGrace"/> it is
/// returned to the last position that was safely inside. The last-safe point is updated
/// on a timer so it never sits inside a wall.
/// </summary>
public sealed class ArenaContainment
{
    public readonly ArenaLayout Layout;
    public float escapeGrace = 0.5f;
    public float safeUpdateInterval = 0.25f;

    readonly Transform _body;
    readonly float _radius;
    Vector3 _lastSafe;
    float _outsideTime;
    float _safeTimer;

    public float OutsideTime => _outsideTime;
    public int RespawnCount { get; private set; }

    public ArenaContainment(ArenaLayout layout, Transform body, float radius = 0.55f)
    {
        Layout = layout;
        _body = body;
        _radius = radius;
        _lastSafe = body != null ? body.position : Vector3.zero;
        _outsideTime = 0f;
        _safeTimer = 0f;
    }

    public Vector3 LastSafe => _lastSafe;

    /// <summary>Escape detection uses the actual union, never an expanded exterior.</summary>
    public bool IsInside(Vector3 point) => Layout.Contains(point);

    bool IsSafe(Vector3 point)
    {
        Quaternion rotation = _body != null ? _body.rotation : Quaternion.identity;
        // Check the rotated hull against the union, not an inset of each room:
        // individual insets would incorrectly seal shared doorway planes.
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
            if (!IsInside(point + rotation * (new Vector3(x, y, z) * _radius))) return false;
        foreach (var other in Physics.OverlapBox(point, Vector3.one * _radius, rotation,
            ~0, QueryTriggerInteraction.Ignore))
        {
            if (_body == null || !other.transform.IsChildOf(_body)) return false;
        }
        return true;
    }

    public void Reset(Vector3 position)
    {
        if (IsSafe(position)) _lastSafe = position;
        _outsideTime = 0f;
        _safeTimer = 0f;
        RespawnCount = 0;
    }

    /// <summary>
    /// Call once per physics substep with the new position. Returns true when a respawn
    /// was performed this step.
    /// </summary>
    public bool Tick(Vector3 position, float dt)
    {
        if (_body == null || dt < 0f) return false;

        if (IsInside(position))
        {
            _outsideTime = 0f;
            if (!IsSafe(position)) { _safeTimer = 0f; return false; }
            _safeTimer += dt;
            if (_safeTimer >= safeUpdateInterval)
            {
                _safeTimer = 0f;
                _lastSafe = position;
            }
            return false;
        }

        _safeTimer = 0f;
        _outsideTime += dt;
        if (_outsideTime < escapeGrace) return false;

        _body.position = _lastSafe;
        _outsideTime = 0f;
        RespawnCount++;
        return true;
    }
}
