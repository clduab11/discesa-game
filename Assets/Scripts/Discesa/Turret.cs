using UnityEngine;

/// <summary>
/// Stationary arc turret guarding the core. Original finite-state machine:
///   IDLE -> TRACK -> TELEGRAPH -> FIRE -> RELOAD
/// It needs field-of-view and line of sight to acquire, telegraphs before firing, and
/// leads the player half of the time with a small random spread. It never fires through
/// walls. Destroying it removes the guard but does not win the run.
/// </summary>
public class Turret : MonoBehaviour
{
    public enum State { Idle, Track, Telegraph, Fire, Reload }

    public float MaxHp = 60f;
    public float Hp { get; private set; }
    public bool IsDead => Hp <= 0f;

    public float Fov = 120f;
    public float Range = 46f;
    public float TrackTime = 0.55f;
    public float TelegraphTime = 0.30f;
    public float ReloadTime = 1.2f;
    public float projectileSpeed = 42f;
    public int projectileDamage = 12;

    public Transform target;
    public Transform head;
    public Transform muzzle;
    public Transform muzzleGlow;
    public ProjectilePool pool;

    public State CurrentState { get; private set; } = State.Idle;
    public float StateTime => _stateTime;

    float _stateTime;
    float _scanAngle;
    bool _leadNext = true;
    float _flash;
    Renderer _renderer;
    Color _baseColor;

    public void Build(Transform targetShip, Transform headPart, Transform muzzlePoint, ProjectilePool projectilePool)
    {
        target = targetShip;
        head = headPart != null ? headPart : transform;
        muzzle = muzzlePoint;
        pool = projectilePool;
        Hp = MaxHp;
        CurrentState = State.Idle;
        _stateTime = 0f;
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _baseColor = _renderer.sharedMaterial != null ? _renderer.sharedMaterial.color : Color.white;
        if (muzzleGlow != null) muzzleGlow.localScale = Vector3.zero;
    }

    Vector3 Eye => muzzle != null ? muzzle.position : transform.position + Vector3.up * 0.6f;

    Vector3 Forward => head != null ? head.forward : transform.forward;

    void Update()
    {
        if (IsDead || target == null) return;

        float dt = Time.deltaTime;
        _stateTime += dt;

        if (_flash > 0f)
        {
            _flash -= dt;
            if (_flash <= 0f && _renderer != null)
                _renderer.material.color = _baseColor;
        }

        switch (CurrentState)
        {
            case State.Idle:
                Scan(dt);
                // Acquire against the mount's forward axis so the scan sweep never delays
                // a clearly visible target for a whole rotation.
                if (CanSeeDirection(transform.forward)) Enter(State.Track);
                break;

            case State.Track:
                AimAt(TargetPoint(false), dt, 4.5f);
                if (!CanSee()) Enter(State.Idle);
                else if (_stateTime >= TrackTime) Enter(State.Telegraph);
                break;

            case State.Telegraph:
                AimAt(TargetPoint(true), dt, 6f);
                SetGlow(_stateTime / TelegraphTime);
                if (!CanSee()) Enter(State.Idle);
                else if (_stateTime >= TelegraphTime) Fire();
                break;

            case State.Fire:
                Enter(State.Reload);
                break;

            case State.Reload:
                SetGlow(0f);
                if (_stateTime >= ReloadTime)
                    Enter(CanSee() ? State.Track : State.Idle);
                break;
        }
    }

    void Enter(State next)
    {
        CurrentState = next;
        _stateTime = 0f;
        if (next == State.Telegraph) SetGlow(0f);
        if (next == State.Idle && muzzleGlow != null) muzzleGlow.localScale = Vector3.zero;
    }

    void Scan(float dt)
    {
        _scanAngle += dt * 22f;
        if (_scanAngle > 55f) _scanAngle = -55f;
        if (head != null)
        {
            Vector3 baseDir = transform.forward;
            Quaternion scan = Quaternion.AngleAxis(_scanAngle, Vector3.up) * Quaternion.LookRotation(baseDir, Vector3.up);
            head.rotation = Quaternion.Slerp(head.rotation, scan, 1f - Mathf.Exp(-3f * dt));
        }
    }

    Vector3 TargetPoint(bool useLead)
    {
        if (target == null) return transform.position;
        Vector3 p = target.position;
        if (useLead && _leadNext)
        {
            var ship = target.GetComponent<ShipController>();
            Vector3 velocity = ship != null ? ship.velocity : Vector3.zero;
            float distance = Vector3.Distance(Eye, p);
            float travel = distance / Mathf.Max(projectileSpeed, 1f);
            p += velocity * travel * 0.5f; // 50% lead
        }
        return p;
    }

    void AimAt(Vector3 point, float dt, float rate)
    {
        Vector3 to = point - Eye;
        if (to.sqrMagnitude < 0.01f) return;
        if (head != null) head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(to), 1f - Mathf.Exp(-rate * dt));
    }

    public bool CanSee() => CanSeeDirection(Forward);

    public bool CanSeeDirection(Vector3 direction)
    {
        if (target == null || muzzle == null) return false;

        Vector3 to = target.position - Eye;
        float distance = to.magnitude;
        if (distance > Range || distance < 0.5f) return false;
        if (Vector3.Angle(direction, to) > Fov * 0.5f) return false;

        RaycastHit hit;
        if (Physics.Raycast(Eye, to / distance, out hit, Mathf.Max(distance - 0.6f, 0.1f), ~0, QueryTriggerInteraction.Ignore))
            return false; // something solid blocks the line

        return true;
    }

    void Fire()
    {
        if (pool == null || muzzle == null) return;

        Vector3 point = TargetPoint(true);
        Vector3 dir = (point - muzzle.position).normalized;
        dir = Quaternion.AngleAxis(Random.Range(-2f, 2f), Vector3.up) *
              Quaternion.AngleAxis(Random.Range(-2f, 2f), Vector3.right) * dir;

        pool.Spawn(muzzle.position, dir * projectileSpeed, projectileDamage, false,
            4.5f, 0.12f, 0f, DiscesaMaterials.ProjectileRed);

        Vfx.Muzzle(muzzle, DiscesaMaterials.ProjectileRed);
        _leadNext = !_leadNext;
        Enter(State.Fire);
    }

    void SetGlow(float t)
    {
        if (muzzleGlow == null) return;
        muzzleGlow.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.62f, Mathf.Clamp01(t));
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
        if (_renderer != null) _renderer.material.color = Color.black;
        if (muzzleGlow != null) muzzleGlow.localScale = Vector3.zero;
        Vfx.Impact(transform.position + Vector3.up * 0.5f, DiscesaMaterials.EmergencyPanel, 12);
        CurrentState = State.Idle;
        enabled = false;
    }

    public void DisableGuard()
    {
        if (muzzleGlow != null) muzzleGlow.localScale = Vector3.zero;
        if (_renderer != null) _renderer.material.color = new Color(0.12f, 0.12f, 0.13f);
        CurrentState = State.Idle;
        enabled = false;
    }

    public void ResetTurret()
    {
        Hp = MaxHp;
        enabled = true;
        CurrentState = State.Idle;
        _stateTime = 0f;
        _scanAngle = 0f;
        if (muzzleGlow != null) muzzleGlow.localScale = Vector3.zero;
        if (_renderer != null) _renderer.material.color = _baseColor;
    }
}
