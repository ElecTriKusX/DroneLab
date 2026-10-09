using System;
using System.Collections;
using DroneLab.Simulation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class ThermalRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private DronePhysicsBody Create(Action<JObject> edit=null,bool pilot=false,string environment="environment_final_acceptance")
        {
            var json=JObject.Parse(Resources.Load<TextAsset>("DronePhysics/quad_test_thermal").text); edit?.Invoke(json);
            rig=new IsolatedPhysicsRig(pilot:pilot,environment:environment,json:json.ToString());
            rig.Physics.SetArmed(true); if(!pilot) for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5);
            return rig.Physics;
        }
        [UnityTest] public IEnumerator ElectricalLossBecomesThermalEnergyOncePerPhysicsStep()
        {
            var p=Create(); rig.Step(); var b=p.Power;
            double expected=(b.MotorLossPower+b.EscLossPower+b.BatteryLossPower)*IsolatedPhysicsRig.Dt;
            Assert.That(b.Thermal.GeneratedEnergyJ,Is.EqualTo(expected).Within(1e-7));
            Assert.That(b.Thermal.GeneratedEnergyJ-b.Thermal.RejectedEnergyJ,Is.EqualTo(b.Thermal.StoredEnergyJ).Within(1e-7));
            var f=DroneTelemetryRecorder.Capture(p,IsolatedPhysicsRig.Dt);
            Assert.That(f.BatteryTemperature,Is.EqualTo(b.Thermal.Battery.TemperatureK));
            Assert.That(f.Rotors[0].MotorTemperature,Is.EqualTo(b.Thermal.Motor(0).TemperatureK)); yield break;
        }
        [UnityTest] public IEnumerator MotorCutoffCoastsAndOtherMotorsKeepElectricalDrive()
        {
            var p=Create(j=>j["rotors"][0]["motor"]["electrical"]["thermal"]["initialTemperatureK"]=373.15);
            for(int i=0;i<4;i++) p.Omega[i]=500; rig.Step();
            Assert.That(p.Omega[0],Is.InRange(1.0,499.99)); Assert.That(p.Power.RotorCurrentA[0],Is.Zero);
            Assert.That(p.Power.RotorCurrentA[1],Is.GreaterThan(0)); Assert.That(p.ReactionTorqueNm[0],Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator BatteryCutoffCoolsAndDoesNotEraseRotorSpin()
        {
            var p=Create(j=>j["powerSystem"]["battery"]["thermal"]["initialTemperatureK"]=333.15);
            for(int i=0;i<4;i++) p.Omega[i]=500; rig.Step();
            Assert.That(p.Power.Current,Is.Zero); Assert.That(p.Omega[0],Is.InRange(1.0,499.99));
            Assert.That(p.Power.Thermal.Battery.TemperatureK,Is.LessThan(333.15)); yield break;
        }
        [UnityTest] public IEnumerator DisarmedHotComponentsContinueCoolingAndResetClearsHistory()
        {
            var p=Create(j=> { foreach(var r in j["rotors"]) r["motor"]["electrical"]["thermal"]["initialTemperatureK"]=330; });
            p.SetArmed(false); rig.Step(100);
            Assert.That(p.Power.Current,Is.Zero); Assert.That(p.Power.Thermal.GeneratedEnergyJ,Is.Zero);
            Assert.That(p.Power.Thermal.Motor(0).TemperatureK,Is.LessThan(330)); p.ResetMotorState();
            Assert.That(p.Power.Thermal.Motor(0).TemperatureK,Is.EqualTo(330)); Assert.That(p.Power.Thermal.StoredEnergyJ,Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator PilotWindAtmosphereBatteryAndThermalHoverRemainStable()
        {
            var p=Create(pilot:true); rig.Pilot.SetTestInput(0,0,0,0); rig.Step(1000);
            Assert.That(p.IsReady,Is.True); Assert.That(p.Body.position.y,Is.InRange(99.5f,100.5f));
            Assert.That(p.Power.Current,Is.GreaterThan(0)); Assert.That(p.Power.Thermal.GeneratedEnergyJ,Is.GreaterThan(0));
            Assert.That(p.Power.Thermal.Motor(0).TemperatureK,Is.LessThan(343.15)); yield break;
        }
        [UnityTest] public IEnumerator WeatherProfileExposesMetadataAndLocalAmbientToRecorder()
        {
            var p=Create(environment:"environment_thermal_rain"); rig.Step();
            var f=DroneTelemetryRecorder.Capture(p,IsolatedPhysicsRig.Dt);
            Assert.That(f.Precipitation,Is.EqualTo("Rain")); Assert.That(f.PrecipitationIntensity,Is.EqualTo(10));
            Assert.That(f.AmbientTemperature,Is.EqualTo(p.Air.TemperatureK)); yield break;
        }
    }
}
