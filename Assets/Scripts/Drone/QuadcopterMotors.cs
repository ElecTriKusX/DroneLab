
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class QuadcopterMotors : MonoBehaviour
{
    [Header("Motors")]
    [SerializeField] private Transform frontLeft;
    [SerializeField] private Transform frontRight;
    [SerializeField] private Transform backLeft;
    [SerializeField] private Transform backRight;

    [Header("Motor physics")]
    [SerializeField]
    private float maxMotorThrust = 5f; // Ньютонов

    [SerializeField]
    private float reactionTorquePerNewton = 0.01f;

    [Header("Controls")]
    [Range(0f, 1f)]
    public float throttle = 0.5f;

    [Range(-0.2f, 0.2f)]
    public float pitch;

    [Range(-0.2f, 0.2f)]
    public float roll;

    [Range(-0.2f, 0.2f)]
    public float yaw;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        float fl = Mathf.Clamp01(
            throttle + pitch - roll + yaw
        );

        float fr = Mathf.Clamp01(
            throttle + pitch + roll - yaw
        );

        float bl = Mathf.Clamp01(
            throttle - pitch - roll - yaw
        );

        float br = Mathf.Clamp01(
            throttle - pitch + roll + yaw
        );

        ApplyMotor(frontLeft, fl, 1f);
        ApplyMotor(frontRight, fr, -1f);
        ApplyMotor(backLeft, bl, -1f);
        ApplyMotor(backRight, br, 1f);

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log(
                $"FL: {transform.InverseTransformPoint(frontLeft.position)}\n" +
                $"FR: {transform.InverseTransformPoint(frontRight.position)}\n" +
                $"BL: {transform.InverseTransformPoint(backLeft.position)}\n" +
                $"BR: {transform.InverseTransformPoint(backRight.position)}"
            );
        }
    }

    private void ApplyMotor(
        Transform motor,
        float power,
        float rotationSign)
    {
        if (motor == null)
            return;

        float thrust = power * maxMotorThrust;

        // Подъёмная сила в точке двигателя
        Vector3 force = motor.up * thrust;

        rb.AddForceAtPosition(
            force,
            motor.position,
            ForceMode.Force
        );

        // Реактивный крутящий момент пропеллера
        rb.AddTorque(
            motor.up * rotationSign *
            thrust * reactionTorquePerNewton,
            ForceMode.Force
        );

        // Визуализация тяги
        Debug.DrawRay(
            motor.position,
            force * 0.05f,
            Color.green
        );
    }
}
