
using UnityEngine;
using Aerodynamics;

[RequireComponent(typeof(AerodynamicsController))]
public class WindReceiver : MonoBehaviour
{
    [SerializeField]
    private WindManager windManager;

    private AerodynamicsController controller;

    private void Awake()
    {
        controller = GetComponent<AerodynamicsController>();
    }

    private void OnEnable()
    {
        if (windManager == null)
        {
            Debug.LogError("WindManager is not assigned!", this);
            return;
        }

        windManager.WindChanged += OnWindChanged;

        // Получаем текущий ветер сразу после подписки
        OnWindChanged(windManager.CurrentWind);
    }

    private void OnDisable()
    {
        if (windManager != null)
            windManager.WindChanged -= OnWindChanged;
    }

    private void OnWindChanged(Vector3 wind)
    {
        Debug.Log("111111111111111111111");
        controller.WindVelocity = wind;
    }
}
