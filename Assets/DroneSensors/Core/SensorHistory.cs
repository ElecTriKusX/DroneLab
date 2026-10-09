using System;
using System.Collections.Generic;

namespace DroneLab.Sensors
{
    /// <summary>Retains sixty seconds at the maximum supported 200 Hz, including invalid samples as gaps.</summary>
    public sealed class SensorHistory
    {
        public const double RetentionS = 60;
        public const int MaximumSamples = 12064;
        private readonly Queue<SensorReading> samples = new Queue<SensorReading>();
        private SensorReading? last;
        public int Count => samples.Count;
        public void Add(SensorReading reading)
        {
            if (last.HasValue && (reading.Sequence <= last.Value.Sequence || reading.CapturedAt < last.Value.CapturedAt)) Clear();
            samples.Enqueue(reading); last = reading;
            while (samples.Count > MaximumSamples || (samples.Count > 1 && reading.CapturedAt - samples.Peek().CapturedAt > RetentionS)) samples.Dequeue();
        }
        public void Clear() { samples.Clear(); last = null; }
        public void CopyWindowTo(List<SensorReading> target, double end, double period)
        {
            target.Clear();
            double start = end - Math.Max(0, Math.Min(RetentionS, period));
            foreach (var sample in samples) if (sample.CapturedAt >= start - 1e-8 && sample.CapturedAt <= end + 1e-8) target.Add(sample);
        }
    }
}
