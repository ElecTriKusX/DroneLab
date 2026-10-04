using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class RigidbodyTests
    {
        private GameObject go;
        private float oldStep;
        [SetUp] public void SetUp()
        {
            oldStep=Time.fixedDeltaTime; Time.fixedDeltaTime=0.01f;
            go=new GameObject("DroneLab integration test"); go.transform.position=new Vector3(0,100,0);
            go.AddComponent<BoxCollider>(); go.AddComponent<Rigidbody>();
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(go); Time.fixedDeltaTime=oldStep; }
        private DronePhysicsBody Create(bool instantaneous=false,string environment="environment_calm")
        {
            // Keep inactive until profile references are assigned so Awake uses the intended profile.
            go.SetActive(false);
            var body=go.AddComponent<DronePhysicsBody>();
            string json=Resources.Load<TextAsset>("DronePhysics/quad_test_basic").text;
            if(instantaneous) json=json.Replace("\"motorResponse\": true","\"motorResponse\": false");
            body.droneProfile=new TextAsset(json); body.environmentProfile=Resources.Load<TextAsset>("DronePhysics/"+environment);
            go.SetActive(true);
            Assert.That(body.IsReady,Is.True); return body;
        }
        [UnityTest] public IEnumerator HoverHasNoTranslationOrRotation()
        {
            var p=Create(true); p.SetArmed(true);
            for(int i=0;i<4;i++) p.SetMotorCommand(i,0.5);
            for(int i=0;i<100;i++) yield return new WaitForFixedUpdate();
            Assert.That(p.Body.linearVelocity.magnitude,Is.LessThan(0.02f));
            Assert.That(p.Body.angularVelocity.magnitude,Is.LessThan(0.02f));
        }
        [UnityTest] public IEnumerator DisarmedDroneFalls()
        {
            var p=Create(true);
            for(int i=0;i<20;i++) yield return new WaitForFixedUpdate();
            Assert.That(p.Body.linearVelocity.y,Is.LessThan(-1.5f));
        }
        [UnityTest] public IEnumerator WindAcceleratesDroneDownwind()
        {
            var p=Create(true,"environment_wind"); p.SetArmed(true);
            for(int i=0;i<4;i++) p.SetMotorCommand(i,0.5);
            for(int i=0;i<100;i++) yield return new WaitForFixedUpdate();
            Assert.That(p.Body.linearVelocity.x,Is.GreaterThan(0.3f));
        }
        [UnityTest] public IEnumerator ReleasedCommandSpinsDownWithMotorLag()
        {
            var p=Create(); p.SetArmed(true);
            for(int i=0;i<4;i++) p.SetMotorCommand(i,0.5);
            for(int i=0;i<50;i++) yield return new WaitForFixedUpdate();
            Assert.That(p.Omega[0],Is.GreaterThan(500));
            for(int i=0;i<4;i++) p.SetMotorCommand(i,0);
            for(int i=0;i<80;i++) yield return new WaitForFixedUpdate();
            Assert.That(p.Omega[0],Is.LessThan(0.05));
            Assert.That(p.ThrustN[0],Is.LessThan(1e-6));
        }
    }
}
