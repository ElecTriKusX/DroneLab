using DroneLab.Simulation;
using UnityEditor;
using UnityEngine;

namespace DroneLab.Editor
{
    public static class ScaleReferenceMenu
    {
        [MenuItem("DroneLab/Test Bench/Scale References (meters)")]
        public static void Create()
        {
            var origin=Selection.activeTransform != null ? Selection.activeTransform.position : Vector3.zero;
            origin+=Vector3.right*3;
            var terrain=Terrain.activeTerrain;
            if(terrain != null)
            {
                var local=origin-terrain.transform.position;
                var size=terrain.terrainData.size;
                if(local.x>=0 && local.z>=0 && local.x<=size.x && local.z<=size.z)
                    origin.y=terrain.SampleHeight(origin)+terrain.transform.position.y;
            }
            var root=new GameObject("Scale references - 1 Unity unit = 1 m");
            Undo.RegisterCreatedObjectUndo(root,"Create scale references");
            root.transform.position=origin+Vector3.up*0.05f;
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/DronePhysics/ScaleReferences.mat");
            if(material == null)
            {
                var shader=Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if(shader != null)
                {
                    material=new Material(shader);
                    if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",new Color(0.7f,0.7f,0.7f));
                    AssetDatabase.CreateAsset(material,"Assets/DronePhysics/ScaleReferences.mat");
                }
            }
            void Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale)
            {
                var obj=GameObject.CreatePrimitive(type); obj.name=name;
                Object.DestroyImmediate(obj.GetComponent<Collider>()); // Visual references must not change flight dynamics.
                obj.transform.SetParent(root.transform,false);
                obj.transform.localPosition=position; obj.transform.localScale=scale;
                if(material != null) obj.GetComponent<Renderer>().sharedMaterial=material;
            }
            Shape("Cube 1 x 1 x 1 m",PrimitiveType.Cube,new Vector3(-2,0.5f,0),Vector3.one);
            Shape("Person height 1.8 m",PrimitiveType.Capsule,new Vector3(-2,0.9f,2),new Vector3(0.4f,0.9f,0.4f));
            Shape("Profile box 0.4 x 0.1 x 0.4 m",PrimitiveType.Cube,new Vector3(-2,0.05f,4),new Vector3(0.4f,0.1f,0.4f));
            Shape("Horizontal ruler 10 m",PrimitiveType.Cube,new Vector3(5,0.025f,0),new Vector3(10,0.05f,0.05f));
            for(int i=0;i<=10;i++) Shape("Mark "+i+" m",PrimitiveType.Cube,new Vector3(i,0.15f,0),new Vector3(0.05f,0.3f,0.05f));
            var labels=root.AddComponent<ScaleReferenceLabels>();
            labels.viewCamera=Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            labels.labels=new[]{"Cube: 1 m","Height: 1.8 m","Drone box: 0.4 m","0 m","5 m","10 m"};
            labels.labelPositionsLocal=new[]{new Vector3(-2,1.2f,0),new Vector3(-2,2,2),new Vector3(-2,0.4f,4),new Vector3(0,0.5f,0),new Vector3(5,0.5f,0),new Vector3(10,0.5f,0)};
            Selection.activeGameObject=root;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }

    [CustomEditor(typeof(ScaleReferenceLabels))]
    public sealed class ScaleReferenceLabelsEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var view=(ScaleReferenceLabels)target;
            if(view.labels == null || view.labelPositionsLocal == null) return;
            for(int i=0;i<Mathf.Min(view.labels.Length,view.labelPositionsLocal.Length);i++)
                Handles.Label(view.transform.TransformPoint(view.labelPositionsLocal[i]),view.labels[i]);
        }
    }
}
