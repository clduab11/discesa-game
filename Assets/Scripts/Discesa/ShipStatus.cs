using System;
using UnityEngine;

/// <summary>
/// Ship resources: shields, energy, selected weapon and missile ammo.
/// Plain data with no UI or Unity input dependencies so it can be tested headlessly.
/// Values mirror the reference model (MAX_ENERGY / MAX_SHIELDS = 100) but the slice
/// only tracks a single life pool and no score/lives counters.
/// </summary>
public class ShipStatus : MonoBehaviour
{
    public const int MaxShield = 100;
    public const int MaxEnergy = 100;
    public const int MaxMissiles = 12;

    [SerializeField] float _shield = MaxShield;
    [SerializeField] float _energy = MaxEnergy;
    [SerializeField] int _missiles = 6;
    [SerializeField] int _weaponIndex = 0;

    public float Shield => _shield;
    public float Energy => _energy;
    public int Missiles => _missiles;
    public int WeaponIndex => _weaponIndex;
    public bool IsDead => _shield <= 0f;

    /// <summary>Raised whenever any resource changes. The HUD reads a snapshot, never mutates state.</summary>
    public event Action Changed;
    public event Action Died;

    public void ResetStatus()
    {
        _shield = MaxShield;
        _energy = MaxEnergy;
        _missiles = 6;
        _weaponIndex = 0;
        Changed?.Invoke();
    }

    public void Damage(float amount)
    {
        if (amount <= 0f || _shield <= 0f) return;
        _shield = Mathf.Max(_shield - amount, 0f);
        Changed?.Invoke();
        if (_shield <= 0f) Died?.Invoke();
    }

    public bool TryConsumeEnergy(float amount)
    {
        if (amount <= 0f) return true;
        if (_energy < amount) return false;
        _energy = Mathf.Max(_energy - amount, 0f);
        Changed?.Invoke();
        return true;
    }

    public bool TryConsumeMissile(int amount = 1)
    {
        if (amount <= 0) return true;
        if (_missiles < amount) return false;
        _missiles -= amount;
        Changed?.Invoke();
        return true;
    }

    public float AddEnergy(float amount)
    {
        if (amount <= 0f) return 0f;
        float before = _energy;
        _energy = Mathf.Min(_energy + amount, MaxEnergy);
        Changed?.Invoke();
        return _energy - before;
    }

    public int AddMissiles(int amount)
    {
        if (amount <= 0) return 0;
        int before = _missiles;
        _missiles = Mathf.Min(_missiles + amount, MaxMissiles);
        Changed?.Invoke();
        return _missiles - before;
    }

    public void SelectWeapon(int index)
    {
        index = Mathf.Clamp(index, 0, WeaponDefinition.Count - 1);
        if (index == _weaponIndex) return;
        _weaponIndex = index;
        Changed?.Invoke();
    }

    public void CycleWeapon()
    {
        SelectWeapon((_weaponIndex + 1) % WeaponDefinition.Count);
    }

    /// <summary>Used by tests and the photo director to force an exact resource state.</summary>
    public void SetResources(float shield, float energy, int missiles)
    {
        _shield = Mathf.Clamp(shield, 0f, MaxShield);
        _energy = Mathf.Clamp(energy, 0f, MaxEnergy);
        _missiles = Mathf.Clamp(missiles, 0, MaxMissiles);
        Changed?.Invoke();
    }
}
