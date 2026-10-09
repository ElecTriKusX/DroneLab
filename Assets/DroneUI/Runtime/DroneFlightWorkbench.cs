using System;
using System.Collections.Generic;
using System.Globalization;
using DroneLab.Physics;
using DroneLab.Sensors;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>F3 live engineering controls and F4 diagnostics. Never applies forces itself.</summary>
    internal sealed class DroneFlightWorkbench : IDisposable
    {
        private readonly DronePhysicsBody body;
        private readonly DroneTestPilot pilot;
        private readonly DroneSensorRig rig;
        private readonly DroneFlightCamera camera;
        private readonly VisualElement root, panel, graphCard;
        private readonly ScrollView content;
        private readonly Label title, purpose, graphTitle;
        private readonly Action<DroneFlightViewMode> changeView;
        private readonly DroneDropdown graphSensor;
        private readonly Dictionary<string, Label> values = new Dictionary<string, Label>();
        private readonly DroneSensorPlot graph;
        private readonly DroneFlightSceneOverlay sceneOverlay;
        private readonly DroneFlightRecording recording;
        private Image cameraImage;
        private string tab = "Оборудование", engineerTab = "Оборудование", diagnosticsTab = "Показания";
        private SensorKind selected = SensorKind.Barometer;
        private DroneFlightViewMode view;
        private bool previousArmed, previousLimited;
        private bool ownsKeyboard, previousReadKeyboard, editRequested, inputSuspended;
        private readonly VisualElement stage;
        private UniformWind wind;
        private double windSpeed, windFrom;
        private Label recordStatus;
        private Button recordButton;
        private UnityEngine.UIElements.Toggle angleToggle, heightToggle;
        private sealed class UniformWind : IWindProvider
        {
            public DVector3 Velocity;
            public DVector3 Sample(DVector3 position, double time) => Velocity;
        }
        public DroneFlightWorkbench(VisualElement parent, DronePhysicsBody selectedBody, DroneTestPilot selectedPilot, DroneSensorRig sensors, DroneFlightCamera flightCamera, Action<DroneFlightViewMode> onView)
        {
            stage=parent; parent.focusable=true; parent.tabIndex=-1;
            body = selectedBody; pilot = selectedPilot; rig = sensors; camera = flightCamera; changeView = onView;
            recording = new DroneFlightRecording(body, pilot, rig);
            sceneOverlay = new DroneFlightSceneOverlay(body, rig, camera); parent.Add(sceneOverlay);
            root = Element(parent, "flight-workbench"); root.pickingMode=PickingMode.Position; panel = Element(root, "flight-workbench-panel");
            panel.pickingMode = PickingMode.Position;
            panel.RegisterCallback<PointerEnterEvent>(_ => { if (camera != null) camera.PointerOverUI = true; });
            panel.RegisterCallback<PointerLeaveEvent>(_ => { if (camera != null) camera.PointerOverUI = false; });
            // UI navigation maps WASD/Space too: only an explicit pointer selection may grant text input.
            parent.RegisterCallback<PointerDownEvent>(evt => {
                if(!InputGateActive || evt.button != 0) return;
                editRequested=ContainsEditor(evt.target as VisualElement);
                if(editRequested) AcquireKeyboard(); else ClearInputFocus();
            },TrickleDown.TrickleDown);
            parent.RegisterCallback<FocusInEvent>(evt => {
                if(!InputGateActive) return;
                if((editRequested || DropdownOpen) && ContainsEditor(evt.target as VisualElement)) AcquireKeyboard();
                else if(!Editing) ReleaseKeyboard();
            },TrickleDown.TrickleDown);
            parent.RegisterCallback<FocusOutEvent>(evt => {
                if(editRequested && !ContainsEditor(evt.relatedTarget as VisualElement) && !DropdownOpen) { editRequested=false; ReleaseKeyboard(); }
            },TrickleDown.TrickleDown);
            parent.RegisterCallback<KeyDownEvent>(BlockFlightNavigation,TrickleDown.TrickleDown);
            parent.RegisterCallback<NavigationMoveEvent>(BlockFlightNavigation,TrickleDown.TrickleDown);
            parent.RegisterCallback<NavigationSubmitEvent>(BlockFlightNavigation,TrickleDown.TrickleDown);
            parent.RegisterCallback<NavigationCancelEvent>(BlockFlightNavigation,TrickleDown.TrickleDown);
            title = Text(panel, "", "flight-title");
            purpose = Text(panel, "", "flight-note"); purpose.AddToClassList("flight-purpose");
            var tabs = Element(panel, "flight-workbench-tabs");
            foreach (var caption in new[] { "Среда", "Оборудование", "Контроллер", "Отказы", "Показания", "Движение", "Силы", "Моторы", "События" }) {
                string name = caption;
                var button = Button(tabs, caption, () => { tab = name; if (view == DroneFlightViewMode.Engineer) engineerTab = name; else diagnosticsTab = name; Build(); }); button.name = "tab-" + caption;
            }
            content = new ScrollView(); DroneFlightControls.ThemeScroll(content); content.AddToClassList("flight-workbench-content"); panel.Add(content);
            graphCard = Element(root, "flight-graph-card");
            graphTitle = Text(graphCard, "ГРАФИК ДАТЧИКА", "flight-subtitle");
            graphSensor = new DroneDropdown("Датчик", SensorNames(), (int)selected); graphSensor.AddToClassList("sensor-selector"); graphCard.Add(graphSensor);
            graphSensor.RegisterValueChangedCallback(evt => SelectSensor((SensorKind)graphSensor.index));
            graphCard.RegisterCallback<PointerEnterEvent>(_ => { if (camera != null) camera.PointerOverUI = true; });
            graphCard.RegisterCallback<PointerLeaveEvent>(_ => { if (camera != null) camera.PointerOverUI = false; });
            graph = new DroneSensorPlot(); graphCard.Add(graph); graph.SetSource(rig, selected);
            SetView(DroneFlightViewMode.Pilot);
        }
        private bool InputGateActive => !inputSuspended && root.style.display.value!=DisplayStyle.None;
        private bool DropdownOpen => root.Query<DroneDropdown>().ToList().Exists(field=>field.IsOpen);
        private bool ContainsEditor(VisualElement element)
        {
            if(element==null) return false;
            // Open popup options live at stage level; closed dropdowns do not own the keyboard.
            for(var ancestor=element;ancestor!=null;ancestor=ancestor.parent)
                if(ancestor.ClassListContains("drone-dropdown-overlay")) return DropdownOpen;
            if(!panel.Contains(element) && !graphCard.Contains(element)) return false;
            for(var ancestor=element;ancestor!=null;ancestor=ancestor.parent)
                if(ancestor is DoubleField || ancestor is FloatField || ancestor is IntegerField || ancestor is TextField || ancestor is DroneDropdown) return true;
            return false;
        }
        private void BlockFlightNavigation<T>(T evt) where T : EventBase<T>, new()
        {
            if(!InputGateActive || Editing) return;
            stage.panel?.focusController?.IgnoreEvent(evt);
            evt.StopImmediatePropagation();
        }
        private void AcquireKeyboard()
        {
            if(pilot==null) return;
            if(!ownsKeyboard) previousReadKeyboard=pilot.readKeyboard;
            ownsKeyboard=true; pilot.readKeyboard=false; pilot.SetFlightInput(default);
        }
        public void ClearInputFocus()
        {
            editRequested=false;
            var focused=stage.panel?.focusController?.focusedElement as VisualElement;
            if(focused!=null && (panel.Contains(focused) || graphCard.Contains(focused))) focused.Blur();
            ReleaseKeyboard();
            if(InputGateActive) stage.Focus();
        }
        public void SetInputSuspended(bool value) { inputSuspended=value; ClearInputFocus(); }
        public void TickInputFocus()
        {
            if(!InputGateActive) return;
            if(Editing) AcquireKeyboard();
            else if(ownsKeyboard) { editRequested=false; ReleaseKeyboard(); }
        }
        public bool Editing => InputGateActive && (DropdownOpen || editRequested && ContainsEditor(stage.panel?.focusController?.focusedElement as VisualElement));
        private static List<string> SensorNames() { var names = new List<string>(); for (int i = 0; i < 7; i++) names.Add(DroneSensorRig.Title((SensorKind)i)); return names; }
        private void SelectSensor(SensorKind sensor) { selected = sensor; graphSensor.SetValueWithoutNotify(DroneSensorRig.Title(sensor)); graph.SetSource(rig, selected); Build(); }
        public void SetView(DroneFlightViewMode mode)
        {
            view = mode; bool visible = mode == DroneFlightViewMode.Engineer || mode == DroneFlightViewMode.Diagnostics;
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            sceneOverlay.style.display = mode == DroneFlightViewMode.Engineer ? DisplayStyle.Flex : DisplayStyle.None;
            if (panel.panel?.focusController?.focusedElement is VisualElement focused && (panel.Contains(focused) || graphCard.Contains(focused))) focused.Blur();
            ClearInputFocus();
            if (camera != null) camera.PointerOverUI = false;
            if (!visible) return;
            tab = mode == DroneFlightViewMode.Engineer ? engineerTab : diagnosticsTab;
            title.text = mode == DroneFlightViewMode.Engineer ? DroneKeyBindings.Caption(FlightKeyAction.Engineer) + " · ИНЖЕНЕР" : DroneKeyBindings.Caption(FlightKeyAction.Diagnostics) + " · ДИАГНОСТИКА";
            purpose.text = "Клик в поле — ввод · клик вне поля / Esc — управление дроном";
            panel.EnableInClassList("diagnostics", mode == DroneFlightViewMode.Diagnostics);
            graphCard.style.display = mode == DroneFlightViewMode.Diagnostics ? DisplayStyle.Flex : DisplayStyle.None;
            var engineer = new HashSet<string> { "Среда", "Оборудование", "Контроллер", "Отказы" };
            foreach (var button in panel.Query<Button>().ToList()) if (button.name.StartsWith("tab-"))
                button.style.display = engineer.Contains(button.text) == (mode == DroneFlightViewMode.Engineer) ? DisplayStyle.Flex : DisplayStyle.None;
            Build();
        }
        private void Build()
        {
            content.Clear(); values.Clear(); cameraImage = null; recordStatus = null; recordButton = null; angleToggle = heightToggle = null;
            foreach (var button in panel.Query<Button>().ToList()) if (button.name.StartsWith("tab-")) button.EnableInClassList("selected", button.text == tab);
            if (tab == "Оборудование" || tab == "Показания") BuildSensors();
            else if (tab == "Среда") BuildEnvironment();
            else if (tab == "Контроллер") BuildControl();
            else if (tab == "Отказы") BuildFaults();
            else if (tab == "Моторы") for (int i = 0; i < body.Parameters.Rotors.Count; i++) Row("motor" + i, body.Parameters.Rotors[i].Id);
            else if (tab == "Силы") { Row("thrust", "Сумма тяг роторов, Н"); Row("weight", "Вес, Н"); Row("drag", "Сопротивление корпуса, Н"); Row("rotorDrag", "Сопротивление роторов, Н"); Row("torque", "Момент корпуса, Н·м"); Row("density", "Плотность, кг/м³"); Text(content, "Контактные силы площадки не входят в эту таблицу. Стрелки показывают подготовленные моделью силы.", "flight-note"); }
            else if (tab == "События") Row("events", "Последние события");
            else { Row("position", "Положение X/Y/Z, м"); Row("velocity", "Скорость X/Y/Z, м/с"); Row("attitude", "Крен / тангаж / курс, °"); Row("rate", "Угловая скорость корпуса X/Y/Z, °/с"); Row("power", "Батарея / питание"); Row("controller", "Контроллер"); }
            if (view == DroneFlightViewMode.Diagnostics) {
                Text(content, "ЗАПИСЬ ПОЛЁТА", "flight-subtitle");
                recordButton = Button(content, recording.Active ? "Остановить запись CSV" : "Начать запись CSV", ToggleRecording);
                Number(content,"Интервал строки CSV, с",recording.IntervalS,.02,10,x=>recording.IntervalS=x);
                Text(content,"Время симуляции. Интервал применяется к физике и отдельно к каждому датчику; частота датчика может быть ниже.","flight-note");
                recordStatus = Text(content, "", "flight-note");
                Button(content, "Открыть папку записей", () => { var folder = System.IO.Path.Combine(Application.persistentDataPath, "DroneLab", "Flights"); System.IO.Directory.CreateDirectory(folder); Application.OpenURL(new Uri(folder).AbsoluteUri); });
            }
            graphTitle.text = "ГРАФИК ДАТЧИКА · " + DroneSensorRig.Title(selected);
        }
        private void BuildSensors()
        {
            var names = SensorNames();
            var selector = new DroneDropdown("Датчик", names, (int)selected);
            selector.RegisterValueChangedCallback(evt => SelectSensor((SensorKind)selector.index)); content.Add(selector);

            Row("sensorState", "Состояние");
            if (view == DroneFlightViewMode.Diagnostics) {
                Row("sensorTruth", "Эталон в момент замера"); Row("sensorMeasured", "Измерение"); if (selected != SensorKind.Camera) Row("sensorError", "Ошибка измерения"); Row("sensorAge", "Время / возраст замера");
                if (selected == SensorKind.Gps) {
                    Row("geographic", "Условные широта / долгота / высота");
                    Row("gpsSpeed", "Скорость по разности GPS, м/с");
                    Text(content, "GPS использует локальные метры E/U/N. Начальная геопривязка задаётся вручную; спутниковая сеть и HDOP не моделируются.", "flight-note");
                }
                if (selected == SensorKind.Gyroscope) Text(content, "Угловые скорости вокруг осей крепления X/Y/Z. Это скорость вращения, а не крен, тангаж и курс в градусах.", "flight-note");
                if (selected == SensorKind.Barometer) Row("baroHeight", "Барометрическая высота от старта, м");
                if (selected == SensorKind.Magnetometer) Text(content, "Задано однородное поле (0, −40, 20) мкТл в мире; компоненты измеряются в осях крепления. Геомагнитная карта не используется.", "flight-note");
                if (selected == SensorKind.Accelerometer) Text(content, "Удельное ускорение a − g. На неподвижной площадке датчик видит опору; в свободном падении — около нуля. Оси X вправо, Y вверх, Z вперёд до поворота крепления.", "flight-note");
                if (selected == SensorKind.Camera) { cameraImage = new Image { scaleMode = ScaleMode.ScaleToFit }; cameraImage.AddToClassList("flight-camera-image"); content.Add(cameraImage); }
                Text(content, "Эталон и измерение относятся к одному моменту захвата. Настройки и отказы задаются в F3. Период на графике — длина окна просмотра, а частота датчика — число замеров в секунду.", "flight-note"); return;
            }
            Button(content, "Открыть показания в F4", () => { diagnosticsTab = "Показания"; changeView?.Invoke(DroneFlightViewMode.Diagnostics); });
            var settings = rig.Settings(selected);
            Text(content, "НАСТРОЙКИ ДАТЧИКА", "flight-subtitle");
            Toggle(content, "Включён", settings.enabled, enabled => { settings.enabled = enabled; rig.Apply(selected); });
            var presetRow = Element(content, "flight-inline-actions");
            Button(presetRow, "Идеальные", () => { rig.Preset(true); Build(); });
            Button(presetRow, "С шумом", () => { rig.Preset(false); Build(); });
            Text(content, "Пресеты применяются ко всем датчикам. Ручное изменение — пользовательские настройки; сохраняются для этого дрона.", "flight-note");
            Number(content, "Частота, Гц", settings.frequencyHz, 1, selected == SensorKind.Camera ? 30 : 200, x => { settings.frequencyHz = x; rig.Apply(selected); });
            var errors=new Foldout { text="Погрешности измерения", value=false }; content.Add(errors);
            var foldToggle=errors.Q<UnityEngine.UIElements.Toggle>();
            if(foldToggle!=null) { foldToggle.focusable=false; foldToggle.RegisterCallback<PointerDownEvent>(_=>foldToggle.panel?.focusController?.focusedElement?.Blur()); }
            Text(errors,"Позиция датчика — место на корпусе. Погрешность — добавка к показанию, она не перемещает датчик.","flight-note");
            Number(errors, "Задержка, мс", settings.latencyMs, 0, 2000, x => { settings.latencyMs = x; rig.Apply(selected); });
            if (selected != SensorKind.Camera) {
                Number(errors, "Шум σ, " + DroneSensorRig.Unit(selected), settings.noiseStd, 0, 1000, x => { settings.noiseStd = x; rig.Apply(selected); });
                if (selected == SensorKind.Barometer || selected == SensorKind.Rangefinder)
                    Number(errors, "Погрешность показания", settings.bias[0], -1000, 1000, x => { settings.bias[0] = x; rig.Apply(selected); });
                else Vector(errors, "Погрешность X/Y/Z", settings.bias, 1000, x => { settings.bias = x; rig.Apply(selected); });
            }
            Vector(content, "Позиция датчика, м", settings.positionM, 20, x => { settings.positionM = x; rig.Apply(selected); });
            Vector(content, "Поворот X/Y/Z, °", settings.rotationDeg, 360, x => { settings.rotationDeg = x; rig.Apply(selected); });
            if (selected == SensorKind.Rangefinder) Number(content, "Дальность, м", settings.maxRangeM, .1, 1000, x => { settings.maxRangeM = x; rig.Apply(selected); });
            if (selected == SensorKind.Camera) { Number(content, "Вертикальный обзор, °", settings.cameraFovDeg, 20, 140, x => { settings.cameraFovDeg = x; rig.Apply(selected); }); Text(content, "Изображение 320×180. Частота ограничена кадровой и физической частотой; светочувствительность и шум матрицы не моделируются.", "flight-note"); }
            if (selected == SensorKind.Gps) {
                Number(content, "Широта старта, °", rig.Latitude, -85, 85, x => rig.SetGeographicOrigin(x, rig.Longitude));
                Number(content, "Долгота старта, °", rig.Longitude, -180, 180, x => rig.SetGeographicOrigin(rig.Latitude, x));
            }

        }
        private void BuildEnvironment()
        {
            Row("wind", "Фактический ветер X/Y/Z, м/с"); Row("air", "Температура / давление / плотность"); Row("weather", "Осадки профиля");
            Text(content, "РУЧНОЙ ВЕТЕР", "flight-subtitle");
            Toggle(content, "Изменять ветер во время полёта", wind != null, x => { wind = x ? new UniformWind() : null; UpdateWind(); });
            Number(content, "Скорость, м/с", windSpeed, 0, 40, x => { windSpeed = x; UpdateWind(); });
            Number(content, "Направление (откуда), °", windFrom, 0, 359.9, x => { windFrom = x; UpdateWind(); });
            Text(content, body.Parameters.Environment.WindEnabled ? "0° — с севера, 90° — с востока. Возврат к профилю восстанавливает исходные порывы. Осадки здесь показываются из профиля среды." : "В профиле отключено влияние ветра: ручной ветер не применяется к физике. Включите модуль в конфигураторе.", "flight-note");
                        Text(content, "ВИЗУАЛИЗАЦИЯ", "flight-subtitle");
            Toggle(content, "Луч дальномера", sceneOverlay.RangeBeam, x => sceneOverlay.RangeBeam = x);
            Toggle(content, "Область обзора камеры", sceneOverlay.CameraCone, x => sceneOverlay.CameraCone = x);
            Toggle(content, "Маршрут в сцене", sceneOverlay.Trail, x => sceneOverlay.Trail = x);
            Toggle(content, "Стрелки сил и ветра", sceneOverlay.Forces, x => sceneOverlay.Forces = x);
            Number(content,"Длина стрелок на экране, px",sceneOverlay.MaxArrowPixels,60,180,x=>sceneOverlay.MaxArrowPixels=(float)x);
            Text(content,"Стрелки сохраняют направление; длина ограничена для читаемости. Величины показаны числами.","flight-note");
        }
        private void UpdateWind()
        {
            if (wind != null) { double angle = windFrom * Math.PI / 180; wind.Velocity = new DVector3(-windSpeed * Math.Sin(angle), 0, -windSpeed * Math.Cos(angle)); }
            body.RuntimeWindOverride = wind; rig.Log(wind == null ? "Ветер восстановлен из профиля" : "Ручной ветер: " + N(windSpeed) + " м/с, направление от " + N(windFrom) + "°");
        }
        private void BuildControl()
        {
            Row("controller", "Состояние контроллера");
            if (pilot != null) {
                angleToggle = Toggle(content, "Стабилизация наклона (Angle)", pilot.autoLevel, x => { pilot.autoLevel = x; rig.Log("Angle: " + x); });
                heightToggle = Toggle(content, "Удержание высоты", pilot.altitudeHold, x => { pilot.altitudeHold = x; rig.Log("Удержание высоты: " + x); });
                Button(content,"Удерживать текущую точку X/Z",()=> { pilot.HoldPosition(); });
                Button(content,"Вернуть ручное управление",()=>pilot.CancelPositionHold());
                Row("navigation","Навигация");
                Number(content, "Максимальный наклон, °", pilot.maxTiltDegrees, 1, 45, x => pilot.maxTiltDegrees = (float)x);
                Number(content, "Скорость подъёма, м/с", pilot.climbSpeedMps, .1, 10, x => pilot.climbSpeedMps = (float)x);
                Number(content, "Ручная тяга, доля", pilot.manualCollectiveFraction, .1, .95, x => pilot.manualCollectiveFraction = (float)x);
            }
            Text(content, "J удерживает текущую точку X/Z по GPS; H удерживает только высоту. Ручной крен/тангаж отменяет удержание точки. Потеря GPS останавливает навигацию. Контуры наклона и высоты пока используют состояние физики.", "flight-note");
        }
        private void BuildFaults()
        {
            Text(content, "ОТКАЗЫ ДАТЧИКОВ", "flight-subtitle");
            for (int i = 0; i < 7; i++) { var kind = (SensorKind)i; Toggle(content, kind == SensorKind.Gps ? "Потеря GPS" : "Отключить данные: " + DroneSensorRig.Title(kind), rig.Channel(kind).Faulted, x => rig.SetFault(kind, x)); }
            Text(content, "ПРИВОД ДВИГАТЕЛЕЙ", "flight-subtitle");
            for (int i = 0; i < body.Parameters.Rotors.Count; i++) { int rotor = i; Number(content, body.Parameters.Rotors[i].Id + " · доля привода", body.Drive.Get(i), 0, 1, x => { body.SetRotorDriveAuthority(rotor, x); rig.Log(body.Parameters.Rotors[rotor].Id + ": привод " + N(x)); }); }
            Button(content, "Восстановить отказы", () => { body.RestoreRotorDrive(); for (int i = 0; i < 7; i++) rig.SetFault((SensorKind)i, false); Build(); });
            Text(content, "Отказ привода меняет достижимые обороты. Это не разрушение винта и не заклинивание вала; контроллер может потерять устойчивость.", "flight-note");
        }
        private void ToggleRecording() { if (recording.Active) recording.Stop(); else recording.Start(); if (recordButton != null) recordButton.text = recording.Active ? "Остановить запись CSV" : "Начать запись CSV"; }
        public void RefreshSceneOverlay() { if (view == DroneFlightViewMode.Engineer) sceneOverlay.Refresh(); }
        public void ResetPanels() { graph.ClearHistory(); if (root.style.display.value != DisplayStyle.None) Build(); }
        private void Row(string key, string caption)
        { Text(content, caption, "flight-value-caption"); values[key] = Text(content, "—", "flight-value"); }
        private void Put(string key, string value) { if (values.TryGetValue(key, out var label)) label.text = value; }
        public void Refresh(DroneFlightTelemetry telemetry)
        {
            if (telemetry.Armed != previousArmed) rig.Log(telemetry.Armed ? "Моторы включены" : "Моторы выключены"); previousArmed = telemetry.Armed;
            if (telemetry.PowerLimited != previousLimited) rig.Log(telemetry.PowerLimited ? "Ограничение мощности" : "Ограничение мощности снято"); previousLimited = telemetry.PowerLimited;
            if (root.style.display.value == DisplayStyle.None) return;
            sceneOverlay.Telemetry = telemetry; graph.Refresh();
            if (pilot != null) { angleToggle?.SetValueWithoutNotify(pilot.autoLevel); heightToggle?.SetValueWithoutNotify(pilot.altitudeHold); }
            var rb = body.Body;
            Put("position", V(rb.position)); Put("velocity", V(rb.linearVelocity)); Put("attitude", V(new Vector3(telemetry.Roll, telemetry.Pitch, telemetry.Heading)));
            Put("rate", V(Quaternion.Inverse(rb.rotation) * rb.angularVelocity * Mathf.Rad2Deg));
            Put("power", body.Power == null ? "Модель батареи не задана" : N(body.Power.Soc * 100) + "% · " + N(body.Power.TerminalVoltage) + " В · " + N(body.Power.Current) + " А · " + N(body.Power.ElectricalPower) + " Вт");
            Put("navigation",pilot==null ? "—" : pilot.NavigationMessage+(pilot.PositionHold ? " · X/Z "+N(pilot.PositionTarget.x)+" / "+N(pilot.PositionTarget.z)+" м" : ""));
            Put("controller", pilot == null ? "Нет контроллера" : (pilot.autoLevel ? "Angle" : "Ручной") + (pilot.altitudeHold ? " · удержание высоты" : "") + (pilot.Saturated ? " · насыщение" : ""));
            double thrust = 0; foreach (double force in body.ThrustN) thrust += force;
            Put("thrust", N(thrust)); Put("weight", N(body.Parameters.Mass * body.Parameters.Gravity)); Put("drag", V(body.DragForce)); Put("rotorDrag", V(body.RotorDragForce)); Put("torque", V(body.DragTorque)); Put("density", N(body.Air.Density, "0.0000"));
            Put("wind", V(body.WindVelocityWorld)); Put("air", N(body.Air.TemperatureK - 273.15) + " °C · " + N(body.Air.PressurePa, "0") + " Па · " + N(body.Air.Density, "0.0000") + " кг/м³");
            Put("weather", body.Weather.Precipitation + " · " + N(body.Weather.IntensityMmPerHour) + " мм/ч");
            for (int i = 0; i < body.Parameters.Rotors.Count; i++) { var rotor = body.GetRotorTelemetry(i); Put("motor" + i, N(rotor.Rpm, "0") + " об/мин · " + N(rotor.Thrust) + " Н · команда " + N(rotor.Command * 100) + "%\nПривод " + N(rotor.DriveAuthority * 100) + "% · ток " + (rotor.BusCurrent.HasValue ? N(rotor.BusCurrent.Value) + " А" : "—") + " · мотор " + (rotor.MotorTemperature.HasValue ? N(rotor.MotorTemperature.Value - 273.15) + " °C" : "—")); }
            Put("events", string.Join("\n", rig.Events));
            var channel = rig.Channel(selected); var reading = channel.Latest;
            string state = !channel.Settings.enabled ? "Выключен" : channel.Faulted ? "Нет данных: отказ" : !reading.HasValue ? "Ожидание замера / задержка" : !reading.Value.Valid ? "Нет измерения" : "Данные доступны";
            Put("sensorState", state + (channel.LastIntervalS > 0 ? " · фактически " + N(1 / channel.LastIntervalS, "0.0") + " Гц" : ""));
            bool valid = reading.HasValue && reading.Value.Valid;
            if (!valid) { Put("geographic", "—"); Put("baroHeight", "—"); }
            Put("sensorTruth", valid ? ReadingValue(reading.Value, true) : "—"); Put("sensorMeasured", valid ? ReadingValue(reading.Value, false) : "—");
            Put("sensorError", valid && selected != SensorKind.Camera ? selected == SensorKind.Rangefinder || selected == SensorKind.Barometer ? N(reading.Value.Value.X - reading.Value.Truth.X) : V(DronePhysicsBody.ToUnity(reading.Value.Value - reading.Value.Truth)) : "—");
            Put("gpsSpeed", rig.GpsSpeedDerived.HasValue ? N(rig.GpsSpeedDerived.Value) + " · шум координат усиливается при дифференцировании" : "—");
            Put("sensorAge", reading.HasValue ? "t = " + N(reading.Value.CapturedAt) + " с · возраст " + N(Math.Max(0, body.SimulationTimeS - reading.Value.CapturedAt) * 1000, "0") + " мс" : "—");
            if (valid && selected == SensorKind.Barometer) Put("baroHeight", N(rig.BarometricHeight(reading.Value.Value.X)));
            if (valid && selected == SensorKind.Gps) { var geo = SensorMath.LocalToGeographic(reading.Value.Value, rig.Latitude, rig.Longitude, rig.ReferenceAltitude); Put("geographic", N(geo.X, "0.000000") + "° / " + N(geo.Y, "0.000000") + "° / " + N(geo.Z) + " м"); }
            if (cameraImage != null) cameraImage.image = rig.SensorCamera.Image;
            if (recordStatus != null) {
                bool visible=recording.Active || recording.Folder!=null && Time.unscaledTime-recording.StoppedAt < 7;
                recordStatus.style.display=visible ? DisplayStyle.Flex : DisplayStyle.None;
                recordStatus.text=recording.Active ? "Записывается: " + recording.Rows + " строк физики\n" + recording.Folder : recording.Folder;
                if(recordButton!=null) recordButton.text=recording.Active ? "Остановить запись CSV" : "Начать запись CSV";
            }
        }
        private string ReadingValue(SensorReading reading, bool truth)
        { var value = truth ? reading.Truth : reading.Value; return selected == SensorKind.Barometer || selected == SensorKind.Rangefinder ? N(value.X) : selected == SensorKind.Camera ? "320 × 180 · " + N(value.Z, "0") + "°" : V(DronePhysicsBody.ToUnity(value)); }
        private static string N(double number, string format = "0.00") => number.ToString(format, CultureInfo.InvariantCulture);
        private static string V(Vector3 vector) => N(vector.x) + " / " + N(vector.y) + " / " + N(vector.z);
        private static VisualElement Element(VisualElement parent, string css) { var element = new VisualElement { pickingMode = PickingMode.Ignore }; element.AddToClassList(css); parent.Add(element); return element; }
        private static Label Text(VisualElement parent, string text, string css) { var label = new Label(text) { pickingMode = PickingMode.Ignore }; label.AddToClassList(css); parent.Add(label); return label; }
        private static Button Button(VisualElement parent, string text, Action action) { var button = new Button(action) { text = text, focusable = false }; button.AddToClassList("pilot-button"); button.RegisterCallback<PointerDownEvent>(_=>button.panel?.focusController?.focusedElement?.Blur()); parent.Add(button); return button; }
        private static UnityEngine.UIElements.Toggle Toggle(VisualElement parent, string caption, bool value, Action<bool> change) { var toggle = new UnityEngine.UIElements.Toggle(caption) { value = value, focusable=false }; toggle.RegisterCallback<PointerDownEvent>(_=>toggle.panel?.focusController?.focusedElement?.Blur()); toggle.AddToClassList("flight-toggle"); toggle.RegisterValueChangedCallback(evt => change(evt.newValue)); parent.Add(toggle); return toggle; }
        private static void Number(VisualElement parent, string caption, double value, double min, double max, Action<double> change)
        { var field = new DoubleField(caption) { value = value, isDelayed = true }; field.AddToClassList("flight-number"); field.RegisterValueChangedCallback(evt => { double number = double.IsNaN(evt.newValue) || double.IsInfinity(evt.newValue) ? value : Math.Max(min, Math.Min(max, evt.newValue)); field.SetValueWithoutNotify(number); change(number); }); parent.Add(field); }
        private static void Vector(VisualElement parent, string caption, double[] value, float max, Action<double[]> change)
        { var field = new Vector3Field(caption) { value = DronePhysicsBody.ToUnity(DVector3.From(value)) }; field.AddToClassList("flight-vector"); field.RegisterValueChangedCallback(evt => { var result = evt.newValue; for (int i = 0; i < 3; i++) result[i] = float.IsNaN(result[i]) || float.IsInfinity(result[i]) ? 0 : Mathf.Clamp(result[i], -max, max); field.SetValueWithoutNotify(result); change(new[] { (double)result.x, result.y, result.z }); }); parent.Add(field); }
        private void ReleaseKeyboard() { if (ownsKeyboard && pilot != null) { pilot.readKeyboard = previousReadKeyboard; pilot.SetFlightInput(default); } ownsKeyboard = false; }
        public void Dispose() { ReleaseKeyboard(); graph.Dispose(); recording.Dispose(); if (body != null && ReferenceEquals(body.RuntimeWindOverride, wind)) body.RuntimeWindOverride = null; }
    }
}
