using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal enum MenuIconKind { Play, Drone, Landscape, Laboratory, Settings, Book, Exit, Chevron, Sun, Wind }

    /// <summary>Resolution-independent outline icons; no font glyphs or bitmap dependencies.</summary>
    internal sealed class DroneMenuIcon : VisualElement
    {
        private readonly MenuIconKind kind;
        public DroneMenuIcon(MenuIconKind kind)
        {
            this.kind = kind;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }
        private void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D;
            p.lineWidth = 1.8f;
            p.strokeColor = resolvedStyle.color;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            float size = Mathf.Min(contentRect.width, contentRect.height);
            Vector2 Point(float x, float y) => new Vector2(x * size / 40, y * size / 40);
            void Line(params float[] points)
            {
                p.BeginPath(); p.MoveTo(Point(points[0], points[1]));
                for (int i = 2; i < points.Length; i += 2) p.LineTo(Point(points[i], points[i + 1]));
                p.Stroke();
            }
            void Circle(float x, float y, float radius)
            {
                p.BeginPath();
                for (int i = 0; i <= 32; i++)
                {
                    float angle = i * Mathf.PI * 2 / 32;
                    var v = Point(x + Mathf.Cos(angle) * radius, y + Mathf.Sin(angle) * radius);
                    if (i == 0) p.MoveTo(v); else p.LineTo(v);
                }
                p.ClosePath(); p.Stroke();
            }
            switch (kind)
            {
                case MenuIconKind.Sun:
                    Circle(20, 20, 8);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; Line(20 + Mathf.Cos(a) * 12, 20 + Mathf.Sin(a) * 12, 20 + Mathf.Cos(a) * 17, 20 + Mathf.Sin(a) * 17); }
                    break;
                case MenuIconKind.Wind:
                    Line(3, 13, 28, 13, 32, 10, 32, 6, 28, 4, 24, 6);
                    Line(3, 21, 34, 21, 37, 18, 37, 15, 34, 13);
                    Line(3, 29, 24, 29, 28, 32, 28, 35, 24, 37, 20, 35); break;
                case MenuIconKind.Play: Line(11, 7, 32, 20, 11, 33, 11, 7); break;
                case MenuIconKind.Drone:
                    Line(10, 10, 30, 30); Line(30, 10, 10, 30);
                    Circle(10, 10, 6); Circle(30, 10, 6); Circle(10, 30, 6); Circle(30, 30, 6); Circle(20, 20, 5); break;
                case MenuIconKind.Landscape:
                    Line(3, 32, 14, 16, 25, 32, 3, 32); Line(16, 21, 23, 13, 36, 32, 25, 32);
                    Line(27, 9, 31, 5, 35, 9, 31, 13, 27, 9); break;
                case MenuIconKind.Laboratory:
                    Line(4, 34, 36, 34); Line(7, 34, 7, 23, 13, 23, 13, 34);
                    Line(17, 34, 17, 16, 23, 16, 23, 34); Line(27, 34, 27, 7, 33, 7, 33, 34); break;
                case MenuIconKind.Settings:
                    p.BeginPath();
                    for (int i = 0; i <= 48; i++)
                    {
                        float angle = i * Mathf.PI * 2 / 48;
                        float radius = i % 6 == 1 || i % 6 == 2 ? 16 : 12;
                        var v = Point(20 + Mathf.Cos(angle) * radius, 20 + Mathf.Sin(angle) * radius);
                        if (i == 0) p.MoveTo(v); else p.LineTo(v);
                    }
                    p.ClosePath(); p.Stroke(); Circle(20, 20, 5); break;
                case MenuIconKind.Book:
                    Line(20, 10, 15, 7, 6, 7, 6, 30, 15, 30, 20, 33, 25, 30, 34, 30, 34, 7, 25, 7, 20, 10, 20, 33);
                    Line(10, 13, 15, 13); Line(10, 18, 15, 18); Line(10, 23, 15, 23);
                    Line(25, 13, 30, 13); Line(25, 18, 30, 18); Line(25, 23, 30, 23); break;
                case MenuIconKind.Exit:
                    Line(10, 7, 30, 7, 30, 33, 10, 33, 10, 7); Line(14, 20, 25, 20); Line(21, 16, 25, 20, 21, 24); break;
                case MenuIconKind.Chevron: Line(15, 9, 26, 20, 15, 31); break;
            }
        }
    }
}
