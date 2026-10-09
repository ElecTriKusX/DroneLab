using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneLab.UI.Editor
{
    public static class DroneMainMenuEditor
    {
        [MenuItem("DroneLab/UI/Create Main Menu Scene")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            const string path = "Assets/Scenes/MainMenu.unity";
            if (System.IO.File.Exists(path))
            {
                EditorSceneManager.OpenScene(path);
                return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("DroneLab Main Menu");
            go.AddComponent<DroneMainMenu>();
            go.AddComponent<DroneScenarioController>();
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            Selection.activeGameObject = go;
        }

        [MenuItem("DroneLab/UI/Add Menu and PhysTest to Build")]
        public static void AddToBuild()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            const string menu = "Assets/Scenes/MainMenu.unity";
            const string flight = "Assets/Scenes/PhysTest.unity";
            if (!System.IO.File.Exists(menu))
            {
                Debug.LogError("Create the main menu scene first.");
                return;
            }
            scenes.Add(new EditorBuildSettingsScene(menu, true));
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.path != menu && scene.path != flight && System.IO.File.Exists(scene.path)) scenes.Add(scene);
            if (System.IO.File.Exists(flight)) scenes.Add(new EditorBuildSettingsScene(flight, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            var catalog = DroneScenarioCatalog.Load();
            if (catalog != null) DroneScenarioCatalogSetup.AddToBuild(catalog);
        }
    }
}
