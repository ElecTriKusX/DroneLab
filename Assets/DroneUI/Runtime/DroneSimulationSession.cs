using System;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>Flight overlay and pause/return controls; does not apply forces.</summary>
    public sealed class DroneSimulationSession : MonoBehaviour
    {
        private DronePhysicsBody body;
        private DroneTestPilot pilot;
        private string menuScene, error;
        private UIDocument document;
        private PanelSettings panel;
        private VisualElement overlay;
        private Label flightStatus, menuStatus;
        private bool paused, returning;
        private float previousTimeScale;
        public void Configure(DronePhysicsBody selectedBody, DroneTestPilot selectedPilot, string map, string weather, string returnScene, string launchError = null)
        {
            body = selectedBody; pilot = selectedPilot; menuScene = returnScene; error = launchError;
            panel = ScriptableObject.CreateInstance<PanelSettings>(); panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080); panel.sortingOrder = 90;
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DroneLab/MainMenuTheme");
            document = gameObject.AddComponent<UIDocument>(); document.panelSettings = panel;
            var root = document.rootVisualElement; root.style.flexGrow = 1; root.pickingMode = PickingMode.Ignore;
            var sheet = Resources.Load<StyleSheet>("DroneLab/Scenarios"); if (sheet != null) root.styleSheets.Add(sheet);
            var hud = new VisualElement(); hud.AddToClassList("flight-hud"); hud.pickingMode = PickingMode.Ignore; root.Add(hud);
            DroneProfileFields.Label(hud, (body != null ? body.name : "Симуляция") + "  /  " + map + "  /  " + weather, "flight-caption");
            flightStatus = DroneProfileFields.Label(hud, "", "flight-caption");
            var pause = new Button(() => SetPaused(true)) { text = "Пауза · Esc" }; pause.AddToClassList("scenario-button"); hud.Add(pause);
            var controls = DroneProfileFields.Label(root, "F — моторы  ·  W/S — тангаж  ·  A/D — крен  ·  Q/E — рыскание  ·  Space / Ctrl — тяга  ·  H — высота  ·  Z — стабилизация  ·  Backspace — на площадку", "flight-controls");
            controls.pickingMode = PickingMode.Ignore;
            overlay = new VisualElement(); overlay.AddToClassList("scenario-prompt"); root.Add(overlay);
            var card = new VisualElement(); card.AddToClassList("scenario-prompt-card"); overlay.Add(card);
            DroneProfileFields.Label(card, error == null ? "СИМУЛЯЦИЯ ПРИОСТАНОВЛЕНА" : "НЕ УДАЛОСЬ ЗАПУСТИТЬ СИМУЛЯЦИЮ", "scenario-panel-title");
            menuStatus = DroneProfileFields.Label(card, error ?? "Продолжите полёт или вернитесь к выбору дрона, сцены и погоды.", "scenario-hint");
            var row = new VisualElement(); row.AddToClassList("scenario-actions"); card.Add(row);
            if (error == null) AddButton(row, "Продолжить", () => SetPaused(false));
            AddButton(row, "В главное меню", ReturnToMenu);
            overlay.style.display = DisplayStyle.None;
            UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true;
            if (error != null) SetPaused(true);
        }
        private static void AddButton(VisualElement parent, string caption, Action action)
        { var button = new Button(action) { text = caption }; button.AddToClassList("scenario-button"); parent.Add(button); }
        private void SetPaused(bool value)
        {
            if (returning || paused == value || error != null && !value) return;
            if (value) { previousTimeScale = Time.timeScale; Time.timeScale = 0; if (pilot != null) { pilot.AutomaticControl = false; pilot.SetFlightInput(default); } }
            else { Time.timeScale = previousTimeScale; if (pilot != null) pilot.AutomaticControl = true; }
            paused = value; overlay.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private void Update()
        {
            if (document == null || returning) return;
            if (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) SetPaused(!paused);
            if (body != null && body.IsReady) flightStatus.text = (body.Armed ? "Моторы включены" : "Моторы выключены · F") +
                "  /  " + (pilot.autoLevel ? "Стабилизация" : "Ручной режим") + (pilot.altitudeHold ? "  /  Удержание высоты" : "");
        }
        private void ReturnToMenu()
        {
            if (returning) return;
            try {
                if (string.IsNullOrWhiteSpace(menuScene) || !Application.CanStreamedLevelBeLoaded(menuScene))
                    throw new InvalidOperationException("Сцена главного меню недоступна. Включите её в список сцен сборки.");
                var operation = SceneManager.LoadSceneAsync(menuScene);
                if (operation == null) throw new InvalidOperationException("Не удалось загрузить главное меню.");
                returning = true; menuStatus.text = "Возвращение в главное меню…";
                if (body != null) { body.SetArmed(false); body.AutomaticSimulation = false; }
                if (paused) { Time.timeScale = previousTimeScale; paused = false; }
            } catch (Exception ex) { menuStatus.text = ex.Message; }
        }
        private void OnDestroy()
        {
            if (paused) Time.timeScale = previousTimeScale;
            if (panel != null) Destroy(panel);
        }
    }
}
