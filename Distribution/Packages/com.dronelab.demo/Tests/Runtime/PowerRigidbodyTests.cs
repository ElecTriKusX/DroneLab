using System;
using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class PowerRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private DronePhysicsBody Create(string mode="simple",Func<string,string> edit=null,bool pilot=false)
        {
            string json=Resources.Load<TextAsset>("DronePhysics/quad_test_battery_"+mode).text;
            rig=new IsolatedPhysicsRig(pilot:pilot,instantaneous:!pilot,json:edit==null ? json : edit(json));
            rig.Physics.SetArmed(true); for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5); return rig.Physics;
        }
        [UnityTest] public IEnumerator SimpleHoverDrawsChargeOncePerPhysicsStep()
        {
            var p=Create(); double initial=p.Power.Soc; rig.Step(100);
            Assert.That(p.Body.linearVelocity.magnitude,Is.LessThan(.02f));
            Assert.That(p.Power.Current,Is.GreaterThan(0)); Assert.That(p.Power.Soc,Is.LessThan(initial));
            Assert.That(p.Power.ConsumedAh,Is.EqualTo((initial-p.Power.Soc)*1.3).Within(1e-10));
            Assert.That(p.Power.ElectricalPower,Is.GreaterThan(p.Power.MechanicalPower)); yield break;
        }
        [UnityTest] public IEnumerator ElectricalHoverUsesLoadedBusAndPreservesMeasuredCurrent()
        {
            var p=Create("electrical"); rig.Step(100);
            Assert.That(p.Power.TerminalVoltage,Is.LessThan(p.Power.OpenVoltage)); Assert.That(p.Power.Current,Is.GreaterThan(0));
            Assert.That(p.Body.linearVelocity.magnitude,Is.LessThan(.02f));
            Assert.That(p.MeasuredCurrentA[0],Is.Null); Assert.That(p.Power.RotorCurrentA[0],Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator PackLimitActuallyReducesThrustInRigidbody()
        {
            var p=Create(edit:j=>j.Replace("\"maxDischargeCurrentA\": 80","\"maxDischargeCurrentA\": 5")); rig.Step(10);
            Assert.That(p.Power.Limited,Is.True); Assert.That(p.Power.Current,Is.LessThanOrEqualTo(5));
            Assert.That(p.Omega[0],Is.LessThan(PhysicsMath.RpmToOmega(5000)));
            Assert.That(p.ThrustN[0]*4,Is.LessThan(9.81)); Assert.That(p.Body.linearVelocity.y,Is.LessThan(0)); yield break;
        }
        [UnityTest] public IEnumerator EmptyBatteryCannotHoverAndResetRestoresInitialState()
        {
            var p=Create(edit:j=>j.Replace("\"initialSoc\": 1","\"initialSoc\": 0")); rig.Step(20);
            Assert.That(p.Omega[0]+p.ThrustN[0]+p.Power.Current,Is.Zero); Assert.That(p.Body.linearVelocity.y,Is.LessThan(-1.5));
            Assert.That(p.Power.Soc,Is.Zero); p.ResetMotorState(); Assert.That(p.Power.Soc,Is.Zero); Assert.That(p.Armed,Is.False); yield break;
        }
        [UnityTest] public IEnumerator DisarmingStopsBatteryDrawAndBackspaceResetRefillsDemoPack()
        {
            var p=Create(); rig.Step(50); Assert.That(p.Power.Soc,Is.LessThan(1)); p.SetArmed(false); double soc=p.Power.Soc;
            rig.Step(50); Assert.That(p.Power.Current,Is.Zero); Assert.That(p.Power.Soc,Is.EqualTo(soc));
            p.ResetMotorState(); Assert.That(p.Power.Soc,Is.EqualTo(1)); Assert.That(p.Power.ConsumedAh,Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator PilotHoldsHeightWithBatteryAndMotorLag()
        {
            var p=Create(pilot:true); rig.Pilot.SetTestInput(0,0,0,0); rig.Step(1000);
            Assert.That(Mathf.Abs(p.Body.position.y-100),Is.LessThan(.025f)); Assert.That(p.Power.Soc,Is.LessThan(1));
            Assert.That(p.Power.Limited,Is.False); yield break;
        }
    }
}
