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
    public sealed class DroneRotorEffectsWindow : EditorWindow
    {
        private DronePhysicsBody body;
        private bool ground=true,drag=true;
        private bool inflow,flapping,lift;
        private double coefficient=1,minRatio=.25,maxGain=1.5,dragCoefficient=.0001;
        private double inflowCoefficient=.0001,flapCoefficient=.000005,liftCoefficient=.002;
        private double referenceDensity=1.225,maxSpeed=15,maxThrustFraction=.25,maxMomentRatio=.2;
        [MenuItem("DroneLab/Rotors/Create Profile with Rotor Effects")]
        public static void Open()
        {
            var window=GetWindow<DroneRotorEffectsWindow>("Rotor effects");
            window.body=Selection.activeTransform?.GetComponentInParent<DronePhysicsBody>();
        }
        private void OnGUI()
        {
            body=(DronePhysicsBody)EditorGUILayout.ObjectField("Drone root",body,typeof(DronePhysicsBody),true);
            ground=EditorGUILayout.Toggle("Ground effect",ground);
            if(ground)
            {
                coefficient=EditorGUILayout.DoubleField("Ground coefficient",coefficient);
                minRatio=EditorGUILayout.DoubleField("Minimum h / radius",minRatio);
                maxGain=EditorGUILayout.DoubleField("Maximum thrust multiplier",maxGain);
            }
            drag=EditorGUILayout.Toggle("Rotor drag",drag);
            if(drag) dragCoefficient=EditorGUILayout.DoubleField("Drag coefficient kg/rad",dragCoefficient);
            inflow=EditorGUILayout.Toggle("Axial inflow correction",inflow);
            if(inflow) inflowCoefficient=EditorGUILayout.DoubleField("Inflow coefficient kg/rad",inflowCoefficient);
            flapping=EditorGUILayout.Toggle("Blade flapping moment",flapping);
            if(flapping) flapCoefficient=EditorGUILayout.DoubleField("Flap coefficient kg*m/rad",flapCoefficient);
            lift=EditorGUILayout.Toggle("Translational lift",lift);
            if(lift) liftCoefficient=EditorGUILayout.DoubleField("Lift coefficient kg/m",liftCoefficient);
            if(inflow || flapping || lift)
            {
                referenceDensity=EditorGUILayout.DoubleField("Coefficient reference rho",referenceDensity);
                maxSpeed=EditorGUILayout.DoubleField("Flow limit m/s",maxSpeed);
                if(inflow || lift) maxThrustFraction=EditorGUILayout.DoubleField("Max thrust correction / T",maxThrustFraction);
                if(flapping) maxMomentRatio=EditorGUILayout.DoubleField("Max flap moment / (T*R)",maxMomentRatio);
            }
            EditorGUILayout.HelpBox("Demo estimates; not calibrated hardware data. Base performance must be out of ground effect. Ground gain changes thrust only. Existing geometry, propeller tables, mass and motor response are preserved.",MessageType.Info);
            using(new EditorGUI.DisabledScope(body==null || EditorApplication.isPlaying))
                if(GUILayout.Button("Validate and save new drone JSON")) Export();
        }
        private void Export()
        {
            try
            {
                var source=body.droneProfile ?? Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
                var env=body.environmentProfile ?? Resources.Load<TextAsset>("DronePhysics/environment_calm");
                var ds=Resources.Load<TextAsset>("DronePhysics/drone-profile.schema");
                var es=Resources.Load<TextAsset>("DronePhysics/environment-profile.schema");
                var original=ProfileLoader.Load(source.text,env.text,ds.text,es.text);
                if(!original.Success) throw new ArgumentException(string.Join("\n",original.Issues));
                var json=JObject.Parse(source.text); var modules=json["physicsConfiguration"]["modules"];
                modules["groundEffect"]=ground; modules["rotorAerodynamics"]=drag || lift;
                modules["inducedDrag"]=inflow; modules["bladeFlapping"]=flapping;
                if(ground) json["groundEffect"]=new JObject { ["coefficient"]=coefficient,["minHeightRadiusRatio"]=minRatio,["maxMultiplier"]=maxGain };
                foreach(var rotor in json["rotors"])
                {
                    var settings=rotor["advancedAerodynamics"] as JObject ?? new JObject();
                    settings["rotorDragCoefficientKgPerRad"]=drag ? dragCoefficient : 0;
                    settings["translationalLiftCoefficientKgPerM"]=lift ? liftCoefficient : 0;
                    if(inflow) settings["inducedDragCoefficient"]=inflowCoefficient;
                    if(flapping) settings["bladeFlappingCoefficient"]=flapCoefficient;
                    if(inflow || flapping || lift)
                    {
                        settings["referenceAirDensityKgM3"]=referenceDensity; settings["maxAirSpeedMps"]=maxSpeed;
                        if(inflow || lift) settings["maxThrustCorrectionFraction"]=maxThrustFraction;
                        if(flapping) settings["maxFlappingMomentRatio"]=maxMomentRatio;
                    }
                    rotor["advancedAerodynamics"]=settings;
                }
                json.Remove("derived");
                ((JArray)json["parameterProvenance"]).Add(new JObject { ["path"]="groundEffect / rotors.advancedAerodynamics",
                    ["sourceType"]="User",["source"]="Rotor effects editor: empirical coefficients entered by user; initial defaults are uncalibrated demo estimates.",["confidence"]=0.3 });
                var check=ProfileLoader.Load(json.ToString(),env.text,ds.text,es.text);
                if(!check.Success) throw new ArgumentException(string.Join("\n",check.Issues));
                foreach(var issue in check.Issues.Where(x=>x.Severity=="Warning")) Debug.LogWarning(issue.ToString(),body);
                string path=EditorUtility.SaveFilePanelInProject("Save rotor effects profile","drone_rotor_effects","json","Save a new profile with rotor effects.");
                if(string.IsNullOrEmpty(path)) return;
                if(AssetDatabase.LoadAssetAtPath<TextAsset>(path)==source) throw new ArgumentException("Choose a new JSON path to preserve the source profile.");
                File.WriteAllText(path,json.ToString(Formatting.Indented)+"\n"); AssetDatabase.ImportAsset(path);
                Undo.RecordObject(body,"Assign rotor effects profile"); body.droneProfile=AssetDatabase.LoadAssetAtPath<TextAsset>(path); EditorUtility.SetDirty(body);
                Debug.Log("DroneLab: rotor effects profile saved and assigned. Save the scene before Play.",body);
            }
            catch(Exception ex) { Debug.LogError("DroneLab rotor effects: "+ex.Message,body); }
        }
    }
}
