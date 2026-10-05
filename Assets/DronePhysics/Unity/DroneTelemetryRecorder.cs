using System;
using System.IO;
using System.Linq;
using System.Text;
using DroneLab.Physics;
using UnityEngine;

namespace DroneLab.Simulation
{
    [DisallowMultipleComponent,RequireComponent(typeof(DronePhysicsBody))]
    public sealed class DroneTelemetryRecorder : MonoBehaviour
    {
        public bool recordOnEnable=true;
        [Min(1)] public int flushEveryRows=100;
        public string RecordingFolder { get; private set; }
        public bool IsRecording=>csv!=null;
        public long Rows=>csv?.Rows ?? completedRows;
        private long completedRows;
        private DronePhysicsBody body;
        private IFlightControlTelemetry pilot;
        private StreamWriter stream;
        private FlightCsvWriter csv;
        private RuntimeDroneParameters recordedParameters;
        private double previousTime=-1;
        private int segment;
        [Serializable] private sealed class Manifest
        {
            public string telemetryVersion=FlightCsvWriter.FormatVersion,utcStarted,unityVersion,scene;
            public string droneProfileFile="drone_profile.json",environmentProfileFile="environment_profile.json";
            public string sampleConvention="pose/velocity before integration; RPM/forces for current step; SOC/charge/energy after step";
            public float fixedDeltaTime;
            public string[] rotorIds;
            public string customWindProvider;
            public string customWindComponentSettings;
            public double environmentReferenceWorldY;
            public string controlSettings;
        }
        private void OnEnable()
        {
            if(!Application.isPlaying) return;
            body=GetComponent<DronePhysicsBody>(); pilot=GetComponents<MonoBehaviour>().OfType<IFlightControlTelemetry>().FirstOrDefault();
            body.StepPrepared+=OnStep;
            if(recordOnEnable && body.IsReady) StartRecording();
        }
        private void OnDisable() { if(body!=null) body.StepPrepared-=OnStep; StopRecording(); }
        private void Start() { if(recordOnEnable && !IsRecording) StartRecording(); }
        [ContextMenu("Start recording")]
        public void StartRecording()
        {
            if(!Application.isPlaying || body==null || !body.IsReady || IsRecording) return;
            try
            {
                RecordingFolder=Path.Combine(Application.persistentDataPath,"DroneLab","Flights",DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(RecordingFolder);
                File.WriteAllText(Path.Combine(RecordingFolder,"drone_profile.json"),body.ActiveDroneJson,new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(RecordingFolder,"environment_profile.json"),body.ActiveEnvironmentJson,new UTF8Encoding(false));
                var ids=body.Parameters.Rotors.Select(r=>r.Id).ToArray();
                var manifest=new Manifest { utcStarted=DateTime.UtcNow.ToString("O"),unityVersion=Application.unityVersion,scene=gameObject.scene.name,
                    fixedDeltaTime=Time.fixedDeltaTime,rotorIds=ids,customWindProvider=body.CustomWindProvider?.GetType().FullName ?? body.customWindProvider?.GetType().FullName ?? "None",
                    customWindComponentSettings=body.customWindProvider==null ? "None" : JsonUtility.ToJson(body.customWindProvider),environmentReferenceWorldY=body.EnvironmentReferenceWorldY,
                    controlSettings=pilot==null ? "None" : pilot.ExportSettingsJson() };
                File.WriteAllText(Path.Combine(RecordingFolder,"manifest.json"),JsonUtility.ToJson(manifest,true),new UTF8Encoding(false));
                stream=new StreamWriter(Path.Combine(RecordingFolder,"flight.csv"),false,new UTF8Encoding(false));
                csv=new FlightCsvWriter(stream,ids); recordedParameters=body.Parameters; previousTime=-1; segment=0; completedRows=0;
                Debug.Log("DroneLab recording: "+RecordingFolder,this);
            }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { StopRecording(); Debug.LogError("DroneLab recording could not start: "+ex.Message,this); }
        }
        [ContextMenu("Stop recording")]
        public void StopRecording()
        {
            completedRows=csv?.Rows ?? completedRows; csv=null;
            try { stream?.Dispose(); }
            catch(IOException ex) { Debug.LogError("DroneLab recording could not close: "+ex.Message,this); }
            finally { stream=null; }
        }
        private void OnStep(DronePhysicsBody source,float dt)
        {
            if(!IsRecording) return;
            if(recordedParameters!=source.Parameters) { StopRecording(); StartRecording(); if(!IsRecording) return; }
            try
            {
                var frame=Capture(source,dt,pilot);
                if(frame.TimeS<=previousTime) segment++;
                frame.Segment=segment; previousTime=frame.TimeS; csv.Write(frame);
                if(csv.Rows%Math.Max(1,flushEveryRows)==0) csv.Flush();
            }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            { StopRecording(); Debug.LogError("DroneLab recording stopped: "+ex.Message,this); }
        }
        // Called only from StepPrepared: no additional forces, no wind queries or power updates.
        public static FlightTelemetryFrame Capture(DronePhysicsBody b,float dt,IFlightControlTelemetry p=null)
        {
            var parameters=b.Parameters; var q=b.Body.rotation; var power=b.Power;
            var f=new FlightTelemetryFrame { TimeS=b.SimulationTimeS-dt,Dt=dt,Armed=b.Armed,Mass=parameters.Mass,Gravity=parameters.Gravity,
                Density=b.Air.Density,AltitudeM=b.Air.AltitudeM,PositionWorld=DronePhysicsBody.FromUnity(b.Body.worldCenterOfMass),
                VelocityWorld=DronePhysicsBody.FromUnity(b.Body.linearVelocity),AngularVelocityLocal=DronePhysicsBody.FromUnity(b.transform.InverseTransformDirection(b.Body.angularVelocity)),
                RotationX=q.x,RotationY=q.y,RotationZ=q.z,RotationW=q.w,
                WindWorld=DronePhysicsBody.FromUnity(b.WindVelocityWorld),BodyDragWorld=DronePhysicsBody.FromUnity(b.DragForce),BodyTorqueWorld=DronePhysicsBody.FromUnity(b.DragTorque),
                ControlMode=p?.ControlMode ?? "None",AltitudeHold=p?.AltitudeHold ?? false,Saturated=p?.Saturated ?? false,
                Input=p?.CurrentInput ?? default,DesiredRateLocal=p?.DesiredRateLocal ?? default,
                PowerLimited=power?.Limited ?? false,HasDriveFault=b.Drive.HasFault,Soc=power?.Soc,Voltage=power?.TerminalVoltage,Current=power?.Current,
                ElectricalPower=power?.ElectricalPower,MechanicalPower=power?.MechanicalPower,ConsumedAh=power?.ConsumedAh,EnergyJ=power?.TerminalEnergyJ,
                PropellerPower=power?.PropellerPower,SpinEnergyRate=parameters.InertialRotors ? power?.SpinEnergyChangePower : null,
                SpinBalanceError=parameters.InertialRotors ? power?.SpinBalanceErrorPower : null,
                MotorLoss=power?.MotorLossPower,EscLoss=power?.EscLossPower,GyroscopicMomentWorld=DronePhysicsBody.FromUnity(b.RotorGyroscopicMoment),
                AmbientTemperature=b.Air.TemperatureK>0 ? (double?)b.Air.TemperatureK : null,
                BatteryTemperature=power?.Thermal?.Battery.TemperatureK,BatteryThermalAuthority=power?.Thermal?.Battery.Authority,
                ThermalDerated=power?.ThermalDerated ?? false,ThermalGeneratedEnergy=power?.Thermal?.GeneratedEnergyJ,
                ThermalRejectedEnergy=power?.Thermal?.RejectedEnergyJ,ThermalStoredEnergy=power?.Thermal?.StoredEnergyJ,
                Precipitation=parameters.Environment.Precipitation,PrecipitationIntensity=parameters.Environment.PrecipitationIntensityMmPerHour,
                Rotors=new RotorTelemetry[parameters.Rotors.Count] };
            for(int i=0;i<f.Rotors.Length;i++) f.Rotors[i]=b.GetRotorTelemetry(i);
            return f;
        }
    }
}
