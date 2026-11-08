using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Owns the win/lose/restart loop, builds the graybox arena and actors at runtime,
/// and hosts the HUD. Self-bootstraps so pressing Play in any scene (or a build)
/// produces a playable slice without scene wiring.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        // Unity's test runner uses its own throwaway scenes; bootstrapping a full match
        // there would run a game in the background of every PlayMode test.
        string sceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(sceneName) && sceneName.Contains("InitTestScene")) return;

        if (Object.FindAnyObjectByType<GameManager>() != null) return;

        var go = new GameObject("Discesa");
        go.AddComponent<GameManager>();
    }

    public int shipMaxHp = 100;
    public int shipHp { get; private set; }
    public bool IsWin { get; private set; }
    public bool IsLose { get; private set; }

    public ShipController ship;
    public Turret turret;
    public ShipCamera shipCamera;

    public float fireInterval = 0.15f;
    public int shipProjectileDamage = 15;

    Canvas _canvas;
    Text _hud;
    Text _banner;
    float _fireCooldown;
    float _hitFlash;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        shipHp = shipMaxHp;

        BuildArena();
        BuildShip();
        BuildTurret();
        BuildCamera();
        BuildHud();

        Debug.Log($"[Discesa] Ready: ship at {ship.transform.position}, " +
                  $"turret at {turret.transform.position} ({turret.hp} hp), " +
                  $"camera {shipCamera.name}, arena 40x20x40");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------- build

    void BuildArena()
    {
        ArenaBuilder.Build(Vector3.zero, 40f, 20f, 40f);
        BuildTrialProp();
    }

    /// <summary>
    /// SimReady GLB trial prop (CC-BY-4.0, credit NVIDIA): a single white crate
    /// as non-gameplay set dressing on the arena floor, out of the ship's spawn
    /// lane. Gameplay collision is unchanged; the prop is visual-only.
    /// </summary>
    void BuildTrialProp()
    {
        var prop = SimReadyPropLoader.TryInstantiate();
        if (prop != null)
        {
            prop.transform.position = new Vector3(0f, -9.2f, -12f);
            prop.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        }
        // If the dataset file has not been downloaded, the arena stays clean.
    }

    void BuildShip()
    {
        var shipGO = new GameObject("Ship");
        shipGO.transform.SetPositionAndRotation(new Vector3(-14f, 0f, 0f), Quaternion.identity);

        // Authored ship visual (Blender GLB). The swept kinematic controller owns
        // collision; the imported mesh is visual-only (colliders stripped).
        var visual = ShipVisualLoader.TryInstantiate(shipGO.transform);
        if (visual == null)
        {
            // Fallback graybox primitives when the GLB asset has not been imported
            // yet (fresh checkout, or Editor not yet refreshed). Same envelope.
            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.SetParent(shipGO.transform, false);
            hull.transform.localScale = new Vector3(0.7f, 0.45f, 1.3f);

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(shipGO.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 0.16f, 0.15f);
            canopy.transform.localScale = new Vector3(0.5f, 0.4f, 0.55f);

            var shipMat = DiscesaMaterials.ShipHull;
            hull.GetComponent<Renderer>().sharedMaterial = shipMat;
            canopy.GetComponent<Renderer>().sharedMaterial = shipMat;

            // Child primitives would add their own colliders and fight the swept controller.
            Object.Destroy(hull.GetComponent<Collider>());
            Object.Destroy(canopy.GetComponent<Collider>());
        }

        // One sphere collider so incoming projectiles can register hits. It is a trigger
        // because the ship's own swept move must not collide with itself (casts ignore triggers).
        var hullCollider = shipGO.AddComponent<SphereCollider>();
        hullCollider.radius = 0.6f;
        hullCollider.isTrigger = true;

        ship = shipGO.AddComponent<ShipController>();
        ship.arenaHalfExtents = new Vector3(20f, 10f, 20f);
        ship.ResetShip(new Vector3(-14f, 0f, 0f), Quaternion.identity);
    }

    void BuildTurret()
    {
        var turretGO = new GameObject("Turret");
        turretGO.transform.SetPositionAndRotation(new Vector3(12f, 0f, 0f), Quaternion.identity);

        var baseMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseMesh.name = "TurretBase";
        baseMesh.transform.SetParent(turretGO.transform, false);
        baseMesh.transform.localScale = new Vector3(1.4f, 1f, 1.4f);

        var turretMat = DiscesaMaterials.Turret;
        baseMesh.GetComponent<Renderer>().sharedMaterial = turretMat;

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(turretGO.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.2f, 1.1f);

        turret = turretGO.AddComponent<Turret>();
        turret.muzzle = muzzle.transform;
        turret.target = ship != null ? ship.transform : null;

        // Objective marker above the turret.
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "ObjectiveCore";
        core.transform.SetPositionAndRotation(turretGO.transform.position + Vector3.up * 2f, Quaternion.identity);
        core.transform.localScale = Vector3.one * 0.6f;
        Object.Destroy(core.GetComponent<Collider>());

        var coreMat = DiscesaMaterials.ObjectiveCore;
        core.GetComponent<Renderer>().sharedMaterial = coreMat;

        _objectiveCore = core.transform;
    }

    Transform _objectiveCore;

    void BuildCamera()
    {
        // Reuse the scene camera when one exists (avoids a second AudioListener and a
        // duplicate render), otherwise create one so an empty scene is still playable.
        Camera cam = Object.FindAnyObjectByType<Camera>();
        GameObject camGO;

        if (cam != null)
        {
            camGO = cam.gameObject;
        }
        else
        {
            camGO = new GameObject("ShipCamera");
            cam = camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
        }

        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;

        if (camGO.GetComponent<AudioListener>() == null)
            camGO.AddComponent<AudioListener>();

        shipCamera = camGO.GetComponent<ShipCamera>();
        if (shipCamera == null) shipCamera = camGO.AddComponent<ShipCamera>();
        shipCamera.target = ship != null ? ship.transform : null;

        camGO.transform.position = ship != null
            ? ship.transform.position - ship.transform.forward * 6.5f + Vector3.up * 2.2f
            : new Vector3(-20f, 2f, 0f);
        camGO.transform.LookAt(ship != null ? ship.transform.position : Vector3.zero);
    }

    void BuildHud()
    {
        var canvasGO = new GameObject("Canvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _hud = new GameObject("HUDText").AddComponent<Text>();
        _hud.transform.SetParent(canvasGO.transform, false);
        _hud.font = font;
        _hud.fontSize = 18;
        _hud.alignment = TextAnchor.UpperLeft;
        _hud.color = Color.white;
        var hudRt = _hud.rectTransform;
        hudRt.anchorMin = new Vector2(0f, 1f);
        hudRt.anchorMax = new Vector2(0f, 1f);
        hudRt.pivot = new Vector2(0f, 1f);
        hudRt.anchoredPosition = new Vector2(20f, -20f);
        hudRt.sizeDelta = new Vector2(560f, 220f);

        _banner = new GameObject("Banner").AddComponent<Text>();
        _banner.transform.SetParent(canvasGO.transform, false);
        _banner.font = font;
        _banner.fontSize = 34;
        _banner.alignment = TextAnchor.MiddleCenter;
        _banner.color = Color.white;
        var bannerRt = _banner.rectTransform;
        bannerRt.anchorMin = Vector2.zero;
        bannerRt.anchorMax = Vector2.one;
        bannerRt.offsetMin = Vector2.zero;
        bannerRt.offsetMax = Vector2.zero;
        _banner.text = string.Empty;

        var crosshair = new GameObject("Crosshair").AddComponent<Text>();
        crosshair.transform.SetParent(canvasGO.transform, false);
        crosshair.font = font;
        crosshair.fontSize = 20;
        crosshair.alignment = TextAnchor.MiddleCenter;
        crosshair.color = new Color(1f, 1f, 1f, 0.85f);
        crosshair.text = "+";
        var crossRt = crosshair.rectTransform;
        crossRt.anchorMin = new Vector2(0.5f, 0.5f);
        crossRt.anchorMax = new Vector2(0.5f, 0.5f);
        crossRt.sizeDelta = new Vector2(40f, 40f);
        crossRt.anchoredPosition = Vector2.zero;
    }

    // ---------------------------------------------------------------- loop

    void Update()
    {
        if (RestartPressed()) Restart();
        if (_hitFlash > 0f) _hitFlash -= Time.deltaTime;

        if (IsWin || IsLose || ship == null || ship.IsDead) return;

        _fireCooldown -= Time.deltaTime;
        var input = ship.input;
        if (input == null) return;

        var state = input.Sample(Time.deltaTime);
        if (state.Fire && _fireCooldown <= 0f)
        {
            FireShipWeapon();
            _fireCooldown = fireInterval;
        }
    }

    void LateUpdate()
    {
        if (_objectiveCore != null)
            _objectiveCore.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);

        UpdateHud();
    }

    bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.rKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    void FireShipWeapon()
    {
        if (ship == null) return;
        Vector3 origin = ship.transform.position + ship.transform.forward * 1.3f;
        Projectile.Spawn(origin, ship.transform.rotation,
            ship.transform.forward * 70f + ship.velocity * 0.5f,
            DiscesaMaterials.ProjectileGreen, shipProjectileDamage);
    }

    public void DamageShip(int amount)
    {
        if (IsWin || IsLose) return;
        shipHp = Mathf.Max(shipHp - amount, 0);
        _hitFlash = 0.15f;
        if (shipHp <= 0) LoseGame();
    }

    public void OnTurretKilled()
    {
        if (IsWin || IsLose) return;
        IsWin = true;
        _banner.text = "CORE DESTROYED\nPress R to restart";
        _banner.color = new Color(0.4f, 1f, 0.6f);
    }

    public void LoseGame()
    {
        if (IsWin || IsLose) return;
        IsLose = true;
        if (ship != null) ship.Kill();
        _banner.text = "SHIP DESTROYED\nPress R to restart";
        _banner.color = new Color(1f, 0.45f, 0.4f);
    }

    public void Restart()
    {
        shipHp = shipMaxHp;
        IsWin = false;
        IsLose = false;
        _banner.text = string.Empty;
        _hitFlash = 0f;
        _fireCooldown = 0f;

        if (ship != null) ship.ResetShip(new Vector3(-14f, 0f, 0f), Quaternion.identity);
        if (turret != null) turret.ResetTurret();
    }

    void UpdateHud()
    {
        if (_hud == null || ship == null) return;

        int turretHp = turret != null ? turret.hp : 0;
        string objective = IsWin ? "Objective: complete" : "Objective: destroy the red core";
        string speed = ship.velocity.magnitude.ToString("F1");

        _hud.text =
            $"HULL {shipHp}/{shipMaxHp}    CORE {turretHp}\n" +
            $"SPEED {speed} m/s\n" +
            objective + "\n" +
            "I/K thrust  J/L strafe  PageUp/Down lift\n" +
            "W/S pitch  A/D yaw  Q/E roll  Space fire  R restart";
        _hud.color = _hitFlash > 0f ? new Color(1f, 0.5f, 0.5f) : Color.white;
    }
}
