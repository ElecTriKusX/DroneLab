using System.Linq;
using DroneLab.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DroneLab.UI.Tests
{
    public sealed class DroneMeshSnapshotTests
    {
        [Test] public void SnapshotUsesRootMetresAndExcludesAssignedBladeSubtree()
        {
            var frame=new GameObject("frame");
            try {
                frame.transform.position=new Vector3(100,200,300);
                frame.transform.rotation=Quaternion.Euler(20,30,40);
                var model=new GameObject("model");model.transform.SetParent(frame.transform,false);
                model.transform.localScale=new Vector3(2,3,4);
                var body=GameObject.CreatePrimitive(PrimitiveType.Cube);body.transform.SetParent(model.transform,false);
                var propRoot=new GameObject("propRoot");propRoot.transform.SetParent(model.transform,false);
                var blade=GameObject.CreatePrimitive(PrimitiveType.Cube);blade.transform.SetParent(propRoot.transform,false);blade.transform.localPosition=new Vector3(30,0,0);
                DroneMeshSnapshot.Read(model.transform,frame.transform,new JObject{["FL"]="@/1"},out var vertices,out var triangles);
                Assert.That(vertices.Max(v=>v.X)-vertices.Min(v=>v.X),Is.EqualTo(2).Within(.0002));
                Assert.That(vertices.Max(v=>v.Y)-vertices.Min(v=>v.Y),Is.EqualTo(3).Within(.0002));
                Assert.That(vertices.Max(v=>v.Z)-vertices.Min(v=>v.Z),Is.EqualTo(4).Within(.0002));
                Assert.That(vertices.Max(v=>v.X),Is.LessThan(2));Assert.That(triangles.Length,Is.GreaterThan(0));
                var samples=DroneMeshProjection.Bake(vertices,triangles,128);Assert.That(samples.Count,Is.EqualTo(13));
                DroneMeshSnapshot.Read(model.transform,frame.transform,new JObject(),out var all,out _);
                Assert.That(all.Max(v=>v.X),Is.GreaterThan(50));
            } finally {Object.DestroyImmediate(frame);}
        }
    }
}
