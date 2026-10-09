using System;
using System.IO;
using System.Linq;
using DroneLab.Physics;
using DroneLab.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DroneLab.Editor
{
    public sealed class DronePerformanceImportWindow : EditorWindow
    {
        private DronePhysicsBody body;
        private TextAsset csv;
        private int model,policy,rotor;
        private bool allRotors=true;
        private double density=1.225;
        [MenuItem("DroneLab/Propellers/Import Performance CSV")]
        public static void Open()
        {
            var window=GetWindow<DronePerformanceImportWindow>("Propeller CSV");
            window.body=Selection.activeTransform?.GetComponentInParent<DronePhysicsBody>();
        }
        private void OnGUI()
        {
            body=(DronePhysicsBody)EditorGUILayout.ObjectField("Drone root",body,typeof(DronePhysicsBody),true);
            csv=(TextAsset)EditorGUILayout.ObjectField("CSV asset",csv,typeof(TextAsset),false);
            model=EditorGUILayout.Popup("Model",model,new[]{"RpmTable","PerformanceMap"});
            policy=EditorGUILayout.Popup("Out of range",policy,new[]{"Clamp","Reject"});
            if(model==0) density=EditorGUILayout.DoubleField("Reference density kg/m³",density);
            allRotors=EditorGUILayout.Toggle("Apply to all rotors",allRotors);
            if(!allRotors) rotor=EditorGUILayout.IntField("Rotor index (zero based)",rotor);
            EditorGUILayout.HelpBox(model==0 ? "CSV: rpm,thrustN,torqueNm[,currentA]. Include the zero row. All quantities in SI."
                : "CSV: rpm,advanceRatio,ct,cq[,reynolds]. Complete RPM × J grid including J=0. Cq is not Cp. Reynolds is metadata.",MessageType.Info);
            using(new EditorGUI.DisabledScope(body==null || csv==null || EditorApplication.isPlaying))
                if(GUILayout.Button("Validate and save new drone JSON")) Import();
        }
        private void Import()
        {
            try
            {
                var env=body.environmentProfile ?? Resources.Load<TextAsset>("DronePhysics/environment_calm");
                var source=body.droneProfile ?? Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
                var ds=Resources.Load<TextAsset>("DronePhysics/drone-profile.schema");
                var es=Resources.Load<TextAsset>("DronePhysics/environment-profile.schema");
                var loaded=ProfileLoader.Load(source.text,env.text,ds.text,es.text);
                if(!loaded.Success) throw new ArgumentException(string.Join("\n",loaded.Issues));
                var json=JObject.Parse(source.text);
                var performance=PerformanceCsv.Parse(csv.text,model==0 ? "RpmTable" : "PerformanceMap",policy==0 ? "Clamp" : "Reject",density);
                // Serialize only fields belonging to the selected model. DTO defaults are not data.
                var data=new JObject { ["model"]=performance.model,["outOfRangePolicy"]=performance.outOfRangePolicy };
                if(model==0)
                {
                    data["referenceAirDensityKgM3"]=density;
                    data["rpmTable"]=JArray.FromObject(performance.rpmTable,new JsonSerializer { NullValueHandling=NullValueHandling.Ignore });
                }
                else
                {
                    var rows=JArray.FromObject(performance.performanceMap);
                    foreach(JObject row in rows) if(row.Value<double>("reynolds")==0) row.Remove("reynolds");
                    data["performanceMap"]=rows;
                }
                var rotors=(JArray)json["rotors"];
                if(!allRotors && (rotor<0 || rotor>=rotors.Count)) throw new ArgumentException("Rotor index is outside this profile.");
                for(int i=0;i<rotors.Count;i++) if(allRotors || i==rotor) rotors[i]["performance"]=data.DeepClone();
                json.Remove("derived");
                ((JArray)json["parameterProvenance"]).Add(new JObject { ["path"]=allRotors ? "rotors.performance" : "rotors["+rotor+"].performance",
                    ["sourceType"]="User",["source"]="SI CSV import: "+csv.name+"; source/measurement quality must be verified by user.",["confidence"]=0.5 });
                var check=ProfileLoader.Load(json.ToString(),env.text,ds.text,es.text);
                if(!check.Success) throw new ArgumentException(string.Join("\n",check.Issues));
                foreach(var issue in check.Issues.Where(x=>x.Severity=="Warning")) Debug.LogWarning(issue.ToString(),body);
                try { new QuadAllocator(check.Parameters); }
                catch(ArgumentException ex) { Debug.LogWarning("Physics profile is valid; test pilot cannot use it: "+ex.Message,body); }
                string path=EditorUtility.SaveFilePanelInProject("Save propeller profile","drone_performance","json","Save a new profile with imported propeller data.");
                if(string.IsNullOrEmpty(path)) return;
                if(AssetDatabase.LoadAssetAtPath<TextAsset>(path)==source) throw new ArgumentException("Choose a new JSON path to preserve the source profile.");
                File.WriteAllText(path,json.ToString(Formatting.Indented)+"\n"); AssetDatabase.ImportAsset(path);
                Undo.RecordObject(body,"Assign propeller profile"); body.droneProfile=AssetDatabase.LoadAssetAtPath<TextAsset>(path); EditorUtility.SetDirty(body);
                Debug.Log("DroneLab: propeller profile saved and assigned. Save the scene before Play.",body);
            }
            catch(Exception ex) { Debug.LogError("DroneLab propeller import: "+ex.Message,body); }
        }
    }
}
