using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class PilotBehaviorTests
    {
        private static RuntimeDroneParameters Parameters()
        {
string Read(string file) => ProfileTestFiles.Read(file);
            return ProfileLoader.Load(Read("quad_test_basic"),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema")).Parameters;
        }
        // Isolated one-axis rigid-body reference: production rate loop + allocator + motor lag.
        // Translation, cross-axis motion and Unity collisions are covered separately in Play Mode.
        private sealed class AxisRun
        {
            public double Rate,Angle,Torque;
            public bool Saturated;
            private readonly RuntimeDroneParameters p=Parameters();
            private readonly QuadAllocator allocator;
            private readonly double[] omega=new double[4],commands=new double[4];
            private readonly bool yaw;
            public AxisRun(bool yawAxis)
            {
                yaw=yawAxis; allocator=new QuadAllocator(p);
                for(int i=0;i<4;i++) omega[i]=p.Rotors[i].MaxOmega/2;
            }
            public double Spread => PhysicsMath.OmegaToRpm(omega.Max()-omega.Min());
            public void Step(double targetRate,double dt)
            {
                double inertia=yaw ? p.Inertia.Y : p.Inertia.X;
                var desired=yaw ? new DVector3(0,targetRate,0) : new DVector3(targetRate,0,0);
                var measured=yaw ? new DVector3(0,Rate,0) : new DVector3(Rate,0,0);
                var acceleration=PilotMath.RateAcceleration(desired,measured,12);
                var requested=acceleration*inertia;
                Saturated=allocator.Allocate(p.Mass*p.Gravity,requested,commands);
                DVector3 moment=default;
                for(int i=0;i<4;i++)
                {
                    var r=p.Rotors[i]; double target=commands[i]*r.MaxOmega;
                    omega[i]=PhysicsMath.MotorStep(omega[i],target,target>omega[i] ? r.TauUp:r.TauDown,dt);
                    var force=r.Axis*PhysicsMath.Thrust(omega[i],r.KT);
                    moment+=DVector3.Cross(r.Position-p.CenterOfMass,force)+r.Axis*(r.ReactionSign*PhysicsMath.Torque(omega[i],r.KQ));
                }
                Torque=yaw ? moment.Y : moment.X;
                Rate+=Torque/inertia*dt; Angle+=Rate*dt;
            }
        }
        [TestCase(0.005)] [TestCase(0.01)] [TestCase(0.02)]
        public void HeldYawReachesSeventyDegreesPerSecondThenRpmEqualize(double dt)
        {
            var run=new AxisRun(true); double target=70*Math.PI/180;
            run.Step(target,dt); Assert.That(run.Spread,Is.GreaterThan(5));
            for(int i=1;i<(int)Math.Round(3/dt);i++) run.Step(target,dt);
            Assert.That(run.Rate*180/Math.PI,Is.EqualTo(70).Within(0.1));
            Assert.That(run.Saturated,Is.False);
            Assert.That(run.Spread,Is.LessThan(0.5));
            Assert.That(Math.Abs(run.Torque),Is.LessThan(0.0001));
            TestContext.Progress.WriteLine($"Yaw dt={dt:F3}: rate={run.Rate*180/Math.PI:F3} deg/s, RPM spread={run.Spread:F4}");
        }
        [Test] public void ReleasedYawCreatesBrakingTorqueAndStops()
        {
            var run=new AxisRun(true); for(int i=0;i<300;i++) run.Step(70*Math.PI/180,0.01);
            for(int i=0;i<10;i++) run.Step(0,0.01);
            Assert.That(run.Torque,Is.LessThan(0)); Assert.That(run.Rate,Is.LessThan(70*Math.PI/180));
            for(int i=0;i<290;i++) run.Step(0,0.01);
            Assert.That(Math.Abs(run.Rate),Is.LessThan(0.002));
        }
        [Test] public void HeldTiltReachesAngleThenNeedsAlmostNoMoment()
        {
            var run=new AxisRun(false); double targetAngle=20*Math.PI/180;
            for(int i=0;i<400;i++)
            {
                double targetRate=PhysicsMath.Clamp(5*Math.Sin(targetAngle-run.Angle),-100*Math.PI/180,100*Math.PI/180);
                run.Step(targetRate,0.01);
            }
            Assert.That(run.Angle*180/Math.PI,Is.EqualTo(20).Within(0.2));
            Assert.That(Math.Abs(run.Rate),Is.LessThan(0.005));
            Assert.That(run.Spread,Is.LessThan(1));
            TestContext.Progress.WriteLine($"Tilt: angle={run.Angle*180/Math.PI:F3} deg, RPM spread={run.Spread:F4}");
        }
        [Test] public void TwentyDegreeTiltWithGravityCompensationStartsAtExpectedHorizontalAcceleration()
        {
            var p=Parameters(); var allocator=new QuadAllocator(p); var commands=new double[4];
            double angle=20*Math.PI/180;
            allocator.Allocate(p.Mass*p.Gravity/Math.Cos(angle),default,commands);
            double total=0;
            for(int i=0;i<4;i++) total+=PhysicsMath.Thrust(commands[i]*p.Rotors[i].MaxOmega,p.Rotors[i].KT);
            Assert.That(total*Math.Cos(angle)/p.Mass-p.Gravity,Is.EqualTo(0).Within(1e-8));
            Assert.That(total*Math.Sin(angle)/p.Mass,Is.EqualTo(3.570547998).Within(1e-8));
        }
    }
}
