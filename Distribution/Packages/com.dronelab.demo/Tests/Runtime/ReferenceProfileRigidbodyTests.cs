using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class ReferenceProfileRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private void Check(string name)
        {
            rig=new IsolatedPhysicsRig(pilot:true,json:Resources.Load<TextAsset>("DronePhysics/"+name).text);
            var p=rig.Physics; rig.Pilot.SetTestInput(0,0,0,0); p.SetArmed(true); rig.Step(1000);
            Assert.That(p.IsReady,Is.True); Assert.That(p.Body.position.y,Is.InRange(99.5f,100.5f));
            Assert.That(p.Parameters.Mass,Is.LessThan(1)); Assert.That(p.Power,Is.Null);
            Assert.That(p.Body.mass,Is.EqualTo(p.Parameters.Mass).Within(1e-6));
        }
        [UnityTest] public IEnumerator Crazyflie20ReferenceHoldsHeightWithMeasuredThrust() { Check("reference_crazyflie20"); yield break; }
        [UnityTest] public IEnumerator BrushlessReferenceHoldsHeightWithPublishedCurve() { Check("reference_crazyflie_brushless"); yield break; }
        [UnityTest] public IEnumerator HummingbirdReferenceHoldsHeightWithPublishedParameters() { Check("reference_hummingbird"); yield break; }
    }
}
