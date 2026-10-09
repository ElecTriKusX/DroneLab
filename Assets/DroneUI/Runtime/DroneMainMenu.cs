using System;
using System.Collections.Generic;
using System.Linq;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    [Serializable]
    public sealed class DroneDeveloperLink
    {
        public string name;
        [Tooltip("Optional https:// link. Leave empty to show a plain name.")]
        public string url;
        public DroneDeveloperLink(string name) { this.name = name; url = ""; }
    }

    [DisallowMultipleComponent]
    public sealed class DroneMainMenu : MonoBehaviour
    {
        [SerializeField] private Texture2D background;
        [SerializeField] private Font menuFont;
        [SerializeField] private string flightScene = "PhysTest";
        [SerializeField] private string version = "0.1.2";
        [SerializeField] private DroneScenarioCatalog scenarioCatalog;
        [SerializeField] private DroneDeveloperLink[] developerLinks =
        {
            new DroneDeveloperLink("Матвиенко А. В."), new DroneDeveloperLink("Якубовский Д. А."),
            new DroneDeveloperLink("Поляков И. М."), new DroneDeveloperLink("Николаев С. Н.")
        };
        [Header("Future screens")]
        [SerializeField] private UnityEvent configureDrone = new UnityEvent();
        [SerializeField] private UnityEvent scenarios = new UnityEvent();
        [SerializeField] private UnityEvent laboratory = new UnityEvent();
        private UIDocument document;
        private PanelSettings panel;
        private VisualElement root, stage, modal, stack;
        private Label status;
        private Button firstButton, returnFocus;
        private bool loading;
        private DroneScenariosScreen scenarioScreen;
        private DroneDronesScreen dronesScreen;

        private void OnEnable()
        {
            DroneApplicationSettings.EnsureInitialized();
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.sortingOrder = 100;
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DroneLab/MainMenuTheme");
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            loading = false;
            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            root = document.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.backgroundColor = Color.black;
            var sheet = Resources.Load<StyleSheet>("DroneLab/MainMenu");
            if (sheet != null) root.styleSheets.Add(sheet);
            var scenariosSheet = Resources.Load<StyleSheet>("DroneLab/Scenarios");
            if (scenariosSheet != null) root.styleSheets.Add(scenariosSheet);
            stage = new VisualElement { name = "reference-stage" };
            stage.AddToClassList("stage");
            if (menuFont != null) stage.style.unityFont = menuFont;
            root.Add(stage);
            root.RegisterCallback<GeometryChangedEvent>(Resize);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            SetBackground(background);
            stack = new VisualElement(); stack.AddToClassList("menu-stack"); stage.Add(stack);
            firstButton = AddMenuButton("НОВАЯ СИМУЛЯЦИЯ", MenuIconKind.Play, StartFlight, true);
            AddMenuButton("КАТАЛОГ ДРОНОВ", MenuIconKind.Drone, OpenDrones);
            AddMenuButton("СЦЕНАРИИ И ОКРУЖЕНИЕ", MenuIconKind.Landscape, OpenScenarios);
            AddMenuButton("ЛАБОРАТОРИЯ", MenuIconKind.Laboratory, () => OpenScreen(laboratory, "Лаборатория"));
            var divider = new VisualElement(); divider.AddToClassList("menu-divider"); stack.Add(divider);
            AddMenuButton("НАСТРОЙКИ", MenuIconKind.Settings, Settings, compact: true);
            AddMenuButton("СПРАВКА / О ПРОГРАММЕ", MenuIconKind.Book, About, compact: true);
            AddMenuButton("ВЫХОД", MenuIconKind.Exit, ConfirmExit, compact: true);
            status = Text(stage, "", "status");
            Text(stage, "ДРОНЛАБ  /  " + version, "footer");
            stage.schedule.Execute(() => firstButton.Focus());
        }

        private Button AddMenuButton(string caption, MenuIconKind icon, Action action, bool primary = false, bool compact = false)
        {
            var button = new Button();
            button.AddToClassList("menu-button");
            if (primary) button.AddToClassList("primary");
            if (compact) button.AddToClassList("compact");
            var glyph = new DroneMenuIcon(icon); glyph.AddToClassList("menu-icon"); button.Add(glyph);
            var text = new Label(caption); MakeReadOnly(text); text.AddToClassList("menu-caption"); button.Add(text);
            var arrow = new DroneMenuIcon(MenuIconKind.Chevron); arrow.AddToClassList("menu-arrow"); button.Add(arrow);
            button.clicked += () => { returnFocus = button; action(); };
            stack.Add(button); return button;
        }

        private static Label Text(VisualElement parent, string text, string className)
        {
            var label = new Label(text); MakeReadOnly(label); label.AddToClassList(className); parent.Add(label); return label;
        }

        private static void MakeReadOnly(Label label)
        {
            label.selection.isSelectable = false;
            label.focusable = false;
            label.pickingMode = PickingMode.Ignore;
        }
        private static DroneDropdown Dropdown(VisualElement parent, string title, List<string> choices, int index)
        {
            var field = new DroneDropdown(title, DroneScenarioCatalog.DisplayChoices(choices), index);
            field.AddToClassList("settings-control");
            field.Query<Label>().ForEach(MakeReadOnly);
            parent.Add(field);
            return field;
        }
        private static ScrollView ModalScroll(VisualElement card)
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("modal-scroll");
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScroller.AddToClassList("graphite-scroller");
            scroll.verticalScroller.lowButton.style.display = DisplayStyle.None;
            scroll.verticalScroller.highButton.style.display = DisplayStyle.None;
            card.Add(scroll);
            return scroll;
        }

        public void SetBackground(Texture2D texture)
        {
            background = texture;
            if (stage == null) return;
            stage.style.backgroundImage = texture == null ? new StyleBackground(StyleKeyword.None) : new StyleBackground(texture);
            var fallback = stage.Q<VisualElement>("fallback-logo"); fallback?.RemoveFromHierarchy();
            if (texture == null)
            {
                fallback = new VisualElement { name = "fallback-logo" }; fallback.AddToClassList("fallback-logo");
                Text(fallback, "ДРОНЛАБ", "logo-title");
                Text(fallback, "ЛАБОРАТОРИЯ СИМУЛЯЦИИ БПЛА", "logo-subtitle");
                stage.Insert(0, fallback);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;
            if (dronesScreen != null) { dronesScreen.RequestClose(); evt.StopPropagation(); return; }
            if (scenarioScreen != null) { scenarioScreen.RequestClose(); evt.StopPropagation(); return; }
            if (modal != null) CloseModal(); else ConfirmExit();
            evt.StopPropagation();
        }
        private void Resize(GeometryChangedEvent evt)
        {
            float scale = Mathf.Min(evt.newRect.width / 1920f, evt.newRect.height / 1080f);
            stage.style.left = (evt.newRect.width - 1920f * scale) * .5f;
            stage.style.top = (evt.newRect.height - 1080f * scale) * .5f;
            stage.style.scale = new Scale(new Vector3(scale, scale, 1));
        }

        private DroneScenarioCatalog GetCatalog()
        {
            var controller = GetComponent<DroneScenarioController>();
            if (controller == null) controller = FindFirstObjectByType<DroneScenarioController>();
            return controller != null ? controller.Catalog : scenarioCatalog != null ? scenarioCatalog : DroneScenarioCatalog.Load();
        }
        public void StartFlight()
        {
            var catalog = GetCatalog();
            if (catalog != null && catalog.maps.Count > 0) { PrepareFlight(catalog); return; }
            LoadLegacyFlight();
        }
        private void LoadLegacyFlight()
        {
            if (loading) return;
            if (string.IsNullOrWhiteSpace(flightScene) || !Application.CanStreamedLevelBeLoaded(flightScene))
            { status.text = "Сцена полёта недоступна. Проверьте Flight Scene и список сцен сборки."; return; }
            loading = true; stack.SetEnabled(false); status.text = "Загрузка симуляции…";
            try
            {
                if (SceneManager.LoadSceneAsync(flightScene) != null) return;
            }
            catch (ArgumentException ex) { Debug.LogError(ex.Message, this); }
            loading = false; stack.SetEnabled(true); status.text = "Не удалось загрузить сцену.";
        }
        private void OpenScenarios()
        {
            CloseModal(); stack.SetEnabled(false);
            var catalog = GetCatalog();
            scenarioScreen = new DroneScenariosScreen(stage, catalog, () => { scenarioScreen = null; stack.SetEnabled(!loading); (returnFocus ?? firstButton)?.Focus(); });
        }
        private void OpenDrones()
        {
            CloseModal(); stack.SetEnabled(false);
            dronesScreen = new DroneDronesScreen(stage, () => { dronesScreen = null; stack.SetEnabled(!loading); (returnFocus ?? firstButton)?.Focus(); });
        }
        private void PrepareFlight(DroneScenarioCatalog catalog)
        {
            if (loading) return;
            var maps = catalog.maps.FindAll(m => m != null && !string.IsNullOrWhiteSpace(m.scenePath));
            var profiles = DroneEnvironmentProfiles.LoadAll(out _);
            if (maps.Count == 0 || profiles.Count == 0) { status.text = "Добавьте карту и профиль среды в каталог."; return; }
            var card = OpenModal("НОВАЯ СИМУЛЯЦИЯ", "launch-card");
            var map = Dropdown(card, "Карта", maps.ConvertAll(m => m.title), 0);
            var environment = Dropdown(card, "Профиль среды", profiles.ConvertAll(p => p.name), 0);
            var drones = DroneProfileLibrary.LoadAll(out _).Where(d => !d.draft).ToList();
            if (drones.Count == 0) { Text(card, "Сначала сохраните готовый профиль в каталоге дронов.", "muted-text"); return; }
            string selectedId = PlayerPrefs.GetString("DroneLab.SelectedDrone", "");
            var drone = Dropdown(card, "Дрон", drones.ConvertAll(d => d.Name), Mathf.Max(0,drones.FindIndex(d=>d.id == selectedId)));
            var error = Text(card, "Карта и профиль среды выбираются независимо.", "muted-text");
            var actions = Actions(card); ActionButton(actions, "НАЗАД", CloseModal);
            ActionButton(actions, "ЗАПУСТИТЬ", () => {
                try {
                    var operation = DroneScenarioLaunch.Load(maps[map.index], profiles[environment.index], catalog, drones[drone.index]);
                    if (operation == null) throw new InvalidOperationException("Не удалось начать загрузку карты.");
                    loading = true; card.SetEnabled(false); status.text = "Загрузка симуляции…";
                } catch (Exception ex) { error.text = ex.Message; }
            });
        }
        private void OpenScreen(UnityEvent handler, string title)
        {
            if (handler.GetPersistentEventCount() > 0) handler.Invoke();
            else status.text = title + ": экран находится в разработке.";
        }

        private VisualElement OpenModal(string title, string className)
        {
            modal?.RemoveFromHierarchy();
            stack.SetEnabled(false);
            modal = new VisualElement(); modal.AddToClassList("modal-overlay"); stage.Add(modal);
            var card = new VisualElement(); card.AddToClassList("modal-card"); card.AddToClassList(className); modal.Add(card);
            Text(card, title, "modal-title");
            return card;
        }
        private void CloseModal()
        {
            modal?.RemoveFromHierarchy(); modal = null;
            stack.SetEnabled(!loading); (returnFocus ?? firstButton)?.Focus();
        }
        private static Button ActionButton(VisualElement parent, string text, Action action)
        {
            var b = new Button(action) { text = text }; b.AddToClassList("action-button");
            if (parent.childCount > 0) b.AddToClassList("spaced-action");
            parent.Add(b); return b;
        }
        private static VisualElement Actions(VisualElement card)
        {
            var row = new VisualElement(); row.AddToClassList("actions-row"); card.Add(row); return row;
        }
        private static void InitialFocus(Button button) => button.schedule.Execute(() => button.Focus());

        private void Settings()
        {
            var draft = DroneApplicationSettings.Current.Copy();
            var card = OpenModal("НАСТРОЙКИ", "settings-card");
            var scroll = ModalScroll(card);
            Text(scroll, "ЗВУК", "section-title");
            VolumeSlider(scroll, "Окружающая среда", draft.environmentVolume, value => draft.environmentVolume = value);
            VolumeSlider(scroll, "Дрон / лопасти", draft.droneVolume, value => draft.droneVolume = value);
            Text(scroll, "ИЗОБРАЖЕНИЕ", "section-title");
            var fullscreen = Dropdown(scroll, "Полный экран", new List<string> { "Включено", "Выключено" }, draft.fullscreen ? 0 : 1);
            var names = new List<string>();
            foreach (var name in QualitySettings.names)
                names.Add(name == "High Fidelity" ? "Высокое качество" : name == "Balanced" ? "Сбалансированное" : name == "Performant" ? "Производительность" : name);
            var quality = Dropdown(scroll, "Настройки качества", names, draft.quality);
            var vsync = Dropdown(scroll, "Вертикальная синхронизация", new List<string> { "Включено", "Выключено" }, draft.vSync ? 0 : 1);
            Text(scroll, "УПРАВЛЕНИЕ", "section-title");
            var device = Dropdown(scroll, "Устройство", new List<string> { "Клавиатура", "Геймпад" }, draft.inputDevice == PilotDevice.Gamepad ? 1 : 0);
            var deviceHint = Text(scroll, "", "muted-text");
            void DeviceHint()
            {
                deviceHint.text = device.index == 1
                    ? (Gamepad.current == null ? "Геймпад не подключён. Подключите его перед полётом." : "Геймпад: " + Gamepad.current.displayName)
                    : "W/S — тангаж, A/D — крен, Q/E — рыскание. F — запуск двигателей.";
            }
            device.RegisterValueChangedCallback(evt => DeviceHint()); DeviceHint();
            deviceHint.schedule.Execute(DeviceHint).Every(1000);
            var row = Actions(card);
            var back = ActionButton(row, "НАЗАД", CloseModal);
            ActionButton(row, "ПРИМЕНИТЬ", () =>
            {
                draft.fullscreen = fullscreen.index == 0; draft.quality = quality.index; draft.vSync = vsync.index == 0;
                draft.inputDevice = device.index == 1 ? PilotDevice.Gamepad : PilotDevice.Keyboard;
                DroneApplicationSettings.Save(draft); CloseModal(); status.text = "Настройки сохранены.";
            });
            InitialFocus(back);
        }
        private static void VolumeSlider(VisualElement parent, string title, float initial, Action<float> changed)
        {
            var row = new VisualElement(); row.AddToClassList("volume-row"); parent.Add(row);
            var slider = new Slider(title, 0, 1) { value = initial }; slider.AddToClassList("volume-slider"); MakeReadOnly(slider.labelElement); row.Add(slider);
            var number = Text(row, Mathf.RoundToInt(initial * 100) + "%", "volume-value");
            slider.RegisterValueChangedCallback(evt => { number.text = Mathf.RoundToInt(evt.newValue * 100) + "%"; changed(evt.newValue); });
        }

        private void About()
        {
            var card = OpenModal("СПРАВКА / О ПРОГРАММЕ", "about-card");
            var scroll = ModalScroll(card);
            Text(scroll, "ДРОНЛАБ", "about-logo");
            Text(scroll, "Лаборатория симуляции БПЛА", "about-subtitle");
            Text(scroll, "Дронлаб — платформа для демонстрации возможностей беспилотной техники потенциальным покупателям и предварительных инженерных испытаний. Виртуальные полёты позволяют исследовать поведение аппарата без риска потери дорогостоящего оборудования и сокращают число реальных испытательных полётов.", "body-text");
            Text(scroll, "ВОЗМОЖНОСТИ ПЛАТФОРМЫ", "section-title");
            Text(scroll, "• Наглядная демонстрация техники: реалистичная графика и кинематографический режим для рекламных материалов.\n• Инженерные испытания: имитация физических свойств и явлений для оценки поведения и качества полёта.\n• Наблюдение за состоянием аппарата: телеметрия и мониторинг датчиков.\n• Подготовка данных: запись полётов и формирование датасетов для анализа.", "body-text");
            Text(scroll, "РАЗРАБОТЧИКИ", "section-title");
            if (developerLinks != null) foreach (var developer in developerLinks)
            {
                if (developer == null) continue;
                if (TryLink(developer.url, out var uri))
                {
                    string url = uri.AbsoluteUri;
                    var link = new Button(() => Application.OpenURL(url)) { text = developer.name }; link.AddToClassList("developer-link"); scroll.Add(link);
                }
                else Text(scroll, developer.name, "developer-name");
            }
            Text(scroll, "Версия " + version, "version-label");
            var row = Actions(card); InitialFocus(ActionButton(row, "НАЗАД", CloseModal));
        }
        private static bool TryLink(string url, out Uri uri)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        }

        private void ConfirmExit()
        {
            var card = OpenModal("Вы точно хотите выйти?", "exit-card");
            Text(card, "Работа приложения будет завершена.", "muted-text");
            var row = Actions(card);
            var back = ActionButton(row, "НАЗАД", CloseModal);
            var quit = ActionButton(row, "ВЫЙТИ", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            quit.AddToClassList("exit-action"); InitialFocus(back);
        }
        private void OnDisable()
        {
            dronesScreen?.Dispose(); dronesScreen = null;
            if (root != null)
            {
                root.UnregisterCallback<GeometryChangedEvent>(Resize);
                root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            }
            if (document != null) Destroy(document);
            if (panel != null) Destroy(panel);
            root = null; stage = null; modal = null; scenarioScreen = null;
        }
    }
}
