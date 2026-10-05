using System;
using System.Collections;
using System.Text.RegularExpressions;
using DroneLab.Simulation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class EnvironmentRigidbodyTests
    {
        private IsolatedPhysicsRig rig,other;
        [UnityTearDown] public IEnumerator TearDown()
        { if(rig!=null) yield return rig.Dispose(); if(other!=null) yield return other.Dispose(); rig=other=null; }
        private DronePhysicsBody Create(string env="environment_gust",string drone="quad_test_rotor_drag",IWindProvider field=null,Action<JObject> edit=null)
        {
            var j=JObject.Parse(Resources.Load<TextAsset>("DronePhysics/"+drone).text); edit?.Invoke(j);
            rig=new IsolatedPhysicsRig(instantaneous:true,environment:env,json:j.ToString(),customWind:field);
            rig.Physics.SetArmed(true); for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5); return rig.Physics;
        }
        private static LinearWindField Field(DVector3 gradientY=default,DVector3 gradientZ=default)
            =>new LinearWindField(new DVector3(5,0,0),new DVector3(0,100,0),default,gradientY,gradientZ,100);
        [UnityTest] public IEnumerator GustActuallyChangesWindAndBodyDrag()
        {
            var p=Create(); rig.Step(); double before=p.DragForce.x; Assert.That(p.WindVelocityWorld.x,Is.EqualTo(5).Within(1e-6));
            // Keep pose/velocity fixed while the simulation clock reaches the gust peak.
            for(int i=0;i<100;i++) { p.Body.position=new Vector3(0,100,0); p.Body.linearVelocity=Vector3.zero; p.Body.angularVelocity=Vector3.zero; rig.Step(); }
            Assert.That(p.WindVelocityWorld.x,Is.EqualTo(7).Within(1e-5)); Assert.That(p.DragForce.x,Is.GreaterThan(before*1.9)); yield break;
        }
        [UnityTest] public IEnumerator SeededWindReplayMatchesAcrossIndependentScenes()
        {
            var p=Create("environment_turbulence");
            other=new IsolatedPhysicsRig(instantaneous:true,environment:"environment_turbulence",json:Resources.Load<TextAsset>("DronePhysics/quad_test_rotor_drag").text);
            other.Physics.SetArmed(true); for(int i=0;i<4;i++) other.Physics.SetMotorCommand(i,.5);
            rig.Step(100); other.Step(100);
            Assert.That((p.Body.position-other.Physics.Body.position).magnitude,Is.LessThan(1e-5));
            Assert.That((p.WindVelocityWorld-other.Physics.WindVelocityWorld).magnitude,Is.LessThan(1e-5)); yield break;
        }
        [UnityTest] public IEnumerator CustomFieldSamplesEachRotorAndCreatesItsLeverArmTorque()
        {
            var p=Create("environment_field",field:Field(gradientZ:new DVector3(1,0,0))); rig.Step();
            Assert.That(p.RotorWindVelocityWorld[0].x,Is.EqualTo(5.14).Within(1e-5));
            Assert.That(p.RotorWindVelocityWorld[2].x,Is.EqualTo(4.86).Within(1e-5));
            double expected=.0001*PhysicsMath.RpmToOmega(5000)*4*.14*.14;
            Assert.That(p.RotorDragTorque.y,Is.EqualTo(expected).Within(1e-6));
            Assert.That(p.Body.angularVelocity.y,Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator CustomFieldBodyCpUsesWindAtCpRatherThanCom()
        {
            var p=Create("environment_field",field:Field(gradientY:new DVector3(4,0,0)),edit:j=>j["bodyAerodynamics"]["dragApplicationPointLocalM"]=new JArray(0,.25,0)); rig.Step();
            double force=.5*1.225*1.1*.04*6*6;
            Assert.That(p.WindVelocityWorld.x,Is.EqualTo(5).Within(1e-6)); Assert.That(p.DragForce.x,Is.EqualTo(force).Within(1e-6));
            Assert.That(p.DragTorque.z,Is.EqualTo(-.25*force).Within(1e-6)); yield break;
        }
        [UnityTest] public IEnumerator SurfacePatchesUseSeparateSpatialFlows()
        {
            var p=Create("environment_field","quad_test_surfaces",Field(gradientZ:new DVector3(5,0,0)),j=> {
                j["bodyAerodynamics"]["surfaces"]=new JArray(
                    new JObject { ["surfaceId"]="front",["positionLocalM"]=new JArray(0,0,.2),["normalLocal"]=new JArray(1,0,0),["areaM2"]=.02,["dragCoefficient"]=1 },
                    new JObject { ["surfaceId"]="rear",["positionLocalM"]=new JArray(0,0,-.2),["normalLocal"]=new JArray(1,0,0),["areaM2"]=.02,["dragCoefficient"]=1 });
            }); rig.Step();
            double front=.5*1.225*.02*36,rear=.5*1.225*.02*16;
            Assert.That(p.DragForce.x,Is.EqualTo(front+rear).Within(1e-6)); Assert.That(p.DragTorque.y,Is.EqualTo(.2*(front-rear)).Within(1e-6)); yield break;
        }
        [UnityTest] public IEnumerator AtmosphereTracksHeightAndChangesThrustAndBodyDrag()
        {
            var p=Create("environment_atmosphere","quad_test_ctcq"); double initial=p.Air.Density;
            p.Body.position=new Vector3(0,3100,0); p.Body.linearVelocity=Vector3.right*5; UnityEngine.Physics.SyncTransforms(); rig.Step();
            double density=Atmosphere.Troposphere(3000).Density;
            Assert.That(p.Air.AltitudeM,Is.EqualTo(3000).Within(1e-3)); Assert.That(p.Air.Density,Is.EqualTo(density).Within(1e-6));
            Assert.That(p.ThrustN[0],Is.EqualTo(p.Parameters.Rotors[0].Performance.Evaluate(p.Omega[0]).Thrust*density/initial).Within(1e-6));
            Assert.That(p.DragForce.x,Is.EqualTo(-.5*density*1.1*.04*25).Within(1e-6)); yield break;
        }
        [UnityTest] public IEnumerator AtmosphereRotorTorqueAndBatteryPowerUseSameDensity()
        {
            var p=Create("environment_atmosphere","quad_test_battery_simple",edit:j=> {
                var ct=JObject.Parse(Resources.Load<TextAsset>("DronePhysics/quad_test_ctcq").text);
                for(int i=0;i<4;i++) j["rotors"][i]["performance"]=ct["rotors"][i]["performance"].DeepClone();
            }); p.Body.position=new Vector3(0,3100,0); UnityEngine.Physics.SyncTransforms(); rig.Step();
            double shaft=0; for(int i=0;i<4;i++) shaft+=Math.Abs(p.ReactionTorqueNm[i])*p.Omega[i];
            Assert.That(p.Power.MechanicalPower,Is.EqualTo(shaft).Within(1e-8)); Assert.That(p.Power.Current,Is.GreaterThan(0)); yield break;
        }
        [UnityTest] public IEnumerator WindInteractionDisabledIgnoresCustomFieldAndNeedsNoProvider()
        {
            var p=Create("environment_field",edit:j=>j["physicsConfiguration"]["modules"]["windInteraction"]=false); rig.Step();
            Assert.That(p.WindVelocityWorld.magnitude+p.DragForce.magnitude+p.RotorDragForce.magnitude,Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator ResetRestartsSeededWindClock()
        {
            var p=Create("environment_turbulence"); rig.Step(); var first=p.WindVelocityWorld; rig.Step(50);
            p.Body.position=new Vector3(0,100,0); p.Body.rotation=Quaternion.identity; p.Body.linearVelocity=p.Body.angularVelocity=Vector3.zero;
            p.ResetMotorState(); UnityEngine.Physics.SyncTransforms(); Assert.That(p.SimulationTimeS,Is.Zero); rig.Step();
            Assert.That((p.WindVelocityWorld-first).magnitude,Is.LessThan(1e-5)); yield break;
        }
        private sealed class InvalidCpWind : IWindProvider
        { public DVector3 Sample(DVector3 point,double time)=>new DVector3(point.Y>100.1 ? double.NaN : 5,0,0); }
        [UnityTest] public IEnumerator InvalidWindAbortsAllForcesAndDoesNotSpendBatteryCharge()
        {
            var p=Create("environment_field","quad_test_battery_simple",new InvalidCpWind(),j=>j["bodyAerodynamics"]["dragApplicationPointLocalM"]=new JArray(0,.25,0));
            LogAssert.Expect(LogType.Error,new Regex("Physics/power query rejected simulation step")); rig.Step();
            Assert.That(p.IsReady,Is.False); Assert.That(p.Body.linearVelocity.magnitude,Is.Zero); Assert.That(p.Power.ConsumedAh,Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator CustomFieldWithoutProviderIsExplicitlyRejected()
        {
            var p=Create("environment_calm");
            LogAssert.Expect(LogType.Error,new Regex("CustomField requires a component implementing IWindProvider"));
            bool result=p.Initialize(p.droneProfile,Resources.Load<TextAsset>("DronePhysics/environment_field"));
            Assert.That(result,Is.False); Assert.That(p.IsReady,Is.False); yield break;
        }
        [UnityTest] public IEnumerator PilotAltitudeHoldCompensatesForChangedDensity()
        {
            rig=new IsolatedPhysicsRig(pilot:true,environment:"environment_atmosphere",json:Resources.Load<TextAsset>("DronePhysics/quad_test_ctcq").text);
            var p=rig.Physics;
            // Preserve the initial atmosphere anchor, but initialize the controller at a thinner-air start.
            p.Body.position=new Vector3(0,3100,0); p.ResetMotorState(); UnityEngine.Physics.SyncTransforms();
            p.SetArmed(true); rig.Pilot.SetTestInput(0,0,0,0); rig.Step(2000);
            Assert.That(Mathf.Abs(p.Body.position.y-3100),Is.LessThan(.03f)); Assert.That(p.Air.Density,Is.LessThan(p.Parameters.Density));
            Assert.That(p.Omega[0],Is.GreaterThan(PhysicsMath.RpmToOmega(5000))); yield break;
        }
    }
}
