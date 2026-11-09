using UnityEngine;

/// <summary>
/// Owns the two cameras: the rigid cockpit hood camera and a smooth chase/beauty camera
/// used for screenshots and debugging. V toggles between them; the cockpit is the default.
/// Exactly one camera is enabled at a time, and a single AudioListener lives on the rig
/// root so switching never doubles the listener.
/// </summary>
public class CameraRig : MonoBehaviour
{
    public ShipController ship;
    public CockpitCamera cockpit;
    public Camera cockpitCamera;
    public Camera beautyCamera;
    public ShipCamera chaseBehaviour;

    public bool IsCockpit { get; private set; } = true;
    public Camera ActiveCamera => IsCockpit ? cockpitCamera : beautyCamera;

    public Camera Build(ShipController target)
    {
        ship = target;

        EnsureListener();

        // ---- cockpit camera: rigid child of the ship
        var cockpitGo = new GameObject("CockpitCamera");
        cockpitGo.transform.SetParent(ship != null ? ship.transform : transform, false);
        cockpitCamera = cockpitGo.AddComponent<Camera>();
        cockpitCamera.depth = 1f;
        cockpitCamera.clearFlags = CameraClearFlags.SolidColor;
        cockpitCamera.backgroundColor = new Color(0.01f, 0.015f, 0.02f);

        cockpit = cockpitGo.AddComponent<CockpitCamera>();
        cockpit.cam = cockpitCamera;
        if (ship != null) cockpit.Configure(ship.transform);

        // ---- beauty/chase camera
        var beautyGo = new GameObject("BeautyCamera");
        beautyGo.transform.SetParent(transform, false);
        beautyCamera = beautyGo.AddComponent<Camera>();
        beautyCamera.depth = 0f;
        beautyCamera.clearFlags = CameraClearFlags.SolidColor;
        beautyCamera.backgroundColor = new Color(0.01f, 0.015f, 0.02f);
        beautyCamera.fieldOfView = 60f;

        chaseBehaviour = beautyGo.AddComponent<ShipCamera>();
        chaseBehaviour.target = ship != null ? ship.transform : null;
        chaseBehaviour.distance = 4.2f;
        chaseBehaviour.height = 1.4f;
        chaseBehaviour.lateralOffset = 1.2f;

        if (ship != null)
        {
            beautyGo.transform.position = ship.transform.position - ship.transform.forward * 6.5f + Vector3.up * 2.2f;
            beautyGo.transform.rotation = Quaternion.LookRotation(ship.transform.position - beautyGo.transform.position, Vector3.up);
        }

        SetCockpit(true);
        return cockpitCamera;
    }

    void EnsureListener()
    {
        var listener = GetComponent<AudioListener>();
        if (listener == null) listener = gameObject.AddComponent<AudioListener>();

        var listeners = Object.FindObjectsByType<AudioListener>();
        foreach (var other in listeners)
        {
            if (other != null && other != listener) other.enabled = false;
        }
    }

    public void Toggle() => SetCockpit(!IsCockpit);

    public void SetCockpit(bool useCockpit)
    {
        IsCockpit = useCockpit;
        if (cockpitCamera != null)
        {
            cockpitCamera.enabled = useCockpit;
            cockpitCamera.gameObject.tag = useCockpit ? "MainCamera" : "Untagged";
        }
        if (beautyCamera != null)
        {
            beautyCamera.enabled = !useCockpit;
            beautyCamera.gameObject.tag = useCockpit ? "Untagged" : "MainCamera";
        }
        // Cockpit has no hull in frame, matching the reference overlay. Beauty shows it.
        if (cockpit != null) cockpit.SetShipVisible(!useCockpit);
    }

    /// <summary>Force the chase camera to snap instead of easing (used by photo poses).</summary>
    public void SnapBeauty()
    {
        if (ship == null || chaseBehaviour == null) return;
        var t = ship.transform;
        beautyCamera.transform.position = t.position - t.forward * chaseBehaviour.distance
                                          + t.up * chaseBehaviour.height
                                          + t.right * chaseBehaviour.lateralOffset;
        beautyCamera.transform.rotation = Quaternion.LookRotation(t.position + t.forward * 10f - beautyCamera.transform.position, t.up);
    }
}
