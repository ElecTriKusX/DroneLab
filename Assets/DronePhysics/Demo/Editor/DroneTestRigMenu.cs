using DroneLab.Simulation;
using UnityEditor;
using UnityEngine;

namespace DroneLab.Editor
{
    public static class DroneTestRigMenu
    {
        [MenuItem("DroneLab/Test Bench/Basic Physics Drone")]
        public static void Create()
        {
            var go=new GameObject("DroneLab TestQuad");
            Undo.RegisterCreatedObjectUndo(go,"Create DroneLab test drone");
            var position=Selection.activeTransform != null ? Selection.activeTransform.position : Vector3.zero;
            if(Terrain.activeTerrain != null)
                position.y=Mathf.Max(position.y,Terrain.activeTerrain.SampleHeight(position)+Terrain.activeTerrain.transform.position.y);
            go.transform.position=position+Vector3.up*2;
            var collider=go.AddComponent<BoxCollider>(); collider.size=new Vector3(0.4f,0.1f,0.4f);
            go.AddComponent<Rigidbody>();
            var physics=go.AddComponent<DronePhysicsBody>();
            physics.droneProfile=Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
            physics.environmentProfile=Resources.Load<TextAsset>("DronePhysics/environment_calm");
            go.AddComponent<DroneTestPilot>();
            var visual=GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name="Visual - replace with your asset";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(go.transform,false); visual.transform.localScale=collider.size;
            Selection.activeGameObject=go;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }
}
