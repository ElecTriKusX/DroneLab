using System;
using System.Collections.Generic;
using DroneLab.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI.Tests
{
    public sealed class DroneConfiguratorInteractionTests
    {
        [Test] public void CategoryFocusDoesNotOpenHelpAndHelpFocusDoesNotToggleCategory()
        {
            var window=ScriptableObject.CreateInstance<EditorWindow>();
            try {
                window.Show();var host=new VisualElement();host.AddToClassList("stage");window.rootVisualElement.Add(host);
                var fields=new DroneProfileFields(()=>{},()=>{},(a,b,c,d)=>{},new Dictionary<string,string>(),new Dictionary<string,string>());
                var value=(JObject)DroneProfileLibrary.Create().profile["massProperties"];
                fields.Field(host,"massProperties",new JObject{["$ref"]="#/$defs/MassPropertiesProfile"},value,"massProperties",_=>{});
                var category=host.Q<Foldout>();var header=category.Q<Toggle>();var help=category.Q<Button>(className:"parameter-help");
                Assert.That(help.parent,Is.SameAs(header.parent));Assert.That(header.Contains(help),Is.False);
                header.Focus();category.value=true;
                Assert.That(host.Q(className:"parameter-tooltip"),Is.Null);
                help.Focus();Assert.That(host.Q(className:"parameter-tooltip"),Is.Not.Null);Assert.That(category.value,Is.True);
            } finally {window.Close();}
        }
        [Test] public void BatteryHasNoModeSelectorAndOpeningFormPreservesItsMode()
        {
            var host=new VisualElement();var fields=new DroneProfileFields(()=>{},()=>{},(a,b,c,d)=>{},new Dictionary<string,string>(),new Dictionary<string,string>());
            var document=new DroneProfileDocument{profile=JObject.Parse(DroneProfileLibrary.Resource("quad_test_thermal"))};
            var battery=(JObject)document.profile["powerSystem"]["battery"];string before=battery.ToString();
            fields.Object(host,battery,"BatteryProfile","powerSystem.battery");
            Assert.That(host.Q<DroneDropdown>(),Is.Null);Assert.That(battery.ToString(),Is.EqualTo(before));
            Assert.That(host.Q<TextField>(),Is.Not.Null);
        }
    }
}
