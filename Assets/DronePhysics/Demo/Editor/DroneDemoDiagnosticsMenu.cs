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
            =>CreateAcceptance("quad_test_final_acceptance","DroneLab Final Acceptance");
        [MenuItem("DroneLab/Diagnostics/Create Advanced Rotor Acceptance Drone")]
        public static void CreateAdvancedRotorAcceptance()
            =>CreateAcceptance("quad_test_advanced_rotors","DroneLab Advanced Rotor Acceptance");
        [MenuItem("DroneLab/Diagnostics/Create Coupled Power Acceptance Drone")]
        public static void CreateCoupledPowerAcceptance()
            =>CreateAcceptance("quad_test_coupled_power","DroneLab Coupled Power Acceptance");
        [MenuItem("DroneLab/Diagnostics/Create Thermal Acceptance Drone")]
        public static void CreateThermalAcceptance()
            =>CreateAcceptance("quad_test_thermal","DroneLab Thermal Acceptance");
        [MenuItem("DroneLab/Diagnostics/Create Descent and Wind Acceptance Drone")]
        public static void CreateDescentWindAcceptance()
            =>CreateAcceptance("quad_test_descent_wind","DroneLab Descent and Wind Acceptance","environment_dryden_frozen");
        private static void CreateAcceptance(string profile,string title,string environment="environment_final_acceptance")
        {
            if(EditorApplication.isPlaying) { Debug.LogError("Stop Play before creating the test drone."); return; }
            DroneTestRigMenu.Create();
            var body=Selection.activeGameObject.GetComponent<DronePhysicsBody>();
            body.gameObject.name=title;
            body.droneProfile=Resources.Load<TextAsset>("DronePhysics/"+profile);
            body.environmentProfile=Resources.Load<TextAsset>("DronePhysics/"+environment);
            DroneDiagnosticsMenu.Add(); DroneGeometryMenu.CreateMarkers();
            EditorUtility.SetDirty(body); EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
            Debug.Log("Acceptance fixture assigned. Connect your camera, save the scene, then use F/H. Fault is manual; see Docs/Physics/FINAL_ACCEPTANCE.md and ROTOR_FLOW.md.",body);
        }
    }
}
