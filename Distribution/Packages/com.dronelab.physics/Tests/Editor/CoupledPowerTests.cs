using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class CoupledPowerTests
    {
        private static string Read(string name)=>ProfileTestFiles.Read(name);
        private static ProfileLoadResult Load(Action<JObject> edit=null,string name="quad_test_coupled_power")
        {
            var j=JObject.Parse(Read(name)); edit?.Invoke(j);
            return ProfileLoader.Load(j.ToString(),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema"));
        }
        private static RuntimeDroneParameters Parameters(Action<JObject> edit=null,string name="quad_test_coupled_power")
        { var p=Load(edit,name); Assert.That(p.Success,Is.True,string.Join("\n",p.Issues)); return p.Parameters; }
        private static double[] All(double value)=>new[]{value,value,value,value};
        private static void Balance(PowerSystem b)
        {
            Assert.That(Math.Abs(b.SpinBalanceErrorPower),Is.LessThan(1e-7));
            Assert.That(b.MechanicalPower,Is.EqualTo(b.PropellerPower+b.SpinEnergyChangePower+b.SpinBalanceErrorPower).Within(1e-7));
            Assert.That(b.ElectricalPower,Is.EqualTo(b.MechanicalPower+b.MotorLossPower+b.EscLossPower).Within(1e-7));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void AccelerationIncludesSpinEnergyLossesAndBodyReaction(double dt)
        {
            var p=Parameters(); var b=new PowerSystem(p); var previous=All(500); var output=new double[4];
            b.Resolve(All(600),output,dt,true,previousOmega:previous); Balance(b);
            Assert.That(output,Is.EqualTo(All(600))); Assert.That(b.Current,Is.GreaterThan(0));
            for(int i=0;i<4;i++)
            {
                double q=p.Rotors[i].Performance.Evaluate((output[i]+previous[i])/2).Torque;
                Assert.That(b.RotorAccelerationTorqueNm[i],Is.EqualTo(.000005*100/dt).Within(1e-12));
                Assert.That(b.RotorTorqueNm[i],Is.EqualTo(q+.000005*100/dt).Within(1e-12));
                Assert.That(b.RotorSpinEnergyJ[i],Is.EqualTo(.5*.000005*600*600).Within(1e-12));
            }
        }
        [Test] public void SteadyStateAgreesWithLegacyDcPowerAndHasZeroSpinEnergyRate()
        {
            var p=Parameters(); var b=new PowerSystem(p); var speeds=All(500);
            b.Resolve(speeds,speeds,.01,true,previousOmega:All(500)); Balance(b);
            Assert.That(b.SpinEnergyChangePower,Is.Zero); Assert.That(b.SpinBalanceErrorPower,Is.Zero);
            double q=p.Rotors[0].Performance.Evaluate(500).Torque,kt=60/(2*Math.PI*900);
            double phase=.4+q/kt,voltage=500*kt+phase*.08;
            Assert.That(b.ElectricalPower,Is.EqualTo(4*phase*voltage/.95).Within(1e-10));
        }
        [TestCase(false)] [TestCase(true)]
        public void UnpoweredOrEmptyPackPreservesCoastingEnergyWithoutCurrent(bool empty)
        {
            var p=Parameters(j=> { if(empty) j["powerSystem"]["battery"]["initialSoc"]=0; });
            var b=new PowerSystem(p); var output=new double[4];
            b.Resolve(All(0),output,.01,empty,previousOmega:All(500)); Balance(b);
            Assert.That(output.All(x=>x>0 && x<500),Is.True); Assert.That(b.Current,Is.Zero);
            Assert.That(b.SpinEnergyChangePower,Is.LessThan(0)); Assert.That(b.RotorTorqueNm,Is.EqualTo(All(0)));
            b.Commit(.01); Assert.That(b.TerminalEnergyJ,Is.Zero);
        }
        [Test] public void CurrentLimitLimitsAccelerationInsteadOfErasingExistingSpeed()
        {
            var p=Parameters(j=>j["powerSystem"]["battery"]["maxDischargeCurrentA"]=4); var b=new PowerSystem(p);
            var output=new double[4]; b.Resolve(All(900),output,.01,true,previousOmega:All(500)); Balance(b);
            Assert.That(b.Limited,Is.True); Assert.That(b.Current,Is.LessThanOrEqualTo(4+1e-9));
            Assert.That(output.All(x=>x>=RotorDynamics.Coast(p.Rotors[0],500,.01,0,1.225)-1e-9 && x<900),Is.True);
        }
        [Test] public void FaultedMotorCoastsWithoutDrawingPowerOrSpuriousMountTorque()
        {
            var p=Parameters(); var b=new PowerSystem(p); var drive=new RotorDriveState(4); drive.Set(0,0);
            var output=new double[4]; b.Resolve(All(600),output,.01,true,drive:drive,previousOmega:All(500)); Balance(b);
            Assert.That(output[0],Is.InRange(1.0,499.999)); Assert.That(b.RotorCurrentA[0],Is.Zero);
            Assert.That(b.RotorTorqueNm[0],Is.Zero); Assert.That(b.RotorAccelerationTorqueNm[0],Is.LessThan(0));
            Assert.That(output[1],Is.EqualTo(600));
        }
        [Test] public void NoActiveBrakingMeansZeroRequestCannotStopRotorInstantly()
        {
            var p=Parameters(); var b=new PowerSystem(p); var output=All(0);
            b.Resolve(output,output,.01,true,previousOmega:All(500)); Balance(b);
            Assert.That(b.Current,Is.Zero); Assert.That(output[0],Is.GreaterThan(0));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void PassiveQuadraticCoastConvergesToIndependentAnalyticSolution(double dt)
        {
            var p=Parameters(); var r=p.Rotors[0]; double omega=500;
            for(int i=0;i<(int)(1/dt);i++) omega=RotorDynamics.Coast(r,omega,dt,0,1.225);
            double analytic=500/(1+r.KQ*500/r.RotatingInertia);
            Assert.That(omega,Is.EqualTo(analytic).Within(.05));
        }
        [Test] public void CumulativeElectricalMechanicalAndChargeBalancesIncludeReset()
        {
            var p=Parameters(); var b=new PowerSystem(p); var omega=All(0); double prop=0,numerical=0,motorLoss=0,escLoss=0;
            const double dt=.01;
            for(int step=0;step<200;step++)
            {
                var requested=All(step<100 ? 500:0);
                b.Resolve(requested,requested,dt,step<100,previousOmega:omega);
                prop+=b.PropellerPower*dt; numerical+=b.SpinBalanceErrorPower*dt;
                motorLoss+=b.MotorLossPower*dt; escLoss+=b.EscLossPower*dt; Balance(b);
                b.Commit(dt); omega=requested;
            }
            Assert.That(b.TerminalEnergyJ,Is.EqualTo(prop+numerical+b.RotorSpinEnergyJ.Sum()+motorLoss+escLoss).Within(1e-6));
            Assert.That(b.ChemicalEnergyJ,Is.GreaterThanOrEqualTo(b.TerminalEnergyJ));
            Assert.That(b.Soc,Is.EqualTo(1-b.ConsumedAh*3600/p.Battery.CapacityC).Within(1e-12));
            b.Reset(); Assert.That(b.RotorSpinEnergyJ.Sum()+b.TerminalEnergyJ+b.SpinBalanceErrorPower,Is.Zero);
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void MotorLagBatteryAndSpinInertiaReachCommandedSteadySpeed(double dt)
        {
            var p=Parameters(); var b=new PowerSystem(p); var omega=All(0);
            for(int step=0;step<(int)(3/dt);step++)
            {
                var requested=omega.Select(x=>PhysicsMath.MotorStep(x,500,.06,dt)).ToArray();
                b.Resolve(requested,requested,dt,true,previousOmega:omega); Balance(b); b.Commit(dt); omega=requested;
            }
            Assert.That(omega[0],Is.EqualTo(500).Within(.01)); Assert.That(b.Soc,Is.InRange(.95,1.0));
        }
        [TestCase(-.5)] [TestCase(0)] [TestCase(.5)]
        public void BatteryUsesActualAdvanceRatioForAerodynamicLoad(double advance)
        {
            var p=Parameters(name:"quad_test_coupled_power_map"); var b=new PowerSystem(p); var omega=All(500);
            double axial=advance*(500/(2*Math.PI))*.127;
            b.Resolve(omega,omega,.01,true,axialVelocity:All(axial),previousOmega:All(500)); Balance(b);
            double q=p.Rotors[0].Performance.Evaluate(500,axial).Torque;
            Assert.That(b.PropellerPower,Is.EqualTo(4*q*500).Within(1e-9));
            Assert.That(b.RotorTorqueNm[0],Is.EqualTo(q).Within(1e-10));
        }
        [Test] public void LegacySimpleAndElectricalGovernorsAlsoUseLocalMapFlow()
        {
            foreach(var mode in new[]{"Simple","Electrical"})
            {
                var p=Parameters(j=> { j["powerSystem"]["battery"]["mode"]=mode; j["physicsConfiguration"]["modules"]["motorElectrical"]=mode=="Electrical";
                    j["physicsConfiguration"]["modules"]["gyroscopicRotorEffects"]=false;
                    foreach(var r in j["rotors"]) r["motor"]["dynamicsModel"]="FirstOrder"; },"quad_test_coupled_power_map");
                var b=new PowerSystem(p); var output=new double[4];
                b.Resolve(All(500),output,.01,true,axialVelocity:All(5));
                double q=p.Rotors[0].Performance.Evaluate(output[0],5).Torque;
                Assert.That(b.MechanicalPower,Is.EqualTo(4*q*output[0]).Within(1e-8));
            }
        }
        [Test] public void OversizedCommandsAndMapClampingRemainFiniteAtLowSoc()
        {
            var p=Parameters(j=> { j["powerSystem"]["battery"]["initialSoc"]=.00001; },"quad_test_coupled_power_map");
            var b=new PowerSystem(p); var output=new double[4];
            b.Resolve(All(1000),output,.02,true,axialVelocity:All(100),previousOmega:All(500)); Balance(b); b.Commit(.02);
            Assert.That(b.Soc,Is.GreaterThanOrEqualTo(0)); Assert.That(b.Current,Is.LessThanOrEqualTo(.00001*p.Battery.CapacityC/.02+1e-8));
            Assert.That(output.All(x=>!double.IsNaN(x) && x>=0),Is.True);
        }
        [Test] public void SymmetricOppositeSpinsCancelGyroscopeMoment()
        {
            var p=Parameters(); Assert.That(RotorDynamics.Momentum(p,All(500)).Length,Is.LessThan(1e-12));
            Assert.That(RotorDynamics.GyroscopicMoment(p,All(500),new DVector3(1,2,3)).Length,Is.LessThan(1e-12));
        }
        [TestCase("CW",-1)] [TestCase("CCW",1)]
        public void GyroscopicMomentHasCorrectSpinSignAndDoesNoInstantaneousWork(string spin,double sign)
        {
            var p=Parameters(j=>j["rotors"][0]["geometry"]["spinDirection"]=spin);
            var rate=new DVector3(1,2,3); var omega=new[]{500.0,0,0,0};
            var moment=RotorDynamics.GyroscopicMoment(p,omega,rate);
            var expected=new DVector3(-3,0,1)*(.000005*500*sign);
            Assert.That((moment-expected).Length,Is.LessThan(1e-12));
            Assert.That(DVector3.Dot(moment,rate),Is.EqualTo(0).Within(1e-12));
        }
        [Test] public void DisabledGyroDoesNotDisablePhysicalSpinEnergyOrAccelerationReaction()
        {
            var p=Parameters(j=>j["physicsConfiguration"]["modules"]["gyroscopicRotorEffects"]=false);
            Assert.That(RotorDynamics.GyroscopicMoment(p,All(500),new DVector3(1,2,3)).Length,Is.Zero);
            var b=new PowerSystem(p); var output=new double[4]; b.Resolve(All(600),output,.01,true,previousOmega:All(500));
            Assert.That(b.SpinEnergyChangePower,Is.GreaterThan(0)); Assert.That(b.RotorAccelerationTorqueNm[0],Is.GreaterThan(0));
        }
        [TestCase("inertia")] [TestCase("mixed")] [TestCase("simple")] [TestCase("noResponse")] [TestCase("oversized")]
        public void InvalidDynamicConfigurationIsRejected(string scenario)
        {
            var result=Load(j=> {
                if(scenario=="inertia") ((JObject)j["rotors"][0]["motor"]).Remove("rotatingInertiaKgM2");
                if(scenario=="mixed") j["rotors"][0]["motor"]["dynamicsModel"]="FirstOrder";
                if(scenario=="simple") j["powerSystem"]["battery"]["mode"]="Simple";
                if(scenario=="noResponse") j["physicsConfiguration"]["modules"]["motorResponse"]=false;
                if(scenario=="oversized") j["rotors"][0]["motor"]["rotatingInertiaKgM2"]=.01;
            }); Assert.That(result.Success,Is.False);
        }
        [TestCase("negative")] [TestCase("reject")] [TestCase("rpmSlope")] [TestCase("jSlope")]
        public void UnsupportedMapEnvelopeIsRejected(string scenario)
        {
            var result=Load(j=> {
                var p=j["rotors"][0]["performance"];
                if(scenario=="negative") p["performanceMap"][0]["cq"]=-.1;
                if(scenario=="reject") p["outOfRangePolicy"]="Reject";
                if(scenario=="rpmSlope") foreach(var row in p["performanceMap"]) row["cq"]=(double)row["rpm"]==2500 ? 10:.001;
                if(scenario=="jSlope") foreach(var row in p["performanceMap"]) { if((double)row["advanceRatio"]==-.5) row["advanceRatio"]=1; row["cq"]=(double)row["advanceRatio"]==1 ? 100:.001; }
            },"quad_test_coupled_power_map"); Assert.That(result.Success,Is.False);
        }
        [Test] public void QueriesRequirePreviousSpeedsAndFinitePerRotorFlow()
        {
            var b=new PowerSystem(Parameters()); var output=new double[4];
            Assert.Throws<ArgumentException>(()=>b.Resolve(All(500),output,.01,true));
            Assert.Throws<ArgumentException>(()=>b.Resolve(All(500),output,.01,true,axialVelocity:new[]{1.0},previousOmega:All(0)));
            Assert.Throws<ArgumentException>(()=>b.Resolve(All(500),output,.01,true,axialVelocity:All(double.NaN),previousOmega:All(0)));
        }
        [Test] public void PreviousAndOutputArraysMayAliasWithoutChangingCalculation()
        {
            var p=Parameters(); var a=new PowerSystem(p); var b=new PowerSystem(p); var output=new double[4]; var aliased=All(500);
            a.Resolve(All(600),output,.01,true,previousOmega:All(500));
            b.Resolve(All(600),aliased,.01,true,previousOmega:aliased);
            Assert.That(aliased,Is.EqualTo(output)); Assert.That(b.ElectricalPower,Is.EqualTo(a.ElectricalPower));
        }
        [Test] public void RuntimeInertiaSnapshotAndCsvRemainIndependentOfMutableProfile()
        {
            var p=Load(); p.Drone.rotors[0].motor.rotatingInertiaKgM2=1;
            Assert.That(p.Parameters.Rotors[0].RotatingInertia,Is.EqualTo(.000005));
            var writer=new StringWriter(); var csv=new FlightCsvWriter(writer,new[]{"FL"});
            csv.Write(new FlightTelemetryFrame {Rotors=new[]{new RotorTelemetry(0,1,5000,2.5,0,0,null,null,null,1,0,default,default,false,spinEnergy:.5)},PropellerPower=2,SpinBalanceError=.1});
            var rows=writer.ToString().Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries); var headers=rows[0].Split(','); var cells=rows[1].Split(',');
            Assert.That(cells.Length,Is.EqualTo(headers.Length)); Assert.That(cells[Array.IndexOf(headers,"rotor_0_FL_spin_energy_j")],Is.EqualTo("0.5"));
            Assert.That(cells[Array.IndexOf(headers,"propeller_power_w")],Is.EqualTo("2"));
        }
        [Test] public void VeryStiffPassiveStopStillBalancesEnergyAndProducesNoRegeneration()
        {
            var p=Parameters(j=> { foreach(var rotor in j["rotors"]) rotor["motor"]["rotatingInertiaKgM2"]=1e-10; });
            var b=new PowerSystem(p); var output=new double[4]; b.Resolve(All(0),output,.02,false,previousOmega:All(500)); Balance(b);
            Assert.That(output,Is.EqualTo(All(0))); Assert.That(b.Current,Is.Zero);
            Assert.That(b.PropellerPower*.02,Is.EqualTo(4*.5*1e-10*500*500).Within(1e-12));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void RatePidTracksYawWithInertialReactionPowerAndRotorDrag(double dt)
        {
            var p=Parameters(); var control=new FlightController(); var allocator=new QuadAllocator(p); var power=new PowerSystem(p);
            var omega=All(p.Rotors[0].MaxOmega/2); var commands=new double[4]; double rate=0,desired=0; bool saturated=false;
            for(int step=0;step<(int)(15/dt);step++)
            {
                desired=PilotMath.SmoothCommand(desired,step<(int)(10/dt) ? 70*Math.PI/180:0,.12,dt);
                var acceleration=control.RateAcceleration(new DVector3(0,desired,0),new DVector3(0,rate,0),dt,12,new PidTerms(2,.15,3),40,!saturated);
                saturated=allocator.Allocate(p.Mass*p.Gravity,acceleration*p.Inertia.Y,commands);
                var requested=new double[4];
                for(int i=0;i<4;i++) { var r=p.Rotors[i]; double target=commands[i]*r.MaxOmega; requested[i]=PhysicsMath.MotorStep(omega[i],target,target>omega[i] ? r.TauUp:r.TauDown,dt); }
                power.Resolve(requested,requested,dt,true,previousOmega:omega); power.Commit(dt);
                double torque=0; for(int i=0;i<4;i++) torque+=p.Rotors[i].ReactionSign*power.RotorTorqueNm[i];
                torque+=RotorAerodynamics.DragWrench(p,requested,default,new DVector3(0,rate,0)).Torque.Y;
                rate+=torque/p.Inertia.Y*dt; omega=requested;
                if(step==(int)(10/dt)-1) Assert.That(rate*180/Math.PI,Is.EqualTo(70).Within(1));
            }
            Assert.That(Math.Abs(rate),Is.LessThan(.03));
        }
    }
}
