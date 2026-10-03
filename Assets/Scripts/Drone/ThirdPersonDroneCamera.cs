using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Add only to Main Camera; keep the camera OUTSIDE the Drone hierarchy.
// IMPORTANT: Set Drone Rigidbody > Interpolate = Interpolate in Inspector.
public class ThirdPersonDroneCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float lookAtHeight = 0.25f;

    [Header("Camera")]
    [SerializeField, Min(0.1f)] private float distance = 4.5f;
    [SerializeField, Min(0.1f)] private float minDistance = 2f;
    [SerializeField, Min(0.1f)] private float maxDistance = 10f;
    [SerializeField, Range(5f, 75f)] private float cameraPitch = 18f;
    [SerializeField, Min(0.01f)] private float followSmoothTime = 0.15f;
    [SerializeField, Min(0.01f)] private float yawSmoothTime = 0.2f;

    [Header("Mouse (hold right button)")]
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.15f;

    private float orbitYaw;
    private float smoothYaw;
    private float yawVelocity;
    private Vector3 smoothFocus;
    private Vector3 focusVelocity;
    private bool initialized;

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                orbitYaw += delta.x * mouseSensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch + delta.y * mouseSensitivity, 5f, 75f);
            }
            distance -= mouse.scroll.ReadValue().y * 0.005f;
        }
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            orbitYaw = 0f;
            cameraPitch = 18f;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButton(1))
        {
            orbitYaw += Input.GetAxis("Mouse X") * 5f;
            cameraPitch = Mathf.Clamp(cameraPitch + Input.GetAxis("Mouse Y") * 5f, 5f, 75f);
        }
        distance -= Input.mouseScrollDelta.y * 0.6f;
        if (Input.GetKeyDown(KeyCode.R))
        {
            orbitYaw = 0f;
            cameraPitch = 18f;
        }
#endif
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredFocus = target.position + Vector3.up * lookAtHeight;
        if (!initialized)
        {
            smoothFocus = desiredFocus;
            smoothYaw = target.eulerAngles.y;
            initialized = true;
        }

        // Smooth the observed target and the heading independently.
        // An interpolated Rigidbody gives LateUpdate smooth target coordinates.
        smoothFocus = Vector3.SmoothDamp(
            smoothFocus, desiredFocus, ref focusVelocity, followSmoothTime);
        smoothYaw = Mathf.SmoothDampAngle(
            smoothYaw, target.eulerAngles.y, ref yawVelocity, yawSmoothTime);

        Quaternion orbit = Quaternion.Euler(cameraPitch, smoothYaw + orbitYaw, 0f);
        transform.position = smoothFocus + orbit * (Vector3.back * distance);
        transform.rotation = Quaternion.LookRotation(smoothFocus - transform.position, Vector3.up);
    }
}
