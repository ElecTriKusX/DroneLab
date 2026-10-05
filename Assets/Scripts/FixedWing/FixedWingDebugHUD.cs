using UnityEngine;
using Aerodynamics;

[RequireComponent(typeof(Rigidbody))]
public class FixedWingDebugHUD : MonoBehaviour
{
    [SerializeField] private FixedWingKeyboardController controller;
    [SerializeField] private AerodynamicsController aerodynamicsController;

    [Header("Optional surfaces")]
    [SerializeField] private AeroSurface leftWing;
    [SerializeField] private AeroSurface rightWing;
    [SerializeField] private AeroSurface horizontalTail;
    [SerializeField] private AeroSurface verticalTail;

    [SerializeField] private bool show = true;

    private Rigidbody rb;
    private GUIStyle style;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (controller == null)
            controller = GetComponent<FixedWingKeyboardController>();

        if (aerodynamicsController == null)
            aerodynamicsController = GetComponent<AerodynamicsController>();
    }

    private void OnGUI()
    {
        if (!show)
            return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                padding = new RectOffset(10, 10, 8, 8)
            };
        }

        Vector3 localVelocity =
            transform.InverseTransformDirection(rb.linearVelocity);

        string text =
            $"FIXED WING\n" +
            $"Throttle: {(controller != null ? controller.Throttle : 0f):F2}\n" +
            $"Pitch/Roll/Yaw: {(controller != null ? controller.Pitch : 0f):F2} / " +
            $"{(controller != null ? controller.Roll : 0f):F2} / " +
            $"{(controller != null ? controller.Yaw : 0f):F2}\n" +
            $"Flaps: {(controller != null ? controller.Flap : 0f):F2}\n" +
            $"Speed: {rb.linearVelocity.magnitude:F1} m/s\n" +
            $"Forward speed: {localVelocity.z:F1} m/s\n" +
            $"Vertical speed: {rb.linearVelocity.y:F1} m/s\n" +
            $"Aero force: {(aerodynamicsController != null ? aerodynamicsController.CurrentForce.magnitude : 0f):F1} N\n" +
            $"Aero torque: {(aerodynamicsController != null ? aerodynamicsController.CurrentTorque.magnitude : 0f):F2} N m\n" +
            $"AoA L/R: {AoA(leftWing):F1} / {AoA(rightWing):F1} deg\n" +
            $"AoA H/V: {AoA(horizontalTail):F1} / {AoA(verticalTail):F1} deg";

        GUI.Box(new Rect(12f, 12f, 410f, 225f), text, style);
    }

    private static float AoA(AeroSurface surface)
    {
        return surface != null ? surface.LastAoADeg : 0f;
    }
}