using UnityEngine;

namespace Eldoria.Terrain
{
    /// <summary>
    /// The shape of the Desert Surprise map (140 × 140 m, centred on the origin): an open salt flat laid out with 180°
    /// symmetry (whatever is at (x, z) is also at (−x, −z)), built for armor and long-range fire. The flat in the middle is
    /// almost bare, with long sightlines; a few low dunes give hull-down positions. The surprise: a dry wadi on each side,
    /// a sunken channel snaking from near each deployment strip up to the centre, out of sight of the flat, so infantry can
    /// close in unseen. Flat deployment strips along the north and south edges. Used by the map builder and the tests.
    /// </summary>
    public static class DesertLayout
    {
        public const float Size = 140f;
        public const float Half = Size / 2f;
        public const float DeployDepth = 10f;

        // ---- The wadis (each listed once; its twin is the mirrored path) ----

        /// <summary>The south-west wadi's course, from near the player's deployment strip to the centre; its twin runs from the north-east.</summary>
        public static readonly Vector2[] WadiPath =
        {
            new Vector2(-50f, -54f), new Vector2(-42f, -36f), new Vector2(-30f, -20f), new Vector2(-24f, -4f), new Vector2(-12f, 6f),
        };
        public const float WadiDepth = 2.6f, WadiHalfWidth = 7f;
        /// <summary>How far (m) from each end the wadi takes to reach its full depth.</summary>
        public const float WadiTaper = 8f;

        // ---- Dunes (each listed once; its twin is at the mirrored point) ----

        public struct Dune
        {
            public Vector2 Centre;
            public float Length, Width, Height, Yaw;
        }

        /// <summary>Low, long dunes: hull-down positions for tanks, and something to duck behind on the flat.</summary>
        public static readonly Dune[] Dunes =
        {
            new Dune { Centre = new Vector2(18f, -36f), Length = 26f, Width = 10f, Height = 2.2f, Yaw = 75f },
            new Dune { Centre = new Vector2(46f, -22f), Length = 20f, Width = 9f, Height = 1.8f, Yaw = 20f },
            new Dune { Centre = new Vector2(-8f, -30f), Length = 16f, Width = 8f, Height = 1.5f, Yaw = -30f },
        };

        /// <summary>The point opposite <paramref name="p"/> (the map's 180° symmetry).</summary>
        public static Vector2 Mirror(Vector2 p) => -p;

        // ---- Height ----

        /// <summary>Ground height (m) at a point: the salt flat, dunes and wadis; the deployment strips are flat. Height(x, z) = Height(−x, −z).</summary>
        public static float GroundHeight(float x, float z)
        {
            var p = new Vector2(x, z);
            // A barely rolling salt flat: even functions, so the same at (x, z) and (−x, −z).
            float h = 0.15f * Mathf.Cos(0.05f * x + 0.03f * z) + 0.1f * Mathf.Cos(0.02f * x - 0.06f * z) - 0.1f;
            foreach (float s in new[] { 1f, -1f })
            {
                foreach (var dune in Dunes) h += DuneHeightAt(p, dune, s);
                h -= WadiDepthAt(p, s);
            }
            // The deployment strips along the north and south edges are flat.
            h *= 1f - Smooth(Half - DeployDepth - 6f, Half - DeployDepth, Mathf.Abs(z));
            return h;
        }

        /// <summary>How deep the wadi (the south-west one for s = 1, its twin for s = −1) is at a point (0 outside it).</summary>
        public static float WadiDepthAt(Vector2 p, float s = 1f)
        {
            float best = float.MaxValue, alongBest = 0f, travelled = 0f, total = 0f;
            for (int i = 0; i < WadiPath.Length - 1; i++) total += Vector2.Distance(WadiPath[i], WadiPath[i + 1]);
            for (int i = 0; i < WadiPath.Length - 1; i++)
            {
                Vector2 a = WadiPath[i] * s, b = WadiPath[i + 1] * s, ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                float d = Vector2.Distance(p, a + ab * t);
                if (d < best) { best = d; alongBest = travelled + t * ab.magnitude; }
                travelled += ab.magnitude;
            }
            if (best >= WadiHalfWidth) return 0f;
            float taper = Mathf.Clamp01(Mathf.Min(alongBest, total - alongBest) / WadiTaper);
            return WadiDepth * taper * 0.5f * (1f + Mathf.Cos(Mathf.PI * best / WadiHalfWidth));
        }

        /// <summary>The middle of the wadi's course, as a point along its bottom (for the tests).</summary>
        public static Vector2 WadiPoint(int segment, float t = 0.5f, float s = 1f) =>
            Vector2.Lerp(WadiPath[segment], WadiPath[segment + 1], t) * s;

        private static float DuneHeightAt(Vector2 p, Dune dune, float s)
        {
            // An elongated mound: a cosine bump in an ellipse turned by the dune's yaw.
            var local = Quaternion.Euler(0f, 0f, dune.Yaw) * (Vector3)(p - dune.Centre * s);
            float u = local.x / (dune.Length / 2f), v = local.y / (dune.Width / 2f);
            float r = Mathf.Sqrt(u * u + v * v);
            return r < 1f ? dune.Height * 0.5f * (1f + Mathf.Cos(Mathf.PI * r)) : 0f;
        }

        /// <summary>0 below a, 1 above b, smooth in between.</summary>
        private static float Smooth(float a, float b, float v) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, v));
    }
}
