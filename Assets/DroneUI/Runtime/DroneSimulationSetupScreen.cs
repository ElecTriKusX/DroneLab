using System;
using System.Collections.Generic;
using System.Linq;
using DroneLab.Physics;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal static class DroneLaunchSteps
    {
        public static void Add(VisualElement page, int step)
        {
            var strip = new VisualElement(); strip.AddToClassList("simulation-steps");
            var first = DroneProfileFields.Label(strip, "01  Выберите дрон", "simulation-step");
            first.EnableInClassList("active", step == 1);
            var arrow = new DroneMenuIcon(MenuIconKind.Chevron); arrow.AddToClassList("simulation-step-arrow"); strip.Add(arrow);
            var second = DroneProfileFields.Label(strip, "02  Выберите сцену и погоду", "simulation-step");
            second.EnableInClassList("active", step == 2); page.Insert(1, strip);
        }
    }

    internal sealed class DroneSimulationSetupScreen : IDisposable
    {
        private readonly VisualElement host;
        private readonly DroneScenarioCatalog catalog;
        private readonly Action closed, launched;
        private DroneDronesScreen drones;
        private VisualElement page;
        private DroneProfileDocument drone;
        private DroneMapEntry map;
        private DroneEnvironmentDocument environment;
        private Label summary, status;
        private Button launch;
        private readonly Dictionary<DroneMapEntry, VisualElement> mapCards = new Dictionary<DroneMapEntry, VisualElement>();
        private readonly Dictionary<DroneEnvironmentDocument, VisualElement> weatherCards = new Dictionary<DroneEnvironmentDocument, VisualElement>();
        private bool disposed, loading;
        private string mapSearch = "", weatherSearch = "";
        private List<DroneEnvironmentDocument> environments;
        private IVisualElementScheduledItem progress;
        public VisualElement Element => drones != null ? drones.Element : page;

        public DroneSimulationSetupScreen(VisualElement host, DroneScenarioCatalog catalog, Action closed, Action launched)
        {
            this.host = host; this.catalog = catalog; this.closed = closed; this.launched = launched;
            ShowDrones();
        }
        private void ShowDrones()
        {
            page?.RemoveFromHierarchy(); page = null;
            drones = new DroneDronesScreen(host, Close, selected => {
                drone = selected; drones.Dispose(); drones.Element.RemoveFromHierarchy(); drones = null; ShowEnvironment();
            }, drone?.id ?? PlayerPrefs.GetString("DroneLab.SelectedDrone", ""));
        }
        private static VisualElement Box(VisualElement parent, string css)
        { var box = new VisualElement(); box.AddToClassList(css); parent.Add(box); return box; }
        private static Button Button(VisualElement parent, string caption, Action action)
        { var button = new Button(action) { text = caption }; button.AddToClassList("scenario-button"); parent.Add(button); return button; }
        private static ScrollView Scroll(VisualElement parent)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("scenario-scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden; scroll.verticalScroller.AddToClassList("graphite-scroller");
            scroll.verticalScroller.lowButton.style.display = DisplayStyle.None; scroll.verticalScroller.highButton.style.display = DisplayStyle.None;
            parent.Add(scroll); return scroll;
        }
        private static TextField Search(VisualElement parent, string title, string value)
        {
            var field = new TextField(title); field.SetValueWithoutNotify(value); field.AddToClassList("scenario-search");
            DroneTextInput.Configure(field); DroneProfileFields.ReadOnly(field); parent.Add(field); return field;
        }
        private static bool Contains(string source, string query) => string.IsNullOrWhiteSpace(query) ||
            (source ?? "").IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        private void ShowEnvironment()
        {
            environments = DroneEnvironmentProfiles.LoadAll(out var warnings);
            var maps = catalog?.maps?.Where(m => m != null).ToList() ?? new List<DroneMapEntry>();
            map = maps.FirstOrDefault(m => m.id == map?.id) ?? maps.FirstOrDefault(m => m.id == PlayerPrefs.GetString("DroneLab.SelectedMap", "")) ?? maps.FirstOrDefault();
            environment = environments.FirstOrDefault(e => e.id == environment?.id) ?? environments.FirstOrDefault(e => e.id == PlayerPrefs.GetString("DroneLab.SelectedEnvironment", "")) ?? environments.FirstOrDefault();
            page = Box(host, "scenario-page"); page.AddToClassList("simulation-page");
            var header = Box(page, "scenario-header"); DroneProfileFields.Label(header, "ДРОНЛАБ", "scenario-brand"); Box(header, "scenario-header-divider");
            DroneProfileFields.Label(header, "НОВАЯ СИМУЛЯЦИЯ", "scenario-title"); DroneLaunchSteps.Add(page, 2);
            var columns = Box(page, "simulation-columns"); var mapsPanel = Box(columns, "simulation-maps");
            DroneProfileFields.Label(mapsPanel, "КАТАЛОГ СЦЕН", "scenario-panel-title");
            var search = Search(mapsPanel, "Поиск карты", mapSearch); var mapScroll = Scroll(mapsPanel); var grid = Box(mapScroll, "map-grid");
            void FillMaps()
            {
                grid.Clear(); mapCards.Clear(); int count = 0;
                foreach (var entry in maps.Where(m => Contains(m.title + " " + m.terrainType, mapSearch))) {
                    var card = Box(grid, "map-card"); card.AddToClassList("simulation-map-card"); card.EnableInClassList("map-row-end", count++ % 2 == 1);
                    bool available = !string.IsNullOrWhiteSpace(entry.scenePath) && Application.CanStreamedLevelBeLoaded(entry.scenePath);
                    card.focusable = true; card.tabIndex = 0; card.tooltip = available ? "Выбрать сцену «" + entry.title + "»" : "Добавьте эту сцену в список сцен сборки.";
                    var preview = Box(card, "map-preview");
                    preview.RegisterCallback<GeometryChangedEvent>(evt => { float height = evt.newRect.width * 9f / 16f; if (Math.Abs(preview.resolvedStyle.height - height) > 1) preview.style.height = height; });
                    if (entry.screenshot != null) preview.style.backgroundImage = new StyleBackground(entry.screenshot);
                    else { var icon = new DroneMenuIcon(MenuIconKind.Landscape); icon.AddToClassList("map-placeholder"); preview.Add(icon); }
                    DroneProfileFields.Label(preview, entry.title, "map-name");
                    string size = entry.sizeM.x > 0 && entry.sizeM.y > 0 ? $"{entry.sizeM.x / 1000f:0.##} × {entry.sizeM.y / 1000f:0.##} км" : "Размер не указан";
                    DroneProfileFields.Label(card, entry.terrainType + " · " + size + (available ? "" : " · Недоступна"), "map-meta");
                    mapCards[entry] = card;
                    void Pick() { map = entry; status.text = available ? "" : "Сцена не включена в сборку."; UpdateSelection(); }
                    card.RegisterCallback<ClickEvent>(evt => { if (evt.button == 0) { Pick(); card.Focus(); } });
                    card.RegisterCallback<KeyDownEvent>(evt => { if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Space) { Pick(); evt.StopPropagation(); } });
                }
                if (count == 0) DroneProfileFields.Label(grid, maps.Count == 0 ? "В каталоге пока нет карт" : "Карты не найдены", "scenario-hint");
                UpdateSelection();
            }
            var weatherPanel = Box(columns, "profile-library"); weatherPanel.AddToClassList("simulation-weather");
            DroneProfileFields.Label(weatherPanel, "ПОГОДА И СРЕДА", "scenario-panel-title");
            var weatherFilter = Search(weatherPanel, "Поиск профиля", weatherSearch); var weatherList = Scroll(weatherPanel);
            void FillWeather()
            {
                weatherList.Clear(); weatherCards.Clear();
                foreach (var entry in environments.Where(e => Contains(e.name, weatherSearch))) {
                    var row = Button(weatherList, "", () => { environment = entry; status.text = ""; UpdateSelection(); });
                    row.AddToClassList("profile-list-item");
                    var physical = DroneEnvironmentProfiles.Effective(entry.environment);
                    var icon = new DroneMenuIcon((string)physical["windMode"] == "None" ? MenuIconKind.Sun : MenuIconKind.Wind); icon.AddToClassList("profile-icon"); row.Add(icon);
                    var text = Box(row, "profile-list-text"); DroneProfileFields.Label(text, entry.name, "profile-list-name");
                    var velocity = DVector3.From(physical["windVelocityWorldMps"].ToObject<double[]>()); double hours = (double?)entry.visual["timeOfDay"] ?? 14;
                    DroneProfileFields.Label(text, $"{velocity.Length:0.##} м/с · {(double)physical["temperatureK"] - 273.15:0.#} °C · {(int)hours:00}:{(int)((hours % 1) * 60):00}", "scenario-muted");
                    weatherCards[entry] = row;
                }
                if (weatherCards.Count == 0) DroneProfileFields.Label(weatherList, environments.Count == 0 ? "Сохраните профиль в разделе «Сценарии и окружение»." : "Профили не найдены", "scenario-hint");
                UpdateSelection();
            }
            var footer = Box(page, "simulation-footer");
            summary = DroneProfileFields.Label(footer, "", "simulation-summary"); status = DroneProfileFields.Label(footer, warnings ?? "", "simulation-status");
            var actions = Box(footer, "simulation-actions"); Button(actions, "Назад", RequestClose);
            launch = Button(actions, "Начать симуляцию", Launch); launch.AddToClassList("primary");
            search.RegisterValueChangedCallback(evt => { mapSearch = evt.newValue; FillMaps(); });
            weatherFilter.RegisterValueChangedCallback(evt => { weatherSearch = evt.newValue; FillWeather(); });
            FillMaps(); FillWeather();
        }
        private void UpdateSelection()
        {
            foreach (var entry in mapCards) entry.Value.EnableInClassList("selected", entry.Key == map);
            foreach (var entry in weatherCards) entry.Value.EnableInClassList("selected", entry.Key == environment);
            if (summary != null) summary.text = drone.Name + "   /   " + (map?.title ?? "Выберите сцену") + "   /   " + (environment?.name ?? "Выберите погоду");
            launch?.SetEnabled(!loading && map != null && environment != null && !string.IsNullOrWhiteSpace(map.scenePath) && Application.CanStreamedLevelBeLoaded(map.scenePath));
        }
        private void Launch()
        {
            if (loading || disposed || map == null || environment == null) return;
            try {
                var operation = DroneScenarioLaunch.Load(map, environment, catalog, drone);
                loading = true; page.SetEnabled(false); status.text = "Загрузка сцены…";
                PlayerPrefs.SetString("DroneLab.SelectedDrone", drone.id); PlayerPrefs.SetString("DroneLab.SelectedMap", map.id ?? "");
                PlayerPrefs.SetString("DroneLab.SelectedEnvironment", environment.id); PlayerPrefs.Save(); launched();
                progress = page.schedule.Execute(() => { if (!disposed) status.text = $"Загрузка сцены… {Mathf.Min(100, Mathf.RoundToInt(operation.progress / .9f * 100))}%"; }).Every(100);
            } catch (Exception ex) { status.text = ex.Message; status.AddToClassList("error"); }
        }
        public void RequestClose() { if (loading || disposed) return; if (drones != null) drones.RequestClose(); else ShowDrones(); }
        private void Close() { Dispose(); closed(); }
        public void Dispose() { if (disposed) return; disposed = true; progress?.Pause(); drones?.Dispose(); drones?.Element.RemoveFromHierarchy(); drones = null; page?.RemoveFromHierarchy(); }
    }
}
