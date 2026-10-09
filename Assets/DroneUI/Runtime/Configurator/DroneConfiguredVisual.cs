using System;
using DroneLab.Configurator;
using DroneLab.Simulation;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    /// <summary>Builds the schematic compound body or loads an imported visual on the unit-scale physics root.</summary>
    internal sealed class DroneConfiguredVisual : MonoBehaviour
    {
        private DroneProfileDocument document;
        private GameObject visual;
        private RuntimeGltfModelLoader loader;
        private readonly DroneRotorVisuals rotorVisuals=new DroneRotorVisuals();
        private DronePhysicsBody body;
        public void Configure(DroneProfileDocument value)
        {
            document=value.Copy();
            body=GetComponent<DronePhysicsBody>();
            foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=false;
            foreach(var collider in GetComponentsInChildren<Collider>())collider.enabled=false;
            var box=GetComponent<BoxCollider>();if(box==null)box=gameObject.AddComponent<BoxCollider>();box.enabled=true;box.center=Vector3.zero;
            box.size=DroneModelViewport.Vec(document.profile["massProperties"]["dimensionsM"]);
            visual=new GameObject("Configured drone visual");visual.transform.SetParent(transform,false);
            if(string.IsNullOrEmpty(DroneProfileLibrary.ModelPath(document))) {
                box.enabled=false;
                DroneSchematicModel.Build(visual.transform,document,true,gameObject.layer);
                return;
            }
            var shell=GameObject.CreatePrimitive(PrimitiveType.Cube);shell.name="Profile envelope";shell.transform.SetParent(visual.transform,false);
            shell.transform.localScale=box.size;Destroy(shell.GetComponent<Collider>());shell.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("DroneLab/ConfiguratorSchematic");
        }
        private async void Start()
        {
            try {
                string path=DroneProfileLibrary.ModelPath(document);if(string.IsNullOrEmpty(path))return;
                DroneModelFiles.ValidateLocalModel(path);
                loader=gameObject.AddComponent<RuntimeGltfModelLoader>();var loaded=await loader.LoadAsync(path,visual.transform);
                if(this==null)return;if(!loaded.success){Debug.LogWarning("DroneLab model: "+loaded.error,this);return;}
                var t=loader.LoadedRoot;t.localScale=Vector3.one*(float)(double)document.profile["coordinateSystem"]["modelScaleMetersPerUnit"];
                t.localRotation=Quaternion.Euler(DroneModelViewport.Vec(document.visual["rotationEulerDeg"]));
                if((bool?)document.visual["centerModel"]??true)if(RuntimeGltfModelLoader.TryGetBoundsInFrame(t,visual.transform,out var bounds))t.localPosition=-bounds.center;
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())if(!renderer.transform.IsChildOf(t))renderer.enabled=false;
                rotorVisuals.Bind(t,(JArray)document.profile["rotors"],(JObject)document.visual["rotorNodes"]);
                if(rotorVisuals.Issues.Count>0)Debug.LogWarning("DroneLab visual bindings: "+string.Join("; ",rotorVisuals.Issues),this);
            }catch(Exception ex){Debug.LogWarning("DroneLab model: "+ex.Message,this);}
        }
        private void LateUpdate()
        {
            if(document==null || body==null || body.Omega==null)return;
            rotorVisuals.Step(transform,(JArray)document.profile["rotors"],index=>index<body.Omega.Length?body.Omega[index]:0,Time.deltaTime);
        }
    }
}
