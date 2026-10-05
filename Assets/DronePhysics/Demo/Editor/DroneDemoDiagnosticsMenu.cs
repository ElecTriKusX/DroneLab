using DroneLab.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneLab.Editor
{
    public static class DroneDemoDiagnosticsMenu
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
            DroneDiagnosticsMenu.Add(); DroneGeometryMenu.CreateMarkers();
            EditorUtility.SetDirty(body); EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
            Debug.Log("Final acceptance fixture assigned. Connect your camera, save the scene, then use F/H. Fault is manual; see Docs/Physics/FINAL_ACCEPTANCE.md.",body);
        }
    }
}
