using System;
using System.IO;
using DroneLab.Physics;
using DroneLab.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DroneLab.Editor
{
    public sealed class DroneThermalWindow : EditorWindow
    {
        private DronePhysicsBody body;
        private double initialC=20,alpha=.0039,referenceC=20;
        private readonly double[] motor={15,.25,.04,70,100},esc={10,.12,.02,60,80},battery={200,.3,.015,40,60};
        [MenuItem("DroneLab/Power/Create Profile with Thermal Model")]
        public static void Open()
        {
            var w=GetWindow<DroneThermalWindow>("Thermal demo");
            w.body=Selection.activeTransform?.GetComponentInParent<DronePhysicsBody>();
        }
        private static void Fields(string title,double[] v)
        {
            EditorGUILayout.LabelField(title,EditorStyles.boldLabel);
            v[0]=EditorGUILayout.DoubleField("Heat capacity J/K",v[0]);
            v[1]=EditorGUILayout.DoubleField("Base cooling W/K",v[1]);
            v[2]=EditorGUILayout.DoubleField("Airflow cooling W/K/(m/s)",v[2]);
            v[3]=EditorGUILayout.DoubleField("Derate start °C",v[3]);
            v[4]=EditorGUILayout.DoubleField("Cutoff °C",v[4]);
        }
        private Vector2 scroll;
        private void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            body=(DronePhysicsBody)EditorGUILayout.ObjectField("Drone root",body,typeof(DronePhysicsBody),true);
            EditorGUILayout.HelpBox("Requires an existing Electrical battery profile. Estimates for testing: one effective temperature per component, air-speed cap 25 m/s. Saves a copy; preserves geometry, motors, battery and aerodynamic settings. Temperatures shown here are °C; JSON uses K.",MessageType.Info);
            initialC=EditorGUILayout.DoubleField("Initial component °C",initialC);
            referenceC=EditorGUILayout.DoubleField("Resistance reference °C",referenceC);
            alpha=EditorGUILayout.DoubleField("Winding R coefficient 1/K",alpha);
            Fields("Each motor",motor); Fields("Each ESC",esc); Fields("Battery pack",battery);
            using(new EditorGUI.DisabledScope(body==null || EditorApplication.isPlaying))
                if(GUILayout.Button("Validate and save thermal JSON")) Export();
            EditorGUILayout.EndScrollView();
        }
        private JObject Node(double[] v)=>new JObject {
            ["heatCapacityJPerK"]=v[0],["heatTransferWPerK"]=v[1],["airflowHeatTransferWPerKPerMps"]=v[2],
            ["maxAirSpeedMps"]=25,["initialTemperatureK"]=initialC+273.15,
            ["derateStartTemperatureK"]=v[3]+273.15,["cutoffTemperatureK"]=v[4]+273.15 };
        private void Export()
        {
            try
            {
                var source=body.droneProfile;
                if(source==null) throw new ArgumentException("Assign an Electrical battery profile first.");
                var env=body.environmentProfile ?? Resources.Load<TextAsset>("DronePhysics/environment_calm");
                var ds=Resources.Load<TextAsset>("DronePhysics/drone-profile.schema"); var es=Resources.Load<TextAsset>("DronePhysics/environment-profile.schema");
                var original=ProfileLoader.Load(source.text,env.text,ds.text,es.text);
                if(!original.Success) throw new ArgumentException(string.Join("\n",original.Issues));
                var json=JObject.Parse(source.text);
                if((string)json["powerSystem"]["battery"]["mode"]!="Electrical") throw new ArgumentException("Create an Electrical battery profile first: DroneLab → Power → Create Profile with Battery.");
                json["powerSystem"]["thermalEnabled"]=true; json["powerSystem"]["battery"]["thermal"]=Node(battery);
                foreach(var rotor in json["rotors"])
                {
                    var e=rotor["motor"]["electrical"]; e["thermal"]=Node(motor); e["escThermal"]=Node(esc);
                    e["resistanceReferenceTemperatureK"]=referenceC+273.15; e["resistanceTemperatureCoefficientPerK"]=alpha;
                }
                json.Remove("derived");
                ((JArray)json["parameterProvenance"]).Add(new JObject { ["path"]="powerSystem.battery.thermal / rotors.motor.electrical.thermal / escThermal",
                    ["sourceType"]="User",["source"]="Thermal editor estimates; explicit capacities, cooling and protection thresholds. Not calibrated hardware.",["confidence"]=.2 });
                var check=ProfileLoader.Load(json.ToString(),env.text,ds.text,es.text);
                if(!check.Success) throw new ArgumentException(string.Join("\n",check.Issues));
                string path=EditorUtility.SaveFilePanelInProject("Save thermal profile","drone_thermal","json","Save a new profile with thermal model.");
                if(string.IsNullOrEmpty(path)) return;
                if(AssetDatabase.LoadAssetAtPath<TextAsset>(path)==source) throw new ArgumentException("Choose a new JSON path.");
                File.WriteAllText(path,json.ToString(Formatting.Indented)+"\n"); AssetDatabase.ImportAsset(path);
                Undo.RecordObject(body,"Assign thermal profile"); body.droneProfile=AssetDatabase.LoadAssetAtPath<TextAsset>(path); EditorUtility.SetDirty(body);
                Debug.Log("DroneLab: thermal profile saved and assigned. Save the scene before Play.",body);
            }
            catch(Exception ex) { Debug.LogError("DroneLab thermal: "+ex.Message,body); }
        }
    }
}
