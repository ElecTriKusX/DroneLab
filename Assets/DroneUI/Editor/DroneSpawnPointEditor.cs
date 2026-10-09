using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneLab.UI.Editor
{
    public static class DroneSpawnPointSetup
    {
        [MenuItem("DroneLab/Simulation/Add Spawn Point to Selected Pad")]
        public static void Add()
        {
            var pad = Selection.activeGameObject;
            if (pad == null || !pad.scene.IsValid()) { Debug.LogError("Выберите площадку на сцене."); return; }
            var existing = pad.GetComponentInChildren<DroneSpawnPoint>(true);
            if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
            var go = new GameObject("DroneLab Spawn Point"); Undo.RegisterCreatedObjectUndo(go, "Create drone spawn point");
            go.transform.SetParent(pad.transform, false);
            var bounds = new Bounds(pad.transform.position, Vector3.zero); bool hasBounds = false;
            foreach (var collider in pad.GetComponentsInChildren<Collider>()) {
                if (!collider.enabled || collider.isTrigger) continue;
                if (!hasBounds) { bounds = collider.bounds; hasBounds = true; } else bounds.Encapsulate(collider.bounds);
            }
            if (!hasBounds) foreach (var renderer in pad.GetComponentsInChildren<Renderer>()) {
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; } else bounds.Encapsulate(renderer.bounds);
            }
            go.transform.position = hasBounds ? new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) : pad.transform.position;
            go.transform.rotation = Quaternion.Euler(0, pad.transform.eulerAngles.y, 0);
            Undo.AddComponent<DroneSpawnPoint>(go); Selection.activeGameObject = go; EditorSceneManager.MarkSceneDirty(pad.scene);
            if (pad.GetComponentInChildren<Collider>() == null) Debug.LogWarning("Добавьте на площадку Collider: иначе дрон провалится сквозь модель.", pad);
        }
        [MenuItem("DroneLab/Simulation/Add Spawn Point to Selected Pad", true)]
        private static bool CanAdd() => Selection.activeGameObject != null && Selection.activeGameObject.scene.IsValid();
    }
    [CustomEditor(typeof(DroneSpawnPoint))]
    public sealed class DroneSpawnPointEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Transform задаёт верхнюю поверхность площадки. Поворот Y — направление носа (+Z). Высота корпуса учитывается автоматически. При нескольких площадках задайте уникальные Id и выберите нужный в каталоге карты. На площадке нужен Collider.", MessageType.Info);
        }
    }
}
