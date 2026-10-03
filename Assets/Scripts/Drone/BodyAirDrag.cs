
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BodyAirDrag : MonoBehaviour
{
    [Header("Wind")]
    [SerializeField] private WindManager windManager;

    [Header("Aerodynamics")]
    [SerializeField] private Transform body;

    [SerializeField]
    private Vector3 bodySize = new(0.2f, 0.06f, 0.2f);

    [SerializeField] private float airDensity = 1.225f;
    [SerializeField] private float dragCoefficient = 1.05f;

    private Rigidbody rb;
    private Vector3 windVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (body == null)
            body = transform;
    }

    private void OnEnable()
    {
        if (windManager == null)
        {
            Debug.LogError("WindManager not assigned!", this);
            return;
        }

        windManager.WindChanged += OnWindChanged;
        OnWindChanged(windManager.CurrentWind);
    }

    private void OnDisable()
    {
        if (windManager != null)
            windManager.WindChanged -= OnWindChanged;
    }

    private void OnWindChanged(Vector3 wind)
    {
        windVelocity = wind;
    }

    private void FixedUpdate()
    {
        // Воздух относительно движущегося дрона
        Vector3 relativeWind =
            windVelocity -
            rb.GetPointVelocity(rb.worldCenterOfMass);

        float speed = relativeWind.magnitude;

        if (speed < 0.001f)
            return;

        // Направление ветра в координатах корпуса
        Vector3 localDirection =
            body.InverseTransformDirection(
                relativeWind.normalized
            );

        // Площади проекций корпуса
        float areaX = bodySize.y * bodySize.z;
        float areaY = bodySize.x * bodySize.z;
        float areaZ = bodySize.x * bodySize.y;

        // Площадь, видимая воздушному потоку
        float effectiveArea =
            Mathf.Abs(localDirection.x) * areaX +
            Mathf.Abs(localDirection.y) * areaY +
            Mathf.Abs(localDirection.z) * areaZ;

        // Аэродинамическое сопротивление
        float forceMagnitude =
            0.5f *
            airDensity *
            dragCoefficient *
            effectiveArea *
            speed * speed;

        Vector3 force =
            relativeWind.normalized * forceMagnitude;

        rb.AddForce(force, ForceMode.Force);

        // Визуализация силы
        Debug.DrawRay(
            rb.worldCenterOfMass,
            force,
            Color.cyan
        );
    }
}
