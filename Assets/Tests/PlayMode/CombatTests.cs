using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CombatTests
{
    readonly System.Collections.Generic.List<GameObject> _objects = new System.Collections.Generic.List<GameObject>();

    GameObject Track(GameObject go)
    {
        _objects.Add(go);
        return go;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in _objects)
            if (go != null) Object.Destroy(go);
        _objects.Clear();
        Vfx.Clear();
    }

    ProjectilePool NewPool()
    {
        var go = Track(new GameObject("Pool"));
        var pool = go.AddComponent<ProjectilePool>();
        pool.Build();
        return pool;
    }

    [UnityTest]
    public IEnumerator Turret_RequiresLineOfSight_AndFires()
    {
        var pool = NewPool();

        var turretGo = Track(new GameObject("Turret"));
        var head = Track(new GameObject("Head"));
        head.transform.SetParent(turretGo.transform, false);
        var muzzle = Track(new GameObject("Muzzle"));
        muzzle.transform.SetParent(head.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 1.2f);

        var target = Track(new GameObject("Target"));
        target.transform.position = new Vector3(0f, 0f, 10f);

        var turret = turretGo.AddComponent<Turret>();
        turret.Build(target.transform, head.transform, muzzle.transform, pool);

        var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
        wall.name = "Blocker";
        wall.transform.position = new Vector3(0f, 0f, 5f);
        wall.transform.localScale = new Vector3(5f, 5f, 0.3f);
        Physics.SyncTransforms();

        yield return new WaitForSeconds(2f);
        Assert.That(pool.SpawnCount, Is.EqualTo(0), "Turret must not fire through a wall");
        Assert.That(turret.CanSee(), Is.False);

        Object.Destroy(wall);
        _objects.Remove(wall);
        Physics.SyncTransforms();

        float waited = 0f;
        while (pool.SpawnCount == 0 && waited < 2.5f)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        Assert.That(pool.SpawnCount, Is.GreaterThan(0), "Turret should engage once the line of sight is clear");
    }

    [UnityTest]
    public IEnumerator Projectile_DamagesTurret()
    {
        var pool = NewPool();

        var turretGo = Track(new GameObject("Turret"));
        turretGo.transform.position = new Vector3(0f, 0f, 10f);
        var col = turretGo.AddComponent<BoxCollider>();
        col.size = new Vector3(2f, 2f, 2f);
        var turret = turretGo.AddComponent<Turret>();
        turret.Build(null, turretGo.transform, turretGo.transform, pool);

        Physics.SyncTransforms();

        pool.Spawn(new Vector3(0f, 0f, 4f), Vector3.forward * 70f, 10, true, 2.5f, 0.1f, 0f,
            DiscesaMaterials.ProjectileGreen);

        yield return new WaitForSeconds(0.3f);

        Assert.That(turret.Hp, Is.LessThan(turret.MaxHp), "Player bolt should damage the turret");
    }

    [UnityTest]
    public IEnumerator Spread_FiresThreePellets_AndCostsEnergy()
    {
        var pool = NewPool();
        var shipGo = Track(new GameObject("Ship"));
        var ship = shipGo.AddComponent<ShipController>();
        var status = shipGo.AddComponent<ShipStatus>();
        status.SetResources(100f, 100f, 6);

        var weaponGo = Track(new GameObject("Weapons"));
        var weapons = weaponGo.AddComponent<WeaponSystem>();
        weapons.Build(ship, status, pool);
        weapons.muzzle = shipGo.transform;

        status.SelectWeapon(1);
        int before = pool.SpawnCount;
        bool fired = weapons.TryFire();

        yield return null;

        Assert.That(fired, Is.True);
        Assert.That(pool.SpawnCount - before, Is.EqualTo(3), "Spread must spawn three bolts");
        Assert.That(status.Energy, Is.EqualTo(94f), "Spread costs 6 energy");
    }

    [UnityTest]
    public IEnumerator Heavy_ConsumesMissileAmmo()
    {
        var pool = NewPool();
        var shipGo = Track(new GameObject("Ship"));
        var ship = shipGo.AddComponent<ShipController>();
        var status = shipGo.AddComponent<ShipStatus>();
        status.SetResources(100f, 100f, 1);

        var weaponGo = Track(new GameObject("Weapons"));
        var weapons = weaponGo.AddComponent<WeaponSystem>();
        weapons.Build(ship, status, pool);
        weapons.muzzle = shipGo.transform;

        status.SelectWeapon(2);
        Assert.That(weapons.TryFire(), Is.True);
        Assert.That(status.Missiles, Is.EqualTo(0));

        yield return null;

        Assert.That(weapons.TryFire(), Is.False, "Empty missile rack must refuse the shot");
        Assert.That(pool.SpawnCount, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator Walker_StaysOnFloor_AndBursts()
    {
        var walkerGo = Track(new GameObject("Walker"));
        var target = Track(new GameObject("Target"));
        target.transform.position = new Vector3(0f, 0f, 8f);
        // Walker LOS requires a ship component on the target collider.
        var targetCol = target.AddComponent<SphereCollider>();
        targetCol.radius = 0.6f;
        targetCol.isTrigger = true;
        target.AddComponent<ShipController>();

        var walker = walkerGo.AddComponent<Walker>();
        var waypoints = new[]
        {
            new Vector3(0f, -5.3f, 0f),
            new Vector3(3f, -5.3f, 2f)
        };
        walker.Build(target.transform, waypoints, -5.3f);

        yield return new WaitForSeconds(1.6f);

        Assert.That(Mathf.Abs(walkerGo.transform.position.y + 5.3f), Is.LessThan(0.01f),
            "Walker must stay on its floor plane");
        Assert.That(walker.ShotsFired, Is.GreaterThanOrEqualTo(3), "Walker should fire a three-shot burst");
    }
}
