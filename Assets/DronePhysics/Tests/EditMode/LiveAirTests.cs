using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class LiveAirTests
    {
        private static RuntimeDroneParameters Parameters(string name="quad_test_thermal")
        {
            var environment=JObject.Parse(ProfileTestFiles.Read("environment_calm"));
            environment["temperatureK"]=293.15; environment["pressurePa"]=101325;
            var r=ProfileLoader.Load(ProfileTestFiles.Read(name),environment.ToString(),
                ProfileTestFiles.Read("drone-profile.schema"),ProfileTestFiles.Read("environment-profile.schema"));
            Assert.That(r.Success,Is.True,string.Join("\n",r.Issues)); return r.Parameters;
        }
        [TestCase(0)] [TestCase(3000)] [TestCase(11000)]
        public void SeaLevelColumnMatchesExistingTroposphere(double h)
        {
            var column=new AtmosphereColumn(288.15,101325,0,100);
            Assert.That(column.TrySampleAir(new DVector3(0,100+h,0),0,out var a),Is.True);
            var reference=Atmosphere.Troposphere(h);
            Assert.That(a.TemperatureK,Is.EqualTo(reference.TemperatureK).Within(1e-10));
            Assert.That(a.PressurePa,Is.EqualTo(reference.PressurePa).Within(1e-8));
            Assert.That(a.Density,Is.EqualTo(reference.Density).Within(1e-10));
        }
        [Test] public void ReferenceMeasurementsAreLocalAndWorldOriginIsIndependentOfAltitude()
        {
            var column=new AtmosphereColumn(280,80000,2000,100);
            column.TrySampleAir(new DVector3(12,100,-40),1,out var at);
            Assert.That(at.AltitudeM,Is.EqualTo(2000)); Assert.That(at.TemperatureK,Is.EqualTo(280));
            Assert.That(at.PressurePa,Is.EqualTo(80000));
            column.TrySampleAir(new DVector3(0,1100,0),5,out var high);
            Assert.That(high.AltitudeM,Is.EqualTo(3000)); Assert.That(high.TemperatureK,Is.EqualTo(273.5));
            Assert.That(high.PressurePa,Is.LessThan(at.PressurePa)); Assert.That(high.Density,Is.LessThan(at.Density));
            Assert.That(high.Density*Atmosphere.GasConstant*high.TemperatureK,Is.EqualTo(high.PressurePa).Within(1e-8));
        }
        [Test] public void WarmerAirReducesThrustAndDragAtFixedPressureAndRpm()
        {
            var p=Parameters(); new AtmosphereColumn(273.15,101325).TrySampleAir(default,0,out var cold);
            new AtmosphereColumn(313.15,101325).TrySampleAir(default,0,out var warm);
            double ratio=cold.TemperatureK/warm.TemperatureK;
            Assert.That(warm.Density/cold.Density,Is.EqualTo(ratio).Within(1e-12));
            var rotor=p.Rotors[0]; double omega=500;
            Assert.That(rotor.Performance.Evaluate(omega,0,warm.Density).Thrust,
                Is.EqualTo(rotor.Performance.Evaluate(omega,0,cold.Density).Thrust*ratio).Within(1e-10));
            var v=new DVector3(5,0,0);
            Assert.That((BodyAerodynamics.EvaluatePoint(p,v,warm.Density).Force-
                BodyAerodynamics.EvaluatePoint(p,v,cold.Density).Force*ratio).Length,Is.LessThan(1e-10));
        }
        [Test] public void PressureScalesDensityWithoutChangingTemperature()
        {
            new AtmosphereColumn(293.15,100000).TrySampleAir(default,0,out var a);
            new AtmosphereColumn(293.15,80000).TrySampleAir(default,0,out var b);
            Assert.That(b.TemperatureK,Is.EqualTo(a.TemperatureK)); Assert.That(b.Density,Is.EqualTo(a.Density*.8).Within(1e-12));
        }
        [TestCase(199,101325,0)] [TestCase(331,101325,0)] [TestCase(293,0,0)]
        [TestCase(293,101325,11001)] [TestCase(double.NaN,101325,0)]
        public void InvalidReferenceIsRejected(double t,double p,double h)
        { Assert.Throws<ArgumentOutOfRangeException>(()=>new AtmosphereColumn(t,p,h)); }
        [TestCase(-501)] [TestCase(11001)]
        public void ColumnRejectsAltitudeOutsidePhysicalDomain(double h)
        { Assert.Throws<ArgumentOutOfRangeException>(()=>new AtmosphereColumn(293,101325).TrySampleAir(new DVector3(0,h,0),0,out _)); }
        [TestCase("quad_test_basic",false)] [TestCase("quad_test_rpm_table",false)]
        [TestCase("quad_test_ctcq",true)] [TestCase("quad_test_performance_map",true)]
        public void ProviderCompatibilityDoesNotInventScalingOfMeasuredRotorData(string name,bool compatible)
        {
            var p=Parameters(name);
            if(compatible) Assert.DoesNotThrow(()=>LiveAirValidation.RequireCompatible(p));
            else Assert.Throws<ArgumentException>(()=>LiveAirValidation.RequireCompatible(p));
        }
        [Test] public void InvalidLiveSampleIsRejectedBeforeItReachesForceOrPowerModels()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>LiveAirValidation.Validate(new AirSample(double.NaN,293,101325,0)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>LiveAirValidation.Validate(new AirSample(1,0,101325,0)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>LiveAirValidation.Validate(new AirSample(1,293,0,0)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>LiveAirValidation.Validate(new AirSample(1,293,101325,12000)));
        }
        [Test] public void ChangingAirPreservesChargeThermalHistoryAndPowerState()
        {
            var p=Parameters(); var power=new PowerSystem(p); var speeds=new double[]{500,500,500,500};
            foreach(double t in new[]{293.15,273.15,313.15})
            {
                new AtmosphereColumn(t,101325).TrySampleAir(default,0,out var air);
                double charge=power.ConsumedAh,energy=power.Thermal.GeneratedEnergyJ;
                power.Resolve(speeds,(double[])speeds.Clone(),.01,true,air.Density,
                    previousOmega:speeds,ambientTemperatureK:air.TemperatureK);
                Assert.That(power.ConsumedAh,Is.EqualTo(charge));
                power.Commit(.01);
                Assert.That(power.ConsumedAh,Is.GreaterThan(charge)); Assert.That(power.Thermal.GeneratedEnergyJ,Is.GreaterThan(energy));
                Assert.That(power.Thermal.GeneratedEnergyJ-power.Thermal.RejectedEnergyJ,
                    Is.EqualTo(power.Thermal.StoredEnergyJ).Within(1e-7));
            }
        }
        [TestCase("Rain",0)] [TestCase("None",10)] [TestCase("Snow",201)] [TestCase("unknown",1)]
        public void PrecipitationMetadataUsesTheExistingContractBounds(string kind,double intensity)
        { Assert.That(()=>new WeatherSample(kind,intensity),Throws.InstanceOf<ArgumentException>()); }
    }
}
