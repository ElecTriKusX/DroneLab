using System;
using System.Collections.Generic;
using System.Linq;
using DroneLab.Configurator;
using DroneLab.Physics;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DroneLab.UI
{
    internal static class DroneMeshSnapshot
    {
        // Unity objects are read on the main thread; rasterization receives plain arrays only.
        public static void Read(Transform model,Transform frame,JObject bindings,out DVector3[] vertices,out int[] triangles)
        {
            var points=new List<DVector3>();var indices=new List<int>();
            var excluded=bindings.Properties().SelectMany(p=>DroneVisualBindings.Paths(p.Value))
                .Select(path=>RuntimeGltfModelLoader.FindByPath(model,path)).Where(t=>t!=null && t!=model).ToArray();
            bool Excluded(Transform transform)=>excluded.Any(root=>transform==root || transform.IsChildOf(root));
            void Append(Mesh mesh,Transform transform) {
                if(mesh==null)return;
                if(!mesh.isReadable)throw new ArgumentException("Геометрия модели недоступна для расчёта силуэта. Нужны читаемые meshes.");
                var source=mesh.vertices;var mapping=new Dictionary<int,int>();
                foreach(int index in mesh.triangles) {
                    if(!mapping.TryGetValue(index,out int mapped)) {
                        mapped=points.Count;mapping.Add(index,mapped);
                        var point=frame.InverseTransformPoint(transform.TransformPoint(source[index]));
                        points.Add(new DVector3(point.x,point.y,point.z));
                    }
                    indices.Add(mapped);
                }
                if(points.Count>4000000 || indices.Count>12000000)throw new ArgumentException("Модель слишком сложная для расчёта силуэта: используйте упрощённую геометрию корпуса.");
            }
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>()) {
                var renderer=filter.GetComponent<MeshRenderer>();
                if(renderer!=null && renderer.enabled && !Excluded(filter.transform))Append(filter.sharedMesh,filter.transform);
            }
            foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                if(!renderer.enabled || Excluded(renderer.transform))continue;
                var mesh=new Mesh();try{renderer.BakeMesh(mesh);Append(mesh,renderer.transform);}finally{UnityEngine.Object.Destroy(mesh);}
            }
            if(points.Count==0 || indices.Count==0)throw new ArgumentException("В модели нет геометрии корпуса для расчёта силуэта. Проверьте привязки винтов.");
            vertices=points.ToArray();triangles=indices.ToArray();
        }
    }
}
