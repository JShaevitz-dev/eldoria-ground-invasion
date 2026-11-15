using UnityEngine;

namespace Eldoria.Terrain
{
    /// <summary>
    /// The shape of the Polar Nonsense map (100 × 100 m, centred on the origin): snow-covered ground laid out with 180°
    /// symmetry (whatever is at (x, z) is also at (−x, −z)), so neither side has the better half. Two knolls, two ridges
    /// and two low rises, gentle snow drifts, flat deployment strips along the north and south edges, and flat pads for
    /// the station huts. Used by the map builder (Editor) and by the tests.
    /// </summary>
    public static class PolarLayout
    {
        public const float Size = 100f;
        public const float Half = Size / 2f;
        public const float DeployDepth = 10f;

        // ---- Hills (each listed once; its twin is at the mirrored point) ----

        public static readonly Vector2 KnollCentre = new Vector2(-26f, -16f);
        public const float KnollRadius = 13f, KnollHeight = 4.5f;

        public static readonly Vector2 RidgeStart = new Vector2(-45f, 10f), RidgeEnd = new Vector2(-24f, 18f);
        public const float RidgeHalfWidth = 8f, RidgeHeight = 3f;

        public static readonly Vector2 RiseCentre = new Vector2(30f, -24f);
        public const float RiseRadius = 7f, RiseHeight = 2f;

        // ---- Station huts (each stands on a flat pad) ----

        public struct Pad
        {
            public string Name;
            public Vector2 Centre;
            public float Radius;
        }

        /// <summary>The pads: the Relay Hut in the centre, and a Weather Station and a Supply Shed on each side.</summary>
        public static readonly Pad[] Pads =
        {
            new Pad { Name = "Relay Hut", Centre = Vector2.zero, Radius = 7f },
            new Pad { Name = "Weather Station", Centre = new Vector2(14f, -22f), Radius = 6f },
            new Pad { Name = "Weather Station", Centre = new Vector2(-14f, 22f), Radius = 6f },
            new Pad { Name = "Supply Shed", Centre = new Vector2(-38f, -6f), Radius = 5f },
            new Pad { Name = "Supply Shed", Centre = new Vector2(38f, 6f), Radius = 5f },
        };

        /// <summary>The point opposite <paramref name="p"/> (the map's 180° symmetry).</summary>
        public static Vector2 Mirror(Vector2 p) => -p;

        // ---- Height ----

        /// <summary>Ground height (m) at a point: hills, snow drifts, flat pads and flat deployment strips. Height(x, z) = Height(−x, −z).</summary>
        public static float GroundHeight(float x, float z)
        {
            float h = Unpadded(x, z);
            foreach (var pad in Pads)
            {
                float d = Vector2.Distance(new Vector2(x, z), pad.Centre);
                float padH = Unpadded(pad.Centre.x, pad.Centre.y);
                h = Mathf.Lerp(padH, h, Smooth(pad.Radius, pad.Radius + 5f, d));
            }
            return h;
        }

        private static float Unpadded(float x, float z)
        {
            var p = new Vector2(x, z);
            // Snow drifts: even functions, so the same at (x, z) and (−x, −z).
            float h = 0.4f * Mathf.Cos(0.09f * x + 0.05f * z) + 0.3f * Mathf.Cos(0.04f * x - 0.11f * z) - 0.2f;
            foreach (float s in new[] { 1f, -1f })
            {
                h += Bump(p, KnollCentre * s, KnollRadius, KnollHeight);
                h += Bump(p, RiseCentre * s, RiseRadius, RiseHeight);
                h += RidgeHeightAt(p, RidgeStart * s, RidgeEnd * s);
            }
            // The deployment strips along the north and south edges are flat.
            h *= 1f - Smooth(Half - DeployDepth - 6f, Half - DeployDepth, Mathf.Abs(z));
            return h;
        }

        private static float Bump(Vector2 p, Vector2 centre, float radius, float height)
        {
            float d = Vector2.Distance(p, centre);
            return d < radius ? height * 0.5f * (1f + Mathf.Cos(Mathf.PI * d / radius)) : 0f;
        }

        private static float RidgeHeightAt(Vector2 p, Vector2 start, Vector2 end)
        {
            Vector2 ab = end - start;
            float t = Mathf.Clamp01(Vector2.Dot(p - start, ab) / ab.sqrMagnitude);
            float across = Vector2.Distance(p, start + ab * t);
            float along = t * ab.magnitude;
            float taper = Mathf.Clamp01(Mathf.Min(along, ab.magnitude - along) / 5f);
            return across < RidgeHalfWidth ? RidgeHeight * taper * 0.5f * (1f + Mathf.Cos(Mathf.PI * across / RidgeHalfWidth)) : 0f;
        }

        /// <summary>0 below a, 1 above b, smooth in between.</summary>
        private static float Smooth(float a, float b, float v) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, v));
    }
}
