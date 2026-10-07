using System;
using Enviro;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneLab.UI
{
    [Serializable]
    public sealed class DroneSettingsValues
    {
        [Range(0, 1)] public float environmentVolume = 1;
        [Range(0, 1)] public float droneVolume = 1;
        public bool fullscreen;
        public PilotDevice inputDevice;
        public int quality;
        public bool vSync;
        public DroneSettingsValues Copy() => (DroneSettingsValues)MemberwiseClone();
    }

    /// <summary>Persistent preferences and adapters for the real Enviro and pilot APIs.</summary>
    public sealed class DroneApplicationSettings : MonoBehaviour
    {
        private const string Key = "DroneLab.ApplicationSettings.v1";
        private static DroneApplicationSettings instance;
        public static DroneSettingsValues Current { get; private set; }
        public static event Action Changed;
        private EnviroAudioModule audioModule;
        private EnviroAudio originalAudio, runtimeAudio;
        private float ambientModifier, weatherModifier, thunderModifier;
        private float nextBind;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; Current = null; Changed = null; }

        public static void EnsureInitialized()
        {
            if (instance != null) return;
            Current = new DroneSettingsValues
            {
                fullscreen = Screen.fullScreen, quality = QualitySettings.GetQualityLevel(),
                vSync = QualitySettings.vSyncCount > 0
            };
            bool saved = PlayerPrefs.HasKey(Key);
            if (saved)
            {
                try { JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(Key), Current); }
                catch (ArgumentException) { PlayerPrefs.DeleteKey(Key); saved = false; }
            }
            Sanitize(Current);
            instance = new GameObject("DroneLab Application Settings").AddComponent<DroneApplicationSettings>();
            DontDestroyOnLoad(instance.gameObject);
            SceneManager.sceneLoaded += instance.OnSceneLoaded;
            if (saved) instance.ApplyDisplay();
            instance.BindScene();
        }

        private static void Sanitize(DroneSettingsValues values)
        {
            values.environmentVolume = Mathf.Clamp01(values.environmentVolume);
            values.droneVolume = Mathf.Clamp01(values.droneVolume);
            values.quality = Mathf.Clamp(values.quality, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            if (values.inputDevice != PilotDevice.Gamepad) values.inputDevice = PilotDevice.Keyboard;
        }

        public static void Save(DroneSettingsValues values)
        {
            EnsureInitialized();
            Current = values.Copy(); Sanitize(Current);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current)); PlayerPrefs.Save();
            instance.ApplyDisplay(); instance.BindScene(); Changed?.Invoke();
        }

        private void ApplyDisplay()
        {
            if (QualitySettings.GetQualityLevel() != Current.quality) QualitySettings.SetQualityLevel(Current.quality, true);
            QualitySettings.vSyncCount = Current.vSync ? 1 : 0;
            Screen.fullScreen = Current.fullscreen;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => BindScene();
        private void Update()
        {
            // Finds Enviro or drones instantiated after scene loading as well.
            if (Time.unscaledTime >= nextBind) { nextBind = Time.unscaledTime + .5f; BindScene(); }
        }

        private void BindScene()
        {
            foreach (var pilot in FindObjectsByType<DroneTestPilot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                pilot.inputDevice = Current.inputDevice;
                // Register existing rotor audio. No synthetic sound or physics is introduced.
                if (pilot.GetComponent<DroneAudioVolume>() == null && pilot.GetComponentsInChildren<AudioSource>(true).Length > 0)
                    pilot.gameObject.AddComponent<DroneAudioVolume>();
            }
            var manager = EnviroManager.instance;
            var module = manager != null ? manager.Audio : null;
            if (audioModule != module || (audioModule != null && !ReferenceEquals(audioModule.Settings, runtimeAudio)))
            {
                RestoreEnviro(); audioModule = module;
                if (module != null && module.Settings != null)
                {
                    originalAudio = module.Settings;
                    runtimeAudio = new EnviroAudio
                    {
                        ambientClips = originalAudio.ambientClips, weatherClips = originalAudio.weatherClips,
                        thunderClips = originalAudio.thunderClips
                    };
                    module.Settings = runtimeAudio;
                    ambientModifier = module.ambientVolumeModifier;
                    weatherModifier = module.weatherVolumeModifier;
                    thunderModifier = module.thunderVolumeModifier;
                }
            }
            if (audioModule == null || originalAudio == null || runtimeAudio == null) return;
            // Enviro applies thunder modifiers after clip gain, unlike weather/ambient.
            // Scale both runtime master gains and modifiers to cover all three channels.
            float gain = Current.environmentVolume;
            runtimeAudio.ambientMasterVolume = originalAudio.ambientMasterVolume * gain;
            runtimeAudio.weatherMasterVolume = originalAudio.weatherMasterVolume * gain;
            runtimeAudio.thunderMasterVolume = originalAudio.thunderMasterVolume * gain;
            audioModule.ambientVolumeModifier = ambientModifier * gain;
            audioModule.weatherVolumeModifier = weatherModifier * gain;
            audioModule.thunderVolumeModifier = thunderModifier * gain;
            // Thunder is a one-shot; adjust a clip already playing as well.
            foreach (var clip in runtimeAudio.thunderClips)
                if (clip != null && clip.myAudioSource != null)
                    clip.myAudioSource.volume = Mathf.Clamp01((clip.volume * originalAudio.thunderMasterVolume + thunderModifier) * gain);
        }

        private void RestoreEnviro()
        {
            if (audioModule == null) return;
            audioModule.ambientVolumeModifier = ambientModifier;
            audioModule.weatherVolumeModifier = weatherModifier;
            audioModule.thunderVolumeModifier = thunderModifier;
            if (ReferenceEquals(audioModule.Settings, runtimeAudio)) audioModule.Settings = originalAudio;
            originalAudio = runtimeAudio = null;
        }
        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded; RestoreEnviro(); instance = null;
        }
    }
}
