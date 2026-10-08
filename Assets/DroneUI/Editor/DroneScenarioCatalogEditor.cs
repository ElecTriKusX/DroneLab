using System;
using System.IO;
using System.Linq;
using Enviro;
using UnityEditor;
using UnityEngine;

namespace DroneLab.UI.Editor
{
    [InitializeOnLoad]
    public static class DroneScenarioCatalogSetup
    {
        private const string CatalogPath = "Assets/DroneUI/Resources/DroneLab/ScenarioCatalog.asset";
        static DroneScenarioCatalogSetup() => EditorApplication.delayCall += Ensure;
        private static void Ensure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            var catalog = AssetDatabase.LoadAssetAtPath<DroneScenarioCatalog>(CatalogPath);
            if (catalog == null) {
                catalog = ScriptableObject.CreateInstance<DroneScenarioCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath); AddScenes(catalog);
            }
            if (catalog.weatherPresets.Count == 0) AddWeather(catalog);
        }
        [MenuItem("DroneLab/UI/Open Maps and Environment Catalog")]
        public static void Open() { Ensure(); Selection.activeObject = DroneScenarioCatalog.Load(); }
        public static void AddScenes(DroneScenarioCatalog catalog)
        {
            Undo.RecordObject(catalog, "Add map scenes");
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == "MainMenu" || catalog.maps.Any(m => m != null && m.scenePath == path)) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                catalog.maps.Add(new DroneMapEntry { id = guid, title = name == "Forest" ? "Лес" : name == "PhysTest" ? "Испытательная площадка" : name,
                    terrainType = name == "Forest" ? "Лес" : "Открытая местность", scenePath = path,
                    screenshot = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DroneUI/Screenshots/Maps/" + name + ".png") });
            }
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }
        public static void AddWeather(DroneScenarioCatalog catalog)
        {
            Undo.RecordObject(catalog, "Register weather presets");
            foreach (string guid in AssetDatabase.FindAssets("t:EnviroWeatherType")) {
                if (catalog.weatherPresets.Any(w => w != null && w.id == guid)) continue;
                var preset = AssetDatabase.LoadAssetAtPath<EnviroWeatherType>(AssetDatabase.GUIDToAssetPath(guid));
                catalog.weatherPresets.Add(new DroneWeatherEntry { id = guid, title = preset.name, preset = preset });
            }
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
        }
        public static void AddToBuild(DroneScenarioCatalog catalog)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (var map in catalog.maps) {
                if (map == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(map.scenePath) == null) { Debug.LogError("DroneLab: карта ссылается на отсутствующую сцену.", catalog); continue; }
                int index = scenes.FindIndex(s => s.path == map.scenePath);
                if (index < 0) scenes.Add(new EditorBuildSettingsScene(map.scenePath, true));
                else scenes[index].enabled = true;
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    [CustomEditor(typeof(DroneScenarioCatalog))]
    public sealed class DroneScenarioCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var catalog = (DroneScenarioCatalog)target;
            EditorGUILayout.HelpBox("Screenshots: Assets/DroneUI/Screenshots/Maps. Sizes are in metres. Profiles are shared by every map. IDs must be unique and stable.", MessageType.Info);
            if (GUILayout.Button("Add scenes from Assets/Scenes")) DroneScenarioCatalogSetup.AddScenes(catalog);
            if (GUILayout.Button("Register Enviro weather presets")) DroneScenarioCatalogSetup.AddWeather(catalog);
            if (GUILayout.Button("Add catalog maps to Build")) DroneScenarioCatalogSetup.AddToBuild(catalog);
            var ids = catalog.maps.Where(m => m != null).Select(m => m.id).ToList();
            if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Count) EditorGUILayout.HelpBox("Map IDs must be non-empty and unique.", MessageType.Error);
        }
    }

    [CustomPropertyDrawer(typeof(DroneMapEntry))]
    public sealed class DroneMapEntryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => (EditorGUIUtility.singleLineHeight + 4) * 8;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(row, label, EditorStyles.boldLabel); row.y += row.height + 4;
            foreach (string field in new[] { "id", "title", "terrainType", "sizeM", "screenshot" }) {
                EditorGUI.PropertyField(row, property.FindPropertyRelative(field)); row.y += row.height + 4;
            }
            var scenePath = property.FindPropertyRelative("scenePath");
            var previous = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath.stringValue);
            var scene = (SceneAsset)EditorGUI.ObjectField(row, "Scene", previous, typeof(SceneAsset), false);
            if (scene != previous) scenePath.stringValue = scene == null ? "" : AssetDatabase.GetAssetPath(scene);
            row.y += row.height + 4; EditorGUI.LabelField(row, scenePath.stringValue, EditorStyles.miniLabel);
            EditorGUI.EndProperty();
        }
    }
}
