using System;
using System.Collections.Generic;
using DroneLab.Physics;

namespace DroneLab.Sensors
{
    public enum SensorKind { Gps, Accelerometer, Gyroscope, Magnetometer, Barometer, Rangefinder, Camera }
    [Serializable]
    public sealed class SensorSettings
    {
        public bool enabled = true;
        public double frequencyHz = 20, noiseStd, latencyMs;
        public double[] bias = new double[3], positionM = new double[3], rotationDeg = new double[3];
        public double maxRangeM = 50, cameraFovDeg = 90;
        public int seed = 12345;
        public void Validate()
        {
            if (!Finite(frequencyHz) || frequencyHz < 1 || frequencyHz > 200 || !Finite(noiseStd) || noiseStd < 0 || noiseStd > 1000 ||
                !Finite(latencyMs) || latencyMs < 0 || latencyMs > 2000 || !Finite(maxRangeM) || maxRangeM < .1 || maxRangeM > 1000 ||
                !Finite(cameraFovDeg) || cameraFovDeg < 20 || cameraFovDeg > 140) throw new ArgumentOutOfRangeException("sensor settings");
            CheckVector(bias, 1000); CheckVector(positionM, 20); CheckVector(rotationDeg, 360);
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void CheckVector(double[] values, double limit)
        { if (values == null || values.Length != 3) throw new ArgumentException("Sensor vector must contain X/Y/Z."); foreach (double value in values) if (!Finite(value) || Math.Abs(value) > limit) throw new ArgumentOutOfRangeException("sensor vector"); }
        public static SensorSettings Default(SensorKind kind, bool ideal = false)
        {
            var rates = new[] { 10d, 100, 100, 50, 25, 20, 15 };
            var noise = new[] { .6, .03, .15, .2, 1.2, .01, 0 };
            var setting = new SensorSettings { frequencyHz = rates[(int)kind], noiseStd = ideal ? 0 : noise[(int)kind], latencyMs = ideal ? 0 : kind == SensorKind.Gps ? 100 : 20, seed = 12345 + (int)kind * 997 };
            if (kind == SensorKind.Camera) { setting.latencyMs = 0; setting.frequencyHz = 15; }
            if (kind == SensorKind.Rangefinder) setting.rotationDeg[0] = 90;
            return setting;
        }
    }
    public readonly struct SensorReading
    {
        public readonly long Sequence;
        public readonly double CapturedAt, DeliverAt;
        public readonly DVector3 Truth, Value;
        public readonly bool Valid;
        public SensorReading(long sequence, double captured, double delivered, DVector3 truth, DVector3 value, bool valid)
        { Sequence = sequence; CapturedAt = captured; DeliverAt = delivered; Truth = truth; Value = value; Valid = valid; }
    }
    /// <summary>Discrete measurements with bounded latency queue and an independent seeded noise stream.</summary>
    public sealed class SensorChannel
    {
        public SensorSettings Settings { get; private set; }
        public SensorReading? Latest { get; private set; }
        public bool Faulted { get; private set; }
        public double LastIntervalS { get; private set; }
        public long Sequence { get; private set; }
        public event Action<SensorReading> Delivered;
        private readonly Queue<SensorReading> pending = new Queue<SensorReading>();
        private Random random;
        private readonly int axes;
        public double? MinimumValue { get; set; }
        public double? MaximumValue { get; set; }
        private double nextCapture, previousCapture = -1, previousTime;
        public SensorChannel(SensorSettings settings, int axes = 3) { if (axes < 0 || axes > 3) throw new ArgumentOutOfRangeException(nameof(axes)); this.axes = axes; Apply(settings); }
        public void Apply(SensorSettings settings) { settings.Validate(); Settings = settings; Reset(); }
        public void Reset()
        { pending.Clear(); Latest = null; random = new Random(Settings.seed); nextCapture = 0; previousCapture = -1; previousTime = 0; LastIntervalS = 0; Sequence = 0; }
        public void SetFault(bool value) { Faulted = value; pending.Clear(); Latest = null; nextCapture = 0; }
        public bool Due(double now) => Settings.enabled && !Faulted && now + 1e-8 >= nextCapture;
        public SensorReading Capture(double now, DVector3 truth, bool valid)
        {
            SensorMath.Finite(now); if (now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            SensorMath.Finite(truth);
            if (!Settings.enabled || Faulted) throw new InvalidOperationException("Sensor is unavailable.");
            valid &= Settings.enabled && !Faulted;
            var bias = new DVector3(axes > 0 ? Settings.bias[0] : 0, axes > 1 ? Settings.bias[1] : 0, axes > 2 ? Settings.bias[2] : 0);
            var noise = new DVector3(axes > 0 ? Gaussian() : 0, axes > 1 ? Gaussian() : 0, axes > 2 ? Gaussian() : 0) * Settings.noiseStd;
            var value = truth + bias + noise;
            valid &= (!MinimumValue.HasValue || value.X >= MinimumValue.Value) && (!MaximumValue.HasValue || value.X <= MaximumValue.Value);
            var reading = new SensorReading(++Sequence, now, now + Settings.latencyMs / 1000, truth, valid ? value : default, valid);
            if (pending.Count >= 512) throw new InvalidOperationException("Sensor queue exceeded its sampling contract.");
            pending.Enqueue(reading); nextCapture = now + 1 / Settings.frequencyHz;
            LastIntervalS = previousCapture < 0 ? 0 : now - previousCapture; previousCapture = now;
            return reading;
        }
        public void Advance(double now)
        {
            SensorMath.Finite(now); if (now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            if (now < previousTime) Reset(); previousTime = now;
            if (!Settings.enabled || Faulted) { pending.Clear(); Latest = null; return; }
            while (pending.Count > 0 && pending.Peek().DeliverAt <= now + 1e-8) { Latest = pending.Dequeue(); Delivered?.Invoke(Latest.Value); }
        }
        private double Gaussian()
        { return Math.Sqrt(-2 * Math.Log(Math.Max(1e-12, random.NextDouble()))) * Math.Cos(2 * Math.PI * random.NextDouble()); }
    }
    public static class SensorMath
    {
        public static void Finite(double value) { if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
        public static void Finite(DVector3 value) { Finite(value.X); Finite(value.Y); Finite(value.Z); }
        public static DVector3 SpecificForce(DVector3 acceleration, double gravity) => acceleration + new DVector3(0, gravity, 0);
        public static double PressureAtHeight(double heightM, double referencePa, double referenceK, double gravity)
        {
            Finite(heightM); Finite(referencePa); Finite(referenceK); Finite(gravity);
            double ratio = 1 - .0065 * heightM / referenceK;
            if (ratio <= 0 || referencePa <= 0 || referenceK <= 0 || gravity <= 0) throw new ArgumentOutOfRangeException(nameof(heightM));
            return referencePa * Math.Pow(ratio, gravity / (287.05 * .0065));
        }
        public static double HeightFromPressure(double pressurePa, double referencePa, double referenceK, double gravity)
        { if (pressurePa <= 0 || referencePa <= 0 || referenceK <= 0 || gravity <= 0) return double.NaN; return referenceK / .0065 * (1 - Math.Pow(pressurePa / referencePa, 287.05 * .0065 / gravity)); }
        public static DVector3 LocalToGeographic(DVector3 eastUpNorth, double latitude, double longitude, double altitude)
        {
            Finite(eastUpNorth); Finite(latitude); Finite(longitude); Finite(altitude);
            if (Math.Abs(latitude) > 85 || Math.Abs(longitude) > 180) throw new ArgumentOutOfRangeException(nameof(latitude));
            double degreesPerMetre = 180 / (Math.PI * 6378137);
            return new DVector3(latitude + eastUpNorth.Z * degreesPerMetre,
                longitude + eastUpNorth.X * degreesPerMetre / Math.Cos(latitude * Math.PI / 180), altitude + eastUpNorth.Y);
        }
    }
}
