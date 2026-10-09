using System;
using System.Collections.Generic;
using System.Linq;
using DroneLab.Configurator;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DroneLab.UI
{
    // Visual-only animation: never changes physical rotor parameters or applies forces.
    internal sealed class DroneRotorVisuals
    {
        private sealed class Part
        {
            public Transform node;
            public Vector3 position;
            public Quaternion rotation;
            public int rotor;
        }
        private readonly List<Part> parts=new List<Part>();
        private readonly Dictionary<int,double> phases=new Dictionary<int,double>();
        public readonly List<string> Issues=new List<string>();
        public static IEnumerable<string> Paths(JToken token)=>DroneVisualBindings.Paths(token);
        public void Restore()
        {
            foreach(var part in parts)if(part.node!=null){part.node.localPosition=part.position;part.node.localRotation=part.rotation;}
        }
        public void Bind(Transform root,JArray rotors,JObject bindings)
        {
            Restore();parts.Clear();phases.Clear();Issues.Clear();if(root==null)return;
            int total=root.GetComponentsInChildren<Renderer>(true).Length;
            var candidates=new List<Part>();
            for(int i=0;i<rotors.Count;i++)foreach(string path in Paths(bindings[(string)rotors[i]["rotorId"]])) {
                var node=RuntimeGltfModelLoader.FindByPath(root,path);
                if(node==null || node==root || node.GetComponentsInChildren<Renderer>(true).Length==0 || node.GetComponentsInChildren<Renderer>(true).Length==total){Issues.Add((string)rotors[i]["rotorId"]+": узел отсутствует или содержит всю модель ("+path+")");continue;}
                candidates.Add(new Part{node=node,position=node.localPosition,rotation=node.localRotation,rotor=i});
            }
            // Ignore ambiguous persisted bindings rather than rotating a subtree twice.
            foreach(var part in candidates)if(!candidates.Any(other=>other!=part && (other.node==part.node || other.node.IsChildOf(part.node) || part.node.IsChildOf(other.node))))parts.Add(part);else Issues.Add("Пересекающиеся привязки узла: "+part.node.name);
        }
        public void Step(Transform frame,JArray rotors,Func<int,double> omega,double deltaTime)
        {
            Restore();
            foreach(int index in parts.Select(p=>p.rotor).Distinct()) {
                double speed=omega(index);if(double.IsNaN(speed)||double.IsInfinity(speed))continue;
                phases.TryGetValue(index,out double phase);phases[index]=DroneVisualBindings.AdvancePhase(phase,speed,deltaTime,(string)rotors[index]["geometry"]["spinDirection"]);
            }
            foreach(var part in parts) {
                if(part.node==null)continue;
                var geometry=rotors[part.rotor]["geometry"];
                var axis=frame.TransformDirection(DroneModelViewport.Vec(geometry["thrustAxisLocal"])).normalized;
                if(axis.sqrMagnitude<1e-10f)continue;
                var pivot=frame.TransformPoint(DroneModelViewport.Vec(geometry["positionLocalM"]));
                phases.TryGetValue(part.rotor,out double phase);var q=Quaternion.AngleAxis((float)phase,axis);
                part.node.SetPositionAndRotation(pivot+q*(part.node.position-pivot),q*part.node.rotation);
            }
        }
    }
}
