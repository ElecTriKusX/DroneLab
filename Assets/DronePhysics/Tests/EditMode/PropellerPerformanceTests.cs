using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class PropellerPerformanceTests
    {
        private static string Read(string name)
        {
#if UNITY_EDITOR
            var folder=Path.Combine(UnityEngine.Application.dataPath,"DronePhysics/Resources/DronePhysics");
#else
            var folder=Path.Combine(TestContext.CurrentContext.TestDirectory,"Profiles");
#endif
            return File.ReadAllText(Path.Combine(folder,name+".json"));
        }
        private static ProfileLoadResult Load(string name,Action<JObject> edit=null,double density=1.225)
        {
            var p=JObject.Parse(Read(name)); edit?.Invoke(p);
            var env=JObject.Parse(Read("environment_calm")); env["airDensityKgM3"]=density;
            return ProfileLoader.Load(p.ToString(),env.ToString(),Read("drone-profile.schema"),Read("environment-profile.schema"));
        }
        private static RuntimeRotorParameters Rotor(string name)
        { var result=Load(name); Assert.That(result.Success,Is.True,string.Join("\n",result.Issues)); return result.Parameters.Rotors[0]; }
        private static PropellerSample At(RuntimeRotorParameters r,double rpm,double axial=0) => r.Performance.Evaluate(PhysicsMath.RpmToOmega(rpm),axial);
        [TestCase("quad_test_rpm_table")] [TestCase("quad_test_performance_map")]
        public void PresetsLoadAndStopAtZero(string name)
        {
            var r=Rotor(name); var s=At(r,0,100);
            Assert.That(s.Thrust,Is.Zero); Assert.That(s.Torque,Is.Zero); Assert.That(r.MaxThrust,Is.EqualTo(9.81).Within(1e-10));
        }
        [Test] public void TableReproducesKnotsAndInterpolatesForcesAndCurrentLinearly()
        {
            var r=Rotor("quad_test_rpm_table"); var knot=At(r,5000); var mid=At(r,6250);
            Assert.That(knot.Thrust,Is.EqualTo(2.4525).Within(1e-12)); Assert.That(knot.Torque,Is.EqualTo(.04905).Within(1e-12));
            Assert.That(mid.Thrust,Is.EqualTo((2.4525+5.518125)/2).Within(1e-12));
            Assert.That(mid.Torque,Is.EqualTo((.04905+.121)/2).Within(1e-12)); Assert.That(mid.Current,Is.EqualTo(5));
            Assert.That(mid.Clamped,Is.False);
        }
        [Test] public void TableOriginDoesNotProduceResidualForceWhenStopped()
        {
            var r=Rotor("quad_test_rpm_table"); Assert.That(At(r,1e-4).Thrust,Is.LessThan(1e-7)); Assert.That(At(r,0).Current,Is.EqualTo(0));
        }
        [Test] public void TableClampHoldsEndpointForcesRatherThanScalingThemAgain()
        {
            var r=Rotor("quad_test_rpm_table"); var s=At(r,20000);
            Assert.That(s.Thrust,Is.EqualTo(9.81)); Assert.That(s.Torque,Is.EqualTo(.25)); Assert.That(s.Current,Is.EqualTo(12)); Assert.That(s.Clamped,Is.True);
        }
        [Test] public void TableRejectDoesNotExtrapolateAndAcceptsExactEndpoint()
        {
            var result=Load("quad_test_rpm_table",p=>p["rotors"][0]["performance"]["outOfRangePolicy"]="Reject");
            Assert.That(result.Success,Is.True); var r=result.Parameters.Rotors[0];
            Assert.That(At(r,10000).Thrust,Is.EqualTo(9.81).Within(1e-12));
            Assert.Throws<ArgumentOutOfRangeException>(()=>At(r,10001));
        }
        [Test] public void MapUsesAxialJAndInterpolatesCoefficientsBeforeActualRpmScaling()
        {
            var r=Rotor("quad_test_performance_map"); double axial=.25*(6250.0/60)*r.Diameter;
            var s=At(r,6250,axial); var basic=At(Rotor("quad_test_basic"),6250);
            Assert.That(s.AdvanceRatio,Is.EqualTo(.25).Within(1e-12));
            Assert.That(s.Thrust,Is.EqualTo(basic.Thrust*.85).Within(1e-10)); Assert.That(s.Torque,Is.EqualTo(basic.Torque*.925).Within(1e-10));
            Assert.That(s.Current,Is.Null);
        }
        [Test] public void MapClampsCoefficientsButStillUsesActualRpmAndReportsClamping()
        {
            var r=Rotor("quad_test_performance_map"); var slow=At(r,1000); var fast=At(r,20000,100);
            Assert.That(slow.Thrust,Is.EqualTo(.0981).Within(1e-10)); Assert.That(slow.Clamped,Is.True);
            Assert.That(fast.Thrust,Is.EqualTo(9.81*4*.7).Within(1e-10)); Assert.That(fast.Clamped,Is.True);
            Assert.That(At(r,5000,-100).Thrust,Is.EqualTo(2.4525*1.1).Within(1e-10));
        }
        [Test] public void MapBilinearInterpolationUsesBothCoordinates()
        {
            var p=new RotorPerformanceProfile { model="PerformanceMap",outOfRangePolicy="Clamp",performanceMap=new[]{
                new PerformanceMapPoint{rpm=1000,advanceRatio=0,ct=1,cq=.1},new PerformanceMapPoint{rpm=1000,advanceRatio=1,ct=2,cq=.2},
                new PerformanceMapPoint{rpm=2000,advanceRatio=0,ct=3,cq=.3},new PerformanceMapPoint{rpm=2000,advanceRatio=1,ct=6,cq=.6}} };
            var performance=new PropellerPerformance(p,1,1); var sample=performance.Evaluate(PhysicsMath.RpmToOmega(1500),12.5);
            Assert.That(sample.Thrust,Is.EqualTo(PhysicsMath.CtThrust(1500,3,1,1)).Within(1e-9));
            Assert.That(sample.Torque,Is.EqualTo(PhysicsMath.CqTorque(1500,.3,1,1)).Within(1e-9));
        }
        [Test] public void OneRpmMapInterpolatesJWithRpmCoefficientClamp()
        {
            var result=Load("quad_test_performance_map",p=>{
                foreach(var r in p["rotors"]) r["performance"]["performanceMap"]=new JArray(r["performance"]["performanceMap"].Where(x=>(double)x["rpm"]==5000).Select(x=>x.DeepClone()));
            }); Assert.That(result.Success,Is.True,string.Join("\n",result.Issues));
            Assert.That(At(result.Parameters.Rotors[0],10000).Thrust,Is.EqualTo(9.81).Within(1e-10));
        }
        [Test] public void DensityScalingAppliesOnlyToCoefficientMaps()
        {
            Assert.That(Load("quad_test_rpm_table",density:2.45).Success,Is.False);
            var a=Load("quad_test_performance_map").Parameters.Rotors[0]; var b=Load("quad_test_performance_map",density:2.45).Parameters.Rotors[0];
            Assert.That(At(b,5000).Thrust,Is.EqualTo(At(a,5000).Thrust*2).Within(1e-10));
            Assert.That(At(b,5000).Torque,Is.EqualTo(At(a,5000).Torque*2).Within(1e-10));
        }
        [TestCase("duplicate")] [TestCase("origin")] [TestCase("current")] [TestCase("policy")] [TestCase("density")] [TestCase("coverage")]
        public void BadTablesRejected(string fault)
        {
            var result=Load("quad_test_rpm_table",p=>{
                var perf=p["rotors"][0]["performance"]; var table=perf["rpmTable"];
                if(fault=="duplicate") table[1]["rpm"]=0;
                if(fault=="origin") table[0]["thrustN"]=1;
                if(fault=="current") ((JObject)table[1]).Remove("currentA");
                if(fault=="policy" || fault=="density") ((JObject)perf).Remove(fault=="policy" ? "outOfRangePolicy" : "referenceAirDensityKgM3");
                if(fault=="coverage") { perf["outOfRangePolicy"]="Reject"; table.Last["rpm"]=9999; }
            }); Assert.That(result.Success,Is.False);
        }
        [TestCase("hole")] [TestCase("duplicate")] [TestCase("static")] [TestCase("policy")]
        public void BadMapsRejected(string fault)
        {
            var result=Load("quad_test_performance_map",p=>{
                var perf=p["rotors"][0]["performance"]; var rows=(JArray)perf["performanceMap"];
                if(fault=="hole") rows.RemoveAt(0);
                if(fault=="duplicate") rows[0]=rows[1].DeepClone();
                if(fault=="static") foreach(var row in rows.Where(x=>(double)x["advanceRatio"]==0)) row["advanceRatio"]=.1;
                if(fault=="policy") ((JObject)perf).Remove("outOfRangePolicy");
            }); Assert.That(result.Success,Is.False);
        }
        [Test] public void RuntimeSnapshotDoesNotRetainMutableTableOrMapRows()
        {
            var result=Load("quad_test_rpm_table"); result.Drone.rotors[0].performance.rpmTable[2].thrustN=999;
            Assert.That(At(result.Parameters.Rotors[0],5000).Thrust,Is.EqualTo(2.4525).Within(1e-12));
            result=Load("quad_test_performance_map"); foreach(var row in result.Drone.rotors[0].performance.performanceMap) row.ct=999;
            Assert.That(At(result.Parameters.Rotors[0],5000).Thrust,Is.EqualTo(2.4525).Within(1e-10));
        }
        [TestCase("quad_test_rpm_table")] [TestCase("quad_test_performance_map")]
        public void AllocatorMatchesStaticWrenchWithNonquadraticInverseAndVariableTorqueRatio(string name)
        {
            var p=Load(name).Parameters; var a=new QuadAllocator(p); var commands=new double[4]; var desired=new DVector3(.03,.02,-.04);
            Assert.That(a.Allocate(9.81,desired,commands),Is.False); AssertWrench(p,commands,9.81,desired);
            Assert.That(a.Allocate(9.81,new DVector3(3,8,4),commands),Is.True);
            AssertWrench(p,commands,9.81,new DVector3(3,8,4)*a.TorqueScale);
        }
        private static void AssertWrench(RuntimeDroneParameters p,double[] commands,double desiredT,DVector3 desiredQ)
        {
            double total=0; DVector3 q=default;
            for(int i=0;i<4;i++)
            {
                Assert.That(commands[i],Is.InRange(0,1)); var r=p.Rotors[i]; var s=r.Performance.Evaluate(commands[i]*r.MaxOmega);
                total+=s.Thrust; q+=DVector3.Cross(r.Position-p.CenterOfMass,r.Axis*s.Thrust)+r.Axis*(r.ReactionSign*s.Torque);
            }
            Assert.That(total,Is.EqualTo(desiredT).Within(1e-7)); Assert.That((q-desiredQ).Length,Is.LessThan(1e-7));
        }
        [Test] public void AllocatorCompensatesUnequalStaticCurvesAndOffsetCom()
        {
            var result=Load("quad_test_rpm_table",p=>{
                p["massProperties"]["centerOfMassLocalM"]=new JArray(.02,0,.01);
                foreach(var row in p["rotors"][0]["performance"]["rpmTable"]) row["torqueNm"]=(double)row["torqueNm"]*1.2;
            }); var commands=new double[4]; var a=new QuadAllocator(result.Parameters);
            Assert.That(a.Allocate(9.81,default,commands),Is.False); AssertWrench(result.Parameters,commands,9.81,default);
        }
        [Test] public void NonmonotoneTableRemainsPhysicsDataButPilotRejectsIt()
        {
            var result=Load("quad_test_rpm_table",p=>p["rotors"][0]["performance"]["rpmTable"][3]["thrustN"]=1);
            Assert.That(result.Success,Is.True); Assert.Throws<ArgumentException>(()=>new QuadAllocator(result.Parameters));
        }
        [Test] public void SignedMapCoefficientsAreNotSilentlyClipped()
        {
            var result=Load("quad_test_performance_map",p=>{
                foreach(var row in p["rotors"][0]["performance"]["performanceMap"].Where(x=>(double)x["advanceRatio"]==.5)) { row["ct"]=-.1; row["cq"]=-.01; }
            }); Assert.That(result.Success,Is.True); var r=result.Parameters.Rotors[0];
            var s=At(r,5000,.5*5000/60*r.Diameter); Assert.That(s.Thrust,Is.LessThan(0)); Assert.That(s.Torque,Is.LessThan(0));
        }
        [Test] public void CsvIsSortedAndInvariantAcrossUserLocale()
        {
            var old=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=new CultureInfo("ru-RU");
                var p=PerformanceCsv.Parse("torqueNm,rpm,thrustN\n0.1,1000,1.5\n0,0,0\n","RpmTable","Clamp");
                Assert.That(p.rpmTable[1].thrustN,Is.EqualTo(1.5)); Assert.That(p.rpmTable[0].currentA,Is.Null);
                var map=PerformanceCsv.Parse("rpm,advanceRatio,ct,cq,reynolds\n5000,0,.1,.01,20000\n5000,.5,.05,.005,21000\n","PerformanceMap","Clamp");
                Assert.That(map.performanceMap.Length,Is.EqualTo(2));
            }
            finally { CultureInfo.CurrentCulture=old; }
        }
        [TestCase("rpm,thrustN,torqueNm\n0,0,0\n1000,NaN,.1")]
        [TestCase("rpm,thrustN,torqueNm\n0,0,0\n1000,1")]
        [TestCase("rpm,thrustN,torqueNm\n0,0,0\n0,1,.1")]
        [TestCase("rpm,thrustN,torqueNm,currentA\n0,0,0,0\n1000,1,.1,")]
        [TestCase("rpm,thrustN,cp\n0,0,0\n1000,1,.1")]
        public void MalformedCsvIsRejected(string csv) => Assert.Throws<ArgumentException>(()=>PerformanceCsv.Parse(csv,"RpmTable","Clamp"));
        [TestCase(0.005)] [TestCase(0.01)] [TestCase(0.02)]
        public void TablePilotRatePidTracksAndBrakesWithMotorLag(double dt)
        {
            var p=Load("quad_test_rpm_table").Parameters; var control=new FlightController(); var allocator=new QuadAllocator(p);
            var omega=new double[4]; var commands=new double[4]; double rate=0,desired=0; bool saturated=false;
            for(int i=0;i<4;i++) omega[i]=p.Rotors[i].Performance.OmegaForStaticThrust(9.81/4,p.Rotors[i].MaxOmega);
            for(int step=0;step<(int)(10/dt);step++)
            {
                desired=PilotMath.SmoothCommand(desired,step<(int)(5/dt) ? 70*Math.PI/180 : 0,.12,dt);
                var accel=control.RateAcceleration(new DVector3(0,desired,0),new DVector3(0,rate,0),dt,12,new PidTerms(2,.15,3),40,!saturated);
                saturated=allocator.Allocate(9.81,accel*p.Inertia.Y,commands); double torque=0;
                for(int i=0;i<4;i++)
                {
                    var r=p.Rotors[i]; double target=commands[i]*r.MaxOmega;
                    omega[i]=PhysicsMath.MotorStep(omega[i],target,target>omega[i] ? r.TauUp : r.TauDown,dt);
                    torque+=r.ReactionSign*r.Performance.Evaluate(omega[i]).Torque;
                }
                rate+=torque/p.Inertia.Y*dt;
                if(step==(int)(5/dt)-1) Assert.That(rate*180/Math.PI,Is.EqualTo(70).Within(1));
            }
            Assert.That(Math.Abs(rate),Is.LessThan(.02));
        }
        [TestCase("quad_test_rpm_table")] [TestCase("quad_test_performance_map")]
        public void ZeroCollectiveStopsAllCommandsAndPeakCollectiveRemainsBounded(string name)
        {
            var p=Load(name).Parameters; var a=new QuadAllocator(p); var c=new double[4];
            a.Allocate(0,new DVector3(1,1,1),c); foreach(var value in c) Assert.That(value,Is.Zero);
            a.Allocate(100,default,c); Assert.That(a.AchievedCollective,Is.LessThanOrEqualTo(p.MaxTotalThrust));
            AssertWrench(p,c,a.AchievedCollective,default);
        }
        [Test] public void VaryingStaticMapMatchesAllocatorWrench()
        {
            var result=Load("quad_test_performance_map",p=>{
                foreach(var r in p["rotors"]) foreach(var row in r["performance"]["performanceMap"])
                { double factor=1+(double)row["rpm"]/10000*.1; row["ct"]=(double)row["ct"]*factor; row["cq"]=(double)row["cq"]*factor*factor; }
            }); var p=result.Parameters; var c=new double[4]; var a=new QuadAllocator(p); var q=new DVector3(.03,.02,-.04);
            Assert.That(a.Allocate(9.81,q,c),Is.False); AssertWrench(p,c,9.81,q);
        }
        [Test] public void ExplicitRejectMapRejectsRpmAndJButAllowsStoppedRotor()
        {
            var result=Load("quad_test_performance_map",p=>p["rotors"][0]["performance"]["outOfRangePolicy"]="Reject");
            Assert.That(result.Success,Is.True); var r=result.Parameters.Rotors[0];
            Assert.Throws<ArgumentOutOfRangeException>(()=>At(r,1000)); Assert.Throws<ArgumentOutOfRangeException>(()=>At(r,5000,100));
            Assert.That(At(r,0,100).Thrust,Is.Zero); Assert.Throws<ArgumentException>(()=>new QuadAllocator(result.Parameters));
        }
    }
}
