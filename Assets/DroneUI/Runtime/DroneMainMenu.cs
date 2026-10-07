using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>Reference-aligned main menu. Does not change physics or flight input.</summary>
    [DisallowMultipleComponent]
    public sealed class DroneMainMenu : MonoBehaviour
    {
        [Tooltip("Full reference without central buttons, including the baked logo and drawings.")]
        [SerializeField] private Texture2D background;
        [Tooltip("Optional Cyrillic-capable font. Leave empty to use Unity's default font.")]
        [SerializeField] private Font menuFont;
        [SerializeField] private string flightScene = "PhysTest";
        [SerializeField, TextArea] private string developers = "Разработчики: Матвиенко, Якубовский, Николаев, Поляков";
        [SerializeField] private UnityEvent configureDrone = new UnityEvent();
        private UIDocument document;
        private PanelSettings panel;
        private VisualElement root, stage, modal;
        private Label status;
        private Button firstButton;
        private bool loading;

        private void OnEnable()
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.sortingOrder = 100;
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DroneLab/MainMenuTheme");
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            root = document.rootVisualElement;
            root.style.flexGrow = 1;
            root.style.backgroundColor = Color.black;
            var sheet = Resources.Load<StyleSheet>("DroneLab/MainMenu");
            if (sheet != null) root.styleSheets.Add(sheet);
            stage = new VisualElement { name = "reference-stage" };
            stage.AddToClassList("stage");
            if (menuFont != null) stage.style.unityFont = menuFont;
            root.Add(stage);
            root.RegisterCallback<GeometryChangedEvent>(Resize);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            SetBackground(background);
            var stack = new VisualElement();
            stack.AddToClassList("menu-stack");
            stage.Add(stack);
            firstButton = AddButton(stack, "НАЧАТЬ ИСПЫТАНИЕ", StartFlight, true);
            AddButton(stack, "КОНФИГУРАТОР ДРОНА", ConfigureDrone);
            AddButton(stack, "НАСТРОЙКИ", Settings);
            AddButton(stack, "О ПРОЕКТЕ", About);
            AddButton(stack, "ВЫХОД", ConfirmExit);
            status = new Label();
            status.AddToClassList("status");
            stage.Add(status);
            var footer = new Label("DRONELAB  /  " + Application.version);
            footer.AddToClassList("footer");
            stage.Add(footer);
            stage.schedule.Execute(() => firstButton.Focus());
        }

        /// <summary>Can also be called by a future theme/background selector.</summary>
        public void SetBackground(Texture2D texture)
        {
            background = texture;
            if (stage == null) return;
            stage.style.backgroundImage = texture == null ? new StyleBackground(StyleKeyword.None) : new StyleBackground(texture);
            var fallback = stage.Q<VisualElement>("fallback-logo");
            if (fallback != null) fallback.RemoveFromHierarchy();
            if (texture == null)
            {
                fallback = new VisualElement { name = "fallback-logo" };
                fallback.AddToClassList("fallback-logo");
                var title = new Label("DRONELAB");
                title.AddToClassList("logo-title");
                fallback.Add(title);
                fallback.Add(new Label("U A V   S I M U L A T O R   L A B O R A T O R Y"));
                stage.Insert(0, fallback);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape) return;
            if (modal != null) CloseModal();
            else ConfirmExit();
            evt.StopPropagation();
        }

        private void Resize(GeometryChangedEvent evt)
        {
            float scale = Mathf.Min(evt.newRect.width / 1920f, evt.newRect.height / 1080f);
            stage.style.left = (evt.newRect.width - 1920f * scale) * .5f;
            stage.style.top = (evt.newRect.height - 1080f * scale) * .5f;
            stage.style.scale = new Scale(new Vector3(scale, scale, 1));
        }

        private static Button AddButton(VisualElement parent, string caption, System.Action action, bool primary = false)
        {
            var button = new Button(action) { text = caption };
            button.AddToClassList("menu-button");
            if (primary) button.AddToClassList("primary");
            parent.Add(button);
            return button;
        }

        public void StartFlight()
        {
            if (loading) return;
            if (string.IsNullOrWhiteSpace(flightScene) || !Application.CanStreamedLevelBeLoaded(flightScene))
            {
                status.text = "Добавьте сцену полёта в Build Profiles → Scene List и укажите Flight Scene.";
                return;
            }
            loading = true;
            status.text = "Загрузка испытания…";
            var operation = SceneManager.LoadSceneAsync(flightScene);
            if (operation == null) { loading = false; status.text = "Не удалось загрузить сцену."; }
        }

        private void ConfigureDrone()
        {
            if (configureDrone.GetPersistentEventCount() > 0) configureDrone.Invoke();
            else status.text = "Конфигуратор ещё не подключён. Назначьте обработчик Configure Drone в Inspector.";
        }

        private VisualElement OpenModal(string title)
        {
            CloseModal();
            modal = new VisualElement();
            modal.AddToClassList("modal-overlay");
            stage.Add(modal);
            var card = new VisualElement();
            card.AddToClassList("modal-card");
            modal.Add(card);
            var heading = new Label(title);
            heading.AddToClassList("modal-title");
            card.Add(heading);
            return card;
        }

        private void CloseModal()
        {
            if (modal != null) modal.RemoveFromHierarchy();
            modal = null;
            firstButton?.Focus();
        }

        private void Settings()
        {
            var card = OpenModal("НАСТРОЙКИ");
            var fullscreen = new Toggle("Полный экран") { value = Screen.fullScreen };
            card.Add(fullscreen);
            var volume = new Slider("Громкость", 0, 1) { value = AudioListener.volume };
            card.Add(volume);
            var names = new System.Collections.Generic.List<string>(QualitySettings.names);
            var quality = new DropdownField("Качество", names, QualitySettings.GetQualityLevel());
            card.Add(quality);
            AddButton(card, "ПРИМЕНИТЬ", () =>
            {
                Screen.fullScreen = fullscreen.value;
                AudioListener.volume = volume.value;
                QualitySettings.SetQualityLevel(quality.index, true);
                PlayerPrefs.SetFloat("DroneLab.Volume", volume.value);
                PlayerPrefs.Save();
                CloseModal();
            });
            AddButton(card, "НАЗАД", CloseModal);
        }

        private void About()
        {
            var card = OpenModal("О ПРОЕКТЕ");
            var body = new Label("DRONELAB\nUAV SIMULATOR LABORATORY\n\n" + developers + "\n\nВерсия " + Application.version);
            body.style.whiteSpace = WhiteSpace.Normal;
            card.Add(body);
            AddButton(card, "НАЗАД", CloseModal);
        }

        private void ConfirmExit()
        {
            var card = OpenModal("ЗАВЕРШИТЬ РАБОТУ?");
            AddButton(card, "ВЫЙТИ", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
            AddButton(card, "НАЗАД", CloseModal);
        }

        private void Start() => AudioListener.volume = PlayerPrefs.GetFloat("DroneLab.Volume", AudioListener.volume);

        private void OnDisable()
        {
            if (root != null) root.UnregisterCallback<GeometryChangedEvent>(Resize);
            if (document != null) Destroy(document);
            if (panel != null) Destroy(panel);
            root = null;
            stage = null;
        }
    }
}
