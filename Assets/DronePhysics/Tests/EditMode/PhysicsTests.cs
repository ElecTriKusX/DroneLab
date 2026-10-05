using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public class PhysicsTests
    {
        private static string Read(string file)
        {
#if UNITY_EDITOR
            return File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"DronePhysics/Resources/DronePhysics",file+".json"));
#else
            return File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"Profiles",file+".json"));
#endif
        }
        private static ProfileLoadResult Load(JObject drone=null,JObject env=null)
            => ProfileLoader.Load(drone?.ToString() ?? Read("quad_test_basic"),env?.ToString() ?? Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema"));
        [Test] public void ExampleLoadsAndHasExpectedCapacity()
        {
            var r=Load(); Assert.That(r.Success,Is.True,string.Join("\n",r.Issues));
            Assert.That(r.Parameters.ThrustToWeight,Is.EqualTo(4).Within(1e-8));
        }
        [Test] public void ZeroRpmMeansZeroThrust() => Assert.That(PhysicsMath.Thrust(0,1e-5),Is.Zero);
        [Test] public void TwiceRpmMeansFourTimesThrust() => Assert.That(PhysicsMath.Thrust(200,1e-5),Is.EqualTo(4*PhysicsMath.Thrust(100,1e-5)));
        [Test] public void TwiceCoefficientMeansTwiceThrust() => Assert.That(PhysicsMath.Thrust(200,2e-5),Is.EqualTo(2*PhysicsMath.Thrust(200,1e-5)));
        [TestCase(1,0.6321205588)] [TestCase(5,0.993262053)]
        public void MotorResponseIsExponential(double multiples,double expected)
            => Assert.That(PhysicsMath.MotorStep(0,100,0.1,0.1*multiples),Is.EqualTo(expected*100).Within(1e-6));
        [Test] public void MotorResponseIndependentOfSubsteps()
        {
            double value=0; for(int i=0;i<20;i++) value=PhysicsMath.MotorStep(value,100,0.1,0.01);
            Assert.That(value,Is.EqualTo(PhysicsMath.MotorStep(0,100,0.1,0.2)).Within(1e-10));
        }
        [Test] public void ZeroTimeConstantRespondsImmediately() => Assert.That(PhysicsMath.MotorStep(10,0,0,0.01),Is.Zero);
        [Test] public void CtCqScaling()
        {
            double t=PhysicsMath.CtThrust(5000,0.1,1.225,0.2);
            Assert.That(PhysicsMath.CtThrust(10000,0.1,1.225,0.2),Is.EqualTo(t*4).Within(1e-10));
            Assert.That(PhysicsMath.CtThrust(5000,0.1,2.45,0.2),Is.EqualTo(t*2).Within(1e-10));
            Assert.That(PhysicsMath.CtThrust(5000,0.1,1.225,0.4),Is.EqualTo(t*16).Within(1e-10));
            Assert.That(PhysicsMath.CqTorque(5000,0.01,1.225,0.4),Is.EqualTo(PhysicsMath.CqTorque(5000,0.01,1.225,0.2)*32).Within(1e-10));
        }
        [Test] public void SymmetricHoverCancelsAllMoments()
        {
            var p=Load().Parameters; DVector3 total=default; double thrust=0;
            foreach(var r in p.Rotors)
            {
                double t=PhysicsMath.Thrust(r.MaxOmega/2,r.KT); thrust+=t;
                total+=DVector3.Cross(r.Position-p.CenterOfMass,r.Axis*t)+r.Axis*(r.ReactionSign*PhysicsMath.Torque(r.MaxOmega/2,r.KQ));
            }
            Assert.That(thrust-p.Mass*p.Gravity,Is.EqualTo(0).Within(1e-10));
            Assert.That(total.Length,Is.LessThan(1e-10));
        }
        [Test] public void FrontLeftRotorAndCwReactionHaveCorrectSigns()
        {
            var r=Load().Parameters.Rotors[0]; var torque=DVector3.Cross(r.Position,r.Axis);
            Assert.That(torque.X,Is.LessThan(0)); Assert.That(torque.Z,Is.LessThan(0)); Assert.That(r.ReactionSign,Is.EqualTo(-1));
        }
        [Test] public void AllocatorProducesRequestedWrench()
        {
            var p=Load().Parameters; var allocator=new QuadAllocator(p); var commands=new double[4];
            var requested=new DVector3(0.03,0.02,-0.04);
            Assert.That(allocator.Allocate(9.81,requested,commands),Is.False);
            DVector3 torque=default; double collective=0;
            for(int i=0;i<4;i++)
            {
                var r=p.Rotors[i]; double omega=commands[i]*r.MaxOmega; double t=PhysicsMath.Thrust(omega,r.KT); collective+=t;
                torque+=DVector3.Cross(r.Position-p.CenterOfMass,r.Axis*t)+r.Axis*(r.ReactionSign*PhysicsMath.Torque(omega,r.KQ));
            }
            Assert.That(collective,Is.EqualTo(9.81).Within(1e-9)); Assert.That((torque-requested).Length,Is.LessThan(1e-9));
        }
        [Test] public void WindPushesStationaryDroneDownwindAndDragDissipatesEnergy()
        {
            var cd=new DVector3(1,1,1); var area=new DVector3(0.1,0.1,0.1); var v=new DVector3(-10,2,4);
            var f=PhysicsMath.AxisDrag(v,1.225,cd,area);
            Assert.That(f.X,Is.GreaterThan(0)); Assert.That(DVector3.Dot(f,v),Is.LessThan(0));
            Assert.That((PhysicsMath.AxisDrag(v*2,1.225,cd,area)-f*4).Length,Is.LessThan(1e-10));
            Assert.That(PhysicsMath.AxisDrag(default,1.225,cd,area).Length,Is.Zero);
        }
        [TestCase("massProperties.massKg",0)]
        [TestCase("rotors[0].motor.maxRpm",0)]
        [TestCase("rotors[0].motor.responseTimeUpS",-1)]
        public void InvalidNumbersRejected(string path,double value)
        {
            var p=JObject.Parse(Read("quad_test_basic")); ((JValue)p.SelectToken(path)).Value=value;
            Assert.That(Load(p).Success,Is.False);
        }
        [Test] public void DuplicateRotorRejected()
        {
            var p=JObject.Parse(Read("quad_test_basic")); p["rotors"][1]["rotorId"]=p["rotors"][0]["rotorId"].DeepClone(); Assert.That(Load(p).Success,Is.False);
        }
        [Test] public void MissingAndUnknownFieldsRejected()
        {
            var p=JObject.Parse(Read("quad_test_basic")); ((JObject)p["massProperties"]).Remove("massKg"); Assert.That(Load(p).Success,Is.False);
            p=JObject.Parse(Read("quad_test_basic")); p["surprise"]=true; Assert.That(Load(p).Success,Is.False);
        }
        [Test] public void NonfiniteAndWrongTypeRejected()
        {
            var p=JObject.Parse(Read("quad_test_basic")); p["massProperties"]["massKg"]=double.NaN; Assert.That(Load(p).Success,Is.False);
            p["massProperties"]["massKg"]="1.0"; Assert.That(Load(p).Success,Is.False);
            p["massProperties"]["massKg"]=double.PositiveInfinity; Assert.That(Load(p).Success,Is.False);
        }
        [Test] public void AxisAndInertiaRejected()
        {
            var p=JObject.Parse(Read("quad_test_basic")); p["rotors"][0]["geometry"]["thrustAxisLocal"]=new JArray(0,0,0); Assert.That(Load(p).Success,Is.False);
            p=JObject.Parse(Read("quad_test_basic")); p["massProperties"]["inertia"]=JObject.Parse("{\"mode\":\"ManualPrincipal\",\"principalMomentsKgM2\":[1,1,4],\"principalAxesRotationXyzw\":[0,0,0,1]}"); Assert.That(Load(p).Success,Is.False);
        }
        [Test] public void UnsupportedModuleNeverSilentlyIgnored()
        {
            var p=JObject.Parse(Read("quad_test_basic")); p["physicsConfiguration"]["modules"]["gyroscopicRotorEffects"]=true; Assert.That(Load(p).Success,Is.False);
        }
        [Test] public void DerivedValuesAreRecomputed()
        {
            var p=JObject.Parse(Read("quad_test_basic")); p["derived"]=JObject.Parse("{\"maxTotalThrustN\":1,\"hoverRpm\":1}");
            Assert.That(Load(p).Parameters.MaxTotalThrust,Is.EqualTo(39.24).Within(1e-8));
        }
        [Test] public void DuplicateJsonPropertyRejected()
        {
            var result=ProfileLoader.Load("{\"schemaVersion\":\"1.0.0\",\"schemaVersion\":\"1.0.0\"}",Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema"));
            Assert.That(result.Success,Is.False);
        }
        [Test] public void CtCqProfileLoads() => Assert.That(ProfileLoader.Load(Read("quad_test_ctcq"),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema")).Success,Is.True);
        [Test] public void FutureSchemaVersionRejected()
        {
            var p=JObject.Parse(Read("quad_test_basic")); p["schemaVersion"]="2.0.0"; Assert.That(Load(p).Success,Is.False);
        }
    }
}
