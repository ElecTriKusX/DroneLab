using DroneLab.Simulation;
using DroneLab.Physics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneLab.Editor
{
    public static class DroneDemoDiagnosticsMenu
    {
        [MenuItem("DroneLab/Test Bench/Module Checks/Body and Battery")]
        public static void CreateFinalAcceptance()
            =>CreateAcceptance("quad_test_final_acceptance","DroneLab Final Acceptance");
        [MenuItem("DroneLab/Test Bench/Module Checks/Rotor Airflow")]
        public static void CreateAdvancedRotorAcceptance()
            =>CreateAcceptance("quad_test_advanced_rotors","DroneLab Advanced Rotor Acceptance");
        [MenuItem("DroneLab/Test Bench/Module Checks/Rotor Inertia and Power")]
        public static void CreateCoupledPowerAcceptance()
            =>CreateAcceptance("quad_test_coupled_power","DroneLab Coupled Power Acceptance");
        [MenuItem("DroneLab/Test Bench/Module Checks/Thermal")]
        public static void CreateThermalAcceptance()
            =>CreateAcceptance("quad_test_thermal","DroneLab Thermal Acceptance");
        [MenuItem("DroneLab/Test Bench/Combined Physics Drone")]
        public static void CreateDescentWindAcceptance()
            =>CreateAcceptance("quad_test_descent_wind","DroneLab Descent and Wind Acceptance","environment_dryden_frozen");
        [MenuItem("DroneLab/Test Bench/Reference Drones/Crazyflie 2.0")]
        public static void CreateCrazyflie20()=>CreateAcceptance("reference_crazyflie20","Crazyflie 2.0 Reference","environment_calm",true);
        [MenuItem("DroneLab/Test Bench/Reference Drones/Crazyflie Brushless")]
        public static void CreateBrushless()=>CreateAcceptance("reference_crazyflie_brushless","Crazyflie Brushless Reference","environment_calm",true);
        [MenuItem("DroneLab/Test Bench/Reference Drones/AscTec Hummingbird")]
        public static void CreateHummingbird()=>CreateAcceptance("reference_hummingbird","Hummingbird Reference","environment_calm",true);
        [MenuItem("DroneLab/Test Bench/Propeller Bench/APC 10x4.7 Static")]
        public static void CreateStaticBench()=>CreateAcceptance("bench_apc_10x47_static","APC Static Synthetic Holder","environment_calm",true);
        [MenuItem("DroneLab/Test Bench/Propeller Bench/APC 10x4.7 Axial")]
        public static void CreateAxialBench()=>CreateAcceptance("bench_apc_10x47_axial","APC Axial Synthetic Holder","environment_calm",true);
        private static void CreateAcceptance(string profile,string title,string environment="environment_final_acceptance",bool reference=false)
        {
            if(EditorApplication.isPlaying) { Debug.LogError("Stop Play before creating the test drone."); return; }
            DroneTestRigMenu.Create();
            var body=Selection.activeGameObject.GetComponent<DronePhysicsBody>();
            body.gameObject.name=title;
            body.droneProfile=Resources.Load<TextAsset>("DronePhysics/"+profile);
            body.environmentProfile=Resources.Load<TextAsset>("DronePhysics/"+environment);
            if(reference)
            {
                var check=ProfileLoader.Load(body.droneProfile.text,body.environmentProfile.text,
                    Resources.Load<TextAsset>("DronePhysics/drone-profile.schema").text,Resources.Load<TextAsset>("DronePhysics/environment-profile.schema").text);
                if(!check.Success) { Debug.LogError(string.Join("\n",check.Issues),body); return; }
                var p=check.Parameters; body.GetComponent<BoxCollider>().size=DronePhysicsBody.ToUnity(p.Dimensions);
                var old=body.transform.Find("Visual - replace with your asset"); if(old!=null) Object.DestroyImmediate(old.gameObject);
                var visual=new GameObject("Reference Visual - schematic, replace with manufacturer mesh"); visual.transform.SetParent(body.transform,false);
                Primitive(visual.transform,PrimitiveType.Cube,"Body",Vector3.zero,new Vector3((float)p.Dimensions.X*.25f,(float)p.Dimensions.Y*.3f,(float)p.Dimensions.Z*.25f));
                foreach(var r in p.Rotors)
                {
                    var pos=DronePhysicsBody.ToUnity(r.Position);
                    var arm=Primitive(visual.transform,PrimitiveType.Cylinder,r.Id+" arm",pos*.5f,new Vector3((float)r.Diameter*.06f,pos.magnitude*.5f,(float)r.Diameter*.06f));
                    arm.transform.localRotation=Quaternion.FromToRotation(Vector3.up,pos);
                    Primitive(visual.transform,PrimitiveType.Cylinder,r.Id+" rotor disk",pos,new Vector3((float)r.Diameter,.001f,(float)r.Diameter));
                }
            }
            DroneDiagnosticsMenu.Add(); DroneGeometryMenu.CreateMarkers();
            EditorUtility.SetDirty(body); EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
            Debug.Log("Acceptance fixture assigned. Connect your camera, save the scene, then use F/H. Fault is manual; see Docs/Physics/MENU.md and REFERENCE_DRONES.md.",body);
        }
        private static GameObject Primitive(Transform parent,PrimitiveType type,string name,Vector3 position,Vector3 scale)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale; return go;
        }
    }
}
