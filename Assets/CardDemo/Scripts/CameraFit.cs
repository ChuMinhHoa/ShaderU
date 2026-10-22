using UnityEngine;

/// Lùi camera theo trục nhìn để vùng (width x height) quanh target luôn nằm trong khung hình.
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    public Vector3 target = Vector3.zero;
    public float width = 10.5f;
    public float height = 4.6f;

    Camera cam;

    void OnEnable() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float dist = Mathf.Max(height * 0.5f / tanHalf, width * 0.5f / (tanHalf * cam.aspect));
        transform.position = target - transform.forward * dist;
    }
}
