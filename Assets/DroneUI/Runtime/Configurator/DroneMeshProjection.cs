using System;
using System.Threading;
using DroneLab.Physics;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    public static class DroneMeshProjection
    {
        public static JArray Bake(DVector3[] vertices,int[] triangles,int resolution,CancellationToken cancellation=default)
        {
            var result=new JArray();
            foreach(var direction in MeshSilhouette.BakeDirections()) {
                cancellation.ThrowIfCancellationRequested();
                double area=MeshSilhouette.Area(vertices,triangles,direction,resolution);
                if(area<=0)throw new ArgumentException("Силуэт имеет нулевую площадь. Для плоской детали используйте аэродинамические поверхности.");
                result.Add(new JObject{["directionLocal"]=new JArray(direction.X,direction.Y,direction.Z),["areaM2"]=area});
            }
            cancellation.ThrowIfCancellationRequested();return result;
        }
        public static bool Apply(JObject profile,JArray samples)
        {
            if(profile["derived"] is not JObject)profile["derived"]=new JObject();
            bool changed=!JToken.DeepEquals(profile["derived"]["projectedAreaLut"],samples);
            profile["derived"]["projectedAreaLut"]=samples.DeepClone();
            if((string)profile["bodyAerodynamics"]?["model"]=="ProjectedArea" && (string)profile["bodyAerodynamics"]?["projectedArea"]?["mode"]=="MeshDirectionalLUT") {
                changed|=!JToken.DeepEquals(profile["bodyAerodynamics"]["projectedArea"]["samples"],samples);
                profile["bodyAerodynamics"]["projectedArea"]["samples"]=samples.DeepClone();
            }
            return changed;
        }
        public static void Invalidate(JObject profile)
        {
            (profile["derived"] as JObject)?.Remove("projectedAreaLut");
            if((string)profile["bodyAerodynamics"]?["projectedArea"]?["mode"]=="MeshDirectionalLUT")
                (profile["bodyAerodynamics"]["projectedArea"] as JObject)?.Remove("samples");
        }
    }
}
