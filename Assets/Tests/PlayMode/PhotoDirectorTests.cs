using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PhotoDirectorTests
{
    GameManager _game;

    [TearDown]
    public void TearDown()
    {
        if (_game != null) Object.Destroy(_game.gameObject);
    }

    [UnityTest]
    public IEnumerator PhotoDirector_AllPosesSettle_AndLoopWorks()
    {
        var go = new GameObject("GameManagerTest");
        _game = go.AddComponent<GameManager>();
        yield return null;

        Assert.That(_game.ship, Is.Not.Null);
        Assert.That(_game.cameraRig, Is.Not.Null);
        Assert.That(_game.hud, Is.Not.Null);
        Assert.That(_game.core, Is.Not.Null);
        Assert.That(_game.turret, Is.Not.Null);
        Assert.That(_game.walker, Is.Not.Null);
        Assert.That(_game.layout.Rooms.Count, Is.EqualTo(3));
        Assert.That(_game.layout.Corridors.Count, Is.EqualTo(2));

        string[] poses =
        {
            "M1_cockpit", "M1_damage", "M1_beauty",
            "M2_corridor", "M2_gallery", "M2_core_chamber",
            "M3_turret", "M3_projectile", "M3_pickup", "M3_walker",
            "M5_win", "M5_lose", "M5_restart"
        };

        foreach (var pose in poses)
        {
            _game.photoDirector.SetPose(pose);
            Assert.That(_game.photoDirector.Ready, Is.False, $"{pose} must clear Ready synchronously");

            float waited = 0f;
            while (!_game.photoDirector.Ready && waited < 6f)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            Assert.That(_game.photoDirector.Ready, Is.True, $"{pose} did not settle");
            var cam = _game.cameraRig.ActiveCamera;
            Assert.That(cam, Is.Not.Null, $"{pose} has no active camera");
            Assert.That(float.IsNaN(cam.transform.position.x) || float.IsInfinity(cam.transform.position.x), Is.False);
            Assert.That(float.IsNaN(cam.transform.position.y) || float.IsInfinity(cam.transform.position.y), Is.False);
            Assert.That(float.IsNaN(cam.transform.position.z) || float.IsInfinity(cam.transform.position.z), Is.False);
        }

        // ---- loop: core destroyed wins
        _game.photoDirector.SetPose("M5_restart");
        yield return WaitReady("M5_restart");

        _game.core.Damage(Mathf.CeilToInt(_game.core.MaxHp));
        yield return null;
        Assert.That(_game.IsWin, Is.True, "Core destruction must win");
        Assert.That(_game.turret.enabled, Is.False, "Core destruction must disable the guard");

        // ---- restart resets everything
        _game.Restart();
        yield return null;
        Assert.That(_game.IsWin, Is.False);
        Assert.That(_game.core.Hp, Is.EqualTo(_game.core.MaxHp));
        Assert.That(_game.turret.enabled, Is.True);
        Assert.That(_game.status.Shield, Is.EqualTo(100f));

        // ---- shields at zero lose
        _game.DamageShip(500);
        yield return null;
        Assert.That(_game.IsLose, Is.True, "Zero shields must lose");
        Assert.That(_game.ship.IsDead, Is.True);

        // ---- restart within the same frame, no stale state
        _game.Restart();
        yield return null;
        Assert.That(_game.IsLose, Is.False);
        Assert.That(_game.ship.IsDead, Is.False);
        Assert.That(_game.status.Shield, Is.EqualTo(100f));
        Assert.That(_game.status.Energy, Is.EqualTo(100f));
        Assert.That(_game.status.Missiles, Is.EqualTo(6));
        Assert.That(_game.projectiles.ActiveCount, Is.EqualTo(0));
    }

    IEnumerator WaitReady(string pose)
    {
        float waited = 0f;
        while (!_game.photoDirector.Ready && waited < 6f)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        Assert.That(_game.photoDirector.Ready, Is.True, $"{pose} did not settle");
    }
}
