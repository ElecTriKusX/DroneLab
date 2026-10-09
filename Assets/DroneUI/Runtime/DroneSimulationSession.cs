using System;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>Flight overlay and pause/return controls; does not apply forces.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class DroneSimulationSession : MonoBehaviour
    {
        private DronePhysicsBody body;
        private DroneTestPilot pilot;
        private string menuScene, error;
        private UIDocument document;
        private PanelSettings panel;
        private VisualElement overlay;
        private Label menuStatus;
        private DroneFlightCamera cameraController;
        private DroneFlightHud hud;
        private DroneFlightTelemetry telemetry;
        private float nextHudUpdate;
        private bool previousAutomaticControl;
        private bool paused, returning;
        private float previousTimeScale;
        public void Configure(DronePhysicsBody selectedBody, DroneTestPilot selectedPilot, string map, string weather, string returnScene, string launchError = null, DroneFlightCamera flightCamera = null)
        {
            body = selectedBody; pilot = selectedPilot; menuScene = returnScene; error = launchError; cameraController = flightCamera;
            panel = ScriptableObject.CreateInstance<PanelSettings>(); panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080); panel.sortingOrder = 90;
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DroneLab/MainMenuTheme");
            document = gameObject.AddComponent<UIDocument>(); document.panelSettings = panel;
            var root = document.rootVisualElement; root.style.flexGrow = 1; root.pickingMode = PickingMode.Ignore;
            var sheet = Resources.Load<StyleSheet>("DroneLab/Scenarios"); if (sheet != null) root.styleSheets.Add(sheet);
            var flightSheet = Resources.Load<StyleSheet>("DroneLab/Flight"); if (flightSheet != null) root.styleSheets.Add(flightSheet);
            if (body != null && body.IsReady) {
                telemetry = new DroneFlightTelemetry(body);
                hud = new DroneFlightHud(root, cameraController, body.name, map, weather, SetView, ToggleCamera, () => SetPaused(true));
                telemetry.Sample(pilot); hud.Refresh(telemetry, pilot);
            }
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
            if (value) { previousTimeScale = Time.timeScale; Time.timeScale = 0; if (pilot != null) { previousAutomaticControl = pilot.AutomaticControl; pilot.AutomaticControl = false; pilot.SetFlightInput(default); } }
            else { Time.timeScale = previousTimeScale; if (pilot != null) pilot.AutomaticControl = previousAutomaticControl; }
            paused = value; overlay.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (cameraController != null) { cameraController.Paused = value; cameraController.PointerOverUI = false; }
        }
        private void SetView(DroneFlightViewMode view) { if (!paused && !returning) hud?.SetView(view); }
        private void ToggleCamera() { if (!paused && !returning) { cameraController?.ToggleMode(); nextHudUpdate = 0; } }
        private void Update()
        {
            if (document == null || returning) return;
            var keyboard = Application.isFocused ? Keyboard.current : null;
            if (keyboard?.escapeKey.wasPressedThisFrame ?? false) SetPaused(!paused);
            if (!paused && hud != null) {
                if (keyboard?.f1Key.wasPressedThisFrame ?? false) SetView(DroneFlightViewMode.Cinema);
                if (keyboard?.f2Key.wasPressedThisFrame ?? false) SetView(DroneFlightViewMode.Pilot);
                if (keyboard?.cKey.wasPressedThisFrame ?? false) ToggleCamera();
                if (keyboard?.backspaceKey.wasPressedThisFrame ?? false) cameraController?.SnapToTarget();
            }
            hud?.TickHint(paused);
            if (telemetry != null && Time.unscaledTime >= nextHudUpdate) {
                nextHudUpdate = Time.unscaledTime + .1f;
                telemetry.Sample(pilot);
                if (telemetry.ResetDetected) cameraController?.SnapToTarget();
                hud.Refresh(telemetry, pilot);
            }
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
