using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class PilotResponseTests
    {
        private IsolatedPhysicsRig rig;
        private GameObject go => rig.Go;
        private DronePhysicsBody physics => rig.Physics;
        private DroneTestPilot pilot => rig.Pilot;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private void StartHover()
        {
            rig=new IsolatedPhysicsRig(pilot:true);
            physics.SetArmed(true); pilot.SetTestInput(0,0,0,0); rig.Step(150);
            Assert.That(physics.Armed,Is.True,"The isolated scripted controller must stay armed.");
        }
        private double Spread()
        {
            double min=double.MaxValue,max=0;
            foreach(double omega in physics.Omega) { min=System.Math.Min(min,omega); max=System.Math.Max(max,omega); }
            return PhysicsMath.OmegaToRpm(max-min);
        }
        private sealed class NavigationFeedback : INavigationFeedback
        {
            public DronePhysicsBody Body; public bool Available=true;
            public bool TryHorizontal(out DVector3 position,out DVector3 velocity)
            { position=DronePhysicsBody.FromUnity(Body.Body.position); velocity=DronePhysicsBody.FromUnity(Body.Body.linearVelocity); return Available; }
        }
        [UnityTest] public IEnumerator PositionHoldReturnsToCapturedXZThroughMotorCommands()
        {
            StartHover(); var feedback=new NavigationFeedback { Body=physics }; pilot.NavigationFeedback=feedback;
            var captured=physics.Body.position; Assert.That(pilot.HoldPosition(),Is.True);
            physics.Body.position+=new Vector3(3,0,-2); rig.Step(3000);
            var delta=physics.Body.position-captured;
            Assert.That(new Vector2(delta.x,delta.z).magnitude,Is.LessThan(.4));
            Assert.That(Mathf.Abs(delta.y),Is.LessThan(.5)); yield break;
        }
        [UnityTest] public IEnumerator PositionHoldCancelsOnGpsLossAndManualTilt()
        {
            StartHover(); var feedback=new NavigationFeedback { Body=physics }; pilot.NavigationFeedback=feedback;
            Assert.That(pilot.HoldPosition(),Is.True); feedback.Available=false; rig.Step();
            Assert.That(pilot.PositionHold,Is.False); Assert.That(pilot.altitudeHold,Is.True); Assert.That(physics.Armed,Is.True);
            feedback.Available=true; rig.Step(); Assert.That(pilot.PositionHold,Is.False);
            pilot.HoldPosition(); pilot.SetTestInput(.2f,0,0,0); rig.Step(); Assert.That(pilot.PositionHold,Is.False); yield break;
        }
        [UnityTest] public IEnumerator HeldYawTracksRateAndReleaseBrakes()
        {
            StartHover(); pilot.SetTestInput(0,0,1,0);
            rig.Step(15);
            Assert.That(Spread(),Is.GreaterThan(5));
            rig.Step(285);
            var rate=go.transform.InverseTransformDirection(physics.Body.angularVelocity)*Mathf.Rad2Deg;
            Assert.That(rate.y,Is.EqualTo(70).Within(1)); Assert.That(Spread(),Is.LessThan(5));
            pilot.SetTestInput(0,0,0,0);
            rig.Step(200);
            Assert.That(physics.Body.angularVelocity.magnitude,Is.LessThan(0.03f)); yield break;
        }
        [UnityTest] public IEnumerator HeldRightTiltAcceleratesRightAndLevelsOnRelease()
        {
            StartHover(); pilot.SetTestInput(1,0,0,0);
            rig.Step(200);
            float tilt=Vector3.Angle(go.transform.up,Vector3.up);
            Assert.That(tilt,Is.EqualTo(20).Within(2));
            Assert.That(physics.Body.linearVelocity.x,Is.GreaterThan(3));
            Assert.That(Mathf.Abs(physics.Body.position.y-100),Is.LessThan(1));
            pilot.SetTestInput(0,0,0,0);
            rig.Step(200);
            Assert.That(Vector3.Angle(go.transform.up,Vector3.up),Is.LessThan(2));
            // Leveling is not a position or horizontal-speed hold: momentum remains and drag slows it.
            Assert.That(physics.Body.linearVelocity.x,Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator VisualChildScaleDoesNotChangeHoverPhysics()
        {
            StartHover();
            var visual=new GameObject("Visual only"); visual.transform.SetParent(go.transform,false);
            visual.transform.localScale=Vector3.one*10;
            rig.Step(100);
            Assert.That(physics.Body.mass,Is.EqualTo(1));
            Assert.That(physics.Body.linearVelocity.magnitude,Is.LessThan(0.1f));
            Assert.That(Mathf.Abs(physics.Body.position.y-100),Is.LessThan(0.2f)); yield break;
        }
        [UnityTest] public IEnumerator LeavingAltitudeHoldWithZeroThrottleSpinsDownEveryMotor()
        {
            StartHover(); pilot.altitudeHold=false; pilot.SetTestInput(0,0,0,0);
            rig.Step(150);
            foreach(double omega in physics.Omega) Assert.That(omega,Is.LessThan(0.001));
            Assert.That(physics.Body.linearVelocity.y,Is.LessThan(-5)); yield break;
        }
        [UnityTest] public IEnumerator AcroStopsRotationWithoutLevelingThenAngleLevels()
        {
            StartHover(); pilot.autoLevel=false; pilot.SetTestInput(0.25f,0,0,0);
            rig.Step(60);
            pilot.SetTestInput(0,0,0,0);
            rig.Step(200);
            Assert.That(Vector3.Angle(go.transform.up,Vector3.up),Is.GreaterThan(5));
            Assert.That(physics.Body.angularVelocity.magnitude,Is.LessThan(0.05f));
            pilot.autoLevel=true;
            rig.Step(400);
            Assert.That(Vector3.Angle(go.transform.up,Vector3.up),Is.LessThan(2)); yield break;
        }
        [UnityTest] public IEnumerator EnteringAltitudeHoldBlendsCollectiveAndDoesNotKickYaw()
        {
            StartHover();
            pilot.altitudeHold=false; pilot.SetTestInput(0,0,0,1);
            rig.Step(60);
            double before=0; foreach(double t in physics.ThrustN) before+=t;
            pilot.altitudeHold=true; pilot.SetTestInput(0,0,0,0);
            rig.Step();
            double after=0; foreach(double t in physics.ThrustN) after+=t;
            Assert.That(System.Math.Abs(after-before),Is.LessThan(0.5));
            Assert.That(pilot.RequestedTorqueLocal.magnitude,Is.LessThan(0.01f));
            rig.Step(600);
            Assert.That(Mathf.Abs(physics.Body.linearVelocity.y),Is.LessThan(0.2f)); yield break;
        }
    }
}
