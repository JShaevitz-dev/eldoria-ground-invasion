using System.Collections.Generic;
using UnityEngine;

namespace Eldoria.Terrain
{
    /// <summary>
    /// The shape of the Untamed map (100 × 100 m, centred on the origin): dense alien jungle laid out with 180° symmetry
    /// (whatever is at (x, z) is also at (−x, −z)). Thickets of undergrowth and trees block sight and movement, so the
    /// fighting happens along winding trails and in a few clearings, at close range. Three trails run from each
    /// deployment strip to a clearing in the centre; two smaller glades sit on each side. The ground rolls gently, with
    /// flat deployment strips along the north and south edges. Used by the map builder and the tests.
    /// </summary>
    public static class JungleLayout
    {
        public const float Size = 100f;
        public const float Half = Size / 2f;
        public const float DeployDepth = 10f;

        // ---- Trails and clearings (south half; each has a twin at the mirrored point) ----

        /// <summary>The trails from the south deployment strip to the central clearing (west, middle and east), and two cross trails.</summary>
        public static readonly Vector2[][] Trails =
        {
            new[] { new Vector2(-32f, -42f), new Vector2(-26f, -27f), new Vector2(-31f, -14f), new Vector2(-15f, -5f), new Vector2(0f, 0f) },
            new[] { new Vector2(2f, -42f), new Vector2(9f, -29f), new Vector2(-3f, -18f), new Vector2(4f, -9f), new Vector2(0f, 0f) },
            new[] { new Vector2(33f, -42f), new Vector2(37f, -25f), new Vector2(24f, -13f), new Vector2(12f, -6f), new Vector2(0f, 0f) },
            // Cross trails through the two glades, linking the three.
            new[] { new Vector2(-26f, -27f), new Vector2(-18f, -24f), new Vector2(-3f, -18f) },
            new[] { new Vector2(9f, -29f), new Vector2(22f, -30f), new Vector2(37f, -25f) },
        };
        /// <summary>Half a trail's width: 7 m wide, room for a tank.</summary>
        public const float TrailHalfWidth = 3.5f;

        public struct Clearing
        {
            public Vector2 Centre;
            public float Radius;
        }

        /// <summary>The central clearing, and two glades in the south half (twinned in the north).</summary>
        public static readonly Clearing[] Clearings =
        {
            new Clearing { Centre = Vector2.zero, Radius = 9f },
            new Clearing { Centre = new Vector2(-18f, -24f), Radius = 5f },
            new Clearing { Centre = new Vector2(22f, -30f), Radius = 5f },
        };

        public struct Thicket
        {
            public Vector2 Centre;
            public float Radius;
        }

        private static List<Thicket> thickets;

        /// <summary>
        /// Every thicket (both halves): blobs of undergrowth on a jittered grid over the south half, keeping clear of the
        /// trails, clearings and the deployment strip, then mirrored. Deterministic (a fixed hash, no random state).
        /// </summary>
        public static IReadOnlyList<Thicket> Thickets
        {
            get
            {
                if (thickets != null) return thickets;
                var south = new List<Thicket>();
                // Small thickets packed on a 2.6 × 2.5 m grid, jittered, so the jungle fills everything but the open ground.
                for (int i = 0; i < 39; i++)
                for (int j = 0; j < 16; j++)
                {
                    float x = -49.2f + i * 2.6f + (Hash(i, j, 1) - 0.5f) * 1.0f;
                    float z = -38.8f + j * 2.5f + (Hash(i, j, 2) - 0.5f) * 1.0f;
                    float r = 1.5f + Hash(i, j, 3) * 0.6f;
                    var c = new Vector2(x, z);
                    if (z > -0.6f * r || z - r < -Half + DeployDepth) continue;   // off the centre line, out of the strip
                    if (Mathf.Abs(x) + r > Half - 0.5f) continue;                 // inside the board
                    if (DistanceToTrails(c) - r < TrailHalfWidth + 0.1f) continue;  // trails stay open
                    if (InClearing(c, r + 0.1f)) continue;
                    south.Add(new Thicket { Centre = c, Radius = r });
                }
                thickets = new List<Thicket>(south);
                foreach (var t in south) thickets.Add(new Thicket { Centre = Mirror(t.Centre), Radius = t.Radius });
                return thickets;
            }
        }

        /// <summary>A pseudo-random number in [0, 1) for a grid cell (the same every time, in every build).</summary>
        private static float Hash(int i, int j, int k)
        {
            double v = System.Math.Sin(i * 12.9898 + j * 78.233 + k * 37.719) * 43758.5453;
            return (float)(v - System.Math.Floor(v));
        }

        /// <summary>Horizontal distance from a point to the nearest trail (either half).</summary>
        public static float DistanceToTrails(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (float s in new[] { 1f, -1f })
            foreach (var trail in Trails)
                for (int i = 0; i < trail.Length - 1; i++)
                    best = Mathf.Min(best, DistanceToSegment(p, trail[i] * s, trail[i + 1] * s));
            return best;
        }

        /// <summary>Is the point within <paramref name="margin"/> of a clearing (either half)?</summary>
        public static bool InClearing(Vector2 p, float margin = 0f)
        {
            foreach (float s in new[] { 1f, -1f })
            foreach (var c in Clearings)
                if (Vector2.Distance(p, c.Centre * s) < c.Radius + margin) return true;
            return false;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>The point opposite <paramref name="p"/> (the map's 180° symmetry).</summary>
        public static Vector2 Mirror(Vector2 p) => -p;

        /// <summary>Ground height (m): gently rolling jungle floor, flat in the deployment strips. Height(x, z) = Height(−x, −z).</summary>
        public static float GroundHeight(float x, float z)
        {
            float h = 0.45f * Mathf.Cos(0.11f * x + 0.07f * z) + 0.3f * Mathf.Cos(0.06f * x - 0.13f * z) - 0.2f;
            return h * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Half - DeployDepth - 6f, Half - DeployDepth, Mathf.Abs(z))));
        }
    }
}
