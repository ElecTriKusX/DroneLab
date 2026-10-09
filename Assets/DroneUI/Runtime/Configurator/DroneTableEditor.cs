using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DroneLab.Physics;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal sealed class DroneTableEditor : VisualElement
    {
        private readonly DroneProfileFields fields;
        private readonly Action changed;
        private readonly Action<string> status;
        private readonly DroneCurvePlot plot;
        private readonly VisualElement gridHost,toolbar,graphToolbar,transferToolbar,graphPanel;
        private readonly List<(string key,Button button)> graphTabs=new List<(string,Button)>();
        private readonly Label tableHeading;
        private readonly Label title;
        private readonly Dictionary<string,float> columnWidths=new Dictionary<string,float>();
        private readonly Dictionary<string,List<VisualElement>> columnCells=new Dictionary<string,List<VisualElement>>();
        private JArray rows;
        private JObject rule,rotor;
        private string path,column;
        private double mapRpm;
        private DroneDropdown rpmChoice;
        private string rpmSignature;
        private bool busy;
        public bool IsBusy=>busy;
        public DroneTableEditor(DroneProfileFields fields,Action changed,Action<string> status)
        {
            this.fields=fields;this.changed=changed;this.status=status;AddToClassList("drone-data-panel");
            var heading=DroneProfileFields.Row(this);title=DroneProfileFields.Label(heading,"Характеристики","drone-panel-title");
            transferToolbar=DroneProfileFields.Row(heading);transferToolbar.AddToClassList("drone-csv-toolbar");
            var content=DroneProfileFields.Row(this);content.AddToClassList("drone-data-content");
            graphPanel=new VisualElement();graphPanel.AddToClassList("drone-graph-panel");content.Add(graphPanel);
            graphToolbar=DroneProfileFields.Row(graphPanel);graphToolbar.AddToClassList("curve-tabs");plot=new DroneCurvePlot();graphPanel.Add(plot);
            var right=new VisualElement();right.AddToClassList("drone-table-panel");content.Add(right);
            tableHeading=DroneProfileFields.Label(right,"ИЗМЕРЕННЫЕ ТОЧКИ","drone-panel-title");tableHeading.style.flexGrow=0;tableHeading.style.flexShrink=0;
            gridHost=new VisualElement();gridHost.AddToClassList("drone-table-host");right.Add(gridHost);
            toolbar=DroneProfileFields.Row(right);toolbar.AddToClassList("drone-table-toolbar");
            plot.Set(Array.Empty<Vector2>(),"","","Выберите характеристики винта или таблицу параметров.");
        }
        public void Set(JArray rows,JToken itemRule,string path,string title)
        {
            this.rows=rows;this.rule=DroneParameterSchema.Resolve(itemRule);this.path=path;rotor=null;this.title.text=title;
            var numeric=((JObject)rule["properties"]).Properties().Where(p=>(string)p.Value["type"]=="number" || (string)p.Value["type"]=="integer").Select(p=>p.Name).ToList();
            bool map=rule["properties"]["advanceRatio"]!=null;
            column=map?"ct":numeric.Skip(1).FirstOrDefault();graphToolbar.Clear();graphTabs.Clear();rpmChoice=null;rpmSignature=null;
            bool curve=rule["properties"]["rpm"]!=null || rule["properties"]["soc"]!=null || rule["properties"]["loadFraction"]!=null;
            graphPanel.style.display=curve?DisplayStyle.Flex:DisplayStyle.None;EnableInClassList("table-only",!curve);tableHeading.text=curve?"ИЗМЕРЕННЫЕ ТОЧКИ":"СТРОКИ ТАБЛИЦЫ";
            foreach(var key in numeric.Skip(1).Where(k=>!map || k!="advanceRatio"))AddGraphTab(key);
            toolbar.Clear();DroneProfileFields.Button(toolbar,"Добавить строку",()=>{rows.Add(new JObject());BuildRows();changed();RefreshPlot();});
            transferToolbar.Clear();DroneProfileFields.Button(transferToolbar,"Импорт CSV",()=>Transfer(false));DroneProfileFields.Button(transferToolbar,"Экспорт CSV",()=>Transfer(true));
            BuildRows();RefreshPlot();
        }
        public void SetPerformance(JObject value,string rotorPath)
        {
            var performance=(JObject)value["performance"];string model=(string)performance?["model"];
            if(model=="RpmTable" && performance["rpmTable"] is JArray rpm){Set(rpm,DroneParameterSchema.ObjectRule("RotorPerformanceProfile")["properties"]["rpmTable"]["items"],rotorPath+".performance.rpmTable","Характеристики винта · RPM");return;}
            if(model=="PerformanceMap" && performance["performanceMap"] is JArray map){Set(map,DroneParameterSchema.ObjectRule("RotorPerformanceProfile")["properties"]["performanceMap"]["items"],rotorPath+".performance.performanceMap","Характеристики винта · RPM / J");return;}
            rows=null;rule=null;rotor=value;path=rotorPath;column="thrustN";title.text="Характеристики винта · "+DroneParameterSchema.Name(model);toolbar.Clear();graphToolbar.Clear();graphTabs.Clear();gridHost.Clear();transferToolbar.Clear();
            graphPanel.style.display=DisplayStyle.Flex;EnableInClassList("table-only",false);tableHeading.text="ПАРАМЕТРЫ ХАРАКТЕРИСТИКИ";
            foreach(string key in new[]{"thrustN","torqueNm"})AddGraphTab(key);
            DroneProfileFields.Label(gridHost,"Кривая рассчитывается из введённых коэффициентов при плотности калибровки. Измерения редактируются через RPM-таблицу или карту RPM/J.","scenario-hint");
            var form=new ScrollView(ScrollViewMode.Vertical);form.AddToClassList("drone-coefficient-form");ThemeScroller(form.verticalScroller,false);gridHost.Add(form);
            fields.Object(form,performance,"RotorPerformanceProfile",rotorPath+".performance","model");
            RefreshPlot();
        }
        private void AddGraphTab(string key)
        {
            var button=DroneProfileFields.Button(graphToolbar,DroneParameterSchema.Name(key),()=>{column=key;RefreshPlot();});graphTabs.Add((key,button));
        }
        public void RefreshPlot()
        {
            foreach(var tab in graphTabs)tab.button.EnableInClassList("active",tab.key==column);
            try {
                if(rows!=null && rule!=null) {
                    if(rule["properties"]["rpm"]==null && rule["properties"]["soc"]==null && rule["properties"]["loadFraction"]==null){plot.Set(Array.Empty<Vector2>(),"","","Геометрия и источники редактируются в таблице. Эти строки не образуют физическую кривую.");return;}
                    var numeric=((JObject)rule["properties"]).Properties().Where(p=>(string)p.Value["type"]=="number" || (string)p.Value["type"]=="integer").Select(p=>p.Name).ToList();
                    bool map=rule["properties"]["advanceRatio"]!=null;
                    string x=map?"advanceRatio":numeric.FirstOrDefault();
                    if(map)RefreshRpmChoices();
                    if(x==null || column==null){plot.Set(Array.Empty<Vector2>(),"","","Эта таблица содержит геометрию или источники, а не числовую кривую.");return;}
                    var selected=rows.Where(r=>r[x]!=null && r[column]!=null && (x!="advanceRatio" || r["rpm"]!=null && Math.Abs((double)r["rpm"]-mapRpm)<1e-6));
                    plot.Set(selected.Select(r=>new Vector2((float)r[x],(float)r[column])),DroneParameterSchema.Name(x)+" "+DroneParameterSchema.Unit(x),DroneParameterSchema.Name(column)+" "+DroneParameterSchema.Unit(column));
                } else if(rotor!=null) {
                    var perf=rotor["performance"].ToObject<RotorPerformanceProfile>();double density=(double?)rotor["performance"]["referenceAirDensityKgM3"]??1.225;
                    var curve=new PropellerPerformance(perf,density,(double)rotor["propeller"]["diameterM"]);
                    double maximum=(double)rotor["motor"]["maxRpm"];var points=new List<Vector2>();
                    for(int i=0;i<=40;i++){double rpm=maximum*i/40;var sample=curve.Evaluate(PhysicsMath.RpmToOmega(rpm));points.Add(new Vector2((float)rpm,(float)(column=="torqueNm"?sample.Torque:sample.Thrust)));}
                    plot.Set(points,"RPM",column=="torqueNm"?"Момент, Н·м":"Тяга, Н");
                }
            } catch(Exception){plot.Set(Array.Empty<Vector2>(),"","","Заполните корректные характеристики для построения графика.");}
        }
        private void RefreshRpmChoices()
        {
            var choices=rows.Where(r=>r["rpm"]!=null).Select(r=>(double)r["rpm"]).Distinct().OrderBy(v=>v).ToList();
            string signature=string.Join("|",choices.Select(v=>v.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));if(signature==rpmSignature)return;
            rpmSignature=signature;rpmChoice?.RemoveFromHierarchy();rpmChoice=null;if(choices.Count==0)return;
            if(!choices.Contains(mapRpm))mapRpm=choices[0];
            var dropdown=new DroneDropdown("RPM",choices.Select(v=>v.ToString("G6")).ToList(),choices.IndexOf(mapRpm));rpmChoice=dropdown;graphToolbar.Add(dropdown);
            dropdown.RegisterValueChangedCallback(e=>{if(e.target==dropdown){mapRpm=choices[dropdown.index];RefreshPlot();}});
        }
        private void BuildRows()
        {
            gridHost.Clear();columnCells.Clear();var scroll=new ScrollView(ScrollViewMode.VerticalAndHorizontal);scroll.AddToClassList("drone-table-scroll");gridHost.Add(scroll);
            ThemeScroller(scroll.verticalScroller,false);ThemeScroller(scroll.horizontalScroller,true);
            var header=DroneProfileFields.Row(scroll);header.AddToClassList("drone-table-line");
            var properties=((JObject)rule["properties"]).Properties().ToList();
            foreach(var property in properties){
                string key=property.Name;var cell=CreateCell(header,key);cell.AddToClassList("drone-table-header-cell");
                var heading=DroneProfileFields.Row(cell);heading.AddToClassList("drone-table-column-heading");
                DroneProfileFields.Label(heading,DroneParameterSchema.Name(key)+"\n"+DroneParameterSchema.Unit(key));DroneHelp.Attach(heading,()=>DroneParameterSchema.Tooltip(key,property.Value));
                AddResizeHandle(cell,key);
            }
            for(int i=0;i<rows.Count;i++) {
                int index=i;var item=(JObject)rows[i];var row=DroneProfileFields.Row(scroll);row.AddToClassList("drone-table-line");
                foreach(var p in properties) {
                    string key=p.Name;var cell=CreateCell(row,key);
                    var propertyRule=DroneParameterSchema.Resolve(p.Value);string type=(string)propertyRule["type"];
                    if(type=="number" || type=="integer") {
                        var input=fields.Numeric(cell,"",propertyRule,item[key],path+"["+index+"]."+key,t=>item[key]=t);
                        bool optional=!((JArray)rule["required"]).Any(t=>(string)t==key);
                        if(optional)input.RegisterValueChangedCallback(e=>{if(string.IsNullOrWhiteSpace(e.newValue)){item.Remove(key);fields.ClearErrors(path+"["+index+"]."+key);input.EnableInClassList("invalid-field",false);changed();}});
                    } else if(type=="array") {
                        if(item[key]==null)item[key]=DroneParameterSchema.Default(p.Value,key);
                        fields.Field(cell,key,p.Value,item[key],path+"["+index+"]."+key,t=>item[key]=t);
                    } else {
                        if(item[key]==null)item[key]=DroneParameterSchema.Default(p.Value,key);
                        fields.Field(cell,key,p.Value,item[key],path+"["+index+"]."+key,t=>item[key]=t);
                    }
                }
                var remove=DroneProfileFields.Button(row,"×",()=> {fields.RemoveRow(rows,index,path);changed();BuildRows();RefreshPlot();});remove.AddToClassList("table-remove");
            }
            DroneProfileFields.ReadOnly(scroll);
        }
        private VisualElement CreateCell(VisualElement host,string key)
        {
            var cell=new VisualElement();cell.AddToClassList("drone-table-cell");host.Add(cell);
            if(!columnCells.TryGetValue(key,out var cells)){cells=new List<VisualElement>();columnCells[key]=cells;}cells.Add(cell);
            float width=columnWidths.TryGetValue(key,out var saved)?saved:160;cell.style.width=width;cell.style.minWidth=width;return cell;
        }
        private void SetColumnWidth(string key,float width)
        {
            width=Mathf.Clamp(width,110,640);columnWidths[key]=width;
            if(columnCells.TryGetValue(key,out var cells))foreach(var cell in cells){cell.style.width=width;cell.style.minWidth=width;}
        }
        private void AddResizeHandle(VisualElement cell,string key)
        {
            var handle=new VisualElement{focusable=true,tabIndex=0,tooltip="Перетащите границу для изменения ширины. Двойной щелчок сбрасывает ширину; стрелки ←/→ меняют её с клавиатуры."};handle.AddToClassList("drone-column-resizer");cell.Add(handle);
            bool resizing=false;float start=0,width=0;
            handle.RegisterCallback<PointerDownEvent>(evt=>{
                if(evt.button!=0)return;resizing=true;start=cell.WorldToLocal(evt.position).x;width=cell.resolvedStyle.width;handle.CapturePointer(evt.pointerId);evt.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(evt=>{if(!resizing)return;SetColumnWidth(key,width+cell.WorldToLocal(evt.position).x-start);evt.StopPropagation();});
            handle.RegisterCallback<PointerUpEvent>(evt=>{if(!resizing)return;resizing=false;if(handle.HasPointerCapture(evt.pointerId))handle.ReleasePointer(evt.pointerId);evt.StopPropagation();});
            handle.RegisterCallback<PointerCaptureOutEvent>(_=>resizing=false);
            handle.RegisterCallback<ClickEvent>(evt=>{if(evt.clickCount==2)SetColumnWidth(key,160);evt.StopPropagation();});
            handle.RegisterCallback<KeyDownEvent>(evt=>{if(evt.keyCode!=KeyCode.LeftArrow && evt.keyCode!=KeyCode.RightArrow)return;SetColumnWidth(key,cell.resolvedStyle.width+(evt.keyCode==KeyCode.RightArrow?12:-12));evt.StopPropagation();});
        }
        private async void Transfer(bool export)
        {
            if(busy)return;busy=true;changed();
            try {
                var targetRows=rows;string targetPath=path;
                var chosen=await DroneFileDialog.Pick(export,DroneProfileLibrary.Root,"CSV таблицы","table.csv","csv");
                if(panel==null || targetRows!=rows || targetPath!=path)return;
                if(chosen.Failed)throw new IOException(chosen.Error);if(string.IsNullOrEmpty(chosen.Path))return;
                if(export){File.WriteAllText(chosen.Path,DroneProfileCsv.Write(rows,rule));status("Таблица экспортирована.");}
                else {var imported=DroneProfileCsv.Read(File.ReadAllText(chosen.Path),rule);rows.RemoveAll();foreach(var row in imported.ToArray())rows.Add(row);
                    fields.ClearErrors(path);changed();BuildRows();RefreshPlot();status("Таблица импортирована. Проверьте полный профиль.");}
            }catch(Exception ex){status(ex.Message);}finally{busy=false;changed();}
        }
        private static void ThemeScroller(Scroller scroller,bool horizontal)
        {
            scroller.AddToClassList(horizontal?"graphite-horizontal-scroller":"graphite-scroller");
            scroller.lowButton.style.display=DisplayStyle.None;scroller.highButton.style.display=DisplayStyle.None;
        }
    }
}
