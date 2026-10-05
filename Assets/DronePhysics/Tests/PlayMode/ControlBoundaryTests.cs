using System.Collections;
using System.IO;
using DroneLab.Simulation;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DroneLab.Physics.Tests
{
    public sealed class ControlBoundaryTests
    {
        private IsolatedPhysicsRig rig;
        [UnityTearDown] public IEnumerator TearDown() { if(rig!=null) yield return rig.Dispose(); rig=null; }
        [UnityTest] public IEnumerator DirectMotorControlAndTelemetryWorkWithoutAnyPilot()
        {
            rig=new IsolatedPhysicsRig(instantaneous:true); var b=rig.Physics; b.SetArmed(true);
            for(int i=0;i<4;i++) b.SetMotorCommand(i,.5); rig.Step(100);
            var frame=DroneTelemetryRecorder.Capture(b,IsolatedPhysicsRig.Dt);
            Assert.That(frame.ControlMode,Is.EqualTo("None")); Assert.That(frame.AltitudeHold,Is.False);
            Assert.That(b.Body.linearVelocity.magnitude,Is.LessThan(.002)); Assert.That(rig.Go.GetComponent<DroneTestPilot>(),Is.Null); yield break;
        }
        [UnityTest] public IEnumerator RecorderDiscoversIndependentControlTelemetryAndSettings()
        {
            rig=new IsolatedPhysicsRig(instantaneous:true); rig.Go.SetActive(false);
            var external=rig.Go.AddComponent<IndependentControlTelemetry>();
            var recorder=rig.Go.AddComponent<DroneTelemetryRecorder>(); recorder.recordOnEnable=false; rig.Go.SetActive(true);
            var frame=DroneTelemetryRecorder.Capture(rig.Physics,IsolatedPhysicsRig.Dt,external);
            Assert.That(frame.ControlMode,Is.EqualTo("External")); Assert.That(frame.AltitudeHold,Is.True);
            Assert.That(frame.DesiredRateLocal.X,Is.EqualTo(.2)); Assert.That(frame.Input.Roll,Is.EqualTo(.1));
            recorder.StartRecording(); string folder=recorder.RecordingFolder;
            try
            {
                Assert.That(recorder.IsRecording,Is.True); rig.Step(); recorder.StopRecording();
                Assert.That(File.ReadAllLines(Path.Combine(folder,"flight.csv"))[1],Does.Contain(",External,"));
                var manifest=JObject.Parse(File.ReadAllText(Path.Combine(folder,"manifest.json")));
                Assert.That(JObject.Parse((string)manifest["controlSettings"])["source"].Value<string>(),Is.EqualTo("independent controller"));
            }
            finally { recorder.StopRecording(); if(Directory.Exists(folder)) Directory.Delete(folder,true); }
            yield break;
        }
    }
}
