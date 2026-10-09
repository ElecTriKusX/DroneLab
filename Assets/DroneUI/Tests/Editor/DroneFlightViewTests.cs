using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI.Tests
{
    public sealed class DroneFlightViewTests
    {
        [TestCase(0, 0)] [TestCase(90, 90)] [TestCase(180, 180)] [TestCase(270, 270)]
        public void HeadingUsesSceneNorthAndClockwiseBearings(float yaw, float expected)
        {
            var forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(expected, DroneFlightMath.Heading(forward))), Is.LessThan(.001f));
        }
        [Test] public void VerticalNosePreservesLastHeading()
        { Assert.That(DroneFlightMath.Heading(Vector3.up, 127), Is.EqualTo(127)); }
        [Test] public void InstrumentsSeparatePitchAndBankFromYaw()
        {
            var attitude = Quaternion.AngleAxis(55, Vector3.up) * Quaternion.AngleAxis(-15, Vector3.right) * Quaternion.AngleAxis(-22, Vector3.forward);
            Assert.That(DroneFlightMath.Pitch(attitude), Is.EqualTo(15).Within(.001f));
            Assert.That(DroneFlightMath.Roll(attitude), Is.EqualTo(22).Within(.001f));
            Assert.That(DroneFlightMath.Heading(attitude * Vector3.forward), Is.EqualTo(55).Within(.001f));
        }
        [Test] public void NorthMovesUpAndEastMovesRightOnMap()
        {
            var rect = new Rect(0, 0, 200, 200);
            Assert.That(DroneFlightMath.MapPoint(new Vector3(10, 0, 20), Vector3.zero, 100, rect), Is.EqualTo(new Vector2(120, 60)));
        }
        [Test] public void MapScaleKeepsHomeAndDroneVisible()
        {
            var start = new Vector3(1234, 9, -450);
            var position = start + new Vector3(180, 27, -80);
            var center = (start + position) / 2;
            var rect = new Rect(0, 0, 200, 200);
            float range = DroneFlightMath.MapRange(position, start);
            Assert.That(rect.Contains(DroneFlightMath.MapPoint(start, center, range, rect)), Is.True);
            Assert.That(rect.Contains(DroneFlightMath.MapPoint(position, center, range, rect)), Is.True);
        }
        [Test] public void MapClipsCrossingSegmentEvenWhenBothEndpointsAreOutside()
        {
            var a = new Vector2(-30, 50); var b = new Vector2(140, 50);
            Assert.That(DroneFlightMath.ClipSegment(new Rect(0, 0, 100, 100), ref a, ref b), Is.True);
            Assert.That(a.x, Is.EqualTo(0).Within(.001f)); Assert.That(b.x, Is.EqualTo(100).Within(.001f));
            Assert.That(a.y, Is.EqualTo(50)); Assert.That(b.y, Is.EqualTo(50));
        }
        [Test] public void MapRejectsEntirelyOutsideSegment()
        {
            var a = new Vector2(-30, -10); var b = new Vector2(140, -10);
            Assert.That(DroneFlightMath.ClipSegment(new Rect(0, 0, 100, 100), ref a, ref b), Is.False);
        }
        [Test] public void CinemaHidesEntirePilotHudAndPilotRestoresIt()
        {
            var root = new VisualElement();
            var hud = new DroneFlightHud(root, null, "Test", "Map", "Weather", _ => { }, () => { }, () => { });
            hud.SetView(DroneFlightViewMode.Cinema);
            Assert.That(root.Q<VisualElement>(className: "pilot-hud").style.display.value, Is.EqualTo(DisplayStyle.None));
            hud.SetView(DroneFlightViewMode.Pilot);
            Assert.That(root.Q<VisualElement>(className: "pilot-hud").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<VisualElement>(className: "pilot-navigation").Query<Button>().ToList().Count, Is.EqualTo(3));
        }
        [Test] public void WheelTicksAndLegacyWheelValuesZoomEqually()
        {
            Assert.That(DroneFlightMath.ZoomDistance(5, 1, .22f, 1, 30), Is.EqualTo(DroneFlightMath.ZoomDistance(5, 120, .22f, 1, 30)).Within(.0001f));
            Assert.That(DroneFlightMath.ZoomDistance(5, 1, .22f, 1, 30), Is.LessThan(4.1f));
        }
        [Test] public void ZoomRespectsDistanceLimits()
        {
            Assert.That(DroneFlightMath.ZoomDistance(2, 10000, .55f, 1, 20), Is.EqualTo(1));
            Assert.That(DroneFlightMath.ZoomDistance(15, -10000, .55f, 1, 20), Is.EqualTo(20));
        }
        [Test] public void EngineerKeepsNavigationAndHidesPilotInstruments()
        {
            var root = new VisualElement();
            var hud = new DroneFlightHud(root, null, "Test", "Map", "Weather", _ => { }, () => { }, () => { });
            hud.SetView(DroneFlightViewMode.Engineer);
            Assert.That(root.Q<VisualElement>(className: "pilot-hud").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(root.Q<VisualElement>(className: "pilot-instruments").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(root.Q<VisualElement>(className: "pilot-navigation").Query<Button>().ToList().Count, Is.EqualTo(3));
        }
        [Test] public void CreatingHudWithoutAReadyBodyOrCameraDoesNotInventTelemetry()
        {
            var root = new VisualElement();
            new DroneFlightHud(root, null, "Test", "Map", "Weather", _ => { }, () => { }, () => { });
            Assert.That(root.Q<Label>(className: "pilot-source"), Is.Null);
            Assert.That(root.Q<Label>(className: "pilot-map-title").text, Is.EqualTo("МАРШРУТ ПОЛЁТА"));
        }
    }
}
