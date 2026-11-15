using UnityEngine;

namespace Eldoria.Terrain
{
    /// <summary>
    /// The shape of the Leisure Cruise map: the inside of a ship, 50 m wide (x) and 150 m long (z), centred on the origin,
    /// laid out with 180° symmetry (whatever is at (x, z) is also at (−x, −z)). A hangar bay at each end (the deployment
    /// zones); a wide spine corridor down the middle and a narrow corridor along each side of the hull; blocks of
    /// compartments between them, crossed by a cross corridor; and a cargo hold in the middle. Used by the map builder
    /// (Editor) and by the tests.
    /// </summary>
    public static class ShipLayout
    {
        public const float Width = 50f, Length = 150f;
        public const float HalfWidth = Width / 2f, HalfLength = Length / 2f;
        public const float DeployDepth = 10f;

        /// <summary>The hangar bays are beyond |z| = 62 (the deployment zones fill their outer 10 m).</summary>
        public const float HangarEdge = 62f;
        /// <summary>The cargo hold spans |z| ≤ 10, between the side corridors.</summary>
        public const float HoldHalfLength = 10f;
        /// <summary>The side corridors run between |x| = 21 and the hull at |x| = 25.</summary>
        public const float SideWallX = 21f;
        /// <summary>The spine corridor runs down the middle, |x| ≤ 3 (6 m wide: wide enough for vehicles).</summary>
        public const float SpineHalfWidth = 3f;
        /// <summary>The cross corridors run from the spine to the side corridors at z = ±36 (4 m wide).</summary>
        public const float CrossCorridorZ = 36f, CrossCorridorWidth = 4f;
        public const float WallHeight = 3f, HullHeight = 4f;

        public struct Room
        {
            public string Name;
            public Vector2 Centre;
        }

        /// <summary>The compartments in the northern half (each 18 × 12 m); their twins are at the mirrored points.</summary>
        public static readonly Room[] Rooms =
        {
            new Room { Name = "Armory", Centre = new Vector2(12f, 16f) },
            new Room { Name = "Med Bay", Centre = new Vector2(12f, 28f) },
            new Room { Name = "Crew Quarters", Centre = new Vector2(12f, 44f) },
            new Room { Name = "Machine Shop", Centre = new Vector2(12f, 56f) },
            new Room { Name = "Galley", Centre = new Vector2(-12f, 16f) },
            new Room { Name = "Comms Room", Centre = new Vector2(-12f, 28f) },
            new Room { Name = "Crew Quarters", Centre = new Vector2(-12f, 44f) },
            new Room { Name = "Reactor Control", Centre = new Vector2(-12f, 56f) },
        };

        /// <summary>The point opposite <paramref name="p"/> (the map's 180° symmetry).</summary>
        public static Vector2 Mirror(Vector2 p) => -p;
    }
}
