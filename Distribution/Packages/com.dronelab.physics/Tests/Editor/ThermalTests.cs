using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class ThermalTests
    {
        private static string Read(string name)=>ProfileTestFiles.Read(name);
        private static ProfileLoadResult Load(Action<JObject> edit=null,string env="environment_final_acceptance",Action<JObject> editEnv=null,string name="quad_test_thermal")
        {
            var j=JObject.Parse(Read(name)); edit?.Invoke(j); var e=JObject.Parse(Read(env)); editEnv?.Invoke(e);
            return ProfileLoader.Load(j.ToString(),e.ToString(),Read("drone-profile.schema"),Read("environment-profile.schema"));
        }
        private static RuntimeDroneParameters Parameters(Action<JObject> edit=null,string env="environment_final_acceptance",Action<JObject> editEnv=null,string name="quad_test_thermal")
        { var p=Load(edit,env,editEnv,name); Assert.That(p.Success,Is.True,string.Join("\n",p.Issues)); return p.Parameters; }
        private static double[] All(double x)=>new[]{x,x,x,x};
        private static JToken Electrical(JObject j,int i=0)=>j["rotors"][i]["motor"]["electrical"];
        private static void Resolve(PowerSystem b,double[] previous,double request=500,double dt=.01,bool powered=true,double ambient=293.15,double air=0,RotorDriveState drive=null)
            =>b.Resolve(All(request),previous,dt,powered,drive:drive,previousOmega:(double[])previous.Clone(),ambientTemperatureK:ambient,rotorAirSpeed:All(air),bodyAirSpeed:air);
        private static void Balance(PowerSystem b)
        {
            var t=b.Thermal;
            Assert.That(t.GeneratedEnergyJ-t.RejectedEnergyJ,Is.EqualTo(t.StoredEnergyJ).Within(1e-7));
            Assert.That(b.ChemicalEnergyJ,Is.EqualTo(b.TerminalEnergyJ+t.Battery.GeneratedEnergyJ).Within(1e-7));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void ConstantHeatingMatchesIndependentExponentialSolution(double dt)
        {
            var n=Parameters().Rotors[0].Power.Thermal; double temperature=293.15;
            for(int i=0;i<(int)(30/dt);i++) temperature=ThermalMath.Integrate(n,temperature,8,293.15,0,dt).Temperature;
            double expected=293.15+8/.25*(1-Math.Exp(-.25*30/15));
            Assert.That(temperature,Is.EqualTo(expected).Within(1e-9));
        }
        [Test] public void AdiabaticHeatingStoresAllLossAndNoRejectedEnergy()
        {
            var p=Parameters(j=> { Electrical(j)["thermal"]["heatTransferWPerK"]=0; Electrical(j)["thermal"]["airflowHeatTransferWPerKPerMps"]=0; });
            var s=ThermalMath.Integrate(p.Rotors[0].Power.Thermal,293.15,30,293.15,100,2);
            Assert.That(s.Temperature,Is.EqualTo(297.15)); Assert.That(s.GeneratedEnergy,Is.EqualTo(60));
            Assert.That(s.StoredEnergyChange,Is.EqualTo(60)); Assert.That(s.RejectedEnergy,Is.Zero);
        }
        [TestCase(280)] [TestCase(310)]
        public void AmbientHeatExchangeHasCorrectSignAndNoOvershoot(double ambient)
        {
            var s=ThermalMath.Integrate(Parameters().Rotors[0].Power.Thermal,293.15,0,ambient,0,1000);
            Assert.That(s.Temperature,Is.InRange(Math.Min(293.15,ambient),Math.Max(293.15,ambient)));
            Assert.That(s.Temperature,Is.EqualTo(ambient).Within(1e-5));
            Assert.That(Math.Sign(s.RejectedEnergy),Is.EqualTo(Math.Sign(293.15-ambient)));
        }
        [Test] public void AirflowCoolingIsStrongerAndCappedWithoutNegativeTemperatures()
        {
            var n=Parameters().Rotors[0].Power.Thermal;
            var slow=ThermalMath.Integrate(n,350,0,293.15,0,10); var fast=ThermalMath.Integrate(n,350,0,293.15,25,10);
            var clipped=ThermalMath.Integrate(n,350,0,293.15,100,10);
            Assert.That(fast.Temperature,Is.LessThan(slow.Temperature)); Assert.That(fast.Temperature,Is.GreaterThan(293.15));
            Assert.That(clipped.Temperature,Is.EqualTo(fast.Temperature));
        }
        [TestCase(330,1)] [TestCase(343.15,1)] [TestCase(358.15,.5)] [TestCase(373.15,0)] [TestCase(400,0)]
        public void ContinuousDeratingHasExplicitThresholds(double temperature,double expected)
            =>Assert.That(Parameters().Rotors[0].Power.Thermal.Authority(temperature),Is.EqualTo(expected).Within(1e-12));
        [Test] public void WarmWindingResistanceFollowsExplicitReferenceAndCoefficient()
        {
            var b=new PowerSystem(Parameters(j=>Electrical(j)["thermal"]["initialTemperatureK"]=343.15));
            Assert.That(b.MotorResistanceOhm(0),Is.EqualTo(.08*(1+.0039*50)).Within(1e-12));
            var cold=new PowerSystem(Parameters()); var warmSpeeds=All(500); var coldSpeeds=All(500);
            Resolve(b,warmSpeeds); Resolve(cold,coldSpeeds);
            Assert.That(b.RotorMotorLossW[0],Is.GreaterThan(cold.RotorMotorLossW[0]));
        }
        [Test] public void DisabledThermalPreservesLegacyElectricalResultEvenWithAuthoringData()
        {
            var p=Parameters(j=>j["powerSystem"]["thermalEnabled"]=false); var b=new PowerSystem(p);
            Assert.That(b.Thermal,Is.Null); Assert.That(b.MotorResistanceOhm(0),Is.EqualTo(.08));
            var output=All(500); b.Resolve(All(500),output,.01,true,previousOmega:All(500));
            var old=ProfileLoader.Load(Read("quad_test_coupled_power"),Read("environment_final_acceptance"),Read("drone-profile.schema"),Read("environment-profile.schema"));
            var baseline=new PowerSystem(old.Parameters); baseline.Resolve(All(500),All(500),.01,true,previousOmega:All(500));
            Assert.That(b.ElectricalPower,Is.EqualTo(baseline.ElectricalPower));
        }
        [Test] public void TrialEvaluationsAndRepeatedResolveDoNotHeatOrSpendCharge()
        {
            var b=new PowerSystem(Parameters(j=>j["powerSystem"]["battery"]["maxDischargeCurrentA"]=2));
            for(int i=0;i<5;i++) Resolve(b,All(500),900);
            Assert.That(b.Limited,Is.True); Assert.That(b.Thermal.GeneratedEnergyJ,Is.Zero); Assert.That(b.Thermal.Motor(0).TemperatureK,Is.EqualTo(293.15));
            Assert.That(b.Soc,Is.EqualTo(1)); double expected=(b.MotorLossPower+b.EscLossPower+b.BatteryLossPower)*.01;
            b.Commit(.01); Assert.That(b.Thermal.GeneratedEnergyJ,Is.EqualTo(expected).Within(1e-12)); Balance(b);
            Assert.Throws<InvalidOperationException>(()=>b.Commit(.01));
        }
        [Test] public void CommitRequiresThePreparedTimestepAndFailedQueryCancelsIt()
        {
            var b=new PowerSystem(Parameters()); Resolve(b,All(500));
            Assert.Throws<InvalidOperationException>(()=>b.Commit(.02));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Resolve(b,All(500),ambient:0));
            Assert.Throws<InvalidOperationException>(()=>b.Commit(.01)); Assert.That(b.Thermal.GeneratedEnergyJ,Is.Zero);
        }
        [Test] public void HeatAndElectricalEnergyBalanceAcrossCoupledFlightAndReset()
        {
            var b=new PowerSystem(Parameters()); var omega=All(0); double motorEnergy=0,escEnergy=0;
            for(int i=0;i<500;i++)
            {
                Resolve(b,omega,700); motorEnergy+=b.MotorLossPower*.01; escEnergy+=b.EscLossPower*.01; b.Commit(.01); Balance(b);
            }
            Assert.That(Enumerable.Range(0,4).Sum(i=>b.Thermal.Motor(i).GeneratedEnergyJ),Is.EqualTo(motorEnergy).Within(1e-8));
            Assert.That(Enumerable.Range(0,4).Sum(i=>b.Thermal.Esc(i).GeneratedEnergyJ),Is.EqualTo(escEnergy).Within(1e-8));
            b.Reset(); Assert.That(b.Thermal.StoredEnergyJ,Is.Zero); Assert.That(b.Thermal.GeneratedEnergyJ,Is.Zero); Assert.That(b.Soc,Is.EqualTo(1));
        }
        [TestCase("motor")] [TestCase("esc")] [TestCase("battery")]
        public void CutoffStopsElectricalDriveButKeepsSpinAndCooling(string component)
        {
            var b=new PowerSystem(Parameters(j=> {
                var n=component=="battery" ? j["powerSystem"]["battery"]["thermal"] : Electrical(j)[component=="motor" ? "thermal":"escThermal"];
                n["initialTemperatureK"]=n["cutoffTemperatureK"].DeepClone();
            }));
            var omega=All(500); double initial=component=="battery" ? b.Thermal.Battery.TemperatureK : component=="motor" ? b.Thermal.Motor(0).TemperatureK : b.Thermal.Esc(0).TemperatureK;
            Resolve(b,omega,900); Assert.That(omega[0],Is.InRange(1.0,499.99)); Assert.That(b.RotorCurrentA[0],Is.Zero);
            Assert.That(b.RotorTorqueNm[0],Is.Zero); Assert.That(b.ThermalDerated,Is.True); b.Commit(.01);
            double final=component=="battery" ? b.Thermal.Battery.TemperatureK : component=="motor" ? b.Thermal.Motor(0).TemperatureK : b.Thermal.Esc(0).TemperatureK;
            Assert.That(final,Is.LessThan(initial)); Balance(b);
        }
        [TestCase("motor")] [TestCase("esc")] [TestCase("battery")]
        public void PartialDeratingConstrainsCurrentWithoutInventingThrustMultiplier(string component)
        {
            var b=new PowerSystem(Parameters(j=> {
                if(component=="motor") Electrical(j)["thermal"]["initialTemperatureK"]=368.15;
                if(component=="esc") Electrical(j)["escThermal"]["initialTemperatureK"]=348.15;
                if(component=="battery") { j["powerSystem"]["battery"]["thermal"]["initialTemperatureK"]=323.15; j["powerSystem"]["battery"]["maxDischargeCurrentA"]=4; }
            }));
            var omega=All(500); Resolve(b,omega,900); Assert.That(b.Limited,Is.True);
            if(component=="motor") Assert.That(b.MotorCurrentA[0],Is.LessThanOrEqualTo(5+1e-9));
            if(component=="esc") Assert.That(b.MotorCurrentA[0],Is.LessThanOrEqualTo(8.75+1e-9));
            if(component=="battery") Assert.That(b.Current,Is.LessThanOrEqualTo(2+1e-9));
            Assert.That(omega[0],Is.GreaterThan(0));
        }
        [Test] public void DisarmAndFaultCoolWithZeroDisconnectedComponentLoss()
        {
            var b=new PowerSystem(Parameters(j=>Electrical(j)["thermal"]["initialTemperatureK"]=330)); var drive=new RotorDriveState(4); drive.Set(0,0);
            var omega=All(500); Resolve(b,omega,500,drive:drive); Assert.That(b.RotorMotorLossW[0],Is.Zero); Assert.That(b.RotorEscLossW[0],Is.Zero);
            b.Commit(.01); Assert.That(b.Thermal.Motor(0).TemperatureK,Is.LessThan(330));
            double generated=b.Thermal.GeneratedEnergyJ; Resolve(b,omega,0,powered:false); b.Commit(.01);
            Assert.That(b.Current,Is.Zero); Assert.That(b.Thermal.GeneratedEnergyJ,Is.EqualTo(generated)); Balance(b);
        }
        [Test] public void NearlyCutoffMotorBelowNoLoadCurrentCoastsWithoutBlockingOthers()
        {
            var b=new PowerSystem(Parameters(j=>Electrical(j)["thermal"]["initialTemperatureK"]=373.1));
            Assert.That(b.IsThermalDriveAvailable(0),Is.False); var omega=All(500); Resolve(b,omega,550);
            Assert.That(b.RotorCurrentA[0],Is.Zero); Assert.That(b.RotorCurrentA[1],Is.GreaterThan(0));
            Assert.That(omega[0],Is.LessThan(500)); Assert.That(omega[1],Is.EqualTo(550));
        }
        [TestCase(false)] [TestCase(true)]
        public void FirstOrderThermalModeKeepsCallerCoastAndCoolsWhenDisconnected(bool cutoff)
        {
            var p=Parameters(j=> {
                j["physicsConfiguration"]["modules"]["gyroscopicRotorEffects"]=false;
                foreach(var r in j["rotors"]) r["motor"]["dynamicsModel"]="FirstOrder";
                if(cutoff) Electrical(j)["thermal"]["initialTemperatureK"]=373.15;
            });
            var b=new PowerSystem(p); var output=new double[4];
            b.Resolve(All(450),output,.01,cutoff,ambientTemperatureK:293.15,rotorAirSpeed:All(0));
            Assert.That(output[0],Is.EqualTo(450)); Assert.That(b.RotorCurrentA[0],Is.Zero);
            double initial=b.Thermal.Motor(0).TemperatureK; b.Commit(.01);
            if(cutoff) Assert.That(b.Thermal.Motor(0).TemperatureK,Is.LessThan(initial));
            Balance(b);
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void CoupledThermalGovernorConvergesAcrossTimesteps(double dt)
        {
            var p=Parameters(name:"quad_test_thermal_stress");
            double[] Simulate(double step)
            {
                var b=new PowerSystem(p); var omega=All(0);
                for(int i=0;i<(int)(12/step);i++)
                {
                    double request=PhysicsMath.MotorStep(omega[0],900,.06,step); Resolve(b,omega,request,step); b.Commit(step);
                }
                Balance(b); Assert.That(b.ThermalDerated,Is.True);
                return new[]{omega[0],b.Thermal.Motor(0).TemperatureK,b.Soc};
            }
            var reference=Simulate(.001); var actual=Simulate(dt);
            Assert.That(actual[0],Is.EqualTo(reference[0]).Within(5)); Assert.That(actual[1],Is.EqualTo(reference[1]).Within(.25));
            Assert.That(actual[2],Is.EqualTo(reference[2]).Within(.0002));
        }
        [TestCase("missingBattery")] [TestCase("missingMotor")] [TestCase("missingEsc")] [TestCase("capacity")] [TestCase("threshold")]
        [TestCase("missingReference")] [TestCase("nonpositiveResistance")] [TestCase("simple")]
        public void InvalidThermalConfigurationsAreRejected(string problem)
        {
            var r=Load(j=> {
                if(problem=="missingBattery") ((JObject)j["powerSystem"]["battery"]).Remove("thermal");
                if(problem=="missingMotor") ((JObject)Electrical(j)).Remove("thermal");
                if(problem=="missingEsc") ((JObject)Electrical(j)).Remove("escThermal");
                if(problem=="capacity") Electrical(j)["thermal"]["heatCapacityJPerK"]=0;
                if(problem=="threshold") Electrical(j)["thermal"]["cutoffTemperatureK"]=300;
                if(problem=="missingReference") ((JObject)Electrical(j)).Remove("resistanceReferenceTemperatureK");
                if(problem=="nonpositiveResistance") { Electrical(j)["resistanceReferenceTemperatureK"]=500; Electrical(j)["resistanceTemperatureCoefficientPerK"]=.01; }
                if(problem=="simple") { j["powerSystem"]["battery"]["mode"]="Simple"; j["physicsConfiguration"]["modules"]["motorElectrical"]=false; }
            }); Assert.That(r.Success,Is.False);
        }
        [Test] public void MissingAmbientInConstantEnvironmentIsRejectedButLegacyStillLoads()
        {
            Assert.That(Load(env:"environment_calm",editEnv:e=>e.Remove("temperatureK")).Success,Is.False);
            Assert.That(Load(j=>j["powerSystem"]["thermalEnabled"]=false,env:"environment_calm",editEnv:e=>e.Remove("temperatureK")).Success,Is.True);
        }
        [Test] public void InvalidCoolingQueriesDoNotChangeThermalOrSocState()
        {
            var b=new PowerSystem(Parameters());
            Assert.Throws<ArgumentException>(()=>b.Resolve(All(500),All(500),.01,true,previousOmega:All(500)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Resolve(b,All(500),air:-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>Resolve(b,All(500),ambient:double.NaN));
            Assert.That(b.Thermal.GeneratedEnergyJ,Is.Zero); Assert.That(b.Soc,Is.EqualTo(1));
        }
        [TestCase("environment_thermal_warm")] [TestCase("environment_thermal_snow")] [TestCase("environment_thermal_rain")] [TestCase("environment_thermal_hail")]
        public void WeatherMetadataDoesNotApplyExtraForcesOrPower(string environment)
        {
            var p=Parameters(env:environment); var q=Parameters(env:environment,editEnv:e=>e.Remove("weather"));
            Assert.That(p.Environment.SampleAir(10).Density,Is.EqualTo(q.Environment.SampleAir(10).Density));
            Assert.That(p.Environment.Sample(new DVector3(3,4,5),7),Is.EqualTo(q.Environment.Sample(new DVector3(3,4,5),7)));
            var a=new PowerSystem(p); var b=new PowerSystem(q); Resolve(a,All(500)); Resolve(b,All(500));
            Assert.That(a.ElectricalPower,Is.EqualTo(b.ElectricalPower));
        }
        [Test] public void SnowAmbientCoolsFasterThanWarmAmbientAndSnapshotIsImmutable()
        {
            var cold=Parameters(env:"environment_thermal_snow"); var warm=Parameters(env:"environment_thermal_warm");
            double t1=ThermalMath.Integrate(cold.Battery.Thermal,293.15,0,cold.Environment.SampleAir(0).TemperatureK,2,60).Temperature;
            double t2=ThermalMath.Integrate(warm.Battery.Thermal,293.15,0,warm.Environment.SampleAir(0).TemperatureK,2,60).Temperature;
            Assert.That(t1,Is.LessThan(t2));
            var loaded=Load(); loaded.Drone.rotors[0].motor.electrical.thermal.heatCapacityJPerK=1;
            Assert.That(loaded.Parameters.Rotors[0].Power.Thermal.Capacity,Is.EqualTo(15));
        }
        [Test] public void InvalidPrecipitationMeaningIsRejected()
            =>Assert.That(Load(env:"environment_thermal_rain",editEnv:e=>e["weather"]["intensityMmPerHour"]=0).Success,Is.False);
        [Test] public void ThermalPowerSupportsActualAxialLoadFromRpmJMap()
        {
            var p=Parameters(j=> {
                var thermal=JObject.Parse(Read("quad_test_thermal"));
                j["powerSystem"]=thermal["powerSystem"].DeepClone();
                for(int i=0;i<4;i++) j["rotors"][i]["motor"]=thermal["rotors"][i]["motor"].DeepClone();
            },name:"quad_test_coupled_power_map");
            var b=new PowerSystem(p); var omega=All(500);
            b.Resolve(All(500),omega,.01,true,axialVelocity:All(3),previousOmega:All(500),ambientTemperatureK:293.15,rotorAirSpeed:All(3),bodyAirSpeed:3);
            double expected=p.Rotors.Sum(r=>r.Performance.Evaluate(500,3,p.Density).Torque*500);
            Assert.That(b.PropellerPower,Is.EqualTo(expected).Within(1e-7)); b.Commit(.01); Balance(b);
            Assert.That(b.Thermal.GeneratedEnergyJ,Is.GreaterThan(0));
        }
        [Test] public void CsvIncludesThermalEnergyStatesAndLeavesUnsupportedValuesEmpty()
        {
            var sw=new StringWriter(); var writer=new FlightCsvWriter(sw,new[]{"FL"});
            writer.Write(new FlightTelemetryFrame { AmbientTemperature=293.15,BatteryTemperature=300,ThermalGeneratedEnergy=20,ThermalRejectedEnergy=5,ThermalStoredEnergy=15,
                Precipitation="Rain",PrecipitationIntensity=10,Rotors=new[]{new RotorTelemetry(0,1,0,0,0,0,null,null,null,1,0,default,default,false,motorTemperature:310)} });
            var lines=sw.ToString().Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries); var head=lines[0].Split(','); var cells=lines[1].Split(',');
            Assert.That(cells[Array.IndexOf(head,"thermal_stored_j_end")],Is.EqualTo("15"));
            Assert.That(cells[Array.IndexOf(head,"rotor_0_FL_motor_temp_k_end")],Is.EqualTo("310"));
            Assert.That(cells[Array.IndexOf(head,"rotor_0_FL_esc_temp_k_end")],Is.Empty);
            Assert.That(cells[Array.IndexOf(head,"precipitation")],Is.EqualTo("Rain")); Assert.That(cells.Length,Is.EqualTo(head.Length));
        }
    }
}
