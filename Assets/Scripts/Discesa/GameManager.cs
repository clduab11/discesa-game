using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Owns the slice: builds the arena, ship, cockpit camera, HUD, weapons, enemies,
/// pickups and the core objective at runtime; runs the win/lose/restart loop.
/// Self-bootstraps so pressing Play in any scene (or a build) produces the game.
///
/// Original implementation. The reference source was studied for behaviour only;
/// no reference code, art, names, music or level data is present here.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(sceneName) && sceneName.Contains("InitTestScene")) return;

        if (Object.FindAnyObjectByType<GameManager>() != null) return;

        var go = new GameObject("Discesa");
        go.AddComponent<GameManager>();
    }

    // ---------------------------------------------------------------- public state

    [System.NonSerialized] public ArenaLayout layout;
    [System.NonSerialized] public ArenaContainment containment;
    public ShipController ship;
    public ShipStatus status;
    public WeaponSystem weapons;
    public ProjectilePool projectiles;
    public Turret turret;
    public Walker walker;
    public CoreObjective core;
    public Pickup cellPickup;
    public Pickup missilePickup;
    public CameraRig cameraRig;
    public CockpitHud hud;
    public PhotoDirector photoDirector;

    public bool IsWin { get; private set; }
    public bool IsLose { get; private set; }

    public ShipCamera shipCamera => cameraRig != null ? cameraRig.chaseBehaviour : null;
    public int shipHp => status != null ? Mathf.CeilToInt(status.Shield) : 0;

    public Vector3 SpawnPosition { get; private set; }
    public Quaternion SpawnRotation { get; private set; }

    // ---------------------------------------------------------------- private

    IShipInput _rawInput;
    LatchedShipInput _latchedInput;
    float _flash;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        layout = ArenaLayout.CreateDefault();
        SpawnPosition = layout.ShipSpawn;
        SpawnRotation = Quaternion.Euler(0f, 90f, 0f); // face +X down the Entry Hall

        _rawInput = new KeyboardMouseShipInput();
        _latchedInput = new LatchedShipInput();

        BuildWorld();

        Debug.Log($"[Discesa] Ready: ship at {ship.transform.position}, " +
                  $"core at {core.transform.position} ({core.MaxHp:0} hp), " +
                  $"turret at {turret.transform.position} ({turret.MaxHp:0} hp), " +
                  $"walker at {walker.transform.position} ({walker.MaxHp:0} hp), " +
                  $"rooms {layout.Rooms.Count} corridors {layout.Corridors.Count} bays {layout.Bays.Count}, " +
                  $"camera {cameraRig.ActiveCamera.name}");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------- build

    void BuildWorld()
    {
        var arena = ArenaBuilder.Build(layout);
        arena.transform.SetParent(transform, true);

        var poolGo = new GameObject("Projectiles");
        poolGo.transform.SetParent(transform, false);
        projectiles = poolGo.AddComponent<ProjectilePool>();
        projectiles.Build();

        var vfxGo = new GameObject("Vfx");
        vfxGo.transform.SetParent(transform, false);
        vfxGo.AddComponent<Vfx>();

        BuildShip();
        BuildTurretAndCore();
        BuildWalker();
        BuildPickups();
        BuildCameraAndHud();

        var pdGo = new GameObject("PhotoDirector");
        pdGo.transform.SetParent(transform, false);
        photoDirector = pdGo.AddComponent<PhotoDirector>();
        photoDirector.Bind(this);
    }

    void BuildShip()
    {
        var shipGo = new GameObject("Ship");
        shipGo.transform.SetPositionAndRotation(SpawnPosition, SpawnRotation);
        shipGo.transform.SetParent(transform, true);

        var visual = ShipVisualLoader.TryInstantiate(shipGo.transform);
        if (visual == null)
        {
            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.SetParent(shipGo.transform, false);
            hull.transform.localScale = new Vector3(0.7f, 0.45f, 1.3f);

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(shipGo.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 0.16f, 0.15f);
            canopy.transform.localScale = new Vector3(0.5f, 0.4f, 0.55f);

            var mat = DiscesaMaterials.ShipHull;
            hull.GetComponent<Renderer>().sharedMaterial = mat;
            canopy.GetComponent<Renderer>().sharedMaterial = mat;

            Object.Destroy(hull.GetComponent<Collider>());
            Object.Destroy(canopy.GetComponent<Collider>());
        }

        // Engine glow: an original emissive marker at the stern, used by the beauty shot.
        var engine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        engine.name = "EngineGlow";
        engine.transform.SetParent(shipGo.transform, false);
        engine.transform.localPosition = new Vector3(0f, 0f, -0.72f);
        engine.transform.localScale = new Vector3(0.13f, 0.13f, 0.20f);
        Object.Destroy(engine.GetComponent<Collider>());
        engine.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.EngineGlow;

        // Hood detail: original primitives that break up the flat hull top seen by the
        // cockpit camera. Dark spine + rails + a cyan nose lamp.
        AddHoodPart(shipGo.transform, "HoodSpine", new Vector3(0f, 0.186f, 0.22f),
            new Vector3(0.10f, 0.025f, 0.62f), DiscesaMaterials.WallTrim);
        AddHoodPart(shipGo.transform, "HoodRailL", new Vector3(-0.235f, 0.182f, 0.22f),
            new Vector3(0.04f, 0.02f, 0.55f), DiscesaMaterials.WallTrim);
        AddHoodPart(shipGo.transform, "HoodRailR", new Vector3(0.235f, 0.182f, 0.22f),
            new Vector3(0.04f, 0.02f, 0.55f), DiscesaMaterials.WallTrim);
        AddHoodPart(shipGo.transform, "NoseLamp", new Vector3(0f, 0.192f, 0.66f),
            new Vector3(0.16f, 0.03f, 0.06f), DiscesaMaterials.EngineGlow);
        AddHoodPart(shipGo.transform, "Cowl", new Vector3(0f, 0.225f, 0.02f),
            new Vector3(0.34f, 0.07f, 0.10f), DiscesaMaterials.WallTrim);

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(shipGo.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 1.35f);

        var hullCollider = shipGo.AddComponent<SphereCollider>();
        hullCollider.radius = 0.6f;
        hullCollider.isTrigger = true;

        ship = shipGo.AddComponent<ShipController>();
        ship.physics = ShipPhysics.CreateDefault();
        ship.SetInput(_latchedInput);
        ship.ResetShip(SpawnPosition, SpawnRotation);

        containment = new ArenaContainment(layout, ship.transform, ship.physics.radius);
        ship.containment = containment;
        containment.Reset(SpawnPosition);

        status = shipGo.AddComponent<ShipStatus>();
        status.Died += LoseGame;

        var weaponGo = new GameObject("Weapons");
        weaponGo.transform.SetParent(shipGo.transform, false);
        weapons = weaponGo.AddComponent<WeaponSystem>();
        weapons.Build(ship, status, projectiles);
        weapons.muzzle = muzzle.transform;
    }

    void AddHoodPart(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        Object.Destroy(go.GetComponent<Collider>());
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    void BuildTurretAndCore()
    {
        var turretGo = new GameObject("Turret");
        turretGo.transform.position = layout.TurretAnchor;
        // Face the corridor entrance (-X) so the guard actually covers the approach.
        turretGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        turretGo.transform.SetParent(transform, true);

        // Guard pedestal so the turret reads above the console line from the approach.
        var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pedestal.name = "TurretPedestal";
        pedestal.transform.SetParent(turretGo.transform, false);
        pedestal.transform.localPosition = new Vector3(0f, -3.2f, 0f);
        pedestal.transform.localScale = new Vector3(3f, 6.4f, 3f);
        pedestal.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.CoreTrim;

        var baseMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseMesh.name = "TurretBase";
        baseMesh.transform.SetParent(turretGo.transform, false);
        baseMesh.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        baseMesh.transform.localScale = new Vector3(1.3f, 0.3f, 1.3f);
        Object.Destroy(baseMesh.GetComponent<Collider>());
        baseMesh.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.Turret;

        var head = new GameObject("Head");
        head.transform.SetParent(turretGo.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.76f, 0f);

        var headBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headBody.name = "HeadBody";
        headBody.transform.SetParent(head.transform, false);
        headBody.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        headBody.transform.localScale = new Vector3(0.9f, 0.52f, 1.05f);
        Object.Destroy(headBody.GetComponent<Collider>());
        headBody.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.Turret;

        var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.SetParent(head.transform, false);
        barrel.transform.localPosition = new Vector3(0f, 0f, 0.82f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.transform.localScale = new Vector3(0.18f, 0.42f, 0.18f);
        Object.Destroy(barrel.GetComponent<Collider>());
        barrel.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.Turret;

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(head.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 1.28f);

        var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "MuzzleGlow";
        glow.transform.SetParent(muzzle.transform, false);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = Vector3.zero;
        Object.Destroy(glow.GetComponent<Collider>());
        glow.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.ProjectileRed;

        var turretCollider = turretGo.AddComponent<BoxCollider>();
        turretCollider.center = new Vector3(0f, 0.7f, 0f);
        turretCollider.size = new Vector3(1.7f, 1.5f, 1.9f);

        turret = turretGo.AddComponent<Turret>();
        turret.muzzleGlow = glow.transform;
        turret.Build(ship.transform, head.transform, muzzle.transform, projectiles);

        // ---- core
        var coreGo = new GameObject("Core");
        coreGo.transform.position = layout.CoreAnchor;
        coreGo.transform.SetParent(transform, true);

        var visual = new GameObject("CoreVisual");
        visual.transform.SetParent(coreGo.transform, false);

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "CoreSphere";
        sphere.transform.SetParent(visual.transform, false);
        sphere.transform.localScale = Vector3.one * 1.05f;
        Object.Destroy(sphere.GetComponent<Collider>());
        sphere.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.CoreReactor;

        for (int i = 0; i < 2; i++)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "CoreRing" + i;
            ring.transform.SetParent(visual.transform, false);
            ring.transform.localRotation = Quaternion.Euler(i == 0 ? 0f : 90f, 0f, i == 0 ? 24f : -24f);
            ring.transform.localScale = new Vector3(1.55f, 0.045f, 1.55f);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.CoreTrim;
        }

        var coreCollider = coreGo.AddComponent<BoxCollider>();
        coreCollider.size = Vector3.one * 1.7f;

        core = coreGo.AddComponent<CoreObjective>();
        core.Build(visual.transform);
        core.Destroyed += OnCoreDestroyed;

        var coreLightGo = new GameObject("CoreLight");
        coreLightGo.transform.SetParent(coreGo.transform, false);
        var coreLight = coreLightGo.AddComponent<Light>();
        coreLight.type = LightType.Point;
        coreLight.color = new Color(0.25f, 0.85f, 1f);
        coreLight.range = 14f;
        coreLight.intensity = 9f;
        coreLight.shadows = LightShadows.None;
    }

    void BuildWalker()
    {
        var walkerGo = new GameObject("Walker");
        walkerGo.transform.position = layout.WalkerAnchor;
        walkerGo.transform.SetParent(transform, true);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(walkerGo.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        body.transform.localScale = new Vector3(0.9f, 1.05f, 1.2f);
        Object.Destroy(body.GetComponent<Collider>());
        body.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.WalkerBody;

        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(walkerGo.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.65f, 0.25f);
        head.transform.localScale = new Vector3(0.55f, 0.42f, 0.55f);
        Object.Destroy(head.GetComponent<Collider>());
        head.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.Turret;

        for (int i = 0; i < 4; i++)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "Leg" + i;
            leg.transform.SetParent(walkerGo.transform, false);
            float sx = (i % 2 == 0) ? -1f : 1f;
            float sz = (i < 2) ? 0.3f : -0.3f;
            leg.transform.localPosition = new Vector3(sx * 0.42f, 0.38f, sz);
            leg.transform.localScale = new Vector3(0.16f, 0.76f, 0.16f);
            Object.Destroy(leg.GetComponent<Collider>());
            leg.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.WalkerBody;
        }

        var capsule = walkerGo.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.95f, 0f);
        capsule.height = 1.9f;
        capsule.radius = 0.6f;

        walker = walkerGo.AddComponent<Walker>();
        walker.Build(ship.transform, layout.WalkerWaypoints, -5.3f);
    }

    void BuildPickups()
    {
        cellPickup = MakePickup(Pickup.Kind.Cell, layout.CellPickupAnchor);
        missilePickup = MakePickup(Pickup.Kind.Missile, layout.MissilePickupAnchor);
    }

    /// <summary>Photo poses freeze enemy AI so captures are deterministic.</summary>
    public void SetEnemyAi(bool active)
    {
        if (turret != null) turret.enabled = active;
        if (walker != null) walker.enabled = active;
    }

    Pickup MakePickup(Pickup.Kind kind, Vector3 anchor)
    {
        var go = new GameObject(kind + "Pickup");
        go.transform.position = anchor;
        go.transform.SetParent(transform, true);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);

        if (kind == Pickup.Kind.Cell)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "CellCore";
            body.transform.SetParent(visual.transform, false);
            body.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            body.transform.localScale = Vector3.one * 0.55f;
            Object.Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.PickupCell;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "CellRing";
            ring.transform.SetParent(visual.transform, false);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            ring.transform.localScale = new Vector3(0.72f, 0.03f, 0.72f);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.PropMetal;
        }
        else
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "MissileBody";
            body.transform.SetParent(visual.transform, false);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale = new Vector3(0.22f, 0.36f, 0.22f);
            Object.Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = DiscesaMaterials.PickupMissile;
        }

        var lightGo = new GameObject("Glow");
        lightGo.transform.SetParent(go.transform, false);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = kind == Pickup.Kind.Cell ? new Color(0.35f, 1f, 0.65f) : new Color(1f, 0.66f, 0.22f);
        light.range = 6f;
        light.intensity = 2f;
        light.shadows = LightShadows.None;

        var sphere = go.AddComponent<SphereCollider>();
        sphere.radius = 0.55f;
        sphere.isTrigger = true;

        var pickup = go.AddComponent<Pickup>();
        pickup.Build(kind, status, ship.transform, anchor, visual.transform, light);
        return pickup;
    }

    void BuildCameraAndHud()
    {
        var rigGo = new GameObject("CameraRig");
        rigGo.transform.SetParent(transform, false);
        cameraRig = rigGo.AddComponent<CameraRig>();
        cameraRig.Build(ship);

        var hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(transform, false);
        hud = hudGo.AddComponent<CockpitHud>();
        hud.Build();
    }

    // ---------------------------------------------------------------- loop

    void Update()
    {
        if (_rawInput == null) return; // duplicate/partially constructed instance awaiting Destroy

        float dt = Time.deltaTime;

        // One hardware sample per frame; ShipController's fixed substeps replay it.
        var raw = _rawInput.Sample(dt);
        _latchedInput.Latch(raw);

        HandleGlobalKeys();

        if (_flash > 0f) _flash = Mathf.Max(0f, _flash - dt * 2.2f);

        if (IsWin || IsLose || ship == null || ship.IsDead || weapons == null) return;

        if (raw.Fire) weapons.TryFire();
    }

    void LateUpdate()
    {
        if (hud != null && status != null)
            hud.Render(HudModel.From(status, core, IsWin, IsLose, _flash));
    }

    void HandleGlobalKeys()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey.wasPressedThisFrame) Restart();
        if (kb.vKey.wasPressedThisFrame && cameraRig != null) cameraRig.Toggle();
        if (weapons != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) weapons.Select(0);
            if (kb.digit2Key.wasPressedThisFrame) weapons.Select(1);
            if (kb.digit3Key.wasPressedThisFrame) weapons.Select(2);
            if (kb.tabKey.wasPressedThisFrame) weapons.Cycle();
        }
#endif
    }

    // ---------------------------------------------------------------- outcomes

    public void DamageShip(int amount)
    {
        if (IsWin || IsLose || status == null) return;
        status.Damage(amount);
        _flash = 0.6f;
        if (status.IsDead) LoseGame();
    }

    public void OnCoreDestroyed(CoreObjective _)
    {
        if (IsWin || IsLose) return;
        IsWin = true;
        if (turret != null) turret.DisableGuard();
    }

    public void LoseGame()
    {
        if (IsWin || IsLose) return;
        IsLose = true;
        if (ship != null) ship.Kill();
    }

    public void Restart()
    {
        IsWin = false;
        IsLose = false;
        _flash = 0f;

        if (status != null) status.ResetStatus();
        if (ship != null) ship.ResetShip(SpawnPosition, SpawnRotation);
        if (containment != null) containment.Reset(SpawnPosition);
        if (projectiles != null) projectiles.ReleaseAll();
        if (turret != null) turret.ResetTurret();
        if (walker != null) walker.ResetWalker();
        if (core != null) core.ResetCore();
        if (cellPickup != null) cellPickup.ResetPickup();
        if (missilePickup != null) missilePickup.ResetPickup();
        if (cameraRig != null) cameraRig.SetCockpit(true);
        ClearTransientVfx();
    }

    public void FlashDamage(float amount) => _flash = Mathf.Clamp01(amount);

    public void HudMessage(string message)
    {
        if (hud != null) hud.ShowPickup(message);
    }

    public void ClearTransientVfx() => Vfx.Clear();
}
