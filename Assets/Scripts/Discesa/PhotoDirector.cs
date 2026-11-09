using System.Collections;
using UnityEngine;

/// <summary>
/// Deterministic screenshot poses for the milestone evidence pipeline.
///
/// Contract used by the CLI capture loop:
///   SetPose(name) immediately sets Ready = false, applies the pose, waits at least two
///   frames, then sets Ready = true. A stale Ready from the previous pose can therefore
///   never satisfy the wait for the next pose.
/// </summary>
public class PhotoDirector : MonoBehaviour
{
    public static PhotoDirector Instance { get; private set; }

    public bool Ready { get; private set; }
    public string CurrentPose { get; private set; }

    /// <summary>Stable order used by the CLI capture loop (no quoted strings needed).</summary>
    public static readonly string[] PoseNames =
    {
        "M1_cockpit", "M1_damage", "M1_beauty",
        "M2_corridor", "M2_gallery", "M2_core_chamber",
        "M3_turret", "M3_projectile", "M3_pickup", "M3_walker",
        "M5_win", "M5_lose", "M5_restart"
    };

    public static int PoseCount => PoseNames.Length;

    GameManager _game;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Bind(GameManager game)
    {
        _game = game;
    }

    public void SetPose(string pose)
    {
        if (_game == null) return;
        Time.timeScale = 1f;
        StopAllCoroutines();
        Ready = false;
        CurrentPose = pose;
        StartCoroutine(RunPose(pose));
    }

    /// <summary>Shell-friendly entry point: SetPoseByIndex(0) is M1_cockpit.</summary>
    public void SetPoseByIndex(int index)
    {
        index = Mathf.Clamp(index, 0, PoseNames.Length - 1);
        SetPose(PoseNames[index]);
    }

    IEnumerator RunPose(string pose)
    {
        ApplyCameraAndShipDefaults();

        switch (pose)
        {
            case "M1_cockpit":
                PoseCockpit();
                break;

            case "M1_damage":
                PoseCockpit();
                _game.status.SetResources(40f, 65f, 6);
                _game.status.SelectWeapon(0);
                _game.FlashDamage(1f);
                break;

            case "M1_beauty":
                // Keep the chase camera inside the Entry Hall: the spawn has only 3 m
                // behind it, which would put a 4.2 m chase camera through the west wall.
                PoseAt(new Vector3(-25f, -1f, 0f), Quaternion.Euler(0f, 90f, 0f));
                _game.cameraRig.SetCockpit(false);
                _game.cameraRig.SnapBeauty();
                break;

            case "M2_corridor":
                PoseAt(new Vector3(-12f, -2.6f, 0f), Quaternion.Euler(0f, 90f, 0f));
                _game.cameraRig.SetCockpit(true);
                break;

            case "M2_gallery":
                PoseAt(new Vector3(-1f, -1.4f, 8.5f), Quaternion.Euler(0f, 0f, 0f));
                _game.cameraRig.SetCockpit(true);
                break;

            case "M2_core_chamber":
                PoseAt(new Vector3(20.5f, 2.0f, 9.5f), Quaternion.Euler(0f, 108f, 0f));
                _game.cameraRig.SetCockpit(true);
                break;

            case "M3_turret":
                PoseAt(new Vector3(21.5f, 2.2f, 9f), Quaternion.Euler(0f, 112f, 0f));
                _game.cameraRig.SetCockpit(true);
                // Only the guard is live: the walker must not chip the shields mid-capture.
                _game.turret.enabled = true;
                _game.walker.enabled = false;
                yield return WaitForTurretShot();
                break;

            case "M3_projectile":
                PoseAt(new Vector3(-28f, -1.2f, 0f), Quaternion.Euler(0f, 90f, 0f));
                _game.cameraRig.SetCockpit(true);
                _game.status.SelectWeapon(0);
                _game.weapons.TryFire();
                yield return null;
                yield return null;
                yield return null;
                yield return null;
                break;

            case "M3_pickup":
                PoseAt(new Vector3(2f, -2.0f, 16.5f), Quaternion.Euler(0f, 0f, 0f));
                _game.cameraRig.SetCockpit(true);
                break;

            case "M3_walker":
                PoseAt(new Vector3(2f, -3.2f, 0.2f), Quaternion.Euler(21f, -53f, 0f));
                _game.cameraRig.SetCockpit(true);
                _game.turret.enabled = false;
                _game.walker.enabled = true;
                yield return new WaitForSeconds(1.2f); // burst lands, then sparks/beams decay
                break;

            case "M5_win":
                PoseAt(new Vector3(21.5f, 2.2f, 9f), Quaternion.Euler(0f, 112f, 0f));
                _game.cameraRig.SetCockpit(true);
                _game.core.Damage(Mathf.CeilToInt(_game.core.MaxHp));
                yield return null;
                break;

            case "M5_lose":
                PoseAt(_game.SpawnPosition, _game.SpawnRotation);
                _game.cameraRig.SetCockpit(true);
                _game.status.SetResources(100f, 100f, 6);
                _game.DamageShip(500);
                yield return null;
                break;

            case "M5_restart":
                PoseAt(_game.SpawnPosition, _game.SpawnRotation);
                _game.Restart();
                _game.cameraRig.SetCockpit(true);
                yield return null;
                break;

            default:
                PoseCockpit();
                break;
        }

        yield return null; // second frame: let the camera/HUD settle
        yield return null;
        Ready = true;
        // Freeze the frame so the CLI capture gets exactly what the pose framed:
        // projectiles stay in flight and the damage flash does not decay.
        Time.timeScale = 0f;
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
    }

    IEnumerator WaitForTurretShot()
    {
        if (_game.projectiles == null) yield break;
        int start = _game.projectiles.SpawnCount;
        float waited = 0f;
        while (_game.projectiles.SpawnCount <= start && waited < 4f)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        yield return null;
        yield return null;
        yield return null;
        yield return null;
    }

    void ApplyCameraAndShipDefaults()
    {
        _game.Restart();
        _game.SetEnemyAi(false);
        _game.projectiles?.ReleaseAll();
        _game.status.SetResources(ShipStatus.MaxShield, ShipStatus.MaxEnergy, 6);
        _game.status.SelectWeapon(0);
        _game.ClearTransientVfx();
    }

    void PoseCockpit()
    {
        PoseAt(_game.SpawnPosition, _game.SpawnRotation);
        _game.cameraRig.SetCockpit(true);
    }

    void PoseAt(Vector3 position, Quaternion rotation)
    {
        _game.ship.ResetShip(position, rotation);
        _game.containment.Reset(position);
        _game.cameraRig.SetCockpit(false);
        _game.cameraRig.SetCockpit(true);
    }
}
