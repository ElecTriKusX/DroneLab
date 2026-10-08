using System;
using DroneLab.Physics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DroneLab.UI.Tests
{
    public sealed class EnvironmentLibraryTests
    {
        [Test]
        public void PlainPhysicsImportKeepsEveryOriginalField()
        {
            var source = Resources.Load<TextAsset>("DronePhysics/environment_dryden_frozen").text;
            var imported = DroneEnvironmentProfiles.Read(source);
            Assert.That(JToken.DeepEquals(imported.environment, JObject.Parse(source)), Is.True);
            Assert.DoesNotThrow(() => DroneEnvironmentProfiles.Validate(imported));
        }
        [Test]
        public void WrapperRoundTripPreservesPhysicsAndVisualOverrides()
        {
            var original = DroneEnvironmentProfiles.New(); original.visual["fogDistance"] = 72;
            var loaded = DroneEnvironmentProfiles.Read(JsonConvert.SerializeObject(original));
            Assert.That(JToken.DeepEquals(original.environment, loaded.environment), Is.True);
            Assert.That((double)loaded.visual["fogDistance"], Is.EqualTo(72));
            Assert.DoesNotThrow(() => DroneEnvironmentProfiles.Validate(loaded));
        }
        [Test]
        public void EditingCopyDoesNotMutateBuiltin()
        {
            var source = DroneEnvironmentProfiles.New(); source.builtIn = true;
            var copy = source.Copy(); copy.environment["windVelocityWorldMps"][0] = 9; copy.visual["timeOfDay"] = 20;
            Assert.That((double)source.environment["windVelocityWorldMps"][0], Is.Zero);
            Assert.That((double)source.visual["timeOfDay"], Is.EqualTo(14));
        }
        [TestCase("None", true)]
        [TestCase("CustomField", true)]
        [TestCase("Gust", false)]
        public void InvalidGustCombinationIsRejected(string mode, bool enabled)
        {
            var d = DroneEnvironmentProfiles.New(); d.environment["windMode"] = mode; d.environment["gustEnabled"] = enabled;
            Assert.Throws<ArgumentException>(() => DroneEnvironmentProfiles.Validate(d));
        }
        [Test]
        public void DrydenRequiresHorizontalUnitDirection()
        {
            var d = DroneEnvironmentProfiles.New(); d.environment["windMode"] = "DrydenFrozen";
            d.environment["dryden"]["advectionDirectionWorld"] = new JArray(1, 1, 0);
            Assert.Throws<ArgumentException>(() => DroneEnvironmentProfiles.Validate(d));
        }
        [Test]
        public void VisualRangesAndUnknownPhysicsFieldsAreRejected()
        {
            var d = DroneEnvironmentProfiles.New(); d.visual["cloudCoverage"] = 2;
            Assert.Throws<ArgumentException>(() => DroneEnvironmentProfiles.Validate(d));
            d.visual.Remove("cloudCoverage"); d.environment["unknown"] = 1;
            Assert.Throws<ArgumentException>(() => DroneEnvironmentProfiles.Validate(d));
        }
        [Test]
        public void ActualMeasuredDroneRejectsVariableAtmosphere()
        {
            var d = DroneEnvironmentProfiles.New(); d.environment["airDensityMode"] = "StandardAtmosphere";
            var drone = Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
            Assert.Throws<ArgumentException>(() => DroneEnvironmentProfiles.Validate(d, drone));
        }
        [Test]
        public void DuplicateJsonFieldsAreRejected()
        {
            Assert.Throws<JsonReaderException>(() => DroneEnvironmentProfiles.Read("{\"windMode\":\"None\",\"windMode\":\"Gust\"}"));
        }
    }
}
