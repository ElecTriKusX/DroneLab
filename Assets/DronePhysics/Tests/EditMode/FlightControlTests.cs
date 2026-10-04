using System;
using System.IO;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class FlightControlTests
    {
        private static RuntimeDroneParameters Parameters()
        {
#if UNITY_EDITOR
            string folder=Path.Combine(UnityEngine.Application.dataPath,"DronePhysics/Resources/DronePhysics");
#else
            string folder=Path.Combine(TestContext.CurrentContext.TestDirectory,"Profiles");
#endif
            string Read(string file) => File.ReadAllText(Path.Combine(folder,file+".json"));
            return ProfileLoader.Load(Read("quad_test_basic"),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema")).Parameters;
        }
        [Test] public void InputClampsAndRejectsNonFiniteAxes()
        {
            var input=new FlightInput(3,-4,double.NaN,double.PositiveInfinity,-0.5);
            Assert.That(input.Roll,Is.EqualTo(1)); Assert.That(input.Pitch,Is.EqualTo(-1));
            Assert.That(input.Yaw,Is.Zero); Assert.That(input.Climb,Is.Zero); Assert.That(input.Throttle,Is.Zero);
        }
        [Test] public void DerivativeHasNoSetpointKickAndResetClearsHistory()
        {
            var pid=new PidController(); var terms=new PidTerms(0,1,0,0);
            pid.Step(0,0,0.01,0,terms,100);
            Assert.That(pid.Step(10,0,0.01,0,terms,100),Is.Zero);
            Assert.That(pid.Step(10,0.2,0.01,0,terms,100),Is.EqualTo(-20).Within(1e-9));
            pid.Reset(); Assert.That(pid.Step(0,20,0.01,0,terms,100),Is.Zero);
        }
        [Test] public void OutputSaturationAndActuatorSaturationPreventWindup()
        {
            var pid=new PidController(); var terms=new PidTerms(1,0,0.5);
            for(int i=0;i<1000;i++) pid.Step(10,0,0.01,2,terms,1);
            Assert.That(pid.Integral,Is.Zero);
            for(int i=0;i<1000;i++) pid.Step(0.1,0,0.01,2,terms,1,false);
            Assert.That(pid.Integral,Is.Zero);
            for(int i=0;i<1000;i++) pid.Step(0.1,0,0.01,0,terms,1);
            Assert.That(pid.Integral,Is.EqualTo(0.5).Within(1e-9));
            pid.Step(-0.1,0,0.01,0,terms,1); Assert.That(pid.Integral,Is.LessThan(0.5));
        }
        [Test] public void CommandSmoothingSupportsSignedTargetsAndIndependentSubsteps()
        {
            double whole=PilotMath.SmoothCommand(1,-1,0.12,0.2), split=1;
            for(int i=0;i<20;i++) split=PilotMath.SmoothCommand(split,-1,0.12,0.01);
            Assert.That(split,Is.EqualTo(whole).Within(1e-12)); Assert.That(whole,Is.InRange(-1,1));
            Assert.That(PilotMath.SmoothCommand(1,-1,0,0.01),Is.EqualTo(-1));
        }
        [Test] public void ControllerResetRemovesAccumulatedTrim()
        {
            var controller=new FlightController(); var terms=new PidTerms(1,0,3);
            for(int i=0;i<100;i++) controller.RateAcceleration(new DVector3(0,0.1,0),default,0.01,0,terms,40,true);
            Assert.That(controller.RateAcceleration(default,default,0.01,0,terms,40,false).Y,Is.GreaterThan(0.09));
            controller.Reset(); Assert.That(controller.RateAcceleration(default,default,0.01,0,terms,40,false).Length,Is.Zero);
        }
        [Test] public void AllocatorPreservesCollectiveAndTorqueDirectionDuringSaturation()
        {
            var p=Parameters(); var allocator=new QuadAllocator(p); var commands=new double[4];
            var requested=new DVector3(0.4,0.5,-0.3);
            Assert.That(allocator.Allocate(9.81,requested,commands),Is.True);
            double total=0; DVector3 torque=default;
            for(int i=0;i<4;i++)
            {
                var r=p.Rotors[i]; double t=r.MaxThrust*commands[i]*commands[i]; total+=t;
                torque+=DVector3.Cross(r.Position-p.CenterOfMass,r.Axis*t)+r.Axis*(r.ReactionSign*r.KQ/r.KT*t);
                Assert.That(commands[i],Is.InRange(0,1));
            }
            Assert.That(total,Is.EqualTo(9.81).Within(1e-9));
            Assert.That((torque-requested*allocator.TorqueScale).Length,Is.LessThan(1e-9));
            allocator.Allocate(0,requested,commands); foreach(var command in commands) Assert.That(command,Is.Zero);
            allocator.Allocate(100,default,commands); Assert.That(allocator.AchievedCollective,Is.EqualTo(p.MaxTotalThrust).Within(1e-8));
        }
        private sealed class RatePlant
        {
            public double Rate, Angle, Spread;
            private bool saturated;
            private double desired;
            private readonly bool roll;
            private DVector3 Axis(double value) => roll ? new DVector3(value,0,0) : new DVector3(0,value,0);
            private double Component(DVector3 v) => roll ? v.X : v.Y;
            private readonly RuntimeDroneParameters p=Parameters();
            private readonly FlightController control=new FlightController();
            private readonly QuadAllocator allocator;
            private readonly double[] omega=new double[4],commands=new double[4];
            private readonly PidTerms rateTerms=new PidTerms(2,0.15,3), attitudeTerms=new PidTerms(0.1,0.1,0.3);
            public RatePlant(bool rollAxis=false) { roll=rollAxis; allocator=new QuadAllocator(p); for(int i=0;i<4;i++) omega[i]=p.Rotors[i].MaxOmega/2; }
            public void Step(double target, double dt, double disturbance=0, bool angle=false)
            {
                if(angle) target=Component(control.AttitudeRate(Axis(Math.Sin(target-Angle)),
                    Axis(Rate),dt,5,attitudeTerms,100*Math.PI/180,!saturated));
                desired=PilotMath.SmoothCommand(desired,target,0.12,dt);
                var accel=control.RateAcceleration(Axis(desired),Axis(Rate),dt,12,rateTerms,40,!saturated);
                saturated=allocator.Allocate(p.Mass*p.Gravity,accel*(roll ? p.Inertia.X : p.Inertia.Y),commands);
                double torque=disturbance,min=double.MaxValue,max=0;
                for(int i=0;i<4;i++)
                {
                    var r=p.Rotors[i]; double targetOmega=commands[i]*r.MaxOmega;
                    omega[i]=PhysicsMath.MotorStep(omega[i],targetOmega,targetOmega>omega[i] ? r.TauUp:r.TauDown,dt);
                    double thrust=PhysicsMath.Thrust(omega[i],r.KT);
                    torque+=Component(DVector3.Cross(r.Position-p.CenterOfMass,r.Axis*thrust)+
                        r.Axis*(r.ReactionSign*PhysicsMath.Torque(omega[i],r.KQ)));
                    min=Math.Min(min,omega[i]); max=Math.Max(max,omega[i]);
                }
                Spread=PhysicsMath.OmegaToRpm(max-min); Rate+=torque/(roll ? p.Inertia.X : p.Inertia.Y)*dt; Angle+=Rate*dt;
            }
        }
        [TestCase(0.005)] [TestCase(0.01)] [TestCase(0.02)]
        public void ProductionPidTracksYawAndBrakesOnRelease(double dt)
        {
            var plant=new RatePlant();
            for(int i=0;i<(int)(5/dt);i++) plant.Step(70*Math.PI/180,dt);
            TestContext.Progress.WriteLine($"PID yaw dt={dt}: {plant.Rate*180/Math.PI:F3} deg/s; spread={plant.Spread:F3} RPM");
            Assert.That(plant.Rate*180/Math.PI,Is.EqualTo(70).Within(1));
            Assert.That(plant.Spread,Is.LessThan(10));
            for(int i=0;i<(int)(5/dt);i++) plant.Step(0,dt);
            Assert.That(Math.Abs(plant.Rate),Is.LessThan(0.02));
        }
        [Test] public void IntegralRejectsSustainedExternalTorque()
        {
            var plant=new RatePlant(); for(int i=0;i<3000;i++) plant.Step(0,0.01,0.01);
            Assert.That(Math.Abs(plant.Rate),Is.LessThan(0.002));
            Assert.That(plant.Spread,Is.GreaterThan(20));
        }
        [TestCase(0.005)] [TestCase(0.01)] [TestCase(0.02)]
        public void CascadedAttitudePidTracksAndLevels(double dt)
        {
            var plant=new RatePlant(true);
            for(int i=0;i<(int)(8/dt);i++) plant.Step(20*Math.PI/180,dt,angle:true);
            Assert.That(plant.Angle*180/Math.PI,Is.EqualTo(20).Within(0.5));
            for(int i=0;i<(int)(8/dt);i++) plant.Step(0,dt,angle:true);
            Assert.That(Math.Abs(plant.Angle*180/Math.PI),Is.LessThan(0.5));
        }
        [TestCase(0.005)] [TestCase(0.01)] [TestCase(0.02)]
        public void CascadedAltitudePidHoldsAfterHeightStepWithMotorLag(double dt)
        {
            var p=Parameters(); var controller=new FlightController();
            var heightTerms=new PidTerms(0.1,0,0.5); var velocityTerms=new PidTerms(0.8,0.1,2);
            double height=0,velocity=0,omega=p.Rotors[0].MaxOmega/2;
            for(int i=0;i<(int)(12/dt);i++)
            {
                double accel=controller.ClimbAcceleration(2-height,velocity,dt,3,heightTerms,3,velocityTerms,2,4,true);
                double target=p.Rotors[0].MaxOmega*Math.Sqrt(p.Mass*(p.Gravity+accel)/p.MaxTotalThrust);
                omega=PhysicsMath.MotorStep(omega,target,target>omega ? 0.06:0.08,dt);
                velocity+=(4*PhysicsMath.Thrust(omega,p.Rotors[0].KT)/p.Mass-p.Gravity)*dt;
                height+=velocity*dt;
            }
            TestContext.Progress.WriteLine($"PID height dt={dt}: {height:F4} m; vy={velocity:F4} m/s");
            Assert.That(height,Is.EqualTo(2).Within(0.15)); Assert.That(Math.Abs(velocity),Is.LessThan(0.05));
        }
    }
}
