using System;
using System.Collections.Generic;
using DroneLab.Simulation;
using DroneLab.Sensors;
using UnityEngine;

namespace DroneLab.UI
{
    /// <summary>Physical snapshot or separate sensor-driven pilot snapshot; physics diagnostics keep the physical snapshot.</summary>
    internal sealed class DroneFlightTelemetry
    {
        private readonly DronePhysicsBody body;
        private readonly Vector3 start;
        private readonly RaycastHit[] ground = new RaycastHit[64];
        private readonly List<Vector3> trail = new List<Vector3>(2048);
        private float nextTrailTime;
        private double previousTime;
        public IReadOnlyList<Vector3> Trail => trail;
        public Vector3 Start => start;
        public Vector3 Position { get; private set; }
        public float Heading { get; private set; }
        public float Pitch { get; private set; }
        public float Roll { get; private set; }
        public float GroundSpeed { get; private set; }
        public float VerticalSpeed { get; private set; }
        public float RelativeHeight { get; private set; }
        public float? SurfaceDistance { get; private set; }
        public float HomeDistance { get; private set; }
        public double? BatteryPercent { get; private set; }
        public double? Voltage { get; private set; }
        public double? MotorTemperatureC { get; private set; }
        public double TimeS { get; private set; }
        public Vector3 Wind { get; private set; }
        public bool Armed { get; private set; }
        public bool PowerLimited { get; private set; }
        public bool MotorFault { get; private set; }
        public bool Saturated { get; private set; }
        public bool ResetDetected { get; private set; }
        public bool GpsValid { get; private set; }=true;
        public bool HeightValid { get; private set; }=true;
        public bool AttitudeValid { get; private set; }=true;
        public bool HeadingValid { get; private set; }=true;
        private long gpsSequence=-1, baroSequence=-1;
        private double lastBaroTime;
        private float lastHeight;
        private bool gpsGap;
        public void ApplySensors(DroneFlightTelemetry truth, DroneSensorRig rig)
        {
            Armed=truth.Armed; TimeS=truth.TimeS; Wind=truth.Wind; MotorFault=truth.MotorFault;
            PowerLimited=truth.PowerLimited; Saturated=truth.Saturated; BatteryPercent=truth.BatteryPercent;
            Voltage=truth.Voltage; MotorTemperatureC=truth.MotorTemperatureC;
            if(truth.ResetDetected) { trail.Clear(); gpsSequence=baroSequence=-1; gpsGap=false; VerticalSpeed=0; }
            GpsValid=rig.TryHorizontal(out var pos,out var vel);
            if(GpsValid) {
                Position=rig.OriginWorld+DronePhysicsBody.ToUnity(rig.Channel(SensorKind.Gps).Latest.Value.Value)-body.Body.rotation*rig.LocalPosition(SensorKind.Gps); GroundSpeed=(float)vel.Length;
                HomeDistance=new Vector2(Position.x-start.x,Position.z-start.z).magnitude;
                var reading=rig.Channel(SensorKind.Gps).Latest.Value;
                if(reading.Sequence!=gpsSequence) {
                    if(gpsGap && trail.Count>0) trail.Add(new Vector3(float.NaN,float.NaN,float.NaN));
                    if(trail.Count>=2048) trail.RemoveRange(0,512);
                    trail.Add(Position); gpsSequence=reading.Sequence; gpsGap=false;
                }
            } else gpsGap=true;
            HeightValid=rig.Available(SensorKind.Barometer);
            if(HeightValid) {
                var r=rig.Channel(SensorKind.Barometer).Latest.Value;
                RelativeHeight=(float)rig.BarometricHeight(r.Value.X);
                if(r.Sequence!=baroSequence) {
                    if(baroSequence>=0 && r.CapturedAt>lastBaroTime) {
                        double dt=r.CapturedAt-lastBaroTime;
                        VerticalSpeed=Mathf.Lerp(VerticalSpeed,(RelativeHeight-lastHeight)/(float)dt,(float)(1-Math.Exp(-dt/.8)));
                    }
                    lastBaroTime=r.CapturedAt; lastHeight=RelativeHeight; baroSequence=r.Sequence;
                }
            } else baroSequence=-1;
            SurfaceDistance=rig.Available(SensorKind.Rangefinder) ? (float?)rig.Channel(SensorKind.Rangefinder).Latest.Value.Value.X : null;
            AttitudeValid=rig.Available(SensorKind.Accelerometer) && rig.Available(SensorKind.Gyroscope);
            if(AttitudeValid) { Pitch=truth.Pitch; Roll=truth.Roll; }
            HeadingValid=AttitudeValid && rig.Available(SensorKind.Magnetometer);
            if(HeadingValid) {
                var field=rig.LocalRotation(SensorKind.Magnetometer)*DronePhysicsBody.ToUnity(rig.Channel(SensorKind.Magnetometer).Latest.Value.Value);
                var tilt=Quaternion.Inverse(Quaternion.Euler(0,truth.Heading,0))*body.Body.rotation;
                var level=tilt*field;
                Heading=Mathf.Repeat(Mathf.Atan2(-level.x,level.z)*Mathf.Rad2Deg,360);
            }
        }
        public DroneFlightTelemetry(DronePhysicsBody selectedBody)
        { body = selectedBody; start = selectedBody.Body.position; Position = start; trail.Add(start); }
        public void Sample(DroneTestPilot pilot)
        {
            if (body == null || !body.IsReady || body.Body == null) return;
            Position = body.Body.position;
            var attitude = body.Body.rotation;
            Heading = DroneFlightMath.Heading(attitude * Vector3.forward, Heading);
            Pitch = DroneFlightMath.Pitch(attitude); Roll = DroneFlightMath.Roll(attitude);
            var velocity = body.Body.linearVelocity;
            GroundSpeed = new Vector2(velocity.x, velocity.z).magnitude; VerticalSpeed = velocity.y;
            RelativeHeight = Position.y - start.y;
            HomeDistance = new Vector2(Position.x - start.x, Position.z - start.z).magnitude;
            TimeS = body.SimulationTimeS; Wind = body.WindVelocityWorld;
            Armed = body.Armed; MotorFault = body.Drive != null && body.Drive.HasFault;
            PowerLimited = body.Power != null && body.Power.Limited;
            Saturated = pilot != null && pilot.Saturated;
            BatteryPercent = body.Power == null ? (double?)null : body.Power.Soc * 100;
            Voltage = body.Power == null ? (double?)null : body.Power.TerminalVoltage;
            MotorTemperatureC = null;
            if (body.Power?.Thermal != null)
                for (int i = 0; i < body.Parameters.Rotors.Count; i++)
                {
                    double temperature = body.Power.Thermal.Motor(i).TemperatureK - 273.15;
                    MotorTemperatureC = Math.Max(MotorTemperatureC ?? temperature, temperature);
                }
            // Physical distance vertically below the body origin; not a mounted sensor's slant distance.
            SurfaceDistance = null;
            int count = UnityEngine.Physics.RaycastNonAlloc(Position, Vector3.down, ground, 10000,
                body.groundLayers, QueryTriggerInteraction.Ignore);
            var hits = count == ground.Length ? UnityEngine.Physics.RaycastAll(Position, Vector3.down, 10000,
                body.groundLayers, QueryTriggerInteraction.Ignore) : ground;
            if (!ReferenceEquals(hits, ground)) count = hits.Length;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(body.transform)) continue;
                SurfaceDistance = Mathf.Min(SurfaceDistance ?? hit.distance, hit.distance);
            }
            ResetDetected = TimeS < previousTime;
            if (ResetDetected) { trail.Clear(); trail.Add(start); nextTrailTime = 0; }
            previousTime = TimeS;
            if (TimeS >= nextTrailTime)
            {
                nextTrailTime = (float)TimeS + .5f;
                if (trail.Count == 0 || (Position - trail[trail.Count - 1]).sqrMagnitude > .25f)
                {
                    // Bounded session memory; decimate old points instead of silently dropping the origin.
                    if (trail.Count >= 2048)
                        for (int i = trail.Count - 2; i > 0; i -= 2) trail.RemoveAt(i);
                    trail.Add(Position);
                }
            }
        }
    }
}
