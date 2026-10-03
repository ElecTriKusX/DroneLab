using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Basic altitude-hold controller for the QuadcopterMotors script from this conversation.
// Add to the Drone object, alongside Rigidbody and QuadcopterMotors.
[RequireComponent(typeof(Rigidbody), typeof(QuadcopterMotors))]
public class DroneKeyboardController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QuadcopterMotors motors;

    [Header("Important: match QuadcopterMotors.maxMotorThrust")]
    [SerializeField, Min(0.1f)] private float maxMotorThrust = 5f;

    [Header("Altitude hold")]
    [SerializeField, Min(0f)] private float climbSpeed = 1.5f; // m/s of target-altitude change
    [SerializeField, Min(0f)] private float altitudeP = 2.0f;
    [SerializeField, Min(0f)] private float verticalD = 2.4f;
    [SerializeField, Min(0f)] private float maxVerticalCorrection = 4f; // m/s^2

    [Header("Attitude control")]
    [SerializeField, Range(0f, 35f)] private float maxTiltDegrees = 18f;
    [SerializeField, Min(0f)] private float maxYawRate = 1.3f; // rad/s
    [SerializeField, Min(0f)] private float tiltP = 0.15f;
    [SerializeField, Min(0f)] private float tiltD = 0.03f;
    [SerializeField, Range(0f, 0.2f)] private float maxTiltMotorCorrection = 0.08f;
    [SerializeField, Min(0f)] private float yawRateP = 0.15f;
    [SerializeField, Range(0f, 0.2f)] private float maxYawMotorCorrection = 0.12f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private float yawInput;
    private float climbInput;
    private float targetAltitude;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (motors == null) motors = GetComponent<QuadcopterMotors>();
        rb.maxAngularVelocity = 10f;
        targetAltitude = rb.position.y;
    }

    private void OnEnable()
    {
        if (rb != null) targetAltitude = rb.position.y;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            moveInput = Vector2.zero;
            yawInput = climbInput = 0f;
            return;
        }
        float right = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float forward = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        moveInput = Vector2.ClampMagnitude(new Vector2(right, forward), 1f);
        yawInput = (kb.eKey.isPressed ? 1f : 0f) - (kb.qKey.isPressed ? 1f : 0f);
        climbInput = (kb.spaceKey.isPressed ? 1f : 0f) -
                     ((kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed) ? 1f : 0f);
#elif ENABLE_LEGACY_INPUT_MANAGER
        float right = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float forward = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        moveInput = Vector2.ClampMagnitude(new Vector2(right, forward), 1f);
        yawInput = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
        climbInput = (Input.GetKey(KeyCode.Space) ? 1f : 0f) -
                     (Input.GetKey(KeyCode.LeftControl) ? 1f : 0f);
#else
        Debug.LogError("Enable the Input System or Legacy Input Manager in Player Settings.", this);
        enabled = false;
        return;
#endif
        // Space / Ctrl change the TARGET height; no keys means hold that target.
        targetAltitude += climbInput * climbSpeed * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (motors == null || rb.isKinematic) return;

        // Altitude controller: gravity compensation + position/vertical-speed feedback.
        float heightError = targetAltitude - rb.position.y;
        float correction = Mathf.Clamp(
            altitudeP * heightError - verticalD * rb.linearVelocity.y,
            -maxVerticalCorrection, maxVerticalCorrection);

        // Compensate for less vertical lift when the drone tilts.
        float upright = Vector3.Dot(transform.up, Vector3.up);
        float acceleration = Mathf.Max(0f, -Physics.gravity.y + correction);
        float totalThrust = upright > 0.35f
            ? rb.mass * acceleration / upright
            : 0f; // Do not attempt to 'hover' while inverted.

        motors.throttle = Mathf.Clamp01(totalThrust / (4f * maxMotorThrust));

        // Desired tilt relative to the drone's heading, not the camera.
        Quaternion heading = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 forward = heading * Vector3.forward;
        Vector3 right = heading * Vector3.right;
        float tilt = Mathf.Tan(maxTiltDegrees * Mathf.Deg2Rad);
        Vector3 desiredUp = (Vector3.up +
                             forward * (moveInput.y * tilt) +
                             right * (moveInput.x * tilt)).normalized;

        Vector3 tiltErrorWorld = Vector3.Cross(transform.up, desiredUp);
        Vector3 tiltErrorLocal = transform.InverseTransformDirection(tiltErrorWorld);
        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);

        // Signs correspond to the specific QuadcopterMotors motor mixer provided earlier.
        motors.pitch = Mathf.Clamp(
            -tiltErrorLocal.x * tiltP + localAngularVelocity.x * tiltD,
            -maxTiltMotorCorrection, maxTiltMotorCorrection);
        motors.roll = Mathf.Clamp(
            tiltErrorLocal.z * tiltP - localAngularVelocity.z * tiltD,
            -maxTiltMotorCorrection, maxTiltMotorCorrection);
        motors.yaw = Mathf.Clamp(
            (yawInput * maxYawRate - localAngularVelocity.y) * yawRateP,
            -maxYawMotorCorrection, maxYawMotorCorrection);
    }

    private void OnDisable()
    {
        if (motors == null) return;
        motors.throttle = 0f;
        motors.pitch = motors.roll = motors.yaw = 0f;
    }
}
