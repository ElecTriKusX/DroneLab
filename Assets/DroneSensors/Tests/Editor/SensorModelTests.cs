using System;
using DroneLab.Physics;
using DroneLab.Sensors;
using NUnit.Framework;

namespace DroneLab.Sensors.Tests
{
    public sealed class SensorModelTests
    {
        [Test] public void LatencyDeliversOriginalTruthRatherThanCurrentTruth()
        {
            var channel = new SensorChannel(new SensorSettings { latencyMs = 100, frequencyHz = 20 });
            channel.Capture(0, new DVector3(1, 2, 3), true); channel.Advance(.099);
            Assert.That(channel.Latest.HasValue, Is.False);
            channel.Capture(.05, new DVector3(99, 0, 0), true); channel.Advance(.1);
            Assert.That(channel.Latest.Value.Truth.X, Is.EqualTo(1)); Assert.That(channel.Latest.Value.Value.X, Is.EqualTo(1));
            Assert.That(channel.Latest.Value.CapturedAt, Is.Zero); Assert.That(channel.Latest.Value.DeliverAt, Is.EqualTo(.1));
        }
        [Test] public void SamplingGateDoesNotInventSubstepSamples()
        {
            var channel = new SensorChannel(new SensorSettings { frequencyHz = 10 });
            Assert.That(channel.Due(0), Is.True); channel.Capture(0, default, true);
            Assert.That(channel.Due(.05), Is.False); Assert.That(channel.Due(.1), Is.True);
            channel.Capture(.12, default, true); Assert.That(channel.LastIntervalS, Is.EqualTo(.12));
        }
        [Test] public void ScalarMeasurementDoesNotInjectImaginaryOtherAxes()
        {
            var channel = new SensorChannel(new SensorSettings { noiseStd = 1, bias = new[] { 2d, 99, 99 } }, 1);
            channel.Capture(0, new DVector3(10, 0, 0), true); channel.Advance(0);
            Assert.That(channel.Latest.Value.Value.Y, Is.Zero); Assert.That(channel.Latest.Value.Value.Z, Is.Zero);
        }
        [Test] public void FaultClearsQueuedValuesAndRecoveryWaitsForFreshData()
        {
            var channel = new SensorChannel(new SensorSettings { latencyMs = 500 });
            channel.Capture(0, new DVector3(123, 0, 0), true); channel.SetFault(true); channel.Advance(1);
            Assert.That(channel.Latest.HasValue, Is.False); Assert.That(channel.Due(1), Is.False);
            channel.SetFault(false); channel.Advance(2); Assert.That(channel.Latest.HasValue, Is.False);
            channel.Capture(2, new DVector3(456, 0, 0), true); channel.Advance(2.5);
            Assert.That(channel.Latest.Value.Value.X, Is.EqualTo(456));
        }
        [Test] public void TimeRewindClearsDelayQueue()
        {
            var channel = new SensorChannel(new SensorSettings { latencyMs = 500 });
            channel.Advance(10); channel.Capture(10, new DVector3(123, 0, 0), true); channel.Advance(0); channel.Advance(11);
            Assert.That(channel.Latest.HasValue, Is.False);
        }
        [Test] public void SameSeedAndResetProduceIdenticalNoise()
        {
            var a = new SensorChannel(new SensorSettings { noiseStd = .5, seed = 17 }); var b = new SensorChannel(new SensorSettings { noiseStd = .5, seed = 17 });
            var first = a.Capture(0, default, true); var second = b.Capture(0, default, true);
            Assert.That(first.Value.X, Is.EqualTo(second.Value.X)); Assert.That(first.Value.Y, Is.EqualTo(second.Value.Y));
            a.Reset(); Assert.That(a.Capture(0, default, true).Value.X, Is.EqualTo(first.Value.X));
        }
        [Test] public void GaussianNoiseHasDeclaredMeanAndStandardDeviation()
        {
            var channel = new SensorChannel(new SensorSettings { frequencyHz = 100, noiseStd = 2, bias = new[] { 3d, 0, 0 } }, 1);
            double sum = 0, square = 0; const int count = 20000;
            for (int i = 0; i < count; i++) { double time = i * .01; double sample = channel.Capture(time, default, true).Value.X; channel.Advance(time); sum += sample; square += sample * sample; }
            double mean = sum / count, deviation = Math.Sqrt(square / count - mean * mean);
            Assert.That(mean, Is.EqualTo(3).Within(.06)); Assert.That(deviation, Is.EqualTo(2).Within(.05));
        }
        [Test] public void InvalidRangeIsNotReportedAsAValidZero()
        {
            var channel = new SensorChannel(new SensorSettings { bias = new[] { -2d, 0, 0 } }, 1) { MinimumValue = 0, MaximumValue = 50 };
            channel.Capture(0, new DVector3(1, 0, 0), true); channel.Advance(0); Assert.That(channel.Latest.Value.Valid, Is.False);
        }
        [Test] public void MissingRayHitStaysInvalidWithoutNoiseFabrication()
        {
            var channel = new SensorChannel(new SensorSettings { noiseStd = 1 }, 1);
            channel.Capture(0, new DVector3(50, 0, 0), false); channel.Advance(0);
            Assert.That(channel.Latest.Value.Valid, Is.False); Assert.That(channel.Latest.Value.Truth.X, Is.EqualTo(50));
        }
        [Test] public void AccelerometerDistinguishesSupportFromFreeFall()
        {
            Assert.That(SensorMath.SpecificForce(default, 9.81).Y, Is.EqualTo(9.81));
            Assert.That(SensorMath.SpecificForce(new DVector3(0, -9.81, 0), 9.81).Length, Is.Zero);
        }
        [TestCase(-100)] [TestCase(0)] [TestCase(500)] [TestCase(3000)]
        public void BarometerPressureInvertsRelativeHeight(double height)
        { double pressure = SensorMath.PressureAtHeight(height, 98700, 295, 9.81); Assert.That(SensorMath.HeightFromPressure(pressure, 98700, 295, 9.81), Is.EqualTo(height).Within(1e-7)); }
        [Test] public void GpsLocalEastNorthAndUpHaveIndependentGeographicEffects()
        {
            var point = SensorMath.LocalToGeographic(new DVector3(100, 20, 200), 57, 65, 100);
            Assert.That(point.X, Is.GreaterThan(57)); Assert.That(point.Y, Is.GreaterThan(65)); Assert.That(point.Z, Is.EqualTo(120));
        }
        [Test] public void CameraMetadataDoesNotAcquirePixelNoise()
        {
            var channel = new SensorChannel(new SensorSettings { noiseStd = 100, bias = new[] { 500d, 500, 500 } }, 0);
            channel.Capture(0, new DVector3(320, 180, 90), true); channel.Advance(0);
            Assert.That(channel.Latest.Value.Value.X, Is.EqualTo(320)); Assert.That(channel.Latest.Value.Value.Z, Is.EqualTo(90));
        }
        [Test] public void DisabledSensorDoesNotDeliverStaleData()
        {
            var settings = new SensorSettings { latencyMs = 100 }; var channel = new SensorChannel(settings);
            channel.Capture(0, default, true); settings.enabled = false; channel.Advance(1);
            Assert.That(channel.Latest.HasValue, Is.False);
        }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(0)] [TestCase(201)]
        public void InvalidSamplingRateIsRejected(double rate)
        { Assert.Throws<ArgumentOutOfRangeException>(() => new SensorChannel(new SensorSettings { frequencyHz = rate })); }
        [Test] public void MaximumLatencyAndRateRemainWithinBoundedQueue()
        {
            var channel = new SensorChannel(new SensorSettings { frequencyHz = 200, latencyMs = 2000 });
            for (int i = 0; i < 1000; i++) { double time = i * .005; channel.Advance(time); if (channel.Due(time)) channel.Capture(time, default, true); }
            Assert.That(channel.Latest.HasValue, Is.True);
        }
    }
}
