using DroneLab.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneLab.Editor
{
    public static class DroneDiagnosticsMenu
    {
        [MenuItem("DroneLab/Diagnostics/Create Final Acceptance Drone")]
        public static void CreateFinalAcceptance()
        {
            if(EditorApplication.isPlaying) { Debug.LogError("Stop Play before creating the test drone."); return; }
            DroneTestRigMenu.Create();
            var body=Selection.activeGameObject.GetComponent<DronePhysicsBody>();
            body.gameObject.name="DroneLab Final Acceptance";
            body.droneProfile=Resources.Load<TextAsset>("DronePhysics/quad_test_final_acceptance");
            body.environmentProfile=Resources.Load<TextAsset>("DronePhysics/environment_final_acceptance");
            Add(); DroneGeometryMenu.CreateMarkers();
            EditorUtility.SetDirty(body); EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
            Debug.Log("Final acceptance fixture assigned. Connect your camera, save the scene, then use F/H. Fault is manual; see Docs/Physics/FINAL_ACCEPTANCE.md.",body);
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
