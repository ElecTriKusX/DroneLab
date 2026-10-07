using UnityEngine;

namespace DroneLab.UI
{
    /// <summary>Applies drone-channel gain to existing AudioSources, preserving their base volume.</summary>
    [DisallowMultipleComponent]
    public sealed class DroneAudioVolume : MonoBehaviour
    {
        [Tooltip("Optional explicit rotor sources. If empty, uses sources under this drone.")]
        [SerializeField] private AudioSource[] sources;
        private float[] baseVolumes;
        private void Awake()
        {
            if (sources == null || sources.Length == 0) sources = GetComponentsInChildren<AudioSource>(true);
            baseVolumes = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++) if (sources[i] != null) baseVolumes[i] = sources[i].volume;
        }
        private void OnEnable() { DroneApplicationSettings.Changed += Apply; Apply(); }
        private void OnDisable()
        {
            DroneApplicationSettings.Changed -= Apply;
            for (int i = 0; i < sources.Length; i++) if (sources[i] != null) sources[i].volume = baseVolumes[i];
        }
        public void SetBaseVolume(int index, float volume)
        {
            if (index < 0 || index >= baseVolumes.Length) return;
            baseVolumes[index] = Mathf.Clamp01(volume); Apply();
        }
        private void Apply()
        {
            float gain = DroneApplicationSettings.Current?.droneVolume ?? 1;
            for (int i = 0; i < sources.Length; i++) if (sources[i] != null) sources[i].volume = baseVolumes[i] * gain;
        }
    }
}
