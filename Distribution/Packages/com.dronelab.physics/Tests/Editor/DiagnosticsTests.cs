using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class DiagnosticsTests
    {
        private static string Read(string name) => ProfileTestFiles.Read(name);
        private static RuntimeDroneParameters Parameters(string name="quad_test_battery_simple",bool empty=false)
        {
            var j=JObject.Parse(Read(name)); if(empty) j["powerSystem"]["battery"]["initialSoc"]=0;
            var r=ProfileLoader.Load(j.ToString(),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema"));
            Assert.That(r.Success,Is.True,string.Join("\n",r.Issues)); return r.Parameters;
        }
        [TestCase(.02)] [TestCase(.01)] [TestCase(.005)]
        public void DriveFailureCoastsWithConfiguredTimeConstantRegardlessOfDt(double dt)
        {
            var p=Parameters(); var drive=new RotorDriveState(4); drive.Set(0,0); double omega=500;
            for(int i=0;i<(int)Math.Round(.2/dt);i++) omega=PhysicsMath.MotorStep(omega,drive.Target(0,1000),p.Rotors[0].TauDown,dt);
            Assert.That(omega,Is.EqualTo(500*Math.Exp(-.2/p.Rotors[0].TauDown)).Within(1e-10));
            Assert.That(PhysicsMath.Thrust(omega,p.Rotors[0].KT),Is.LessThan(PhysicsMath.Thrust(500,p.Rotors[0].KT)));
            drive.Reset(); Assert.That(drive.HasFault,Is.False); Assert.That(drive.Target(0,1000),Is.EqualTo(1000));
        }
        [TestCase("quad_test_battery_simple")] [TestCase("quad_test_battery_electrical")]
        public void FailedDriveHasZeroBusCurrentButResidualSpinAndThrustRemain(string name)
        {
            var p=Parameters(name); var drive=new RotorDriveState(4); drive.Set(0,0);
            var power=new PowerSystem(p); double[] requested={500,500,500,500},result=new double[4];
            power.Resolve(requested,result,.01,true,drive:drive);
            Assert.That(result[0],Is.EqualTo(500)); Assert.That(power.RotorCurrentA[0],Is.Zero);
            double shaft=Enumerable.Range(1,3).Sum(i=>p.Rotors[i].Performance.Evaluate(result[i]).Torque*result[i]);
            Assert.That(power.MechanicalPower,Is.EqualTo(shaft).Within(1e-10));
            Assert.That(power.Current,Is.EqualTo(power.RotorCurrentA.Sum()).Within(1e-10));
            Assert.That(p.Rotors[0].Performance.Evaluate(result[0]).Thrust,Is.GreaterThan(0));
        }
        [TestCase("quad_test_battery_simple")] [TestCase("quad_test_battery_electrical")]
        public void AllFailedDrivesSpendNoChargeAndEmptyPackDoesNotStopCoastingInstantly(string name)
        {
            var p=Parameters(name,true); var drive=new RotorDriveState(4);
            for(int i=0;i<4;i++) drive.Set(i,0);
            var power=new PowerSystem(p); double[] omega={400,400,400,400}; power.Resolve(omega,omega,.01,true,drive:drive); power.Commit(.01);
            Assert.That(power.Current+power.ConsumedAh+power.MechanicalPower,Is.Zero); Assert.That(omega.All(x=>x==400),Is.True);
        }
        [TestCase(-.1)] [TestCase(1.1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidDriveFractionsRejected(double value)=>Assert.Throws<ArgumentOutOfRangeException>(()=>new RotorDriveState(4).Set(0,value));
        [Test] public void HalfDriveMeansHalfTargetSpeedAndQuarterQuadraticThrust()
        {
            var d=new RotorDriveState(4); d.Set(1,.5); var p=Parameters(); var rotor=p.Rotors[1];
            Assert.That(rotor.Performance.Evaluate(d.Target(1,500)).Thrust,Is.EqualTo(rotor.Performance.Evaluate(500).Thrust*.25).Within(1e-12));
            Assert.That(d.Get(0),Is.EqualTo(1)); Assert.That(d.HasFault,Is.True);
        }
        [TestCase("ru-RU")] [TestCase("en-US")]
        public void CsvUsesInvariantDecimalsEscapesIdsAndDistinguishesUnknownFromZero(string culture)
        {
            var old=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=new CultureInfo(culture); var w=new StringWriter(); var csv=new FlightCsvWriter(w,new[]{"FL,\"test\""});
                var f=new FlightTelemetryFrame {TimeS=1.25,Dt=.01,Mass=1,Density=1.225,Rotors=new[]{new RotorTelemetry(0,1,0,0,0,0,null,0,null,1,0,default,default,false)}};
                csv.Write(f); csv.Flush(); var lines=w.ToString().Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
                Assert.That(lines[0],Does.Contain("\"rotor_0_FL,\"\"test\"\"_rpm\""));
                string[] cells=lines[1].Split(','); Assert.That(cells[1],Is.EqualTo("1.25")); Assert.That(cells.Length,Is.EqualTo(68));
                Assert.That(cells[56],Is.Empty); Assert.That(cells[57],Is.EqualTo("0")); Assert.That(cells[58],Is.Empty); Assert.That(csv.Rows,Is.EqualTo(1));
            }
            finally { CultureInfo.CurrentCulture=old; }
        }
        [Test] public void InvalidCsvFrameIsRejectedBeforeAnyRowIsWritten()
        {
            var w=new StringWriter(); var csv=new FlightCsvWriter(w,new[]{"FL"}); string header=w.ToString();
            Assert.Throws<ArgumentException>(()=>csv.Write(new FlightTelemetryFrame {TimeS=double.NaN,Rotors=new RotorTelemetry[1]}));
            Assert.That(w.ToString(),Is.EqualTo(header)); Assert.That(csv.Rows,Is.Zero);
            Assert.Throws<ArgumentException>(()=>csv.Write(new FlightTelemetryFrame {Rotors=new RotorTelemetry[2]}));
        }
        [Test] public void InducedSpeedOutputIsHoverEstimateAndProducesNoExtraForce()
        {
            Assert.That(PhysicsMath.InducedHoverVelocity(2.5,1.225,.127),Is.EqualTo(Math.Sqrt(2.5/(2*1.225*Math.PI*.127*.127/4))).Within(1e-12));
            Assert.That(PhysicsMath.InducedHoverVelocity(-1,1.225,.127),Is.Zero);
        }
    }
}
