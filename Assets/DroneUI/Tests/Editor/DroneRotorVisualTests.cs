using System;
using DroneLab.Configurator;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DroneLab.UI.Tests
{
    public sealed class DroneRotorVisualTests
    {
        private GameObject frame;
        private Transform model,blade,body;
        private JArray rotors;
        [SetUp] public void Setup()
        {
            frame=new GameObject("Physical frame");model=new GameObject("Imported model").transform;model.SetParent(frame.transform,false);
            model.localScale=Vector3.one*.002f;
            blade=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;blade.name="same/name";blade.SetParent(model,false);
            body=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;body.name="same/name";body.SetParent(model,false);
            rotors=JArray.Parse("[{\"rotorId\":\"R1\",\"geometry\":{\"positionLocalM\":[1,0,0],\"thrustAxisLocal\":[0,1,0],\"spinDirection\":\"CW\"}},{\"rotorId\":\"R2\",\"geometry\":{\"positionLocalM\":[0,0,0],\"thrustAxisLocal\":[0,1,0],\"spinDirection\":\"CCW\"}}]");
        }
        [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(frame);}
        [Test] public void IndexPathsResolveDuplicateAndSlashNames()
        {
            string first=RuntimeGltfModelLoader.PathFrom(model,blade),second=RuntimeGltfModelLoader.PathFrom(model,body);
            Assert.That(first,Is.Not.EqualTo(second));Assert.That(RuntimeGltfModelLoader.FindByPath(model,first),Is.SameAs(blade));
            Assert.That(RuntimeGltfModelLoader.FindByPath(model,"@/9"),Is.Null);
        }
        [Test] public void ZeroSourcePivotRotatesAroundPhysicalPointDespiteVisualScale()
        {
            var animation=new DroneRotorVisuals();animation.Bind(model,rotors,new JObject{["R1"]=RuntimeGltfModelLoader.PathFrom(model,blade)});
            animation.Step(frame.transform,rotors,_=>Math.PI,.5);
            var point=frame.transform.InverseTransformPoint(blade.TransformPoint(new Vector3(500,0,100)));
            Assert.That(Vector3.Distance(point,new Vector3(1.2f,0,0)),Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(body.localPosition,Vector3.zero),Is.LessThan(1e-5f));
            animation.Restore();Assert.That(Vector3.Distance(blade.localPosition,Vector3.zero),Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(blade.localRotation,Quaternion.identity),Is.LessThan(1e-4f));
        }
        [Test] public void AnimationFollowsMovingRotatedPhysicalFrameWithoutAccumulatedPose()
        {
            var animation=new DroneRotorVisuals();animation.Bind(model,rotors,new JObject{["R1"]=RuntimeGltfModelLoader.PathFrom(model,blade)});
            animation.Step(frame.transform,rotors,_=>Math.PI,.5);
            frame.transform.SetPositionAndRotation(new Vector3(20,3,-4),Quaternion.Euler(15,40,25));
            animation.Step(frame.transform,rotors,_=>Math.PI,.5);
            var point=frame.transform.InverseTransformPoint(blade.TransformPoint(new Vector3(500,0,100)));
            Assert.That(Vector3.Distance(point,new Vector3(1,0,-.2f)),Is.LessThan(1e-4f));
        }
        [Test] public void ConflictingBindingsAreSkippedAndReported()
        {
            var animation=new DroneRotorVisuals();string path=RuntimeGltfModelLoader.PathFrom(model,blade);
            animation.Bind(model,rotors,new JObject{["R1"]=path,["R2"]=path});animation.Step(frame.transform,rotors,_=>Math.PI,.5);
            Assert.That(animation.Issues,Is.Not.Empty);Assert.That(blade.localPosition,Is.EqualTo(Vector3.zero));
        }
        [Test] public void MultiplePartsShareOnePhaseAndRemainInTheirParents()
        {
            var staticPart=GameObject.CreatePrimitive(PrimitiveType.Cube);staticPart.transform.SetParent(model,false);
            body.localPosition=new Vector3(0,0,100);var animation=new DroneRotorVisuals();
            animation.Bind(model,rotors,new JObject{["R1"]=new JArray(RuntimeGltfModelLoader.PathFrom(model,blade),RuntimeGltfModelLoader.PathFrom(model,body))});
            animation.Step(frame.transform,rotors,_=>Math.PI,.5);
            Assert.That(Quaternion.Angle(blade.rotation,body.rotation),Is.LessThan(1e-4f));Assert.That(blade.parent,Is.SameAs(model));Assert.That(body.parent,Is.SameAs(model));
            Assert.That(Vector3.Distance(body.position-blade.position,new Vector3(.2f,0,0)),Is.LessThan(1e-5f));
        }
    }
}
