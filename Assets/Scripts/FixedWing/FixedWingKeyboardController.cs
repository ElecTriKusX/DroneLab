using UnityEngine;
using Aerodynamics;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(AerodynamicsController))]
public class FixedWingKeyboardController : MonoBehaviour
{
    [SerializeField] private AerodynamicsController aerodynamicsController;

    [Header("Throttle")]
    [SerializeField, Min(0.01f)] private float throttleChangePerSecond = 0.35f;
    [SerializeField, Range(0f, 1f)] private float initialThrottle = 0f;

    [Header("Control smoothing")]
    [SerializeField, Min(0.01f)] private float controlSlewRate = 5f;

    [Header("Authority")]
    [SerializeField, Range(0f, 1f)] private float pitchAuthority = 1f;
    [SerializeField, Range(0f, 1f)] private float rollAuthority = 1f;
    [SerializeField, Range(0f, 1f)] private float yawAuthority = 0.65f;

    [Header("Flaps")]
    [SerializeField, Range(0f, 1f)] private float takeoffFlaps = 0.35f;
    [SerializeField, Range(0f, 1f)] private float landingFlaps = 0.75f;

    private float throttle;
    private float pitch;
    private float roll;
    private float yaw;
    private float flap;

    public float Throttle => throttle;
    public float Pitch => pitch;
    public float Roll => roll;
    public float Yaw => yaw;
    public float Flap => flap;

    private void Awake()
    {
        if (aerodynamicsController == null)
            aerodynamicsController = GetComponent<AerodynamicsController>();

        throttle = initialThrottle;
    }

    private void Update()
    {
        ReadKeyboard(out float pitchTarget,
                     out float rollTarget,
                     out float yawTarget,
                     out bool throttleUp,
                     out bool throttleDown,
                     out bool flapPressed,
                     out bool throttleCutPressed);

        float throttleDirection =
            (throttleUp ? 1f : 0f) -
            (throttleDown ? 1f : 0f);

        throttle = Mathf.Clamp01(
            throttle + throttleDirection * throttleChangePerSecond * Time.deltaTime
        );

        if (throttleCutPressed)
            throttle = 0f;

        pitch = Mathf.MoveTowards(
            pitch, pitchTarget * pitchAuthority,
            controlSlewRate * Time.deltaTime);

        roll = Mathf.MoveTowards(
            roll, rollTarget * rollAuthority,
            controlSlewRate * Time.deltaTime);

        yaw = Mathf.MoveTowards(
            yaw, yawTarget * yawAuthority,
            controlSlewRate * Time.deltaTime);

        if (flapPressed)
            CycleFlaps();

        aerodynamicsController.ThrustPercent = throttle;
        aerodynamicsController.SetControlInputs(pitch, roll, yaw, flap);
    }

    private void ReadKeyboard(
        out float pitchTarget,
        out float rollTarget,
        out float yawTarget,
        out bool throttleUp,
        out bool throttleDown,
        out bool flapPressed,
        out bool throttleCutPressed)
    {
        pitchTarget = rollTarget = yawTarget = 0f;
        throttleUp = throttleDown = flapPressed = throttleCutPressed = false;

#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        pitchTarget =
            (kb.sKey.isPressed ? 1f : 0f) -
            (kb.wKey.isPressed ? 1f : 0f);

        rollTarget =
            (kb.dKey.isPressed ? 1f : 0f) -
            (kb.aKey.isPressed ? 1f : 0f);

        yawTarget =
            (kb.eKey.isPressed ? 1f : 0f) -
            (kb.qKey.isPressed ? 1f : 0f);

        throttleUp = kb.spaceKey.isPressed;
        throttleDown = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
        flapPressed = kb.fKey.wasPressedThisFrame;
        throttleCutPressed = kb.backspaceKey.wasPressedThisFrame;

#elif ENABLE_LEGACY_INPUT_MANAGER
        pitchTarget =
            (Input.GetKey(KeyCode.S) ? 1f : 0f) -
            (Input.GetKey(KeyCode.W) ? 1f : 0f);

        rollTarget =
            (Input.GetKey(KeyCode.D) ? 1f : 0f) -
            (Input.GetKey(KeyCode.A) ? 1f : 0f);

        yawTarget =
            (Input.GetKey(KeyCode.E) ? 1f : 0f) -
            (Input.GetKey(KeyCode.Q) ? 1f : 0f);

        throttleUp = Input.GetKey(KeyCode.Space);
        throttleDown =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        flapPressed = Input.GetKeyDown(KeyCode.F);
        throttleCutPressed = Input.GetKeyDown(KeyCode.Backspace);
#endif
    }

    private void CycleFlaps()
    {
        const float eps = 0.01f;

        if (flap < eps)
            flap = takeoffFlaps;
        else if (flap < landingFlaps - eps)
            flap = landingFlaps;
        else
            flap = 0f;
    }

    public void SetThrottle(float value) => throttle = Mathf.Clamp01(value);

    public void SetControls(float pitchInput, float rollInput, float yawInput)
    {
        pitch = Mathf.Clamp(pitchInput, -1f, 1f);
        roll = Mathf.Clamp(rollInput, -1f, 1f);
        yaw = Mathf.Clamp(yawInput, -1f, 1f);
    }

    public void SetFlaps(float value) => flap = Mathf.Clamp01(value);

    private void OnDisable()
    {
        if (aerodynamicsController == null)
            return;

        aerodynamicsController.ThrustPercent = 0f;
        aerodynamicsController.SetControlInputs(0f, 0f, 0f, 0f);
    }
}