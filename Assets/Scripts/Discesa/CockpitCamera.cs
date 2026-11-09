using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rigid first-person hood camera. Lives as a child of the ship at a fixed local
/// offset with identity local rotation, so it never smooths or lags behind the hull.
/// The authored canopy is hidden because the camera sits just above the hull and the
/// canopy would otherwise fill the view.
///
/// Local offset (0, 0.30, 0.10) m keeps the nose visible at the bottom of the frame.
/// </summary>
public class CockpitCamera : MonoBehaviour
{
    public Transform ship;
    public Camera cam;
    public Vector3 localOffset = new Vector3(0f, 0.30f, 0.10f);
    public float fov = 72f;
    public float nearClip = 0.1f;

    public bool CanopyHidden { get; private set; }
    public bool ShipHidden => _shipHidden;

    readonly Dictionary<Renderer, bool> _rendererState = new Dictionary<Renderer, bool>();
    bool _shipHidden;

    void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
    }

    public void Configure(Transform shipTransform)
    {
        ship = shipTransform;
        if (cam == null) cam = GetComponent<Camera>();

        if (transform.parent != ship)
        {
            transform.SetParent(ship, false);
        }

        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (cam != null)
        {
            cam.fieldOfView = fov;
            cam.nearClipPlane = nearClip;
            cam.farClipPlane = 500f;
        }

        HideCanopy();
    }

    /// <summary>
    /// The reference cockpit is a 2D overlay with no hull in frame. Hiding the ship in
    /// cockpit mode removes the flat hood slab while keeping the chase/beauty view intact.
    /// </summary>
    public void SetShipVisible(bool visible)
    {
        if (ship == null) return;

        if (!visible)
        {
            if (_shipHidden) return;
            _rendererState.Clear();
            foreach (var r in ship.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                _rendererState[r] = r.enabled;
                r.enabled = false;
            }
            _shipHidden = true;
            return;
        }

        if (!_shipHidden) return;
        foreach (var pair in _rendererState)
            if (pair.Key != null) pair.Key.enabled = pair.Value;
        _rendererState.Clear();
        _shipHidden = false;
    }

    /// <summary>Disables any renderer under the ship named like a canopy.</summary>
    public void HideCanopy()
    {
        if (ship == null) return;
        CanopyHidden = false;

        var renderers = ship.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r == null) continue;
            string n = r.gameObject.name.ToLowerInvariant();
            string mat = r.sharedMaterial != null ? r.sharedMaterial.name.ToLowerInvariant() : string.Empty;
            if (n.Contains("canopy") || mat.Contains("canopy"))
            {
                r.enabled = false;
                CanopyHidden = true;
            }
        }
    }
}
