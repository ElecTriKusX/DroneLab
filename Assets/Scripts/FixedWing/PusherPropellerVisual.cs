using UnityEngine;

public class PusherPropellerVisual : MonoBehaviour
{
    [SerializeField] private FixedWingKeyboardController controller;
    [SerializeField] private Vector3 localRotationAxis = Vector3.forward;
    [SerializeField, Min(0f)] private float maxVisualRpm = 9000f;

    private void Update()
    {
        if (controller == null)
            return;

        float rpm = controller.Throttle * maxVisualRpm;

        transform.Rotate(
            localRotationAxis.normalized,
            rpm * 6f * Time.deltaTime,
            Space.Self
        );
    }
}