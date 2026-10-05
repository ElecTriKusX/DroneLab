using System;
using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneLab.Physics.Tests
{
    // Local physics scene: no Terrain, frame scheduling, focus, or global timestep changes.
    internal sealed class IsolatedPhysicsRig
    {
        public const float Dt=0.01f;
        public readonly GameObject Go;
        public readonly DronePhysicsBody Physics;
        public readonly DroneTestPilot Pilot;
        private readonly Scene scene;
        private readonly PhysicsScene physicsScene;
        private readonly TextAsset profile;
        public IsolatedPhysicsRig(bool pilot=false,bool instantaneous=false,string environment="environment_calm",string json=null)
        {
            scene=SceneManager.CreateScene("DroneLab test "+Guid.NewGuid(),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            physicsScene=scene.GetPhysicsScene();
            Go=new GameObject("DroneLab isolated test"); Go.SetActive(false);
            SceneManager.MoveGameObjectToScene(Go,scene); Go.transform.position=new Vector3(0,100,0);
            Go.AddComponent<BoxCollider>().size=new Vector3(0.4f,0.1f,0.4f); Go.AddComponent<Rigidbody>();
            Physics=Go.AddComponent<DronePhysicsBody>(); Physics.AutomaticSimulation=false;
            json=json ?? Resources.Load<TextAsset>("DronePhysics/quad_test_basic").text;
            if(instantaneous) json=json.Replace("\"motorResponse\": true","\"motorResponse\": false");
            profile=new TextAsset(json); Physics.droneProfile=profile;
            Physics.environmentProfile=Resources.Load<TextAsset>("DronePhysics/"+environment);
            if(pilot)
            {
                Pilot=Go.AddComponent<DroneTestPilot>(); Pilot.readKeyboard=false; Pilot.showTelemetry=false;
                Pilot.altitudeHold=true; Pilot.AutomaticControl=false; Pilot.DisarmOnFocusLoss=false;
            }
            Go.SetActive(true); Assert.That(Physics.IsReady,Is.True);
            Physics.Body.interpolation=RigidbodyInterpolation.None; Pilot?.InitializeController();
        }
        public void Step(int count=1)
        {
            for(int i=0;i<count;i++)
            { Pilot?.StepControl(Dt); Physics.StepPhysics(Dt); physicsScene.Simulate(Dt); }
        }
        public IEnumerator Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Go); UnityEngine.Object.DestroyImmediate(profile);
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
