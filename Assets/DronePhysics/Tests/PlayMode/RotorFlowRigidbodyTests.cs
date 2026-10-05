using System;
using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class RotorFlowRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown()
        { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private DronePhysicsBody Create(bool pilot=false,string environment="environment_calm")
        {
            rig=new IsolatedPhysicsRig(pilot:pilot,instantaneous:!pilot,environment:environment,
                json:Resources.Load<TextAsset>("DronePhysics/quad_test_advanced_rotors").text);
            rig.Physics.SetArmed(true);
            if(!pilot) for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5);
            return rig.Physics;
        }
        [UnityTest] public IEnumerator HoverHasNoAirflowCorrectionsAndPowerRemainsFinite()
        {
            var p=Create(); rig.Step();
            foreach(var c in p.RotorThrustCorrectionN) Assert.That(c,Is.Zero);
            Assert.That(p.RotorFlappingMoment.magnitude,Is.Zero);
            Assert.That(p.Body.linearVelocity.magnitude,Is.LessThan(1e-5));
            Assert.That(p.Power.Current,Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator ForwardFlowAddsBoundedLiftAndNoseDownHubMoment()
        {
            var p=Create(); p.Body.linearVelocity=Vector3.forward*3; rig.Step();
            foreach(var c in p.RotorThrustCorrectionN) Assert.That(c,Is.EqualTo(.018).Within(1e-7));
            Assert.That(p.RotorFlappingMoment.x,Is.EqualTo(4*.000005*PhysicsMath.RpmToOmega(5000)*3).Within(1e-7));
            Assert.That(p.RotorDragTorque.magnitude,Is.LessThan(1e-7));
            Assert.That(p.Body.angularVelocity.x,Is.GreaterThan(0));
            Assert.That(p.Body.linearVelocity.y,Is.GreaterThan(0));
            var telemetry=p.GetRotorTelemetry(0);
            Assert.That(telemetry.ThrustCorrection,Is.EqualTo(.018).Within(1e-7));
            Assert.That(telemetry.FlappingMomentWorld.X,Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator ClimbAndDescentHaveOppositeInflowCorrections()
        {
            var p=Create(); p.Body.linearVelocity=Vector3.up*2; p.StepPhysics(.01f);
            foreach(var c in p.RotorThrustCorrectionN) Assert.That(c,Is.EqualTo(-.0001*PhysicsMath.RpmToOmega(5000)*2).Within(1e-7));
            p.Body.linearVelocity=Vector3.down*2; p.StepPhysics(.01f);
            foreach(var c in p.RotorThrustCorrectionN) Assert.That(c,Is.EqualTo(.0001*PhysicsMath.RpmToOmega(5000)*2).Within(1e-7));
            Assert.That(p.RotorFlappingMoment.magnitude,Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator HubMomentAndForceLeverArmAreAppliedExactlyOnce()
        {
            var p=Create(); for(int i=1;i<4;i++) p.SetMotorCommand(i,0);
            p.Body.linearVelocity=Vector3.forward*3;
            p.StepPhysics(.01f);
            var arm=p.transform.TransformPoint(ToUnity(p.Parameters.Rotors[0].Position))-p.Body.worldCenterOfMass;
            var force=Vector3.up*(float)p.ThrustN[0]+p.RotorDragForceN[0];
            var torque=Vector3.Cross(arm,force)+Vector3.up*(float)p.ReactionTorqueNm[0]+p.RotorFlappingMomentNm[0]+p.DragTorque;
            var q=p.Body.inertiaTensorRotation;
            var local=Quaternion.Inverse(q)*torque; var inertia=p.Body.inertiaTensor;
            var expected=q*new Vector3(local.x/inertia.x,local.y/inertia.y,local.z/inertia.z)*.01f;
            // Integrate the already prepared step, without preparing its forces again.
            p.gameObject.scene.GetPhysicsScene().Simulate(.01f);
            Assert.That((p.Body.angularVelocity-expected).magnitude,Is.LessThan(2e-5)); yield break;
        }
        [UnityTest] public IEnumerator StoppedFaultedRotorAndResetClearNewEffects()
        {
            var p=Create(); p.SetRotorDriveAuthority(0,0); p.Body.linearVelocity=new Vector3(4,-2,3); rig.Step();
            Assert.That(p.Omega[0],Is.Zero); Assert.That(p.RotorThrustCorrectionN[0],Is.Zero);
            Assert.That(p.RotorFlappingMomentNm[0].magnitude,Is.Zero);
            p.ResetMotorState();
            foreach(var c in p.RotorThrustCorrectionN) Assert.That(c,Is.Zero);
            Assert.That(p.RotorFlappingMoment.magnitude,Is.Zero);
            p.SetArmed(false); p.Body.linearVelocity=new Vector3(4,-2,3); rig.Step();
            foreach(var c in p.RotorThrustCorrectionN) Assert.That(c,Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator FullAtmosphereWindBatteryPilotScenarioStaysWithinEnvelope()
        {
            var p=Create(pilot:true,environment:"environment_final_acceptance");
            rig.Pilot.SetTestInput(0,0,0,0); rig.Step(800);
            Assert.That(p.IsReady,Is.True); Assert.That(p.Body.position.y,Is.InRange(99.5f,100.5f));
            Assert.That(p.Power.Soc,Is.InRange(0.0,1.0)); Assert.That(p.Power.Current,Is.GreaterThan(0));
            for(int i=0;i<4;i++)
            {
                double raw=p.Parameters.Rotors[i].Performance.Evaluate(p.Omega[i],0,p.Air.Density).Thrust;
                Assert.That(Math.Abs(p.RotorThrustCorrectionN[i]),Is.LessThanOrEqualTo(.25*raw+1e-8));
                Assert.That(p.RotorFlappingMomentNm[i].magnitude,Is.LessThanOrEqualTo(.2*raw*.0635+1e-7));
            }
            yield break;
        }
        private static Vector3 ToUnity(DVector3 v)=>new Vector3((float)v.X,(float)v.Y,(float)v.Z);
    }
}
