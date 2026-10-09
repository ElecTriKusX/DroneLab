using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class MultirotorAllocatorTests
    {
        private static RuntimeDroneParameters Load(int count,bool table=false)
        {
            var json=JObject.Parse(ProfileTestFiles.Read("quad_test_basic"));
            if(count!=4) {
                var original=(JObject)json["rotors"][0]; var rotors=new JArray();
                for(int i=0;i<count;i++) {
                    var rotor=(JObject)original.DeepClone(); rotor["rotorId"]="motor-"+i;
                    double angle=2*Math.PI*i/count;
                    rotor["geometry"]["positionLocalM"]=new JArray(.2*Math.Cos(angle),0,.2*Math.Sin(angle));
                    rotor["geometry"]["spinDirection"]=i%2==0?"CW":"CCW";rotors.Add(rotor);
                }
                json["rotors"]=rotors;
            }
            if(table) foreach(var rotor in json["rotors"]) rotor["performance"]=JObject.Parse(@"{
                'model':'RpmTable','referenceAirDensityKgM3':1.225,'outOfRangePolicy':'Clamp',
                'rpmTable':[{'rpm':0,'thrustN':0,'torqueNm':0},{'rpm':5000,'thrustN':2,'torqueNm':0.03},{'rpm':10000,'thrustN':8,'torqueNm':0.2}]
            }");
            var result=ProfileLoader.Load(json.ToString(),ProfileTestFiles.Read("environment_calm"),ProfileTestFiles.Read("drone-profile.schema"),ProfileTestFiles.Read("environment-profile.schema"));
            Assert.That(result.Success,Is.True,string.Join("; ",result.Issues)); return result.Parameters;
        }
        private static (double collective,DVector3 torque) Wrench(RuntimeDroneParameters p,double[] commands)
        {
            double total=0;var moment=default(DVector3);
            for(int i=0;i<p.Rotors.Count;i++) {
                var r=p.Rotors[i];var force=r.Performance.Evaluate(commands[i]*r.MaxOmega);
                total+=force.Thrust;moment+=DVector3.Cross(r.Position-p.CenterOfMass,r.Axis)*force.Thrust+r.Axis*(r.ReactionSign*force.Torque);
            }
            return (total,moment);
        }
        [TestCase(6)] [TestCase(8)] public void HoverAndCombinedMomentsMatchTheRequestedWrench(int count)
        {
            var p=Load(count);var allocator=new MultirotorAllocator(p);var commands=new double[count];
            foreach(var requested in new[]{default(DVector3),new DVector3(.08,.03,-.05),new DVector3(-.07,-.02,.06)}) {
                Assert.That(allocator.Allocate(9.81,requested,commands),Is.False);
                var achieved=Wrench(p,commands); Assert.That(achieved.collective,Is.EqualTo(9.81).Within(1e-8));
                Assert.That((achieved.torque-requested).Length,Is.LessThan(1e-8));
                foreach(double command in commands)Assert.That(command,Is.InRange(0,1));
            }
        }
        [TestCase(6)] [TestCase(8)] public void NonlinearTorqueCurvesPreserveCollectiveRollPitchAndYaw(int count)
        {
            var p=Load(count,true);var allocator=new MultirotorAllocator(p);var commands=new double[count];var requested=new DVector3(.06,.03,-.04);
            Assert.That(allocator.Allocate(9.81,requested,commands),Is.False);
            var achieved=Wrench(p,commands);Assert.That(achieved.collective,Is.EqualTo(9.81).Within(1e-7));
            Assert.That((achieved.torque-requested).Length,Is.LessThan(1e-7));
        }
        [Test] public void SaturatedYawPreservesCollectiveAndCannotIncreaseItByClipping()
        {
            var p=Load(6);var allocator=new MultirotorAllocator(p);var commands=new double[6];
            Assert.That(allocator.Allocate(9.81,new DVector3(0,100,0),commands),Is.True);
            var achieved=Wrench(p,commands);Assert.That(achieved.collective,Is.EqualTo(9.81).Within(1e-8));
            Assert.That(achieved.torque.X,Is.EqualTo(0).Within(1e-8));Assert.That(achieved.torque.Z,Is.EqualTo(0).Within(1e-8));
            Assert.That(allocator.TorqueScale,Is.InRange(0,1));
        }
        [Test] public void FourRotorsRetainExistingAllocatorResultsExactly()
        {
            var p=Load(4);var quad=new QuadAllocator(p);var multi=new MultirotorAllocator(p);var a=new double[4];var b=new double[4];
            var torque=new DVector3(.1,.03,-.08);
            Assert.That(multi.Allocate(9.81,torque,b),Is.EqualTo(quad.Allocate(9.81,torque,a)));Assert.That(b,Is.EqualTo(a));
        }
        [Test] public void WrongOutputCountAndNonfiniteWrenchAreRejected()
        {
            var a=new MultirotorAllocator(Load(6));Assert.Throws<ArgumentException>(()=>a.Allocate(9.81,default,new double[4]));
            Assert.Throws<ArgumentException>(()=>a.Allocate(double.NaN,default,new double[6]));
        }
    }
}
