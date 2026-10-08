using System;
using DroneLab.Physics;
using DroneLab.Simulation;
using DroneLab.Weather;
using Enviro;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneLab.UI
{
    public static class DroneScenarioLaunch
    {
        private static DroneEnvironmentDocument active;
        private static string scenePath;
        private static DroneScenarioCatalog activeCatalog;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { active = null; scenePath = null; activeCatalog = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DronePhysicsBody.PreparingSceneBody -= PrepareBody;
            DronePhysicsBody.PreparingSceneBody += PrepareBody;
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
        }
        public static AsyncOperation Load(DroneMapEntry map, DroneEnvironmentDocument profile, DroneScenarioCatalog catalog = null)
        {
            if (map == null || string.IsNullOrWhiteSpace(map.scenePath) || !Application.CanStreamedLevelBeLoaded(map.scenePath))
                throw new ArgumentException("Карта не включена в список сцен сборки.");
            DroneEnvironmentProfiles.Validate(profile,null,catalog);
            var physical = DroneEnvironmentProfiles.Effective(profile.environment);
            var air = (string)physical["airDensityMode"] == "StandardAtmosphere" ?
                Atmosphere.Troposphere((double)physical["altitudeM"], (double)physical["temperatureK"], (double)physical["pressurePa"]) :
                new AirSample((double)physical["airDensityKgM3"], (double)physical["temperatureK"], (double)physical["pressurePa"], (double)physical["altitudeM"]);
            try { new AtmosphereColumn(air.TemperatureK, air.PressurePa, air.AltitudeM); }
            catch (ArgumentException) { throw new ArgumentException("Связь с погодной системой требует локальную температуру 200–330 К, давление 1000–200000 Па и высоту −500–11000 м."); }
            activeCatalog = catalog != null ? catalog : DroneScenarioCatalog.Load();
            string presetId = (string)profile.visual["weatherPresetId"];
            if (!string.IsNullOrEmpty(presetId) && activeCatalog?.Weather(presetId)?.preset == null)
                throw new ArgumentException("Погодный пресет профиля отсутствует в каталоге.");
            active = profile.Copy(); scenePath = map.scenePath;
            try { var operation = SceneManager.LoadSceneAsync(scenePath); if (operation == null) throw new InvalidOperationException("Не удалось загрузить карту."); return operation; }
            catch { active = null; scenePath = null; throw; }
        }
        private static void PrepareBody(DronePhysicsBody body)
        {
            if (active == null || body.gameObject.scene.path != scenePath) return;
            DroneEnvironmentProfiles.Validate(active, body.droneProfile,activeCatalog);
            var asset = new TextAsset(DroneEnvironmentProfiles.PhysicsJson(active)) { name = active.name };
            body.environmentProfile = asset;
            var lifetime = body.gameObject.AddComponent<DroneProfileAssetLifetime>(); lifetime.asset = asset;
            // Profile air remains authoritative, including Constant density and sea-level atmosphere.
            // Enviro's LOCAL live-air column must not replace those values silently.
            if (body.CustomAirProvider is EnviroWeatherController) body.CustomAirProvider = null;
            if (body.customAirProvider is EnviroWeatherController) body.customAirProvider = null;
            if (body.CustomAirProvider != null || body.customAirProvider != null)
                throw new ArgumentException("В сцене назначен другой источник воздуха. Уберите его перед запуском среды из JSON.");
            var bridge = UnityEngine.Object.FindFirstObjectByType<EnviroWeatherController>();
            if (bridge == null) {
                var manager = UnityEngine.Object.FindFirstObjectByType<EnviroManager>();
                if (manager != null) {
                    var go = new GameObject("DroneLab Weather Bridge"); SceneManager.MoveGameObjectToScene(go, body.gameObject.scene);
                    bridge = go.AddComponent<EnviroWeatherController>(); bridge.manager = manager;
                }
            }
            if (bridge != null) { bridge.syncAir = false; bridge.showEngineeringWindow = false; }
            var existingWind = body.CustomWindProvider ?? body.customWindProvider as IWindProvider;
            if ((string)active.environment["windMode"] == "CustomField" && bridge != null &&
                (existingWind == null || existingWind is EnviroWeatherController))
            { body.CustomWindProvider = bridge; body.customWindProvider = bridge; }
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (active == null || scene.path != scenePath) return;
            var go = new GameObject("DroneLab Profile Environment"); SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<DroneScenarioEnvironmentDriver>().Configure(active, activeCatalog);
        }
    }

}
