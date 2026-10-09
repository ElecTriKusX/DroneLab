using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class BodyAerodynamicsTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        [UnityTest] public IEnumerator ProjectedBodyReceivesExpectedWindForce()
        {
            string json=Resources.Load<TextAsset>("DronePhysics/quad_test_projected_box").text;
            rig=new IsolatedPhysicsRig(instantaneous:true,environment:"environment_wind",json:json);
            var p=rig.Physics; p.SetArmed(true); for(int i=0;i<4;i++) p.SetMotorCommand(i,0.5);
            rig.Step();
            Assert.That(p.ProjectedAreaM2,Is.EqualTo(0.04).Within(1e-6));
            Assert.That(p.DragForce.x,Is.EqualTo(0.5*1.225*0.04*25).Within(1e-5));
            Assert.That(p.Body.linearVelocity.x,Is.GreaterThan(0));
            Assert.That(Mathf.Abs(p.Body.linearVelocity.y),Is.LessThan(0.001)); yield break;
        }
        [UnityTest] public IEnumerator PressureSurfacesDissipateAngularMotion()
        {
            string json=Resources.Load<TextAsset>("DronePhysics/quad_test_surfaces").text;
            rig=new IsolatedPhysicsRig(json:json); var p=rig.Physics;
            p.Body.angularVelocity=new Vector3(0,2,0); rig.Step(300);
            Assert.That(p.Body.angularVelocity.y,Is.InRange(0,1.9f));
            Assert.That(p.DragTorque.y,Is.LessThan(0)); yield break;
        }
        [Test] public void MeshCoordinatesRespectChildScaleRotationAndTranslatedRoot()
        {
            var root=new GameObject("Geometry meters test");
            try
            {
                root.transform.position=new Vector3(10,20,-3); root.transform.rotation=Quaternion.Euler(10,45,0);
                var mesh=new GameObject("Scaled imported visual"); mesh.transform.SetParent(root.transform,false);
                mesh.transform.localPosition=new Vector3(1,0,0); mesh.transform.localRotation=Quaternion.Euler(0,90,0);
                mesh.transform.localScale=new Vector3(2,3,4);
                var point=GeometryCoordinates.MeshVertexInMeters(root.transform,mesh.transform,new Vector3(1,2,3));
                Assert.That(Vector3.Distance(point,new Vector3(13,6,-2)),Is.LessThan(0.0001f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
