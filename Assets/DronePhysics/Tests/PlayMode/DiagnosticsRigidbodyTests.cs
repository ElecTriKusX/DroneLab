using System.Collections;
using System.IO;
using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class DiagnosticsRigidbodyTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        private DronePhysicsBody Create(string drone="quad_test_basic",bool instant=false)
        {
            rig=new IsolatedPhysicsRig(instantaneous:instant,json:Resources.Load<TextAsset>("DronePhysics/"+drone).text);
            rig.Physics.SetArmed(true); for(int i=0;i<4;i++) rig.Physics.SetMotorCommand(i,.5); return rig.Physics;
        }
        [UnityTest] public IEnumerator OneDriveFailureReducesThrustAndCreatesRotation()
        {
            var b=Create(instant:true); rig.Step(); b.SetRotorDriveAuthority(0,0); rig.Step(10);
            Assert.That(b.Omega[0]+b.ThrustN[0]+b.ReactionTorqueNm[0],Is.Zero);
            Assert.That(b.Omega[1],Is.EqualTo(PhysicsMath.RpmToOmega(5000)).Within(1e-8));
            Assert.That(b.Body.angularVelocity.magnitude,Is.GreaterThan(.1)); Assert.That(b.Body.linearVelocity.y,Is.LessThan(-.1)); yield break;
        }
        [UnityTest] public IEnumerator FailedMotorCoastsAndBatteryNoLongerFeedsIt()
        {
            var b=Create("quad_test_battery_simple"); rig.Step(30); double old=b.Omega[0];
            b.SetRotorDriveAuthority(0,0); rig.Step();
            Assert.That(b.Omega[0],Is.EqualTo(old*System.Math.Exp(-IsolatedPhysicsRig.Dt/b.Parameters.Rotors[0].TauDown)).Within(1e-8));
            Assert.That(b.ThrustN[0],Is.GreaterThan(0)); Assert.That(b.Power.RotorCurrentA[0],Is.Zero); yield break;
        }
        [UnityTest] public IEnumerator ResetClearsFaultAndRestoresNormalMotorTarget()
        {
            var b=Create(instant:true); b.SetRotorDriveAuthority(0,0); rig.Step(); b.ResetMotorState();
            Assert.That(b.Drive.HasFault,Is.False); b.SetArmed(true); for(int i=0;i<4;i++) b.SetMotorCommand(i,.5);
            rig.Step(); Assert.That(b.ThrustN[0],Is.EqualTo(2.4525).Within(1e-6)); yield break;
        }
        [UnityTest] public IEnumerator TelemetryCapturesPreparedForcesBeforeIntegrationAndDoesNotChangeMotion()
        {
            var b=Create(instant:true); var output=new StringWriter(); var csv=new FlightCsvWriter(output,new[]{"FL","FR","RR","RL"});
            int events=0; b.StepPrepared+=(source,dt)=> {
                var f=DroneTelemetryRecorder.Capture(source,dt); events++;
                Assert.That(f.TimeS,Is.EqualTo((events-1)*(double)IsolatedPhysicsRig.Dt).Within(1e-9));
                Assert.That(f.PositionWorld.Y,Is.EqualTo(100).Within(1e-4));
                Assert.That(f.Rotors[0].Thrust,Is.EqualTo(2.4525).Within(1e-6)); csv.Write(f);
            };
            rig.Step(100); Assert.That(csv.Rows,Is.EqualTo(100)); Assert.That(b.Body.linearVelocity.magnitude,Is.LessThan(.002)); yield break;
        }
        [UnityTest] public IEnumerator RecorderClosesFileAndSeparatesResetSegments()
        {
            var b=Create(instant:true); rig.Go.SetActive(false); var recorder=rig.Go.AddComponent<DroneTelemetryRecorder>(); recorder.recordOnEnable=false; rig.Go.SetActive(true);
            recorder.StartRecording(); string folder=recorder.RecordingFolder;
            try
            {
                Assert.That(recorder.IsRecording,Is.True); rig.Step(2); b.ResetMotorState(); rig.Step(); recorder.StopRecording();
                var lines=File.ReadAllLines(Path.Combine(folder,"flight.csv")); Assert.That(lines.Length,Is.EqualTo(4));
                Assert.That(lines[3],Does.StartWith("1,0,"));
                Assert.That(File.ReadAllText(Path.Combine(folder,"drone_profile.json")),Is.EqualTo(b.droneProfile.text));
                Assert.That(File.Exists(Path.Combine(folder,"manifest.json")),Is.True);
                using(var exclusive=new FileStream(Path.Combine(folder,"flight.csv"),FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {}
            }
            finally { recorder.StopRecording(); if(Directory.Exists(folder)) Directory.Delete(folder,true); }
            yield break;
        }
        [UnityTest] public IEnumerator HoverAndInstantFailureConvergeAtFiftyHundredAndTwoHundredHz()
        {
            double reference=0;
            foreach(float dt in new[]{.005f,.01f,.02f})
            {
                var b=Create(instant:true); rig.Step((int)System.Math.Round(1/dt),dt);
                Assert.That((b.Body.position-new Vector3(0,100,0)).magnitude,Is.LessThan(.002));
                b.SetRotorDriveAuthority(0,0); rig.Step((int)System.Math.Round(.1/dt),dt);
                if(reference==0) reference=b.Body.linearVelocity.y;
                Assert.That(b.Body.linearVelocity.y,Is.EqualTo(reference).Within(.03)); Assert.That(b.Body.angularVelocity.magnitude,Is.GreaterThan(.1));
                yield return rig.Dispose(); rig=null;
            }
        }
    }
}
