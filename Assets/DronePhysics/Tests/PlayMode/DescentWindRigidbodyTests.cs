using System;
using System.Collections;
using DroneLab.Simulation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class DescentWindRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private DronePhysicsBody Create(bool pilot=false,Action<JObject> edit=null)
        {
            var json=JObject.Parse(Resources.Load<TextAsset>("DronePhysics/quad_test_descent_wind").text); edit?.Invoke(json);
            rig=new IsolatedPhysicsRig(pilot:pilot,environment:"environment_dryden_frozen",json:json.ToString());
            rig.Physics.SetArmed(true); if(!pilot) for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5);
            return rig.Physics;
        }
        [UnityTest] public IEnumerator CombinedProfileRunsAllStatesAndRecorderUsesRotorDiagnostics()
        {
            var p=Create(); rig.Step(20); var f=DroneTelemetryRecorder.Capture(p,IsolatedPhysicsRig.Dt);
            Assert.That(p.Power.Current,Is.GreaterThan(0)); Assert.That(p.Power.Thermal.GeneratedEnergyJ,Is.GreaterThan(0));
            Assert.That(p.Parameters.InertialRotors,Is.True); Assert.That(p.Air.Density,Is.GreaterThan(0));
            Assert.That(f.WindSamplingRatio,Is.EqualTo(p.WindSamplingRatio));
            Assert.That(f.Rotors[0].Envelope.Value.AxialSpeed,Is.EqualTo(p.RotorEnvelopeSamples[0].AxialSpeed)); yield break;
        }
        [UnityTest] public IEnumerator LocalEnvelopeUsesWindAndRigidBodyPointVelocity()
        {
            var p=Create(); p.Body.linearVelocity=new Vector3(2,-4,3); p.Body.angularVelocity=new Vector3(1,2,3);
            for(int i=0;i<4;i++) p.Omega[i]=500;
            var expected=new double[4];
            for(int i=0;i<4;i++)
            {
                var r=p.Parameters.Rotors[i]; var point=p.transform.TransformPoint(new Vector3((float)r.Position.X,(float)r.Position.Y,(float)r.Position.Z));
                var w=p.Parameters.Environment.Sample(new DVector3(point.x,point.y,point.z),p.SimulationTimeS);
                var air=p.Body.GetPointVelocity(point)-new Vector3((float)w.X,(float)w.Y,(float)w.Z);
                expected[i]=Vector3.Dot(air,p.transform.up);
            }
            p.StepPhysics(IsolatedPhysicsRig.Dt);
            for(int i=0;i<4;i++) Assert.That(p.RotorEnvelopeSamples[i].AxialSpeed,Is.EqualTo(expected[i]).Within(1e-5)); yield break;
        }
        [UnityTest] public IEnumerator DeepDescentReportsEnvelopeAndDoesNotApplyAnInventedVrsLoss()
        {
            var p=Create(); p.Body.linearVelocity=new Vector3(0,-8,0); for(int i=0;i<4;i++) p.Omega[i]=500;
            p.StepPhysics(IsolatedPhysicsRig.Dt);
            foreach(var s in p.RotorEnvelopeSamples)
            { Assert.That(s.Exceeded,Is.True); Assert.That(s.Regime,Is.EqualTo(RotorFlowRegime.PositiveThrustDescent)); Assert.That(s.DescentToHoverInflowRatio,Is.GreaterThan(0)); }
            foreach(double thrust in p.ThrustN) Assert.That(thrust,Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator SamplingWarningUsesFastestMovingRotorPoint()
        {
            var p=Create(); p.Body.linearVelocity=new Vector3(5,0,0); p.Body.maxAngularVelocity=100; p.Body.angularVelocity=new Vector3(0,100,0);
            p.StepPhysics(.1f);
            Assert.That(p.WindSamplingRatio,Is.GreaterThan(.5)); Assert.That(p.WindUnderResolved,Is.True); yield break;
        }
        [UnityTest] public IEnumerator StoppedDisarmedRotorsHaveNoLiftWhileWindDragStillActs()
        {
            var p=Create(); p.SetArmed(false); p.Body.linearVelocity=new Vector3(0,-10,0); p.StepPhysics(IsolatedPhysicsRig.Dt);
            foreach(double thrust in p.ThrustN) Assert.That(thrust,Is.Zero);
            foreach(var s in p.RotorEnvelopeSamples) { Assert.That(s.Regime,Is.EqualTo(RotorFlowRegime.Stopped)); Assert.That(s.DescentToHoverInflowRatio,Is.Null); }
            Assert.That(p.DragForce.magnitude,Is.GreaterThan(0)); Assert.That(p.RotorGyroscopicMoment.magnitude,Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator HeightHoldInSpectralWindRemainsStableAndResetRestartsFieldClock()
        {
            var p=Create(pilot:true); rig.Pilot.SetTestInput(0,0,0,0); rig.Step(1000);
            Assert.That(p.Body.position.y,Is.InRange(99.5f,100.5f)); Assert.That(p.Power.Current,Is.GreaterThan(0));
            Assert.That(p.Power.Thermal.Motor(0).TemperatureK,Is.LessThan(343.15)); p.ResetMotorState();
            Assert.That(p.SimulationTimeS,Is.Zero); Assert.That(p.WindSamplingRatio,Is.Null);
            var origin=p.transform.position; p.StepPhysics(IsolatedPhysicsRig.Dt);
            var expected=p.Parameters.Environment.Sample(new DVector3(origin.x,origin.y,origin.z),0);
            Assert.That((p.WindVelocityWorld-new Vector3((float)expected.X,(float)expected.Y,(float)expected.Z)).magnitude,Is.LessThan(1e-5)); yield break;
        }
    }
}
