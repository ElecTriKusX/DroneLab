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
        private DroneFlightTelemetry telemetry, sensorTelemetry;
        private DroneFlightRoutePlanner route;
        private bool focusPaused;
        private DroneSensorRig sensors;
        private DroneFlightWorkbench workbench;
        private DroneFlightControls controls;
        private float nextHudUpdate;
        private bool previousAutomaticControl;
        private bool paused, returning;
        private float previousTimeScale;
        public void Configure(DronePhysicsBody selectedBody, DroneTestPilot selectedPilot, string map, string weather, string returnScene, string launchError = null, DroneFlightCamera flightCamera = null)
        {
            body = selectedBody; pilot = selectedPilot;
            if(pilot!=null) { pilot.DisarmOnFocusLoss=false; pilot.DisarmOnInputLoss=false; } menuScene = returnScene; error = launchError; cameraController = flightCamera;
            panel = ScriptableObject.CreateInstance<PanelSettings>(); panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080); panel.sortingOrder = 90;
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; panel.match = 1;
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DroneLab/MainMenuTheme");
            document = gameObject.AddComponent<UIDocument>(); document.panelSettings = panel;
            var root = document.rootVisualElement; root.AddToClassList("flight-ui"); root.style.flexGrow = 1; root.pickingMode = PickingMode.Ignore;
            var sheet = Resources.Load<StyleSheet>("DroneLab/Scenarios"); if (sheet != null) root.styleSheets.Add(sheet);
            var flightSheet = Resources.Load<StyleSheet>("DroneLab/Flight"); if (flightSheet != null) root.styleSheets.Add(flightSheet);
            if (body != null && body.IsReady) {
                telemetry = new DroneFlightTelemetry(body); sensorTelemetry=new DroneFlightTelemetry(body);
                sensors = body.gameObject.AddComponent<DroneSensorRig>(); sensors.Configure(body, cameraController?.Camera);
                if(cameraController!=null) cameraController.SensorRig=sensors;
                hud = new DroneFlightHud(root, cameraController, body.name, map, weather, SetView, ToggleCamera, OpenControls);
                workbench = new DroneFlightWorkbench(root, body, pilot, sensors, cameraController, SetView);
                if(pilot!=null) pilot.NavigationFeedback=sensors;
                if(cameraController!=null) route=new DroneFlightRoutePlanner(root,body,pilot,sensors,cameraController);
                telemetry.Sample(pilot); sensorTelemetry.ApplySensors(telemetry,sensors); hud.Refresh(sensorTelemetry, pilot);
                root.RegisterCallback<GeometryChangedEvent>(_ => root.EnableInClassList("flight-compact", root.contentRect.width < 1650));
            }
            overlay = new VisualElement(); overlay.AddToClassList("scenario-prompt"); root.Add(overlay);
            var card = new VisualElement(); card.AddToClassList("scenario-prompt-card"); overlay.Add(card);
            DroneProfileFields.Label(card, error == null ? "СИМУЛЯЦИЯ ПРИОСТАНОВЛЕНА" : "НЕ УДАЛОСЬ ЗАПУСТИТЬ СИМУЛЯЦИЮ", "scenario-panel-title");
            menuStatus = DroneProfileFields.Label(card, error ?? "Продолжите полёт или вернитесь к выбору дрона, сцены и погоды.", "scenario-hint");
            var row = new VisualElement(); row.AddToClassList("scenario-actions"); card.Add(row);
            if (error == null) AddButton(row, "Продолжить", () => SetPaused(false));
            AddButton(row, "В главное меню", ReturnToMenu);
            overlay.style.display = DisplayStyle.None;
            controls = new DroneFlightControls(root, pilot, cameraController, CloseControls);
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
            paused = value; overlay.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; if(value) overlay.BringToFront();
            if (cameraController != null) { cameraController.Paused = value; cameraController.PointerOverUI = false; }
        }
        private void SetView(DroneFlightViewMode view) { if (!paused && !returning) { hud?.SetView(view); workbench?.SetView(view); route?.SetView(view); } }
        private void OpenControls() { if (returning || error != null) return; SetPaused(true); overlay.style.display = DisplayStyle.None; controls?.SetOpen(true); }
        private void CloseControls() { controls?.SetOpen(false); SetPaused(false); }
        private void ToggleCamera() { if (!paused && !returning && sensors?.CameraAvailable==true) {
            if(hud?.View==DroneFlightViewMode.Route) SetView(DroneFlightViewMode.Pilot);
            cameraController?.ToggleMode(); nextHudUpdate = 0;
        } }
        private void OnApplicationFocus(bool focused)
        {
            if(document==null || returning) return;
            if(!focused && !paused) { focusPaused=true; SetPaused(true); }
            else if(focused && focusPaused) { focusPaused=false; SetPaused(false); }
        }
        private void FixedUpdate() { if(!paused && !returning) route?.FixedTick(Time.fixedDeltaTime); }
        private void Update()
        {
            if (document == null || returning) return;
            var keyboard = Application.isFocused ? Keyboard.current : null;
            if (keyboard?.escapeKey.wasPressedThisFrame ?? false) {
                var openDropdown = document.rootVisualElement.Query<DroneDropdown>().ToList().Find(field => field.IsOpen);
                if (openDropdown != null) openDropdown.ClosePopup();
                else if (controls?.IsOpen == true) { if (controls.IsCapturing) controls.CancelCapture(); else CloseControls(); }
                else if (workbench?.Editing == true || route?.Editing==true) document.rootVisualElement.focusController?.focusedElement?.Blur();
                else SetPaused(!paused);
            }
            controls?.Tick(keyboard);
            if (!paused && hud != null && workbench?.Editing != true && route?.Editing!=true) {
                if (DroneKeyBindings.Pressed(keyboard, FlightKeyAction.Cinema)) SetView(DroneFlightViewMode.Cinema);
                if (DroneKeyBindings.Pressed(keyboard, FlightKeyAction.Pilot)) SetView(DroneFlightViewMode.Pilot);
                if (DroneKeyBindings.Pressed(keyboard, FlightKeyAction.Engineer)) SetView(DroneFlightViewMode.Engineer);
                if (DroneKeyBindings.Pressed(keyboard, FlightKeyAction.Diagnostics)) SetView(DroneFlightViewMode.Diagnostics);
                if (DroneKeyBindings.Pressed(keyboard, FlightKeyAction.Route)) SetView(DroneFlightViewMode.Route);
                if (DroneKeyBindings.Pressed(keyboard, FlightKeyAction.PositionHold) && pilot!=null) { if(pilot.PositionHold) pilot.CancelPositionHold(); else pilot.HoldPosition(); }
                if (DroneKeyBindings.Pressed(keyboard, FlightKeyAction.Camera)) ToggleCamera();
                if (DroneKeyBindings.Pressed(keyboard, FlightKeyAction.Controls)) OpenControls();
                if (DroneKeyBindings.Pressed(keyboard, FlightKeyAction.Reset)) cameraController?.SnapToTarget();
            }
            route?.Tick(paused);
            if(sensors!=null && !sensors.CameraAvailable && cameraController?.Mode==DroneFlightCameraMode.Fpv) {
                cameraController.SetMode(DroneFlightCameraMode.Chase); sensors.Log("Камера отказала: FPV выключен");
            }
            hud?.TickHint(paused);
            if (telemetry != null && Time.unscaledTime >= nextHudUpdate) {
                nextHudUpdate = Time.unscaledTime + .1f;
                telemetry.Sample(pilot);
                if (telemetry.ResetDetected) { sensors?.ResetReadings(); cameraController?.SnapToTarget(); workbench?.ResetPanels(); route?.Reset(); }
                sensorTelemetry.ApplySensors(telemetry,sensors); hud.Refresh(sensorTelemetry, pilot); hud.RefreshSensors(sensors); workbench?.Refresh(telemetry);
            }
        }
        private void LateUpdate() { workbench?.RefreshSceneOverlay(); route?.RefreshOverlay(); }
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
            workbench?.Dispose();
            if (paused) Time.timeScale = previousTimeScale;
            if (panel != null) Destroy(panel);
        }
    }
}
