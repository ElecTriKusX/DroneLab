
using System;
using UnityEngine;

public class WindManager : MonoBehaviour
{
    [SerializeField]
    private Vector3 windVelocity = new(0, 0, 10);

    private Vector3 appliedWind;
    private bool initialized;

    public Vector3 CurrentWind =>
        initialized ? appliedWind : windVelocity;

    public event Action<Vector3> WindChanged;

    private void Awake()
    {
        appliedWind = windVelocity;
        initialized = true;
    }

    public void SetWind(Vector3 newWind)
    {
        windVelocity = newWind;

        if (initialized && appliedWind == newWind)
            return;

        appliedWind = newWind;
        initialized = true;

        WindChanged?.Invoke(newWind);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += ApplyInspectorWind;
    }

    private void ApplyInspectorWind()
    {
        if (this == null || !Application.isPlaying)
            return;

        SetWind(windVelocity);
    }
#endif
}
