using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DroneLab.UI
{
    /// <summary>One metre-based schematic for editor previews and the simulated compound body.</summary>
    internal static class DroneSchematicModel
    {
        private struct Part
        {
            public string name;
            public PrimitiveType type;
            public Vector3 position, scale;
            public Quaternion rotation;
        }
        private static IEnumerable<Part> Parts(DroneProfileDocument document)
        {
            var dimensions=DroneModelViewport.Vec(document.profile["massProperties"]["dimensionsM"]);
            yield return new Part { name="Frame",type=PrimitiveType.Cube,rotation=Quaternion.identity,
                scale=new Vector3(Mathf.Max(.01f,dimensions.x*.4f),Mathf.Max(.01f,dimensions.y),Mathf.Max(.01f,dimensions.z*.4f)) };
            foreach(var rotor in (JArray)document.profile["rotors"]) {
                var p=DroneModelViewport.Vec(rotor["geometry"]["positionLocalM"]);
                float diameter=(float?)rotor["propeller"]?["diameterM"]??.127f;
                string id=(string)rotor["rotorId"];
                yield return new Part { name=id+" disc",type=PrimitiveType.Cylinder,position=p,
                    scale=new Vector3(diameter,.004f,diameter),rotation=Quaternion.identity };
                yield return new Part { name=id+" arm",type=PrimitiveType.Cube,position=p*.5f,
                    scale=new Vector3(.015f,.01f,Mathf.Max(.01f,p.magnitude)),
                    rotation=Quaternion.LookRotation(p.sqrMagnitude>0?p:Vector3.forward) };
            }
        }
        public static void Build(Transform parent,DroneProfileDocument document,bool colliders,int layer=0)
        {
            var material=Resources.Load<Material>("DroneLab/ConfiguratorSchematic");
            foreach(var part in Parts(document)) {
                var go=GameObject.CreatePrimitive(part.type);go.name=part.name;go.layer=layer;
                go.transform.SetParent(parent,false);go.transform.localPosition=part.position;
                go.transform.localRotation=part.rotation;go.transform.localScale=part.scale;
                go.GetComponent<Renderer>().sharedMaterial=material;
                var original=go.GetComponent<Collider>();
                if(!colliders || part.type==PrimitiveType.Cylinder) {
                    // The primitive cylinder's capsule is unsuitable for a thin rotor disc.
                    original.enabled=false;
                    if(Application.isPlaying)Object.Destroy(original);else Object.DestroyImmediate(original);
                    if(colliders) { var mesh=go.AddComponent<MeshCollider>();
                        mesh.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;mesh.convex=true; }
                }
            }
        }
        public static Bounds LocalBounds(DroneProfileDocument document)
        {
            bool first=true;var bounds=new Bounds();
            foreach(var part in Parts(document)) {
                // Unity's cylinder mesh is one unit wide and two units tall.
                var half=part.scale*.5f;if(part.type==PrimitiveType.Cylinder)half.y=part.scale.y;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2) {
                    var corner=part.position+part.rotation*Vector3.Scale(half,new Vector3(x,y,z));
                    if(first){bounds=new Bounds(corner,Vector3.zero);first=false;}else bounds.Encapsulate(corner);
                }
            }
            return bounds;
        }
    }
}
