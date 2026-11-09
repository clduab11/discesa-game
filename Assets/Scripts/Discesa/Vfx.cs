using UnityEngine;

/// <summary>
/// Original combat VFX helpers: pooled impact sparks, muzzle flashes and hitscan beams.
/// Nothing here allocates per shot: objects are pre-created and toggled.
/// </summary>
public class Vfx : MonoBehaviour
{
    public static Vfx Instance { get; private set; }

    const int SparkCount = 24;
    const int BeamCount = 8;

    GameObject[] _sparks = new GameObject[SparkCount];
    Renderer[] _sparkRenderers = new Renderer[SparkCount];
    float[] _sparkLife = new float[SparkCount];
    float[] _sparkBase = new float[SparkCount];
    float[] _sparkMax = new float[SparkCount];

    LineRenderer[] _beams = new LineRenderer[BeamCount];
    float[] _beamLife = new float[BeamCount];

    int _sparkCursor;
    int _beamCursor;

    void Awake()
    {
        Instance = this;

        var sparkRoot = new GameObject("Sparks");
        sparkRoot.transform.SetParent(transform, false);

        for (int i = 0; i < SparkCount; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Spark";
            go.transform.SetParent(sparkRoot.transform, false);
            go.transform.localScale = Vector3.zero;
            Object.Destroy(go.GetComponent<Collider>());
            _sparkRenderers[i] = go.GetComponent<Renderer>();
            go.SetActive(false);
            _sparks[i] = go;
        }

        var beamRoot = new GameObject("Beams");
        beamRoot.transform.SetParent(transform, false);
        for (int i = 0; i < BeamCount; i++)
        {
            var go = new GameObject("Beam");
            go.transform.SetParent(beamRoot.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.09f;
            line.enabled = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = DiscesaMaterials.Trail;
            _beams[i] = line;
            go.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Hide every spark and beam (used on restart and between photo poses).</summary>
    public static void Clear()
    {
        var vfx = Instance;
        if (vfx == null) return;
        for (int i = 0; i < SparkCount; i++)
        {
            if (vfx._sparks[i] != null) vfx._sparks[i].SetActive(false);
            vfx._sparkLife[i] = 0f;
        }
        for (int i = 0; i < BeamCount; i++)
        {
            if (vfx._beams[i] != null)
            {
                vfx._beams[i].enabled = false;
                vfx._beams[i].gameObject.SetActive(false);
            }
            vfx._beamLife[i] = 0f;
        }
    }

    /// <summary>Short-lived expanding sparks at an impact point.</summary>
    public static void Impact(Vector3 position, Material material, int count = 5)
    {
        var vfx = Instance;
        if (vfx == null) return;
        vfx.SpawnSparks(position, material, count);
    }

    /// <summary>A brief glow at a muzzle.</summary>
    public static void Muzzle(Transform muzzle, Material material)
    {
        var vfx = Instance;
        if (vfx == null || muzzle == null) return;
        vfx.SpawnSparks(muzzle.position, material, 1, 0.16f);
    }

    /// <summary>Hitscan beam that fades over ~0.15 s.</summary>
    public static void Beam(Vector3 from, Vector3 to, Material material)
    {
        var vfx = Instance;
        if (vfx == null) return;
        vfx.SpawnBeam(from, to, material);
    }

    void SpawnSparks(Vector3 position, Material material, int count, float size = 0.14f)
    {
        if (material == null) material = DiscesaMaterials.ProjectileGreen;
        for (int i = 0; i < count; i++)
        {
            int idx = _sparkCursor;
            _sparkCursor = (_sparkCursor + 1) % SparkCount;

            var go = _sparks[idx];
            go.SetActive(true);
            go.transform.position = position + Random.insideUnitSphere * 0.12f;
            go.transform.localScale = Vector3.one * size;
            go.transform.rotation = Random.rotation;
            _sparkRenderers[idx].sharedMaterial = material;
            _sparkLife[idx] = 0.16f;
            _sparkMax[idx] = 0.16f;
            _sparkBase[idx] = size;
        }
    }

    void SpawnBeam(Vector3 from, Vector3 to, Material material)
    {
        int idx = _beamCursor;
        _beamCursor = (_beamCursor + 1) % BeamCount;

        var line = _beams[idx];
        line.gameObject.SetActive(true);
        line.material = material != null ? material : DiscesaMaterials.Trail;
        line.enabled = true;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startColor = new Color(1f, 0.55f, 0.25f, 1f);
        line.endColor = new Color(1f, 0.30f, 0.20f, 0.25f);
        _beamLife[idx] = 0.15f;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = 0; i < SparkCount; i++)
        {
            if (!_sparks[i].activeSelf) continue;
            _sparkLife[i] -= dt;
            if (_sparkLife[i] <= 0f)
            {
                _sparks[i].SetActive(false);
                continue;
            }
            float k = _sparkLife[i] / _sparkMax[i];
            _sparks[i].transform.localScale = Vector3.one * (_sparkBase[i] * (0.35f + 0.65f * k));
        }

        for (int i = 0; i < BeamCount; i++)
        {
            if (!_beams[i].gameObject.activeSelf) continue;
            _beamLife[i] -= dt;
            if (_beamLife[i] <= 0f)
            {
                _beams[i].enabled = false;
                _beams[i].gameObject.SetActive(false);
            }
        }
    }
}
