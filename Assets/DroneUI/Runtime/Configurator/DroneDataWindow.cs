using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal sealed class DroneDataWindow : VisualElement
    {
        public DroneProfileEditBuffer Buffer { get; }
        public bool Busy=>editor.IsBusy;
        private readonly DroneProfileFields fields;
        private readonly DroneTableEditor editor;
        private readonly Label message;
        private readonly Button apply,cancel,close;
        private DroneDropdown modelChoice;
        private readonly ScrollView performanceSettings;
        private readonly JObject performanceRotor;
        private readonly string rotorPath;

        public DroneDataWindow(JToken source,JToken rule,string path,string title,string subtitle,
            Dictionary<string,string> errors,Dictionary<string,string> inputs,Action applied,Action cancelled,Action requestedClose,
            JObject rotor=null,string rotorPath=null)
        {
            AddToClassList("drone-data-overlay");focusable=true;
            Buffer=new DroneProfileEditBuffer(source,rule,path,errors,inputs);this.rotorPath=rotorPath;
            if(rotor!=null){performanceRotor=(JObject)rotor.DeepClone();performanceRotor["performance"]=Buffer.Value;}
            var card=Box(this,"drone-data-window");var header=Box(card,"drone-window-header");
            var heading=Box(header,"drone-window-heading");DroneProfileFields.Label(heading,title.ToUpperInvariant(),"drone-panel-title");
            DroneProfileFields.Label(heading,subtitle,"drone-window-subtitle");
            close=DroneProfileFields.Button(header,"Закрыть",requestedClose);close.AddToClassList("drone-window-close");
            if(performanceRotor!=null) {
                var models=Box(card,"drone-window-model");var modelRule=DroneParameterSchema.ObjectRule("RotorPerformanceProfile")["properties"]["model"];
                var keys=((JArray)modelRule["enum"]).Select(v=>(string)v).ToList();
                var choice=new DroneDropdown("Модель характеристики",keys.Select(DroneParameterSchema.Name).ToList(),Math.Max(0,keys.IndexOf((string)Buffer.Value["model"])));
                modelChoice=choice;choice.AddToClassList("environment-field");models.Add(choice);
                choice.RegisterValueChangedCallback(evt=>{if(evt.target!=choice)return;Buffer.Value["model"]=keys[choice.index];Rebuild();UpdateState();});
                DroneHelp.Attach(models,()=>DroneParameterSchema.Tooltip("model",modelRule));
            }
            fields=new DroneProfileFields(UpdateState,Rebuild,OpenNested,Buffer.Errors,Buffer.Inputs);
            if(performanceRotor!=null){
                performanceSettings=new ScrollView(ScrollViewMode.Vertical);performanceSettings.AddToClassList("drone-performance-settings");performanceSettings.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
                performanceSettings.verticalScroller.AddToClassList("graphite-scroller");performanceSettings.verticalScroller.lowButton.style.display=DisplayStyle.None;performanceSettings.verticalScroller.highButton.style.display=DisplayStyle.None;card.Add(performanceSettings);
            }
            editor=new DroneTableEditor(fields,UpdateState,SetMessage);card.Add(editor);
            var footer=Box(card,"drone-window-footer");message=DroneProfileFields.Label(footer,"Изменения применятся к текущему профилю.","drone-window-message");
            cancel=DroneProfileFields.Button(footer,"Отмена",cancelled);apply=DroneProfileFields.Button(footer,"Применить",applied);apply.AddToClassList("primary");
            RegisterCallback<KeyDownEvent>(evt=>{
                if(evt.keyCode!=KeyCode.Tab)return;
                var candidates=new List<VisualElement>();this.Query<VisualElement>().ForEach(element=>{
                    if(element.focusable && element.enabledInHierarchy && element.tabIndex>=0 && element.worldBound.width>0 && element.worldBound.height>0 && element.resolvedStyle.display!=DisplayStyle.None)candidates.Add(element);
                });
                if(candidates.Count==0)return;int index=candidates.IndexOf(panel.focusController.focusedElement as VisualElement);
                candidates[(index+(evt.shiftKey?-1:1)+candidates.Count)%candidates.Count].Focus();evt.StopPropagation();evt.PreventDefault();
            },TrickleDown.TrickleDown);
            Rebuild();UpdateState();schedule.Execute(()=>close.Focus());
        }
        private static VisualElement Box(VisualElement host,string style){var element=new VisualElement();element.AddToClassList(style);host.Add(element);return element;}
        private void Rebuild()
        {
            if(performanceRotor!=null) {
                var performance=(JObject)Buffer.Value;string model=(string)performance["model"];
                string table=model=="RpmTable"?"rpmTable":model=="PerformanceMap"?"performanceMap":null;
                if(table!=null && performance[table]==null)performance[table]=new JArray();
                performanceSettings.Clear();performanceSettings.style.display=table!=null?DisplayStyle.Flex:DisplayStyle.None;
                if(table!=null)fields.Object(performanceSettings,performance,"RotorPerformanceProfile",rotorPath+".performance","model","rpmTable","performanceMap");
                editor.SetPerformance(performanceRotor,rotorPath);
            } else editor.Set((JArray)Buffer.Value,DroneParameterSchema.Resolve(Buffer.Rule)["items"],Buffer.Path,"Таблица параметров");
            UpdateState();
        }
        private void OpenNested(JArray rows,JToken arrayRule,string path,string title)
        {
            editor.Set(rows,DroneParameterSchema.Resolve(arrayRule)["items"],path,title);UpdateState();
        }
        private void SetMessage(string value){message.text=value;message.EnableInClassList("error",false);}
        private void UpdateState()
        {
            if(editor==null || apply==null)return;
            editor.SetEnabled(!Busy);modelChoice?.SetEnabled(!Busy);performanceSettings?.SetEnabled(!Busy);
            string error=Buffer.ValidationError();apply.SetEnabled(!Busy && error==null);cancel.SetEnabled(!Busy);close.SetEnabled(!Busy);
            if(error!=null){message.text=error;message.EnableInClassList("error",true);}
            else if(message.ClassListContains("error")){message.text="Изменения применятся к текущему профилю.";message.EnableInClassList("error",false);}
            editor.RefreshPlot();
        }
    }
}
