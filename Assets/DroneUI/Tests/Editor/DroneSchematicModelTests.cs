using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DroneLab.UI.Tests
{
    public sealed class DroneSchematicModelTests
    {
        private GameObject root;
        private static DroneProfileDocument Profile() => new DroneProfileDocument {
            profile=JObject.Parse(@"{'massProperties':{'dimensionsM':[0.3,0.1,0.3]},'rotors':[
                {'rotorId':'a','geometry':{'positionLocalM':[-0.8,-0.4,0.6]},'propeller':{'diameterM':0.3}},
                {'rotorId':'b','geometry':{'positionLocalM':[0.5,0.2,-0.5]},'propeller':{'diameterM':0.2}}]}")
        };
        [TearDown] public void TearDown(){if(root!=null)Object.DestroyImmediate(root);}
        [Test] public void EveryVisiblePartHasMatchingCollisionGeometryOnOneBody()
        {
            root=new GameObject("Schematic test");root.AddComponent<Rigidbody>();
            var profile=Profile();DroneSchematicModel.Build(root.transform,profile,true);
            UnityEngine.Physics.SyncTransforms();
            Assert.That(root.GetComponentsInChildren<Rigidbody>().Length,Is.EqualTo(1));
            var renderers=root.GetComponentsInChildren<Renderer>();
            Assert.That(renderers.Length,Is.EqualTo(5));
            foreach(var renderer in renderers) {
                var collider=renderer.GetComponent<Collider>();
                Assert.That(collider,Is.Not.Null);Assert.That(collider.enabled,Is.True);
                Assert.That(collider.attachedRigidbody,Is.SameAs(root.GetComponent<Rigidbody>()));
                Assert.That(Vector3.Distance(collider.bounds.center,renderer.bounds.center),Is.LessThan(1e-4));
                Assert.That(Vector3.Distance(collider.bounds.size,renderer.bounds.size),Is.LessThan(1e-4));
                if(collider is MeshCollider mesh)Assert.That(mesh.convex,Is.True);
            }
            Assert.That(root.GetComponentsInChildren<CapsuleCollider>(),Is.Empty);
            var actual=renderers[0].bounds;foreach(var renderer in renderers)actual.Encapsulate(renderer.bounds);
            var expected=DroneSchematicModel.LocalBounds(profile);
            Assert.That(Vector3.Distance(actual.center,expected.center),Is.LessThan(1e-4));
            Assert.That(Vector3.Distance(actual.size,expected.size),Is.LessThan(1e-4));
            Assert.That(expected.min.y,Is.LessThan(-.4f));Assert.That(expected.min.x,Is.LessThan(-.8f));
        }
        [Test] public void PreviewAndRuntimeUseIdenticalShapesWhilePreviewHasNoColliders()
        {
            root=new GameObject("Comparison test");
            var preview=new GameObject("Preview");preview.transform.SetParent(root.transform,false);
            var runtime=new GameObject("Runtime");runtime.transform.SetParent(root.transform,false);
            var profile=Profile();DroneSchematicModel.Build(preview.transform,profile,false,7);
            DroneSchematicModel.Build(runtime.transform,profile,true);
            Assert.That(preview.GetComponentsInChildren<Collider>(),Is.Empty);
            Assert.That(preview.transform.childCount,Is.EqualTo(runtime.transform.childCount));
            for(int i=0;i<preview.transform.childCount;i++) {
                var a=preview.transform.GetChild(i);var b=runtime.transform.GetChild(i);
                Assert.That(a.localPosition,Is.EqualTo(b.localPosition));Assert.That(a.localScale,Is.EqualTo(b.localScale));
                Assert.That(a.localRotation,Is.EqualTo(b.localRotation));Assert.That(a.gameObject.layer,Is.EqualTo(7));
                Assert.That(a.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(b.GetComponent<MeshFilter>().sharedMesh));
            }
        }
    }
}
