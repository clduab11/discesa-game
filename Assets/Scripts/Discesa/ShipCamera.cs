using UnityEngine;

/// <summary>Smooth chase camera for the 6DoF ship. Sits behind and above the ship's local axes.</summary>
public class ShipCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 6.5f;
    public float height = 2.2f;
    public float lateralOffset = 0f;
    public float positionLerp = 8f;
    public float rotationLerp = 10f;
    public float fov = 72f;

    Camera _camera;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera != null) _camera.fieldOfView = fov;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position
                             - target.forward * distance
                             + target.up * height
                             + target.right * lateralOffset;

        transform.position = Vector3.Lerp(transform.position, desiredPos,
            1f - Mathf.Exp(-positionLerp * Time.deltaTime));

        Quaternion desiredRot = Quaternion.LookRotation(
            (target.position + target.forward * 10f) - transform.position,
            target.up);

        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot,
            1f - Mathf.Exp(-rotationLerp * Time.deltaTime));
    }
}
