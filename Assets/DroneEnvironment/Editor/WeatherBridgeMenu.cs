using System.IO;
using System.Text;
using DroneLab.Physics;
using DroneLab.Simulation;
using Enviro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneLab.Weather.Editor
{
    public static class WeatherBridgeMenu
    {
        [MenuItem("DroneLab/Environment/Connect Selected Drone to Enviro")]
        public static void Connect()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorUtility.DisplayDialog("Weather bridge", "Connect in Edit Mode before the drone initializes.", "OK"); return; }
            var body = Selection.activeTransform != null ? Selection.activeTransform.GetComponentInParent<DronePhysicsBody>() : null;
            var manager = Object.FindFirstObjectByType<EnviroManager>();
            if (body == null || manager == null)
            { EditorUtility.DisplayDialog("Weather bridge", "Select a DronePhysicsBody and add/configure the Enviro prefab first.", "OK"); return; }
            if (body.customWindProvider != null && !(body.customWindProvider is EnviroWeatherController))
            { EditorUtility.DisplayDialog("Weather bridge", "The drone already has another custom wind provider.", "OK"); return; }
            var controller = body.customWindProvider as EnviroWeatherController;
            if(controller==null)
                foreach(var candidate in Object.FindObjectsByType<EnviroWeatherController>(FindObjectsSortMode.None))
                    if(candidate.isActiveAndEnabled && candidate.manager==manager)
                    { controller=candidate; break; }
            if(body.customAirProvider!=null && body.customAirProvider!=controller)
            { EditorUtility.DisplayDialog("Weather bridge", "The drone already has another air provider.", "OK"); return; }
            var original = body.environmentProfile != null ? body.environmentProfile : Resources.Load<TextAsset>("DronePhysics/environment_calm");
            if (original == null) throw new System.InvalidOperationException("Assign an environment profile first.");
            var json = JObject.Parse(original.text);
            json["windMode"] = "CustomField"; json["gustEnabled"] = false;
            var drone = body.droneProfile != null ? body.droneProfile : Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
            var droneJson=drone!=null ? JObject.Parse(drone.text) : null;
            if (droneJson?["physicsConfiguration"]?["modules"]?["windInteraction"]?.Value<bool>() != true)
            { EditorUtility.DisplayDialog("Weather bridge", "Enable windInteraction in the drone profile first. Combined Physics Drone is suitable.", "OK"); return; }
            if(controller==null || controller.syncAir)
                foreach(var rotor in droneJson["rotors"])
                    if(rotor["performance"]?["model"]?.Value<string>()!="CtCq" && rotor["performance"]?["model"]?.Value<string>()!="PerformanceMap")
                    { EditorUtility.DisplayDialog("Weather bridge", "Live air requires CtCq/PerformanceMap. Use Combined Physics Drone or disable syncAir for wind-only integration.", "OK"); return; }
            string path = EditorUtility.SaveFilePanelInProject("Save CustomField environment copy", "enviro_environment", "json", "Original air/power parameters remain unchanged.");
            if (string.IsNullOrEmpty(path)) return;
            if (File.Exists(path))
            { EditorUtility.DisplayDialog("Weather bridge", "Choose a new file to preserve existing profiles.", "OK"); return; }
            File.WriteAllText(path, json.ToString(Formatting.Indented) + "\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            if (controller == null)
            {
                var go = new GameObject("DroneLab Weather"); Undo.RegisterCreatedObjectUndo(go, "Create weather bridge");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, body.gameObject.scene);
                controller = Undo.AddComponent<EnviroWeatherController>(go);
                controller.referenceAltitudeM=json["altitudeM"]?.Value<float>() ?? 0;
                var com=droneJson["massProperties"]?["centerOfMassLocalM"];
                controller.referenceWorldY=body.transform.TransformPoint(com!=null ? new Vector3(com[0].Value<float>(),com[1].Value<float>(),com[2].Value<float>()) : Vector3.zero).y;
                if(json["airDensityMode"]?.Value<string>()=="StandardAtmosphere")
                    controller.referencePressurePa=(float)Atmosphere.Troposphere(controller.referenceAltitudeM,
                        json["temperatureK"]?.Value<double>() ?? 288.15,json["pressurePa"]?.Value<double>() ?? 101325).PressurePa;
                else if(json["pressurePa"]?.Value<float>()>0) controller.referencePressurePa=json["pressurePa"].Value<float>();
            }
            Undo.RecordObject(controller, "Bind Enviro weather"); Undo.RecordObject(body, "Bind weather provider");
            controller.manager = manager;
            var targets = new System.Collections.Generic.List<DronePhysicsBody>(controller.droneTargets ?? System.Array.Empty<DronePhysicsBody>());
            if (!targets.Contains(body)) targets.Add(body);
            controller.droneTargets = targets.ToArray();
            body.customWindProvider = controller; body.environmentProfile = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if(controller.syncAir) body.customAirProvider=controller;
            EditorUtility.SetDirty(body); EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeGameObject = controller.gameObject;
        }
    }
}
