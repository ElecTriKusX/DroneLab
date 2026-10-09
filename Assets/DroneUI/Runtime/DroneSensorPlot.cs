using System;
using System.Collections.Generic;
using System.Globalization;
using DroneLab.Sensors;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>Draw reads a prepared snapshot only. UI labels are updated outside generateVisualContent.</summary>
    internal sealed class DroneSensorPlot : VisualElement, IDisposable
    {
        private readonly SensorHistory[] histories = new SensorHistory[7];
        private readonly Action<SensorReading>[] handlers = new Action<SensorReading>[7];
        private readonly List<SensorReading> samples = new List<SensorReading>(12064);
        private readonly Label[] verticalTicks = new Label[5], timeTicks = new Label[5];
        private readonly Toggle[] axes = new Toggle[3];
        private readonly Label status, legend, unit;

        private DroneSensorRig rig;
        private SensorKind kind;
        private double start, end, minimum, maximum;
        private bool hasData, errorOnly;
        private double period = 20;
        public static readonly Color[] AxisColors = { new Color(.48f, .82f, .94f), new Color(1f, .73f, .40f), new Color(.58f, .88f, .59f) };
        public double PeriodS => period;
        public bool ErrorOnly => errorOnly;
        public DroneSensorPlot()
        {
            AddToClassList("sensor-plot"); pickingMode = PickingMode.Ignore;
            var controls = new VisualElement(); controls.AddToClassList("sensor-plot-controls"); Add(controls);
            var periods = new List<string> { "5 с", "10 с", "20 с", "60 с" };
            var window = new DroneDropdown("Период", periods, 2); window.AddToClassList("sensor-period");
            window.tooltip = "Длина окна просмотра. Частота измерений задаётся в F3, в настройках оборудования.";
            window.RegisterValueChangedCallback(evt => { period = new[] { 5d, 10, 20, 60 }[periods.IndexOf(evt.newValue)]; Refresh(); }); controls.Add(window);
            var modes = new List<string> { "Измерение и эталон", "Ошибка измерения" };
            var mode = new DroneDropdown("График", modes, 0); mode.AddToClassList("sensor-mode");
            mode.RegisterValueChangedCallback(evt => { errorOnly = evt.newValue == modes[1]; Refresh(); }); controls.Add(mode);
            var axisRow = new VisualElement(); axisRow.AddToClassList("sensor-axis-row"); Add(axisRow);
            for (int i = 0; i < 3; i++) {
                axes[i] = new Toggle { value = true, focusable=false }; axes[i].AddToClassList("sensor-axis-toggle"); axes[i].RegisterCallback<PointerDownEvent>(_=>panel?.focusController?.focusedElement?.Blur());
                axes[i].RegisterValueChangedCallback(_ => Refresh()); axisRow.Add(axes[i]);
            }
            unit = Label("sensor-plot-unit");
            for (int i = 0; i < 5; i++) { verticalTicks[i] = Label("sensor-plot-y"); timeTicks[i] = Label("sensor-plot-time"); }
            legend = Label("sensor-plot-legend"); status = Label("sensor-plot-empty");
            RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            generateVisualContent += Draw;
        }
        private Label Label(string css) { var text = new Label { pickingMode = PickingMode.Ignore }; text.AddToClassList(css); Add(text); return text; }
        public void SetSource(DroneSensorRig selected, SensorKind sensor)
        {
            if (rig != selected) {
                Dispose(); rig = selected;
                if (rig != null) for (int i = 0; i < 7; i++) {
                    int index = i; histories[i] = new SensorHistory(); handlers[i] = reading => histories[index].Add(reading);
                    var channel = rig.Channel((SensorKind)i); channel.Delivered += handlers[i];
                    if (channel.Latest.HasValue) histories[i].Add(channel.Latest.Value);
                }
            }
            kind = sensor; Refresh();
        }
        public void Dispose()
        {
            if (rig != null) for (int i = 0; i < 7; i++) if (handlers[i] != null) rig.Channel((SensorKind)i).Delivered -= handlers[i];
            rig = null; samples.Clear();
        }
        public void ClearHistory() { foreach (var history in histories) history?.Clear(); Refresh(); }
        private int AxisCount => kind == SensorKind.Camera ? 0 : kind == SensorKind.Barometer || kind == SensorKind.Rangefinder ? 1 : 3;
        private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        private float Value(SensorReading reading, bool truth, int axis)
        {
            var v = truth ? reading.Truth : reading.Value;
            if (kind == SensorKind.Barometer) return (float)rig.BarometricHeight(v.X);
            return DronePhysicsBody.ToUnity(v)[axis];
        }
        private float PlottedValue(SensorReading reading, bool truth, int axis) => errorOnly ? Value(reading, false, axis) - Value(reading, true, axis) : Value(reading, truth, axis);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private Rect PlotRect => new Rect(64, 100, Mathf.Max(1, contentRect.width - 80), Mathf.Max(1, contentRect.height - 154));
        public void Refresh()
        {
            end = rig?.TimeS ?? 0; start = end - period; hasData = false;
            samples.Clear(); if (rig != null) histories[(int)kind]?.CopyWindowTo(samples, end, period);
            string[] captions = kind == SensorKind.Gps ? new[] { "E · восток", "U · вверх", "N · север" } : new[] { "X · вправо", "Y · вверх", "Z · вперёд" };
            for (int axis = 0; axis < 3; axis++) {
                axes[axis].style.display = axis < AxisCount ? DisplayStyle.Flex : DisplayStyle.None;
                axes[axis].text = AxisCount == 1 ? kind == SensorKind.Barometer ? "Высота от старта" : "Расстояние вдоль луча" : captions[axis];
                var axisText = axes[axis].Q<TextElement>(className: "unity-toggle__text"); if (axisText != null) axisText.style.color = AxisColors[axis];
            }
            unit.text = kind == SensorKind.Barometer || kind == SensorKind.Rangefinder || kind == SensorKind.Gps ? "м" : kind == SensorKind.Gyroscope ? "°/с" : kind == SensorKind.Accelerometer ? "м/с²" : kind == SensorKind.Magnetometer ? "мкТл" : "";
            legend.text = errorOnly ? "Ошибка = измерение − эталон · нулевая линия — точное совпадение" : "Сплошная — измерение · пунктир — эталон того же момента";
            minimum = errorOnly ? 0 : double.PositiveInfinity; maximum = errorOnly ? 0 : double.NegativeInfinity;
            foreach (var reading in samples) if (reading.Valid) for (int axis = 0; axis < AxisCount; axis++) if (axes[axis].value) {
                float a = PlottedValue(reading, true, axis), b = PlottedValue(reading, false, axis);
                if (!Finite(a) || !Finite(b)) continue;
                minimum = Math.Min(minimum, Math.Min(a, b)); maximum = Math.Max(maximum, Math.Max(a, b)); hasData = true;
            }
            if (hasData) { double margin = Math.Max(.05, (maximum - minimum) * .12); minimum -= margin; maximum += margin; }
            var rect = PlotRect;
            for (int i = 0; i < 5; i++) {
                verticalTicks[i].text = hasData ? Number(maximum - (maximum - minimum) * i / 4) : "";
                verticalTicks[i].style.top = rect.y + rect.height * i / 4 - 9;
                timeTicks[i].text = Number(-period + period * i / 4) + " с";
                timeTicks[i].style.left = rect.x + rect.width * i / 4 - 26; timeTicks[i].style.top = rect.yMax + 5;
            }
            status.text = kind == SensorKind.Camera ? "Изображение камеры — во вкладке «Показания»." : hasData ? "" : "Нет данных или все оси скрыты";
            status.style.display = status.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            MarkDirtyRepaint();
        }
        private void Draw(MeshGenerationContext context)
        {
            if (contentRect.width < 100 || contentRect.height < 180) return;
            var rect = PlotRect; var p = context.painter2D;
            void Line(Vector2 a, Vector2 b) { p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.Stroke(); }
            p.strokeColor = new Color(.25f, .28f, .30f); p.lineWidth = 1;
            for (int i = 0; i <= 4; i++) {
                Line(new Vector2(rect.x, rect.y + rect.height * i / 4), new Vector2(rect.xMax, rect.y + rect.height * i / 4));
                Line(new Vector2(rect.x + rect.width * i / 4, rect.y), new Vector2(rect.x + rect.width * i / 4, rect.yMax));
            }
            if (!hasData || rig == null) return;
            if (errorOnly) { float zero = rect.yMax - (float)(-minimum / (maximum - minimum)) * rect.height; p.strokeColor = new Color(.55f, .59f, .60f); Line(new Vector2(rect.x, zero), new Vector2(rect.xMax, zero)); }
            double gap = Math.Max(.3, 2.5 / rig.Channel(kind).Settings.frequencyHz);
            for (int axis = 0; axis < AxisCount; axis++) if (axes[axis].value) for (int line = 0; line < (errorOnly ? 1 : 2); line++) {
                bool truth = !errorOnly && line == 0; p.strokeColor = AxisColors[axis]; p.lineWidth = truth ? 1 : 1.8f;
                Vector2 Point(SensorReading reading) => new Vector2(rect.x + (float)((reading.CapturedAt - start) / period) * rect.width,
                    rect.yMax - (float)((PlottedValue(reading, truth, axis) - minimum) / (maximum - minimum)) * rect.height);
                // Bucket by display column: retain first, extrema and last, so high-rate spikes remain visible.
                var points = new List<Vector2>(8); Vector2? previous = null; double previousTime = -1; int column = -1; float dashDistance = 0;
                void Flush() {
                    if (points.Count == 0) return;
                    var first = points[0]; var last = points[points.Count - 1]; var low = first; var high = first;
                    foreach (var point in points) { if (point.y < low.y) low = point; if (point.y > high.y) high = point; }
                    var reduced = new List<Vector2> { first, low, high, last }; reduced.Sort((a, b) => a.x.CompareTo(b.x));
                    foreach (var point in reduced) {
                        if (previous.HasValue) {
                            var a = previous.Value; var b = point;
                            if (DroneFlightMath.ClipSegment(rect, ref a, ref b)) {
                                if (!truth) Line(a, b);
                                else {
                                    float length = Vector2.Distance(a, b), t = 0;
                                    while (t < length) {
                                        float phase = dashDistance % 10;
                                        float step = Math.Min(length - t, phase < 5 ? 5 - phase : 10 - phase);
                                        if (phase < 5) Line(Vector2.Lerp(a, b, t / length), Vector2.Lerp(a, b, (t + step) / length));
                                        t += step; dashDistance += step;
                                    }
                                }
                            }
                        }
                        previous = point;
                    }
                    points.Clear();
                }
                foreach (var reading in samples) {
                    if (!reading.Valid || !Finite(PlottedValue(reading, truth, axis))) { Flush(); previous = null; previousTime = -1; column = -1; dashDistance = 0; continue; }
                    if (previousTime >= 0 && reading.CapturedAt - previousTime > gap) { Flush(); previous = null; column = -1; dashDistance = 0; }
                    var point = Point(reading); int nextColumn = Mathf.FloorToInt(point.x);
                    if (nextColumn != column) { Flush(); column = nextColumn; }
                    points.Add(point); previousTime = reading.CapturedAt;
                }
                Flush();
            }
        }
    }
}
