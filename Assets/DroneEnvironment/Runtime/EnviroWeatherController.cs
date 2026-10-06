using System;
using DroneLab.Physics;
using DroneLab.Simulation;
using Enviro;
using UnityEngine;

namespace DroneLab.Weather
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-500)]
    public sealed class EnviroWeatherController : MonoBehaviour, IWindProvider
    {
        public EnviroManager manager;
        public DronePhysicsBody[] droneTargets = Array.Empty<DronePhysicsBody>();
        [Tooltip("Explicit conversion: Enviro windSpeed=1 corresponds to this many m/s. Not a measured Enviro unit.")]
        [Min(0.001f)] public float fullStrengthWindMps = 20;
        [Tooltip("Clockwise angle of scene north from world +Z.")]
        public float northYawDegrees;
        public bool manualWeatherControl = true;
        public bool showEngineeringWindow = true;
        public bool overrideWind;
        [Min(0)] public float manualWindMps = 5;
        [Range(0, 360)] public float manualWindFromDegrees;

        public WeatherSnapshot Current { get; private set; }
        public string Status { get; private set; } = "Waiting for the first Enviro frame";
        public bool IsReady => Current != null && string.IsNullOrEmpty(Status);
        private EnviroWeatherModule ownedWeather;
        private bool previousAutoWeather;
        private EnviroManager capturedManager;
        private Rect windowRect = new Rect(10, 250, 350, 440);

        private void Awake()
        {
            if (manager == null) manager = FindFirstObjectByType<EnviroManager>();
            foreach (var body in droneTargets ?? Array.Empty<DronePhysicsBody>())
            {
                if (body == null) continue;
                if ((body.CustomWindProvider != null && !ReferenceEquals(body.CustomWindProvider, this)) ||
                    (body.customWindProvider != null && body.customWindProvider != this))
                {
                    Debug.LogError("Drone already has another wind provider; binding was not replaced.", body);
                    continue;
                }
                if (body.IsReady && !ReferenceEquals(body.CustomWindProvider, this) && body.customWindProvider != this)
                {
                    Debug.LogError("Connect weather before the drone initializes. Restart Play Mode after binding.", body);
                    continue;
                }
                // Must run before DronePhysicsBody.Awake/Initialize. Never initialize a flying drone here.
                body.CustomWindProvider = this;
            }
        }

        private void LateUpdate()
        {
            // All Start/Update methods (including Enviro's runtime module cloning) have completed.
            if (manager == null || manager != EnviroManager.instance || !manager.isActiveAndEnabled || manager.Environment == null ||
                !manager.Environment.active || manager.Environment.Settings == null ||
                manager.Weather == null || !manager.Weather.active || manager.Weather.Settings == null)
            {
                Reject("Enable the Enviro Manager, Environment and Weather modules");
                return;
            }
            try
            {
                if (manualWeatherControl) AcquireWeatherControl(); else ReleaseWeatherControl();
                var settings = manager.Environment.Settings;
                if (overrideWind)
                {
                    var wind = WindConversion.FromMeteorological(manualWindMps, manualWindFromDegrees, northYawDegrees);
                    var mapped = WindConversion.ToEnviro(wind, fullStrengthWindMps);
                    settings.windSpeed = (float)mapped.Strength;
                    settings.windDirectionX = (float)mapped.DirectionX;
                    settings.windDirectionY = (float)mapped.DirectionZ;
                    // Environment.UpdateWindZone ran before the override; update the same visual zone now.
                    var zone = manager.Objects != null ? manager.Objects.windZone : null;
                    if (zone != null)
                    {
                        zone.windMain = settings.windSpeed;
                        zone.windTurbulence = settings.windTurbulence;
                        zone.transform.forward = new Vector3(-settings.windDirectionX, 0, -settings.windDirectionY);
                    }
                }
                var velocity = WindConversion.FromEnviro(settings.windSpeed, settings.windDirectionX,
                    settings.windDirectionY, fullStrengthWindMps);
                var snapshot = new WeatherSnapshot(manager.Weather.targetWeatherType != null ?
                    manager.Weather.targetWeatherType.name : "", velocity, WindConversion.CelsiusToKelvin(settings.temperature),
                    settings.wetness, settings.snow, settings.windTurbulence, Time.timeAsDouble);
                Current = snapshot; capturedManager = manager; Status = "";
            }
            catch (ArgumentException ex) { Reject(ex.Message); }
        }

        private void Reject(string message)
        {
            ReleaseWeatherControl();
            if (Status != message) Debug.LogWarning("Weather bridge: " + message, this);
            Current = null; capturedManager = null; Status = message;
        }
        private void AcquireWeatherControl()
        {
            if (ownedWeather != manager.Weather)
            {
                ReleaseWeatherControl();
                ownedWeather = manager.Weather;
                previousAutoWeather = ownedWeather.globalAutoWeatherChange;
            }
            ownedWeather.globalAutoWeatherChange = false;
        }
        private void ReleaseWeatherControl()
        {
            if (ownedWeather != null) ownedWeather.globalAutoWeatherChange = previousAutoWeather;
            ownedWeather = null;
        }
        private void OnDisable()
        { ReleaseWeatherControl(); Current = null; capturedManager = null; Status = "Weather bridge disabled"; }

        public DVector3 Sample(DVector3 worldPositionM, double timeS)
        {
            EnvironmentMath.Finite(worldPositionM); EnvironmentMath.Finite(timeS);
            if (timeS < 0) throw new ArgumentOutOfRangeException(nameof(timeS));
            // No Enviro queries, mutations, random numbers or HTTP inside a physics query.
            return Current != null ? Current.WindWorldMps : default;
        }
        private void RequireRuntime()
        {
            if (!Application.isPlaying || !IsReady || capturedManager != manager)
                throw new InvalidOperationException("Wait for the first valid Enviro frame before changing weather.");
        }
        public int WeatherPresetCount => manager != null && manager.Weather != null && manager.Weather.Settings != null ?
            (manager.Weather.Settings.weatherTypes?.Count ?? 0) : 0;
        public string GetWeatherName(int index)
        {
            if (index < 0 || index >= WeatherPresetCount) throw new ArgumentOutOfRangeException(nameof(index));
            var weather = manager.Weather.Settings.weatherTypes[index];
            return weather != null ? weather.name : "Missing preset";
        }
        public void SelectWeather(int index, bool instant = false)
        {
            RequireRuntime();
            if (index < 0 || index >= WeatherPresetCount) throw new ArgumentOutOfRangeException(nameof(index));
            var preset = manager.Weather.Settings.weatherTypes[index];
            if (preset == null) throw new ArgumentException("Weather preset is missing.");
            manualWeatherControl = true; AcquireWeatherControl();
            if (instant) manager.Weather.ChangeWeatherInstant(preset); else manager.Weather.ChangeWeather(preset);
        }
        public void SetManualWind(double speedMps, double fromDegrees)
        {
            // Validate completely before changing the public request; no silent wind-speed clipping.
            WindConversion.ToEnviro(WindConversion.FromMeteorological(speedMps, fromDegrees, northYawDegrees), fullStrengthWindMps);
            manualWindMps = (float)speedMps; manualWindFromDegrees = (float)(fromDegrees % 360);
            overrideWind = true;
        }
        public void UsePresetWind() { overrideWind = false; }
        public void SetTimeOfDay(double hours, bool simulate = false)
        {
            RequireRuntime(); EnvironmentMath.Finite(hours);
            if (hours < 0 || (float)hours >= 24) throw new ArgumentOutOfRangeException(nameof(hours));
            if (manager.Time == null || !manager.Time.active || manager.Time.Settings == null)
                throw new InvalidOperationException("Enable the Time module.");
            manager.Time.Settings.simulate = simulate;
            manager.Time.SetTimeOfDay((float)hours);
        }

        private void OnGUI()
        {
            if (!showEngineeringWindow) return;
            windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "DroneLab Weather / Wind");
        }
        private void DrawWindow(int id)
        {
            GUILayout.Label(IsReady ? Current.WeatherName : Status);
            if (Current != null)
            {
                var w = Current.WindWorldMps;
                GUILayout.Label($"Wind {w.Length:F2} m/s | ({w.X:F2}, {w.Y:F2}, {w.Z:F2})");
                GUILayout.Label($"Enviro air {Current.TemperatureK:F2} K | wetness {Current.Wetness:F2} | snow {Current.SnowCover:F2}");
            }
            GUILayout.Label("Physics air/temperature stays in its JSON profile.");
            bool oldEnabled = GUI.enabled; GUI.enabled = IsReady;
            for (int i = 0; i < WeatherPresetCount; i++)
                if (GUILayout.Button(GetWeatherName(i))) SelectWeather(i);
            overrideWind = GUILayout.Toggle(overrideWind, "Manual wind override");
            if (overrideWind)
            {
                GUILayout.Label($"Speed {manualWindMps:F1} m/s (full scale {fullStrengthWindMps:F1})");
                manualWindMps = GUILayout.HorizontalSlider(manualWindMps, 0, Mathf.Max(.001f, fullStrengthWindMps));
                GUILayout.Label($"Wind FROM {manualWindFromDegrees:F0} degrees");
                manualWindFromDegrees = GUILayout.HorizontalSlider(manualWindFromDegrees, 0, 360);
            }
            GUI.enabled = oldEnabled;
            foreach (var body in droneTargets ?? Array.Empty<DronePhysicsBody>())
                if (body != null) GUILayout.Label(body.IsReady && body.Parameters.Environment.WindEnabled &&
                    body.Parameters.Environment.WindMode == "CustomField" &&
                    (ReferenceEquals(body.CustomWindProvider, this) || body.customWindProvider == this) ?
                    body.name + ": CustomField" : body.name + ": check profile/provider");
            GUI.DragWindow();
        }
    }
}
