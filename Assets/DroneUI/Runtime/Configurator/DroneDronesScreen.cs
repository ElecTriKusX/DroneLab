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
        private readonly Dictionary<string,bool> expandedGroups=new Dictionary<string,bool>();
        private readonly Dictionary<string,Vector2> inspectorOffsets=new Dictionary<string,Vector2>();
        private readonly Dictionary<string,Button> viewButtons=new Dictionary<string,Button>(),toolButtons=new Dictionary<string,Button>();
        private bool wideInspector;
        private Button save,draftSave,cancel,validate,galleryOpen,galleryCopy,galleryDelete;
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
            galleryCount=DroneProfileFields.Label(footer,"","scenario-muted");galleryCount.AddToClassList("drone-gallery-count");
            message=DroneProfileFields.Label(footer,"","drone-status");
            galleryOpen=DroneProfileFields.Button(footer,"Открыть",()=>{if(selected!=null&&!busy)Open(selected);});
            galleryCopy=DroneProfileFields.Button(footer,"Копировать",CopySelected);
            galleryDelete=DroneProfileFields.Button(footer,"Удалить",DeleteSelected);
            validate=DroneProfileFields.Button(footer,"Проверить",Validate);
            cancel=DroneProfileFields.Button(footer,"Отменить",Cancel);
            draftSave=DroneProfileFields.Button(footer,"Сохранить черновик",()=>Save(true));
            save=DroneProfileFields.Button(footer,"Сохранить профиль",()=>Save(false));save.AddToClassList("primary");
            DroneProfileFields.Button(footer,"Назад",RequestClose);BuildGallery();
            if(!string.IsNullOrEmpty(warnings))Status(warnings,true);
        }
        private bool Dirty=>document!=null && (newProfile || document.Snapshot()!=original || preview!=null);
        private void BuildGallery()
        {
            galleryRevision++;viewport?.Dispose();viewport=null;galleryRenderer?.Dispose();galleryRenderer?.RemoveFromHierarchy();galleryRenderer=null;
            CloseDataWindow();document=null;selected=null;body.Clear();heading.text="КАТАЛОГ ДРОНОВ";state.text="";errors.Clear();input.Clear();
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
            viewport?.Dispose();CloseDataWindow();selected=value;document=value.Copy();newProfile=created;original=document.Snapshot();preview=null;errors.Clear();input.Clear();rotorIndex=0;section="Model";inspectorSection=null;inspectorOffsets.Clear();expandedGroups.Clear();
            foreach(var pair in document.unfinishedInputs){input[pair.Key]=pair.Value;errors[pair.Key]="Незавершённое значение из черновика.";}
            body.Clear();page.RemoveFromClassList("drone-gallery-page");page.AddToClassList("drone-editor-page");page.EnableInClassList("drone-wide-inspector",wideInspector);body.RemoveFromClassList("drone-gallery-body");heading.text="НАСТРОЙКА ДРОНА";
            navigation=Scroll(body,"drone-navigation");var center=Box(body,"drone-center");inspector=Box(body,"drone-inspector");
            inspector.focusable=true;var toolbar=Box(center,"drone-viewport-toolbar");viewButtons.Clear();toolButtons.Clear();
            foreach(var view in new[]{("3D","3D"),("Сверху","Top"),("Спереди","Front"),("Сбоку","Side")}) {
                string key=view.Item2;viewButtons[key]=DroneProfileFields.Button(toolbar,view.Item1,()=>{viewport.View(key);UpdateViewportControls();});
            }
            toolsToolbar=Box(center,"drone-rotor-tools");
            foreach(var tool in new[]{("Положение точки","Move"),("Направление тяги","Axis")}) {
                string key=tool.Item2;toolButtons[key]=DroneProfileFields.Button(toolsToolbar,tool.Item1,()=>{viewport.Tool=key;UpdateViewportControls();});
            }
            var snap=new Toggle("Привязка к сетке");snap.AddToClassList("drone-snap");toolsToolbar.Add(snap);
            snap.RegisterValueChangedCallback(evt=>viewport.Snap=evt.newValue);
            DroneHelp.Attach(toolsToolbar,()=>"Выберите точку ротора. Инструмент «Положение точки» перемещает её в плоскости вида или вдоль выбранной оси. «Направление тяги» меняет направление стрелки. Привязка округляет положение до 1 см. Точные компоненты доступны справа.");
            viewport=new DroneModelViewport();center.Add(viewport);viewport.SetDocument(document);
            if(DroneProfileLibrary.ModelPath(document)==null && (created || document.id.StartsWith("builtin-") && !File.Exists(Path.Combine(DroneProfileLibrary.Folder(document.id),"document.json"))))viewport.Frame();
            original=document.Snapshot();
            var views=Box(center,"drone-viewport-bottom");
            DroneProfileFields.Button(views,"Показать дрон целиком",()=>viewport.Frame());
            DroneHelp.Attach(views,()=>"Возвращает камеру к модели и подбирает масштаб, чтобы весь дрон и его точки роторов помещались в окне. Правая кнопка мыши вращает камеру, средняя перемещает её, колесо меняет масштаб.");
            DroneProfileFields.Label(views,"Вращение — ПКМ · Перемещение — СКМ · Масштаб — колесо","drone-camera-note");
            DroneProfileFields.Label(views,"X вправо · Y вверх · Z вперёд · 1 ед. = 1 м","drone-view-note");
            fields=new DroneProfileFields(Changed,RebuildInspector,OpenTable,errors,input,expandedGroups);
            viewport.Selected+=i=> {rotorIndex=i;section="Rotor";BuildNavigation();RebuildInspector();};
            viewport.Changed+=()=> {if(viewport.LastEditedField!=null)fields.ClearErrors(viewport.LastEditedField);Changed();if(section=="Rotor")RebuildInspector();};
            viewport.Status+=s=>Status(s);BuildNavigation();RebuildInspector();UpdateState();
            string model=DroneProfileLibrary.ModelPath(document);if(model!=null)LoadExisting(model);
        }
        private async void LoadExisting(string path)
        {
            busy=true;try {await viewport.LoadModel(path,false);if(!disposed && document!=null){RebuildInspector();}}
            catch(Exception ex){Status(ex.Message,true);}finally{busy=false;if(!disposed)UpdateState();}
        }
        private void BuildNavigation()
        {
            var offset=((ScrollView)navigation).scrollOffset;
            navigation.Clear();DroneProfileFields.Label(navigation,"ПРОФИЛЬ","drone-panel-title");
            foreach(var item in new[]{("Модель","Model"),("Масса и инерция","Mass"),("Роторы","Rotor"),("Характеристики винта","Performance"),("Аэродинамика","Aero"),("Батарея и питание","Power"),("Температура","Thermal"),("Модули","Modules"),("Источники и метаданные","Sources"),("Проверка","Check")}) {
                string key=item.Item2;var b=DroneProfileFields.Button(navigation,item.Item1,()=> {section=key;BuildNavigation();RebuildInspector();});b.EnableInClassList("active",section==key);
                var icon=new DroneMenuIcon(key=="Model"||key=="Rotor"?MenuIconKind.Drone:key=="Aero"?MenuIconKind.Wind:key=="Sources"?MenuIconKind.Book:key=="Check"?MenuIconKind.Laboratory:MenuIconKind.Settings);icon.AddToClassList("drone-nav-icon");b.Add(icon);
                if(key=="Rotor" && (section=="Rotor" || section=="Performance"))foreach(var pair in ((JArray)document.profile["rotors"]).Select((r,i)=>(r,i))) {
                    int index=pair.i;string rotorSection=section=="Performance"?"Performance":"Rotor";var rotor=DroneProfileFields.Button(navigation,(string)pair.r["rotorId"],()=>{rotorIndex=index;section=rotorSection;viewport.Select(index);BuildNavigation();RebuildInspector();});rotor.AddToClassList("rotor-navigation");rotor.EnableInClassList("active",rotorIndex==index);
                }
            }
            DroneProfileFields.Button(navigation,"+ Добавить ротор",AddRotor);
            if(((JArray)document.profile["rotors"]).Count>1)DroneProfileFields.Button(navigation,"Удалить выбранный",RemoveRotor);
            navigation.schedule.Execute(()=>((ScrollView)navigation).scrollOffset=offset);
        }
        private void RebuildInspector()
        {
            if(document==null || inspector==null || disposed)return;
            var previous=inspector.Q<ScrollView>();if(previous!=null && inspectorSection!=null)inspectorOffsets[inspectorSection]=previous.scrollOffset;
            var rotors=(JArray)document.profile["rotors"];rotorIndex=Mathf.Clamp(rotorIndex,0,Math.Max(0,rotors.Count-1));
            inspector.Clear();inspectorSection=section;var header=Box(inspector,"drone-inspector-heading");
            string title=section=="Model"?"МОДЕЛЬ И ПРОФИЛЬ":section=="Mass"?"МАССА И ИНЕРЦИЯ":section=="Rotor"?"РОТОР "+(rotors.Count>0?(string)rotors[rotorIndex]["rotorId"]:""):section=="Performance"?"ХАРАКТЕРИСТИКИ ВИНТА":section=="Aero"?"АЭРОДИНАМИКА":section=="Power"?"БАТАРЕЯ И ПИТАНИЕ":section=="Thermal"?"ТЕМПЕРАТУРА":section=="Modules"?"МОДУЛИ":section=="Sources"?"ИСТОЧНИКИ":"ПРОВЕРКА";
            DroneProfileFields.Label(header,title,"drone-panel-title");
            var width=DroneProfileFields.Button(header,wideInspector?"Обычная ширина":"Больше места",()=>{wideInspector=!wideInspector;page.EnableInClassList("drone-wide-inspector",wideInspector);RebuildInspector();});width.AddToClassList("drone-width-button");
            var scroll=Scroll(inspector,"drone-inspector-scroll");
            var rotor=rotors.Count>0?(JObject)rotors[rotorIndex]:null;string rp="rotors["+rotorIndex+"]";
            void Group(string key,string def){
                if(document.profile[key]==null)document.profile[key]=DroneParameterSchema.Default(DroneParameterSchema.ObjectRule("DroneProfile")["properties"][key],key);
                fields.Object(scroll,(JObject)document.profile[key],def,key);}
            switch(section) {
                case "Model":ModelInspector(scroll);break;
                case "Mass":Group("massProperties","MassProperties");break;
                case "Rotor":if(rotor!=null){DroneProfileFields.Button(scroll,"Открыть характеристики винта",()=>OpenPerformance(rotor,rp));fields.Object(scroll,rotor,"RotorProfile",rp);
                    DroneProfileFields.Button(scroll,"Применить двигатель и винт ко всем",()=> {foreach(var r in rotors.OfType<JObject>())if(r!=rotor)foreach(string key in new[]{"motor","propeller","performance","advancedAerodynamics","operatingEnvelope"}){if(rotor[key]!=null)r[key]=rotor[key].DeepClone();else r.Remove(key);fields.ClearErrors("rotors["+rotors.IndexOf(r)+"]."+key);}Changed();RebuildInspector();});}break;
                case "Performance":if(rotor!=null){DroneProfileFields.Button(scroll,"Открыть таблицу и графики",()=>OpenPerformance(rotor,rp));fields.Object(scroll,(JObject)rotor["performance"],"RotorPerformanceProfile",rp+".performance");}break;
                case "Aero":Group("bodyAerodynamics","BodyAerodynamicsProfile");
                    if(document.profile["groundEffect"]!=null)fields.Field(scroll,"groundEffect",DroneParameterSchema.ObjectRule("DroneProfile")["properties"]["groundEffect"],document.profile["groundEffect"],"groundEffect",t=>document.profile["groundEffect"]=t);
                    else DroneProfileFields.Button(scroll,"+ Параметры экрана",()=>{document.profile["groundEffect"]=DroneParameterSchema.Default(DroneParameterSchema.ObjectRule("DroneProfile")["properties"]["groundEffect"]);Changed();RebuildInspector();});break;
                case "Power":Group("powerSystem","PowerSystemProfile");break;
                case "Thermal":
                    DroneProfileFields.Label(scroll,"Тепловой расчёт включается в питании. Здесь редактируются данные компонентов; температуры хранятся в Кельвинах.","scenario-hint");
                    fields.Field(scroll,"thermalEnabled",DroneParameterSchema.ObjectRule("PowerSystemProfile")["properties"]["thermalEnabled"],document.profile["powerSystem"]["thermalEnabled"]??new JValue(false),"powerSystem.thermalEnabled",v=>document.profile["powerSystem"]["thermalEnabled"]=v);
                    foreach(var r in rotors.OfType<JObject>()) {
                        DroneProfileFields.Label(scroll,(string)r["rotorId"],"drone-panel-title");
                        if(r["motor"]?["electrical"] is JObject electric)fields.Object(scroll,electric,"MotorElectricalProfile","rotors["+rotors.IndexOf(r)+"].motor.electrical");
                        else DroneProfileFields.Label(scroll,"Добавьте электрические параметры на странице ротора.","scenario-hint");
                    }break;
                case "Modules":Group("physicsConfiguration","PhysicsConfiguration");break;
                case "Sources":
                    if(document.profile["parameterProvenance"] is JArray provenance)fields.Field(scroll,"parameterProvenance",DroneParameterSchema.ObjectRule("DroneProfile")["properties"]["parameterProvenance"],provenance,"parameterProvenance",v=>document.profile["parameterProvenance"]=v);
                    else DroneProfileFields.Button(scroll,"+ Источники параметров",()=>{document.profile["parameterProvenance"]=new JArray();Changed();RebuildInspector();});
                    if(document.profile["derived"] is JObject derived)fields.Object(scroll,derived,"DerivedProfile","derived");
                    else DroneProfileFields.Button(scroll,"+ Производные метаданные",()=>{document.profile["derived"]=new JObject();Changed();RebuildInspector();});break;
                case "Check":ShowValidation(scroll);break;
            }
            if(inspectorOffsets.TryGetValue(section,out var savedOffset))scroll.schedule.Execute(()=>scroll.scrollOffset=savedOffset);
            DroneProfileFields.ReadOnly(scroll);UpdateViewportControls();UpdateState();
        }
        private void ModelInspector(VisualElement host)
        {
            fields.Object(host,(JObject)document.profile["metadata"],"Metadata","metadata");
            var category=new TextField("Категория");category.SetValueWithoutNotify(document.Category);category.AddToClassList("environment-field");DroneTextInput.Configure(category);
            var categoryRow=DroneProfileFields.Row(host);categoryRow.AddToClassList("drone-parameter-row");categoryRow.Add(category);
            category.RegisterValueChangedCallback(evt=>{if(evt.target!=category)return;document.category=evt.newValue;Changed();});
            DroneHelp.Attach(categoryRow,()=>"Метка для группировки дронов в каталоге. Например: Учебные, Исследовательские, FPV или Грузовые. Можно указать свою категорию. Пустое значение означает «Без категории». Метка сохраняется в пакете профиля и не влияет на расчёт физики.");
            fields.Field(host,"schemaVersion",DroneParameterSchema.ObjectRule("DroneProfile")["properties"]["schemaVersion"],document.profile["schemaVersion"],"schemaVersion",v=>document.profile["schemaVersion"]=v);
            DroneProfileFields.Button(host,"Импорт GLB / glTF",ImportModel);
            DroneProfileFields.Label(host,"GLB 2.0 с встроенными текстурами — предпочтительный формат. glTF поддерживается вместе с его локальными текстурами и .bin. FBX/OBJ предварительно экспортируйте в GLB.","scenario-hint");
            fields.Object(host,(JObject)document.profile["coordinateSystem"],"CoordinateSystem","coordinateSystem");
            var rotationRule=new JObject{["type"]="array",["minItems"]=3,["maxItems"]=3,["items"]=new JObject{["type"]="number"}};
            fields.Field(host,"rotationEulerDeg",rotationRule,document.visual["rotationEulerDeg"],"visual.rotationEulerDeg",v=>{document.visual["rotationEulerDeg"]=v;viewport.ApplyTransform();});
            var center=new Toggle("Центрировать модель") {value=(bool?)document.visual["centerModel"]??true};host.Add(center);center.RegisterValueChangedCallback(e=>{document.visual["centerModel"]=e.newValue;viewport.ApplyTransform();Changed();});
            DroneHelp.Attach(center,()=>"Переносит центр визуальных границ модели в начало локальных координат. Центр масс задаётся отдельно. По умолчанию включено. Варианты: включено / выключено.");
            DroneProfileFields.Button(host,"Габариты по модели",()=>{var bounds=viewport.ModelBounds();document.profile["massProperties"]["dimensionsM"]=DroneModelViewport.Array(bounds.size);fields.ClearErrors("massProperties.dimensionsM");Changed();Status("Физические габариты обновлены по модели. Проверьте масштаб.");});
            DroneProfileFields.Button(host,"Сделать превью для галереи",()=>{viewport.SaveView();preview=viewport.Capture();Changed();Status("Превью подготовлено. Сохраните профиль или черновик.");});
            if(viewport.ModelRoot==null)DroneProfileFields.Label(host,"Пока показана схематическая геометрия из профиля.","scenario-hint");
        }
        private void UpdateViewportControls()
        {
            if(viewport==null)return;viewport.EditingEnabled=section=="Rotor";
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
            viewport?.ApplyTransform();viewport?.GeometryChanged();UpdateViewportControls();UpdateState();
        }
        private void UpdateState()
        {
            bool editing=document!=null;state.text=!editing?"":Dirty?"Изменения не сохранены":document.draft?"Черновик":"Профиль сохранён";
            identity.text=document?.Name??"";identity.style.display=editing?DisplayStyle.Flex:DisplayStyle.None;
            galleryCount.style.display=editing?DisplayStyle.None:DisplayStyle.Flex;
            validate.style.display=editing?DisplayStyle.Flex:DisplayStyle.None;
            foreach(var button in new[]{galleryOpen,galleryCopy,galleryDelete}) {button.style.display=editing?DisplayStyle.None:DisplayStyle.Flex;button.SetEnabled(!busy&&selected!=null);}
            galleryCreate?.SetEnabled(!busy);galleryImport?.SetEnabled(!busy);
            save.style.display=editing && (Dirty || document.draft)?DisplayStyle.Flex:DisplayStyle.None;
            draftSave.style.display=editing && Dirty?DisplayStyle.Flex:DisplayStyle.None;cancel.style.display=editing && Dirty?DisplayStyle.Flex:DisplayStyle.None;
            save.SetEnabled(!busy && dataWindow==null && errors.Count==0);draftSave.SetEnabled(!busy && dataWindow==null);cancel.SetEnabled(!busy && dataWindow==null);validate.SetEnabled(!busy && dataWindow==null);
        }
        private bool Save(bool asDraft)
        {
            if(document==null || busy)return false;
            if(!asDraft && errors.Count>0){Status("Исправьте незавершённые значения или сохраните черновик.",true);return false;}
            try {
                viewport.SaveView();DroneProfileLibrary.Save(document,asDraft);
                if(preview!=null)File.WriteAllBytes(Path.Combine(DroneProfileLibrary.Folder(document.id),"preview.png"),preview);
                else if(!File.Exists(Path.Combine(DroneProfileLibrary.Folder(document.id),"preview.png")))File.WriteAllBytes(Path.Combine(DroneProfileLibrary.Folder(document.id),"preview.png"),viewport.Capture());
                if(!asDraft)PlayerPrefs.SetString("DroneLab.SelectedDrone",document.id);
                preview=null;newProfile=false;original=document.Snapshot();var index=documents.FindIndex(x=>x.id==document.id);
                selected=document.Copy();gallerySelection=selected.id;if(index>=0)documents[index]=selected;else documents.Add(selected);
                Status(asDraft?"Черновик сохранён.":"Профиль сохранён и доступен для выбора в симуляции.");UpdateState();return true;
            }catch(Exception ex){Status(ex.Message,true);section="Check";RebuildInspector();return false;}
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
        private void Validate(){if(document==null)return;section="Check";BuildNavigation();RebuildInspector();}
        private void ShowValidation(VisualElement host)
        {
            foreach(var error in errors)DroneProfileFields.Label(host,DroneValidationText.Path(error.Key)+": "+error.Value,"drone-error");
            try {
                var result=DroneProfileLibrary.Validate(document);
                if(result.Success) {
                    var p=result.Parameters;DroneProfileFields.Label(host,$"Масса {p.Mass:0.###} кг · максимальная тяга {p.MaxTotalThrust:0.###} Н · тяга/вес {p.ThrustToWeight:0.###}","scenario-hint");
                    try{new QuadAllocator(p);DroneProfileFields.Label(host,"Геометрия совместима с текущим пилотом.","scenario-hint");}catch(ArgumentException ex){DroneProfileFields.Label(host,"Профиль можно сохранить, но текущий четырёхроторный пилот его не поддерживает: "+DroneValidationText.Message(ex.Message),"drone-warning");}
                    if(result.Issues.Count==0)DroneProfileFields.Label(host,"Профиль прошёл проверку.","scenario-hint");
                }
                foreach(var issue in result.Issues)DroneProfileFields.Label(host,DroneValidationText.Path(issue.Path)+"\n"+DroneValidationText.Message(issue.Message),issue.Severity=="Error"?"drone-error":"drone-warning");
                DroneProfileFields.Label(host,"Проверяется схема и связность моделей. Оценочная тяга не учитывает все ограничения питания и не доказывает точность реального полёта. Совместимость со средой проверяется при запуске.","scenario-hint");
            }catch(Exception ex){DroneProfileFields.Label(host,ex.Message,"drone-error");}
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
        public void Dispose(){if(disposed)return;disposed=true;++galleryRevision;statusTimer?.Pause();dataWindow?.RemoveFromHierarchy();dataWindow=null;viewport?.Dispose();galleryRenderer?.Dispose();foreach(var t in thumbnails)Object.Destroy(t);thumbnails.Clear();}
    }
}
