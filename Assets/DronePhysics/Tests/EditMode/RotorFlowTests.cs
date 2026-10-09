using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class RotorFlowTests
    {
        private static string Read(string name)=>ProfileTestFiles.Read(name);
        private static ProfileLoadResult Load(Action<JObject> edit=null,string name="quad_test_advanced_rotors")
        {
            var j=JObject.Parse(Read(name)); edit?.Invoke(j);
            return ProfileLoader.Load(j.ToString(),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema"));
        }
        private static RuntimeRotorParameters Rotor(Action<JObject> edit=null)
        { var p=Load(edit); Assert.That(p.Success,Is.True,string.Join("\n",p.Issues)); return p.Parameters.Rotors[0]; }
        private static RotorFlowSample Sample(DVector3 v,double omega=500,double thrust=2.5,double density=1.225,Action<JObject> edit=null)
            =>RotorFlow.Evaluate(Rotor(edit),omega,thrust,v,new DVector3(0,1,0),density);
        [Test] public void LocalFlowMatchesPrimaryUnboundedFormulaInsideEnvelope()
        {
            var s=Sample(new DVector3(3,2,4));
            Assert.That(s.ThrustCorrection,Is.EqualTo(.002*25-.0001*500*2).Within(1e-12));
            Assert.That((s.FlappingMoment-new DVector3(.01,0,-.0075)).Length,Is.LessThan(1e-12));
            Assert.That(s.Clamped,Is.False);
        }
        [TestCase(-2,.1)] [TestCase(0,0)] [TestCase(2,-.1)]
        public void AxialCorrectionHasDissipativeSignWithoutLift(double velocity,double expected)
        {
            var s=Sample(new DVector3(0,velocity,0));
            Assert.That(s.ThrustCorrection,Is.EqualTo(expected).Within(1e-12));
            Assert.That(s.ThrustCorrection*velocity,Is.LessThanOrEqualTo(0)); Assert.That(s.FlappingMoment.Length,Is.Zero);
        }
        [TestCase(3)] [TestCase(-3)]
        public void LiftUsesOnlySquaredLateralSpeedAndFlapOpposesForwardTilting(double speed)
        {
            var s=Sample(new DVector3(0,0,speed));
            Assert.That(s.ThrustCorrection,Is.EqualTo(.002*speed*speed).Within(1e-12));
            Assert.That(s.FlappingMoment.X,Is.EqualTo(.000005*500*speed).Within(1e-12));
        }
        [Test] public void CoefficientsScaleWithDensityOnceAndIgnoreSpinDirection()
        {
            var v=new DVector3(3,2,4); var a=Sample(v); var b=Sample(v,density:.6125);
            Assert.That(b.ThrustCorrection,Is.EqualTo(a.ThrustCorrection/2));
            Assert.That((b.FlappingMoment-a.FlappingMoment/2).Length,Is.LessThan(1e-12));
            var reverse=Sample(v,edit:j=>j["rotors"][0]["geometry"]["spinDirection"]="CCW");
            Assert.That(reverse.ThrustCorrection,Is.EqualTo(a.ThrustCorrection));
            Assert.That((reverse.FlappingMoment-a.FlappingMoment).Length,Is.Zero);
        }
        [TestCase(0,2.5)] [TestCase(500,0)] [TestCase(500,-2)]
        public void StoppedOrWindmillingRotorNeverReceivesExtraLiftOrMoment(double omega,double thrust)
        {
            var s=Sample(new DVector3(1e6,-1e6,1e6),omega,thrust);
            Assert.That(s.ThrustCorrection,Is.Zero); Assert.That(s.FlappingMoment.Length,Is.Zero);
        }
        [Test] public void CorrectionsVanishContinuouslyAsRotorSlows()
        {
            var r=Rotor();
            foreach(var omega in new[]{100.0,10,1,.1,.01})
            {
                double t=r.Performance.Evaluate(omega).Thrust;
                var s=RotorFlow.Evaluate(r,omega,t,new DVector3(5,-5,5),r.Axis,1.225);
                Assert.That(Math.Abs(s.ThrustCorrection),Is.LessThanOrEqualTo(.25*t));
                Assert.That(s.FlappingMoment.Length,Is.LessThanOrEqualTo(.2*t*r.Diameter/2+1e-15));
            }
        }
        [Test] public void CombinedCorrectionsAndMomentsRespectSeparateCaps()
        {
            var s=Sample(new DVector3(1e6,-1e6,1e6),edit:j=> {
                foreach(var r in j["rotors"]) { r["advancedAerodynamics"]["bladeFlappingCoefficient"]=1; r["advancedAerodynamics"]["translationalLiftCoefficientKgPerM"]=1; }
            });
            Assert.That(s.Clamped,Is.True); Assert.That(s.ThrustCorrection,Is.EqualTo(.625));
            Assert.That(s.FlappingMoment.Length,Is.EqualTo(.2*2.5*.0635).Within(1e-12));
            Assert.That(DVector3.Dot(s.FlappingMoment,new DVector3(0,1,0)),Is.Zero);
        }
        [Test] public void FlowClipPreservesDirectionAndIsContinuousAtBoundary()
        {
            Action<JObject> bounds=j=>j["rotors"][0]["advancedAerodynamics"]["maxFlappingMomentRatio"]=1;
            var a=Sample(new DVector3(0,0,15),edit:bounds); var b=Sample(new DVector3(0,0,15000),edit:bounds);
            Assert.That(a.Clamped,Is.False); Assert.That(b.Clamped,Is.True);
            Assert.That(b.ThrustCorrection,Is.EqualTo(a.ThrustCorrection));
            Assert.That((a.FlappingMoment-b.FlappingMoment).Length,Is.LessThan(1e-12));
            Assert.That(Math.Abs(Sample(new DVector3(0,0,15-1e-8)).ThrustCorrection-b.ThrustCorrection),Is.LessThan(1e-8));
        }
        [Test] public void ArbitraryRotorAxisUsesSameInvariantEquations()
        {
            var r=Rotor(j=>j["rotors"][0]["geometry"]["thrustAxisLocal"]=new JArray(1,0,0));
            var s=RotorFlow.Evaluate(r,500,2.5,new DVector3(2,3,4),r.Axis,1.225);
            Assert.That(s.ThrustCorrection,Is.EqualTo(-.05).Within(1e-12));
            Assert.That((s.FlappingMoment-new DVector3(0,-.01,.0075)).Length,Is.LessThan(1e-12));
        }
        [Test] public void DisabledFlagsAndStoredCoefficientsDoNotChangeLegacyBehavior()
        {
            var r=Rotor(j=> { var m=j["physicsConfiguration"]["modules"]; m["rotorAerodynamics"]=false; m["bladeFlapping"]=false; m["inducedDrag"]=false; });
            Assert.That(r.Flow,Is.Null);
            var s=RotorFlow.Evaluate(r,500,2.5,new DVector3(3,2,4),r.Axis,1.225);
            Assert.That(s.ThrustCorrection+s.FlappingMoment.Length,Is.Zero);
            Assert.That(Load(name:"quad_test_basic").Parameters.Rotors[0].Flow,Is.Null);
        }
        [Test] public void RuntimeSnapshotIsIndependentOfMutableProfile()
        {
            var p=Load(); p.Drone.rotors[0].advancedAerodynamics.inducedDragCoefficient=1;
            Assert.That(p.Parameters.Rotors[0].Flow.AxialCoefficient,Is.EqualTo(.0001));
        }
        [TestCase("referenceAirDensityKgM3")] [TestCase("maxAirSpeedMps")] [TestCase("bladeFlappingCoefficient")]
        [TestCase("inducedDragCoefficient")] [TestCase("maxThrustCorrectionFraction")] [TestCase("maxFlappingMomentRatio")]
        public void EnabledEffectsRequireExplicitCoefficientsAndEnvelope(string field)
            =>Assert.That(Load(j=>((JObject)j["rotors"][0]["advancedAerodynamics"]).Remove(field)).Success,Is.False);
        [TestCase("maxThrustCorrectionFraction",.51)] [TestCase("maxFlappingMomentRatio",1.01)]
        [TestCase("maxAirSpeedMps",0)] [TestCase("inducedDragCoefficient",-1)] [TestCase("referenceAirDensityKgM3",1e-300)]
        public void InvalidBoundsAreRejected(string field,double value)
            =>Assert.That(Load(j=>j["rotors"][0]["advancedAerodynamics"][field]=value).Success,Is.False);
        [TestCase(true,false,false)] [TestCase(false,true,false)] [TestCase(false,false,true)]
        public void EachEffectCanBeEnabledIndependently(bool flap,bool axial,bool lift)
        {
            var r=Rotor(j=> { var m=j["physicsConfiguration"]["modules"]; m["rotorAerodynamics"]=lift; m["bladeFlapping"]=flap; m["inducedDrag"]=axial; });
            var s=RotorFlow.Evaluate(r,500,2.5,new DVector3(3,2,4),r.Axis,1.225);
            Assert.That(s.ThrustCorrection,Is.EqualTo((lift ? .05:0)-(axial ? .1:0)).Within(1e-12));
            Assert.That(s.FlappingMoment.Length>0,Is.EqualTo(flap));
        }
        [Test] public void AxialAndLiftBundleCannotDoubleCountRpmJMapButFlappingCan()
        {
            Action<JObject> settings=j=> { foreach(var r in j["rotors"]) r["advancedAerodynamics"]=JObject.Parse(Read("quad_test_advanced_rotors"))["rotors"][0]["advancedAerodynamics"].DeepClone(); };
            Assert.That(Load(j=> { settings(j); j["physicsConfiguration"]["modules"]["inducedDrag"]=true; },"quad_test_performance_map").Success,Is.False);
            Assert.That(Load(j=> { settings(j); j["physicsConfiguration"]["modules"]["rotorAerodynamics"]=true; },"quad_test_performance_map").Success,Is.False);
            Assert.That(Load(j=> { settings(j); j["physicsConfiguration"]["modules"]["bladeFlapping"]=true; },"quad_test_performance_map").Success,Is.True);
        }
        [Test] public void InvalidQueriesAreRejected()
        {
            var r=Rotor(); var y=new DVector3(0,1,0);
            Assert.Throws<ArgumentOutOfRangeException>(()=>RotorFlow.Evaluate(r,-1,2.5,default,y,1.225));
            Assert.Throws<ArgumentOutOfRangeException>(()=>RotorFlow.Evaluate(r,500,2.5,default,default,1.225));
            Assert.Throws<ArgumentOutOfRangeException>(()=>RotorFlow.Evaluate(r,500,2.5,default,y,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>RotorFlow.Evaluate(r,500,2.5,new DVector3(double.NaN,0,0),y,1.225));
        }
        [Test] public void NewTelemetryColumnsContainActualCorrectionMomentAndClipFlag()
        {
            var w=new StringWriter(); var csv=new FlightCsvWriter(w,new[]{"FL"});
            csv.Write(new FlightTelemetryFrame {Rotors=new[]{new RotorTelemetry(0,1,5000,2.5,0,0,null,null,null,1,0,default,default,false,.12,new DVector3(.01,0,-.02),true)}});
            var lines=w.ToString().Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
            var header=lines[0].Split(','); var cells=lines[1].Split(',');
            Assert.That(cells.Length,Is.EqualTo(header.Length));
            Assert.That(cells[Array.IndexOf(header,"rotor_0_FL_thrust_correction_n")],Is.EqualTo("0.12"));
            Assert.That(cells[Array.IndexOf(header,"rotor_0_FL_flap_z_nm")],Is.EqualTo("-0.02"));
            Assert.That(cells[Array.IndexOf(header,"rotor_0_FL_flow_clamped")],Is.EqualTo("1"));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void RateControllerCountersLocalFlappingAndForceMomentsAtThreeTimesteps(double dt)
        {
            var result=Load(j=>j["massProperties"]["inertia"]["principalAxesRotationXyzw"]=new JArray(0,0,0,1));
            var p=result.Parameters; var controller=new FlightController(); var allocator=new QuadAllocator(p);
            var omega=new[]{500.0,500,500,500}; var commands=new double[4]; double rate=0;
            for(int step=0;step<(int)(15/dt);step++)
            {
                var acceleration=controller.RateAcceleration(default,new DVector3(rate,0,0),dt,12,new PidTerms(2,.15,3),40,true);
                allocator.Allocate(p.Mass*p.Gravity,new DVector3(acceleration.X*p.Inertia.X,0,0),commands);
                double torque=0;
                for(int i=0;i<4;i++)
                {
                    var r=p.Rotors[i]; double target=commands[i]*r.MaxOmega;
                    omega[i]=PhysicsMath.MotorStep(omega[i],target,target>omega[i] ? r.TauUp:r.TauDown,dt);
                    var arm=r.Position-p.CenterOfMass;
                    var air=new DVector3(0,0,3)+DVector3.Cross(new DVector3(rate,0,0),arm);
                    double thrust=r.Performance.Evaluate(omega[i]).Thrust;
                    var flow=RotorFlow.Evaluate(r,omega[i],thrust,air,r.Axis,1.225);
                    var force=r.Axis*(thrust+flow.ThrustCorrection)+RotorAerodynamics.Drag(air,r.Axis,omega[i],r.RotorDragCoefficient);
                    torque+=DVector3.Cross(arm,force).X+flow.FlappingMoment.X;
                }
                rate+=torque/p.Inertia.X*dt;
            }
            Assert.That(Math.Abs(rate),Is.LessThan(.025));
            Assert.That(Math.Abs(omega[0]-omega[3]),Is.GreaterThan(1));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void HeightControllerTrimsLiftInflowGroundGainAndBatteryTogether(double dt)
        {
            var p=Load().Parameters; var controller=new FlightController(); var power=new PowerSystem(p);
            var omega=new[]{500.0,500,500,500}; double height=.1,velocity=0;
            for(int step=0;step<(int)(15/dt);step++)
            {
                double acceleration=controller.ClimbAcceleration(.1-height,velocity,dt,3,new PidTerms(.1,0,.5),3,new PidTerms(.8,.1,2),2,4,true);
                double target=p.Rotors[0].MaxOmega*Math.Sqrt(p.Mass*(p.Gravity+acceleration)/p.MaxTotalThrust);
                for(int i=0;i<4;i++) omega[i]=PhysicsMath.MotorStep(omega[i],target,target>omega[i] ? p.Rotors[i].TauUp:p.Rotors[i].TauDown,dt);
                power.Resolve(omega,omega,dt,true); power.Commit(dt);
                double total=0;
                for(int i=0;i<4;i++)
                {
                    var r=p.Rotors[i]; double thrust=r.Performance.Evaluate(omega[i]).Thrust;
                    var flow=RotorFlow.Evaluate(r,omega[i],thrust,new DVector3(3,velocity,0),r.Axis,1.225);
                    double gain=RotorAerodynamics.GroundMultiplier(p.GroundEffect,r.Diameter/2,height);
                    total+=RotorAerodynamics.ThrustWithGroundEffect(thrust+flow.ThrustCorrection,gain);
                }
                velocity+=(total/p.Mass-p.Gravity)*dt; height+=velocity*dt;
            }
            Assert.That(height,Is.EqualTo(.1).Within(.005)); Assert.That(Math.Abs(velocity),Is.LessThan(.005));
            Assert.That(power.Soc,Is.InRange(.9,1.0)); Assert.That(power.Current,Is.GreaterThan(0));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void AxialDynamicsConvergeToIndependentLinearDecay(double dt)
        {
            var r=Rotor(j=> { j["physicsConfiguration"]["modules"]["bladeFlapping"]=false; j["physicsConfiguration"]["modules"]["rotorAerodynamics"]=false; });
            double velocity=1,omega=500,thrust=2.5;
            for(int i=0;i<(int)(2/dt);i++) velocity+=4*RotorFlow.Evaluate(r,omega,thrust,new DVector3(0,velocity,0),r.Axis,1.225).ThrustCorrection*dt;
            Assert.That(velocity,Is.EqualTo(Math.Exp(-4*.0001*omega*2)).Within(.001));
        }
    }
}
