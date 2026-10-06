using System;
using DroneLab.Physics;
using NUnit.Framework;

namespace DroneLab.Weather.Tests
{
    public sealed class WeatherConversionTests
    {
        [Test] public void EnviroUsesNegativeWindZoneDirectionAndNormalizesMagnitude()
        {
            var wind = WindConversion.FromEnviro(.25, .6, .8, 20);
            Assert.That(wind.X, Is.EqualTo(-3).Within(1e-10));
            Assert.That(wind.Z, Is.EqualTo(-4).Within(1e-10)); Assert.That(wind.Length, Is.EqualTo(5).Within(1e-10));
        }
        [TestCase(0, 0, -5)] [TestCase(90, -5, 0)] [TestCase(180, 0, 5)] [TestCase(270, 5, 0)]
        public void MeteorologicalFromBearingBecomesVelocityTowards(double bearing, double x, double z)
        {
            var wind = WindConversion.FromMeteorological(5, bearing);
            Assert.That(wind.X, Is.EqualTo(x).Within(1e-10)); Assert.That(wind.Z, Is.EqualTo(z).Within(1e-10));
            var mapped = WindConversion.ToEnviro(wind, 20);
            var roundtrip = WindConversion.FromEnviro(mapped.Strength, mapped.DirectionX, mapped.DirectionZ, 20);
            Assert.That((roundtrip - wind).Length, Is.LessThan(1e-10));
        }
        [Test] public void SceneNorthRotationAndWrappedBearingAreEquivalent()
        {
            Assert.That((WindConversion.FromMeteorological(5, -270, 90) -
                WindConversion.FromMeteorological(5, 180)).Length, Is.LessThan(1e-10));
        }
        [Test] public void AmbiguousZeroDirectionProducesCalmRatherThanOldHeading()
        { Assert.That(WindConversion.FromEnviro(1, 0, 0, 20).Length, Is.Zero); }
        [Test] public void CalmRoundtripHasSafeNonzeroEnviroDirection()
        { var m = WindConversion.ToEnviro(default, 20); Assert.That(m.Strength, Is.Zero); Assert.That(m.DirectionZ, Is.EqualTo(1)); }
        [TestCase(-.1)] [TestCase(1.1)] [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)]
        public void InvalidEnviroStrengthIsRejected(double strength)
        { Assert.Throws<ArgumentOutOfRangeException>(() => WindConversion.FromEnviro(strength, 1, 0, 20)); }
        [Test] public void WindOutsideScaleAndVerticalWindAreNotSilentlyClipped()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WindConversion.ToEnviro(new DVector3(21, 0, 0), 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => WindConversion.ToEnviro(new DVector3(0, 1, 0), 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => WindConversion.FromEnviro(.5, 1, 0, 0));
        }
        [TestCase(17)] [TestCase(43)] [TestCase(219)]
        public void FullScaleRoundtripAcceptsFloatingPointRounding(double bearing)
        {
            var wind = WindConversion.FromMeteorological(20, bearing);
            var m = WindConversion.ToEnviro(wind, 20);
            Assert.That(m.Strength, Is.LessThanOrEqualTo(1));
            Assert.That((WindConversion.FromEnviro(m.Strength, m.DirectionX, m.DirectionZ, 20) - wind).Length, Is.LessThan(1e-10));
            Assert.Throws<ArgumentOutOfRangeException>(() => WindConversion.ToEnviro(wind * 1.000001, 20));
        }
        [Test] public void TemperatureIsKelvinAndMetadataDoesNotInventRainfall()
        {
            var snapshot = new WeatherSnapshot("Rain", default, WindConversion.CelsiusToKelvin(20), .5, .1, .25, 0);
            Assert.That(snapshot.TemperatureK, Is.EqualTo(293.15).Within(1e-10));
            Assert.That(snapshot.Wetness, Is.EqualTo(.5)); Assert.That(snapshot.SnowCover, Is.EqualTo(.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => WindConversion.CelsiusToKelvin(-273.15));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeatherSnapshot("Rain", default, 293, 2, 0, 0, 0));
        }
    }
}
