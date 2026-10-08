using System;
using System.IO;
using System.Linq;
using DroneLab.UI;
using Enviro;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DroneLab.UI.Tests
{
    public sealed class EnvironmentEditorTests
    {
        private string folder;
        [SetUp] public void Setup() { folder = Path.Combine(Path.GetTempPath(),"DroneLab-"+Guid.NewGuid().ToString("N")); }
        [TearDown] public void Cleanup() { if (Directory.Exists(folder)) Directory.Delete(folder,true); }
        [Test] public void BundledProfileOverrideKeepsIdentityAndDeletionSurvivesRestart()
        {
            var store = new DroneEnvironmentProfileStore(folder);
            var d = DroneEnvironmentProfiles.New(); d.id = "builtin-environment_calm"; d.builtIn = true; d.name = "Новые условия";
            store.Save(d); Assert.That(store.Files().Count(),Is.EqualTo(1));
            var read = DroneEnvironmentProfiles.Read(File.ReadAllText(store.PathFor(d.id)));
            Assert.That(read.id,Is.EqualTo(d.id)); Assert.That(read.name,Is.EqualTo(d.name));
            store.Delete(read); var restarted = new DroneEnvironmentProfileStore(folder);
            Assert.That(restarted.Removed(),Does.Contain(d.id)); Assert.That(restarted.Files(),Is.Empty);
            restarted.Save(read); Assert.That(restarted.Removed(),Does.Not.Contain(d.id));
        }
        [TestCase("../../outside")]
        [TestCase("builtin-environment_../outside")]
        [TestCase("builtin-environment_calm\\outside")]
        public void StoreRejectsPathTraversal(string id) => Assert.Throws<ArgumentException>(() => new DroneEnvironmentProfileStore(folder).PathFor(id));
        [TestCase("gravityMps2",0)] [TestCase("pressurePa",999)] [TestCase("gustIntensityMps",101)]
        [TestCase("dryden.modesPerComponent",129)] [TestCase("dryden.sigmaUvwMps[1]",31)]
        public void InvalidFieldIsReportedAtItsOwnControl(string path,double value)
        {
            var d = DroneEnvironmentProfiles.New(); d.environment.SelectToken(path).Replace(new JValue(value));
            string control = path.Split('[')[0]; Assert.That(DroneEnvironmentFields.Errors(d).ContainsKey(control),Is.True);
        }
        [Test] public void RainHasOneAuthoritativeIntensity()
        {
            var d = DroneEnvironmentProfiles.New(); d.environment["weather"]["precipitation"] = "Rain";
            Assert.That(DroneEnvironmentFields.Errors(d).ContainsKey("weather.intensityMmPerHour"),Is.True);
            d.environment["weather"]["intensityMmPerHour"] = 10; Assert.That(DroneEnvironmentFields.Errors(d),Is.Empty);
        }
        [Test] public void WeatherOptionsDeduplicateAndDoNotOfferPrecipitation()
        {
            var catalog = ScriptableObject.CreateInstance<DroneScenarioCatalog>();
            var clear1 = ScriptableObject.CreateInstance<EnviroWeatherType>(); clear1.name = "Clear Sky";
            var clear2 = ScriptableObject.CreateInstance<EnviroWeatherType>(); clear2.name = "Clear Sky";
            var rain = ScriptableObject.CreateInstance<EnviroWeatherType>(); rain.name = "Rain";
            try {
                catalog.weatherPresets.Add(new DroneWeatherEntry { id="one",title="Clear Sky",preset=clear1 });
                catalog.weatherPresets.Add(new DroneWeatherEntry { id="two",title="Clear Sky",preset=clear2 });
                catalog.weatherPresets.Add(new DroneWeatherEntry { id="rain",title="Rain",preset=rain });
                Assert.That(catalog.WeatherOptions(),Has.Count.EqualTo(1)); Assert.That(catalog.WeatherOptions()[0].id,Is.EqualTo("one"));
                Assert.That(catalog.Weather("two").preset,Is.SameAs(clear2));
                Assert.That(DroneScenarioCatalog.WeatherTitle("Cloudy 4"),Is.EqualTo("Пасмурно"));
            } finally { UnityEngine.Object.DestroyImmediate(clear1); UnityEngine.Object.DestroyImmediate(clear2); UnityEngine.Object.DestroyImmediate(rain); UnityEngine.Object.DestroyImmediate(catalog); }
        }
#if ENVIRO_HDRP
        [Test] public void FogOverrideValidatesAgainstInheritedBoundary()
        {
            var d = DroneEnvironmentProfiles.New(); d.visual["fogMaxHeight"] = -1;
            Assert.That(DroneEnvironmentFields.Errors(d).ContainsKey("visual.fogMaxHeight"),Is.True);
        }
#endif
    }
}
