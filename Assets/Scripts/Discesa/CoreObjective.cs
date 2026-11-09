using System;
using UnityEngine;

/// <summary>
/// Core reactor objective. Floating, rotating, destroyed by player fire.
/// Destroying it wins the run and disables the guarding turret. It is not the turret:
/// shooting the turret only removes the guard.
/// </summary>
public class CoreObjective : MonoBehaviour
{
    public const float DefaultMaxHp = 150f;

    public float MaxHp = DefaultMaxHp;
    public float Hp { get; private set; }
    public bool IsDestroyed { get; private set; }

    public event Action<CoreObjective> Destroyed;

    Transform _spin;
    float _flash;

    public void Build(Transform spinRoot)
    {
        _spin = spinRoot != null ? spinRoot : transform;
        Hp = MaxHp;
    }

    void Update()
    {
        if (_spin != null && !IsDestroyed)
        {
            _spin.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);
            _spin.Rotate(Vector3.right, 18f * Time.deltaTime, Space.World);
        }

        if (_flash > 0f)
        {
            _flash -= Time.deltaTime;
            if (_flash <= 0f && !IsDestroyed) SetGlow(1f);
        }
    }

    public void Damage(int amount)
    {
        if (IsDestroyed || amount <= 0) return;
        Hp = Mathf.Max(0f, Hp - amount);
        _flash = 0.08f;
        SetGlow(1.8f);
        if (Hp <= 0f)
        {
            Vfx.Impact(transform.position, DiscesaMaterials.CoreReactor, 10);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            IsDestroyed = true;
            Destroyed?.Invoke(this);
        }
    }

    void SetGlow(float scale)
    {
        if (_spin != null) _spin.localScale = Vector3.one * scale;
    }

    public void ResetCore()
    {
        Hp = MaxHp;
        IsDestroyed = false;
        _flash = 0f;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        if (_spin != null) _spin.localScale = Vector3.one;
    }

    public float Fraction => MaxHp <= 0f ? 0f : Hp / MaxHp;
}
