using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class RotorEffectsRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        private TerrainData terrainData;
        [UnityTearDown] public IEnumerator TearDown()
        {
            if(rig!=null) yield return rig.Dispose(); rig=null;
            if(terrainData!=null) Object.DestroyImmediate(terrainData); terrainData=null;
        }
        private void Create(string name="quad_test_rotor_effects",bool pilot=false,string environment="environment_calm")
        {
            rig=new IsolatedPhysicsRig(pilot:pilot,instantaneous:!pilot,environment:environment,json:Resources.Load<TextAsset>("DronePhysics/"+name).text);
            if(!pilot) { rig.Physics.SetArmed(true); for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5); }
        }
        private GameObject Box(string name,Vector3 position,Vector3 size,bool trigger=false)
        {
            var go=new GameObject(name); SceneManager.MoveGameObjectToScene(go,rig.Go.scene);
            go.transform.position=position; var collider=go.AddComponent<BoxCollider>(); collider.size=size; collider.isTrigger=trigger;
            return go;
        }
        private void Ground(float height=.1f)
        { Box("Ground",new Vector3(0,100-height-.05f,0),new Vector3(5,.1f,5)); }
        private void Step(int count=1) { UnityEngine.Physics.SyncTransforms(); rig.Step(count); }
        [UnityTest] public IEnumerator GroundGainBoostsThrustWithoutChangingReactionTorque()
        {
            Create("quad_test_ground_effect"); Ground(); Step(); var p=rig.Physics;
            double expected=1+System.Math.Pow(.0635/(4*.1),2);
            for(int i=0;i<4;i++)
            {
                Assert.That(p.GroundHeightM[i],Is.EqualTo(.1).Within(2e-5)); Assert.That(p.GroundEffectMultiplier[i],Is.EqualTo(expected).Within(1e-5));
                Assert.That(p.ThrustN[i],Is.EqualTo(2.4525*expected).Within(3e-5)); Assert.That(System.Math.Abs(p.ReactionTorqueNm[i]),Is.EqualTo(.04905).Within(1e-8));
            }
            Assert.That(p.Body.linearVelocity.y,Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator NoGroundHitAndStoppedMotorsDoNotCreateExtraLift()
        {
            Create(); Step(); foreach(double multiplier in rig.Physics.GroundEffectMultiplier) Assert.That(multiplier,Is.EqualTo(1));
            Ground(); rig.Physics.SetArmed(false); Step();
            foreach(double thrust in rig.Physics.ThrustN) Assert.That(thrust,Is.Zero);
            Assert.That(rig.Physics.Body.linearVelocity.y,Is.LessThan(0)); yield break;
        }
        [UnityTest] public IEnumerator GroundProbeIgnoresSelfTriggersAndSelectsNearestExternalSurface()
        {
            Create("quad_test_ground_effect"); Ground(.2f);
            Box("Nearest ground",new Vector3(0,99.85f,0),new Vector3(5,.1f,5));
            Box("Ignored trigger",new Vector3(0,99.98f,0),new Vector3(5,.01f,5),true);
            var self=Box("Drone child collider",new Vector3(0,99.925f,0),new Vector3(1,.01f,1)); self.transform.SetParent(rig.Go.transform,true);
            Step(); foreach(double height in rig.Physics.GroundHeightM) Assert.That(height,Is.EqualTo(.1).Within(2e-5)); yield break;
        }
        [UnityTest] public IEnumerator GroundProbeGrowsBufferRatherThanSelectingAnIncompleteHitSet()
        {
            Create("quad_test_ground_effect"); Ground();
            for(int i=0;i<40;i++)
            {
                var self=Box("Compound collider "+i,new Vector3(0,99.948f-i*.0005f,0),new Vector3(.5f,.0002f,.5f));
                self.transform.SetParent(rig.Go.transform,true);
            }
            Step(); foreach(double height in rig.Physics.GroundHeightM) Assert.That(height,Is.EqualTo(.1).Within(2e-5)); yield break;
        }
        [UnityTest] public IEnumerator GroundLayerMaskCanExcludeAndIncludeSurface()
        {
            Create("quad_test_ground_effect"); Ground(); rig.Physics.groundLayers=0; Step();
            foreach(double multiplier in rig.Physics.GroundEffectMultiplier) Assert.That(multiplier,Is.EqualTo(1));
            rig.Physics.groundLayers=UnityEngine.Physics.DefaultRaycastLayers; Step();
            foreach(double multiplier in rig.Physics.GroundEffectMultiplier) Assert.That(multiplier,Is.GreaterThan(1.02)); yield break;
        }
        [UnityTest] public IEnumerator SlopedSurfaceProducesDifferentRotorGainsAndOneLeverArmMoment()
        {
            Create("quad_test_ground_effect");
            var floor=Box("Sloped floor",new Vector3(0,99.75f,0),new Vector3(5,.1f,5)); floor.transform.rotation=Quaternion.Euler(0,0,10);
            Step();
            Assert.That(rig.Physics.GroundEffectMultiplier[1],Is.GreaterThan(rig.Physics.GroundEffectMultiplier[0]));
            Assert.That(rig.Physics.ThrustN[1],Is.GreaterThan(rig.Physics.ThrustN[0]));
            Assert.That(rig.Physics.Body.angularVelocity.z,Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator TerrainColliderIsDetectedInLocalPhysicsScene()
        {
            Create("quad_test_ground_effect"); terrainData=new TerrainData { heightmapResolution=33,size=new Vector3(4,1,4) };
            terrainData.SetHeights(0,0,new float[33,33]); var terrain=Terrain.CreateTerrainGameObject(terrainData);
            SceneManager.MoveGameObjectToScene(terrain,rig.Go.scene); terrain.transform.position=new Vector3(-2,99.9f,-2); terrain.GetComponent<Terrain>().Flush();
            Step(); foreach(double height in rig.Physics.GroundHeightM) Assert.That(height,Is.EqualTo(.1).Within(2e-5)); yield break;
        }
        [UnityTest] public IEnumerator WindCreatesSeparateBodyAndRotorDragForces()
        {
            Create("quad_test_rotor_drag",environment:"environment_wind"); Step(); var p=rig.Physics;
            double expected=4*.0001*PhysicsMath.RpmToOmega(5000)*5;
            Assert.That(p.RotorDragForce.x,Is.EqualTo(expected).Within(1e-6)); Assert.That(p.DragForce.x,Is.EqualTo(.6125).Within(1e-6));
            Assert.That(p.Body.linearVelocity.x,Is.GreaterThan(0)); Assert.That(p.RotorDragTorque.magnitude,Is.LessThan(1e-6)); yield break;
        }
        [UnityTest] public IEnumerator YawRotorDragBrakesRotationWithoutAddingTranslation()
        {
            Create("quad_test_rotor_drag"); rig.Physics.Body.angularVelocity=Vector3.up*2; Step();
            double expected=-.0001*PhysicsMath.RpmToOmega(5000)*2*4*(.14*.14+.14*.14);
            Assert.That(rig.Physics.RotorDragTorque.y,Is.EqualTo(expected).Within(1e-6));
            Assert.That(rig.Physics.RotorDragForce.magnitude,Is.LessThan(1e-6));
            Assert.That(rig.Physics.Body.angularVelocity.y,Is.InRange(1.9f,2f)); yield break;
        }
        [UnityTest] public IEnumerator PilotTrimsGroundEffectWhileHoldingHeight()
        {
            Create("quad_test_ground_effect",pilot:true); Ground(); rig.Physics.SetArmed(true); rig.Pilot.SetTestInput(0,0,0,0); Step(1000);
            Assert.That(Mathf.Abs(rig.Physics.Body.position.y-100),Is.LessThan(.025f));
            Assert.That(Mathf.Abs(rig.Physics.Body.linearVelocity.y),Is.LessThan(.025f));
            foreach(double omega in rig.Physics.Omega) Assert.That(PhysicsMath.OmegaToRpm(omega),Is.LessThan(4990)); yield break;
        }
    }
}
