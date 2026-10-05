using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class EnvironmentTests
    {
        private static string Read(string name) => ProfileTestFiles.Read(name);
        private static ProfileLoadResult Load(string env="environment_turbulence",string drone="quad_test_ctcq",Action<JObject> edit=null,Action<JObject> editDrone=null)
        {
            var e=JObject.Parse(Read(env)); var d=JObject.Parse(Read(drone)); edit?.Invoke(e); editDrone?.Invoke(d);
            return ProfileLoader.Load(d.ToString(),e.ToString(),Read("drone-profile.schema"),Read("environment-profile.schema"));
        }
        private static RuntimeDroneParameters Parameters(string env="environment_turbulence",string drone="quad_test_ctcq",Action<JObject> edit=null,Action<JObject> editDrone=null)
        { var r=Load(env,drone,edit,editDrone); Assert.That(r.Success,Is.True,string.Join("\n",r.Issues)); return r.Parameters; }
        [TestCase(0,288.15,101325,1.225)] [TestCase(3000,268.65,70108.5,.909122)] [TestCase(11000,216.65,22632.04,.363918)]
        public void TroposphereMatchesStandardReferenceValues(double altitude,double temperature,double pressure,double density)
        {
            var a=Atmosphere.Troposphere(altitude); Assert.That(a.TemperatureK,Is.EqualTo(temperature).Within(1e-10));
            Assert.That(a.PressurePa,Is.EqualTo(pressure).Within(.1)); Assert.That(a.Density,Is.EqualTo(density).Within(1e-5));
            Assert.That(a.PressurePa,Is.EqualTo(a.Density*Atmosphere.GasConstant*a.TemperatureK).Within(1e-10));
        }
        [TestCase(-501)] [TestCase(11001)] [TestCase(double.NaN)]
        public void AtmosphereOutOfDomainRejected(double altitude)=>Assert.Throws<ArgumentOutOfRangeException>(()=>Atmosphere.Troposphere(altitude));
        [Test] public void AtmosphereDensityDecreasesContinuouslyAndDefaultsIgnoreConstantDensityCache()
        {
            var p=Parameters("environment_atmosphere",edit:e=>e["airDensityKgM3"]=99); double previous=double.MaxValue;
            for(int h=-500;h<=11000;h+=50) { var a=p.Environment.SampleAir(h); Assert.That(a.Density,Is.LessThan(previous)); previous=a.Density; }
            Assert.That(p.Density,Is.EqualTo(1.225).Within(1e-5));
            p=Parameters("environment_atmosphere",edit:e=> { e.Remove("temperatureK"); e.Remove("pressurePa"); e.Remove("altitudeM"); });
            Assert.That(p.Density,Is.EqualTo(1.225).Within(1e-5));
        }
        [TestCase("quad_test_basic")] [TestCase("quad_test_rpm_table")]
        public void VariableAtmosphereRejectsMeasuredDensityTiedRotors(string drone)
        {
            var r=Load("environment_atmosphere",drone); Assert.That(r.Success,Is.False);
            Assert.That(r.Issues.Any(x=>x.Message.Contains("Variable atmosphere requires")),Is.True);
        }
        [TestCase("quad_test_ctcq")] [TestCase("quad_test_performance_map")]
        public void VariableDensityChangesRotorThrustAndTorqueOnceButNotAdvanceRatio(string drone)
        {
            var p=Parameters("environment_atmosphere",drone); var r=p.Rotors[0]; double omega=PhysicsMath.RpmToOmega(5000);
            var a=r.Performance.Evaluate(omega,1,p.Density); var b=r.Performance.Evaluate(omega,1,p.Density*.7);
            Assert.That(b.Thrust,Is.EqualTo(a.Thrust*.7).Within(1e-10)); Assert.That(b.Torque,Is.EqualTo(a.Torque*.7).Within(1e-10));
            Assert.That(b.AdvanceRatio,Is.EqualTo(a.AdvanceRatio));
        }
        [TestCase("quad_test_basic")] [TestCase("quad_test_rpm_table")]
        public void DirectQueryCannotSilentlyScaleMeasuredRotorData(string drone)
        {
            var p=Parameters("environment_calm",drone);
            Assert.Throws<ArgumentOutOfRangeException>(()=>p.Rotors[0].Performance.Evaluate(500,density:p.Density*.5));
        }
        [TestCase("quad_test_basic")] [TestCase("quad_test_projected_box")] [TestCase("quad_test_surfaces")]
        public void BodyPointWrenchScalesWithDensityAndIncludesExactlyOneLeverArm(string drone)
        {
            var p=Parameters("environment_calm",drone); var velocity=new DVector3(3,4,5);
            var a=BodyAerodynamics.EvaluatePoint(p,velocity,p.Density); var b=BodyAerodynamics.EvaluatePoint(p,velocity,p.Density*.5);
            Assert.That((b.Force-a.Force*.5).Length,Is.LessThan(1e-12)); Assert.That((b.Torque-a.Torque*.5).Length,Is.LessThan(1e-12));
            Assert.That(DVector3.Dot(a.Force,velocity),Is.LessThanOrEqualTo(0));
        }
        [Test] public void NoneConstantAndDisabledWindPreserveOldBehavior()
        {
            var p=Parameters("environment_calm"); Assert.That(p.Environment.Sample(new DVector3(100,200,300),99).Length,Is.Zero);
            p=Parameters("environment_wind"); Assert.That((p.Environment.Sample(default,99)-new DVector3(5,0,0)).Length,Is.Zero);
            p=Parameters(editDrone:d=>d["physicsConfiguration"]["modules"]["windInteraction"]=false);
            Assert.That(p.Environment.Sample(new DVector3(1,2,3),1).Length,Is.Zero);
        }
        [TestCase(0,5)] [TestCase(.5,6)] [TestCase(1,7)] [TestCase(2,5)]
        public void GustUsesRaisedCosineWithConfiguredPeakAndPeriod(double time,double expected)
        {
            var wind=Parameters("environment_gust").Environment.Sample(default,time);
            Assert.That(wind.X,Is.EqualTo(expected).Within(1e-12)); Assert.That(wind.Y+wind.Z,Is.Zero);
        }
        [Test] public void GustFollowsMeanWindAndFallsBackToXAtZeroMean()
        {
            var p=Parameters("environment_gust",edit:e=>e["windVelocityWorldMps"]=new JArray(0,0,-5));
            Assert.That((p.Environment.Sample(default,1)-new DVector3(0,0,-7)).Length,Is.LessThan(1e-12));
            p=Parameters("environment_gust",edit:e=>e["windVelocityWorldMps"]=new JArray(0,0,0));
            Assert.That(p.Environment.Sample(default,1).X,Is.EqualTo(2));
        }
        [Test] public void TurbulenceIsSeededQueryOrderIndependentAndSpatiallyVarying()
        {
            var a=Parameters().Environment; var b=Parameters().Environment; var c=Parameters(edit:e=>e["turbulenceSeed"]=99).Environment;
            var point=new DVector3(1,2,3); var first=a.Sample(point,3);
            for(int i=0;i<100;i++) a.Sample(new DVector3(i,-i,i),i);
            Assert.That((a.Sample(point,3)-first).Length,Is.Zero); Assert.That((b.Sample(point,3)-first).Length,Is.Zero);
            Assert.That((c.Sample(point,3)-first).Length,Is.GreaterThan(.01));
            Assert.That((a.Sample(point+new DVector3(1,0,0),3)-first).Length,Is.GreaterThan(.001));
        }
        [Test] public void TurbulenceBoundContinuityAndFrozenAdvectionAgree()
        {
            var p=Parameters().Environment; var rng=new Random(178);
            for(int i=0;i<500;i++)
            {
                var pos=new DVector3(rng.NextDouble()*100,rng.NextDouble()*100,rng.NextDouble()*100); double time=rng.NextDouble()*100;
                var a=p.Sample(pos,time); Assert.That((a-p.MeanWind).Length,Is.LessThanOrEqualTo(2+1e-12));
                Assert.That((p.Sample(pos,time+1e-6)-a).Length,Is.LessThan(1e-4));
                Assert.That((p.Sample(pos+p.MeanWind*3,time+3)-a).Length,Is.LessThan(1e-10));
            }
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void SameTimeSamplesDoNotDependOnStepSize(double dt)
        {
            var env=Parameters().Environment; double time=0; for(int k=0;k<(int)(2/dt);k++) time+=dt;
            Assert.That((env.Sample(new DVector3(2,3,4),time)-env.Sample(new DVector3(2,3,4),2)).Length,Is.LessThan(1e-10));
        }
        [Test] public void TurbulenceCanCombineWithPeriodicGustWithinCombinedBound()
        {
            var env=Parameters(edit:e=>e["gustEnabled"]=true).Environment;
            for(int i=0;i<100;i++) Assert.That((env.Sample(default,i*.1)-env.MeanWind).Length,Is.LessThanOrEqualTo(4+1e-12));
        }
        [Test] public void ZeroIntensityAndImmutableSnapshotKeepConfiguredMean()
        {
            var result=Load(edit:e=>e["gustIntensityMps"]=0); result.Environment.windVelocityWorldMps[0]=99;
            Assert.That(result.Parameters.Environment.Sample(default,1).X,Is.EqualTo(5));
        }
        [TestCase("gustIntensityMps")] [TestCase("gustTimeScaleS")] [TestCase("turbulenceSeed")]
        public void TurbulenceRequiresExplicitSettings(string key)=>Assert.That(Load(edit:e=>e.Remove(key)).Success,Is.False);
        [TestCase(.001)] [TestCase(1e7)]
        public void InvalidTimescaleRejected(double time)=>Assert.That(Load(edit:e=>e["gustTimeScaleS"]=time).Success,Is.False);
        [Test] public void InconsistentModesAndInvalidAtmosphereSettingsRejected()
        {
            Assert.That(Load("environment_gust",edit:e=>e["gustEnabled"]=false).Success,Is.False);
            Assert.That(Load("environment_field",edit:e=>e["gustEnabled"]=true).Success,Is.False);
            Assert.That(Load("environment_calm",edit:e=>e["gustEnabled"]=true).Success,Is.False);
            Assert.That(Load("environment_atmosphere",edit:e=>e["altitudeM"]=11001).Success,Is.False);
            Assert.That(Load("environment_atmosphere",edit:e=>e["temperatureK"]=100).Success,Is.False);
        }
        [Test] public void LinearFieldIsBoundedAndRequiresExplicitCustomProvider()
        {
            var field=new LinearWindField(new DVector3(5,0,0),default,default,new DVector3(.5,0,0),default,2);
            Assert.That(field.Sample(new DVector3(0,2,0),0).X,Is.EqualTo(6)); Assert.That(field.Sample(new DVector3(0,20,0),0).X,Is.EqualTo(7));
            var env=Parameters("environment_field").Environment; Assert.Throws<InvalidOperationException>(()=>env.Sample(default,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>field.Sample(new DVector3(double.NaN,0,0),0));
        }
        [Test] public void DynamicDensityFeedsPowerFromTheSameRotorTorque()
        {
            var p=Parameters("environment_atmosphere","quad_test_battery_simple",editDrone:d=> {
                var ct=JObject.Parse(Read("quad_test_ctcq")); for(int i=0;i<4;i++) d["rotors"][i]["performance"]=ct["rotors"][i]["performance"].DeepClone(); });
            var power=new PowerSystem(p); var speed=p.Rotors.Select(r=>r.MaxOmega*.5).ToArray(); double rho=p.Environment.SampleAir(3000).Density;
            power.Resolve(speed,speed,.01,true,rho);
            double expected=p.Rotors.Sum(r=>r.Performance.Evaluate(r.MaxOmega*.5,density:rho).Torque*r.MaxOmega*.5);
            Assert.That(power.MechanicalPower,Is.EqualTo(expected).Within(1e-10)); Assert.That(power.RpmScale,Is.EqualTo(1));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void TurbulentWindTranslationConvergesAtDifferentTimesteps(double dt)
        {
            var p=Parameters(); var env=p.Environment;
            DVector3 Run(double step)
            {
                DVector3 pos=default,v=default;
                for(int k=0;k<(int)(2/step);k++)
                {
                    var flow=v-env.Sample(pos,k*step);
                    var f=BodyAerodynamics.EvaluatePoint(p,flow,p.Density).Force;
                    v+=f*(step/p.Mass); pos+=v*step;
                }
                return pos;
            }
            Assert.That((Run(dt)-Run(.0025)).Length,Is.LessThan(.02));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void AltitudePidAllocatesAtActualDensityWithMotorLag(double dt)
        {
            var p=Parameters("environment_atmosphere"); var controller=new FlightController(); var allocator=new QuadAllocator(p);
            var omega=new double[4]; var commands=new double[4]; double h=0,v=0; bool saturated=false;
            for(int k=0;k<(int)(15/dt);k++)
            {
                double rho=p.Environment.SampleAir(3000+h).Density,scale=rho/p.Density;
                double accel=controller.ClimbAcceleration(-h,v,dt,3,new PidTerms(.1,0,.5),3,new PidTerms(.8,.1,2),2,4,!saturated);
                saturated=allocator.Allocate(p.Mass*(p.Gravity+accel)/scale,default,commands); double thrust=0;
                for(int i=0;i<4;i++)
                {
                    var r=p.Rotors[i]; double target=commands[i]*r.MaxOmega;
                    omega[i]=PhysicsMath.MotorStep(omega[i],target,target>omega[i] ? r.TauUp : r.TauDown,dt);
                    thrust+=r.Performance.Evaluate(omega[i],density:rho).Thrust;
                }
                v+=(thrust/p.Mass-p.Gravity)*dt; h+=v*dt;
            }
            Assert.That(Math.Abs(h),Is.LessThan(.025)); Assert.That(Math.Abs(v),Is.LessThan(.01));
            Assert.That(omega[0],Is.GreaterThan(PhysicsMath.RpmToOmega(5000)));
        }
    }
}
