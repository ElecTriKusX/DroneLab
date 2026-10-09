using System;
using System.Collections.Generic;
using DroneLab.Sensors;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal sealed class DroneFlightSceneOverlay : VisualElement
    {
        private readonly DronePhysicsBody body;
        private readonly DroneSensorRig rig;
        private readonly DroneFlightCamera camera;
        private readonly Label[] labels = new Label[5];
        private readonly List<Segment> segments = new List<Segment>(8192);
        private readonly struct Segment
        {
            public readonly Vector2 From, To;
            public readonly Color Color;
            public readonly float Width;
            public Segment(Vector2 from, Vector2 to, Color color, float width) { From = from; To = to; Color = color; Width = width; }
        }
        public float MaxArrowPixels=120;
        public bool Forces = true, RangeBeam = true, CameraCone, Trail;
        public DroneFlightTelemetry Telemetry;
        public DroneFlightSceneOverlay(DronePhysicsBody selectedBody, DroneSensorRig selectedRig, DroneFlightCamera selectedCamera)
        {
            body = selectedBody; rig = selectedRig; camera = selectedCamera;
            AddToClassList("flight-scene-overlay"); pickingMode = PickingMode.Ignore;
            for (int i = 0; i < labels.Length; i++) { labels[i] = new Label(); labels[i].AddToClassList("flight-vector-caption"); labels[i].pickingMode = PickingMode.Ignore; Add(labels[i]); }
            generateVisualContent += Draw;
        }
        private bool Project(Vector3 world, out Vector2 point)
        {
            var screen = camera.Camera.WorldToViewportPoint(world);
            point = new Vector2(screen.x * contentRect.width, (1 - screen.y) * contentRect.height);
            return screen.z > camera.Camera.nearClipPlane;
        }
        public void Refresh()
        {
            segments.Clear();
            foreach (var label in labels) label.style.display = DisplayStyle.None;
            if (camera?.Camera == null || body == null || !body.IsReady) return;
            Color ink = Color.white; float width = 2;
            var screen = new Rect(12, 120, Mathf.Max(1,contentRect.width-24), Mathf.Max(1,contentRect.height-240));
            void Line(Vector3 a, Vector3 b) {
                if (!Project(a, out var from) || !Project(b, out var to) || !DroneFlightMath.ClipSegment(screen, ref from, ref to)) return;
                segments.Add(new Segment(from, to, ink, width));
            }
            void ScreenLine(Vector2 from, Vector2 to) { if (DroneFlightMath.ClipSegment(screen, ref from, ref to)) segments.Add(new Segment(from, to, ink, width)); }
            void Arrow(Vector3 vector, float scale, Color color, string title, string unit, int index) {
                if (vector.sqrMagnitude < .0001f) return;
                var origin = body.transform.TransformPoint(body.Body.centerOfMass);
                var end = origin + vector.normalized * Mathf.Min(vector.magnitude*scale,.5f); ink = color;
                if (!Project(origin, out var a) || !Project(end, out var b) || !screen.Contains(a)) return;
                var delta=b-a; if(delta.sqrMagnitude<.01f) return;
                float pixels=Mathf.Min(MaxArrowPixels,delta.magnitude*Mathf.Max(1,vector.magnitude*scale/.5f));
                b=a+delta.normalized*pixels;
                if(!DroneFlightMath.ClipSegment(screen,ref a,ref b)) return;
                ScreenLine(a,b);
                var direction = (b - a).normalized; var normal = new Vector2(-direction.y, direction.x);
                ScreenLine(b - direction * 10 + normal * 4, b); ScreenLine(b, b - direction * 10 - normal * 4);
                var label = labels[index]; label.text = title + " " + vector.magnitude.ToString("0.00") + " " + unit;
                label.style.left = Mathf.Clamp(b.x+8,12,Mathf.Max(12,contentRect.width-740)); label.style.top = Mathf.Clamp(b.y-13,120,contentRect.height-150); label.style.color = color; label.style.display = DisplayStyle.Flex;
            }
            if (Forces) {
                Vector3 thrust = Vector3.zero;
                for (int i = 0; i < body.Parameters.Rotors.Count; i++) thrust += body.transform.TransformDirection(DronePhysicsBody.ToUnity(body.Parameters.Rotors[i].Axis)) * (float)body.ThrustN[i];
                float forceScale = Mathf.Clamp(2 / (float)(body.Parameters.Mass * body.Parameters.Gravity), .005f, .6f);
                Arrow(thrust, forceScale, new Color(.67f, .86f, .68f), "ТЯГА", "Н", 0);
                Arrow(Vector3.down * (float)(body.Parameters.Mass * body.Parameters.Gravity), forceScale, new Color(.88f, .72f, .53f), "ВЕС", "Н", 1);
                Arrow(body.DragForce + body.RotorDragForce, forceScale, new Color(.80f, .62f, .58f), "СОПРОТИВЛЕНИЕ", "Н", 2);
                Arrow(body.WindVelocityWorld, .2f, new Color(.58f, .79f, .88f), "ВЕТЕР", "м/с", 3);
            }
            if (RangeBeam && rig.Settings(SensorKind.Rangefinder).enabled && !rig.Channel(SensorKind.Rangefinder).Faulted) {
                ink = rig.RangeHit ? new Color(.7f, .9f, .87f) : new Color(.65f, .65f, .65f);
                Line(rig.RangeOrigin, rig.RangeEnd);
                if (Project(rig.RangeEnd, out var point) && screen.Contains(point)) {
                    labels[4].text = rig.RangeHit ? "ДАЛЬНОМЕР " + Vector3.Distance(rig.RangeOrigin, rig.RangeEnd).ToString("0.00") + " м · геометрия" : "НЕТ ПОПАДАНИЯ";
                    labels[4].style.left = point.x + 8; labels[4].style.top = point.y; labels[4].style.display = DisplayStyle.Flex;
                }
            }
            if (CameraCone && camera.Mode == DroneFlightCameraMode.Chase && rig.Settings(SensorKind.Camera).enabled && rig.SensorCamera.Mount != null) {
                var mount = rig.SensorCamera.Mount; var corners = new Vector3[4];
                float half = Mathf.Tan((float)rig.Settings(SensorKind.Camera).cameraFovDeg * .5f * Mathf.Deg2Rad) * 4;
                for (int i = 0; i < 4; i++) corners[i] = mount.TransformPoint(new Vector3((i == 0 || i == 3 ? -1 : 1) * half * 16 / 9, (i < 2 ? 1 : -1) * half, 4));
                ink = new Color(.64f, .76f, .86f);
                for (int i = 0; i < 4; i++) { Line(mount.position, corners[i]); Line(corners[i], corners[(i + 1) % 4]); }
            }
            if (Trail && Telemetry != null) {
                width = 1.5f; ink = new Color(.72f, .82f, .80f);
                float phase=0;
                for (int i = 1; i < Telemetry.Trail.Count; i++) {
                    if(!Project(Telemetry.Trail[i-1],out var a) || !Project(Telemetry.Trail[i],out var b) || !DroneFlightMath.ClipSegment(screen,ref a,ref b)) continue;
                    float length=Vector2.Distance(a,b); if(length<.01f) continue;
                    var direction=(b-a)/length;
                    for(float t=0;t<length;) {
                        float step=Mathf.Min(length-t,14-phase);
                        if(phase<9) ScreenLine(a+direction*t,a+direction*(t+Mathf.Min(step,9-phase)));
                        t+=step; phase=(phase+step)%14;
                    }
                }
            }
            MarkDirtyRepaint();
        }
        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            foreach (var segment in segments) {
                painter.strokeColor = segment.Color; painter.lineWidth = segment.Width;
                painter.BeginPath(); painter.MoveTo(segment.From); painter.LineTo(segment.To); painter.Stroke();
            }
        }
    }
}
