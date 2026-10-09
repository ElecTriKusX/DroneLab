using System;
using System.Collections.Generic;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal sealed class DroneFlightControls
    {
        private readonly VisualElement root;
        private readonly Label message;
        private readonly Dictionary<FlightKeyAction, Button> buttons = new Dictionary<FlightKeyAction, Button>();
        private FlightKeyAction? capturing;
        private float captureAfter;
        public bool IsOpen => root.style.display.value != DisplayStyle.None;
        public bool IsCapturing => capturing.HasValue;
        public DroneFlightControls(VisualElement parent, DroneTestPilot pilot, DroneFlightCamera camera, Action close)
        {
            root = new VisualElement(); root.AddToClassList("flight-modal"); parent.Add(root);
            var card = new VisualElement(); card.AddToClassList("flight-controls-card"); root.Add(card);
            Label(card, "УПРАВЛЕНИЕ", "flight-title");
            Label(card, "Выберите действие и нажмите новую клавишу. Esc — отменить назначение / закрыть меню. Полёт на паузе.", "flight-note");
            var columns = new VisualElement(); columns.AddToClassList("flight-controls-columns"); card.Add(columns);
            var scroll = new ScrollView(); ThemeScroll(scroll); scroll.AddToClassList("flight-key-list"); columns.Add(scroll);
            for (int i = 0; i < DroneKeyBindings.Count; i++) {
                var action = (FlightKeyAction)i;
                var row = new VisualElement(); row.AddToClassList("flight-key-row"); scroll.Add(row);
                Label(row, DroneKeyBindings.Name(action), "flight-key-name");
                var button = new Button(() => BeginCapture(action)) { focusable = false };
                button.AddToClassList("flight-key-button"); row.Add(button); buttons.Add(action, button);
            }
            var settings = new VisualElement(); settings.AddToClassList("flight-control-options"); columns.Add(settings);
            Label(settings, "УСТРОЙСТВО", "flight-subtitle");
            if (pilot != null) {
                var device = new DroneDropdown("Управлять дроном", new List<string> { "Клавиатура", "Геймпад" }, pilot.inputDevice == PilotDevice.Gamepad ? 1 : 0);
                device.RegisterValueChangedCallback(evt => pilot.inputDevice = evt.newValue == "Геймпад" ? PilotDevice.Gamepad : PilotDevice.Keyboard);
                settings.Add(device);
            }
            Label(settings, "Геймпад: Start — моторы; правый стик — наклон; левый — поворот / подъём; RT — тяга; X — режим; A — высота; Y — на старт.", "flight-note");
            Label(settings, "КАМЕРА", "flight-subtitle");
            Label(settings, "ПКМ — вращение · колесо — дистанция · СКМ — вернуть обзор. В FPV камера закреплена на корпусе.", "flight-note");
            if (camera != null) {
                Slider(settings, "Скорость вращения", .05f, .6f, camera.OrbitSensitivity, x => { camera.OrbitSensitivity = x; PlayerPrefs.SetFloat("DroneLab.CameraOrbit", x); });
                Slider(settings, "Скорость приближения", .08f, .55f, camera.ZoomSensitivity, x => { camera.ZoomSensitivity = x; PlayerPrefs.SetFloat("DroneLab.CameraZoom", x); });
                var resetCamera = new Button(camera.ResetOrbit) { text = "Вернуть камеру", focusable = false }; resetCamera.AddToClassList("pilot-button"); settings.Add(resetCamera);
            }
            message = Label(card, "", "flight-note");
            var footer = new VisualElement(); footer.AddToClassList("flight-controls-footer"); card.Add(footer);
            var reset = new Button(() => { capturing = null; DroneKeyBindings.RestoreDefaults(); Refresh(); message.text = "Стандартные клавиши восстановлены."; }) { text = "Сбросить клавиши", focusable = false };
            reset.AddToClassList("pilot-button"); footer.Add(reset);
            var done = new Button(close) { text = "Готово", focusable = false }; done.AddToClassList("pilot-button"); footer.Add(done);
            SetOpen(false);
        }
        private static Label Label(VisualElement parent, string caption, string css)
        { var label = new Label(caption); label.AddToClassList(css); parent.Add(label); return label; }
        internal static void ThemeScroll(ScrollView scroll)
        {
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.verticalScroller.AddToClassList("graphite-scroller");
            scroll.verticalScroller.lowButton.style.display = DisplayStyle.None;
            scroll.verticalScroller.highButton.style.display = DisplayStyle.None;
        }
        private static void Slider(VisualElement parent, string label, float min, float max, float value, Action<float> change)
        { var slider = new UnityEngine.UIElements.Slider(label, min, max) { value = value, showInputField = true }; slider.RegisterValueChangedCallback(evt => change(evt.newValue)); parent.Add(slider); }
        public void SetOpen(bool open) { capturing = null; root.style.display = open ? DisplayStyle.Flex : DisplayStyle.None; if (open) { message.text = "Настройки сохраняются автоматически."; Refresh(); } else PlayerPrefs.Save(); }
        private void Refresh() { foreach (var item in buttons) item.Value.text = DroneKeyBindings.Caption(item.Key); }
        private void BeginCapture(FlightKeyAction action)
        { capturing = action; captureAfter = Time.unscaledTime + .1f; Refresh(); buttons[action].text = "Нажмите…"; message.text = "Новое назначение: " + DroneKeyBindings.Name(action); }
        public void CancelCapture() { capturing = null; Refresh(); message.text = "Назначение отменено."; }
        public void Tick(Keyboard keyboard)
        {
            if (!IsOpen || !capturing.HasValue || keyboard == null || Time.unscaledTime < captureAfter) return;
            foreach (var key in keyboard.allKeys) if (key.wasPressedThisFrame && key.keyCode != Key.Escape) {
                if (DroneKeyBindings.TryAssign(capturing.Value, key.keyCode, out string result)) { capturing = null; Refresh(); }
                message.text = result; break;
            }
        }
    }
}
