using UnityEditor;
using UnityEngine;

namespace DroneLab.UI.Editor
{
    [CustomEditor(typeof(DroneScenarioController))]
    public sealed class DroneScenarioControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update(); EditorGUILayout.PropertyField(serializedObject.FindProperty("catalog"),new GUIContent("Каталог")); serializedObject.ApplyModifiedProperties();
            var source = ((DroneScenarioController)target).Catalog;
            if (source == null) { EditorGUILayout.HelpBox("Каталог отсутствует. Откройте DroneLab → UI → Open Maps and Environment Catalog.",MessageType.Error); return; }
            var catalog = new SerializedObject(source); catalog.Update();
            EditorGUILayout.Space(); EditorGUILayout.PropertyField(catalog.FindProperty("maps"),new GUIContent("Карты"),true);
            if (catalog.ApplyModifiedProperties()) EditorUtility.SetDirty(source);
            EditorGUILayout.HelpBox("Размеры вводятся в километрах. Превью: Assets/DroneUI/Screenshots/Maps. Данные хранятся в общем каталоге и доступны загрузчику симуляции.",MessageType.Info);
            if (GUILayout.Button("Добавить сцены из Assets/Scenes")) DroneScenarioCatalogSetup.AddScenes(source);
            if (GUILayout.Button("Добавить карты в сборку")) DroneScenarioCatalogSetup.AddToBuild(source);
            if (GUILayout.Button("Открыть основы визуальной погоды")) Selection.activeObject = source;
        }
    }
}
