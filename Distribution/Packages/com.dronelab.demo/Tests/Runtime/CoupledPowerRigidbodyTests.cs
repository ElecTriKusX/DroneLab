using System;
using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class CoupledPowerRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown()
        { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private DronePhysicsBody Create(string profile="quad_test_coupled_power",bool pilot=false,string environment="environment_calm")
        {
            rig=new IsolatedPhysicsRig(pilot:pilot,environment:environment,json:Resources.Load<TextAsset>("DronePhysics/"+profile).text);
            rig.Physics.SetArmed(true); if(!pilot) for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5);
            return rig.Physics;
        }
        private void Spin()
        {
            for(int i=0;i<200;i++)
            {
                rig.Physics.Body.position=new Vector3(0,100,0); rig.Physics.Body.rotation=Quaternion.identity;
                rig.Physics.Body.linearVelocity=rig.Physics.Body.angularVelocity=Vector3.zero;
                rig.Step();
            }
        }
        [UnityTest] public IEnumerator StartupRecordsPositiveSpinEnergyAndElectricalBalance()
        {
            var p=Create(); rig.Step(); var b=p.Power;
            Assert.That(b.RotorSpinEnergyJ[0],Is.GreaterThan(0)); Assert.That(b.RotorAccelerationTorqueNm[0],Is.GreaterThan(0));
            Assert.That(p.ReactionTorqueNm[0],Is.LessThan(-p.PropellerTorqueNm[0]));
            Assert.That(b.MechanicalPower,Is.EqualTo(b.PropellerPower+b.SpinEnergyChangePower).Within(1e-7));
            var frame=DroneTelemetryRecorder.Capture(p,.01f);
            Assert.That(frame.Rotors[0].SpinEnergy,Is.GreaterThan(0)); Assert.That(frame.Rotors[0].MotorCurrent,Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator SteadySymmetricHoverHasNoGyroscopicTorque()
        {
            var p=Create(); Spin(); rig.Step(100);
            Assert.That(p.RotorGyroscopicMoment.magnitude,Is.LessThan(1e-7));
            Assert.That(p.Body.linearVelocity.magnitude,Is.LessThan(.02f));
            Assert.That(p.Body.angularVelocity.magnitude,Is.LessThan(.02f)); yield break;
        }
        [UnityTest] public IEnumerator DisarmAndDriveFaultCoastWithoutElectricalCurrent()
        {
            var p=Create(); Spin(); double old=p.Omega[0]; p.SetRotorDriveAuthority(0,0); rig.Step();
            Assert.That(p.Omega[0],Is.InRange(1.0,old)); Assert.That(p.Power.RotorCurrentA[0],Is.Zero);
            Assert.That(p.ReactionTorqueNm[0],Is.Zero); Assert.That(p.Power.RotorAccelerationTorqueNm[0],Is.LessThan(0));
            p.SetArmed(false); rig.Step(); Assert.That(p.Power.Current,Is.Zero);
            Assert.That(p.Omega[1],Is.GreaterThan(0)); p.ResetMotorState();
            Assert.That(p.Power.RotorSpinEnergyJ[1],Is.Zero); Assert.That(p.RotorGyroscopicMoment.magnitude,Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator MapLoadUsesActualRotorPointFlowInForcesAndPower()
        {
            var p=Create("quad_test_coupled_power_map"); Spin(); p.Body.linearVelocity=Vector3.up*3;
            var previous=(double[])p.Omega.Clone(); p.StepPhysics(.01f);
            double expected=0;
            for(int i=0;i<4;i++)
            {
                var r=p.Parameters.Rotors[i]; double midpoint=(previous[i]+p.Omega[i])/2;
                var sample=r.Performance.Evaluate(midpoint,3,p.Air.Density); expected+=sample.Torque*midpoint;
                Assert.That(p.PropellerTorqueNm[i],Is.EqualTo(sample.Torque).Within(1e-7));
                Assert.That(p.AdvanceRatio[i],Is.EqualTo(3/(midpoint/(2*Math.PI)*r.Diameter)).Within(1e-7));
            }
            Assert.That(p.Power.PropellerPower,Is.EqualTo(expected).Within(1e-6)); yield break;
        }
        [UnityTest] public IEnumerator UnbalancedSpinProducesExpectedWorldGyroscopeMoment()
        {
            var p=Create(); p.Omega[0]=500; for(int i=1;i<4;i++) p.SetMotorCommand(i,0);
            p.SetArmed(false); p.Body.rotation=Quaternion.Euler(0,30,0); p.Body.angularVelocity=p.transform.right;
            p.StepPhysics(.01f);
            double h=.000005*(500+p.Omega[0])/2;
            var expected=-p.transform.forward*(float)h;
            Assert.That((p.RotorGyroscopicMoment-expected).magnitude,Is.LessThan(1e-7)); yield break;
        }
        [UnityTest] public IEnumerator CompletePilotWindAtmosphereInertialBatteryScenarioStaysStable()
        {
            var p=Create(pilot:true,environment:"environment_final_acceptance"); rig.Pilot.SetTestInput(0,0,0,0); rig.Step(1000);
            Assert.That(p.IsReady,Is.True); Assert.That(p.Body.position.y,Is.InRange(99.5f,100.5f));
            Assert.That(p.Power.Soc,Is.InRange(0.0,1.0)); Assert.That(p.Power.Current,Is.GreaterThan(0));
            Assert.That(p.Power.MechanicalPower,Is.EqualTo(p.Power.PropellerPower+p.Power.SpinEnergyChangePower).Within(1e-6)); yield break;
        }
    }
}
