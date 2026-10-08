using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using DroneLab.Physics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>Independent map gallery and environment library. No map/profile pairing is stored here.</summary>
    internal sealed class DroneScenariosScreen
    {
        private readonly VisualElement page, content, footer;
        private readonly Action closed;
        private readonly DroneScenarioCatalog catalog;
        private readonly List<DroneEnvironmentDocument> profiles;
        private readonly Button mapsTab, profilesTab;
        private readonly Label footnote, transferStatus;
        private readonly Dictionary<string,List<VisualElement>> controls = new Dictionary<string,List<VisualElement>>();
        private readonly Dictionary<string,string> inputErrors = new Dictionary<string,string>();
        private bool savedStatus, fileDialogBusy;
        private readonly Dictionary<string,string> inputText = new Dictionary<string,string>();
        private DroneDropdown weatherChoice;
        private bool HasVisualOverrides => draft != null && draft.visual.Properties().Any(p => p.Name != "timeOfDay" && p.Name != "simulateTime" && p.Name != "weatherPresetId");
        private DroneEnvironmentDocument selected, draft;
        private string original, filter = "";
        private VisualElement library, editor, prompt;
        private Label message;
        public VisualElement Element => page;
        private bool Dirty => draft != null && (JsonConvert.SerializeObject(draft) != original || inputErrors.Count > 0);

        public DroneScenariosScreen(VisualElement host, DroneScenarioCatalog catalog, Action closed)
        {
            this.catalog = catalog; this.closed = closed;
            profiles = DroneEnvironmentProfiles.LoadAll(out var warnings);
            page = Box(host, "scenario-page");
            var header = Box(page, "scenario-header");
            Label(header, "ДРОНЛАБ", "scenario-brand"); Box(header, "scenario-header-divider");
            Label(header, "СЦЕНАРИИ И ОКРУЖЕНИЕ", "scenario-title");
            var tabs = Box(page, "scenario-tabs");
            mapsTab = Button(tabs, "Карты", () => SwitchTab(false), "scenario-tab");
            profilesTab = Button(tabs, "Профили среды", () => SwitchTab(true), "scenario-tab");
            content = Box(page, "scenario-content");
            footer = Box(page, "scenario-footer");
            footnote = Label(footer, "", "scenario-muted");
            transferStatus = Label(footer, "", "scenario-transfer-status");
            Button(footer, "НАЗАД", RequestClose);
            SwitchTab(false);
            if (!string.IsNullOrEmpty(warnings)) Debug.LogWarning("DroneLab profiles: " + warnings);
        }
        public void RequestClose()
        {
            if (prompt != null) { prompt.RemoveFromHierarchy(); prompt = null; return; }
            Guard(() => { page.RemoveFromHierarchy(); closed(); });
        }
        private void SwitchTab(bool profileTab)
        {
            Guard(() => {
                content.Clear();
                mapsTab.EnableInClassList("active", !profileTab); profilesTab.EnableInClassList("active", profileTab);
                if (profileTab) BuildProfiles(); else BuildMaps();
            });
        }
        private void Guard(Action action)
        {
            if (fileDialogBusy) return;
            if (!Dirty) { action(); return; }
            Ask("Сохранить изменения профиля?", "Несохранённые изменения будут потеряны.",
                ("Назад", () => { }), ("Не сохранять", () => { draft = null; original = null; action(); }),
                ("Сохранить", () => { if (Save()) action(); }));
        }
        private void Ask(string title, string text, params (string caption, Action action)[] actions)
        {
            prompt?.RemoveFromHierarchy(); prompt = Box(page, "scenario-prompt");
            var card = Box(prompt, "scenario-prompt-card"); Label(card, title, "scenario-panel-title"); Label(card, text, "scenario-hint");
            var row = Box(card, "scenario-actions");
            foreach (var item in actions) Button(row, item.caption, () => { prompt.RemoveFromHierarchy(); prompt = null; item.action(); });
        }
        private void BuildMaps()
        {
            draft = null; inputErrors.Clear(); transferStatus.text = ""; Label(content, "Каталог доступных карт", "scenario-hint");
            var toolbar = Box(content, "scenario-toolbar");
            var search = new TextField { value = "" }; search.AddToClassList("scenario-search"); search.tooltip = "Поиск карты";
            search.label = "Поиск карты"; ReadOnly(search.labelElement); toolbar.Add(search);
            var maps = catalog?.maps.Where(m => m != null).ToList() ?? new List<DroneMapEntry>();
            var types = new List<string> { "Все местности" }; types.AddRange(maps.Select(m => m.terrainType).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
            var kind = Choice(toolbar, "", types, types, types[0], _ => { }); kind.AddToClassList("map-filter");
            var scroll = Scroll(content); scroll.AddToClassList("map-scroll");
            var grid = Box(scroll, "map-grid");
            void Fill()
            {
                grid.Clear(); int count = 0;
                foreach (var map in maps) {
                    if (!Contains(map.title, search.value) || (kind.index > 0 && map.terrainType != kind.value)) continue;
                    var card = Box(grid, "map-card");
                    card.EnableInClassList("map-row-end", count % 3 == 2);
                    var preview = Box(card, "map-preview");
                    preview.RegisterCallback<GeometryChangedEvent>(evt => { if (Math.Abs(preview.resolvedStyle.height - evt.newRect.width * 9f / 16f) > 1) preview.style.height = evt.newRect.width * 9f / 16f; });
                    if (map.screenshot != null) preview.style.backgroundImage = new StyleBackground(map.screenshot);
                    else { var icon = new DroneMenuIcon(MenuIconKind.Landscape); icon.AddToClassList("map-placeholder"); preview.Add(icon); Label(preview, "Превью не добавлено", "map-placeholder-caption"); }
                    Label(preview, map.title, "map-name");
                    string size = map.sizeM.x > 0 && map.sizeM.y > 0 ? $"{map.sizeM.x / 1000f:0.##} × {map.sizeM.y / 1000f:0.##} км" : "Размер не указан";
                    Label(card, (string.IsNullOrWhiteSpace(map.terrainType) ? "Тип не указан" : map.terrainType) + " · " + size, "map-meta");
                    count++;
                }
                if (count == 0) Label(grid, "Карты не найдены", "scenario-hint");
                footnote.text = $"Карт в каталоге: {maps.Count}";
            }
            search.RegisterValueChangedCallback(_ => Fill()); kind.RegisterValueChangedCallback(_ => Fill()); Fill();
        }
        private void BuildProfiles()
        {
            Label(content, "Создавайте условия для будущих испытаний", "scenario-hint");
            var columns = Box(content, "profile-columns");
            library = Box(columns, "profile-library");
            var top = Box(library, "scenario-toolbar"); Label(top, "ПРОФИЛИ", "scenario-panel-title");
            Button(top, "+ Создать профиль", () => Guard(() => Select(DroneEnvironmentProfiles.New())));
            var search = new TextField("Поиск профиля") { value = filter }; search.AddToClassList("scenario-search"); ReadOnly(search.labelElement); library.Add(search);
            var list = Scroll(library); list.name = "profile-list";
            search.RegisterValueChangedCallback(evt => { filter = evt.newValue; FillList(); });
            var exchange = Box(library, "scenario-actions");
            Button(exchange, "Импорт", () => Guard(Import)); Button(exchange, "Экспорт", Export);
            editor = Box(columns, "profile-editor");
            if (selected == null || !profiles.Any(p => p.id == selected.id)) selected = profiles.FirstOrDefault();
            if (selected == null) selected = DroneEnvironmentProfiles.New();
            Select(selected);
            footnote.text = "Профилей: " + profiles.Count;
        }
        private void FillList()
        {
            if (library == null) return;
            var list = library.Q<ScrollView>("profile-list"); list.Clear();
            foreach (var d in profiles.Where(p => Contains(p.name, filter))) {
                var row = Button(list, "", () => Guard(() => Select(d)), "profile-list-item");
                row.EnableInClassList("selected", selected?.id == d.id);
                string mode = (string)d.environment["windMode"];
                var icon = new DroneMenuIcon(mode == "None" ? MenuIconKind.Sun : MenuIconKind.Wind); icon.AddToClassList("profile-icon"); row.Add(icon);
                var text = Box(row, "profile-list-text"); Label(text, d.name, "profile-list-name");
                var e = DroneEnvironmentProfiles.Effective(d.environment); var v = DVector3.From(e["windVelocityWorldMps"].ToObject<double[]>());
                double hours = (double?)d.visual["timeOfDay"] ?? 14;
                Label(text, $"{v.Length:0.##} м/с · {(double)e["temperatureK"] - 273.15:0.#} °C · {(int)hours:00}:{(int)((hours % 1) * 60):00}", "scenario-muted");
            }
        }
        private void Select(DroneEnvironmentDocument source)
        {
            selected = source; draft = source.Copy();
            // Explicit standard defaults are editable; unused optional parameters remain harmless.
            draft.environment = DroneEnvironmentProfiles.Effective(draft.environment);
            original = JsonConvert.SerializeObject(draft); savedStatus = false; inputErrors.Clear(); inputText.Clear();
            FillList(); BuildEditor();
        }
        private void BuildEditor()
        {
            editor.Clear(); controls.Clear(); weatherChoice = null;
            var head = Box(editor, "scenario-toolbar"); Label(head, "РЕДАКТОР ПРОФИЛЯ", "scenario-panel-title");
            Label(head, "Профили доступны для всех карт", "scenario-muted editor-caption");
            if (profiles.Any(p => p.id == draft.id)) Button(head, "Удалить", Delete);
            var name = new TextField("Название") { value = draft.name }; name.AddToClassList("profile-name-field"); ReadOnly(name.labelElement); editor.Add(name);
            Register(name, "name");
            name.RegisterValueChangedCallback(evt => { draft.name = evt.newValue; Changed(); });
            var scroll = Scroll(editor); scroll.AddToClassList("profile-form-scroll");
            var columns = Box(scroll, "profile-form-columns");
            var left = Box(columns, "profile-form-panel"); var right = Box(columns, "profile-form-panel");
            Label(left, "ПОГОДА И ВРЕМЯ", "scenario-panel-title");
            WeatherChoice(left);
            TimeOfDay(left);
            Bool(left, draft.visual, "simulateTime", "Ход времени", false);
            Register(Choice(left, "Осадки *", new[] { "Нет", "Дождь", "Снег", "Град" }, new[] { "None", "Rain", "Snow", "Hail" }, (string)draft.environment["weather"]["precipitation"], value => {
                draft.environment["weather"]["precipitation"] = value;
                if (value == "None") draft.environment["weather"]["intensityMmPerHour"] = 0;
                else if ((double)draft.environment["weather"]["intensityMmPerHour"] == 0) draft.environment["weather"]["intensityMmPerHour"] = 5;
                Changed(); RebuildEditor();
            }), "weather.precipitation");
            Number(left, draft.environment, "weather.intensityMmPerHour", "Осадки, мм/ч *", 0);
            var registered = catalog?.Weather((string)draft.visual["weatherPresetId"]);
            Label(right, "ВЕТЕР И ВОЗДУХ", "scenario-panel-title");
            var modes = new[] { "None", "Constant", "Gust", "Turbulence", "DrydenFrozen", "CustomField" };
            Register(Choice(right, "Модель ветра", new[] { "Штиль", "Постоянный", "Порывы", "Турбулентность", "Модель Драйдена", "Внешнее поле / Enviro" }, modes,
                (string)draft.environment["windMode"], mode => {
                    draft.environment["windMode"] = mode;
                    if (mode == "Gust") draft.environment["gustEnabled"] = true;
                    if (mode == "None" || mode == "CustomField") draft.environment["gustEnabled"] = false;
                    Changed(); RebuildEditor();
                }), "windMode");
            Wind(right);
            Register(Choice(right, "Модель воздуха", new[] { "Постоянная плотность", "Стандартная атмосфера" }, new[] { "Constant", "StandardAtmosphere" },
                (string)draft.environment["airDensityMode"], mode => { draft.environment["airDensityMode"] = mode; Changed(); RebuildEditor(); }), "airDensityMode");
            Temperature(right);
            var advanced = new Foldout { text = "Расширенные параметры", value = false }; advanced.AddToClassList("environment-advanced"); scroll.Add(advanced);
            var atmosphere = Section(advanced, "АТМОСФЕРА И ГРАВИТАЦИЯ");
            Number(atmosphere, draft.environment, "airDensityKgM3", "Плотность, кг/м³", 1.225);
            Number(atmosphere, draft.environment, "pressurePa", "Давление, Па", 101325);
            Number(atmosphere, draft.environment, "altitudeM", "Опорная высота, м", 0);
            Number(atmosphere, draft.environment, "gravityMps2", "Гравитация, м/с²", 9.81);
            var wind = Section(advanced, "ПОРЫВЫ И ТУРБУЛЕНТНОСТЬ");
            Vector(wind, "windVelocityWorldMps", "Ветер X / Y / Z, м/с", new[] { 0.0, 0, 0 });
            Bool(wind, draft.environment, "gustEnabled", "Периодические порывы", false);
            Number(wind, draft.environment, "gustIntensityMps", "Амплитуда колебаний, м/с", 0);
            Number(wind, draft.environment, "gustTimeScaleS", "Период / масштаб времени, с", 2);
            Number(wind, draft.environment, "turbulenceSeed", "Начальное число генератора", 48271, true);
            var dryden = new Foldout { text = "Спектральная турбулентность — модель Драйдена", value = (string)draft.environment["windMode"] == "DrydenFrozen" }; advanced.Add(dryden);
            Vector(dryden, "dryden.sigmaUvwMps", "Колебания u / v / w, м/с", new[] { .4, .4, .25 });
            Vector(dryden, "dryden.lengthScaleUvwM", "Масштабы u / v / w, м", new[] { 20.0, 20, 10 });
            Vector(dryden, "dryden.advectionDirectionWorld", "Ось переноса X / Y / Z", new[] { 1.0, 0, 0 });
            Number(dryden, draft.environment, "dryden.advectionSpeedMps", "Скорость переноса, м/с", 5);
            Number(dryden, draft.environment, "dryden.modesPerComponent", "Гармоник на компоненту", 32, true);
            Number(dryden, draft.environment, "dryden.minDimensionlessWaveNumber", "Минимальное kL", .02);
            Number(dryden, draft.environment, "dryden.maxDimensionlessWaveNumber", "Максимальное kL", 20);
            var visuals = Section(advanced, "ВИЗУАЛЬНОЕ ОКРУЖЕНИЕ");
            VisualNumber(visuals, "windTurbulence", "Турбулентность растительности", registered?.preset?.environmentOverride?.windTurbulence ?? .25f);
            VisualNumber(visuals, "wetness", "Влажность поверхности", registered?.preset?.environmentOverride?.wetnessTarget ?? 0);
            VisualNumber(visuals, "snow", "Снежный покров", registered?.preset?.environmentOverride?.snowTarget ?? 0);
            VisualNumber(visuals, "cloudCoverage", "Облачность", registered?.preset?.cloudsOverride?.coverage ?? 0);
            VisualNumber(visuals, "cloudDensity", "Плотность облаков", registered?.preset?.cloudsOverride?.density ?? 1);
#if ENVIRO_HDRP
            VisualNumber(visuals, "fogDistance", "Дальность ослабления тумана, м", registered?.preset?.fogOverride?.fogAttenuationDistance ?? 400);
            VisualNumber(visuals, "fogBaseHeight", "Нижняя граница тумана, м", registered?.preset?.fogOverride?.baseHeight ?? 0);
            VisualNumber(visuals, "fogMaxHeight", "Верхняя граница тумана, м", registered?.preset?.fogOverride?.maxHeight ?? 250);
#endif
            var actions = Box(editor, "profile-editor-actions"); message = Label(actions, "", "profile-message");
            Button(actions, "Сохранить профиль", () => Save(), "scenario-button primary");
            Changed();
        }
        private void RebuildEditor() => editor.schedule.Execute(BuildEditor);
        private void WeatherChoice(VisualElement parent)
        {
            var titles = new List<string> { "Из сцены" }; var ids = new List<string> { "" };
            if (catalog != null) foreach (var weather in catalog.WeatherOptions()) { titles.Add(DroneScenarioCatalog.WeatherTitle(weather.title)); ids.Add(weather.id); }
            string id = (string)draft.visual["weatherPresetId"];
            if (!ids.Contains(id)) { titles.Add(catalog?.Weather(id)?.preset != null ? "Сохранённая основа" : "Недоступная основа"); ids.Add(id); }
            void SetBase(string value)
            {
                draft.visual["weatherPresetId"] = value;
                foreach (var key in inputErrors.Keys.Where(k => k.StartsWith("visual.") && k != "visual.timeOfDay").ToList()) { inputErrors.Remove(key); inputText.Remove(key); }
                foreach (var property in draft.visual.Properties().Where(p => p.Name != "timeOfDay" && p.Name != "simulateTime" && p.Name != "weatherPresetId").ToList()) property.Remove();
                Changed(); RebuildEditor();
            }
            weatherChoice = Choice(parent, "Погода *", titles, ids, id, SetBase);
            weatherChoice.Reselected += () => { if (HasVisualOverrides) SetBase((string)draft.visual["weatherPresetId"]); };
            Register(weatherChoice,"visual.weatherPresetId"); RefreshWeatherCaption();
        }
        private void RefreshWeatherCaption()
        {
            if (weatherChoice == null) return;
            weatherChoice.SetCaption(HasVisualOverrides ? "Кастомный" : weatherChoice.value);
        }
        private void TimeOfDay(VisualElement parent)
        {
            double hours = (double)draft.visual["timeOfDay"];
            string Caption(double time) => $"{(int)time:00}:{(int)((time % 1) * 60 + 1e-7):00}";
            var field = new TextField("Время суток *") { value = inputErrors.ContainsKey("visual.timeOfDay") && inputText.TryGetValue("visual.timeOfDay",out var typedTime) ? typedTime : Caption(hours), isDelayed = true };
            Field(parent, field); Register(field,"visual.timeOfDay"); DefaultStyle(field, hours, 14);
            field.RegisterCallback<InputEvent>(evt => {
                inputText["visual.timeOfDay"] = evt.newData; var parts = evt.newData.Trim().Split(':');
                bool valid = parts.Length == 2 && int.TryParse(parts[0],out int h) && int.TryParse(parts[1],out int m) && h >= 0 && h <= 23 && m >= 0 && m <= 59;
                if (valid) inputErrors.Remove("visual.timeOfDay"); else inputErrors["visual.timeOfDay"] = "Введите время от 00:00 до 23:59 в формате ЧЧ:ММ.";
                Changed();
            });
            field.RegisterValueChangedCallback(evt => {
                inputText["visual.timeOfDay"] = evt.newValue;
                var parts = evt.newValue.Trim().Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int hour) || !int.TryParse(parts[1], out int minute) || hour < 0 || hour > 23 || minute < 0 || minute > 59) {
                    inputErrors["visual.timeOfDay"] = "Введите время от 00:00 до 23:59 в формате ЧЧ:ММ."; Changed(); return;
                }
                inputErrors.Remove("visual.timeOfDay"); inputText.Remove("visual.timeOfDay"); hours = hour + minute / 60.0; draft.visual["timeOfDay"] = hours;
                field.SetValueWithoutNotify(Caption(hours)); DefaultStyle(field, hours, 14); Changed();
            });
        }
        private void Wind(VisualElement parent)
        {
            bool external = (string)draft.environment["windMode"] == "CustomField";
            var v = DVector3.From(draft.environment["windVelocityWorldMps"].ToObject<double[]>());
            double speed = Math.Sqrt(v.X * v.X + v.Z * v.Z);
            double bearing = speed < 1e-9 ? 0 : (Math.Atan2(-v.X, -v.Z) * 180 / Math.PI + 360) % 360;
            var speedRow = Box(parent, "wind-speed-row");
            var slider = new Slider("Скорость, м/с", 0, 100) { value = (float)speed }; slider.AddToClassList("environment-field"); ReadOnly(slider.labelElement); speedRow.Add(slider);
            slider.name = "mean-wind-slider";
            var number = new DroneDoubleField { value = speed, isDelayed = true, name = "mean-wind-speed" }; number.AddToClassList("wind-speed-number"); speedRow.Add(number); Register(slider,"meanWindSpeed"); Register(number,"meanWindSpeed", false); WatchInput(number,"meanWindSpeed", false);
            var direction = new DroneDoubleField("Направление ветра, °") { value = bearing, isDelayed = true, name = "mean-wind-bearing" }; direction.AddToClassList("environment-field"); ReadOnly(direction.labelElement);
            var row = Box(parent, "wind-direction-row"); row.Add(direction); Register(direction,"meanWindBearing"); WatchInput(direction,"meanWindBearing",false);
            var compass = new DroneWindCompass { Bearing = (float)bearing }; row.Add(compass);
            void UpdateWind(double s, double degrees) {
                foreach (string key in new[] { "meanWindSpeed", "meanWindBearing" }) if (inputText.TryGetValue(key,out var typed) &&
                    (!double.TryParse(typed.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double parsed) || !DroneEnvironmentFields.Finite(parsed))) {
                    inputErrors[key] = "Введите конечное число."; Changed(); return;
                }
                if (!DroneEnvironmentFields.Finite(s) || s < 0 || s > 100 || !DroneEnvironmentFields.Finite(degrees) || degrees < 0 || degrees > 360) {
                    if (!DroneEnvironmentFields.Finite(s) || s < 0 || s > 100) inputErrors["meanWindSpeed"] = "Скорость: 0–100 м/с.";
                    if (!DroneEnvironmentFields.Finite(degrees) || degrees < 0 || degrees > 360) inputErrors["meanWindBearing"] = "Направление: 0–360°.";
                    Changed(); return;
                }
                inputErrors.Remove("meanWindSpeed"); inputErrors.Remove("meanWindBearing"); inputText.Remove("meanWindSpeed"); inputText.Remove("meanWindBearing");
                speed = s; bearing = degrees;
                double rad = degrees * Math.PI / 180;
                double vertical = (double)draft.environment["windVelocityWorldMps"][1];
                draft.environment["windVelocityWorldMps"] = new JArray(-s * Math.Sin(rad), vertical, -s * Math.Cos(rad));
                compass.Bearing = (float)degrees; compass.MarkDirtyRepaint(); SyncWindVector(); Changed();
            }
            slider.RegisterValueChangedCallback(evt => { number.SetValueWithoutNotify(evt.newValue); UpdateWind(evt.newValue, direction.value); });
            number.RegisterValueChangedCallback(evt => { slider.SetValueWithoutNotify((float)evt.newValue); UpdateWind(evt.newValue, direction.value); });
            direction.RegisterValueChangedCallback(evt => UpdateWind(number.value, evt.newValue));
            speedRow.SetEnabled(!external); row.SetEnabled(!external);
            if (external) Label(parent, "Ветер берётся из погодного пресета Enviro / провайдера сцены", "scenario-hint small");
        }
        private void Temperature(VisualElement parent)
        {
            var field = new DroneDoubleField("Температура, °C") { value = (double)draft.environment["temperatureK"] - 273.15, isDelayed = true };
            field.AddToClassList("environment-field"); ReadOnly(field.labelElement); parent.Add(field); Register(field,"temperatureK"); WatchInput(field,"temperatureK",false);
            DefaultStyle(field, field.value, 15);
            field.RegisterValueChangedCallback(evt => { if (!AcceptNumber(evt.newValue,"temperatureK")) return; draft.environment["temperatureK"] = evt.newValue + 273.15; DefaultStyle(field, evt.newValue, 15); Changed(); });
        }
        private void Number(VisualElement parent, JObject source, string path, string title, double defaultValue, bool integer = false)
        {
            double value = (double?)source.SelectToken(path) ?? defaultValue;
            if (integer) {
                var field = new DroneIntegerField(title) { value = (int)value, isDelayed = true }; Field(parent, field); Register(field,path); WatchInput(field,path,integer); DefaultStyle(field, value, defaultValue);
                field.RegisterValueChangedCallback(evt => { if (!AcceptNumber(evt.newValue,path)) return; Set(source, path, evt.newValue); DefaultStyle(field, evt.newValue, defaultValue); Changed(); });
            } else {
                var field = new DroneDoubleField(title) { value = value, isDelayed = true }; Field(parent, field); Register(field,path); WatchInput(field,path,integer); DefaultStyle(field, value, defaultValue);
                field.RegisterValueChangedCallback(evt => { if (!AcceptNumber(evt.newValue,path)) return; Set(source, path, evt.newValue); DefaultStyle(field, evt.newValue, defaultValue); Changed(); });
            }
        }
        private void VisualNumber(VisualElement parent, string key, string title, double inherited)
        {
            var field = new DroneDoubleField(title + " *") { value = (double?)draft.visual[key] ?? inherited, isDelayed = true }; Field(parent, field);
            Register(field,"visual." + key, true, () => DroneEnvironmentFields.Rules["visual." + key].Help(inherited.ToString("0.###",CultureInfo.CurrentCulture) + " (" + BaseWeatherTitle() + ")") + ((string)draft.visual["weatherPresetId"] == "" ? "\nПри основе «Из сцены» это справочное значение; фактическое наследуется при запуске карты." : ""));
            WatchInput(field,"visual." + key,false);
            field.EnableInClassList("is-default", draft.visual[key] == null);
            field.RegisterValueChangedCallback(evt => { if (!AcceptNumber(evt.newValue,"visual." + key)) return;
                if (Math.Abs(evt.newValue-inherited) < 1e-8) draft.visual.Remove(key); else draft.visual[key] = evt.newValue;
                field.EnableInClassList("is-default",draft.visual[key] == null); Changed(); });
        }
        private string BaseWeatherTitle() => catalog?.Weather((string)draft.visual["weatherPresetId"]) is DroneWeatherEntry entry ? DroneScenarioCatalog.WeatherTitle(entry.title) : "Из сцены";
        private void Vector(VisualElement parent, string path, string title, double[] defaults)
        {
            var heading = Box(parent,"vector-heading"); Label(heading,title,"vector-title"); DroneHelp.Attach(heading, () => DroneEnvironmentFields.Rules[path].Help()); var row = Box(parent, "vector-row");
            var vector = (JArray)draft.environment.SelectToken(path);
            for (int i = 0; i < 3; i++) {
                int axis = i; var field = new DroneDoubleField { value = (double)vector[i], isDelayed = true }; field.AddToClassList("vector-field"); row.Add(field); Register(field,path,false); WatchInput(field,path,false,path+"["+i+"]");
                field.name = path + "-" + i;
                DefaultStyle(field, field.value, defaults[i]);
                field.RegisterValueChangedCallback(evt => {
                    if (!AcceptNumber(evt.newValue,path+"["+axis+"]")) return; ((JArray)draft.environment.SelectToken(path))[axis] = evt.newValue; DefaultStyle(field, evt.newValue, defaults[axis]); Changed();
                    if (path == "windVelocityWorldMps") SyncMeanWind();
                });
            }
        }
        private void SyncWindVector()
        {
            var values = (JArray)draft.environment["windVelocityWorldMps"];
            for (int i=0;i<3;i++) editor.Q<DoubleField>("windVelocityWorldMps-"+i)?.SetValueWithoutNotify((double)values[i]);
        }
        private void SyncMeanWind()
        {
            var v = DVector3.From(draft.environment["windVelocityWorldMps"].ToObject<double[]>());
            double speed = Math.Sqrt(v.X * v.X + v.Z * v.Z), bearing = speed < 1e-9 ? 0 : (Math.Atan2(-v.X, -v.Z) * 180 / Math.PI + 360) % 360;
            editor.Q<DoubleField>("mean-wind-speed")?.SetValueWithoutNotify(speed);
            editor.Q<DoubleField>("mean-wind-bearing")?.SetValueWithoutNotify(bearing);
            editor.Q<Slider>("mean-wind-slider")?.SetValueWithoutNotify((float)speed);
            var compass = editor.Q<DroneWindCompass>(); if (compass != null) { compass.Bearing = (float)bearing; compass.MarkDirtyRepaint(); }
        }
        private void Bool(VisualElement parent, JObject source, string path, string title, bool fallback)
        {
            Register(Choice(parent, title + (source == draft.visual ? " *" : ""), new[] { "Включено", "Выключено" }, new[] { "true", "false" }, ((bool?)source.SelectToken(path) ?? fallback) ? "true" : "false",
                value => { Set(source, path, value == "true"); Changed(); }), source == draft.visual ? "visual." + path : path);
        }
        private static void Set(JObject source, string path, JToken value)
        {
            var split = path.Split('.'); JObject parent = source;
            for (int i = 0; i < split.Length - 1; i++) parent = (JObject)parent[split[i]];
            parent[split[split.Length - 1]] = value;
        }
        private void Register(VisualElement field,string path,bool help = true,Func<string> text = null)
        {
            if (!controls.TryGetValue(path,out var fields)) controls[path] = fields = new List<VisualElement>(); fields.Add(field);
            if (help && DroneEnvironmentFields.Rules.TryGetValue(path,out var rule)) DroneHelp.Attach(field,text ?? (() => rule.Help()));
        }
        private void WatchInput<T>(TextInputBaseField<T> field,string path,bool integer,string errorKey = null)
        {
            errorKey = errorKey ?? path;
            if (inputErrors.ContainsKey(errorKey) && inputText.TryGetValue(errorKey,out var existing)) {
                if (field is DroneDoubleField number) number.RestoreInput(existing);
                if (field is DroneIntegerField whole) whole.RestoreInput(existing);
            }
            field.RegisterCallback<InputEvent>(evt => {
                inputText[errorKey] = evt.newData;
                bool valid = double.TryParse(evt.newData.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double value) && DroneEnvironmentFields.Finite(value);
                if (integer) valid &= value == Math.Truncate(value) && value >= int.MinValue && value <= int.MaxValue;
                string error = integer ? "Введите целое число." : "Введите конечное число.";
                if (valid && DroneEnvironmentFields.Rules.TryGetValue(path,out var rule)) {
                    double physical = path == "temperatureK" ? value + 273.15 : value;
                    valid = physical >= rule.Min && physical <= rule.Max; error = "Допустимо: " + rule.Limits;
                }
                if (!valid) inputErrors[errorKey] = error; else inputErrors.Remove(errorKey);
                Changed();
            });
        }
        private bool AcceptNumber(double value,string path)
        {
            if (inputText.TryGetValue(path,out var typed) &&
                (!double.TryParse(typed.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out double parsed) || !DroneEnvironmentFields.Finite(parsed) ||
                 ((path == "turbulenceSeed" || path == "dryden.modesPerComponent") && parsed != Math.Truncate(parsed)))) {
                inputErrors[path] = "Введите число."; Changed(); return false;
            }
            if (!DroneEnvironmentFields.Finite(value)) { inputErrors[path] = "Введите конечное число."; Changed(); return false; }
            inputErrors.Remove(path); inputText.Remove(path); return true;
        }
        private Dictionary<string,string> ValidateFields()
        {
            var errors = DroneEnvironmentFields.Errors(draft,catalog);
            foreach (var input in inputErrors) errors[input.Key.Split('[')[0]] = input.Value;
            foreach (var pair in controls) foreach (var field in pair.Value) field.EnableInClassList("invalid-field",errors.ContainsKey(pair.Key));
            return errors;
        }
        private void Changed()
        {
            if (message == null || draft == null) return;
            ValidateFields(); RefreshWeatherCaption();
            message.text = Dirty ? "Изменения не сохранены" : savedStatus ? "Сохранено" : "";
            message.RemoveFromClassList("error");
        }
        private bool Save()
        {
            try {
                var errors = ValidateFields(); if (errors.Count > 0) { Error(errors.First().Value); return false; }
                var saved = DroneEnvironmentProfiles.Save(draft,catalog);
                int index = profiles.FindIndex(p => p.id == saved.id);
                if (index < 0) profiles.Add(saved); else profiles[index] = saved;
                Select(saved); savedStatus = true; Changed(); footnote.text = "Профилей: " + profiles.Count; Transfer("Сохранено: " + new DroneEnvironmentProfileStore(DroneEnvironmentProfiles.Folder).PathFor(saved.id)); return true;
            } catch (Exception ex) { Error(ex.Message); return false; }
        }
        private void Delete()
        {
            Ask("Удалить профиль?", selected.name, ("Назад", () => { }), ("Удалить", () => {
                try { DroneEnvironmentProfiles.Delete(selected); profiles.RemoveAll(p => p.id == selected.id); draft = null; selected = null; content.Clear(); BuildProfiles(); }
                catch (Exception ex) { Error(ex.Message); }
            }));
        }
        private async void Export()
        {
            if (fileDialogBusy || draft == null) return;
            if (ValidateFields().Count > 0) { Error("Исправьте поля с красной обводкой."); return; }
            fileDialogBusy = true;
            try {
                var result = await DroneFileDialog.Pick(true,DroneEnvironmentProfiles.ExchangeFolder);
                if (page.panel == null) return;
                if (result.Failed) { Debug.LogWarning("DroneLab file dialog: " + result.Error); ExportTo(null); }
                else if (!string.IsNullOrEmpty(result.Path)) ExportTo(result.Path);
            } catch (Exception ex) { if (page.panel != null) { Debug.LogWarning(ex.Message); ExportTo(null); } }
            finally { fileDialogBusy = false; }
        }
        private void ExportTo(string destination)
        {
            try { Transfer("Экспорт: " + DroneEnvironmentProfiles.Export(draft,destination,catalog)); }
            catch (Exception ex) { Transfer(ex.Message,true); }
        }
        private async void Import()
        {
            if (fileDialogBusy) return; fileDialogBusy = true;
            try {
                var result = await DroneFileDialog.Pick(false,DroneEnvironmentProfiles.ExchangeFolder);
                if (page.panel == null) return;
                if (result.Failed) { Debug.LogWarning("DroneLab file dialog: " + result.Error); ImportFallback(); }
                else if (!string.IsNullOrEmpty(result.Path)) ImportFrom(result.Path);
            } catch (Exception ex) { if (page.panel != null) { Debug.LogWarning(ex.Message); ImportFallback(); } }
            finally { fileDialogBusy = false; }
        }
        private void ImportFrom(string path)
        {
            try {
                var imported = DroneEnvironmentProfiles.Import(path); profiles.Add(imported); Select(imported);
                footnote.text = "Профилей: " + profiles.Count; Transfer("Импортировано: " + new DroneEnvironmentProfileStore(DroneEnvironmentProfiles.Folder).PathFor(imported.id));
            } catch (Exception ex) { Transfer(ex.Message,true); ImportFallback(path); }
        }
        private void ImportFallback(string initial = null)
        {
            prompt?.RemoveFromHierarchy(); prompt = Box(page, "scenario-prompt");
            var card = Box(prompt, "scenario-prompt-card"); Label(card, "Импорт профиля", "scenario-panel-title");
            Label(card, "Укажите полный путь к JSON профиля среды", "scenario-hint");
            var path = new TextField("Файл") { value = initial ?? DroneEnvironmentProfiles.ExchangeFolder }; Field(card, path);
            var error = Label(card, "", "scenario-hint"); var actions = Box(card, "scenario-actions");
            Button(actions, "Назад", () => { prompt.RemoveFromHierarchy(); prompt = null; });
            Button(actions, "Импортировать", () => { try {
                var imported = DroneEnvironmentProfiles.Import(path.value); profiles.Add(imported);
                prompt.RemoveFromHierarchy(); prompt = null; Select(imported); footnote.text = "Профилей: " + profiles.Count; Transfer("Импортировано: " + new DroneEnvironmentProfileStore(DroneEnvironmentProfiles.Folder).PathFor(imported.id));
            } catch (Exception ex) { path.AddToClassList("invalid-field"); error.text = ex.Message; } });
        }
        private void Transfer(string text,bool error = false) { transferStatus.text = text; transferStatus.tooltip = text; transferStatus.EnableInClassList("error",error); }
        private void Error(string text, bool error = true) { if (message != null) { message.text = text; message.EnableInClassList("error", error); } }
        private static bool Contains(string text, string query) => string.IsNullOrWhiteSpace(query) || (text ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        private static void DefaultStyle(VisualElement field, double value, double fallback) => field.EnableInClassList("is-default", Math.Abs(value - fallback) < 1e-8);
        private static void Field<T>(VisualElement parent, BaseField<T> field) { field.AddToClassList("environment-field"); ReadOnly(field.labelElement); parent.Add(field); }
        private static DroneDropdown Choice(VisualElement parent, string title, IEnumerable<string> names, IEnumerable<string> ids, string value, Action<string> changed)
        {
            var keys = ids.ToList(); var field = new DroneDropdown(title, DroneScenarioCatalog.DisplayChoices(names), Math.Max(0, keys.IndexOf(value)));
            Field(parent, field); field.Query<Label>().ForEach(ReadOnly);
            field.RegisterValueChangedCallback(_ => { if (field.index >= 0 && field.index < keys.Count) changed(keys[field.index]); }); return field;
        }
        private static VisualElement Section(VisualElement parent, string title) { var section = Box(parent, "environment-section"); Label(section, title, "scenario-panel-title"); return section; }
        private static VisualElement Box(VisualElement parent, string classes) { var box = new VisualElement(); Classes(box, classes); parent.Add(box); return box; }
        private static Label Label(VisualElement parent, string text, string classes) { var label = new Label(text); ReadOnly(label); Classes(label, classes); parent.Add(label); return label; }
        private static Button Button(VisualElement parent, string text, Action clicked, string classes = "scenario-button") { var button = new Button(clicked) { text = text }; Classes(button, classes); parent.Add(button); return button; }
        private static void Classes(VisualElement element, string classes) { foreach (var c in classes.Split(' ')) element.AddToClassList(c); }
        private static void ReadOnly(Label label) { label.selection.isSelectable = false; label.focusable = false; label.pickingMode = PickingMode.Ignore; }
        private static ScrollView Scroll(VisualElement parent)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("scenario-scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScroller.AddToClassList("graphite-scroller");
            scroll.verticalScroller.lowButton.style.display = DisplayStyle.None; scroll.verticalScroller.highButton.style.display = DisplayStyle.None;
            parent.Add(scroll); return scroll;
        }
    }

    internal sealed class DroneWindCompass : VisualElement
    {
        public float Bearing;
        public DroneWindCompass() { AddToClassList("wind-compass"); pickingMode = PickingMode.Ignore;
            var north = new Label("N"); north.AddToClassList("compass-north"); north.pickingMode = PickingMode.Ignore; Add(north); generateVisualContent += Draw; }
        private void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D; float r = contentRect.width * .35f; var c = contentRect.center;
            p.lineWidth = 1.2f; p.strokeColor = new Color(.7f, .7f, .69f);
            p.BeginPath(); for (int i = 0; i <= 48; i++) { float a = i * Mathf.PI / 24; var q = c + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * r; if (i == 0) p.MoveTo(q); else p.LineTo(q); } p.ClosePath(); p.Stroke();
            float angle = Bearing * Mathf.Deg2Rad; var d = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)); var normal = new Vector2(-d.y, d.x);
            p.strokeColor = Color.white; p.BeginPath(); p.MoveTo(c - d * r * .35f); p.LineTo(c + d * r * .85f); p.Stroke();
            p.BeginPath(); p.MoveTo(c + d * r * .5f + normal * 5); p.LineTo(c + d * r * .85f); p.LineTo(c + d * r * .5f - normal * 5); p.Stroke();
            for (int i = 0; i < 4; i++) { float a = i * Mathf.PI / 2; var d2 = new Vector2(Mathf.Sin(a), -Mathf.Cos(a)); p.BeginPath(); p.MoveTo(c + d2 * r); p.LineTo(c + d2 * (r + 5)); p.Stroke(); }
        }
    }
}
