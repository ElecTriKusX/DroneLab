using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal enum FlightInstrumentKind { Heading, Horizon, Altitude, Speed, Map, Fpv }

    /// <summary>UI Toolkit vector instruments. Numeric text uses Labels, never font-dependent icon glyphs.</summary>
    internal sealed class DroneFlightInstrument : VisualElement
    {
        private readonly FlightInstrumentKind kind;
        private readonly Label[] ticks;
        private readonly Label value;
        private DroneFlightTelemetry data;
        private static readonly Color Ink = new Color(.91f, .93f, .93f);
        private static readonly Color Dim = new Color(.54f, .59f, .61f);
        public DroneFlightInstrument(FlightInstrumentKind selectedKind)
        {
            kind = selectedKind; pickingMode = PickingMode.Ignore;
            AddToClassList("flight-instrument");
            ticks = new Label[selectedKind == FlightInstrumentKind.Heading ? 9 : selectedKind == FlightInstrumentKind.Altitude || selectedKind == FlightInstrumentKind.Speed ? 7 : 5];
            for (int i = 0; i < ticks.Length; i++) { ticks[i] = Text(); Add(ticks[i]); }
            value = Text(); value.AddToClassList("flight-instrument-value"); Add(value);
            generateVisualContent += Draw;
        }
        private static Label Text()
        {
            var label = new Label(); label.pickingMode = PickingMode.Ignore;
            label.AddToClassList("flight-instrument-text"); return label;
        }
        private void Put(Label label, string text, float x, float y, float width = 54)
        {
            label.text = text; label.style.left = x; label.style.top = y; label.style.width = width;
            label.style.display = DisplayStyle.Flex;
        }
        public void SetData(DroneFlightTelemetry telemetry) { data = telemetry; RefreshLabels(); MarkDirtyRepaint(); }
        private void RefreshLabels()
        {
            if (data == null) return;
            float w = contentRect.width, h = contentRect.height;
            foreach (var label in ticks) label.style.display = DisplayStyle.None;
            value.style.display = DisplayStyle.None;
            if (w < 1 || h < 1) return;
            if (kind == FlightInstrumentKind.Heading)
            {
                float origin = Mathf.Floor(data.Heading / 15) * 15;
                for (int i = 0; i < ticks.Length; i++)
                {
                    float bearing = origin + (i - 4) * 15;
                    float x = w / 2 + (bearing - data.Heading) * w / 135;
                    if (x < 24 || x > w - 24) continue;
                    int normalized = Mathf.RoundToInt(Mathf.Repeat(bearing, 360));
                    string caption = normalized == 0 ? "С" : normalized == 90 ? "В" : normalized == 180 ? "Ю" : normalized == 270 ? "З" : normalized.ToString("000");
                    Put(ticks[i], caption, x - 23, 16, 46);
                }
                Put(value, Mathf.RoundToInt(Mathf.Repeat(data.Heading, 360)).ToString("000") + "°", w / 2 - 41, 34, 82);
            }
            else if (kind == FlightInstrumentKind.Altitude || kind == FlightInstrumentKind.Speed)
            {
                float reading = kind == FlightInstrumentKind.Altitude ? data.RelativeHeight : data.GroundSpeed;
                float step = kind == FlightInstrumentKind.Altitude ? 10 : 2;
                float origin = Mathf.Floor(reading / step) * step;
                for (int i = 0; i < ticks.Length; i++)
                {
                    float number = origin + (i - 3) * step;
                    float y = h / 2 - (number - reading) / step * 34;
                    if (y < 4 || y > h - 22 || kind == FlightInstrumentKind.Speed && number < 0) continue;
                    Put(ticks[i], number.ToString("0"), 43, y - 10, w - 48);
                }
                Put(value, reading.ToString("0.0"), 12, h / 2 - 15, w - 14);
            }
            else if (kind == FlightInstrumentKind.Horizon || kind == FlightInstrumentKind.Fpv)
            {
                float scale = kind == FlightInstrumentKind.Fpv ? 5 : 2;
                for (int i = 0; i < ticks.Length; i++)
                {
                    float angle = (i - 2) * 10;
                    if (Mathf.Abs(angle) < .1f) continue;
                    var point = HorizonPoint(new Vector2(36, (data.Pitch - angle) * scale), w, h);
                    bool inside = kind == FlightInstrumentKind.Fpv ? point.y > 30 && point.y < h - 30 : (point - new Vector2(w / 2, h / 2)).magnitude < Mathf.Min(w, h) * .39f;
                    if (inside) Put(ticks[i], angle.ToString("0"), point.x + 6, point.y - 10, 32);
                }
            }
            else if (kind == FlightInstrumentKind.Map)
            {
                Put(ticks[0], "С (+Z)", w / 2 - 38, 2, 76);
                float range = DroneFlightMath.MapRange(data.Position, data.Start);
                Put(ticks[1], (range / 4).ToString("0") + " м", w - 76, h - 28, 70);
            }
        }
        private Vector2 HorizonPoint(Vector2 point, float w, float h)
        {
            float angle = -data.Roll * Mathf.Deg2Rad;
            return new Vector2(w / 2 + point.x * Mathf.Cos(angle) - point.y * Mathf.Sin(angle),
                h / 2 + point.x * Mathf.Sin(angle) + point.y * Mathf.Cos(angle));
        }
        private void Draw(MeshGenerationContext context)
        {
            if (data == null) return;
            float w = contentRect.width, h = contentRect.height;
            if (w < 1 || h < 1) return;
            var painter = context.painter2D; painter.lineWidth = 1.4f; painter.strokeColor = Ink;
            void Line(Vector2 a, Vector2 b) { painter.BeginPath(); painter.MoveTo(a); painter.LineTo(b); painter.Stroke(); }
            void Polygon(IReadOnlyList<Vector2> points, bool fill)
            {
                if (points.Count == 0) return;
                painter.BeginPath(); painter.MoveTo(points[0]);
                for (int i = 1; i < points.Count; i++) painter.LineTo(points[i]);
                painter.ClosePath(); if (fill) painter.Fill(); else painter.Stroke();
            }
            var center = new Vector2(w / 2, h / 2);
            if (kind == FlightInstrumentKind.Heading)
            {
                float origin = Mathf.Floor(data.Heading / 15) * 15;
                for (int i = -14; i <= 14; i++)
                {
                    float bearing = origin + i * 5, x = w / 2 + (bearing - data.Heading) * w / 135;
                    if (x < 12 || x > w - 12) continue;
                    Line(new Vector2(x, 2), new Vector2(x, i % 3 == 0 ? 14 : 9));
                }
                painter.fillColor = Ink;
                Polygon(new[] { new Vector2(w / 2 - 5, 0), new Vector2(w / 2 + 5, 0), new Vector2(w / 2, 7) }, true);
            }
            else if (kind == FlightInstrumentKind.Altitude || kind == FlightInstrumentKind.Speed)
            {
                float reading = kind == FlightInstrumentKind.Altitude ? data.RelativeHeight : data.GroundSpeed;
                float step = kind == FlightInstrumentKind.Altitude ? 10 : 2;
                float origin = Mathf.Floor(reading / step) * step;
                Line(new Vector2(32, 10), new Vector2(32, h - 10));
                for (int i = -8; i <= 8; i++)
                {
                    float number = origin + i * step / 2, y = h / 2 - (number - reading) / step * 34;
                    if (y < 10 || y > h - 10 || kind == FlightInstrumentKind.Speed && number < 0) continue;
                    Line(new Vector2(32, y), new Vector2(i % 2 == 0 ? 45 : 39, y));
                }
                painter.fillColor = Ink;
                Polygon(new[] { new Vector2(0, h / 2), new Vector2(8, h / 2 - 6), new Vector2(8, h / 2 + 6) }, true);
            }
            else if (kind == FlightInstrumentKind.Horizon || kind == FlightInstrumentKind.Fpv)
            {
                float radius = Mathf.Min(w, h) * .45f;
                float scale = kind == FlightInstrumentKind.Fpv ? 5 : 2;
                if (kind == FlightInstrumentKind.Horizon)
                {
                    var disk = new List<Vector2>(73);
                    for (int i = 0; i <= 72; i++)
                    { float angle = i * Mathf.PI * 2 / 72; disk.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius); }
                    painter.fillColor = new Color(.22f, .22f, .20f, .92f); Polygon(disk, true);
                    // Clip the sky half-plane against the circular instrument, including inverted attitudes.
                    var sky = new List<Vector2>();
                    float roll = -data.Roll * Mathf.Deg2Rad;
                    var normal = new Vector2(-Mathf.Sin(roll), Mathf.Cos(roll));
                    float threshold = data.Pitch * scale;
                    Vector2 previous = disk[disk.Count - 1]; float previousSide = Vector2.Dot(previous - center, normal) - threshold;
                    foreach (var current in disk)
                    {
                        float side = Vector2.Dot(current - center, normal) - threshold;
                        if ((side <= 0) != (previousSide <= 0)) sky.Add(Vector2.Lerp(previous, current, previousSide / (previousSide - side)));
                        if (side <= 0) sky.Add(current);
                        previous = current; previousSide = side;
                    }
                    painter.fillColor = new Color(.12f, .17f, .20f, .95f); Polygon(sky, true);
                    painter.strokeColor = Dim; Polygon(disk, false); painter.strokeColor = Ink;
                }
                for (int degree = -30; degree <= 30; degree += 5)
                {
                    float y = (data.Pitch - degree) * scale;
                    float half = degree == 0 ? radius * .8f : degree % 10 == 0 ? 30 : 17;
                    if (kind == FlightInstrumentKind.Horizon)
                    {
                        if (Mathf.Abs(y) >= radius * .85f) continue;
                        half = Mathf.Min(half, Mathf.Sqrt(radius * radius - y * y) * .85f);
                    }
                    else if (Mathf.Abs(y) > h * .4f) continue;
                    Line(HorizonPoint(new Vector2(-half, y), w, h), HorizonPoint(new Vector2(half, y), w, h));
                }
                // Fixed aircraft symbol; the horizon and pitch ladder move beneath it.
                painter.lineWidth = 2;
                Line(center + new Vector2(-48, 0), center + new Vector2(-13, 0));
                Line(center + new Vector2(13, 0), center + new Vector2(48, 0));
                Line(center + new Vector2(-13, 0), center + new Vector2(0, 6));
                Line(center + new Vector2(0, 6), center + new Vector2(13, 0));
                if (kind == FlightInstrumentKind.Horizon)
                    for (int degree = -60; degree <= 60; degree += 15)
                    {
                        float angle = degree * Mathf.Deg2Rad;
                        var direction = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
                        Line(center + direction * (radius + 3), center + direction * (radius + (degree % 30 == 0 ? 12 : 8)));
                    }
            }
            else if (kind == FlightInstrumentKind.Map)
            {
                float side = Mathf.Min(w - 28, h - 56);
                var rect = new Rect((w - side) / 2, 28, side, side);
                float range = DroneFlightMath.MapRange(data.Position, data.Start);
                var mapCenter = (data.Position + data.Start) * .5f;
                painter.strokeColor = new Color(.28f, .32f, .34f);
                for (int i = 0; i <= 4; i++)
                {
                    float x = rect.x + rect.width * i / 4, y = rect.y + rect.height * i / 4;
                    Line(new Vector2(x, rect.y), new Vector2(x, rect.yMax));
                    Line(new Vector2(rect.x, y), new Vector2(rect.xMax, y));
                }
                Vector2 Map(Vector3 position) => DroneFlightMath.MapPoint(position, mapCenter, range, rect);
                painter.strokeColor = Dim; painter.lineWidth = 1.5f;
                // Clip segments before drawing; old trail points may lie outside the current scale.
                for (int i = 1; i < data.Trail.Count; i++)
                {
                    var a = Map(data.Trail[i - 1]); var b = Map(data.Trail[i]);
                    if (DroneFlightMath.ClipSegment(rect, ref a, ref b)) Line(a, b);
                }
                var home = Map(data.Start); painter.strokeColor = Ink;
                Polygon(new[] { home + new Vector2(-6, 0), home + new Vector2(0, -6), home + new Vector2(6, 0), home + new Vector2(6, 7), home + new Vector2(-6, 7) }, false);
                var drone = Map(data.Position); float heading = data.Heading * Mathf.Deg2Rad;
                Vector2 Rotate(Vector2 point) => drone + new Vector2(point.x * Mathf.Cos(heading) - point.y * Mathf.Sin(heading), point.x * Mathf.Sin(heading) + point.y * Mathf.Cos(heading));
                painter.fillColor = Ink;
                Polygon(new[] { Rotate(new Vector2(0, -11)), Rotate(new Vector2(7, 8)), Rotate(new Vector2(0, 4)), Rotate(new Vector2(-7, 8)) }, true);
                Line(new Vector2(w - 80, h - 9), new Vector2(w - 80 + rect.width / 4, h - 9));
            }
        }
    }
}
