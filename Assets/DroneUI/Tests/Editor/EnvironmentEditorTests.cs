using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;
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
        [Test] public void DropdownCaptionChangeDoesNotSelectAnotherWeather()
        {
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            try {
                window.Show();
                var dropdown = new DroneDropdown("Погода", new List<string> { "Ясно", "Облачно" }, 0);
                window.rootVisualElement.Add(dropdown);
                int changes = 0; dropdown.RegisterValueChangedCallback(_ => changes++);
                dropdown.SetCaption("Кастомное");
                Assert.That(changes, Is.Zero); Assert.That(dropdown.value, Is.EqualTo("Ясно"));
                Assert.That(dropdown.Q<Label>(className: "drone-dropdown-value").text, Is.EqualTo("Кастомное"));
                dropdown.index = 1;
                Assert.That(changes, Is.EqualTo(1)); Assert.That(dropdown.value, Is.EqualTo("Облачно"));
            } finally { window.Close(); }
        }
        [Test] public void VisualEditKeepsOverrideAndExpandedFormAndCancelRestoresProfile()
        {
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            var catalog = ScriptableObject.CreateInstance<DroneScenarioCatalog>();
            try {
                window.Show(); var host = new VisualElement(); host.AddToClassList("stage"); window.rootVisualElement.Add(host);
                var type = typeof(DroneDropdown).Assembly.GetType("DroneLab.UI.DroneScenariosScreen", true);
                var screen = Activator.CreateInstance(type, new object[] { host, catalog, (Action)(() => { }) });
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                type.GetMethod("SwitchTab", flags).Invoke(screen, new object[] { true });
                var original = DroneEnvironmentProfiles.New();
                var profiles = (List<DroneEnvironmentDocument>)type.GetField("profiles", flags).GetValue(screen);
                profiles.Add(original); type.GetMethod("Select", flags).Invoke(screen, new object[] { original, false });
                var editor = (VisualElement)type.GetField("editor", flags).GetValue(screen);
                editor.Q<Foldout>("environment-advanced").value = true;
                editor.Q<DoubleField>("visual.cloudCoverage").value = .73;
                var draft = (DroneEnvironmentDocument)type.GetField("draft", flags).GetValue(screen);
                Assert.That((double?)draft.visual["cloudCoverage"], Is.EqualTo(.73));
                Assert.That((string)draft.visual["weatherPresetId"], Is.EqualTo(""));
                Assert.That(editor.Q<Foldout>("environment-advanced").value, Is.True);
                var weather = (DroneDropdown)type.GetField("weatherChoice", flags).GetValue(screen);
                Assert.That(weather.Q<Label>(className: "drone-dropdown-value").text, Is.EqualTo("Кастомное"));
                Assert.That(((Button)type.GetField("cancelButton", flags).GetValue(screen)).style.display.value, Is.EqualTo(DisplayStyle.Flex));
                type.GetMethod("BuildEditor", flags).Invoke(screen, new object[] { true });
                Assert.That(editor.Q<Foldout>("environment-advanced").value, Is.True);
                type.GetMethod("DiscardChanges", flags).Invoke(screen, null);
                draft = (DroneEnvironmentDocument)type.GetField("draft", flags).GetValue(screen);
                Assert.That(draft.visual["cloudCoverage"], Is.Null); Assert.That(original.visual["cloudCoverage"], Is.Null);
                Assert.That(editor.Q<Foldout>("environment-advanced").value, Is.True);
                Assert.That(((Button)type.GetField("cancelButton", flags).GetValue(screen)).style.display.value, Is.EqualTo(DisplayStyle.None));
            } finally { window.Close(); UnityEngine.Object.DestroyImmediate(catalog); }
        }
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
