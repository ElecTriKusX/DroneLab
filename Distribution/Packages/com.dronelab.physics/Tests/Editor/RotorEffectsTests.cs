using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class RotorEffectsTests
    {
        private static string Read(string name) => ProfileTestFiles.Read(name);
        private static ProfileLoadResult Load(string name="quad_test_rotor_effects",Action<JObject> edit=null)
        {
            var json=JObject.Parse(Read(name)); edit?.Invoke(json);
            return ProfileLoader.Load(json.ToString(),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema"));
        }
        private static RuntimeDroneParameters Parameters(string name="quad_test_rotor_effects")
        { var r=Load(name); Assert.That(r.Success,Is.True,string.Join("\n",r.Issues)); return r.Parameters; }
        [TestCase("ground_effect",true,false)] [TestCase("rotor_drag",false,true)] [TestCase("rotor_effects",true,true)]
        public void FixturesSelectModulesIndependently(string name,bool ground,bool drag)
        {
            var p=Parameters("quad_test_"+name); Assert.That(p.GroundEffect!=null,Is.EqualTo(ground)); Assert.That(p.RotorDrag,Is.EqualTo(drag));
        }
        [Test] public void EnabledModulesRequireSettingsForEveryRotor()
        {
            Assert.That(Load(edit:p=>((JObject)p).Remove("groundEffect")).Success,Is.False);
            Assert.That(Load(edit:p=>((JObject)p["rotors"][2]).Remove("advancedAerodynamics")).Success,Is.False);
        }
        [TestCase("coefficient",-1)] [TestCase("minHeightRadiusRatio",0)] [TestCase("maxMultiplier",.99)]
        public void InvalidGroundSettingsRejected(string field,double value)
            => Assert.That(Load(edit:p=>p["groundEffect"][field]=value).Success,Is.False);
        [Test] public void NegativeRotorDragRejected()
            => Assert.That(Load(edit:p=>p["rotors"][0]["advancedAerodynamics"]["rotorDragCoefficientKgPerRad"]=-1).Success,Is.False);
        [TestCase("bladeFlapping")] [TestCase("inducedDrag")] [TestCase("gyroscopicRotorEffects")]
        public void UnimplementedRotorEffectsRemainRejected(string module)
            => Assert.That(Load(edit:p=>p["physicsConfiguration"]["modules"][module]=true).Success,Is.False);
        [TestCase(0,1.5)] [TestCase(.25,1.5)] [TestCase(.5,1.25)] [TestCase(1,1.0625)] [TestCase(2,1.015625)] [TestCase(50,1)] [TestCase(51,1)]
        public void GroundGainMatchesBoundedFormula(double heightOverRadius,double expected)
            => Assert.That(RotorAerodynamics.GroundMultiplier(Parameters().GroundEffect,.2,heightOverRadius*.2),Is.EqualTo(expected).Within(1e-12));
        [Test] public void NoSurfaceNegativeHeightOrDisabledModuleMeansNoBoost()
        {
            var p=Parameters(); Assert.That(RotorAerodynamics.GroundMultiplier(p.GroundEffect,.1,double.PositiveInfinity),Is.EqualTo(1));
            Assert.That(RotorAerodynamics.GroundMultiplier(p.GroundEffect,.1,-.1),Is.EqualTo(1));
            Assert.That(RotorAerodynamics.GroundMultiplier(null,.1,0),Is.EqualTo(1));
        }
        [Test] public void DisabledEffectsIgnoreStoredCoefficientsAndPreserveBasicPhysics()
        {
            var p=Load(edit:j=> { j["physicsConfiguration"]["modules"]["groundEffect"]=false; j["physicsConfiguration"]["modules"]["rotorAerodynamics"]=false; }).Parameters;
            Assert.That(p.GroundEffect,Is.Null); Assert.That(p.Rotors.All(x=>x.RotorDragCoefficient==0),Is.True);
            var w=RotorAerodynamics.DragWrench(p,null,new DVector3(2,3,4),new DVector3(1,2,3)); Assert.That(w.Force.Length+w.Torque.Length,Is.Zero);
            Assert.That(p.MaxTotalThrust,Is.EqualTo(39.24).Within(1e-10));
        }
        [Test] public void GroundCoefficientsSnapshotAndNoChangeToFreeAirCapacityOrTorque()
        {
            var result=Load(); result.Drone.groundEffect.coefficient=100; result.Drone.rotors[0].advancedAerodynamics.rotorDragCoefficientKgPerRad=100;
            Assert.That(result.Parameters.GroundEffect.Coefficient,Is.EqualTo(1)); Assert.That(result.Parameters.Rotors[0].RotorDragCoefficient,Is.EqualTo(.0001));
            var basic=Parameters("quad_test_basic"); var effects=result.Parameters;
            Assert.That(effects.MaxTotalThrust,Is.EqualTo(basic.MaxTotalThrust));
            Assert.That(effects.Rotors[0].Performance.Evaluate(500).Torque,Is.EqualTo(basic.Rotors[0].Performance.Evaluate(500).Torque));
        }
        [Test] public void GroundHeightClampAndZeroCoefficientAreFinite()
        {
            var p=Load(edit:j=>j["groundEffect"]["minHeightRadiusRatio"]=1e-200).Parameters;
            Assert.That(RotorAerodynamics.GroundMultiplier(p.GroundEffect,.1,0),Is.EqualTo(1.5));
            p=Load(edit:j=>j["groundEffect"]["coefficient"]=0).Parameters;
            Assert.That(RotorAerodynamics.GroundMultiplier(p.GroundEffect,.1,0),Is.EqualTo(1));
            p=Load(edit:j=>j["groundEffect"]["maxMultiplier"]=1).Parameters;
            Assert.That(RotorAerodynamics.GroundMultiplier(p.GroundEffect,.1,0),Is.EqualTo(1));
        }
        [TestCase(.5,1)] [TestCase(.75,1.0625)] [TestCase(1,1.25)] [TestCase(-1,1)]
        public void NormalAlignmentFadesSteepSurfaces(double alignment,double expected)
            => Assert.That(RotorAerodynamics.GroundMultiplier(Parameters().GroundEffect,.2,.1,alignment),Is.EqualTo(expected).Within(1e-12));
        [Test] public void RadiusAndHeightScaleTogetherAndGainIsMonotone()
        {
            var p=Parameters().GroundEffect; double previous=1.5;
            for(int i=0;i<=500;i++)
            {
                double h=i*.01;
                double g=RotorAerodynamics.GroundMultiplier(p,.1,h);
                Assert.That(g,Is.InRange(1,1.5)); Assert.That(g,Is.LessThanOrEqualTo(previous+1e-12)); previous=g;
                Assert.That(RotorAerodynamics.GroundMultiplier(p,.2,2*h),Is.EqualTo(g).Within(1e-12));
            }
        }
        [Test] public void ProbeRangeFadeRemainsContinuousEvenForLargeUserCoefficient()
        {
            var p=Load(edit:j=>j["groundEffect"]["coefficient"]=10000).Parameters.GroundEffect;
            double inside=RotorAerodynamics.GroundMultiplier(p,.1,4.99999),outside=RotorAerodynamics.GroundMultiplier(p,.1,5.00001);
            Assert.That(inside-outside,Is.LessThan(1e-8)); Assert.That(outside,Is.EqualTo(1));
        }
        [Test] public void GroundDoesNotAmplifyZeroOrWindmillingThrust()
        {
            Assert.That(RotorAerodynamics.ThrustWithGroundEffect(0,1.5),Is.Zero);
            Assert.That(RotorAerodynamics.ThrustWithGroundEffect(-1,1.5),Is.EqualTo(-1));
            Assert.That(RotorAerodynamics.ThrustWithGroundEffect(2,1.25),Is.EqualTo(2.5));
        }
        [Test] public void DragUsesPerpendicularVelocityAndScalesLinearlyWithOmegaAndK()
        {
            var v=new DVector3(3,4,5); var n=new DVector3(0,1,0); var f=RotorAerodynamics.Drag(v,n,500,.0001);
            Assert.That((f-new DVector3(-.15,0,-.25)).Length,Is.LessThan(1e-12)); Assert.That(DVector3.Dot(f,v),Is.LessThan(0));
            Assert.That((RotorAerodynamics.Drag(v,n,1000,.0001)-f*2).Length,Is.LessThan(1e-12));
            Assert.That((RotorAerodynamics.Drag(v,n,500,.0002)-f*2).Length,Is.LessThan(1e-12));
            Assert.That((RotorAerodynamics.Drag(v,n*-1,500,.0001)-f).Length,Is.LessThan(1e-12));
        }
        [TestCase(0,.0001)] [TestCase(500,0)]
        public void StoppedRotorOrZeroKHasNoDrag(double omega,double k)
            => Assert.That(RotorAerodynamics.Drag(new DVector3(1,2,3),new DVector3(0,1,0),omega,k).Length,Is.Zero);
        [Test] public void AxialFlowHasNoRotorDragAndArbitraryAxisIsSupported()
        {
            var n=new DVector3(1,2,3).Normalized;
            Assert.That(RotorAerodynamics.Drag(n*5,n,500,.0001).Length,Is.LessThan(1e-12));
            var f=RotorAerodynamics.Drag(new DVector3(4,-1,2),n,500,.0001); Assert.That(Math.Abs(DVector3.Dot(f,n)),Is.LessThan(1e-12));
        }
        [Test] public void WindFixtureProducesIndependentBodyAndRotorDrag()
        {
            var result=ProfileLoader.Load(Read("quad_test_rotor_drag"),Read("environment_wind"),Read("drone-profile.schema"),Read("environment-profile.schema"));
            Assert.That(result.Success,Is.True,string.Join("\n",result.Issues)); var p=result.Parameters;
            var airVelocity=p.Wind*-1; // Stationary body in +X wind, no rotation.
            var body=BodyAerodynamics.Evaluate(p,airVelocity,default);
            double omega=PhysicsMath.RpmToOmega(5000);
            var rotors=RotorAerodynamics.DragWrench(p,new[]{omega,omega,omega,omega},airVelocity,default);
            // Independent analytical baselines for this fixture, including its CdX=1.1.
            double expectedBodyDrag=.5*1.225*1.1*.04*5*5;
            double expectedRotorDrag=4*.0001*omega*5;
            Assert.That((body.Force-new DVector3(expectedBodyDrag,0,0)).Length,Is.LessThan(1e-12));
            Assert.That((rotors.Force-new DVector3(expectedRotorDrag,0,0)).Length,Is.LessThan(1e-12));
            Assert.That(body.Torque.Length+rotors.Torque.Length,Is.LessThan(1e-12));
        }
        [Test] public void YawDragOpposesRotationWithoutSymmetricTranslation()
        {
            var p=Parameters(); var w=RotorAerodynamics.DragWrench(p,new[]{500.0,500,500,500},default,new DVector3(0,2,0));
            Assert.That(w.Force.Length,Is.LessThan(1e-12)); Assert.That(w.Torque.Y,Is.EqualTo(-.01568).Within(1e-12));
            Assert.That(w.Torque.X,Is.Zero); Assert.That(w.Torque.Z,Is.Zero);
        }
        [Test] public void RotorDragIncludesMomentAboutOffsetCom()
        {
            var p=Load(edit:j=>j["massProperties"]["centerOfMassLocalM"]=new JArray(.05,0,.03)).Parameters;
            var w=RotorAerodynamics.DragWrench(p,new[]{500.0,500,500,500},new DVector3(5,0,0),default);
            Assert.That(w.Force.X,Is.EqualTo(-1)); Assert.That(w.Torque.Y,Is.EqualTo(.03).Within(1e-12));
        }
        [Test] public void DragEnergyIsNonpositiveForTranslationRotationAndAsymmetricAxes()
        {
            var result=Load(edit:j=> {
                j["massProperties"]["centerOfMassLocalM"]=new JArray(.02,.01,-.03);
                j["rotors"][0]["geometry"]["thrustAxisLocal"]=new JArray(0,.8,.6);
            }); var p=result.Parameters; var rng=new Random(741);
            double Next()=>rng.NextDouble()*20-10;
            for(int k=0;k<500;k++)
            {
                var v=new DVector3(Next(),Next(),Next()); var angular=new DVector3(Next(),Next(),Next());
                var w=RotorAerodynamics.DragWrench(p,new[]{100.0,200,300,400},v,angular);
                Assert.That(DVector3.Dot(w.Force,v)+DVector3.Dot(w.Torque,angular),Is.LessThanOrEqualTo(1e-10));
            }
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void YawDragTimeConvergenceMatchesLinearDecay(double dt)
        {
            var p=Parameters(); double rate=2, damping=.0001*500*4*(.14*.14+.14*.14);
            for(int i=0;i<(int)(2/dt);i++) rate+=RotorAerodynamics.DragWrench(p,new[]{500.0,500,500,500},default,new DVector3(0,rate,0)).Torque.Y/p.Inertia.Y*dt;
            Assert.That(rate,Is.EqualTo(2*Math.Exp(-damping/p.Inertia.Y*2)).Within(.003));
        }
        [TestCase("quad_test_rpm_table")] [TestCase("quad_test_performance_map")]
        public void EffectsDoNotDoubleCountTableOrMapPerformance(string name)
        {
            var baseline=Parameters(name);
            var result=Load(name,j=> {
                j["physicsConfiguration"]["modules"]["groundEffect"]=true;
                j["physicsConfiguration"]["modules"]["rotorAerodynamics"]=true;
                j["groundEffect"]=new JObject { ["coefficient"]=1,["minHeightRadiusRatio"]=.25,["maxMultiplier"]=1.5 };
                foreach(var rotor in j["rotors"]) rotor["advancedAerodynamics"]=new JObject { ["rotorDragCoefficientKgPerRad"]=.0001 };
            }); Assert.That(result.Success,Is.True,string.Join("\n",result.Issues));
            var before=baseline.Rotors[0].Performance.Evaluate(500);
            var after=result.Parameters.Rotors[0].Performance.Evaluate(500);
            Assert.That(after.Thrust,Is.EqualTo(before.Thrust)); Assert.That(after.Torque,Is.EqualTo(before.Torque)); Assert.That(after.Current,Is.EqualTo(before.Current));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void RatePidCompensatesSteadyRotorDragAndBrakes(double dt)
        {
            var p=Parameters(); var control=new FlightController(); var allocator=new QuadAllocator(p);
            var omega=new double[4]; var commands=new double[4]; double rate=0,desired=0; bool saturated=false;
            for(int i=0;i<4;i++) omega[i]=p.Rotors[i].MaxOmega/2;
            for(int k=0;k<(int)(15/dt);k++)
            {
                desired=PilotMath.SmoothCommand(desired,k<(int)(10/dt) ? 70*Math.PI/180 : 0,.12,dt);
                var acceleration=control.RateAcceleration(new DVector3(0,desired,0),new DVector3(0,rate,0),dt,12,new PidTerms(2,.15,3),40,!saturated);
                saturated=allocator.Allocate(9.81,acceleration*p.Inertia.Y,commands); double torque=0;
                for(int i=0;i<4;i++)
                {
                    var r=p.Rotors[i]; double target=commands[i]*r.MaxOmega;
                    omega[i]=PhysicsMath.MotorStep(omega[i],target,target>omega[i] ? r.TauUp : r.TauDown,dt);
                    torque+=r.ReactionSign*r.Performance.Evaluate(omega[i]).Torque;
                }
                torque+=RotorAerodynamics.DragWrench(p,omega,default,new DVector3(0,rate,0)).Torque.Y;
                rate+=torque/p.Inertia.Y*dt;
                if(k==(int)(10/dt)-1)
                {
                    Assert.That(rate*180/Math.PI,Is.EqualTo(70).Within(1));
                    Assert.That(PhysicsMath.OmegaToRpm(omega.Max()-omega.Min()),Is.GreaterThan(20));
                }
            }
            Assert.That(Math.Abs(rate),Is.LessThan(.03));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void AltitudePidTrimsGroundGainWithMotorLag(double dt)
        {
            var p=Parameters(); var controller=new FlightController(); double h=.1,v=0,omega=p.Rotors[0].MaxOmega/2;
            var heightTerms=new PidTerms(.1,0,.5); var speedTerms=new PidTerms(.8,.1,2);
            for(int i=0;i<(int)(15/dt);i++)
            {
                double accel=controller.ClimbAcceleration(.1-h,v,dt,3,heightTerms,3,speedTerms,2,4,true);
                double target=p.Rotors[0].MaxOmega*Math.Sqrt(p.Mass*(p.Gravity+accel)/p.MaxTotalThrust);
                omega=PhysicsMath.MotorStep(omega,target,target>omega ? .06 : .08,dt);
                double gain=RotorAerodynamics.GroundMultiplier(p.GroundEffect,p.Rotors[0].Diameter/2,h);
                double thrust=4*RotorAerodynamics.ThrustWithGroundEffect(p.Rotors[0].Performance.Evaluate(omega).Thrust,gain);
                v+=(thrust/p.Mass-p.Gravity)*dt; h+=v*dt;
            }
            Assert.That(h,Is.EqualTo(.1).Within(.005)); Assert.That(Math.Abs(v),Is.LessThan(.005));
            Assert.That(omega,Is.LessThan(p.Rotors[0].MaxOmega/2));
        }
    }
}
