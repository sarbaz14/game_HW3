using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Catmull-Rom spline through waypoints. Used for curved entry paths so chickens
    /// sweep into their formation slot instead of sliding in a straight line.
    /// </summary>
    public class WaypointPath
    {
        private readonly Vector3[] _points;

        public WaypointPath(params Vector3[] points)
        {
            _points = points ?? new Vector3[0];
        }

        public int PointCount => _points.Length;

        public Vector3 Evaluate(float t)
        {
            if (_points.Length == 0) return Vector3.zero;
            if (_points.Length == 1) return _points[0];

            t = Mathf.Clamp01(t);
            int segments = _points.Length - 1;
            float scaled = t * segments;
            int index = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, segments - 1);
            float localT = scaled - index;

            Vector3 p0 = _points[Mathf.Max(index - 1, 0)];
            Vector3 p1 = _points[index];
            Vector3 p2 = _points[index + 1];
            Vector3 p3 = _points[Mathf.Min(index + 2, _points.Length - 1)];

            return CatmullRom(p0, p1, p2, p3, localT);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * ((2f * p1) +
                           (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }
    }
}
