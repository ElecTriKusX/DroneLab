using System;
using System.Collections.Generic;
using DroneLab.Physics;
using DroneLab.Sensors;
using DroneLab.Simulation;
using UnityEngine;

namespace DroneLab.UI
{
    [DefaultExecutionOrder(50)]
    public sealed class DroneSensorRig : MonoBehaviour, INavigationFeedback
    {
        [Serializable] private sealed class Saved { public SensorSettings[] sensors; public double latitude, longitude; }
        private DronePhysicsBody body;
        private Vector3 origin, previousVelocity;
        private bool hasPrevious;
        private double previousTime = -1;
        private readonly RaycastHit[] hits = new RaycastHit[64];
        private readonly SensorChannel[] channels = new SensorChannel[7];
        private readonly List<string> events = new List<string>(64);
        public IReadOnlyList<string> Events => events;
        public double TimeS => body?.SimulationTimeS ?? 0;
        public double? GpsSpeedDerived { get; private set; }
        private SensorReading? previousGps;
        private DVector3 gpsVelocity;
        private readonly GpsNavigationFilter navigationGps=new GpsNavigationFilter();
        public Vector3 OriginWorld => origin;
        public bool Available(SensorKind kind)
        {
            var channel=Channel(kind); var reading=channel.Latest;
            return channel.Settings.enabled && !channel.Faulted && reading.HasValue && reading.Value.Valid &&
                TimeS-reading.Value.CapturedAt <= channel.Settings.latencyMs/1000 + Math.Max(.5,3/channel.Settings.frequencyHz);
        }
        public bool CameraAvailable => Settings(SensorKind.Camera).enabled && !Channel(SensorKind.Camera).Faulted;
        public bool TryHorizontal(out DVector3 position, out DVector3 velocity)
        {
            position=default; velocity=default;
            if(!Available(SensorKind.Gps)) return false;
            position=navigationGps.Predict(TimeS) + DronePhysicsBody.FromUnity(origin-body.Body.rotation*LocalPosition(SensorKind.Gps));
            velocity=navigationGps.Velocity; return true;
        }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double ReferenceAltitude { get; private set; }
        public double ReferencePressure { get; private set; }
        public double ReferenceTemperature { get; private set; }
        public Vector3 RangeOrigin { get; private set; }
        public Vector3 RangeEnd { get; private set; }
        public bool RangeHit { get; private set; }
        public DroneSensorCamera SensorCamera { get; private set; }
        public SensorChannel Channel(SensorKind kind) => channels[(int)kind];
        public SensorSettings Settings(SensorKind kind) => Channel(kind).Settings;
        public void Configure(DronePhysicsBody selectedBody, Camera sourceCamera)
        {
            body = selectedBody; origin = body.Body.position;
            ReferencePressure = body.Air.PressurePa; ReferenceTemperature = body.Air.TemperatureK; ReferenceAltitude = body.Air.AltitudeM;
            for (int i = 0; i < channels.Length; i++) channels[i] = new SensorChannel(SensorSettings.Default((SensorKind)i), i == (int)SensorKind.Camera ? 0 : i == (int)SensorKind.Barometer || i == (int)SensorKind.Rangefinder ? 1 : 3);
            Channel(SensorKind.Barometer).MinimumValue = 1;
            Channel(SensorKind.Rangefinder).MinimumValue = 0;
            // The camera starts outside the nose; rangefinder starts just beneath the hull.
            Settings(SensorKind.Camera).positionM[2] = body.Parameters.Dimensions.Z * .5 + .06;
            Settings(SensorKind.Rangefinder).positionM[1] = -body.Parameters.Dimensions.Y * .5 - .02;
            Load(); Channel(SensorKind.Rangefinder).MaximumValue = Settings(SensorKind.Rangefinder).maxRangeM; body.StepPrepared += SampleStep;
            Channel(SensorKind.Gps).Delivered += GpsDelivered;
            SensorCamera = gameObject.AddComponent<DroneSensorCamera>(); SensorCamera.Configure(this, body, sourceCamera);
            Log("Сессия начата. GPS: условная геопривязка; IMU: оси крепления; барометр: сухая атмосфера.");
        }
        public Vector3 LocalPosition(SensorKind kind) => DronePhysicsBody.ToUnity(DVector3.From(Settings(kind).positionM));
        public Quaternion LocalRotation(SensorKind kind) => Quaternion.Euler(DronePhysicsBody.ToUnity(DVector3.From(Settings(kind).rotationDeg)));
        public void Apply(SensorKind kind)
        {
            if (kind == SensorKind.Camera) Settings(kind).frequencyHz = Math.Min(30, Settings(kind).frequencyHz);
            Settings(kind).Validate(); Channel(kind).Apply(Settings(kind));
            if (kind == SensorKind.Rangefinder) Channel(kind).MaximumValue = Settings(kind).maxRangeM;
            if (kind == SensorKind.Accelerometer) hasPrevious = false;
            if (kind == SensorKind.Gps) { previousGps = null; gpsVelocity=default; navigationGps.Reset(); GpsSpeedDerived = null; }
            if (kind == SensorKind.Camera) SensorCamera?.ClearFrames();
            Save(); Log(Title(kind) + ": настройки изменены");
        }
        public void Preset(bool ideal)
        {
            for (int i = 0; i < channels.Length; i++) {
                var old = channels[i].Settings; var setting = SensorSettings.Default((SensorKind)i, ideal);
                setting.positionM = (double[])old.positionM.Clone(); setting.rotationDeg = (double[])old.rotationDeg.Clone();
                setting.maxRangeM = old.maxRangeM; setting.cameraFovDeg = old.cameraFovDeg;
                channels[i].Apply(setting);
            }
            hasPrevious = false; previousGps=null; gpsVelocity=default; navigationGps.Reset(); GpsSpeedDerived=null; SensorCamera?.ClearFrames(); Save(); Log(ideal ? "Датчики: идеальные" : "Датчики: с шумом");
            Channel(SensorKind.Rangefinder).MaximumValue = Settings(SensorKind.Rangefinder).maxRangeM;
        }
        public void SetFault(SensorKind kind, bool fault)
        { Channel(kind).SetFault(fault); if (kind == SensorKind.Gps) { previousGps = null; gpsVelocity=default; navigationGps.Reset(); GpsSpeedDerived = null; } if (kind == SensorKind.Camera) SensorCamera?.ClearFrames(); Log(Title(kind) + (fault ? ": отказ / потеря данных" : ": восстановлен")); }
        public void SetGeographicOrigin(double latitude, double longitude)
        { SensorMath.LocalToGeographic(default, latitude, longitude, 0); Latitude = latitude; Longitude = longitude; Save(); Log("Изменена условная геопривязка GPS"); }
        public void Log(string text)
        { if (events.Count >= 64) events.RemoveAt(0); events.Add((body?.SimulationTimeS ?? 0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " с · " + text); }
        private void SampleStep(DronePhysicsBody sender, float dt)
        {
            if (!body.IsReady || dt <= 0) return;
            // StepPrepared exposes the pose before integration; its clock has already advanced by dt.
            double now = Math.Max(0, body.SimulationTimeS - dt);
            if (now < previousTime - 1e-6) ResetReadings();
            foreach (var channel in channels) channel.Advance(now);
            var rotation = body.Body.rotation;
            var point = body.Body.position + rotation * LocalPosition(SensorKind.Accelerometer);
            var pointVelocity = body.Body.GetPointVelocity(point);
            var acceleration = hasPrevious && now > previousTime ? (pointVelocity - previousVelocity) / (float)(now - previousTime) : Vector3.zero;
            var accelWorld = SensorMath.SpecificForce(DronePhysicsBody.FromUnity(acceleration), body.Parameters.Gravity);
            Capture(SensorKind.Accelerometer, now, DronePhysicsBody.FromUnity(Quaternion.Inverse(rotation * LocalRotation(SensorKind.Accelerometer)) * DronePhysicsBody.ToUnity(accelWorld)), hasPrevious);
            Capture(SensorKind.Gyroscope, now, DronePhysicsBody.FromUnity(Quaternion.Inverse(rotation * LocalRotation(SensorKind.Gyroscope)) * body.Body.angularVelocity * Mathf.Rad2Deg), true);
            Capture(SensorKind.Magnetometer, now, DronePhysicsBody.FromUnity(Quaternion.Inverse(rotation * LocalRotation(SensorKind.Magnetometer)) * new Vector3(0, -40, 20)), true);
            var gpsPosition = body.Body.position + rotation * LocalPosition(SensorKind.Gps) - origin;
            Capture(SensorKind.Gps, now, DronePhysicsBody.FromUnity(gpsPosition), true);
            double height = body.Body.position.y + (rotation * LocalPosition(SensorKind.Barometer)).y - origin.y;
            bool supportedPressure = 1 - .0065 * height / ReferenceTemperature > 0;
            double pressure = supportedPressure ? SensorMath.PressureAtHeight(height, ReferencePressure, ReferenceTemperature, body.Parameters.Gravity) : 0;
            Capture(SensorKind.Barometer, now, new DVector3(pressure, 0, 0), supportedPressure);
            if (Channel(SensorKind.Rangefinder).Due(now)) {
                RangeOrigin = body.Body.position + rotation * LocalPosition(SensorKind.Rangefinder);
                var direction = rotation * LocalRotation(SensorKind.Rangefinder) * Vector3.forward;
                float distance = (float)Settings(SensorKind.Rangefinder).maxRangeM;
                int count = UnityEngine.Physics.RaycastNonAlloc(RangeOrigin, direction, hits, distance, body.groundLayers, QueryTriggerInteraction.Ignore);
                var selectedHits = count == hits.Length ? UnityEngine.Physics.RaycastAll(RangeOrigin, direction, distance, body.groundLayers, QueryTriggerInteraction.Ignore) : hits;
                if (!ReferenceEquals(selectedHits, hits)) count = selectedHits.Length;
                RangeHit = false;
                for (int i = 0; i < count; i++) {
                    var hit = selectedHits[i]; if (hit.collider == null || hit.collider.transform.IsChildOf(body.transform)) continue;
                    if (!RangeHit || hit.distance < distance) { distance = hit.distance; RangeHit = true; }
                }
                RangeEnd = RangeOrigin + direction * distance;
                Capture(SensorKind.Rangefinder, now, new DVector3(distance, 0, 0), RangeHit);
            }
            previousVelocity = pointVelocity; previousTime = now; hasPrevious = true;
            foreach (var channel in channels) channel.Advance(now);
        }
        private void Capture(SensorKind kind, double time, DVector3 truth, bool valid)
        { var channel = Channel(kind); if (channel.Due(time)) channel.Capture(time, truth, valid); }
        public void ResetReadings()
        { foreach (var channel in channels) channel.Reset(); previousTime = -1; hasPrevious = false; previousGps = null; gpsVelocity=default; navigationGps.Reset(); GpsSpeedDerived = null; SensorCamera?.ClearFrames(); Log("Возврат на старт: показания и очереди сброшены"); }
        private void GpsDelivered(SensorReading reading)
        {
            GpsSpeedDerived = null;
            if(reading.Valid) navigationGps.Feed(reading.Value,reading.CapturedAt,Settings(SensorKind.Gps).noiseStd);
            if (reading.Valid && previousGps.HasValue && previousGps.Value.Valid && reading.Sequence > previousGps.Value.Sequence && reading.CapturedAt > previousGps.Value.CapturedAt) {
                var delta = reading.Value - previousGps.Value.Value;
                double dt=reading.CapturedAt-previousGps.Value.CapturedAt;
                double alpha=1-Math.Exp(-dt/.6);
                gpsVelocity=gpsVelocity*(1-alpha)+new DVector3(delta.X/dt,0,delta.Z/dt)*alpha;
                GpsSpeedDerived=gpsVelocity.Length;
            }
            previousGps = reading;
        }
        public double BarometricHeight(double pressurePa) => SensorMath.HeightFromPressure(pressurePa, ReferencePressure, ReferenceTemperature, body.Parameters.Gravity);
        private string Preference => "DroneLab.Sensors.v1." + body.name;
        public void Save()
        {
            var saved = new Saved { sensors = new SensorSettings[channels.Length], latitude = Latitude, longitude = Longitude };
            for (int i = 0; i < channels.Length; i++) saved.sensors[i] = channels[i].Settings;
            PlayerPrefs.SetString(Preference, JsonUtility.ToJson(saved)); PlayerPrefs.Save();
        }
        private void Load()
        {
            try {
                var saved = JsonUtility.FromJson<Saved>(PlayerPrefs.GetString(Preference, ""));
                if (saved?.sensors == null || saved.sensors.Length != channels.Length) return;
                foreach (var setting in saved.sensors) { if (setting == null) return; setting.Validate(); }
                SensorMath.LocalToGeographic(default, saved.latitude, saved.longitude, 0);
                saved.sensors[(int)SensorKind.Camera].frequencyHz = Math.Min(30, saved.sensors[(int)SensorKind.Camera].frequencyHz);
                for (int i = 0; i < channels.Length; i++) channels[i].Apply(saved.sensors[i]);
                Latitude = saved.latitude; Longitude = saved.longitude;
            } catch (ArgumentException) { /* Invalid saved settings retain the complete defaults. */ }
        }
        public static string Title(SensorKind kind) => new[] { "GPS", "Акселерометр", "Гироскоп", "Магнитометр", "Барометр", "Дальномер", "Камера" }[(int)kind];
        public static string Unit(SensorKind kind) => new[] { "м · E/U/N", "м/с² · X/Y/Z", "°/с · X/Y/Z", "мкТл · X/Y/Z", "Па", "м вдоль луча", "пиксели / угол" }[(int)kind];
        private void OnDestroy() { if (body != null) body.StepPrepared -= SampleStep; }
    }
}
