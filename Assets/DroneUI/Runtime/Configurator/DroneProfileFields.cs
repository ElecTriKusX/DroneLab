using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>Every branch of the live schema is editable, including optional nested models and arrays.</summary>
    internal sealed class DroneProfileFields
    {
        private readonly Action changed, rebuild;
        private readonly Action<JArray,JToken,string,string> table;
        private readonly Dictionary<string,string> errors, text;
        private readonly Dictionary<string,bool> expanded;
        public DroneProfileFields(Action changed,Action rebuild,Action<JArray,JToken,string,string> table,Dictionary<string,string> errors,Dictionary<string,string> text,Dictionary<string,bool> expanded=null)
        {this.changed=changed;this.rebuild=rebuild;this.table=table;this.errors=errors;this.text=text;this.expanded=expanded??new Dictionary<string,bool>();}
        public void Object(VisualElement host,JObject value,string definition,string path,params string[] excluded)
        {
            var rule=DroneParameterSchema.ObjectRule(definition);var required=((JArray)rule["required"]).Select(x=>(string)x).ToHashSet();
            foreach(var property in ((JObject)rule["properties"]).Properties()) {
                string key=property.Name, child=path.Length==0?key:path+"."+key;
                if(excluded.Contains(key))continue;
                if(!Relevant(definition,key,value))continue;
                if(value[key]==null) {
                    var row=Row(host);var add=new Button(()=> {value[key]=DroneParameterSchema.Default(property.Value,key);ClearErrors(child);changed();rebuild();}) {text="+ "+DroneParameterSchema.Name(key)};
                    add.AddToClassList("optional-parameter");row.Add(add);DroneHelp.Attach(row,()=>DroneParameterSchema.Tooltip(key,property.Value));continue;
                }
                Field(host,key,property.Value,value[key],child,t=>value[key]=t);
                if(!required.Contains(key)) {
                    var remove=new Button(()=>{value.Remove(key);ClearErrors(child);changed();rebuild();}) {text="Убрать «"+DroneParameterSchema.Name(key)+"»"};
                    remove.AddToClassList("remove-parameter");host.Add(remove);
                }
            }
        }
        public void Field(VisualElement host,string key,JToken unresolved,JToken value,string path,Action<JToken> write)
        {
            var rule=DroneParameterSchema.Resolve(unresolved);var type=(string)rule["type"];
            string label=DroneParameterSchema.Name(key);if(DroneParameterSchema.Unit(key).Length>0)label+=" · "+DroneParameterSchema.Unit(key);
            if(type=="object") {
                var section=new Foldout {text=label,value=expanded.TryGetValue(path,out var open)?open:key=="geometry"};section.AddToClassList("drone-field-group");host.Add(section);
                section.RegisterValueChangedCallback(evt=>{if(evt.target==section)expanded[path]=evt.newValue;});
                DroneHelp.Attach(section.Q<Toggle>(),()=>DroneParameterSchema.Tooltip(key,unresolved));
                Object(section,(JObject)value,DroneParameterSchema.Definition(unresolved),path);ReadOnly(section);return;
            }
            if(type=="array") {
                var array=(JArray)value;var heading=Row(host);heading.AddToClassList("drone-vector-heading");Label(heading,label,"drone-vector-title");DroneHelp.Attach(heading,()=>DroneParameterSchema.Tooltip(key,unresolved));
                var itemRule=DroneParameterSchema.Resolve(rule["items"]);string itemType=(string)itemRule["type"];
                if(itemType=="object") {
                    var button=new Button(()=>table(array,unresolved,path,label)) {text=$"Открыть таблицу · {array.Count} строк"};button.AddToClassList("drone-table-open");host.Add(button);
                    return;
                }
                var row=Row(host);row.AddToClassList("drone-vector-row");
                var componentRule=DroneParameterSchema.ComponentRule(key,rule["items"],path);
                for(int i=0;i<array.Count;i++) {int index=i;Numeric(row,array.Count<=4?new[]{"X","Y","Z","W"}[i]:i.ToString(),componentRule,array[i],path+"["+i+"]",t=>array[index]=t);}
                if(rule["maxItems"]==null) {
                    var actions=Row(host);Button(actions,"+ Компонента",()=>{array.Add(DroneParameterSchema.Default(rule["items"]));changed();rebuild();});
                    if(array.Count>0)Button(actions,"− Последняя",()=>{array.RemoveAt(array.Count-1);ClearErrors(path);changed();rebuild();});
                }
                if(key=="thrustAxisLocal" || key=="normalLocal" || key=="directionLocal" || key=="principalAxesRotationXyzw")
                    Button(host,"Нормализовать",()=> {double n=Math.Sqrt(array.Sum(x=>Math.Pow((double)x,2)));if(n<=1e-12){errors[path]="Нулевой вектор нельзя нормализовать.";changed();return;}for(int i=0;i<array.Count;i++)array[i]=(double)array[i]/n;ClearErrors(path);changed();rebuild();});
                return;
            }
            var container=Row(host);container.AddToClassList("drone-parameter-row");
            if(rule["enum"] is JArray choices) {
                var keys=choices.Select(x=>(string)x).ToList();var field=new DroneDropdown(label,keys.Select(DroneParameterSchema.Name).ToList(),Math.Max(0,keys.IndexOf((string)value)));
                field.AddToClassList("environment-field");container.Add(field);
                field.RegisterValueChangedCallback(e=> {if(e.target!=field)return;write(keys[field.index]);ClearErrors(path);changed();host.schedule.Execute(rebuild);});
                field.Reselected+=()=> {write(keys[field.index]);ClearErrors(path);changed();host.schedule.Execute(rebuild);};
            } else if(type=="boolean") {
                var toggle=new Toggle(label) {value=(bool)value};toggle.AddToClassList("environment-field");container.Add(toggle);
                toggle.RegisterValueChangedCallback(e=>{write(e.newValue);ClearErrors(path);changed();});
            } else if(type=="string") {
                var field=new TextField(label) {value=(string)value};field.AddToClassList("environment-field");DroneTextInput.Configure(field);container.Add(field);
                field.RegisterValueChangedCallback(e=> {write(e.newValue);if((int?)rule["minLength"]>0 && string.IsNullOrWhiteSpace(e.newValue))errors[path]="Введите непустой текст.";else errors.Remove(path);field.EnableInClassList("invalid-field",errors.ContainsKey(path));changed();});
            } else Numeric(container,label,rule,value,path,write);
            DroneHelp.Attach(container,()=>DroneParameterSchema.Tooltip(key,unresolved));ReadOnly(container);
        }
        public TextField Numeric(VisualElement host,string label,JToken rule,JToken value,string path,Action<JToken> write)
        {
            var field=new TextField(label);field.SetValueWithoutNotify(text.TryGetValue(path,out var pending)?pending:Format(value));
            DroneTextInput.Configure(field);field.AddToClassList("environment-field");field.AddToClassList("drone-numeric");host.Add(field);
            field.EnableInClassList("invalid-field",errors.ContainsKey(path));
            field.RegisterValueChangedCallback(e=> {
                text[path]=e.newValue;
                if(DroneParameterSchema.TryNumber(e.newValue,rule,out var number,out var error)) {write(number);errors.Remove(path);}
                else errors[path]=error;
                field.EnableInClassList("invalid-field",errors.ContainsKey(path));changed();
            });ReadOnly(field);return field;
        }
        public static string Format(JToken token)=>token==null?"":token.Type==JTokenType.Float||token.Type==JTokenType.Integer?((double)token).ToString("G10",CultureInfo.InvariantCulture):token.ToString();
        public void ClearErrors(string path)
        {foreach(var key in errors.Keys.Where(k=>k==path || k.StartsWith(path+".") || k.StartsWith(path+"[")).ToArray())errors.Remove(key);foreach(var key in text.Keys.Where(k=>k==path || k.StartsWith(path+".") || k.StartsWith(path+"[")).ToArray())text.Remove(key);}
        public void RemoveRow(JArray rows,int index,string path)=>DroneArrayEdits.Remove(rows,index,path,errors,text);
        private static bool Relevant(string type,string key,JObject value)
        {
            string mode=(string)value["model"]??(string)value["mode"];
            if(type=="RotorPerformanceProfile") {
                if(key=="kThrustNPerRadPerSecSquared" || key=="kTorqueNmPerRadPerSecSquared")return mode=="OmegaSquared";
                if(key=="ct" || key=="cq")return mode=="CtCq";
                if(key=="rpmTable")return mode=="RpmTable";
                if(key=="performanceMap")return mode=="PerformanceMap";
                if(key=="outOfRangePolicy")return mode=="RpmTable" || mode=="PerformanceMap";
            }
            if(type=="InertiaProfile" && key!="mode")return mode=="ManualPrincipal";
            if(type=="BodyAerodynamicsProfile") {
                if(key=="dragCd" || key=="referenceAreaM2")return mode=="AxisApproximation";
                if(key=="projectedArea" || key=="dragCoefficient")return mode=="ProjectedArea";
                if(key=="surfaces")return mode=="Surfaces";
            }
            if(type=="ProjectedAreaProfile") {if(key=="samples")return mode!="AxisApproximation";if(key=="referenceAreaM2")return mode=="AxisApproximation";}
            if(type=="BatteryProfile" && key!="mode" && mode=="None")return false;
            return true;
        }
        internal static VisualElement Row(VisualElement parent){var v=new VisualElement();v.AddToClassList("drone-row");parent.Add(v);return v;}
        internal static Label Label(VisualElement host,string value,string className=null){var label=new Label(value);label.selection.isSelectable=false;label.pickingMode=PickingMode.Ignore;if(className!=null)label.AddToClassList(className);host.Add(label);return label;}
        internal static Button Button(VisualElement host,string title,Action action){var b=new Button(action){text=title};b.AddToClassList("drone-button");host.Add(b);return b;}
        internal static void ReadOnly(VisualElement host)=>host.Query<Label>().ForEach(l=> {l.selection.isSelectable=false;l.focusable=false;l.pickingMode=PickingMode.Ignore;});
    }
}
