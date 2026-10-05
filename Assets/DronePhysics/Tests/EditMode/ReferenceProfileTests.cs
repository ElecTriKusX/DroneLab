using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class ReferenceProfileTests
    {
        private static JObject Bench=>JObject.Parse(ProfileTestFiles.Read("Benchmarks/reference-benchmarks"));
        private static RuntimeDroneParameters Load(string name)
        {
            var result=ProfileLoader.Load(ProfileTestFiles.Read(name),ProfileTestFiles.Read("environment_calm"),ProfileTestFiles.Read("drone-profile.schema"),ProfileTestFiles.Read("environment-profile.schema"));
            Assert.That(result.Success,Is.True,string.Join("\n",result.Issues)); return result.Parameters;
        }
        [TestCase("reference_crazyflie20")] [TestCase("reference_crazyflie_brushless")] [TestCase("reference_hummingbird")]
        [TestCase("bench_apc_10x47_static")] [TestCase("bench_apc_10x47_axial")]
        public void OpenProfilesHaveControllableLayoutAndUnknownEffectsDisabled(string name)
        {
            var p=Load(name); var commands=new double[4];
            Assert.That(new QuadAllocator(p).Allocate(p.Mass*p.Gravity,default,commands),Is.False);
            Assert.That(commands.All(x=>x>0 && x<1),Is.True); Assert.That(p.BodyDrag || p.RotorDrag || p.InertialRotors || p.ThermalEnabled,Is.False);
            Assert.That(p.Battery,Is.Null);
        }
        [Test] public void CrazyflieWithheldMeasurementsWereNotImportedIntoRpmTable()
        {
            var data=(JObject)Bench["datasets"].First(x=>(string)x["id"]=="cf20_bitcraze_2015"); var p=Load((string)data["profile"]);
            var imported=JObject.Parse(ProfileTestFiles.Read((string)data["profile"]))["rotors"][0]["performance"]["rpmTable"];
            double max=0,sum=0, above10000Max=0; int count=0;
            foreach(var row in data["rows"].Where(x=>(string)x["role"]=="holdout"))
            {
                double rpm=(double)row["rpm"],expected=(double)row["totalThrustGram"]*.00980665/4;
                Assert.That(imported.Any(x=>(double)x["rpm"]==rpm),Is.False);
                double predicted=p.Rotors[0].Performance.Evaluate(PhysicsMath.RpmToOmega(rpm)).Thrust;
                double relative=Math.Abs(predicted-expected)/expected; max=Math.Max(max,relative); if(rpm>=10000) above10000Max=Math.Max(above10000Max,relative); sum+=relative; count++;
            }
            TestContext.WriteLine($"Bitcraze independent holdouts: {count}, mean relative={sum/count:P3}, max={max:P3}");
            Assert.That(count,Is.EqualTo(7)); // Acceptance bounds for this sparse table; low-RPM interpolation limitation is reported, not hidden.
            Assert.That(max,Is.LessThan(.20)); Assert.That(above10000Max,Is.LessThan(.025));
        }
        [TestCase("uiuc_apc_10x47_static")] [TestCase("uiuc_apc_10x47_axial")]
        public void UiucWithheldCoefficientsMatchWithCpConvertedToCq(string id)
        {
            var data=(JObject)Bench["datasets"].First(x=>(string)x["id"]==id); var p=Load((string)data["profile"]); var r=p.Rotors[0];
            var original=JObject.Parse(ProfileTestFiles.Read((string)data["profile"]))["rotors"][0]["performance"]; bool axial=id.EndsWith("axial");
            double maxT=0,maxP=0; int count=0;
            foreach(var row in data["rows"].Where(x=>(string)x["role"]=="holdout"))
            {
                double rpm=(double)(row["rpm"] ?? data["rpm"]),j=(double?)row["advanceRatio"] ?? 0,n=rpm/60;
                Assert.That((axial ? original["performanceMap"] : original["rpmTable"]).Any(x=>(double)x[axial ? "advanceRatio" : "rpm"]==(axial ? j:rpm)),Is.False);
                var predicted=r.Performance.Evaluate(PhysicsMath.RpmToOmega(rpm),j*n*r.Diameter);
                double ct=predicted.Thrust/(1.225*n*n*Math.Pow(r.Diameter,4)),cp=predicted.Torque*2*Math.PI/(1.225*n*n*Math.Pow(r.Diameter,5));
                maxT=Math.Max(maxT,Math.Abs(ct-(double)row["ct"]));maxP=Math.Max(maxP,Math.Abs(cp-(double)row["cp"]));count++;
            }
            TestContext.WriteLine($"UIUC {id}: {count} independent holdouts, max CT error={maxT:F7}, max CP error={maxP:F7}");
            Assert.That(count,Is.EqualTo(axial ? 8:7)); Assert.That(maxT,Is.LessThan(.0025)); Assert.That(maxP,Is.LessThan(.001));
        }
        [Test] public void AxialMapKeepsMeasuredNegativeThrustBranchAndZeroAtStop()
        {
            var r=Load("bench_apc_10x47_axial").Rotors[0]; double rpm=4014;
            Assert.That(r.Performance.Evaluate(PhysicsMath.RpmToOmega(rpm),.719*rpm/60*r.Diameter).Thrust,Is.LessThan(0));
            Assert.That(r.Performance.Evaluate(0,100).Thrust,Is.Zero);
        }
        [Test] public void PublishedBrushlessCurveHasSmallRepresentationErrorButIsNotRawData()
        {
            var p=Load("reference_crazyflie_brushless"); Assert.That(p.Mass,Is.EqualTo(.044)); Assert.That(p.Inertia.Y,Is.EqualTo(5.9e-5));
            double maxT=0,maxQ=0;
            for(double w=275;w<2900;w+=50)
            {
                double x=w/2900,t=-.23009526*x*x*x+.56176458*x*x-.0433191*x,q=-.0003396*x*x*x+.00087032*x*x+.0002896*x;
                var s=p.Rotors[0].Performance.Evaluate(w); maxT=Math.Max(maxT,Math.Abs(s.Thrust-t));maxQ=Math.Max(maxQ,Math.Abs(s.Torque-q));
            }
            TestContext.WriteLine($"Published fit representation: max dT={maxT:F8} N, max dQ={maxQ:F10} Nm");
            Assert.That(maxT,Is.LessThan(.0001));Assert.That(maxQ,Is.LessThan(.0000001));
        }
        [Test] public void HummingbirdUsesPaperRpmUnitsAndRoundedSaturationReferences()
        {
            var p=Load("reference_hummingbird"); var r=p.Rotors[0];
            Assert.That(r.Performance.Evaluate(PhysicsMath.RpmToOmega(6000)).Thrust,Is.EqualTo(2.052).Within(1e-12));
            Assert.That(r.Performance.Evaluate(r.MaxOmega).Thrust,Is.EqualTo(3.5).Within(1e-12));
            Assert.That(.544/p.Inertia.X,Is.EqualTo(77.7).Within(.05)); Assert.That(.102/p.Inertia.Y,Is.EqualTo(8.5).Within(.01));
            Assert.That(r.KT,Is.Not.EqualTo(8.54858e-6));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void PublishedMotorStepConstantIsReproducedAtAllTimesteps(double dt)
        {
            var r=Load("reference_crazyflie_brushless").Rotors[0]; double w=0;
            for(int k=0;k<(int)Math.Round(.2/dt);k++) w=PhysicsMath.MotorStep(w,2000,r.TauUp,dt);
            Assert.That(w,Is.EqualTo(2000*(1-Math.Exp(-4))).Within(1e-10));
        }
    }
}
