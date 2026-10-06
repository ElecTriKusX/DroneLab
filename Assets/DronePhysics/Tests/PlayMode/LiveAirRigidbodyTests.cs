using System.Collections;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class LiveAirRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        private sealed class Provider : IAirProvider, IWeatherProvider
        {
            public bool Ready=true;
            public AtmosphereColumn Column=new AtmosphereColumn(293.15,101325,0,100);
            public bool TrySampleAir(DVector3 position,double time,out AirSample air)
            { if(Ready) return Column.TrySampleAir(position,time,out air); air=default; return false; }
            public bool TrySampleWeather(out WeatherSample weather)
            { weather=new WeatherSample("Rain",10); return Ready; }
        }
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        [UnityTest] public IEnumerator AirUpdateChangesForcesAndTelemetryWithoutResettingState()
        {
            var provider=new Provider();
            rig=new IsolatedPhysicsRig(environment:"environment_thermal_rain",
                json:Resources.Load<TextAsset>("DronePhysics/quad_test_thermal").text,customAir:provider);
            var p=rig.Physics; p.SetArmed(true); for(int i=0;i<4;i++) p.SetMotorCommand(i,.5);
            rig.Step(10);
            var parameters=p.Parameters; var power=p.Power; var drive=p.Drive; double time=p.SimulationTimeS,charge=power.ConsumedAh,energy=power.Thermal.GeneratedEnergyJ;
            provider.Column=new AtmosphereColumn(313.15,80000,0,100);
            p.Body.rotation=Quaternion.identity; p.Body.angularVelocity=Vector3.zero;
            p.Body.position=new Vector3(0,100,0); p.Body.linearVelocity=Vector3.right*5; UnityEngine.Physics.SyncTransforms(); rig.Step();
            Assert.That(p.Parameters,Is.SameAs(parameters)); Assert.That(p.Power,Is.SameAs(power)); Assert.That(p.Drive,Is.SameAs(drive));
            Assert.That(p.Armed,Is.True); Assert.That(p.SimulationTimeS,Is.GreaterThan(time)); Assert.That(power.ConsumedAh,Is.GreaterThan(charge));
            Assert.That(power.Thermal.GeneratedEnergyJ,Is.GreaterThan(energy)); Assert.That(p.UsesLiveAir,Is.True);
            Assert.That(p.Air.TemperatureK,Is.EqualTo(313.15).Within(1e-6)); Assert.That(p.Air.PressurePa,Is.EqualTo(80000).Within(1e-3));
            var drag=BodyAerodynamics.EvaluatePoint(parameters,DronePhysicsBody.FromUnity(p.AirVelocity),p.Air.Density).Force;
            Assert.That((DronePhysicsBody.FromUnity(p.DragForce)-drag).Length,Is.LessThan(1e-5));
            var frame=DroneTelemetryRecorder.Capture(p,IsolatedPhysicsRig.Dt);
            Assert.That(frame.Density,Is.EqualTo(p.Air.Density)); Assert.That(frame.AmbientTemperature,Is.EqualTo(p.Air.TemperatureK));
            Assert.That(frame.Precipitation,Is.EqualTo("Rain")); Assert.That(frame.PrecipitationIntensity,Is.EqualTo(10));
            yield break;
        }
        [UnityTest] public IEnumerator UntilFirstValidProviderStateAirFallsBackToJson()
        {
            var provider=new Provider { Ready=false };
            rig=new IsolatedPhysicsRig(environment:"environment_thermal_rain",
                json:Resources.Load<TextAsset>("DronePhysics/quad_test_thermal").text,customAir:provider);
            rig.Step(); var p=rig.Physics; Assert.That(p.UsesLiveAir,Is.False);
            Assert.That(p.Air.Density,Is.EqualTo(p.Parameters.Density));
            provider.Ready=true; rig.Step(); Assert.That(p.UsesLiveAir,Is.True); yield break;
        }
    }
}
