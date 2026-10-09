using System;
using System.Globalization;
using DroneLab.Simulation;
using DroneLab.Sensors;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    public enum DroneFlightViewMode { Cinema, Pilot, Engineer, Diagnostics }

    /// <summary>Compact pilot HUD and cinema visibility. Reads a single telemetry snapshot.</summary>
    internal sealed class DroneFlightHud
    {
        private readonly VisualElement pilotRoot, pilotInstruments, cinemaHint, modePicker, fpvRoot, charge, mapPanel, mapBody;
        private readonly Label mode, battery, temperature, time, ground, vertical, home, attitude, wind, warning, cameraCaption, sensorStatus;
        private readonly Button modeButton, cameraButton, mapToggle;
        private bool mapCollapsed;
        private readonly DroneFlightInstrument compass, horizon, altitude, speed, map, fpv;
        private readonly DroneFlightCamera camera;
        private readonly Action<DroneFlightViewMode> changeView;
        private float hintUntil;
        public DroneFlightViewMode View { get; private set; } = DroneFlightViewMode.Pilot;
        public DroneFlightHud(VisualElement parent, DroneFlightCamera flightCamera, string drone, string scene, string weather,
            Action<DroneFlightViewMode> onView, Action onCamera, Action onControls)
        {
            camera = flightCamera; changeView = onView;
            pilotRoot = Element(parent, "pilot-hud");
            var branding = Element(pilotRoot, "pilot-branding");
            Text(branding, "ДРОНЛАБ", "pilot-brand");
            Text(branding, drone + "  /  " + scene + "  /  " + weather, "pilot-caption");
            compass = Instrument(pilotRoot, FlightInstrumentKind.Heading, "pilot-compass");
            var status = Element(pilotRoot, "pilot-status");
            mode = Text(status, "ANGLE", "pilot-status-cell");
            var power = Element(status, "pilot-power");
            battery = Text(power, "БАТАРЕЯ", "pilot-status-cell");
            var track = Element(power, "pilot-charge-track"); charge = Element(track, "pilot-charge-fill");
            temperature = Text(status, "", "pilot-status-cell");
            time = Text(status, "00:00", "pilot-status-cell");
            pilotInstruments = Element(pilotRoot, "pilot-instruments");
            var tapes = Element(pilotInstruments, "pilot-tapes");
            Text(tapes, "ВЫСОТА ОТ СТАРТА · м", "pilot-tape-caption");
            altitude = Instrument(tapes, FlightInstrumentKind.Altitude, "pilot-tape");
            Text(tapes, "СКОРОСТЬ ПО ЗЕМЛЕ · м/с", "pilot-tape-caption");
            speed = Instrument(tapes, FlightInstrumentKind.Speed, "pilot-tape");
            ground = Text(tapes, "", "pilot-surface");
            horizon = Instrument(pilotInstruments, FlightInstrumentKind.Horizon, "pilot-horizon");
            attitude = Text(pilotInstruments, "", "pilot-attitude");
            fpvRoot = Element(pilotInstruments, "pilot-fpv");
            fpv = Instrument(fpvRoot, FlightInstrumentKind.Fpv, "pilot-fpv-ladder");
            mapPanel = Element(pilotInstruments, "pilot-map-panel");
            var mapHeader = Element(mapPanel, "pilot-map-header");
            Text(mapHeader, "МАРШРУТ ПОЛЁТА", "pilot-map-title");
            mapToggle = Button(mapHeader, "-", () => SetMapCollapsed(!mapCollapsed)); mapToggle.AddToClassList("pilot-map-toggle");
            mapToggle.tooltip = "Свернуть карту";
            mapBody = Element(mapPanel, "pilot-map-body");
            map = Instrument(mapBody, FlightInstrumentKind.Map, "pilot-map");
            home = Text(mapBody, "ДО СТАРТА 0 м", "pilot-map-home");
            sensorStatus = Text(mapBody, "", "pilot-sensor-status");
            vertical = Text(pilotInstruments, "", "pilot-climb");
            wind = Text(pilotInstruments, "", "pilot-wind");
            warning = Text(pilotRoot, "", "pilot-warning");
            var navigation = Element(pilotRoot, "pilot-navigation");
            modeButton = Button(navigation, "F2  Пилот ▴", () => modePicker.style.display = modePicker.style.display.value == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None);
            cameraButton = Button(navigation, "C  FPV", onCamera);
            Button(navigation, "Управление", onControls);
            modePicker = Element(pilotRoot, "flight-mode-picker"); modePicker.style.display = DisplayStyle.None;
            for (int i = 0; i < 4; i++) {
                var view = (DroneFlightViewMode)i;
                Button(modePicker, ViewCaption(view), () => changeView(view));
            }
            cameraCaption = Text(pilotRoot, "", "pilot-camera-caption");
            cinemaHint = Element(parent, "cinema-hint");
            Text(cinemaHint, "F1 КИНО · F2 ПИЛОТ · F3 ИНЖЕНЕР · F4 ДИАГНОСТИКА · C КАМЕРА · ESC ПАУЗА", "cinema-hint-text");
            SetView(DroneFlightViewMode.Pilot);
        }
        private static VisualElement Element(VisualElement parent, string css)
        {
            var element = new VisualElement(); element.AddToClassList(css); element.pickingMode = PickingMode.Ignore;
            parent.Add(element); return element;
        }
        private static Label Text(VisualElement parent, string text, string css)
        {
            var label = new Label(text); label.AddToClassList(css); label.pickingMode = PickingMode.Ignore;
            parent.Add(label); return label;
        }
        private static DroneFlightInstrument Instrument(VisualElement parent, FlightInstrumentKind kind, string css)
        { var instrument = new DroneFlightInstrument(kind); instrument.AddToClassList(css); parent.Add(instrument); return instrument; }
        private Button Button(VisualElement parent, string text, Action click)
        {
            // Space is a flight axis; clicking a HUD button must not leave it armed for keyboard activation.
            var button = new Button(click) { text = text, focusable = false }; button.AddToClassList("pilot-button");
            button.RegisterCallback<PointerEnterEvent>(_ => { if (camera != null) camera.PointerOverUI = true; });
            button.RegisterCallback<PointerLeaveEvent>(_ => { if (camera != null) camera.PointerOverUI = false; });
            parent.Add(button); return button;
        }
        public void SetView(DroneFlightViewMode view)
        {
            View = view; pilotRoot.style.display = view != DroneFlightViewMode.Cinema ? DisplayStyle.Flex : DisplayStyle.None;
            pilotInstruments.style.display = view == DroneFlightViewMode.Pilot ? DisplayStyle.Flex : DisplayStyle.None;
            modeButton.text = ViewCaption(view) + " ▴";
            modePicker.style.display = DisplayStyle.None;
            if (camera != null) camera.PointerOverUI = false;
            hintUntil = Time.unscaledTime + 2.5f;
            cinemaHint.style.display = view == DroneFlightViewMode.Cinema ? DisplayStyle.Flex : DisplayStyle.None;
        }
        public void TickHint(bool paused)
        { cinemaHint.style.display = View == DroneFlightViewMode.Cinema && !paused && Time.unscaledTime < hintUntil ? DisplayStyle.Flex : DisplayStyle.None; }
        internal void SetMapCollapsed(bool collapsed)
        {
            mapCollapsed = collapsed;
            mapBody.style.display = collapsed ? DisplayStyle.None : DisplayStyle.Flex;
            mapPanel.EnableInClassList("collapsed", collapsed);
            mapToggle.text = collapsed ? "+" : "-";
            mapToggle.tooltip = collapsed ? "Развернуть карту" : "Свернуть карту";
        }
        public void Refresh(DroneFlightTelemetry data, DroneTestPilot pilot)
        {
            bool isFpv = camera != null && camera.Mode == DroneFlightCameraMode.Fpv;
            fpvRoot.style.display = isFpv ? DisplayStyle.Flex : DisplayStyle.None;
            horizon.style.display = isFpv ? DisplayStyle.None : DisplayStyle.Flex;
            string control = pilot == null ? "НЕТ УПРАВЛЕНИЯ" : pilot.autoLevel ? "ANGLE" : "РУЧНОЙ";
            mode.text = control + (pilot != null && pilot.altitudeHold ? " · ВЫСОТА" : "");
            mode.tooltip = "Удержание высоты управляет только вертикальным движением; положение X/Z не удерживается.";
            battery.text = data.BatteryPercent.HasValue ? "БАТАРЕЯ " + Number(data.BatteryPercent.Value, "0") + "%  ·  " + Number(data.Voltage ?? 0, "0.0") + " В" : "БАТАРЕЯ НЕ ЗАДАНА";
            charge.style.width = Length.Percent((float)Math.Max(0, Math.Min(100, data.BatteryPercent ?? 0)));
            temperature.style.display = data.MotorTemperatureC.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
            temperature.text = "МОТОР " + Number(data.MotorTemperatureC ?? 0, "0") + " °C";
            temperature.tooltip = "Максимальная расчётная температура двигателей из тепловой модели профиля.";
            var elapsed = TimeSpan.FromSeconds(Math.Max(0, data.TimeS));
            time.text = (elapsed.TotalHours >= 1 ? elapsed.ToString(@"hh\:mm\:ss") : elapsed.ToString(@"mm\:ss"));
            time.tooltip = "Время симуляции. При паузе останавливается, при возврате на старт сбрасывается.";
            ground.text = data.SurfaceDistance.HasValue ? "ДО ПОВЕРХНОСТИ " + Number(data.SurfaceDistance.Value, "0.0") + " м" : "ДО ПОВЕРХНОСТИ —";
            ground.tooltip = "Истинное вертикальное расстояние от центра корпуса до первого внешнего Collider. Это не измерение дальномера.";
            vertical.text = "ВЕРТИКАЛЬНАЯ " + data.VerticalSpeed.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " м/с";
            home.text = "ДО СТАРТА " + Number(data.HomeDistance, "0") + " м";
            attitude.text = "КРЕН " + Signed(data.Roll) + "°  ·  ТАНГАЖ " + Signed(data.Pitch) + "°";
            float windSpeed = data.Wind.magnitude;
            float flowHeading = DroneFlightMath.Heading(data.Wind);
            wind.text = "ВЕТЕР " + Number(windSpeed, "0.0") + " м/с" + (windSpeed > .05f ? "  ·  НАПРАВЛЕНИЕ " + Number(flowHeading, "000") + "°" : "");
            wind.tooltip = "Фактическая скорость воздушного потока у дрона. Направление показывает, куда дует ветер.";
            cameraCaption.text = isFpv ? "FPV · 90°" : "КАМЕРА ПРЕСЛЕДОВАНИЯ";
            cameraButton.text = isFpv ? DroneKeyBindings.Caption(FlightKeyAction.Camera) + "  Третье лицо" : DroneKeyBindings.Caption(FlightKeyAction.Camera) + "  FPV";
            string message = data.MotorFault ? "ОТКАЗ ДВИГАТЕЛЯ" : data.PowerLimited ? "ОГРАНИЧЕНИЕ МОЩНОСТИ" :
                data.BatteryPercent.HasValue && data.BatteryPercent.Value <= 20 ? "НИЗКИЙ ЗАРЯД БАТАРЕИ" : !data.Armed ? "МОТОРЫ ВЫКЛЮЧЕНЫ · " + DroneKeyBindings.Caption(FlightKeyAction.Arm) : "";
            warning.text = message; warning.style.display = message.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            warning.EnableInClassList("critical", data.MotorFault || data.PowerLimited || data.BatteryPercent <= 20);
            modeButton.text = ViewCaption(View) + " ▴";
            compass.SetData(data); altitude.SetData(data); speed.SetData(data); horizon.SetData(data); map.SetData(data); fpv.SetData(data);
        }
        private static string ViewCaption(DroneFlightViewMode view)
        {
            var actions = new[] { FlightKeyAction.Cinema, FlightKeyAction.Pilot, FlightKeyAction.Engineer, FlightKeyAction.Diagnostics };
            var names = new[] { "Кино", "Пилот", "Инженер", "Диагностика" };
            return DroneKeyBindings.Caption(actions[(int)view]) + "  " + names[(int)view];
        }
        public void RefreshSensors(DroneSensorRig sensors)
        {
            var gps = sensors.Channel(SensorKind.Gps); var range = sensors.Channel(SensorKind.Rangefinder);
            string fix = !gps.Settings.enabled ? "выкл." : gps.Faulted ? "потеря данных" : gps.Latest?.Valid == true ? "доступен" : "ожидание";
            string distance = range.Latest?.Valid == true ? Number(range.Latest.Value.Value.X, "0.00") + " м" : "нет измерения";
            sensorStatus.text = "GPS " + fix + " · ДАЛЬНОМЕР " + distance;
        }
        private static string Number(double value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
        private static string Signed(float value) => value.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture);
    }
}
