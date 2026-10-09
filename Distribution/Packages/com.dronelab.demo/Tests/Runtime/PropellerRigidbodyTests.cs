using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class PropellerRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private static string Profile(string name) => Resources.Load<TextAsset>("DronePhysics/"+name).text;
        private void Hover(string name)
        {
            rig=new IsolatedPhysicsRig(instantaneous:true,json:Profile(name)); rig.Physics.SetArmed(true);
            for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5);
            rig.Step(100);
            Assert.That(rig.Physics.Body.linearVelocity.magnitude,Is.LessThan(.02f));
            Assert.That(rig.Physics.Body.angularVelocity.magnitude,Is.LessThan(.02f));
        }
        [UnityTest] public IEnumerator TableHoverUsesMeasuredThrustAndCurrent()
        {
            Hover("quad_test_rpm_table"); Assert.That(rig.Physics.MeasuredCurrentA[0],Is.EqualTo(3).Within(1e-8)); yield break;
        }
        [UnityTest] public IEnumerator MapStaticHoverMatchesBasic() { Hover("quad_test_performance_map"); yield break; }
        [UnityTest] public IEnumerator MapUsesSignedRotorPointVelocityIncludingRotation()
        {
            rig=new IsolatedPhysicsRig(instantaneous:true,json:Profile("quad_test_performance_map")); rig.Physics.SetArmed(true);
            for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5);
            rig.Physics.Body.linearVelocity=Vector3.up*2; rig.Physics.Body.angularVelocity=Vector3.right;
            double frontJ=(2-.14)/(5000.0/60*.127),rearJ=(2+.14)/(5000.0/60*.127);
            rig.Step();
            Assert.That(rig.Physics.AdvanceRatio[0],Is.EqualTo(frontJ).Within(1e-6));
            Assert.That(rig.Physics.AdvanceRatio[2],Is.EqualTo(rearJ).Within(1e-6));
            Assert.That(rig.Physics.ThrustN[0],Is.GreaterThan(rig.Physics.ThrustN[2])); yield break;
        }
        [UnityTest] public IEnumerator MapStartupAndSpinDownRemainContinuousWithClamp()
        {
            rig=new IsolatedPhysicsRig(json:Profile("quad_test_performance_map")); rig.Physics.SetArmed(true);
            for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5);
            rig.Step(); Assert.That(rig.Physics.PerformanceClamped[0],Is.True);
            Assert.That(rig.Physics.ThrustN[0],Is.GreaterThan(0)); Assert.That(rig.Physics.ThrustN[0],Is.LessThan(.613125));
            rig.Step(49); for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,0);
            rig.Step(150); Assert.That(rig.Physics.ThrustN[0],Is.LessThan(1e-10)); yield break;
        }
        [UnityTest] public IEnumerator TablePilotHoldsHeightTracksYawAndStopsOnZeroThrottle()
        {
            rig=new IsolatedPhysicsRig(pilot:true,json:Profile("quad_test_rpm_table")); rig.Physics.SetArmed(true);
            rig.Pilot.SetTestInput(0,0,0,0); rig.Step(500);
            Assert.That(Mathf.Abs(rig.Physics.Body.position.y-100),Is.LessThan(.25f));
            rig.Pilot.SetTestInput(0,0,1,0); rig.Step(400);
            Assert.That(rig.Physics.Body.angularVelocity.y*Mathf.Rad2Deg,Is.EqualTo(70).Within(2));
            rig.Pilot.altitudeHold=false; rig.Pilot.SetTestInput(0,0,0,0); rig.Step(150);
            foreach(double omega in rig.Physics.Omega) Assert.That(omega,Is.LessThan(.001)); yield break;
        }
    }
}
