using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CockpitCameraTests
{
    GameObject _ship;
    GameObject _camGo;

    [TearDown]
    public void TearDown()
    {
        if (_camGo != null) Object.Destroy(_camGo);
        if (_ship != null) Object.Destroy(_ship);
    }

    [UnityTest]
    public IEnumerator CockpitCamera_IsChildAtLocalOffset_AndMatchesShipForward()
    {
        _ship = new GameObject("Ship");
        _ship.transform.SetPositionAndRotation(new Vector3(1f, 2f, 3f), Quaternion.Euler(0f, 35f, 0f));

        _camGo = new GameObject("CockpitCamera");
        var cam = _camGo.AddComponent<Camera>();
        _camGo.transform.SetParent(_ship.transform, false);

        var cockpit = _camGo.AddComponent<CockpitCamera>();
        cockpit.cam = cam;
        cockpit.Configure(_ship.transform);

        yield return null;

        Assert.That(_camGo.transform.parent, Is.EqualTo(_ship.transform), "Cockpit camera must be a rigid child of the ship");
        Assert.That(_camGo.transform.localPosition, Is.EqualTo(new Vector3(0f, 0.30f, 0.10f)));
        Assert.That(Quaternion.Angle(_camGo.transform.localRotation, Quaternion.identity), Is.LessThan(0.01f));
        Assert.That(Vector3.Dot(_camGo.transform.forward, _ship.transform.forward), Is.GreaterThan(0.999f),
            "Cockpit camera must share the ship's forward axis");
        Assert.That(cam.nearClipPlane, Is.EqualTo(0.1f));
        Assert.That(cam.fieldOfView, Is.EqualTo(72f));
    }

    [UnityTest]
    public IEnumerator CanopyRenderer_IsHiddenInCockpit()
    {
        _ship = new GameObject("Ship");

        var visual = new GameObject("ShipVisual");
        visual.transform.SetParent(_ship.transform, false);

        var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hull.name = "Hull";
        hull.transform.SetParent(visual.transform, false);

        var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "Canopy";
        canopy.transform.SetParent(visual.transform, false);
        Object.Destroy(canopy.GetComponent<Collider>());
        Object.Destroy(hull.GetComponent<Collider>());

        _camGo = new GameObject("CockpitCamera");
        var cam = _camGo.AddComponent<Camera>();
        _camGo.transform.SetParent(_ship.transform, false);
        var cockpit = _camGo.AddComponent<CockpitCamera>();
        cockpit.cam = cam;
        cockpit.Configure(_ship.transform);

        yield return null;

        Assert.That(cockpit.CanopyHidden, Is.True, "Canopy renderer should be hidden");
        Assert.That(canopy.GetComponent<Renderer>().enabled, Is.False);
        Assert.That(hull.GetComponent<Renderer>().enabled, Is.True, "Hull/nose must stay visible");
    }
}
