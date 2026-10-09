using System;
using System.IO;
using DroneLab.Physics;
using DroneLab.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneLab.Editor
{
    public sealed class DroneEnvironmentWindow : EditorWindow
    {
        private enum WindKind { None,Constant,Gust,Turbulence,CustomField,DrydenFrozen }
        private DronePhysicsBody body;
        private MonoBehaviour custom;
        private WindKind wind=WindKind.Turbulence;
        private Vector3 velocity=new Vector3(5,0,0);
        private bool atmosphere,overlay;
        private double intensity=2,timeScale=2,altitude;
        private int seed=48271,modes=32;
        private Vector3 sigma=new Vector3(.4f,.4f,.25f),length=new Vector3(20,20,10),direction=Vector3.right;
        private double advectionSpeed=5,minWave=.02,maxWave=20;
        private Vector2 scroll;
        [MenuItem("DroneLab/Environment/Create Environment Profile")]
        public static void Open()
        {
            var window=GetWindow<DroneEnvironmentWindow>("Environment");
            window.body=Selection.activeTransform?.GetComponentInParent<DronePhysicsBody>();
            window.custom=window.body?.customWindProvider;
        }
        private void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            body=(DronePhysicsBody)EditorGUILayout.ObjectField("Drone root",body,typeof(DronePhysicsBody),true);
            wind=(WindKind)EditorGUILayout.EnumPopup("Wind mode",wind);
            if(wind!=WindKind.None && wind!=WindKind.CustomField) velocity=EditorGUILayout.Vector3Field("Mean wind world m/s",velocity);
            if(wind==WindKind.Constant || wind==WindKind.Turbulence || wind==WindKind.DrydenFrozen) overlay=EditorGUILayout.Toggle("Periodic gust overlay",overlay);
            if(wind==WindKind.Gust || wind==WindKind.Turbulence || ((wind==WindKind.Constant || wind==WindKind.DrydenFrozen) && overlay))
            {
                intensity=EditorGUILayout.DoubleField("Fluctuation bound m/s",intensity);
                timeScale=EditorGUILayout.DoubleField("Time scale / gust period s",timeScale);
            }
            if(wind==WindKind.Turbulence || wind==WindKind.DrydenFrozen) seed=EditorGUILayout.IntField("Turbulence seed",seed);
            if(wind==WindKind.DrydenFrozen)
            {
                sigma=EditorGUILayout.Vector3Field("u/v/w sigma m/s",sigma);
                length=EditorGUILayout.Vector3Field("u/v/w length scale m",length);
                direction=EditorGUILayout.Vector3Field("Horizontal unit advection axis",direction);
                advectionSpeed=EditorGUILayout.DoubleField("Advection speed m/s",advectionSpeed);
                modes=EditorGUILayout.IntField("Modes per component (8..128)",modes);
                minWave=EditorGUILayout.DoubleField("Minimum kL",minWave);
                maxWave=EditorGUILayout.DoubleField("Maximum kL",maxWave);
            }
            if(wind==WindKind.CustomField) custom=(MonoBehaviour)EditorGUILayout.ObjectField("IWindProvider component",custom,typeof(MonoBehaviour),true);
            atmosphere=EditorGUILayout.Toggle("Variable atmosphere",atmosphere);
            if(atmosphere) altitude=EditorGUILayout.DoubleField("Start altitude MSL m",altitude);
            EditorGUILayout.HelpBox("Wind is in world axes; +X pushes right. Turbulence is a bounded reproducible demo field, not Dryden/CFD. DrydenFrozen is a finite-band spectrum on one horizontal frozen line; sigma is the full-spectrum RMS, not a peak bound. H holds height only. Variable atmosphere requires CtCq or PerformanceMap and uses sea-level 288.15 K / 101325 Pa. Existing drone parameters are preserved; RPM tables cannot be silently scaled with density.",MessageType.Info);
            using(new EditorGUI.DisabledScope(body==null || EditorApplication.isPlaying))
                if(GUILayout.Button("Validate and save new environment JSON")) Export();
            EditorGUILayout.EndScrollView();
        }
        private void Export()
        {
            try
            {
                var drone=body.droneProfile ?? Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
                var source=body.environmentProfile ?? Resources.Load<TextAsset>("DronePhysics/environment_calm");
                var ds=Resources.Load<TextAsset>("DronePhysics/drone-profile.schema"); var es=Resources.Load<TextAsset>("DronePhysics/environment-profile.schema");
                var json=JObject.Parse(source.text);
                json["airDensityMode"]=atmosphere ? "StandardAtmosphere" : "Constant";
                if(atmosphere) { json["temperatureK"]=288.15; json["pressurePa"]=101325; json["altitudeM"]=altitude; }
                json["windMode"]=wind.ToString(); json["windVelocityWorldMps"]=new JArray(velocity.x,velocity.y,velocity.z);
                bool gust=wind==WindKind.Gust || ((wind==WindKind.Constant || wind==WindKind.Turbulence || wind==WindKind.DrydenFrozen) && overlay);
                json["gustEnabled"]=gust;
                if(gust || wind==WindKind.Turbulence) { json["gustIntensityMps"]=intensity; json["gustTimeScaleS"]=timeScale; }
                if(wind==WindKind.Turbulence || wind==WindKind.DrydenFrozen) json["turbulenceSeed"]=seed;
                if(wind==WindKind.DrydenFrozen) json["dryden"]=new JObject {
                    ["sigmaUvwMps"]=new JArray(sigma.x,sigma.y,sigma.z),["lengthScaleUvwM"]=new JArray(length.x,length.y,length.z),
                    ["advectionDirectionWorld"]=new JArray(direction.x,direction.y,direction.z),["advectionSpeedMps"]=advectionSpeed,
                    ["modesPerComponent"]=modes,["minDimensionlessWaveNumber"]=minWave,["maxDimensionlessWaveNumber"]=maxWave };
                var check=ProfileLoader.Load(drone.text,json.ToString(),ds.text,es.text);
                if(!check.Success) throw new ArgumentException(string.Join("\n",check.Issues));
                if(wind==WindKind.CustomField && check.Parameters.Environment.WindEnabled && !(custom is IWindProvider))
                    throw new ArgumentException("Assign a component implementing IWindProvider. Example: DroneWindField on a separate stationary GameObject.");
                if(wind!=WindKind.None && !check.Parameters.Environment.WindEnabled) Debug.LogWarning("DroneLab: windInteraction is disabled in this drone JSON; wind forces will be ignored.",body);
                string path=EditorUtility.SaveFilePanelInProject("Save environment","environment_test","json","Save a new environment JSON.");
                if(string.IsNullOrEmpty(path)) return;
                if(AssetDatabase.LoadAssetAtPath<TextAsset>(path)==source) throw new ArgumentException("Choose a new JSON path to preserve the source environment.");
                File.WriteAllText(path,json.ToString(Formatting.Indented)+"\n"); AssetDatabase.ImportAsset(path);
                Undo.RecordObject(body,"Assign environment profile"); body.environmentProfile=AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if(wind==WindKind.CustomField) body.customWindProvider=custom;
                EditorUtility.SetDirty(body); EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
                Debug.Log("DroneLab: environment saved and assigned. Save the scene before Play.",body);
            }
            catch(Exception ex) { Debug.LogError("DroneLab environment: "+ex.Message,body); }
        }
    }
}
