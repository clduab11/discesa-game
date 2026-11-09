using UnityEngine;

/// <summary>
/// Ground-bound walker enemy. Patrols a waypoint loop on the floor, telegraphs, then
/// fires a three-shot hitscan burst and repositions. No navmesh: waypoints are authored
/// inside the room volumes and it stays on the floor plane.
/// </summary>
public class Walker : MonoBehaviour
{
    public enum State { Patrol, Alert, Burst, Reposition }

    public float MaxHp = 80f;
    public float Hp { get; private set; }
    public bool IsDead => Hp <= 0f;

    public Transform target;
    public Vector3[] waypoints;
    public float patrolSpeed = 2.2f;
    public float sightRange = 32f;
    public int burstDamage = 6;
    public int burstShots = 3;
    public float burstInterval = 0.1f;

    public State CurrentState { get; private set; } = State.Patrol;
    public int ShotsFired { get; private set; }

    float _stateTime;
    float _settleTimer;
    float _shotTimer;
    int _shotsFired;
    int _waypoint;
    float _groundY = -5.3f;
    float _flash;
    Renderer _renderer;
    Color _baseColor;

    public void Build(Transform ship, Vector3[] patrolWaypoints, float groundY)
    {
        target = ship;
        waypoints = patrolWaypoints;
        _groundY = groundY;
        Hp = MaxHp;
        CurrentState = State.Patrol;
        _stateTime = 0f;
        _waypoint = 0;
        if (waypoints != null && waypoints.Length > 0)
        {
            var p = transform.position;
            p.y = _groundY;
            transform.position = p;
        }
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null && _renderer.sharedMaterial != null) _baseColor = _renderer.sharedMaterial.color;
    }

    Vector3 Eye => transform.position + Vector3.up * 1.15f;
    Vector3 Muzzle => transform.position + Vector3.up * 1.05f + transform.forward * 0.75f;

    void Update()
    {
        if (IsDead || target == null) return;

        float dt = Time.deltaTime;
        _stateTime += dt;

        if (_flash > 0f)
        {
            _flash -= dt;
            if (_flash <= 0f && _renderer != null) _renderer.material.color = _baseColor;
        }

        // Keep the walker planted on the floor plane.
        var pos = transform.position;
        if (Mathf.Abs(pos.y - _groundY) > 0.001f)
        {
            pos.y = _groundY;
            transform.position = pos;
        }

        switch (CurrentState)
        {
            case State.Patrol:
                Patrol(dt);
                if (CanSeePlayer()) Enter(State.Alert);
                break;

            case State.Alert:
                FaceTarget(dt, 6f);
                if (_stateTime >= 0.4f) { Enter(State.Burst); _shotsFired = 0; _shotTimer = 0f; }
                break;

            case State.Burst:
                FaceTarget(dt, 8f);
                _shotTimer -= dt;
                if (_shotsFired < burstShots && _shotTimer <= 0f)
                {
                    FireHitScan();
                    _shotsFired++;
                    _shotTimer = burstInterval;
                }
                if (_shotsFired >= burstShots) Enter(State.Reposition);
                break;

            case State.Reposition:
                if (_stateTime >= 1.4f) Enter(State.Patrol);
                break;
        }
    }

    void Enter(State next)
    {
        CurrentState = next;
        _stateTime = 0f;
        if (next == State.Reposition)
        {
            if (waypoints != null && waypoints.Length > 0)
                _waypoint = (_waypoint + 1) % waypoints.Length;
        }
    }

    void Patrol(float dt)
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Vector3 goal = waypoints[_waypoint];
        goal.y = _groundY;
        Vector3 to = goal - transform.position;
        to.y = 0f;
        float distance = to.magnitude;

        if (distance < 0.6f)
        {
            _waypoint = (_waypoint + 1) % waypoints.Length;
            return;
        }

        Vector3 dir = to / distance;
        transform.position += dir * Mathf.Min(patrolSpeed * dt, distance);

        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir, Vector3.up), 1f - Mathf.Exp(-5f * dt));
    }

    void FaceTarget(float dt, float rate)
    {
        Vector3 to = target.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(to.normalized, Vector3.up), 1f - Mathf.Exp(-rate * dt));
    }

    public bool CanSeePlayer()
    {
        if (target == null) return false;

        Vector3 origin = Muzzle;
        Vector3 to = target.position - origin;
        float distance = to.magnitude;
        if (distance > sightRange) return false;

        RaycastHit hit;
        if (!Physics.Raycast(origin, to / distance, out hit, distance, ~0, QueryTriggerInteraction.Collide))
            return false;

        if (hit.collider == null) return false;
        if (hit.collider.GetComponentInParent<Walker>() == this) return true;
        return hit.collider.GetComponentInParent<ShipController>() != null;
    }

    void FireHitScan()
    {
        ShotsFired++;
        Vector3 origin = Muzzle;
        Vector3 aim = target.position + Random.insideUnitSphere * 0.35f;
        Vector3 dir = (aim - origin).normalized;
        float distance = sightRange;

        RaycastHit hit;
        Vector3 end;
        if (Physics.Raycast(origin, dir, out hit, distance, ~0, QueryTriggerInteraction.Collide))
        {
            end = hit.point;
            if (hit.collider != null && hit.collider.GetComponentInParent<ShipController>() != null)
            {
                if (GameManager.Instance != null) GameManager.Instance.DamageShip(burstDamage);
                Vfx.Impact(hit.point, DiscesaMaterials.ProjectileRed, 3);
            }
        }
        else
        {
            end = origin + dir * distance;
        }

        Vfx.Beam(origin, end, DiscesaMaterials.Trail);
    }

    public void Damage(int amount)
    {
        if (IsDead || amount <= 0) return;
        Hp = Mathf.Max(0f, Hp - amount);

        if (_renderer != null)
            _renderer.material.color = Color.Lerp(_baseColor, Color.white, 0.8f);
        _flash = 0.08f;

        if (Hp <= 0f) Die();
    }

    void Die()
    {
        Vfx.Impact(transform.position + Vector3.up * 0.8f, DiscesaMaterials.WalkerBody, 10);
        transform.rotation = Quaternion.Euler(70f, transform.eulerAngles.y, 0f);
        enabled = false;
    }

    public void ResetWalker()
    {
        Hp = MaxHp;
        enabled = true;
        CurrentState = State.Patrol;
        _stateTime = 0f;
        _shotsFired = 0;
        _waypoint = 0;
        ShotsFired = 0;
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = new Vector3(waypoints[0].x, _groundY, waypoints[0].z);
        }
        if (_renderer != null) _renderer.material.color = _baseColor;
    }
}
