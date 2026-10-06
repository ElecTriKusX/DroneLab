using System.IO;
using System.Text;
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
            var original = body.environmentProfile != null ? body.environmentProfile : Resources.Load<TextAsset>("DronePhysics/environment_calm");
            if (original == null) throw new System.InvalidOperationException("Assign an environment profile first.");
            var json = JObject.Parse(original.text);
            json["windMode"] = "CustomField"; json["gustEnabled"] = false;
            var drone = body.droneProfile != null ? body.droneProfile : Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
            if (drone == null || JObject.Parse(drone.text)["physicsConfiguration"]?["modules"]?["windInteraction"]?.Value<bool>() != true)
            { EditorUtility.DisplayDialog("Weather bridge", "Enable windInteraction in the drone profile first. Combined Physics Drone is suitable.", "OK"); return; }
            string path = EditorUtility.SaveFilePanelInProject("Save CustomField environment copy", "enviro_environment", "json", "Original air/power parameters remain unchanged.");
            if (string.IsNullOrEmpty(path)) return;
            if (File.Exists(path))
            { EditorUtility.DisplayDialog("Weather bridge", "Choose a new file to preserve existing profiles.", "OK"); return; }
            File.WriteAllText(path, json.ToString(Formatting.Indented) + "\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            var controller = body.customWindProvider as EnviroWeatherController;
            if (controller == null)
            {
                var go = new GameObject("DroneLab Weather"); Undo.RegisterCreatedObjectUndo(go, "Create weather bridge");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, body.gameObject.scene);
                controller = Undo.AddComponent<EnviroWeatherController>(go);
            }
            Undo.RecordObject(controller, "Bind Enviro weather"); Undo.RecordObject(body, "Bind weather provider");
            controller.manager = manager;
            var targets = new System.Collections.Generic.List<DronePhysicsBody>(controller.droneTargets ?? System.Array.Empty<DronePhysicsBody>());
            if (!targets.Contains(body)) targets.Add(body);
            controller.droneTargets = targets.ToArray();
            body.customWindProvider = controller; body.environmentProfile = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            EditorUtility.SetDirty(body); EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeGameObject = controller.gameObject;
        }
    }
}
