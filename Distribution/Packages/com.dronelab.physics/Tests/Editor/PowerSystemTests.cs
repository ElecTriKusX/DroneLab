using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class PowerSystemTests
    {
        private static string Read(string name) => ProfileTestFiles.Read(name);
        private static ProfileLoadResult Load(string mode="simple",Action<JObject> edit=null)
        {
            var j=JObject.Parse(Read("quad_test_battery_"+mode)); edit?.Invoke(j);
            return ProfileLoader.Load(j.ToString(),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema"));
        }
        private static RuntimeDroneParameters Parameters(string mode="simple",Action<JObject> edit=null)
        {
            var result=Load(mode,edit); Assert.That(result.Success,Is.True,string.Join("\n",result.Issues)); return result.Parameters;
        }
        private static double[] Speeds(RuntimeDroneParameters p,double fraction=.5)=>p.Rotors.Select(r=>r.MaxOmega*fraction).ToArray();
        private static void ConstantVoltage(JObject j)
        {
            j["physicsConfiguration"]["modules"]["batteryVoltageSag"]=false;
            foreach(var point in j["powerSystem"]["battery"]["ocvCurve"]) point["voltageV"]=14.8;
        }
        [TestCase("simple")] [TestCase("electrical")]
        public void AtRestAndDisarmedDrawNoCurrent(string mode)
        {
            var p=Parameters(mode); var b=new PowerSystem(p); var omega=new double[4];
            b.Resolve(omega,omega,.01,true); b.Commit(.01);
            Assert.That(b.Current,Is.Zero); Assert.That(b.Soc,Is.EqualTo(1));
            omega=Speeds(p); var output=new double[4]; b.Resolve(omega,output,.01,false); b.Commit(.01);
            Assert.That(output,Is.EqualTo(omega)); Assert.That(b.Current,Is.Zero); Assert.That(b.TerminalVoltage,Is.EqualTo(b.OpenVoltage));
            Assert.That(b.TerminalEnergyJ,Is.Zero);
        }
        [TestCase(0)] [TestCase(.06)] [TestCase(1e-15)]
        public void BatteryRootMatchesIndependentCircuitEquation(double resistance)
        {
            double i=PowerSystem.CurrentForPower(100,16,resistance);
            Assert.That(i*(16-i*resistance),Is.EqualTo(100).Within(1e-10));
            Assert.That(i,Is.LessThanOrEqualTo(resistance==0 ? double.PositiveInfinity : 16/(2*resistance)));
            Assert.That(PowerSystem.CurrentForPower(0,16,resistance),Is.Zero);
        }
        [Test] public void ImpossibleConstantPowerHasNoRealRoot()
            => Assert.That(PowerSystem.CurrentForPower(65,16,1),Is.EqualTo(double.PositiveInfinity));
        [Test] public void SimplePowerMatchesShaftEfficiencyAndNoLoadLoss()
        {
            var p=Parameters(edit:ConstantVoltage); var b=new PowerSystem(p); var omega=Speeds(p);
            double shaft=4*p.Rotors[0].Performance.Evaluate(omega[0]).Torque*omega[0];
            double expected=(shaft/.85+4*14.8*.4)/.95;
            b.Resolve(omega,omega,.01,true);
            Assert.That(b.RpmScale,Is.EqualTo(1)); Assert.That(b.MechanicalPower,Is.EqualTo(shaft).Within(1e-10));
            Assert.That(b.ElectricalPower,Is.EqualTo(expected).Within(1e-10)); Assert.That(b.Current,Is.EqualTo(expected/14.8).Within(1e-10));
        }
        [Test] public void ElectricalPowerMatchesBackEmfCopperAndEscLossWithoutEfficiencyDoubleCount()
        {
            var p=Parameters("electrical",ConstantVoltage); var b=new PowerSystem(p); var omega=Speeds(p);
            double kt=60/(2*Math.PI*900),q=p.Rotors[0].Performance.Evaluate(omega[0]).Torque;
            double phase=.4+q/kt,voltage=omega[0]*kt+phase*.08;
            b.Resolve(omega,omega,.01,true);
            Assert.That(b.RpmScale,Is.EqualTo(1)); Assert.That(b.ElectricalPower,Is.EqualTo(4*voltage*phase/.95).Within(1e-10));
            Assert.That(b.ElectricalPower,Is.GreaterThan(b.MechanicalPower));
        }
        [TestCase("simple")] [TestCase("electrical")]
        public void SagAndEnergyBalanceAgreeAndBusCurrentsSum(string mode)
        {
            var p=Parameters(mode); var b=new PowerSystem(p); var omega=Speeds(p); b.Resolve(omega,omega,.1,true); b.Commit(.1);
            Assert.That(b.TerminalVoltage,Is.EqualTo(b.OpenVoltage-b.Current*.06).Within(1e-12));
            Assert.That(b.RotorCurrentA.Sum(),Is.EqualTo(b.Current).Within(1e-10));
            Assert.That(b.ChemicalEnergyJ,Is.EqualTo(b.TerminalEnergyJ+b.BatteryLossPower*.1).Within(1e-10));
            Assert.That(b.Soc,Is.EqualTo(1-b.Current*.1/(1.3*3600)).Within(1e-12));
            Assert.That(b.ConsumedAh,Is.EqualTo(b.Current*.1/3600).Within(1e-12));
        }
        [TestCase("simple")] [TestCase("electrical")]
        public void OverloadReducesAllRpmWithoutChangingTheirRatio(string mode)
        {
            var p=Parameters(mode,j=>j["powerSystem"]["battery"]["maxDischargeCurrentA"]=5); var b=new PowerSystem(p);
            var requested=Speeds(p,.9); requested[1]*=.8; var output=new double[4]; b.Resolve(requested,output,.01,true);
            Assert.That(b.Limited,Is.True); Assert.That(b.Current,Is.LessThanOrEqualTo(5)); Assert.That(b.Current,Is.EqualTo(5).Within(1e-8));
            Assert.That(output[1]/output[0],Is.EqualTo(.8).Within(1e-12));
            Assert.That(b.ElectricalPower,Is.GreaterThanOrEqualTo(b.MechanicalPower));
        }
        [TestCase("simple","maxPowerW",15)] [TestCase("electrical","maxPowerW",15)]
        [TestCase("simple","maxCurrentA",2)] [TestCase("electrical","maxCurrentA",2)]
        [TestCase("simple","escMaxCurrentA",2)] [TestCase("electrical","escMaxCurrentA",2)]
        public void IndividualMotorAndEscLimitsReduceRpm(string mode,string field,double limit)
        {
            var p=Parameters(mode,j=>j["rotors"][0]["motor"]["electrical"][field]=limit); var b=new PowerSystem(p); var omega=Speeds(p,.9);
            b.Resolve(omega,omega,.01,true); Assert.That(b.Limited,Is.True);
            var r=p.Rotors[0]; double q=r.Performance.Evaluate(omega[0]).Torque;
            if(field=="maxPowerW") Assert.That(b.RotorCurrentA[0]*b.TerminalVoltage*.95,Is.LessThanOrEqualTo(limit+1e-8));
            else if(mode=="electrical") Assert.That(.4+q/(60/(2*Math.PI*900)),Is.LessThanOrEqualTo(limit+1e-8));
            else Assert.That(b.RotorCurrentA[0],Is.LessThanOrEqualTo(limit+1e-8));
        }
        [TestCase("simple")] [TestCase("electrical")]
        public void VoltageLimitsAvailableSpeed(string mode)
        {
            var p=Parameters(mode,j=> { foreach(var v in j["powerSystem"]["battery"]["ocvCurve"]) v["voltageV"]=8; });
            var b=new PowerSystem(p); var omega=Speeds(p,1); b.Resolve(omega,omega,.01,true); Assert.That(b.Limited,Is.True);
            if(mode=="simple") Assert.That(omega[0],Is.LessThanOrEqualTo(p.Rotors[0].MaxOmega*b.TerminalVoltage/14.8+1e-8));
            else
            {
                double kt=60/(2*Math.PI*900),q=p.Rotors[0].Performance.Evaluate(omega[0]).Torque;
                Assert.That(omega[0]*kt+(.4+q/kt)*.08,Is.LessThanOrEqualTo(b.TerminalVoltage+1e-8));
            }
        }
        [Test] public void OcvInterpolationClampsAndSnapshotDoesNotFollowDtoMutations()
        {
            var result=Load(); var b=result.Parameters.Battery;
            result.Drone.powerSystem.battery.ocvCurve[0].voltageV=999; result.Drone.rotors[0].motor.electrical.motorEfficiency=.1;
            Assert.That(b.OpenCircuitVoltage(-1),Is.EqualTo(12)); Assert.That(b.OpenCircuitVoltage(.5),Is.EqualTo(14.8).Within(1e-12));
            Assert.That(b.OpenCircuitVoltage(2),Is.EqualTo(16.8)); Assert.That(result.Parameters.Rotors[0].Power.Efficiency,Is.EqualTo(.85));
        }
        [TestCase("simple")] [TestCase("electrical")]
        public void EmptyPackCannotCreateForceAndDischargeDoesNotGoNegative(string mode)
        {
            var p=Parameters(mode,j=>j["powerSystem"]["battery"]["initialSoc"]=0); var b=new PowerSystem(p); var omega=Speeds(p);
            b.Resolve(omega,omega,.01,true); b.Commit(.01);
            Assert.That(omega.All(x=>x==0),Is.True); Assert.That(b.Soc,Is.Zero); Assert.That(b.Current,Is.Zero); Assert.That(b.Limited,Is.True);
            p=Parameters(mode,j=>j["powerSystem"]["battery"]["capacityAh"]=1e-5); b=new PowerSystem(p); omega=Speeds(p);
            b.Resolve(omega,omega,1,true); b.Commit(1);
            Assert.That(b.ConsumedAh,Is.LessThanOrEqualTo(1e-5+1e-12)); Assert.That(b.Soc,Is.GreaterThanOrEqualTo(0));
        }
        [Test] public void DisabledDischargeKeepsSocButStillModelsPowerAndSag()
        {
            var p=Parameters(edit:j=>j["physicsConfiguration"]["modules"]["batteryDischarge"]=false); var b=new PowerSystem(p); var omega=Speeds(p);
            b.Resolve(omega,omega,1,true); b.Commit(1); Assert.That(b.Soc,Is.EqualTo(1)); Assert.That(b.ConsumedAh,Is.GreaterThan(0));
            Assert.That(b.TerminalVoltage,Is.LessThan(b.OpenVoltage));
        }
        [TestCase("simple",.005)] [TestCase("simple",.01)] [TestCase("simple",.02)]
        [TestCase("electrical",.005)] [TestCase("electrical",.01)] [TestCase("electrical",.02)]
        public void ConstantOcvCoulombCountingConvergesExactlyAcrossTimesteps(string mode,double dt)
        {
            var p=Parameters(mode,ConstantVoltage); var b=new PowerSystem(p); var requested=Speeds(p); var output=new double[4];
            b.Resolve(requested,output,dt,true); double initialCurrent=b.Current;
            for(int k=0;k<(int)(20/dt);k++) { b.Resolve(requested,output,dt,true); b.Commit(dt); }
            Assert.That(b.Soc,Is.EqualTo(1-initialCurrent*20/(1.3*3600)).Within(1e-10));
            b.Reset(); Assert.That(b.Soc,Is.EqualTo(1)); Assert.That(b.Current+b.ConsumedAh+b.TerminalEnergyJ,Is.Zero);
        }
        [TestCase("capacityAh")] [TestCase("initialSoc")] [TestCase("ocvCurve")] [TestCase("nominalVoltageV")]
        public void RequiredBatteryFieldsCannotBeOmitted(string field)
            => Assert.That(Load(edit:j=>((JObject)j["powerSystem"]["battery"]).Remove(field)).Success,Is.False);
        [Test] public void InconsistentFlagsMissingMotorAndUnorderedOcvRejected()
        {
            Assert.That(Load(edit:j=>j["physicsConfiguration"]["modules"]["motorElectrical"]=true).Success,Is.False);
            Assert.That(Load("electrical",j=>j["physicsConfiguration"]["modules"]["motorElectrical"]=false).Success,Is.False);
            Assert.That(Load(edit:j=>((JObject)j["rotors"][0]["motor"]).Remove("electrical")).Success,Is.False);
            Assert.That(Load(edit:j=>j["powerSystem"]["battery"]["ocvCurve"][1]["soc"]=0).Success,Is.False);
            Assert.That(Load(edit:j=>j["powerSystem"]["battery"]["ocvCurve"][1]["voltageV"]=11).Success,Is.False);
            Assert.That(Load(edit:j=>j["powerSystem"]["battery"]["mode"]="None").Success,Is.False);
        }
        [Test] public void UnsupportedPowerMapAndEfficiencyCurveAreExplicitlyRejected()
        {
            Assert.That(Load(edit:j=>j["rotors"][0]["motor"]["electrical"]["efficiencyCurve"]=new JArray(new JObject { ["loadFraction"]=0,["efficiency"]=.8 })).Success,Is.False);
            Assert.That(Load(edit:j=>j["rotors"][0]["performance"]=JObject.Parse(Read("quad_test_performance_map"))["rotors"][0]["performance"]).Success,Is.False);
        }
        [TestCase("simple")] [TestCase("electrical")]
        public void RpmTablePowerUsesTorqueAndKeepsCsvCurrentSeparate(string mode)
        {
            var p=Parameters(mode,j=> { var table=JObject.Parse(Read("quad_test_rpm_table"));
                for(int i=0;i<4;i++) j["rotors"][i]["performance"]=table["rotors"][i]["performance"].DeepClone(); });
            var b=new PowerSystem(p); var omega=Speeds(p); b.Resolve(omega,omega,.01,true);
            Assert.That(b.MechanicalPower,Is.EqualTo(4*p.Rotors[0].Performance.Evaluate(omega[0]).Torque*omega[0]).Within(1e-10));
            Assert.That(p.Rotors[0].Performance.Evaluate(omega[0]).Current.HasValue,Is.True);
        }
        [Test] public void InvalidQueriesDoNotSpendCharge()
        {
            var p=Parameters(); var b=new PowerSystem(p); var omega=Speeds(p);
            Assert.Throws<ArgumentOutOfRangeException>(()=>b.Resolve(omega,omega,0,true));
            omega[0]=double.NaN; Assert.Throws<ArgumentOutOfRangeException>(()=>b.Resolve(omega,omega,.01,true));
            Assert.That(b.Soc,Is.EqualTo(1));
        }
        [TestCase("simple")] [TestCase("electrical")]
        public void CtCqPowerMatchesTheEquivalentOmegaSquaredFixture(string mode)
        {
            var basic=Parameters(mode); var p=Parameters(mode,j=> { var table=JObject.Parse(Read("quad_test_ctcq"));
                for(int i=0;i<4;i++) j["rotors"][i]["performance"]=table["rotors"][i]["performance"].DeepClone(); });
            var a=new PowerSystem(basic); var b=new PowerSystem(p); var wa=Speeds(basic); var wb=Speeds(p);
            a.Resolve(wa,wa,.01,true); b.Resolve(wb,wb,.01,true);
            Assert.That(b.Current,Is.EqualTo(a.Current).Within(1e-5)); Assert.That(b.MechanicalPower,Is.EqualTo(a.MechanicalPower).Within(1e-4));
        }
        [TestCase("simple")] [TestCase("electrical")]
        public void RandomAsymmetricCommandsRespectEnergyCurrentAndVoltageBounds(string mode)
        {
            var p=Parameters(mode); var b=new PowerSystem(p); var rng=new Random(963); var output=new double[4];
            for(int k=0;k<500;k++)
            {
                var requested=p.Rotors.Select(r=>rng.NextDouble()*r.MaxOmega).ToArray();
                b.Resolve(requested,output,.02,true); double previous=b.Soc; b.Commit(.02);
                Assert.That(b.Current,Is.InRange(0,p.Battery.MaxCurrent));
                Assert.That(b.Soc,Is.InRange(0,previous)); Assert.That(b.TerminalVoltage,Is.InRange(b.OpenVoltage/2,b.OpenVoltage));
                Assert.That(b.ElectricalPower,Is.GreaterThanOrEqualTo(b.MechanicalPower-1e-9));
                Assert.That(b.RotorCurrentA.Sum(),Is.EqualTo(b.Current).Within(1e-9));
                Assert.That(b.ChemicalEnergyJ-b.TerminalEnergyJ,Is.GreaterThanOrEqualTo(-1e-9));
                for(int i=0;i<4;i++) Assert.That(output[i],Is.InRange(0,requested[i]));
            }
        }
        [TestCase("simple")] [TestCase("electrical")]
        public void VariableOcvDischargeConvergesAcrossTimesteps(string mode)
        {
            double[] final=new double[3]; double[] steps={.005,.01,.02};
            for(int i=0;i<3;i++)
            {
                var p=Parameters(mode); var b=new PowerSystem(p); var requested=Speeds(p); var output=new double[4];
                for(int k=0;k<(int)(20/steps[i]);k++) { b.Resolve(requested,output,steps[i],true); b.Commit(steps[i]); }
                final[i]=b.Soc;
            }
            Assert.That(final.Max()-final.Min(),Is.LessThan(1e-6));
        }
        [TestCase("simple",.005)] [TestCase("simple",.01)] [TestCase("simple",.02)]
        [TestCase("electrical",.005)] [TestCase("electrical",.01)] [TestCase("electrical",.02)]
        public void AltitudePidAndMotorLagHoverWithBatteryAcrossTimesteps(string mode,double dt)
        {
            var p=Parameters(mode); var power=new PowerSystem(p); var controller=new FlightController(); var allocator=new QuadAllocator(p);
            var omega=new double[4]; var commands=new double[4]; double h=0,v=0; bool saturated=false;
            for(int k=0;k<(int)(10/dt);k++)
            {
                double accel=controller.ClimbAcceleration(-h,v,dt,3,new PidTerms(.1,0,.5),3,new PidTerms(.8,.1,2),2,4,!saturated && !power.Limited);
                saturated=allocator.Allocate(p.Mass*(p.Gravity+accel),default,commands);
                for(int i=0;i<4;i++)
                {
                    var r=p.Rotors[i]; double target=commands[i]*r.MaxOmega;
                    omega[i]=PhysicsMath.MotorStep(omega[i],target,target>omega[i] ? r.TauUp : r.TauDown,dt);
                }
                power.Resolve(omega,omega,dt,true); power.Commit(dt);
                double thrust=0; for(int i=0;i<4;i++) thrust+=p.Rotors[i].Performance.Evaluate(omega[i]).Thrust;
                v+=(thrust/p.Mass-p.Gravity)*dt; h+=v*dt;
            }
            Assert.That(Math.Abs(h),Is.LessThan(.025)); Assert.That(Math.Abs(v),Is.LessThan(.01));
            Assert.That(power.Soc,Is.LessThan(1)); Assert.That(power.Limited,Is.False);
        }
    }
}
