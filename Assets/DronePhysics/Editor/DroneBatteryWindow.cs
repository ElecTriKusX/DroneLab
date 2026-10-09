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
    public sealed class DroneBatteryWindow : EditorWindow
    {
        private DronePhysicsBody body;
        private bool electrical;
        private bool inertia,gyro=true;
        private double spinInertia=.000005;
        private double capacity=1.3,soc=1,resistance=.06,maxCurrent=80;
        [MenuItem("DroneLab/Power/Create Profile with Battery")]
        public static void Open()
        {
            var window=GetWindow<DroneBatteryWindow>("Battery demo");
            window.body=Selection.activeTransform?.GetComponentInParent<DronePhysicsBody>();
        }
        private void OnGUI()
        {
            body=(DronePhysicsBody)EditorGUILayout.ObjectField("Drone root",body,typeof(DronePhysicsBody),true);
            electrical=EditorGUILayout.Toggle("Electrical motor model",electrical);
            inertia=EditorGUILayout.Toggle("Rotor inertia dynamics",inertia);
            if(inertia)
            {
                electrical=true;
                spinInertia=EditorGUILayout.DoubleField("Motor + prop Jr kg*m^2",spinInertia);
                gyro=EditorGUILayout.Toggle("Gyroscopic rotor moment",gyro);
            }
            capacity=EditorGUILayout.DoubleField("Capacity Ah",capacity);
            soc=EditorGUILayout.DoubleField("Initial SOC 0..1",soc);
            resistance=EditorGUILayout.DoubleField("Pack resistance ohm",resistance);
            maxCurrent=EditorGUILayout.DoubleField("Pack current limit A",maxCurrent);
            EditorGUILayout.HelpBox("Creates a 4S demo: 14.8 V nominal, OCV 12..16.8 V. Motor defaults: 900 Kv, 0.08 ohm, 0.4 A no-load, 30 A / 400 W limit, motor efficiency 0.85, ESC 0.95 / 35 A. Estimates for physics testing; existing motor power settings are preserved. Geometry, propeller data and ground effects are preserved.",MessageType.Info);
            using(new EditorGUI.DisabledScope(body==null || EditorApplication.isPlaying))
                if(GUILayout.Button("Validate and save new battery JSON")) Export();
        }
        private void Export()
        {
            try
            {
                var source=body.droneProfile ?? Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
                var env=body.environmentProfile ?? Resources.Load<TextAsset>("DronePhysics/environment_calm");
                var ds=Resources.Load<TextAsset>("DronePhysics/drone-profile.schema"); var es=Resources.Load<TextAsset>("DronePhysics/environment-profile.schema");
                var original=ProfileLoader.Load(source.text,env.text,ds.text,es.text);
                if(!original.Success) throw new ArgumentException(string.Join("\n",original.Issues));
                var json=JObject.Parse(source.text); var modules=json["physicsConfiguration"]["modules"];
                modules["batteryDischarge"]=true; modules["batteryVoltageSag"]=true; modules["motorElectrical"]=electrical;
                modules["gyroscopicRotorEffects"]=inertia && gyro;
                if(inertia) modules["motorResponse"]=true;
                var thermal=json["powerSystem"]["battery"]["thermal"]?.DeepClone();
                json["powerSystem"]["battery"]=new JObject {
                    ["mode"]=electrical ? "Electrical" : "Simple",["cellCount"]=4,["nominalVoltageV"]=14.8,
                    ["capacityAh"]=capacity,["initialSoc"]=soc,["internalResistanceOhm"]=resistance,["maxDischargeCurrentA"]=maxCurrent,
                    ["ocvCurve"]=new JArray(new JObject { ["soc"]=0,["voltageV"]=12 },new JObject { ["soc"]=.2,["voltageV"]=14 },
                        new JObject { ["soc"]=.8,["voltageV"]=15.6 },new JObject { ["soc"]=1,["voltageV"]=16.8 }) };
                if(thermal!=null) json["powerSystem"]["battery"]["thermal"]=thermal;
                foreach(var rotor in json["rotors"])
                {
                    rotor["motor"]["dynamicsModel"]=inertia ? "RotorInertia" : "FirstOrder";
                    if(inertia) rotor["motor"]["rotatingInertiaKgM2"]=spinInertia;
                    if(rotor["motor"]["electrical"]==null) rotor["motor"]["electrical"]=new JObject { ["motorKvRpmPerVolt"]=900,["motorResistanceOhm"]=.08,
                        ["noLoadCurrentA"]=.4,["maxCurrentA"]=30,["maxPowerW"]=400,["motorEfficiency"]=.85,["escEfficiency"]=.95,["escMaxCurrentA"]=35 };
                }
                json.Remove("derived");
                ((JArray)json["parameterProvenance"]).Add(new JObject { ["path"]="powerSystem / rotors.motor.electrical",
                    ["sourceType"]="User",["source"]="Battery editor: uncalibrated 4S demo OCV/motor defaults and optional spin-inertia estimate; existing electrical settings preserved.",["confidence"]=.2 });
                var check=ProfileLoader.Load(json.ToString(),env.text,ds.text,es.text);
                if(!check.Success) throw new ArgumentException(string.Join("\n",check.Issues));
                string path=EditorUtility.SaveFilePanelInProject("Save battery profile","drone_battery","json","Save a new drone profile with battery.");
                if(string.IsNullOrEmpty(path)) return;
                if(AssetDatabase.LoadAssetAtPath<TextAsset>(path)==source) throw new ArgumentException("Choose a new JSON path to preserve the source profile.");
                File.WriteAllText(path,json.ToString(Formatting.Indented)+"\n"); AssetDatabase.ImportAsset(path);
                Undo.RecordObject(body,"Assign battery profile"); body.droneProfile=AssetDatabase.LoadAssetAtPath<TextAsset>(path); EditorUtility.SetDirty(body);
                Debug.Log("DroneLab: battery profile saved and assigned. Save the scene before Play.",body);
            }
            catch(Exception ex) { Debug.LogError("DroneLab battery: "+ex.Message,body); }
        }
    }
}
