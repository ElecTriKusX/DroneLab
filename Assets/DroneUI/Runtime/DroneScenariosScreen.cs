using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Label footnote;
        private DroneEnvironmentDocument selected, draft;
        private string original, filter = "";
        private VisualElement library, editor, prompt;
        private Label message;
        public VisualElement Element => page;
        private bool Dirty => draft != null && JsonConvert.SerializeObject(draft) != original;

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
            draft = null; Label(content, "Каталог доступных карт", "scenario-hint");
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
            if (selected == null) selected = profiles.FirstOrDefault();
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
                Label(row, d.builtIn ? "Встроенный" : "Мой профиль", "profile-origin");
            }
        }
        private void Select(DroneEnvironmentDocument source)
        {
            selected = source; draft = source.Copy();
            // Explicit standard defaults are editable; unused optional parameters remain harmless.
            draft.environment = DroneEnvironmentProfiles.Effective(draft.environment);
            original = !source.builtIn && !profiles.Any(p => p.id == source.id) ? null : JsonConvert.SerializeObject(draft);
            FillList(); BuildEditor();
        }
        private void BuildEditor()
        {
            editor.Clear();
            var head = Box(editor, "scenario-toolbar"); Label(head, "РЕДАКТОР ПРОФИЛЯ", "scenario-panel-title");
            if (!draft.builtIn && profiles.Any(p => p.id == draft.id)) Button(head, "Удалить", Delete);
            Label(editor, "Профили доступны для всех карт", "scenario-muted");
            var name = new TextField("Название") { value = draft.name }; name.AddToClassList("profile-name-field"); ReadOnly(name.labelElement); editor.Add(name);
            name.RegisterValueChangedCallback(evt => { draft.name = evt.newValue; Changed(); });
            var scroll = Scroll(editor); scroll.AddToClassList("profile-form-scroll");
            var columns = Box(scroll, "profile-form-columns");
            var left = Box(columns, "profile-form-panel"); var right = Box(columns, "profile-form-panel");
            Label(left, "ПОГОДА И ВРЕМЯ", "scenario-panel-title");
            WeatherChoice(left);
            TimeOfDay(left);
            Bool(left, draft.visual, "simulateTime", "Ход времени", false);
            Choice(left, "Осадки", new[] { "Нет", "Дождь", "Снег", "Град" }, new[] { "None", "Rain", "Snow", "Hail" }, (string)draft.environment["weather"]["precipitation"], value => {
                draft.environment["weather"]["precipitation"] = value;
                if (value == "None") draft.environment["weather"]["intensityMmPerHour"] = 0;
                else if ((double)draft.environment["weather"]["intensityMmPerHour"] == 0) draft.environment["weather"]["intensityMmPerHour"] = 5;
                Changed(); RebuildEditor();
            });
            Number(left, draft.environment, "weather.intensityMmPerHour", "Осадки, мм/ч", 0);
            Label(left, "Осадки: визуальный эффект", "scenario-muted");
            var weatherPreview = Box(left, "weather-preview");
            var registered = catalog?.Weather((string)draft.visual["weatherPresetId"]);
            if (registered?.screenshot != null) weatherPreview.style.backgroundImage = new StyleBackground(registered.screenshot);
            else { var icon = new DroneMenuIcon(MenuIconKind.Sun); icon.AddToClassList("weather-preview-icon"); weatherPreview.Add(icon); Label(weatherPreview, "Превью погоды", "scenario-muted"); }
            Label(right, "ВЕТЕР И ВОЗДУХ", "scenario-panel-title");
            var modes = new[] { "None", "Constant", "Gust", "Turbulence", "DrydenFrozen", "CustomField" };
            Choice(right, "Режим ветра", new[] { "Штиль", "Постоянный", "Порывы", "Турбулентность", "Dryden", "Enviro / внешнее поле" }, modes,
                (string)draft.environment["windMode"], mode => {
                    draft.environment["windMode"] = mode;
                    if (mode == "Gust") draft.environment["gustEnabled"] = true;
                    if (mode == "None" || mode == "CustomField") draft.environment["gustEnabled"] = false;
                    Changed(); RebuildEditor();
                });
            Wind(right);
            Choice(right, "Модель воздуха", new[] { "Постоянная плотность", "Стандартная атмосфера" }, new[] { "Constant", "StandardAtmosphere" },
                (string)draft.environment["airDensityMode"], mode => { draft.environment["airDensityMode"] = mode; Changed(); RebuildEditor(); });
            Temperature(right);
            Label(right, (string)draft.environment["airDensityMode"] == "StandardAtmosphere" ? "Температура и давление заданы на уровне моря" : "Плотность воздуха задаётся независимо", "scenario-hint small");
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
            var dryden = new Foldout { text = "Параметры Dryden", value = (string)draft.environment["windMode"] == "DrydenFrozen" }; advanced.Add(dryden);
            Vector(dryden, "dryden.sigmaUvwMps", "RMS u / v / w, м/с", new[] { .4, .4, .25 });
            Vector(dryden, "dryden.lengthScaleUvwM", "Масштабы u / v / w, м", new[] { 20.0, 20, 10 });
            Vector(dryden, "dryden.advectionDirectionWorld", "Ось переноса X / Y / Z", new[] { 1.0, 0, 0 });
            Number(dryden, draft.environment, "dryden.advectionSpeedMps", "Скорость переноса, м/с", 5);
            Number(dryden, draft.environment, "dryden.modesPerComponent", "Гармоник на компоненту", 32, true);
            Number(dryden, draft.environment, "dryden.minDimensionlessWaveNumber", "Минимальное kL", .02);
            Number(dryden, draft.environment, "dryden.maxDimensionlessWaveNumber", "Максимальное kL", 20);
            Label(dryden, "Ось переноса: единичный горизонтальный вектор. RMS — среднеквадратичная величина, не максимальный порыв.", "scenario-hint small");
            var visuals = Section(advanced, "ВИЗУАЛЬНОЕ ОКРУЖЕНИЕ");
            Label(visuals, "Серые значения наследуются из погодного пресета. «Из пресета» отменяет переопределение.", "scenario-hint small");
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
            Label(advanced, "Контракт физики 1.0.0 · Осадки VisualOnly", "scenario-muted");
            var actions = Box(editor, "profile-editor-actions"); message = Label(actions, "", "profile-message");
            Button(actions, "Отменить", () => Select(selected));
            Button(actions, draft.builtIn ? "Сохранить копию" : "Сохранить профиль", () => Save(), "scenario-button primary");
            Changed();
        }
        private void RebuildEditor() => editor.schedule.Execute(BuildEditor);
        private void WeatherChoice(VisualElement parent)
        {
            var titles = new List<string> { "Из сцены" }; var ids = new List<string> { "" };
            if (catalog != null) foreach (var weather in catalog.weatherPresets.Where(w => w != null && w.preset != null)) { titles.Add(weather.title); ids.Add(weather.id); }
            string id = (string)draft.visual["weatherPresetId"];
            if (!ids.Contains(id)) { titles.Add("Недоступный пресет"); ids.Add(id); }
            Choice(parent, "Погода", titles, ids, id, value => { draft.visual["weatherPresetId"] = value; Changed(); RebuildEditor(); });
        }
        private void TimeOfDay(VisualElement parent)
        {
            double hours = (double)draft.visual["timeOfDay"];
            string Caption(double time) => $"{(int)time:00}:{(int)((time % 1) * 60 + 1e-7):00}";
            var field = new TextField("Время суток") { value = Caption(hours), isDelayed = true };
            Field(parent, field); DefaultStyle(field, hours, 14);
            field.RegisterValueChangedCallback(evt => {
                var parts = evt.newValue.Trim().Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int hour) || !int.TryParse(parts[1], out int minute) || hour < 0 || hour > 23 || minute < 0 || minute > 59) {
                    field.SetValueWithoutNotify(Caption((double)draft.visual["timeOfDay"]));
                    Error("Введите время в формате ЧЧ:ММ, от 00:00 до 23:59."); return;
                }
                hours = hour + minute / 60.0; draft.visual["timeOfDay"] = hours;
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
            var number = new DoubleField { value = speed, isDelayed = true, name = "mean-wind-speed" }; number.AddToClassList("wind-speed-number"); speedRow.Add(number);
            var direction = new DoubleField("Откуда дует, °") { value = bearing, isDelayed = true, name = "mean-wind-bearing" }; direction.AddToClassList("environment-field"); ReadOnly(direction.labelElement);
            var row = Box(parent, "wind-direction-row"); row.Add(direction);
            var compass = new DroneWindCompass { Bearing = (float)bearing }; row.Add(compass);
            void UpdateWind(double s, double degrees) {
                if (double.IsNaN(s) || double.IsInfinity(s) || s < 0 || double.IsNaN(degrees) || double.IsInfinity(degrees)) {
                    number.SetValueWithoutNotify(speed); direction.SetValueWithoutNotify(bearing); slider.SetValueWithoutNotify((float)speed);
                    Error("Скорость ветра должна быть неотрицательным конечным числом."); return;
                }
                speed = s; bearing = degrees;
                double rad = degrees * Math.PI / 180;
                double vertical = (double)draft.environment["windVelocityWorldMps"][1];
                draft.environment["windVelocityWorldMps"] = new JArray(-s * Math.Sin(rad), vertical, -s * Math.Cos(rad));
                compass.Bearing = (float)degrees; compass.MarkDirtyRepaint(); Changed();
            }
            slider.RegisterValueChangedCallback(evt => { number.SetValueWithoutNotify(evt.newValue); UpdateWind(evt.newValue, direction.value); });
            number.RegisterValueChangedCallback(evt => { slider.SetValueWithoutNotify((float)evt.newValue); UpdateWind(evt.newValue, direction.value); });
            direction.RegisterValueChangedCallback(evt => UpdateWind(number.value, evt.newValue));
            speedRow.SetEnabled(!external); row.SetEnabled(!external);
            if (external) Label(parent, "Ветер берётся из погодного пресета Enviro / провайдера сцены", "scenario-hint small");
        }
        private void Temperature(VisualElement parent)
        {
            var field = new DoubleField("Температура, °C") { value = (double)draft.environment["temperatureK"] - 273.15, isDelayed = true };
            field.AddToClassList("environment-field"); ReadOnly(field.labelElement); parent.Add(field);
            DefaultStyle(field, field.value, 15);
            field.RegisterValueChangedCallback(evt => { draft.environment["temperatureK"] = evt.newValue + 273.15; DefaultStyle(field, evt.newValue, 15); Changed(); });
        }
        private void Number(VisualElement parent, JObject source, string path, string title, double defaultValue, bool integer = false)
        {
            double value = (double?)source.SelectToken(path) ?? defaultValue;
            if (integer) {
                var field = new IntegerField(title) { value = (int)value, isDelayed = true }; Field(parent, field); DefaultStyle(field, value, defaultValue);
                field.RegisterValueChangedCallback(evt => { Set(source, path, evt.newValue); DefaultStyle(field, evt.newValue, defaultValue); Changed(); });
            } else {
                var field = new DoubleField(title) { value = value, isDelayed = true }; Field(parent, field); DefaultStyle(field, value, defaultValue);
                field.RegisterValueChangedCallback(evt => { Set(source, path, evt.newValue); DefaultStyle(field, evt.newValue, defaultValue); Changed(); });
            }
        }
        private void VisualNumber(VisualElement parent, string key, string title, double inherited)
        {
            var row = Box(parent, "visual-override-row");
            var field = new DoubleField(title) { value = (double?)draft.visual[key] ?? inherited, isDelayed = true }; Field(row, field);
            field.EnableInClassList("is-default", draft.visual[key] == null);
            field.RegisterValueChangedCallback(evt => { draft.visual[key] = evt.newValue; field.RemoveFromClassList("is-default"); Changed(); });
            Button(row, "Из пресета", () => { draft.visual.Remove(key); field.SetValueWithoutNotify(inherited); field.AddToClassList("is-default"); Changed(); }, "scenario-button inherit-button");
        }
        private void Vector(VisualElement parent, string path, string title, double[] defaults)
        {
            Label(parent, title, "vector-title"); var row = Box(parent, "vector-row");
            var vector = (JArray)draft.environment.SelectToken(path);
            for (int i = 0; i < 3; i++) {
                int axis = i; var field = new DoubleField { value = (double)vector[i], isDelayed = true }; field.AddToClassList("vector-field"); row.Add(field);
                DefaultStyle(field, field.value, defaults[i]);
                field.RegisterValueChangedCallback(evt => {
                    ((JArray)draft.environment.SelectToken(path))[axis] = evt.newValue; DefaultStyle(field, evt.newValue, defaults[axis]); Changed();
                    if (path == "windVelocityWorldMps") SyncMeanWind();
                });
            }
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
            Choice(parent, title, new[] { "Включено", "Выключено" }, new[] { "true", "false" }, ((bool?)source.SelectToken(path) ?? fallback) ? "true" : "false",
                value => { Set(source, path, value == "true"); Changed(); });
        }
        private static void Set(JObject source, string path, JToken value)
        {
            var split = path.Split('.'); JObject parent = source;
            for (int i = 0; i < split.Length - 1; i++) parent = (JObject)parent[split[i]];
            parent[split[split.Length - 1]] = value;
        }
        private void Changed()
        {
            if (message == null) return;
            message.text = Dirty ? "Изменения не сохранены" : draft.builtIn ? "Встроенный профиль" : "Профиль сохранён";
            message.RemoveFromClassList("error");
        }
        private bool Save()
        {
            try {
                var toSave = draft.Copy();
                if (toSave.builtIn && toSave.name == selected.name) toSave.name += " — копия";
                var saved = DroneEnvironmentProfiles.Save(toSave);
                int index = profiles.FindIndex(p => p.id == saved.id);
                if (index < 0) profiles.Add(saved); else profiles[index] = saved;
                Select(saved); footnote.text = "Профилей: " + profiles.Count; return true;
            } catch (Exception ex) { Error(ex.Message); return false; }
        }
        private void Delete()
        {
            Ask("Удалить профиль?", selected.name, ("Назад", () => { }), ("Удалить", () => {
                try { DroneEnvironmentProfiles.Delete(selected); profiles.RemoveAll(p => p.id == selected.id); draft = null; selected = null; content.Clear(); BuildProfiles(); }
                catch (Exception ex) { Error(ex.Message); }
            }));
        }
        private void Export()
        {
            try { string path = DroneEnvironmentProfiles.Export(draft); Error("Экспорт: " + path, false); }
            catch (Exception ex) { Error(ex.Message); }
        }
        private void Import()
        {
            prompt?.RemoveFromHierarchy(); prompt = Box(page, "scenario-prompt");
            var card = Box(prompt, "scenario-prompt-card"); Label(card, "Импорт профиля", "scenario-panel-title");
            Label(card, "Укажите полный путь к JSON профиля среды", "scenario-hint");
            var path = new TextField("Файл") { value = DroneEnvironmentProfiles.ExchangeFolder }; Field(card, path);
            var error = Label(card, "", "scenario-hint"); var actions = Box(card, "scenario-actions");
            Button(actions, "Назад", () => { prompt.RemoveFromHierarchy(); prompt = null; });
            Button(actions, "Импортировать", () => { try {
                var imported = DroneEnvironmentProfiles.Import(path.value); profiles.Add(imported);
                prompt.RemoveFromHierarchy(); prompt = null; Select(imported); footnote.text = "Профилей: " + profiles.Count;
            } catch (Exception ex) { error.text = ex.Message; } });
        }
        private void Error(string text, bool error = true) { if (message != null) { message.text = text; message.EnableInClassList("error", error); } }
        private static bool Contains(string text, string query) => string.IsNullOrWhiteSpace(query) || (text ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        private static void DefaultStyle(VisualElement field, double value, double fallback) => field.EnableInClassList("is-default", Math.Abs(value - fallback) < 1e-8);
        private static void Field<T>(VisualElement parent, BaseField<T> field) { field.AddToClassList("environment-field"); ReadOnly(field.labelElement); parent.Add(field); }
        private static DropdownField Choice(VisualElement parent, string title, IEnumerable<string> names, IEnumerable<string> ids, string value, Action<string> changed)
        {
            var keys = ids.ToList(); var field = new DropdownField(title, DroneScenarioCatalog.DisplayChoices(names), Math.Max(0, keys.IndexOf(value)));
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
        public DroneWindCompass() { AddToClassList("wind-compass"); pickingMode = PickingMode.Ignore; generateVisualContent += Draw; }
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
