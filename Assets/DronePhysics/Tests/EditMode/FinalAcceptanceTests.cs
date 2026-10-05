using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class FinalAcceptanceTests
    {
        private static string Read(string name)
        {
#if UNITY_EDITOR
            string folder=Path.Combine(UnityEngine.Application.dataPath,"DronePhysics/Resources/DronePhysics");
#else
            string folder=Path.Combine(TestContext.CurrentContext.TestDirectory,"Profiles");
#endif
            return File.ReadAllText(Path.Combine(folder,name+".json"));
        }
        private static RuntimeDroneParameters Load(Action<JObject> edit=null)
        {
            var json=JObject.Parse(Read("quad_test_final_acceptance")); edit?.Invoke(json);
            var result=ProfileLoader.Load(json.ToString(),Read("environment_final_acceptance"),Read("drone-profile.schema"),Read("environment-profile.schema"));
            Assert.That(result.Success,Is.True,string.Join("\n",result.Issues));
            return result.Parameters;
        }
        [Test] public void FinalProfileHasControllableLayoutAndSupportedCoupling()
        {
            var p=Load(); var commands=new double[4];
            Assert.That(new QuadAllocator(p).Allocate(p.Mass*p.Gravity,default,commands),Is.False);
            Assert.That(commands.All(x=>x>0 && x<1),Is.True);
            Assert.That(p.Battery.Mode,Is.EqualTo("Electrical")); Assert.That(p.GroundEffect,Is.Not.Null);
            Assert.That(p.RotorDrag && p.BodyDrag,Is.True); Assert.That(p.AreaSamples.Count,Is.EqualTo(13));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void CoupledHoverBudgetConservesChargeAndPowerAtDifferentTimesteps(double dt)
        {
            var p=Load(); var power=new PowerSystem(p); var drive=new RotorDriveState(4);
            var omega=new double[4]; var commands=new double[4]; var allocator=new QuadAllocator(p);
            double rho=p.Environment.SampleAir(500).Density,spent=0,initial=power.Soc;
            double gain=RotorAerodynamics.GroundMultiplier(p.GroundEffect,p.Rotors[0].Diameter/2,.15);
            allocator.Allocate(p.Mass*p.Gravity/(gain*rho/p.Density),default,commands);
            for(int k=0;k<(int)Math.Round(10/dt);k++)
            {
                var desired=new double[4];
                for(int i=0;i<4;i++) desired[i]=PhysicsMath.MotorStep(omega[i],commands[i]*p.Rotors[i].MaxOmega,p.Rotors[i].TauUp,dt);
                power.Resolve(desired,omega,dt,true,rho,drive); spent+=power.Current*dt/3600; power.Commit(dt);
                double shaft=p.Rotors.Select((r,i)=>r.Performance.Evaluate(omega[i],density:rho).Torque*omega[i]).Sum();
                Assert.That(power.MechanicalPower,Is.EqualTo(shaft).Within(1e-9));
                Assert.That(power.OpenVoltage*power.Current,Is.EqualTo(power.ElectricalPower+power.BatteryLossPower).Within(1e-8));
                Assert.That(power.TerminalVoltage,Is.GreaterThan(0));
            }
            Assert.That(power.ConsumedAh,Is.EqualTo(spent).Within(1e-12));
            Assert.That(power.Soc,Is.EqualTo(initial-spent*3600/p.Battery.CapacityC).Within(1e-10));
            Assert.That(omega.Select((w,i)=>p.Rotors[i].Performance.Evaluate(w,density:rho).Thrust*gain).Sum(),Is.EqualTo(p.Mass*p.Gravity).Within(1e-5));
            var wind=p.Environment.Sample(new DVector3(0,500,0),10);
            var body=BodyAerodynamics.EvaluatePoint(p,wind*(-1),rho);
            var rotor=RotorAerodynamics.DragWrench(p,omega,wind*(-1),default);
            Assert.That(body.Force.Length,Is.GreaterThan(0)); Assert.That(rotor.Force.Length,Is.GreaterThan(0));
            Assert.That(DVector3.Dot(body.Force,wind*(-1)),Is.LessThanOrEqualTo(0));
            Assert.That(DVector3.Dot(rotor.Force,wind*(-1)),Is.LessThanOrEqualTo(0));
            drive.Set(0,0); var coast=(double[])omega.Clone(); coast[0]*=Math.Exp(-dt/p.Rotors[0].TauDown);
            power.Resolve(coast,omega,dt,true,rho,drive);
            Assert.That(power.RotorCurrentA[0],Is.Zero); Assert.That(omega[0],Is.GreaterThan(0));
            Assert.That(power.Soc,Is.LessThan(initial));
        }
        [Test] public void SurfaceAlternativePreservesElectricalRotorAndEnvironmentCoupling()
        {
            var surfaces=JObject.Parse(Read("quad_test_surfaces"))["bodyAerodynamics"];
            var p=Load(d=>d["bodyAerodynamics"]=surfaces.DeepClone());
            Assert.That(p.Surfaces.Count,Is.GreaterThan(0)); Assert.That(p.Battery.Mode,Is.EqualTo("Electrical"));
            var wrench=BodyAerodynamics.EvaluatePoint(p,new DVector3(3,4,5),p.Density);
            Assert.That(wrench.Force.Length,Is.GreaterThan(0));
            Assert.That(DVector3.Dot(wrench.Force,new DVector3(3,4,5)),Is.LessThan(0));
        }
        [TestCase("quad_test_rpm_table","environment_calm")]
        [TestCase("quad_test_performance_map","environment_atmosphere")]
        [TestCase("quad_test_battery_simple","environment_calm")]
        public void AlternativeModelsRemainValidOutsideTheMainCombination(string drone,string env)
        {
            var result=ProfileLoader.Load(Read(drone),Read(env),Read("drone-profile.schema"),Read("environment-profile.schema"));
            Assert.That(result.Success,Is.True,string.Join("\n",result.Issues));
            var commands=new double[4]; new QuadAllocator(result.Parameters).Allocate(result.Parameters.Mass*result.Parameters.Gravity,default,commands);
            Assert.That(commands.All(x=>!double.IsNaN(x) && x>=0 && x<=1),Is.True);
        }
    }
}
