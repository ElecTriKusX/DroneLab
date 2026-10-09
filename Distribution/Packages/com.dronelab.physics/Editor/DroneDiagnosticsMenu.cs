using DroneLab.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneLab.Editor
{
    public static class DroneDiagnosticsMenu
    {
        [MenuItem("DroneLab/Diagnostics/Show Project Manifest (package tests)")]
        public static void ShowProjectManifest()
        {
            string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"..","Packages","manifest.json"));
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("DroneLab/Diagnostics/Add Flight Recorder and Motor Fault Scenario")]
        public static void Add()
        {
            var body=Selection.activeTransform?.GetComponentInParent<DronePhysicsBody>();
            if(body==null) { Debug.LogError("Select a drone root with DronePhysicsBody."); return; }
            if(body.GetComponent<DroneTelemetryRecorder>()==null) Undo.AddComponent<DroneTelemetryRecorder>(body.gameObject);
            if(body.GetComponent<DroneMotorFaultScenario>()==null) Undo.AddComponent<DroneMotorFaultScenario>(body.gameObject);
            EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
            Selection.activeGameObject=body.gameObject;
        }
        [MenuItem("DroneLab/Diagnostics/Open Flight Recordings Folder")]
        public static void Open()
        {
            string path=System.IO.Path.Combine(Application.persistentDataPath,"DroneLab","Flights");
            System.IO.Directory.CreateDirectory(path); EditorUtility.RevealInFinder(path);
        }
    }
}
