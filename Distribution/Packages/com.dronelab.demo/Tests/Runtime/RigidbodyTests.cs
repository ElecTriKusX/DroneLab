using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class RigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private DronePhysicsBody Create(bool instantaneous=false,string environment="environment_calm")
        {
            rig=new IsolatedPhysicsRig(instantaneous:instantaneous,environment:environment); return rig.Physics;
        }
        [UnityTest] public IEnumerator HoverHasNoTranslationOrRotation()
        {
            var p=Create(true); p.SetArmed(true);
            for(int i=0;i<4;i++) p.SetMotorCommand(i,0.5);
            rig.Step(100);
            Assert.That(p.Body.linearVelocity.magnitude,Is.LessThan(0.02f));
            Assert.That(p.Body.angularVelocity.magnitude,Is.LessThan(0.02f)); yield break;
        }
        [UnityTest] public IEnumerator DisarmedDroneFalls()
        {
            var p=Create(true);
            rig.Step(20);
            Assert.That(p.Body.linearVelocity.y,Is.LessThan(-1.5f)); yield break;
        }
        [UnityTest] public IEnumerator WindAcceleratesDroneDownwind()
        {
            var p=Create(true,"environment_wind"); p.SetArmed(true);
            for(int i=0;i<4;i++) p.SetMotorCommand(i,0.5);
            rig.Step(100);
            Assert.That(p.Body.linearVelocity.x,Is.GreaterThan(0.3f)); yield break;
        }
        [UnityTest] public IEnumerator ReleasedCommandSpinsDownWithMotorLag()
        {
            var p=Create(); p.SetArmed(true);
            for(int i=0;i<4;i++) p.SetMotorCommand(i,0.5);
            rig.Step(50);
            Assert.That(p.Omega[0],Is.GreaterThan(500));
            for(int i=0;i<4;i++) p.SetMotorCommand(i,0);
            rig.Step(80);
            Assert.That(p.Omega[0],Is.LessThan(0.05));
            Assert.That(p.ThrustN[0],Is.LessThan(1e-6)); yield break;
        }
    }
}
