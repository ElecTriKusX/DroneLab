using System;
using System.Linq;
using System.Collections.Generic;
using DroneLab.Physics;
using DroneLab.Simulation;
using DroneLab.Weather;
using Enviro;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DroneLab.UI
{
    /// <summary>Visual consumer of the same validated wind/air used by physics; adds no forces.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class DroneScenarioEnvironmentDriver : MonoBehaviour
    {
        private DroneEnvironmentDocument document;
        private EnviroWeatherController bridge;
        private DronePhysicsBody body;
        private EnviroWeatherType weatherClone;
        private bool applied;
        private DroneScenarioCatalog catalog;
        public void Configure(DroneEnvironmentDocument profile, DroneScenarioCatalog sourceCatalog, DronePhysicsBody selectedBody = null) { document = profile.Copy(); catalog = sourceCatalog; body = selectedBody; }
        private void LateUpdate()
        {
            if (document == null) return;
            if (bridge == null) bridge = FindFirstObjectByType<EnviroWeatherController>();
            if (body == null) body = FindFirstObjectByType<DronePhysicsBody>();
            if (bridge == null || !bridge.IsReady || bridge.manager == null) return;
            try {
                if (!applied) ApplyPreset();
                IWindProvider windSource = body != null && body.IsReady ?
                    (body.RuntimeWindOverride ?? (body.Parameters.Environment.WindMode == "CustomField" ? body.CustomWindProvider ?? body.customWindProvider as IWindProvider : body.Parameters.Environment)) : null;
                if (body != null && body.IsReady && windSource != null && !ReferenceEquals(windSource, bridge)) {
                    var wind = windSource.Sample(DronePhysicsBody.FromUnity(body.transform.position), body.SimulationTimeS);
                    var horizontal = new DVector3(wind.X, 0, wind.Z);
                    // Visual normalisation is explicitly bounded; physics retains the full vector and speed.
                    var env = bridge.manager.Environment.Settings;
                    env.windSpeed = Mathf.Clamp01((float)(horizontal.Length / Math.Max(.001f, bridge.fullStrengthWindMps)));
                    if (horizontal.Length > 1e-9) { env.windDirectionX = (float)(-horizontal.X / horizontal.Length); env.windDirectionY = (float)(-horizontal.Z / horizontal.Length); }
                    if (weatherClone != null) {
                        weatherClone.environmentOverride.windSpeed = env.windSpeed;
                        weatherClone.environmentOverride.windDirectionX = env.windDirectionX;
                        weatherClone.environmentOverride.windDirectionY = env.windDirectionY;
                    }
                    var air = body.Parameters.Environment.SampleAir(0);
                    env.temperature = (float)(air.TemperatureK - 273.15);
                    bridge.overrideTemperature = true; bridge.manualTemperatureC = env.temperature;
                    bridge.referencePressurePa = (float)air.PressurePa;
                    bridge.referenceAltitudeM = (float)air.AltitudeM;
                    bridge.referenceWorldY = (float)body.EnvironmentReferenceWorldY;
                    var zone = bridge.manager.Objects?.windZone;
                    if (zone != null) { zone.windMain = env.windSpeed; zone.windTurbulence = env.windTurbulence;
                        if (horizontal.Length > 1e-9) zone.transform.forward = new Vector3((float)horizontal.X, 0, (float)horizontal.Z); }
                }
            } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException) {
                Debug.LogError("DroneLab profile visuals: " + ex.Message, this); enabled = false;
            }
        }
        private void ApplyPreset()
        {
            var manager = bridge.manager; var visual = document.visual;
            var registered = catalog?.Weather((string)visual["weatherPresetId"]);
            var source = registered?.preset ?? manager.Weather.targetWeatherType;
            if (source == null) throw new ArgumentException("Нет погодного пресета в сцене.");
            weatherClone = Instantiate(source); weatherClone.name = document.name + " (runtime)";
            weatherClone.environmentOverride = weatherClone.environmentOverride ?? new EnviroWeatherTypeEnvironmentOverride();
            weatherClone.cloudsOverride = weatherClone.cloudsOverride ?? new EnviroWeatherTypeCloudsOverride();
            weatherClone.effectsOverride = weatherClone.effectsOverride ?? new EnviroWeatherTypeEffectsOverride();
            weatherClone.fogOverride = weatherClone.fogOverride ?? new EnviroWeatherTypeFogOverride();
            weatherClone.lightingOverride = weatherClone.lightingOverride ?? new EnviroWeatherTypeLightingOverride();
            weatherClone.skyOverride = weatherClone.skyOverride ?? new EnviroWeatherTypeSkyOverride();
            weatherClone.flatCloudsOverride = weatherClone.flatCloudsOverride ?? new EnviroWeatherTypeFlatCloudsOverride();
            weatherClone.auroraOverride = weatherClone.auroraOverride ?? new EnviroWeatherTypeAuroraOverride();
            weatherClone.audioOverride = weatherClone.audioOverride ?? new EnviroWeatherTypeAudioOverride();
            weatherClone.lightningOverride = weatherClone.lightningOverride ?? new EnviroWeatherTypeLightningOverride();
            // Only this private clone is edited. Project Enviro assets and Forest tuning stay intact.
            Set(visual, "windTurbulence", x => weatherClone.environmentOverride.windTurbulence = x);
            Set(visual, "wetness", x => weatherClone.environmentOverride.wetnessTarget = x);
            Set(visual, "snow", x => weatherClone.environmentOverride.snowTarget = x);
            Set(visual, "cloudCoverage", x => weatherClone.cloudsOverride.coverage = x);
            Set(visual, "cloudDensity", x => weatherClone.cloudsOverride.density = x);
#if ENVIRO_HDRP
            Set(visual, "fogDistance", x => weatherClone.fogOverride.fogAttenuationDistance = x);
            Set(visual, "fogBaseHeight", x => weatherClone.fogOverride.baseHeight = x);
            Set(visual, "fogMaxHeight", x => weatherClone.fogOverride.maxHeight = x);
#endif
#if ENVIRO_HDRP
            if (weatherClone.fogOverride.maxHeight <= weatherClone.fogOverride.baseHeight)
                throw new ArgumentException("Верхняя граница тумана должна быть выше нижней в выбранной основе.");
#endif
            var e = DroneEnvironmentProfiles.Effective(document.environment);
            var airMode = (string)e["airDensityMode"];
            var temperature = (double)e["temperatureK"];
            var pressure = (double)e["pressurePa"];
            var air = airMode == "StandardAtmosphere" ? Atmosphere.Troposphere((double)e["altitudeM"], temperature, pressure) :
                new AirSample((double)e["airDensityKgM3"], temperature, pressure, (double)e["altitudeM"]);
            bridge.syncAir = false; bridge.manualWeatherControl = true;
            bridge.SetAirReference(air.PressurePa, air.AltitudeM, body != null ? body.EnvironmentReferenceWorldY : 0);
            bridge.SetManualTemperature(air.TemperatureK - 273.15);
            bridge.UsePresetWind();
            var weather = e["weather"];
            if (weather != null) {
                var precipitation = (string)weather["precipitation"];
                float intensity = (float)weather["intensityMmPerHour"];
                bridge.weatherBindings = new[] { new WeatherPrecipitationBinding { preset = weatherClone,
                    precipitation = (PrecipitationKind)Enum.Parse(typeof(PrecipitationKind), precipitation), intensityMmPerHour = intensity } };
                // Use the scene's available effects, even when the sky preset has no rain/snow overrides.
                // This makes precipitation independent of the selected sky/cloud/fog foundation.
                var effects = manager.Effects?.Settings?.effectTypes;
                var overrides = weatherClone.effectsOverride.effectsOverride ?? new List<EnviroEffectsOverrideType>();
                weatherClone.effectsOverride.effectsOverride = overrides;
                if (effects != null) foreach (var effect in effects.Where(x => x != null)) {
                    string kind = EffectKind(effect.name);
                    if (kind == "None") continue;
                    var target = overrides.Find(x => x != null && x.name == effect.name);
                    if (target == null) { target = new EnviroEffectsOverrideType { name = effect.name }; overrides.Add(target); }
                }
                bool available = false;
                foreach (var effect in overrides.Where(x => x != null)) {
                    string kind = EffectKind(effect.name); if (kind == "None") continue;
                    bool selected = kind == precipitation;
                    effect.emission = selected ? Mathf.Clamp01(intensity / 20f) : 0;
                    if (selected && effects != null) available |= effects.Any(x => x != null && x.name == effect.name && (x.prefab != null || x.prefabVFXGraph != null));
                }
                if (precipitation != "None" && (manager.Effects == null || !manager.Effects.active || !available))
                    Debug.LogWarning("DroneLab: для выбранных осадков нет активного эффекта осадков в сцене. Физический профиль и метаданные сохранены; настройте модуль Effects / имена эффектов в каталоге.",this);
            }
            manager.Weather.ChangeWeatherInstant(weatherClone);
            if (manager.Time != null && manager.Time.active) bridge.SetTimeOfDay((double)visual["timeOfDay"], (bool)visual["simulateTime"]);
            else Debug.LogWarning("DroneLab: модуль времени выключен; время профиля не применено.", this);
            applied = true;
        }
        private string EffectKind(string name)
        {
            string key = (name ?? "").ToLowerInvariant();
            bool Matches(string configured) => !string.IsNullOrWhiteSpace(configured) && string.Equals(name,configured,StringComparison.OrdinalIgnoreCase);
            if (Matches(catalog?.rainEffectName) || key.Contains("rain") || key.Contains("дожд")) return "Rain";
            if (Matches(catalog?.snowEffectName) || key.Contains("snow") || key.Contains("снег")) return "Snow";
            if (Matches(catalog?.hailEffectName) || key.Contains("hail") || key.Contains("град")) return "Hail";
            return "None";
        }
        private static void Set(JObject visual, string key, Action<float> setter)
        { if (visual[key] != null) setter((float)visual[key]); }
        private void OnDestroy() { if (weatherClone != null) Destroy(weatherClone); }
    }
}
