using System;
using System.Collections;
using System.IO;
using DroneLab.Simulation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class FinalAcceptanceRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private static string FinalJson=>Resources.Load<TextAsset>("DronePhysics/quad_test_final_acceptance").text;
        private void Ground()
        {
            var go=new GameObject("Final acceptance ground"); SceneManager.MoveGameObjectToScene(go,rig.Go.scene);
            go.transform.position=new Vector3(0,99.8f,0); go.AddComponent<BoxCollider>().size=new Vector3(100,.1f,100);
            UnityEngine.Physics.SyncTransforms();
        }
        [UnityTest] public IEnumerator CoupledHoverRecordsWindGroundBatteryAndFault()
        {
            rig=new IsolatedPhysicsRig(pilot:true,json:FinalJson,environment:"environment_final_acceptance"); Ground();
            var b=rig.Physics; b.SetArmed(true); rig.Pilot.SetTestInput(0,0,0,0);
            var csv=new FlightCsvWriter(new StringWriter(),new[]{"FL","FR","RR","RL"});
            double maxGroundGain=1,maxBodyForce=0,maxRotorForce=0,maxBodyTorque=0;
            b.StepPrepared+=(source,dt)=> {
                csv.Write(DroneTelemetryRecorder.Capture(source,dt,rig.Pilot));
                maxGroundGain=Math.Max(maxGroundGain,source.GroundEffectMultiplier[0]);
                maxBodyForce=Math.Max(maxBodyForce,source.DragForce.magnitude);
                maxRotorForce=Math.Max(maxRotorForce,source.RotorDragForce.magnitude);
                maxBodyTorque=Math.Max(maxBodyTorque,source.DragTorque.magnitude);
                Assert.That(source.Power.Soc,Is.InRange(0d,1d));
                Assert.That(source.Air.Density,Is.GreaterThan(0));
                Assert.That(float.IsNaN(source.Body.position.y),Is.False);
            };
            rig.Step(800);
            Assert.That(Mathf.Abs(b.Body.position.y-100),Is.LessThan(.2f));
            Assert.That(b.Body.angularVelocity.magnitude,Is.LessThan(.5f));
            Assert.That(maxGroundGain,Is.GreaterThan(1.005));
            Assert.That(maxBodyForce,Is.GreaterThan(.01)); Assert.That(maxRotorForce,Is.GreaterThan(.01)); Assert.That(maxBodyTorque,Is.GreaterThan(.0001));
            Assert.That(b.Power.Soc,Is.LessThan(1)); Assert.That(b.Power.TerminalEnergyJ,Is.GreaterThan(1)); Assert.That(csv.Rows,Is.EqualTo(800));
            double previous=b.Omega[0]; b.SetRotorDriveAuthority(0,0); rig.Step();
            Assert.That(b.Omega[0],Is.EqualTo(previous*Math.Exp(-IsolatedPhysicsRig.Dt/b.Parameters.Rotors[0].TauDown)).Within(1e-8));
            Assert.That(b.Power.RotorCurrentA[0],Is.Zero);
            rig.Step(15); Assert.That(b.Body.angularVelocity.magnitude,Is.GreaterThan(.1f));
            b.ResetMotorState(); Assert.That(b.Drive.HasFault,Is.False); Assert.That(b.Power.Soc,Is.EqualTo(1));
            yield break;
        }
        [UnityTest] public IEnumerator ManualPrincipalInertiaTorquePulseMatchesTensor()
        {
            rig=new IsolatedPhysicsRig(json:FinalJson,environment:"environment_calm"); var b=rig.Physics;
            // No motor/body motion before this single pulse; gravity produces no torque about COM.
            var p=b.Parameters;
            Vector3 torque=new Vector3(.02f,.03f,.04f);
            Quaternion q=b.Body.rotation*new Quaternion((float)p.RotationX,(float)p.RotationY,(float)p.RotationZ,(float)p.RotationW);
            Vector3 local=Quaternion.Inverse(q)*torque, moments=DronePhysicsBody.ToUnity(p.Inertia);
            Vector3 expected=q*new Vector3(local.x/moments.x,local.y/moments.y,local.z/moments.z)*IsolatedPhysicsRig.Dt;
            b.Body.AddTorque(torque,ForceMode.Force); rig.Step();
            Assert.That((b.Body.angularVelocity-expected).magnitude,Is.LessThan(expected.magnitude*.02f+1e-6f)); yield break;
        }
        [UnityTest] public IEnumerator SymmetricPoweredDescentDoesNotInventAStallTorque()
        {
            rig=new IsolatedPhysicsRig(instantaneous:true); var b=rig.Physics; b.SetArmed(true);
            for(int i=0;i<4;i++) b.SetMotorCommand(i,.4); rig.Step(100);
            Assert.That(b.Body.linearVelocity.y,Is.LessThan(-1));
            Assert.That(b.Body.angularVelocity.magnitude,Is.LessThan(.001f));
            Assert.That(Vector3.Angle(b.Body.rotation*Vector3.up,Vector3.up),Is.LessThan(.01f)); yield break;
        }
        [UnityTest] public IEnumerator SceneRotorMarkerEditsDoNotMutateLoadedPhysics()
        {
            rig=new IsolatedPhysicsRig(instantaneous:true); var b=rig.Physics;
            var go=new GameObject("Edited authoring rotor"); go.transform.SetParent(rig.Go.transform,false);
            var marker=go.AddComponent<RotorGeometryMarker>(); marker.rotorId="FL";
            go.transform.localPosition=new Vector3(5,2,3); go.transform.localRotation=Quaternion.Euler(180,0,0);
            b.SetArmed(true); for(int i=0;i<4;i++) b.SetMotorCommand(i,.5); rig.Step(50);
            Assert.That(b.Parameters.Rotors[0].Axis.Y,Is.EqualTo(1));
            Assert.That(b.Parameters.Rotors[0].Position.X,Is.EqualTo(-.14));
            Assert.That(b.Body.angularVelocity.magnitude,Is.LessThan(.001f)); yield break;
        }
        [UnityTest] public IEnumerator JsonInvertedAxesReverseThrustAndAreRejectedByQuadPilot()
        {
            var json=JObject.Parse(Resources.Load<TextAsset>("DronePhysics/quad_test_basic").text);
            foreach(var rotor in json["rotors"]) rotor["geometry"]["thrustAxisLocal"]=new JArray(0,-1,0);
            json["physicsConfiguration"]["modules"]["motorResponse"]=false;
            rig=new IsolatedPhysicsRig(json:json.ToString()); var b=rig.Physics;
            Assert.Throws<ArgumentException>(()=>new QuadAllocator(b.Parameters));
            b.SetArmed(true); for(int i=0;i<4;i++) b.SetMotorCommand(i,.5); rig.Step();
            Assert.That(b.Body.linearVelocity.y,Is.EqualTo(-2*9.81*IsolatedPhysicsRig.Dt).Within(1e-5)); yield break;
        }
    }
}
