using UnityEngine;

namespace DroneLab.UI
{
    /// <summary>Read-only display coordinates: Unity +Y is up and +Z is the scene's north.</summary>
    internal static class DroneFlightMath
    {
        public static float Heading(Vector3 forward, float fallback = 0)
        {
            if (forward.x * forward.x + forward.z * forward.z < .000001f) return Mathf.Repeat(fallback, 360);
            return Mathf.Repeat(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 360);
        }
        public static float Pitch(Quaternion attitude)
        {
            var forward = attitude * Vector3.forward;
            return Mathf.Asin(Mathf.Clamp(forward.y, -1, 1)) * Mathf.Rad2Deg;
        }
        public static float Roll(Quaternion attitude)
        {
            var forward = attitude * Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < .000001f) return 0; // Bank is ambiguous with a vertical nose.
            right.Normalize();
            var referenceUp = Vector3.Cross(forward, right).normalized;
            var actualRight = attitude * Vector3.right;
            return Mathf.Atan2(-Vector3.Dot(actualRight, referenceUp), Vector3.Dot(actualRight, right)) * Mathf.Rad2Deg;
        }
        public static Vector2 MapPoint(Vector3 world, Vector3 center, float range, Rect rect)
            => new Vector2(rect.center.x + (world.x - center.x) / range * rect.width,
                rect.center.y - (world.z - center.z) / range * rect.height);
        public static float MapRange(Vector3 position, Vector3 start)
        {
            float separation = Mathf.Max(Mathf.Abs(position.x - start.x), Mathf.Abs(position.z - start.z));
            float required = Mathf.Max(100, separation * 1.35f);
            // Stable scale steps rather than a constantly zooming map.
            return 100 * Mathf.Pow(2, Mathf.Ceil(Mathf.Log(required / 100, 2)));
        }
        public static bool ClipSegment(Rect rect, ref Vector2 a, ref Vector2 b)
        {
            var delta = b - a; float enter = 0, leave = 1;
            bool Bound(float direction, float margin)
            {
                if (Mathf.Abs(direction) < .000001f) return margin >= 0;
                float ratio = margin / direction;
                if (direction < 0) { if (ratio > leave) return false; enter = Mathf.Max(enter, ratio); }
                else { if (ratio < enter) return false; leave = Mathf.Min(leave, ratio); }
                return true;
            }
            if (!Bound(-delta.x, a.x - rect.x) || !Bound(delta.x, rect.xMax - a.x) ||
                !Bound(-delta.y, a.y - rect.y) || !Bound(delta.y, rect.yMax - a.y)) return false;
            var start = a; a = start + delta * enter; b = start + delta * leave; return true;
        }
    }
}
