using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using DroneLab.Physics;
using DroneLab.Sensors;
using DroneLab.Simulation;
using UnityEngine;

namespace DroneLab.UI
{
    internal sealed class DroneFlightRecording : IDisposable
    {
        private readonly DronePhysicsBody body;
        private readonly DroneTestPilot pilot;
        private readonly DroneSensorRig rig;
        private readonly Action<SensorReading>[] handlers = new Action<SensorReading>[7];
        private StreamWriter flightStream, sensorStream;
        private FlightCsvWriter flight;
        private double previousTime = -1;
        private int segment;
        private readonly RecordingCadence flightCadence=new RecordingCadence();
        private readonly RecordingCadence[] sensorCadences=Enumerable.Range(0,7).Select(_=>new RecordingCadence()).ToArray();
        private double intervalS=.1;
        public double IntervalS {
            get=>intervalS;
            set {
                if(double.IsNaN(value) || double.IsInfinity(value) || value<.02 || value>10) throw new ArgumentOutOfRangeException(nameof(value));
                if(value==intervalS) return;
                intervalS=value; flightCadence.Reset(); foreach(var cadence in sensorCadences) cadence.Reset();
                if(Active) rig.Log("Интервал записи изменён: "+value.ToString("0.00",CultureInfo.InvariantCulture)+" с");
            }
        }
        public float StoppedAt { get; private set; }=float.NegativeInfinity;
        public string Folder { get; private set; }
        public bool Active => flight != null;
        public long Rows => flight?.Rows ?? 0;
        public DroneFlightRecording(DronePhysicsBody selected, DroneTestPilot controller, DroneSensorRig sensors)
        { body = selected; pilot = controller; rig = sensors; }
        public void Start()
        {
            if (Active) return;
            try {
                Folder = Path.Combine(Application.persistentDataPath, "DroneLab", "Flights", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Folder);
                File.WriteAllText(Path.Combine(Folder, "drone_profile.json"), body.ActiveDroneJson, new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(Folder, "environment_profile.json"), body.ActiveEnvironmentJson, new UTF8Encoding(false));
                WriteSettings("sensors_start.json");
                flightStream = new StreamWriter(Path.Combine(Folder, "flight.csv"), false, new UTF8Encoding(false));
                sensorStream = new StreamWriter(Path.Combine(Folder, "sensors.csv"), false, new UTF8Encoding(false));
                sensorStream.WriteLine("segment,sensor,sequence,capture_s,delivery_s,valid,truth_x,truth_y,truth_z,measured_x,measured_y,measured_z");
                flight = new FlightCsvWriter(flightStream, body.Parameters.Rotors.Select(r => r.Id).ToArray());
                previousTime = -1; segment = 0; flightCadence.Reset(); foreach(var cadence in sensorCadences) cadence.Reset(); body.StepPrepared += Step;
                for (int i = 0; i < handlers.Length; i++) { var kind = (SensorKind)i; handlers[i] = reading => Sensor(kind, reading); rig.Channel(kind).Delivered += handlers[i]; }
                rig.Log("Начата запись полёта и датчиков в CSV");
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException) { Stop(); rig.Log("Не удалось начать запись: " + ex.Message); }
        }
        [Serializable] private sealed class SettingsExport { public SensorSettings[] channels; public double latitude, longitude, referencePressurePa, referenceTemperatureK, referenceAltitudeM, recordingIntervalS; public string conventions = "GPS E/U/N m relative start; IMU mount X/Y/Z; gyro deg/s; magnetic field uT; barometer Pa; range m along beam; camera width/height/FOV. Simplified sensor models, not calibrated hardware."; }
        private void WriteSettings(string name)
        {
            var settings = new SettingsExport { channels = Enumerable.Range(0, 7).Select(i => rig.Settings((SensorKind)i)).ToArray(), latitude = rig.Latitude, longitude = rig.Longitude,
                recordingIntervalS=IntervalS, referencePressurePa = rig.ReferencePressure, referenceTemperatureK = rig.ReferenceTemperature, referenceAltitudeM = rig.ReferenceAltitude };
            File.WriteAllText(Path.Combine(Folder, name), JsonUtility.ToJson(settings, true), new UTF8Encoding(false));
        }
        private void Step(DronePhysicsBody selected, float dt)
        {
            if (!Active) return;
            try {
                var frame = DroneTelemetryRecorder.Capture(selected, dt, pilot);
                if (frame.TimeS < previousTime) segment++;
                frame.Segment = segment; previousTime = frame.TimeS; if(!flightCadence.Due(frame.TimeS,IntervalS)) return; flight.Write(frame);
                if (flight.Rows % 50 == 0) { flight.Flush(); sensorStream.Flush(); }
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { Stop(); rig.Log("Запись остановлена: " + ex.Message); }
        }
        private void Sensor(SensorKind kind, SensorReading reading)
        {
            if (!Active || sensorStream == null || !sensorCadences[(int)kind].Due(reading.CapturedAt,IntervalS)) return;
            try {
                var values = new[] { reading.CapturedAt, reading.DeliverAt, reading.Valid ? 1d : 0, reading.Truth.X, reading.Truth.Y, reading.Truth.Z, reading.Value.X, reading.Value.Y, reading.Value.Z };
                int readingSegment = body.SimulationTimeS - Time.fixedDeltaTime < previousTime - 1e-6 ? segment + 1 : segment;
                sensorStream.WriteLine(readingSegment + "," + kind + "," + reading.Sequence + "," + string.Join(",", values.Select(x => x.ToString("R", CultureInfo.InvariantCulture))));
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { Stop(); rig.Log("Ошибка записи датчиков: " + ex.Message); }
        }
        public void Stop()
        {
            bool wasActive = Active; if(wasActive) StoppedAt=Time.unscaledTime; body.StepPrepared -= Step;
            for (int i = 0; i < handlers.Length; i++) if (handlers[i] != null) { rig.Channel((SensorKind)i).Delivered -= handlers[i]; handlers[i] = null; }
            flight = null;
            try { flightStream?.Dispose(); sensorStream?.Dispose(); if (wasActive) { WriteSettings("sensors_end.json"); File.WriteAllLines(Path.Combine(Folder, "events.txt"), rig.Events, new UTF8Encoding(false)); rig.Log("Запись завершена: " + Folder); } }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { rig.Log("Ошибка завершения записи: " + ex.Message); }
            finally { flightStream = sensorStream = null; }
        }
        public void Dispose() => Stop();
    }
}
