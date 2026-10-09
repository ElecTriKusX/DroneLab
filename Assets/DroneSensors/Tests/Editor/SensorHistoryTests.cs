using System.Collections.Generic;
using DroneLab.Physics;
using NUnit.Framework;

namespace DroneLab.Sensors.Tests
{
    public sealed class SensorHistoryTests
    {
        private static SensorReading Reading(long sequence, double captured, bool valid = true)
            => new SensorReading(sequence, captured, captured + .1, new DVector3(sequence, 0, 0), new DVector3(sequence, 0, 0), valid);

        [TestCase(5, 1001)] [TestCase(20, 4001)] [TestCase(60, 12001)]
        public void HighRateHistoryCoversTheWholeRequestedPeriod(double period, int expected)
        {
            var history = new SensorHistory(); var visible = new List<SensorReading>();
            for (int i = 0; i <= 14000; i++) history.Add(Reading(i + 1, i / 200d));
            history.CopyWindowTo(visible, 70, period);
            Assert.That(visible.Count, Is.EqualTo(expected));
            Assert.That(visible[0].CapturedAt, Is.EqualTo(70 - period));
            Assert.That(visible[visible.Count - 1].CapturedAt, Is.EqualTo(70));
            Assert.That(history.Count, Is.LessThanOrEqualTo(SensorHistory.MaximumSamples));
        }
        [Test] public void InvalidSamplesAndCaptureTimestampsSurviveForGapDrawing()
        {
            var history = new SensorHistory(); var visible = new List<SensorReading>();
            history.Add(Reading(1, 10)); history.Add(Reading(2, 10.1, false)); history.Add(Reading(3, 10.2));
            history.CopyWindowTo(visible, 10.2, 5);
            Assert.That(visible.Count, Is.EqualTo(3)); Assert.That(visible[1].Valid, Is.False);
            Assert.That(visible[0].CapturedAt, Is.EqualTo(10)); Assert.That(visible[0].DeliverAt, Is.EqualTo(10.1));
        }
        [Test] public void ResetSequenceAtTheSameTimeDiscardsOldSession()
        {
            var history = new SensorHistory(); var visible = new List<SensorReading>();
            history.Add(Reading(99, 10)); history.Add(Reading(1, 10)); history.CopyWindowTo(visible, 10, 20);
            Assert.That(visible.Count, Is.EqualTo(1)); Assert.That(visible[0].Sequence, Is.EqualTo(1));
        }
        [Test] public void ClockRewindDiscardsPreviousFlight()
        {
            var history = new SensorHistory(); var visible = new List<SensorReading>();
            history.Add(Reading(1, 50)); history.Add(Reading(2, 1)); history.CopyWindowTo(visible, 1, 20);
            Assert.That(visible.Count, Is.EqualTo(1)); Assert.That(visible[0].CapturedAt, Is.EqualTo(1));
        }
        [Test] public void DisabledChannelHistoryAgesOutWithoutNewSamples()
        {
            var history = new SensorHistory(); var visible = new List<SensorReading>();
            history.Add(Reading(1, 0)); history.CopyWindowTo(visible, 30, 20);
            Assert.That(visible, Is.Empty);
        }
        [Test] public void WindowChangesDoNotEraseLongerHistory()
        {
            var history = new SensorHistory(); var visible = new List<SensorReading>();
            for (int i = 0; i <= 60; i++) history.Add(Reading(i + 1, i));
            history.CopyWindowTo(visible, 60, 5); Assert.That(visible.Count, Is.EqualTo(6));
            history.CopyWindowTo(visible, 60, 60); Assert.That(visible.Count, Is.EqualTo(61));
        }
        [Test] public void CorruptExcessiveRatesCannotGrowStorageWithoutBound()
        {
            var history = new SensorHistory();
            for (int i = 0; i < 20000; i++) history.Add(Reading(i + 1, 1));
            Assert.That(history.Count, Is.EqualTo(SensorHistory.MaximumSamples));
        }
    }
}
