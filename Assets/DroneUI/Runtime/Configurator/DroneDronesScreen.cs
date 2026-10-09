using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DroneLab.Physics;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace DroneLab.UI
{
    internal sealed class DroneDronesScreen : IDisposable
    {
        private readonly VisualElement page,body,footer;
        private readonly Action closed;
        private readonly Label heading,state,message,identity;
        private readonly List<DroneProfileDocument> documents;
        private readonly Dictionary<string,string> errors=new Dictionary<string,string>(),input=new Dictionary<string,string>();
        private readonly List<Texture2D> thumbnails=new List<Texture2D>();
        private DroneProfileDocument selected,document;
        private string original,section="Model",inspectorSection;
        private bool newProfile,busy,disposed;
        private int rotorIndex,galleryRevision;
        private VisualElement navigation,inspector,prompt;
        private VisualElement toolsToolbar;
        private Label massEstimate;
        private readonly Dictionary<string,bool> expandedGroups=new Dictionary<string,bool>();
        private readonly Dictionary<string,Vector2> inspectorOffsets=new Dictionary<string,Vector2>();
        private readonly Dictionary<string,Button> viewButtons=new Dictionary<string,Button>(),toolButtons=new Dictionary<string,Button>();
        private bool wideInspector;
        private Button save,draftSave,cancel,back,galleryOpen,galleryCopy,galleryDelete;
        private VisualElement validationWindow,validationReturnFocus;
        private Label validationSaveError;
        private Button galleryCreate,galleryImport;
        private readonly Label galleryCount;
        private readonly Dictionary<string,VisualElement> galleryCards=new Dictionary<string,VisualElement>();
        private string gallerySearch="",galleryCategory="Все дроны",gallerySelection;
        private DroneProfileFields fields;
        private DroneModelViewport viewport,galleryRenderer;
        private DroneDataWindow dataWindow;
        private VisualElement dataReturnFocus;
        private byte[] preview;
        private IVisualElementScheduledItem statusTimer;

        public DroneDronesScreen(VisualElement host,Action closed)
        {
            this.closed=closed;documents=DroneProfileLibrary.LoadAll(out var warnings);
            page=new VisualElement();page.AddToClassList("scenario-page");page.AddToClassList("drone-page");host.Add(page);
            var sheet=Resources.Load<StyleSheet>("DroneLab/Configurator");if(sheet!=null)page.styleSheets.Add(sheet);
            var header=Box(page,"drone-header");DroneProfileFields.Label(header,"ДРОНЛАБ","scenario-brand");Box(header,"scenario-header-divider");
            heading=DroneProfileFields.Label(header,"КАТАЛОГ ДРОНОВ","scenario-title");
            identity=DroneProfileFields.Label(header,"","drone-profile-identity");
            state=DroneProfileFields.Label(header,"","drone-state");body=Box(page,"drone-body");footer=Box(page,"drone-footer");
            back=DroneProfileFields.Button(footer,"Назад",RequestClose);back.AddToClassList("drone-back");
            galleryCount=DroneProfileFields.Label(footer,"","scenario-muted");galleryCount.AddToClassList("drone-gallery-count");
            message=DroneProfileFields.Label(footer,"","drone-status");
            galleryOpen=DroneProfileFields.Button(footer,"Открыть",()=>{if(selected!=null&&!busy)Open(selected);});
            galleryCopy=DroneProfileFields.Button(footer,"Копировать",CopySelected);
            galleryDelete=DroneProfileFields.Button(footer,"Удалить",DeleteSelected);
            cancel=DroneProfileFields.Button(footer,"Отменить",Cancel);
            draftSave=DroneProfileFields.Button(footer,"Сохранить черновик",()=>Save(true));
            save=DroneProfileFields.Button(footer,"Проверить и сохранить",Validate);save.AddToClassList("primary");
            BuildGallery();
            if(!string.IsNullOrEmpty(warnings))Status(warnings,true);
        }
        private bool Dirty=>document!=null && (newProfile || document.Snapshot()!=original || preview!=null);
        private void BuildGallery()
        {
            galleryRevision++;viewport?.Dispose();viewport=null;galleryRenderer?.Dispose();galleryRenderer?.RemoveFromHierarchy();galleryRenderer=null;
            CloseValidation();CloseDataWindow();document=null;selected=null;body.Clear();heading.text="КАТАЛОГ ДРОНОВ";state.text="";errors.Clear();input.Clear();
            page.RemoveFromClassList("drone-editor-page");page.RemoveFromClassList("drone-wide-inspector");
            foreach(var texture in thumbnails)Object.Destroy(texture);thumbnails.Clear();
            page.AddToClassList("drone-gallery-page");body.AddToClassList("drone-gallery-body");
            statusTimer?.Pause();message.text="";message.EnableInClassList("error",false);
            DroneProfileFields.Label(body,"Каталог доступных дронов","scenario-hint");
            var toolbar=Box(body,"drone-gallery-toolbar");galleryCreate=DroneProfileFields.Button(toolbar,"Создать дрон",()=>{if(!busy)Open(DroneProfileLibrary.Create(),true);});
            galleryImport=DroneProfileFields.Button(toolbar,"Импорт профиля",ImportProfile);
            var search=new TextField("Поиск");search.SetValueWithoutNotify(gallerySearch);DroneTextInput.Configure(search);search.AddToClassList("scenario-search");toolbar.Add(search);
            search.tooltip="Поиск по названию или категории дрона";
            var categories=new List<string>{"Все дроны"};categories.AddRange(documents.Select(d=>d.Category).Distinct().OrderBy(c=>c,StringComparer.CurrentCulture));
            if(!categories.Contains(galleryCategory))galleryCategory="Все дроны";
            var kind=new DroneDropdown("",categories,categories.IndexOf(galleryCategory));kind.AddToClassList("environment-field");kind.AddToClassList("map-filter");toolbar.Add(kind);
            var scroll=Scroll(body,"drone-gallery-scroll");var grid=Box(scroll,"map-grid");
            void Select(DroneProfileDocument value) {
                selected=value;gallerySelection=value?.id;
                foreach(var pair in galleryCards)pair.Value.EnableInClassList("selected",pair.Key==gallerySelection);
                UpdateState();
            }
            void Fill() {
                foreach(var texture in thumbnails)Object.Destroy(texture);thumbnails.Clear();
                grid.Clear();galleryCards.Clear();var visible=documents.Where(x=>(galleryCategory=="Все дроны"||x.Category==galleryCategory) &&
                    (string.IsNullOrWhiteSpace(gallerySearch)||(x.Name+" "+x.Category).IndexOf(gallerySearch.Trim(),StringComparison.OrdinalIgnoreCase)>=0)).ToList();
                int count=0;var missing=new List<(DroneProfileDocument document,Image image)>();
                foreach(var item in visible) {
                    var card=Box(grid,"map-card");card.AddToClassList("drone-gallery-card");card.EnableInClassList("map-row-end",count++%3==2);
                    card.focusable=true;card.tabIndex=0;card.tooltip=item.Name+" · "+item.Category;galleryCards[item.id]=card;
                    var image=new Image{scaleMode=ScaleMode.ScaleAndCrop,pickingMode=PickingMode.Ignore};image.AddToClassList("drone-card-preview");card.Add(image);
                    image.RegisterCallback<GeometryChangedEvent>(evt=>{float height=evt.newRect.width*9f/16f;if(Math.Abs(image.resolvedStyle.height-height)>1)image.style.height=height;});
                    string path=Path.Combine(DroneProfileLibrary.Folder(item.id),"preview.png");
                    if(File.Exists(path)){try {var t=new Texture2D(2,2);if(t.LoadImage(File.ReadAllBytes(path))){thumbnails.Add(t);image.image=t;}else Object.Destroy(t);}catch(Exception ex){Debug.LogWarning(ex.Message);}}
                    if(image.image==null)missing.Add((item,image));
                    DroneProfileFields.Label(card,item.Name,"drone-card-title");
                    DroneProfileFields.Label(card,item.Category+" · "+(item.draft?"Черновик":"Профиль"),"drone-card-category");
                    DroneProfileFields.Label(card,$"{((JArray)item.profile["rotors"]).Count} ротора · {(double?)item.profile["massProperties"]?["massKg"]:0.###} кг","map-meta");
                    card.RegisterCallback<ClickEvent>(evt=>{if(evt.button!=0)return;Select(item);card.Focus();if(evt.clickCount==2)Open(item);});
                    card.RegisterCallback<FocusInEvent>(_=>Select(item));
                    card.RegisterCallback<KeyDownEvent>(evt=>{
                        if(evt.keyCode==KeyCode.Return||evt.keyCode==KeyCode.KeypadEnter){Open(item);evt.StopPropagation();}
                        else if(evt.keyCode==KeyCode.Delete){Select(item);DeleteSelected();evt.StopPropagation();}
                        else {
                            int delta=evt.keyCode==KeyCode.RightArrow?1:evt.keyCode==KeyCode.LeftArrow?-1:evt.keyCode==KeyCode.DownArrow?3:evt.keyCode==KeyCode.UpArrow?-3:0;
                            if(delta!=0){int next=Mathf.Clamp(visible.IndexOf(item)+delta,0,visible.Count-1);var target=galleryCards[visible[next].id];target.Focus();scroll.ScrollTo(target);evt.StopPropagation();}
                        }
                    });
                }
                if(count==0) {
                    var empty=Box(grid,"drone-gallery-empty");DroneProfileFields.Label(empty,documents.Count==0?"В каталоге пока нет дронов":"Дроны не найдены","drone-panel-title");
                    DroneProfileFields.Label(empty,documents.Count==0?"Создайте дрон или импортируйте профиль.":"Измените запрос или выберите другую категорию.","scenario-hint");
                    if(documents.Count>0)DroneProfileFields.Button(empty,"Сбросить фильтры",()=>{gallerySearch="";galleryCategory="Все дроны";search.SetValueWithoutNotify("");kind.SetValueWithoutNotify(galleryCategory);Fill();});
                }
                Select(visible.FirstOrDefault(x=>x.id==gallerySelection));
                galleryCount.text=$"Дронов в каталоге: {documents.Count}"+(visible.Count==documents.Count?"":$" · Показано: {visible.Count}");
                GenerateGalleryPreviews(missing,++galleryRevision);
            }
            search.RegisterValueChangedCallback(evt=>{if(evt.target!=search)return;gallerySearch=evt.newValue;Fill();});
            kind.RegisterValueChangedCallback(evt=>{if(evt.target!=kind)return;galleryCategory=kind.value;Fill();});Fill();UpdateState();
        }
        private void CopySelected()
        {
            if(selected==null||busy)return;var copy=selected.Copy();copy.sourceModel=DroneProfileLibrary.ModelPath(selected);
            copy.id=Guid.NewGuid().ToString("N");copy.profile["metadata"]["name"]=selected.Name+" · копия";copy.draft=true;Open(copy,true);
        }
        private void DeleteSelected()
        {
            if(selected==null||busy)return;var item=selected;
            Ask("Удалить дрон?","«"+item.Name+"» будет удалён из каталога вместе с сохранённым профилем и моделью.",
                ("Отмена",()=>{}),("Удалить",()=>{
                    ++galleryRevision;galleryRenderer?.Dispose();galleryRenderer?.RemoveFromHierarchy();galleryRenderer=null;
                    try {DroneProfileLibrary.Delete(item);documents.RemoveAll(d=>d.id==item.id);gallerySelection=null;
                        if(PlayerPrefs.GetString("DroneLab.SelectedDrone")==item.id)PlayerPrefs.DeleteKey("DroneLab.SelectedDrone");
                        BuildGallery();Status("Дрон удалён.");
                    }catch(Exception ex){Status("Не удалось удалить дрон: "+ex.Message,true);}
                }));
        }
        private async void GenerateGalleryPreviews(List<(DroneProfileDocument document,Image image)> entries,int revision)
        {
            galleryRenderer?.Dispose();galleryRenderer?.RemoveFromHierarchy();galleryRenderer=null;if(entries.Count==0)return;galleryRenderer=new DroneModelViewport();
            galleryRenderer.style.position=Position.Absolute;galleryRenderer.style.left=-3000;galleryRenderer.style.width=640;galleryRenderer.style.height=360;page.Add(galleryRenderer);
            var renderer=galleryRenderer;
            try {
                foreach(var entry in entries) {
                    if(disposed || revision!=galleryRevision)break;
                    renderer.SetDocument(entry.document.Copy());string model=DroneProfileLibrary.ModelPath(entry.document);
                    if(model!=null && File.Exists(model))await renderer.LoadModel(model,false);else renderer.Frame();
                    await Task.Delay(100);if(disposed || revision!=galleryRevision)break;
                    byte[] png=renderer.Capture();string folder=DroneProfileLibrary.Folder(entry.document.id);Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"preview.png"),png);
                    var texture=new Texture2D(2,2);texture.LoadImage(png);thumbnails.Add(texture);entry.image.image=texture;
                }
            }catch(Exception ex){if(!disposed)Status("Не удалось построить превью: "+ex.Message,true);}
            finally {renderer.Dispose();renderer.RemoveFromHierarchy();if(galleryRenderer==renderer)galleryRenderer=null;}
        }
        private void Open(DroneProfileDocument value,bool created=false)
        {
            ++galleryRevision;galleryRenderer?.Dispose();galleryRenderer?.RemoveFromHierarchy();galleryRenderer=null;
            viewport?.Dispose();CloseValidation();CloseDataWindow();selected=value;document=value.Copy();newProfile=created;original=document.Snapshot();preview=null;errors.Clear();input.Clear();rotorIndex=0;section="Model";inspectorSection=null;inspectorOffsets.Clear();expandedGroups.Clear();
            foreach(var pair in document.unfinishedInputs){input[pair.Key]=pair.Value;errors[pair.Key]="Незавершённое значение из черновика.";}
            body.Clear();page.RemoveFromClassList("drone-gallery-page");page.AddToClassList("drone-editor-page");page.EnableInClassList("drone-wide-inspector",wideInspector);body.RemoveFromClassList("drone-gallery-body");heading.text="НАСТРОЙКА ДРОНА";
            navigation=Scroll(body,"drone-navigation");var center=Box(body,"drone-center");inspector=Box(body,"drone-inspector");
            inspector.focusable=true;var toolbar=Box(center,"drone-viewport-toolbar");viewButtons.Clear();toolButtons.Clear();
            foreach(var view in new[]{("3D","3D"),("Сверху","Top"),("Спереди","Front"),("Сбоку","Side")}) {
                string key=view.Item2;viewButtons[key]=DroneProfileFields.Button(toolbar,view.Item1,()=>{viewport.View(key);UpdateViewportControls();});
            }
            var cameraInfo=Box(toolbar,"drone-camera-info");
            DroneProfileFields.Label(cameraInfo,"Вращение — ПКМ · Перемещение — СКМ · Масштаб — колесо","drone-camera-note");
            DroneHelp.Attach(cameraInfo,()=>"X вправо · Y вверх · Z вперёд. Физические координаты и габариты задаются в метрах. Масштаб импортированной модели задаёт метры на одну исходную единицу.\n\nПравая кнопка мыши вращает камеру, средняя перемещает её, колесо меняет масштаб. Кнопка 3D возвращает обзор всего дрона; Сверху, Спереди и Сбоку показывают ортографические виды.");
            toolsToolbar=Box(center,"drone-rotor-tools");
            foreach(var tool in new[]{("Положение точки","Move"),("Направление тяги","Axis")}) {
                string key=tool.Item2;toolButtons[key]=DroneProfileFields.Button(toolsToolbar,tool.Item1,()=>{viewport.Tool=key;UpdateViewportControls();});
            }
            DroneHelp.Attach(toolsToolbar,()=>"Выберите точку ротора. Инструмент «Положение точки» перемещает её в плоскости вида или вдоль выбранной оси. «Направление тяги» меняет направление стрелки. Привязка округляет положение до 1 см. Точные компоненты доступны справа.");
            var snapRow=Box(toolsToolbar,"drone-viewport-snap");
            var snap=new DroneSwitch("Привязка к сетке · 1 см",false);snapRow.Add(snap);snap.ValueChanged+=value=>viewport.Snap=value;
            DroneHelp.Attach(snapRow,()=>"При перетаскивании точки ротора координаты округляются до ближайшего сантиметра. Направление тяги не округляется. Точный ввод координат остаётся доступен независимо от привязки.");
            viewport=new DroneModelViewport();center.Add(viewport);viewport.SetDocument(document);
            if(DroneProfileLibrary.ModelPath(document)==null && (created || document.id.StartsWith("builtin-") && !File.Exists(Path.Combine(DroneProfileLibrary.Folder(document.id),"document.json"))))viewport.Frame();
            original=document.Snapshot();
            fields=new DroneProfileFields(Changed,RebuildInspector,OpenTable,errors,input,expandedGroups);
            viewport.Selected+=i=> {rotorIndex=i;section="Rotor";BuildNavigation();RebuildInspector();};
            viewport.Changed+=()=> {if(viewport.LastEditedField!=null)fields.ClearErrors(viewport.LastEditedField);Changed();if(section=="Rotor" || section=="Mass" && viewport.LastEditedField!=null)RebuildInspector();};
            viewport.SelectionCleared+=()=>{if(section=="Rotor"){BuildNavigation();RebuildInspector();}};
            viewport.Status+=s=>Status(s);BuildNavigation();RebuildInspector();UpdateState();
            string model=DroneProfileLibrary.ModelPath(document);if(model!=null)LoadExisting(model);
        }
        private async void LoadExisting(string path)
        {
            busy=true;UpdateState();try {await viewport.LoadModel(path,false);if(!disposed && document!=null){RebuildInspector();}}
            catch(Exception ex){Status(ex.Message,true);}finally{busy=false;if(!disposed)UpdateState();}
        }
        private void BuildNavigation()
        {
            var offset=((ScrollView)navigation).scrollOffset;
            navigation.Clear();DroneProfileFields.Label(navigation,"ПРОФИЛЬ","drone-panel-title");
            foreach(var item in new[]{("Модель","Model"),("Масса и инерция","Mass"),("Роторы","Rotor"),("Аэродинамика","Aero"),("Батарея и питание","Power"),("Температура","Thermal"),("Модули","Modules"),("Источники и метаданные","Sources")}) {
                string key=item.Item2;var b=DroneProfileFields.Button(navigation,item.Item1,()=> {section=key;if(key=="Rotor")viewport.Select(rotorIndex);BuildNavigation();RebuildInspector();});b.EnableInClassList("active",section==key);
                var icon=new DroneMenuIcon(key=="Model"||key=="Rotor"?MenuIconKind.Drone:key=="Aero"?MenuIconKind.Wind:key=="Sources"?MenuIconKind.Book:MenuIconKind.Settings);icon.AddToClassList("drone-nav-icon");b.Add(icon);
                if(key=="Rotor" && section=="Rotor") { foreach(var pair in ((JArray)document.profile["rotors"]).Select((r,i)=>(r,i))) {
                    int index=pair.i;string rotorSection="Rotor";var rotor=DroneProfileFields.Button(navigation,(string)pair.r["rotorId"],()=>{rotorIndex=index;section=rotorSection;viewport.Select(index);BuildNavigation();RebuildInspector();});rotor.AddToClassList("rotor-navigation");rotor.EnableInClassList("active",viewport.SelectedRotor==index);
                }
                    var add=DroneProfileFields.Button(navigation,"+ Добавить ротор",AddRotor);add.AddToClassList("rotor-navigation");
                }
            }
            navigation.schedule.Execute(()=>((ScrollView)navigation).scrollOffset=offset);
        }
        private void RebuildInspector()
        {
            if(document==null || inspector==null || disposed)return;
            var previous=inspector.Q<ScrollView>();if(previous!=null && inspectorSection!=null)inspectorOffsets[inspectorSection]=previous.scrollOffset;
            var rotors=(JArray)document.profile["rotors"];rotorIndex=Mathf.Clamp(rotorIndex,0,Math.Max(0,rotors.Count-1));
            massEstimate=null;inspector.Clear();inspectorSection=section;var header=Box(inspector,"drone-inspector-heading");
            string title=section=="Model"?"МОДЕЛЬ И ПРОФИЛЬ":section=="Mass"?"МАССА И ИНЕРЦИЯ":section=="Rotor"?(viewport.SelectedRotor>=0?"РОТОР "+(string)rotors[rotorIndex]["rotorId"]:"РОТОРЫ"):section=="Aero"?"АЭРОДИНАМИКА":section=="Power"?"БАТАРЕЯ И ПИТАНИЕ":section=="Thermal"?"ТЕМПЕРАТУРА":section=="Modules"?"МОДУЛИ":section=="Sources"?"ИСТОЧНИКИ":"ПРОВЕРКА";
            DroneProfileFields.Label(header,title,"drone-panel-title");
            var width=DroneProfileFields.Button(header,wideInspector?"Обычная ширина":"Больше места",()=>{wideInspector=!wideInspector;page.EnableInClassList("drone-wide-inspector",wideInspector);RebuildInspector();});width.AddToClassList("drone-width-button");
            var scroll=Scroll(inspector,"drone-inspector-scroll");
            var rotor=rotors.Count>0?(JObject)rotors[rotorIndex]:null;string rp="rotors["+rotorIndex+"]";
            void Group(string key,string def){
                if(document.profile[key]==null)document.profile[key]=DroneParameterSchema.Default(DroneParameterSchema.ObjectRule("DroneProfile")["properties"][key],key);
                fields.Object(scroll,(JObject)document.profile[key],def,key);}
            switch(section) {
                case "Model":ModelInspector(scroll);break;
                case "Mass":MassInspector(scroll);break;
                case "Rotor":if(rotor!=null && viewport.SelectedRotor>=0){
                    DroneProfileFields.Button(scroll,"Открыть характеристики винта",()=>OpenPerformance(rotor,rp));fields.Object(scroll,rotor,"RotorProfile",rp,"performance");
                    DroneProfileFields.Button(scroll,"Применить двигатель и винт ко всем",()=> {foreach(var r in rotors.OfType<JObject>())if(r!=rotor)foreach(string key in new[]{"motor","propeller","performance","advancedAerodynamics","operatingEnvelope"}){if(rotor[key]!=null)r[key]=rotor[key].DeepClone();else r.Remove(key);fields.ClearErrors("rotors["+rotors.IndexOf(r)+"]."+key);}Changed();RebuildInspector();});
                    var remove=DroneProfileFields.Button(scroll,"Удалить ротор «"+(string)rotor["rotorId"]+"»",()=>Ask("Удалить ротор?","Точка ротора, двигатель, винт и характеристики будут удалены из текущего профиля.",("Отмена",()=>{}),("Удалить",RemoveRotor)));remove.SetEnabled(rotors.Count>1);
                }else DroneProfileFields.Label(scroll,"Выберите ротор в списке слева или его точку на 3D-виде. Щелчок по свободному фону снимает выбор.","scenario-hint");break;
                case "Aero":Group("bodyAerodynamics","BodyAerodynamicsProfile");
                    if(document.profile["groundEffect"]!=null)fields.Field(scroll,"groundEffect",DroneParameterSchema.ObjectRule("DroneProfile")["properties"]["groundEffect"],document.profile["groundEffect"],"groundEffect",t=>document.profile["groundEffect"]=t);
                    else DroneProfileFields.Button(scroll,"+ Параметры экрана",()=>{document.profile["groundEffect"]=DroneParameterSchema.Default(DroneParameterSchema.ObjectRule("DroneProfile")["properties"]["groundEffect"]);Changed();RebuildInspector();});break;
                case "Power":
                    fields.Object(scroll,(JObject)document.profile["powerSystem"],"PowerSystemProfile","powerSystem","thermalEnabled");
                    DroneProfileFields.Label(scroll,"Электрические параметры моторов и ESC задаются у каждого ротора. Тепловые параметры компонентов — в «Температуре», включение расчёта — в «Модулях».","scenario-hint");break;
                case "Thermal":
                    DroneProfileFields.Label(scroll,"Тепловой расчёт включается в «Модулях». Здесь задаются тепловые данные батареи, моторов и ESC; температуры — в Кельвинах. Температуру воздуха задаёт выбранное окружение.","scenario-hint");
                    var battery=(JObject)document.profile["powerSystem"]["battery"];
                    if((string)battery["mode"]!="None"){
                        DroneProfileFields.Label(scroll,"БАТАРЕЯ","drone-panel-title");ThermalField(scroll,battery,"BatteryProfile","thermal","powerSystem.battery");
                    }
                    foreach(var r in rotors.OfType<JObject>()) {
                        DroneProfileFields.Label(scroll,(string)r["rotorId"],"drone-panel-title");
                        if(r["motor"]?["electrical"] is JObject electric)foreach(string key in new[]{"thermal","escThermal","resistanceReferenceTemperatureK","resistanceTemperatureCoefficientPerK"})ThermalField(scroll,electric,"MotorElectricalProfile",key,"rotors["+rotors.IndexOf(r)+"].motor.electrical");
                        else DroneProfileFields.Label(scroll,"Добавьте электрические параметры на странице ротора.","scenario-hint");
                    }break;
                case "Modules":
                    fields.Object(scroll,(JObject)document.profile["physicsConfiguration"]["modules"],"PhysicsModulesProfile","physicsConfiguration.modules");
                    var power=(JObject)document.profile["powerSystem"];
                    fields.Field(scroll,"thermalEnabled",DroneParameterSchema.ObjectRule("PowerSystemProfile")["properties"]["thermalEnabled"],power["thermalEnabled"]??new JValue(false),"powerSystem.thermalEnabled",v=>power["thermalEnabled"]=v);break;
                case "Sources":
                    if(document.profile["parameterProvenance"] is JArray provenance)fields.Field(scroll,"parameterProvenance",DroneParameterSchema.ObjectRule("DroneProfile")["properties"]["parameterProvenance"],provenance,"parameterProvenance",v=>document.profile["parameterProvenance"]=v);
                    else DroneProfileFields.Button(scroll,"+ Источники параметров",()=>{document.profile["parameterProvenance"]=new JArray();Changed();RebuildInspector();});
                    if(document.profile["derived"] is JObject derived)fields.Object(scroll,derived,"DerivedProfile","derived");
                    else DroneProfileFields.Button(scroll,"+ Производные метаданные",()=>{document.profile["derived"]=new JObject();Changed();RebuildInspector();});break;
            }
            if(inspectorOffsets.TryGetValue(section,out var savedOffset))scroll.schedule.Execute(()=>scroll.scrollOffset=savedOffset);
            DroneProfileFields.ReadOnly(scroll);UpdateViewportControls();UpdateState();
        }
        private void ThermalField(VisualElement host,JObject owner,string definition,string key,string parentPath)
        {
            string path=parentPath+"."+key;var rule=DroneParameterSchema.ObjectRule(definition)["properties"][key];
            if(owner[key]==null){
                var row=DroneProfileFields.Row(host);DroneProfileFields.Button(row,"+ "+DroneParameterSchema.Name(key),()=>{owner[key]=DroneParameterSchema.Default(rule,key);fields.ClearErrors(path);Changed();RebuildInspector();});
                DroneHelp.Attach(row,()=>DroneParameterSchema.Tooltip(key,rule));
            }else fields.Field(host,key,rule,owner[key],path,value=>owner[key]=value,()=>{owner.Remove(key);fields.ClearErrors(path);Changed();RebuildInspector();});
        }
        private void MassInspector(VisualElement host)
        {
            var mass=(JObject)document.profile["massProperties"];fields.Object(host,mass,"MassProperties","massProperties","inertia");
            DroneProfileFields.Label(host,"На 3D-виде показана коробка габаритов. Выберите COM и перемещайте его в плоскости вида; концы цветных осей ограничивают движение по X/Y/Z. Щелчок по фону снимает выбор.","scenario-hint");
            var inertia=(JObject)mass["inertia"];var rule=DroneParameterSchema.ObjectRule("InertiaProfile");
            var row=DroneProfileFields.Row(host);row.AddToClassList("drone-parameter-row");
            var keys=((JArray)rule["properties"]["mode"]["enum"]).Select(v=>(string)v).ToList();
            var mode=new DroneDropdown("Расчёт инерции",keys.Select(DroneParameterSchema.Name).ToList(),Math.Max(0,keys.IndexOf((string)inertia["mode"])));mode.AddToClassList("environment-field");row.Add(mode);
            mode.RegisterValueChangedCallback(evt=>{if(evt.target!=mode)return;inertia["mode"]=keys[mode.index];Changed();RebuildInspector();});
            DroneHelp.Attach(row,()=>DroneParameterSchema.Tooltip("inertia",DroneParameterSchema.ObjectRule("MassProperties")["properties"]["inertia"]));
            if((string)inertia["mode"]=="AutoBox") {
                massEstimate=DroneProfileFields.Label(host,"","drone-inertia-estimate");UpdateMassEstimate();
            } else {
                foreach(string key in new[]{"principalMomentsKgM2","principalAxesRotationXyzw"}) {
                    if(inertia[key]==null)inertia[key]=DroneParameterSchema.Default(rule["properties"][key],key);
                    fields.Field(host,key,rule["properties"][key],inertia[key],"massProperties.inertia."+key,value=>inertia[key]=value);
                }
            }
        }
        private void UpdateMassEstimate()
        {
            if(massEstimate==null || document==null)return;
            const string explanation="\nНе учитывает отдельное размещение батареи, моторов и нагрузки; для точных данных используйте главные моменты из измерений или CAD.";
            bool invalid=errors.Keys.Any(path=>path=="massProperties.massKg" || path=="massProperties.dimensionsM" || path.StartsWith("massProperties.dimensionsM["));
            if(invalid){massEstimate.text="Оценка однородной коробки: завершите корректный ввод массы и габаритов."+explanation;return;}
            var mass=document.profile["massProperties"];
            var estimate=PhysicsMath.BoxInertia((double)mass["massKg"],DVector3.From(((JArray)mass["dimensionsM"]).Select(v=>(double)v).ToArray()));
            massEstimate.text=$"Оценка однородной коробки: Ix = {estimate.X:G6}; Iy = {estimate.Y:G6}; Iz = {estimate.Z:G6} кг·м²."+explanation;
        }
        private void ModelInspector(VisualElement host)
        {
            var metadata=(JObject)document.profile["metadata"];
            fields.Field(host,"name",DroneParameterSchema.ObjectRule("Metadata")["properties"]["name"],metadata["name"],"metadata.name",v=>metadata["name"]=v);
            foreach(string key in new[]{"manufacturer","model","description"})fields.OptionalText(host,metadata,key,"metadata."+key);
            var category=new TextField("Категория");category.SetValueWithoutNotify(document.Category);category.AddToClassList("environment-field");DroneTextInput.Configure(category);
            var categoryRow=DroneProfileFields.Row(host);categoryRow.AddToClassList("drone-parameter-row");categoryRow.Add(category);
            category.RegisterValueChangedCallback(evt=>{if(evt.target!=category)return;document.category=evt.newValue;Changed();});
            DroneHelp.Attach(categoryRow,()=>"Метка для группировки дронов в каталоге. Например: Учебные, Исследовательские, FPV или Грузовые. Можно указать свою категорию. Пустое значение означает «Без категории». Метка сохраняется в пакете профиля и не влияет на расчёт физики.");
            DroneProfileFields.Button(host,"Импорт GLB / glTF",ImportModel);
            DroneProfileFields.Label(host,"GLB 2.0 с встроенными текстурами — предпочтительный формат. glTF поддерживается вместе с его локальными текстурами и .bin. FBX/OBJ предварительно экспортируйте в GLB.","scenario-hint");
            if(viewport.HasImportedModel) {
                DroneProfileFields.Label(host,"НАСТРОЙКА ИМПОРТИРОВАННОЙ МОДЕЛИ","drone-panel-title");
                var coordinates=(JObject)document.profile["coordinateSystem"];
                fields.Field(host,"modelScaleMetersPerUnit",DroneParameterSchema.ObjectRule("CoordinateSystem")["properties"]["modelScaleMetersPerUnit"],coordinates["modelScaleMetersPerUnit"],"coordinateSystem.modelScaleMetersPerUnit",v=>coordinates["modelScaleMetersPerUnit"]=v);
                DroneProfileFields.Label(host,"При первом импорте масштаб оценивается по наибольшему габариту в разделе «Масса и инерция». Изменение этих габаритов позже не масштабирует модель автоматически.","scenario-hint");
                var rotationRule=new JObject{["type"]="array",["minItems"]=3,["maxItems"]=3,["items"]=new JObject{["type"]="number"}};
                fields.Field(host,"rotationEulerDeg",rotationRule,document.visual["rotationEulerDeg"],"visual.rotationEulerDeg",v=>{document.visual["rotationEulerDeg"]=v;viewport.ApplyTransform();});
                var centerRow=DroneProfileFields.Row(host);centerRow.AddToClassList("drone-parameter-row");
                var center=new DroneSwitch("Центрировать модель",(bool?)document.visual["centerModel"]??true);centerRow.Add(center);
                center.ValueChanged+=value=>{document.visual["centerModel"]=value;viewport.ApplyTransform();Changed();};
                DroneHelp.Attach(centerRow,()=>"Переносит центр визуальных границ модели в начало локальных координат. Центр масс и физические точки роторов не изменяются.");
                var dimensions=DroneProfileFields.Row(host);dimensions.AddToClassList("drone-model-dimensions-action");
                var copy=DroneProfileFields.Button(dimensions,"Взять физические размеры из модели",()=>{
                    if(busy)return;var bounds=viewport.ModelBounds();
                    Ask("Обновить физические размеры?",$"В разделе «Масса и инерция» будут установлены габариты X/Y/Z: {bounds.size.x:0.###} / {bounds.size.y:0.###} / {bounds.size.z:0.###} м. Они учитывают текущий масштаб и поворот модели. Точки роторов останутся прежними.",
                        ("Отмена",()=>{}),("Обновить размеры",()=>{document.profile["massProperties"]["dimensionsM"]=DroneModelViewport.Array(bounds.size);fields.ClearErrors("massProperties.dimensionsM");Changed();Status("Физические размеры обновлены из модели.");}));
                });
                DroneHelp.Attach(dimensions,()=>"Переносит размеры ограничивающей коробки импортированной модели (после масштаба и поворота) в физические габариты X/Y/Z. Это меняет исходные данные для автоматического расчёта инерции. Включённые в модель антенны и винты тоже могут увеличить её границы. Проверьте, соответствуют ли эти размеры реальному корпусу.");
                var scaleRow=DroneProfileFields.Row(host);scaleRow.AddToClassList("drone-model-dimensions-action");
                DroneProfileFields.Button(scaleRow,"Масштабировать модель по физическим размерам",()=>{
                    if(busy)return;try{viewport.ScaleModelToPhysicalDimensions();fields.ClearErrors("coordinateSystem.modelScaleMetersPerUnit");Changed();RebuildInspector();Status("Масштаб модели обновлён по физическим габаритам. Пропорции сохранены.");}catch(Exception ex){Status(ex.Message,true);}
                });
                DroneHelp.Attach(scaleRow,()=>"Подбирает единый масштаб, чтобы наибольший габарит модели после её поворота совпал с наибольшим физическим габаритом X/Y/Z. Пропорции сохраняются: остальные размеры могут отличаться. Физические габариты, центр масс и точки роторов не изменяются.");
            } else DroneProfileFields.Label(host,"Сейчас показана схема из профиля: корпус, точки роторов и размеры винтов. После успешного импорта её заменит ваша модель; появятся настройки масштаба, поворота и центрирования.","scenario-hint");
            DroneProfileFields.Button(host,"Сделать превью для галереи",()=>{viewport.SaveView();preview=viewport.Capture();Changed();Status("Превью подготовлено. Сохраните профиль или черновик.");});
        }
        private void UpdateViewportControls()
        {
            if(viewport==null)return;viewport.SetEditContext(section=="Rotor",section=="Mass");
            toolsToolbar.style.display=viewport.EditingEnabled?DisplayStyle.Flex:DisplayStyle.None;
            foreach(var pair in toolButtons)pair.Value.EnableInClassList("active",pair.Key==viewport.Tool);
            foreach(var pair in viewButtons)pair.Value.EnableInClassList("active",pair.Key==viewport.ViewName);
        }
        private void OpenTable(JArray rows,JToken rule,string path,string title)=>OpenDataWindow(rows,rule,path,title,null,null);
        private void OpenPerformance(JObject rotor,string path)=>OpenDataWindow(rotor["performance"],DroneParameterSchema.ObjectRule("RotorPerformanceProfile"),path+".performance","Характеристики винта",rotor,path);
        private void OpenDataWindow(JToken source,JToken rule,string path,string title,JObject rotor,string rotorPath)
        {
            if(dataWindow!=null || busy || document==null)return;var owner=document;dataReturnFocus=page.panel.focusController.focusedElement as VisualElement;
            dataWindow=new DroneDataWindow(source,rule,path,title,document.Name+(rotor==null?"":" · Ротор "+(string)rotor["rotorId"]),errors,input,
                ()=>{if(document!=owner)return;try{var value=dataWindow.Buffer.BuildValue();source.Replace(value);fields.ClearErrors(path);CloseDataWindow();Changed();RebuildInspector();Status("Изменения применены. Сохраните профиль.");}catch(Exception ex){Status(ex.Message,true);}},
                CloseDataWindow,RequestDataClose,rotor,rotorPath);page.Add(dataWindow);UpdateState();
        }
        private void RequestDataClose()
        {
            if(dataWindow==null)return;if(dataWindow.Busy)return;
            if(!dataWindow.Buffer.Dirty){CloseDataWindow();return;}
            Ask("Отменить изменения окна?","Изменения таблицы или характеристик ещё не применены к профилю.",("Продолжить",()=>{}),("Отменить изменения",CloseDataWindow));
        }
        private void CloseDataWindow()
        {
            if(dataWindow==null)return;dataWindow.RemoveFromHierarchy();dataWindow=null;
            if(dataReturnFocus?.parent!=null)dataReturnFocus.Focus();else inspector?.Focus();dataReturnFocus=null;
            if(!disposed)UpdateState();
        }
        private void Changed()
        {
            if(document==null || disposed)return;document.unfinishedInputs.Clear();foreach(var key in errors.Keys)if(input.TryGetValue(key,out var value))document.unfinishedInputs[key]=value;
            if(document.profile["coordinateSystem"]?["modelScaleMetersPerUnit"] is JValue scale)document.visual["scale"]=scale.DeepClone();
            viewport?.ApplyTransform();viewport?.GeometryChanged();UpdateMassEstimate();UpdateViewportControls();UpdateState();
        }
        private void UpdateState()
        {
            bool editing=document!=null;state.text=!editing?"":Dirty?"Изменения не сохранены":document.draft?"Черновик":"Профиль сохранён";
            identity.text=document?.Name??"";identity.style.display=editing?DisplayStyle.Flex:DisplayStyle.None;
            galleryCount.style.display=editing?DisplayStyle.None:DisplayStyle.Flex;
            foreach(var button in new[]{galleryOpen,galleryCopy,galleryDelete}) {button.style.display=editing?DisplayStyle.None:DisplayStyle.Flex;button.SetEnabled(!busy&&selected!=null);}
            galleryCreate?.SetEnabled(!busy);galleryImport?.SetEnabled(!busy);
            save.style.display=editing && (Dirty || document.draft)?DisplayStyle.Flex:DisplayStyle.None;
            draftSave.style.display=editing && Dirty?DisplayStyle.Flex:DisplayStyle.None;cancel.style.display=editing && Dirty?DisplayStyle.Flex:DisplayStyle.None;
            bool available=!busy && dataWindow==null && validationWindow==null;
            save.SetEnabled(available);draftSave.SetEnabled(available);cancel.SetEnabled(available);
            body.SetEnabled(!busy && validationWindow==null);
        }
        private bool Save(bool asDraft,string approvedSnapshot=null)
        {
            if(document==null || busy)return false;
            if(!asDraft && !DroneProfileEdits.MatchesValidation(document,approvedSnapshot)){CloseValidation();Validate();return false;}
            if(!asDraft && errors.Count>0){Status("Исправьте незавершённые значения или сохраните черновик.",true);return false;}
            try {
                viewport.SaveView();DroneProfileLibrary.Save(document,asDraft);
                if(preview!=null)File.WriteAllBytes(Path.Combine(DroneProfileLibrary.Folder(document.id),"preview.png"),preview);
                else if(!File.Exists(Path.Combine(DroneProfileLibrary.Folder(document.id),"preview.png")))File.WriteAllBytes(Path.Combine(DroneProfileLibrary.Folder(document.id),"preview.png"),viewport.Capture());
                if(!asDraft)PlayerPrefs.SetString("DroneLab.SelectedDrone",document.id);
                preview=null;newProfile=false;original=document.Snapshot();var index=documents.FindIndex(x=>x.id==document.id);
                selected=document.Copy();gallerySelection=selected.id;if(index>=0)documents[index]=selected;else documents.Add(selected);
                Status(asDraft?"Черновик сохранён.":"Профиль сохранён и доступен для выбора в симуляции.");UpdateState();return true;
            }catch(Exception ex){Status(ex.Message,true);if(validationSaveError!=null){validationSaveError.text=ex.Message;validationSaveError.style.display=DisplayStyle.Flex;}return false;}
        }
        private void Cancel(){if(selected==null)return;Open(selected,newProfile);Status("Изменения отменены.");}
        private void Guard(Action action)
        {
            if(busy){Status("Дождитесь завершения загрузки модели.");return;}
            if(!Dirty){action();return;}
            Ask("Сохранить изменения?","Профиль изменён. Можно сохранить черновик, продолжить редактирование или отказаться от изменений.",
                ("Продолжить",()=>{}),("Не сохранять",action),("Сохранить черновик",()=>{if(Save(true))action();}));
        }
        public void RequestClose()
        {
            if(prompt!=null){prompt.RemoveFromHierarchy();prompt=null;if(dataWindow!=null)dataWindow.Focus();return;}
            if(validationWindow!=null){CloseValidation();return;}
            if(dataWindow!=null){RequestDataClose();return;}
            Guard(()=> {if(document!=null){BuildGallery();}else{Dispose();page.RemoveFromHierarchy();closed();}});
        }
        private void Ask(string title,string text,params (string name,Action action)[] actions)
        {
            prompt?.RemoveFromHierarchy();prompt=Box(page,"scenario-prompt");var card=Box(prompt,"scenario-prompt-card");DroneProfileFields.Label(card,title,"scenario-panel-title");DroneProfileFields.Label(card,text,"scenario-hint");
            var row=DroneProfileFields.Row(card);var buttons=new List<Button>();
            foreach(var a in actions)buttons.Add(DroneProfileFields.Button(row,a.name,()=>{prompt.RemoveFromHierarchy();prompt=null;if(dataWindow!=null)dataWindow.Focus();a.action();}));
            prompt.RegisterCallback<KeyDownEvent>(evt=>{
                if(evt.keyCode!=KeyCode.Tab || buttons.Count==0)return;
                int index=buttons.FindIndex(button=>button==page.panel.focusController.focusedElement);
                buttons[(index+(evt.shiftKey?-1:1)+buttons.Count)%buttons.Count].Focus();evt.StopPropagation();evt.PreventDefault();
            },TrickleDown.TrickleDown);
            if(buttons.Count>0)prompt.schedule.Execute(()=>buttons[0].Focus());
        }
        private void AddRotor()
        {
            var rotors=(JArray)document.profile["rotors"];var rotor=rotors.Count>0?(JObject)rotors[rotorIndex].DeepClone():(JObject)DroneParameterSchema.Default(DroneParameterSchema.ObjectRule("DroneProfile")["properties"]["rotors"]["items"]);
            int id=1;while(rotors.Any(r=>(string)r["rotorId"]=="M"+id))id++;rotor["rotorId"]="M"+id;
            rotor["geometry"]["positionLocalM"]=new JArray(0.0,0.0,0.0);rotors.Add(rotor);rotorIndex=rotors.Count-1;viewport.Select(rotorIndex);section="Rotor";Changed();viewport.GeometryChanged(true);BuildNavigation();RebuildInspector();
        }
        private void RemoveRotor()
        {
            var rotors=(JArray)document.profile["rotors"];if(rotors.Count<=1)return;string id=(string)rotors[rotorIndex]["rotorId"];
            fields.RemoveRow(rotors,rotorIndex,"rotors");((JObject)document.visual["rotorNodes"]).Remove(id);rotorIndex=Math.Min(rotorIndex,rotors.Count-1);viewport.Select(rotorIndex);Changed();viewport.GeometryChanged(true);BuildNavigation();RebuildInspector();
        }
        private void Validate()
        {
            if(document==null || busy || dataWindow!=null || validationWindow!=null)return;
            viewport.SaveView();var owner=document;string approvedSnapshot=document.Snapshot();
            validationReturnFocus=page.panel?.focusController.focusedElement as VisualElement;
            validationWindow=Box(page,"drone-data-overlay");validationWindow.AddToClassList("drone-validation-overlay");
            var card=Box(validationWindow,"drone-validation-window");
            var header=Box(card,"drone-window-header");DroneProfileFields.Label(header,"ПРОВЕРКА ПРОФИЛЯ","drone-panel-title");
            var close=DroneProfileFields.Button(header,"Закрыть",CloseValidation);
            var scroll=Scroll(card,"drone-validation-scroll");bool valid=ShowValidation(scroll);
            validationSaveError=DroneProfileFields.Label(scroll,"","drone-error");validationSaveError.style.display=DisplayStyle.None;
            var actions=Box(card,"drone-window-footer");
            DroneProfileFields.Label(actions,valid?"Ошибок нет. Профиль можно сохранить.":"Исправьте ошибки в редакторе и повторите проверку.","drone-window-message");
            var edit=DroneProfileFields.Button(actions,"Вернуться к настройкам",CloseValidation);
            var publish=DroneProfileFields.Button(actions,"Сохранить профиль",()=>{
                if(document!=owner)return;if(Save(false,approvedSnapshot))CloseValidation();
            });publish.AddToClassList("primary");publish.SetEnabled(valid);
            var buttons=new[]{close,edit,publish};validationWindow.RegisterCallback<KeyDownEvent>(evt=>{
                if(evt.keyCode!=KeyCode.Tab)return;
                var active=buttons.Where(button=>button.enabledInHierarchy).ToArray();if(active.Length==0)return;
                int index=Array.FindIndex(active,button=>button==page.panel.focusController.focusedElement);
                active[(index+(evt.shiftKey?-1:1)+active.Length)%active.Length].Focus();evt.StopPropagation();evt.PreventDefault();
            },TrickleDown.TrickleDown);
            validationWindow.schedule.Execute(()=>edit.Focus());UpdateState();
        }
        private void CloseValidation()
        {
            if(validationWindow==null)return;validationWindow.RemoveFromHierarchy();validationWindow=null;validationSaveError=null;
            if(!disposed)UpdateState();if(validationReturnFocus?.parent!=null)validationReturnFocus.Focus();validationReturnFocus=null;
        }
        private bool ShowValidation(VisualElement host)
        {
            try {
                var result=DroneProfileLibrary.Validate(document);string pilotWarning=null;
                if(result.Success)try{new QuadAllocator(result.Parameters);}catch(ArgumentException ex){pilotWarning="Профиль можно сохранить, но текущий четырёхроторный пилот его не поддерживает: "+DroneValidationText.Message(ex.Message);}
                int errorCount=errors.Count+result.Issues.Count(issue=>issue.Severity=="Error");int warningCount=result.Issues.Count(issue=>issue.Severity!="Error")+(pilotWarning==null?0:1);
                DroneProfileFields.Label(host,$"Ошибок: {errorCount} · Предупреждений: {warningCount}",errorCount>0?"drone-error":"drone-validation-success");
                foreach(var error in errors)DroneProfileFields.Label(host,DroneValidationText.Path(error.Key)+": "+error.Value,"drone-error");
                if(result.Success) {
                    var p=result.Parameters;DroneProfileFields.Label(host,$"Масса {p.Mass:0.###} кг · максимальная тяга {p.MaxTotalThrust:0.###} Н · тяга/вес {p.ThrustToWeight:0.###}","scenario-hint");
                    DroneProfileFields.Label(host,pilotWarning??"Геометрия совместима с текущим пилотом.",pilotWarning==null?"scenario-hint":"drone-warning");
                    if(result.Issues.Count==0)DroneProfileFields.Label(host,"Профиль прошёл проверку.","scenario-hint");
                }
                foreach(var issue in result.Issues)DroneProfileFields.Label(host,DroneValidationText.Path(issue.Path)+"\n"+DroneValidationText.Message(issue.Message),issue.Severity=="Error"?"drone-error":"drone-warning");
                DroneProfileFields.Label(host,"Проверяется схема и связность моделей. Оценочная тяга не учитывает все ограничения питания и не доказывает точность реального полёта. Совместимость со средой проверяется при запуске.","scenario-hint");
                return result.Success && errors.Count==0 && document.unfinishedInputs.Count==0;
            }catch(Exception ex){DroneProfileFields.Label(host,ex.Message,"drone-error");return false;}
        }
        private async void ImportModel()
        {
            if(busy)return;busy=true;UpdateState();
            try {
                var chosen=await DroneFileDialog.Pick(false,DroneProfileLibrary.Root,"Модель дрона","model.glb","glb,gltf");
                if(chosen.Failed)throw new IOException(chosen.Error);if(string.IsNullOrEmpty(chosen.Path)||disposed)return;
                if(await viewport.LoadModel(chosen.Path,true)) {document.sourceModel=chosen.Path;Changed();RebuildInspector();}
            }catch(Exception ex){Status(ex.Message,true);}finally{busy=false;if(!disposed)UpdateState();}
        }
        private async void ImportProfile()
        {
            if(busy)return;busy=true;UpdateState();
            try {var chosen=await DroneFileDialog.Pick(false,DroneProfileLibrary.Root,"Профиль дрона","profile.json","json");if(chosen.Failed)throw new IOException(chosen.Error);if(!string.IsNullOrEmpty(chosen.Path)&&!disposed)Open(DroneProfileLibrary.Import(chosen.Path),true);}
            catch(Exception ex){Status(ex.Message,true);}finally{busy=false;if(!disposed)UpdateState();}
        }
        private void Status(string text,bool error=false)
        {
            if(disposed)return;statusTimer?.Pause();message.text=text;message.EnableInClassList("error",error);
            if(!error)statusTimer=page.schedule.Execute(()=>message.text="").StartingIn(10000);
        }
        private static VisualElement Box(VisualElement host,string style){var box=new VisualElement();box.AddToClassList(style);host.Add(box);return box;}
        private static ScrollView Scroll(VisualElement host,string style)
        {
            var scroll=new ScrollView(ScrollViewMode.Vertical);scroll.AddToClassList(style);scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
            scroll.verticalScroller.AddToClassList("graphite-scroller");scroll.verticalScroller.lowButton.style.display=DisplayStyle.None;scroll.verticalScroller.highButton.style.display=DisplayStyle.None;
            host.Add(scroll);return scroll;
        }
        public void Dispose(){if(disposed)return;disposed=true;++galleryRevision;statusTimer?.Pause();validationWindow?.RemoveFromHierarchy();validationWindow=null;dataWindow?.RemoveFromHierarchy();dataWindow=null;viewport?.Dispose();galleryRenderer?.Dispose();foreach(var t in thumbnails)Object.Destroy(t);thumbnails.Clear();}
    }
}
