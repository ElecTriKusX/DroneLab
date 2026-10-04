using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class PilotResponseTests
    {
        private GameObject go;
        private DronePhysicsBody physics;
        private DroneTestPilot pilot;
        private float oldStep;
        [SetUp] public void SetUp()
        {
            oldStep=Time.fixedDeltaTime; Time.fixedDeltaTime=0.01f;
            go=new GameObject("DroneLab controller response test"); go.SetActive(false);
            go.transform.position=new Vector3(0,100,0);
            go.AddComponent<BoxCollider>().size=new Vector3(0.4f,0.1f,0.4f);
            go.AddComponent<Rigidbody>(); physics=go.AddComponent<DronePhysicsBody>();
            pilot=go.AddComponent<DroneTestPilot>(); pilot.readKeyboard=false; pilot.showTelemetry=false; pilot.altitudeHold=true;
            go.SetActive(true);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(go); Time.fixedDeltaTime=oldStep; }
        private IEnumerator StartHover()
        {
            yield return null; // Start initializes the pilot after the body's Awake.
            Assert.That(physics.IsReady,Is.True);
            physics.SetArmed(true); pilot.SetTestInput(0,0,0,0);
            for(int i=0;i<150;i++) yield return new WaitForFixedUpdate();
        }
        private double Spread()
        {
            double min=double.MaxValue,max=0;
            foreach(double omega in physics.Omega) { min=System.Math.Min(min,omega); max=System.Math.Max(max,omega); }
            return PhysicsMath.OmegaToRpm(max-min);
        }
        [UnityTest] public IEnumerator HeldYawTracksRateAndReleaseBrakes()
        {
            yield return StartHover(); pilot.SetTestInput(0,0,1,0);
            for(int i=0;i<15;i++) yield return new WaitForFixedUpdate();
            Assert.That(Spread(),Is.GreaterThan(5));
            for(int i=0;i<285;i++) yield return new WaitForFixedUpdate();
            var rate=go.transform.InverseTransformDirection(physics.Body.angularVelocity)*Mathf.Rad2Deg;
            Assert.That(rate.y,Is.EqualTo(70).Within(1)); Assert.That(Spread(),Is.LessThan(5));
            pilot.SetTestInput(0,0,0,0);
            for(int i=0;i<200;i++) yield return new WaitForFixedUpdate();
            Assert.That(physics.Body.angularVelocity.magnitude,Is.LessThan(0.03f));
        }
        [UnityTest] public IEnumerator HeldRightTiltAcceleratesRightAndLevelsOnRelease()
        {
            yield return StartHover(); pilot.SetTestInput(1,0,0,0);
            for(int i=0;i<200;i++) yield return new WaitForFixedUpdate();
            float tilt=Vector3.Angle(go.transform.up,Vector3.up);
            Assert.That(tilt,Is.EqualTo(20).Within(2));
            Assert.That(physics.Body.linearVelocity.x,Is.GreaterThan(3));
            Assert.That(Mathf.Abs(physics.Body.position.y-100),Is.LessThan(1));
            pilot.SetTestInput(0,0,0,0);
            for(int i=0;i<200;i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Angle(go.transform.up,Vector3.up),Is.LessThan(2));
            // Leveling is not a position or horizontal-speed hold: momentum remains and drag slows it.
            Assert.That(physics.Body.linearVelocity.x,Is.GreaterThan(0));
        }
        [UnityTest] public IEnumerator VisualChildScaleDoesNotChangeHoverPhysics()
        {
            yield return StartHover();
            var visual=new GameObject("Visual only"); visual.transform.SetParent(go.transform,false);
            visual.transform.localScale=Vector3.one*10;
            for(int i=0;i<100;i++) yield return new WaitForFixedUpdate();
            Assert.That(physics.Body.mass,Is.EqualTo(1));
            Assert.That(physics.Body.linearVelocity.magnitude,Is.LessThan(0.1f));
            Assert.That(Mathf.Abs(physics.Body.position.y-100),Is.LessThan(0.2f));
        }
        [UnityTest] public IEnumerator LeavingAltitudeHoldWithZeroThrottleSpinsDownEveryMotor()
        {
            yield return StartHover(); pilot.altitudeHold=false; pilot.SetTestInput(0,0,0,0);
            for(int i=0;i<150;i++) yield return new WaitForFixedUpdate();
            foreach(double omega in physics.Omega) Assert.That(omega,Is.LessThan(0.001));
            Assert.That(physics.Body.linearVelocity.y,Is.LessThan(-5));
        }
        [UnityTest] public IEnumerator AcroStopsRotationWithoutLevelingThenAngleLevels()
        {
            yield return StartHover(); pilot.autoLevel=false; pilot.SetTestInput(0.25f,0,0,0);
            for(int i=0;i<60;i++) yield return new WaitForFixedUpdate();
            pilot.SetTestInput(0,0,0,0);
            for(int i=0;i<200;i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Angle(go.transform.up,Vector3.up),Is.GreaterThan(5));
            Assert.That(physics.Body.angularVelocity.magnitude,Is.LessThan(0.05f));
            pilot.autoLevel=true;
            for(int i=0;i<400;i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Angle(go.transform.up,Vector3.up),Is.LessThan(2));
        }
        [UnityTest] public IEnumerator EnteringAltitudeHoldBlendsCollectiveAndDoesNotKickYaw()
        {
            yield return StartHover();
            pilot.altitudeHold=false; pilot.SetTestInput(0,0,0,1);
            for(int i=0;i<60;i++) yield return new WaitForFixedUpdate();
            double before=0; foreach(double t in physics.ThrustN) before+=t;
            pilot.altitudeHold=true; pilot.SetTestInput(0,0,0,0);
            yield return new WaitForFixedUpdate();
            double after=0; foreach(double t in physics.ThrustN) after+=t;
            Assert.That(System.Math.Abs(after-before),Is.LessThan(0.5));
            Assert.That(pilot.RequestedTorqueLocal.magnitude,Is.LessThan(0.01f));
            for(int i=0;i<600;i++) yield return new WaitForFixedUpdate();
            Assert.That(Mathf.Abs(physics.Body.linearVelocity.y),Is.LessThan(0.2f));
        }
    }
}
