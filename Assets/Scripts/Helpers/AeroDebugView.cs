
using UnityEngine;
using Aerodynamics;

public class AeroDebugView : MonoBehaviour
{
    public AerodynamicsController controller;
    public AeroSurface wing;

    private float nextLogTime;

    void Update()
    {
        if (controller == null || wing == null)
            return;

        Vector3 force = controller.CurrentForce;
        Vector3 position = wing.transform.position;

        // Зелёный вектор — аэродинамическая сила
        Debug.DrawRay(
            position,
            force * 0.02f,
            Color.green
        );

        // Голубой вектор — направление ветра
        Debug.DrawRay(
            position,
            controller.WindVelocity * 0.2f,
            Color.cyan
        );

        if (Time.time >= nextLogTime)
        {
            Debug.Log(
                $"Угол атаки: {wing.LastAoADeg:F2}°\n" +
                $"Сила: {force} Н\n" +
                $"Число Рейнольдса: " +
                $"{wing.LastReynoldsNumber:F0}"
            );

            nextLogTime = Time.time + 0.5f;
        }
    }
}
