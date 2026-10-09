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
        private static DroneEnvironmentDocument Starter(string name) => DroneEnvironmentProfiles.Read(
            Resources.Load<TextAsset>("DroneLab/EnvironmentPresets/" + name).text);
        private static RuntimeEnvironment StarterWind(string name)
        {
            var d = Starter(name);
            var loaded = ProfileLoader.Load(Resources.Load<TextAsset>("DronePhysics/quad_test_ctcq").text,
                DroneEnvironmentProfiles.PhysicsJson(d), Resources.Load<TextAsset>("DronePhysics/drone-profile.schema").text,
                Resources.Load<TextAsset>("DronePhysics/environment-profile.schema").text);
            Assert.That(loaded.Success, Is.True, string.Join("\n", loaded.Issues));
            return loaded.Parameters.Environment;
        }
        [TestCase("01_calm_day")]
        [TestCase("02_northwest_gusts")]
        [TestCase("03_western_turbulence")]
        public void CuratedStarterWorksWithCoefficientAndMeasuredDrones(string name)
        {
            var d = Starter(name);
            Assert.That(DroneEnvironmentProfileStore.ValidId(d.id), Is.True);
            Assert.DoesNotThrow(() => DroneEnvironmentProfiles.Validate(d));
            Assert.DoesNotThrow(() => DroneEnvironmentProfiles.Validate(d, Resources.Load<TextAsset>("DronePhysics/quad_test_basic")));
            Assert.That(DroneScenarioCatalog.Load().Weather((string)d.visual["weatherPresetId"])?.preset, Is.Not.Null);
        }
        [Test]
        public void NorthwestStarterGustRunsFromFiveToEightAndRepeatsInTwelveSeconds()
        {
            var wind = StarterWind("02_northwest_gusts");
            Assert.That(wind.Sample(default, 0).Length, Is.EqualTo(5).Within(1e-10));
            Assert.That(wind.Sample(default, 6).Length, Is.EqualTo(8).Within(1e-10));
            Assert.That(wind.Sample(default, 12).Length, Is.EqualTo(5).Within(1e-10));
            Assert.That(wind.MeanWind.X, Is.Positive); Assert.That(wind.MeanWind.Z, Is.Negative);
        }
        [Test]
        public void DrydenStarterRepeatsAndDoesNotAddPeriodicGusts()
        {
            var a = StarterWind("03_western_turbulence"); var b = StarterWind("03_western_turbulence");
            Assert.That(a.GustEnabled, Is.False); Assert.That(a.Dryden, Is.Not.Null);
            Assert.That((a.Sample(new DVector3(20, 10, 30), 8) - b.Sample(new DVector3(20, 10, 30), 8)).Length, Is.Zero);
            Assert.That(StarterWind("01_calm_day").Sample(default, 100).Length, Is.Zero);
        }
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
