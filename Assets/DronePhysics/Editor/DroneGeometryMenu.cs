using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DroneLab.Physics;
using DroneLab.Simulation;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace DroneLab.Editor
{
    public static class DroneGeometryMenu
    {
        private static DronePhysicsBody SelectedBody()
        {
            if(EditorApplication.isPlaying) throw new ArgumentException("Stop Play before editing geometry.");
            var body=Selection.activeTransform?.GetComponentInParent<DronePhysicsBody>();
            if(body==null) throw new ArgumentException("Select the drone root with DronePhysicsBody.");
            if((body.transform.lossyScale-Vector3.one).sqrMagnitude>1e-8f) throw new ArgumentException("Physics root and parents must have scale (1,1,1).");
            return body;
        }
        private static TextAsset DroneAsset(DronePhysicsBody b) => b.droneProfile!=null ? b.droneProfile : Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
        private static TextAsset EnvironmentAsset(DronePhysicsBody b) => b.environmentProfile!=null ? b.environmentProfile : Resources.Load<TextAsset>("DronePhysics/environment_calm");
        private static ProfileLoadResult Validate(DronePhysicsBody b,string json)
        {
            var result=ProfileLoader.Load(json,EnvironmentAsset(b).text,
                Resources.Load<TextAsset>("DronePhysics/drone-profile.schema").text,Resources.Load<TextAsset>("DronePhysics/environment-profile.schema").text);
            if(!result.Success) throw new ArgumentException(string.Join("\n",result.Issues.Where(x=>x.Severity=="Error")));
            return result;
        }
        private static Transform NewMarker(string name,Transform parent,Vector3 position)
        {
            var go=new GameObject(name); Undo.RegisterCreatedObjectUndo(go,"Create drone geometry marker");
            go.transform.SetParent(parent,false); go.transform.localPosition=position; return go.transform;
        }
        [MenuItem("DroneLab/Geometry/Create Markers from Profile")]
        public static void CreateMarkers()
        {
            try
            {
                var body=SelectedBody(); var profile=Validate(body,DroneAsset(body).text).Drone;
                var author=body.GetComponent<DroneGeometryAuthoring>();
                if(author!=null && author.rotors!=null && author.rotors.Length>0)
                { Selection.activeGameObject=body.gameObject; Debug.Log("Geometry markers already exist; edit them in the hierarchy.",body); return; }
                if(author==null) author=Undo.AddComponent<DroneGeometryAuthoring>(body.gameObject);
                Undo.RecordObject(author,"Set geometry markers");
                var parent=NewMarker("Physics Markers",body.transform,Vector3.zero);
                author.centerOfMassMarker=NewMarker("COM",parent,DronePhysicsBody.ToUnity(DVector3.From(profile.massProperties.centerOfMassLocalM)));
                author.dragPointMarker=NewMarker("Drag Point",parent,DronePhysicsBody.ToUnity(DVector3.From(
                    profile.bodyAerodynamics.dragApplicationPointLocalM ?? profile.massProperties.centerOfMassLocalM)));
                var rotors=new List<RotorGeometryMarker>();
                foreach(var r in profile.rotors)
                {
                    var point=NewMarker("Rotor "+r.rotorId,parent,DronePhysicsBody.ToUnity(DVector3.From(r.geometry.positionLocalM)));
                    point.localRotation=Quaternion.FromToRotation(Vector3.up,DronePhysicsBody.ToUnity(DVector3.From(r.geometry.thrustAxisLocal)));
                    var marker=Undo.AddComponent<RotorGeometryMarker>(point.gameObject);
                    marker.rotorId=r.rotorId; marker.clockwise=r.geometry.spinDirection=="CW"; rotors.Add(marker);
                }
                author.rotors=rotors.ToArray(); var surfaces=new List<AeroSurfaceMarker>();
                if(profile.bodyAerodynamics.surfaces!=null) foreach(var s in profile.bodyAerodynamics.surfaces)
                {
                    var point=NewMarker("Surface "+s.surfaceId,parent,DronePhysicsBody.ToUnity(DVector3.From(s.positionLocalM)));
                    point.localRotation=Quaternion.FromToRotation(Vector3.up,DronePhysicsBody.ToUnity(DVector3.From(s.normalLocal)));
                    var marker=Undo.AddComponent<AeroSurfaceMarker>(point.gameObject);
                    marker.surfaceId=s.surfaceId; marker.areaM2=(float)s.areaM2; marker.dragCoefficient=(float)s.dragCoefficient; surfaces.Add(marker);
                }
                author.surfaces=surfaces.ToArray();
                if(profile.bodyAerodynamics.model=="ProjectedArea") author.projectedDragCoefficient=(float)profile.bodyAerodynamics.dragCoefficient;
                EditorUtility.SetDirty(author); Selection.activeGameObject=body.gameObject;
            }
            catch(Exception ex) { Debug.LogError("DroneLab geometry: "+ex.Message); }
        }
        [MenuItem("DroneLab/Geometry/Export Profile with Box Drag")]
        public static void ExportBox() => Export("Box");
        [MenuItem("DroneLab/Geometry/Export Profile with Mesh Silhouette")]
        public static void ExportMesh() => Export("Mesh");
        [MenuItem("DroneLab/Geometry/Export Profile with Surfaces")]
        public static void ExportSurfaces() => Export("Surfaces");
        private static JArray Vector(Vector3 v) => new JArray((double)v.x,(double)v.y,(double)v.z);
        private static JArray Vector(DVector3 v) => new JArray(v.X,v.Y,v.Z);
        private static void CheckMarker(Transform point,Transform root)
        {
            if(point==null || !point.IsChildOf(root)) throw new ArgumentException("All markers must be assigned and be children of the physics root.");
        }
        private static void Provenance(JObject json,string path,string source,string type="AutoGeometry")
            => ((JArray)json["parameterProvenance"]).Add(new JObject { ["path"]=path,["sourceType"]=type,["source"]=source,["confidence"]=0.5 });
        private static void Export(string mode)
        {
            try
            {
                var body=SelectedBody(); var root=body.transform; var author=body.GetComponent<DroneGeometryAuthoring>();
                if(author==null) throw new ArgumentException("Run Create Markers from Profile first.");
                CheckMarker(author.centerOfMassMarker,root); CheckMarker(author.dragPointMarker,root);
                var json=JObject.Parse(DroneAsset(body).text); Validate(body,json.ToString());
                var sourceRotors=(JArray)json["rotors"];
                if(author.rotors==null || author.rotors.Length!=sourceRotors.Count) throw new ArgumentException("Assign exactly one marker for every rotor ID in the profile.");
                var markers=new Dictionary<string,RotorGeometryMarker>();
                foreach(var marker in author.rotors)
                {
                    if(marker==null) throw new ArgumentException("Missing rotor marker.");
                    CheckMarker(marker.transform,root);
                    if(string.IsNullOrWhiteSpace(marker.rotorId) || markers.ContainsKey(marker.rotorId)) throw new ArgumentException("Rotor marker IDs must be nonempty and unique.");
                    markers.Add(marker.rotorId,marker);
                }
                foreach(var rotor in sourceRotors)
                {
                    if(!markers.TryGetValue((string)rotor["rotorId"],out var marker)) throw new ArgumentException("A rotor ID does not match the source profile.");
                    rotor["geometry"]["positionLocalM"]=Vector(root.InverseTransformPoint(marker.transform.position));
                    rotor["geometry"]["thrustAxisLocal"]=Vector(root.InverseTransformDirection(marker.transform.up).normalized);
                    rotor["geometry"]["spinDirection"]=marker.clockwise ? "CW":"CCW";
                }
                json["massProperties"]["centerOfMassLocalM"]=Vector(root.InverseTransformPoint(author.centerOfMassMarker.position));
                Provenance(json,"rotors.geometry","Rotor markers in physics-root local meters; engine/propeller characteristics preserved.");
                Provenance(json,"massProperties.centerOfMassLocalM","User-placed COM marker; no mass estimation from mesh.","User");
                JObject aero;
                if(mode=="Surfaces")
                {
                    if(author.surfaces==null || author.surfaces.Length==0) throw new ArgumentException("Assign at least one AeroSurfaceMarker in Surfaces.");
                    var surfaces=new JArray();
                    foreach(var marker in author.surfaces)
                    {
                        if(marker==null) throw new ArgumentException("Missing surface marker."); CheckMarker(marker.transform,root);
                        surfaces.Add(new JObject { ["surfaceId"]=marker.surfaceId,["positionLocalM"]=Vector(root.InverseTransformPoint(marker.transform.position)),
                            ["normalLocal"]=Vector(root.InverseTransformDirection(marker.transform.up).normalized),["areaM2"]=(double)marker.areaM2,["dragCoefficient"]=(double)marker.dragCoefficient });
                    }
                    aero=new JObject { ["model"]="Surfaces",["surfaces"]=surfaces };
                    Provenance(json,"bodyAerodynamics.surfaces","User-placed two-sided pressure patches; no shielding or lift polar.","User");
                }
                else
                {
                    DVector3 dimensions=DVector3.From(json["massProperties"]["dimensionsM"].ToObject<double[]>());
                    DVector3[] vertices=null; int[] triangles=null;
                    if(author.bodyMeshRoot!=null)
                    {
                        CheckMarker(author.bodyMeshRoot,root); ReadMesh(author.bodyMeshRoot,root,out vertices,out triangles);
                        dimensions=BoundsSize(vertices); json["massProperties"]["dimensionsM"]=Vector(dimensions);
                        Provenance(json,"massProperties.dimensionsM","AABB of selected mesh subtree in physics-root meters; AutoBox inertia remains a uniform-box approximation.");
                    }
                    JObject area;
                    if(mode=="Box")
                        area=new JObject { ["mode"]="AxisApproximation",["referenceAreaM2"]=new JArray(dimensions.Y*dimensions.Z,dimensions.X*dimensions.Z,dimensions.X*dimensions.Y) };
                    else
                    {
                        if(vertices==null) throw new ArgumentException("Assign Body Mesh Root to the stationary body mesh subtree.");
                        var directions=MeshSilhouette.BakeDirections(); var samples=new JArray();
                        for(int i=0;i<directions.Length;i++)
                        {
                            if(EditorUtility.DisplayCancelableProgressBar("DroneLab silhouette","Baking direction "+(i+1)+" / "+directions.Length,(float)i/directions.Length)) throw new OperationCanceledException();
                            double value=MeshSilhouette.Area(vertices,triangles,directions[i],author.silhouetteResolution);
                            if(value<=0) throw new ArgumentException("Zero silhouette area. Select a volumetric body mesh or use Surfaces for a thin plate.");
                            samples.Add(new JObject { ["directionLocal"]=Vector(directions[i]),["areaM2"]=value });
                        }
                        area=new JObject { ["mode"]="MeshDirectionalLUT",["samples"]=samples };
                        Provenance(json,"bodyAerodynamics.projectedArea","Union silhouette of "+author.bodyMeshRoot.name+", resolution "+author.silhouetteResolution+", 13 unsigned axes; static mesh snapshot.");
                    }
                    aero=new JObject { ["model"]="ProjectedArea",["dragCoefficient"]=(double)author.projectedDragCoefficient,["projectedArea"]=area };
                    Provenance(json,"bodyAerodynamics.dragCoefficient","User-specified Cd for projected frontal area; not inferred from geometry.","User");
                }
                aero["dragApplicationPointLocalM"]=Vector(root.InverseTransformPoint(author.dragPointMarker.position));
                json["bodyAerodynamics"]=aero; json["physicsConfiguration"]["modules"]["bodyDrag"]=true;
                json.Remove("derived"); // Do not retain stale geometry/inertia caches.
                var validated=Validate(body,json.ToString());
                try { new QuadAllocator(validated.Parameters); }
                catch(ArgumentException ex) { Debug.LogWarning("Physics profile valid, but DroneTestPilot cannot control this layout: "+ex.Message,body); }
                string path=EditorUtility.SaveFilePanelInProject("Save drone geometry profile","drone_geometry_"+mode.ToLowerInvariant(),"json","Save a new physics profile.");
                if(string.IsNullOrEmpty(path)) return;
                File.WriteAllText(path,json.ToString()+"\n"); AssetDatabase.ImportAsset(path);
                Undo.RecordObject(body,"Assign geometry profile"); body.droneProfile=AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                EditorUtility.SetDirty(body); Debug.Log("DroneLab: saved and assigned "+path+". Save the scene, then enter Play.",body);
            }
            catch(OperationCanceledException) { }
            catch(Exception ex) { Debug.LogError("DroneLab geometry: "+ex.Message); }
            finally { EditorUtility.ClearProgressBar(); }
        }
        private static DVector3 BoundsSize(DVector3[] vertices)
        {
            double xmin=double.PositiveInfinity,ymin=xmin,zmin=xmin,xmax=double.NegativeInfinity,ymax=xmax,zmax=xmax;
            foreach(var v in vertices) { xmin=Math.Min(xmin,v.X); xmax=Math.Max(xmax,v.X); ymin=Math.Min(ymin,v.Y); ymax=Math.Max(ymax,v.Y); zmin=Math.Min(zmin,v.Z); zmax=Math.Max(zmax,v.Z); }
            return new DVector3(xmax-xmin,ymax-ymin,zmax-zmin);
        }
        private static void ReadMesh(Transform meshRoot,Transform physicsRoot,out DVector3[] vertices,out int[] triangles)
        {
            var points=new List<DVector3>(); var indices=new List<int>();
            void Append(Mesh mesh,Transform transform)
            {
                if(mesh==null) return;
                var verticesSource=mesh.vertices; var mapping=new Dictionary<int,int>();
                foreach(int index in mesh.triangles)
                {
                    if(!mapping.TryGetValue(index,out int output))
                    {
                        output=points.Count; mapping.Add(index,output);
                        points.Add(DronePhysicsBody.FromUnity(GeometryCoordinates.MeshVertexInMeters(physicsRoot,transform,verticesSource[index])));
                    }
                    indices.Add(output);
                }
                if(points.Count>2000000 || indices.Count>6000000) throw new ArgumentException("Mesh exceeds the offline bake limit; use simplified body geometry.");
            }
            foreach(var filter in meshRoot.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null || !renderer.enabled || filter.sharedMesh==null) continue;
                if(!filter.sharedMesh.isReadable) throw new ArgumentException("Enable Read/Write in Model Import Settings for "+filter.sharedMesh.name+".");
                Append(filter.sharedMesh,filter.transform);
            }
            foreach(var renderer in meshRoot.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if(!renderer.enabled || renderer.sharedMesh==null) continue;
                var mesh=new Mesh();
                try { renderer.BakeMesh(mesh); Append(mesh,renderer.transform); }
                finally { UnityEngine.Object.DestroyImmediate(mesh); }
            }
            if(points.Count==0 || indices.Count==0) throw new ArgumentException("No visible triangle meshes found under Body Mesh Root.");
            vertices=points.ToArray(); triangles=indices.ToArray();
        }
    }
}
