using System;
using System.IO;
using System.Linq;
using DroneLab.Physics;
using DroneLab.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DroneLab.Editor
{
    public static class DroneInertiaImportMenu
    {
        [MenuItem("DroneLab/Mass Properties/Import CAD Inertia Tensor")]
        public static void Import()
        {
            try
            {
                if(EditorApplication.isPlaying) throw new ArgumentException("Stop Play before importing inertia.");
                var body=Selection.activeTransform?.GetComponentInParent<DronePhysicsBody>();
                if(body==null || body.droneProfile==null) throw new ArgumentException("Select a drone root with an assigned Drone Profile.");
                if((body.transform.lossyScale-Vector3.one).sqrMagnitude>1e-8f) throw new ArgumentException("Physics root and parents need unit scale.");
                // Existing COM markers would otherwise silently overwrite the imported COM at next export.
                var author=body.GetComponent<DroneGeometryAuthoring>();
                if(author!=null && author.centerOfMassMarker!=null && !author.centerOfMassMarker.IsChildOf(body.transform)) throw new ArgumentException("COM marker must belong to the physics root.");
                string source=EditorUtility.OpenFilePanel("CAD tensor about COM, kg*m^2, physics-root axes","","json");
                if(string.IsNullOrEmpty(source)) return;
                string json=CadInertiaImport.Apply(body.droneProfile.text,File.ReadAllText(source));
                var env=body.environmentProfile ?? Resources.Load<TextAsset>("DronePhysics/environment_calm");
                var result=ProfileLoader.Load(json,env.text,Resources.Load<TextAsset>("DronePhysics/drone-profile.schema").text,Resources.Load<TextAsset>("DronePhysics/environment-profile.schema").text);
                if(!result.Success) throw new ArgumentException(string.Join("\n",result.Issues.Where(x=>x.Severity=="Error")));
                string path=EditorUtility.SaveFilePanelInProject("Save profile with CAD mass properties","drone_cad_inertia","json","Save the imported drone profile.");
                if(string.IsNullOrEmpty(path)) return;
                File.WriteAllText(path,json); AssetDatabase.ImportAsset(path); Undo.RecordObject(body,"Assign CAD inertia profile");
                body.droneProfile=AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if(author!=null && author.centerOfMassMarker!=null)
                {
                    Undo.RecordObject(author.centerOfMassMarker,"Update COM marker from CAD");
                    author.centerOfMassMarker.position=body.transform.TransformPoint(DronePhysicsBody.ToUnity(result.Parameters.CenterOfMass));
                }
                EditorUtility.SetDirty(body); EditorSceneManager.MarkSceneDirty(body.gameObject.scene);
                Debug.Log("Imported mass, COM and full inertia tensor. Rotor/aero/power data preserved; verify the controller for the new mass/COM before flight.",body);
            }
            catch(Exception ex) { Debug.LogError("DroneLab CAD inertia: "+ex.Message); }
        }
    }
}
