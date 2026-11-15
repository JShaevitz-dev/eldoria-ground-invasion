using System.Collections;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using L = Eldoria.Terrain.ShipLayout;

namespace Eldoria.Tests
{
    /// <summary>The Leisure Cruise map: the cluttered inside of a ship, 50 × 150 m, with corridors, compartments and a cargo hold.</summary>
    public class ShipPlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadMap()
        {
            ArmySelection.Clear();
            TestArmies.LoadScene(Battlefields.LeisureCruise.Scene);
            yield return null;
            yield return null;
        }

        private static IMoveMeasurer Measurer => BattlefieldNavigation.Measurer;

        private static Vector3 Stand(float x, float z, float radius = 0.35f)
        {
            Assert.That(Measurer.TryGetStandPoint(new Vector3(x, 0f, z), radius, 1.8f, out var p, out var why), Is.True, $"({x}, {z}): {why}");
            return p;
        }

        [Test]
        public void TheMapIs50By150WithAZoneInEachHangar()
        {
            Assert.That(Battlefields.All, Does.Contain(Battlefields.LeisureCruise));
            Assert.That((Core.BattlefieldScale.BoardWidth, Core.BattlefieldScale.BoardDepth), Is.EqualTo((50f, 150f)));
            var zones = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None);
            Assert.That(zones.Single(z => z.Side == Side.Player).transform.position.z, Is.EqualTo(-70f));
            Assert.That(zones.Single(z => z.Side == Side.AI).transform.position.z, Is.EqualTo(70f));
            Assert.That(zones.All(z => z.Size == new Vector2(50f, 10f)), Is.True);
        }

        [Test]
        public void TheShipIsSymmetrical()
        {
            // Every wall and every piece of clutter has a twin, of the same kind, at the mirrored point.
            var things = Object.FindObjectsByType<TerrainObject>(FindObjectsSortMode.None).Select(o => (name: o.DisplayName, at: o.transform.position))
                .Concat(Object.FindObjectsByType<StructureTerrain>(FindObjectsSortMode.None).Where(s => s.Kind == StructureKind.Building)
                    .Select(s => (name: s.name, at: s.transform.position)))
                .ToList();
            Assert.That(things.Count, Is.GreaterThan(150));
            foreach (var (name, at) in things)
                Assert.That(things.Any(t => t.name == name && Measure.Horizontal(t.at, new Vector3(-at.x, 0f, -at.z)) < 0.05f), Is.True, $"{name} at {at}");
        }

        [Test]
        public void EveryCompartmentCanBeReachedFromTheSpine()
        {
            foreach (var room in L.Rooms)
            foreach (float s in new[] { 1f, -1f })
            {
                var c = room.Centre * s;
                var path = Measurer.Measure(Stand(0f, 30f * s), Stand(c.x, c.y));
                Assert.That(path.Reachable, Is.True, $"{room.Name} at {c}: {path.Reason}");
            }
        }

        [Test]
        public void SoldiersCanCrossFromHangarToHangar()
        {
            foreach (float x in new[] { 0f, 23f, -23f })
            {
                var path = Measurer.Measure(Stand(x, -66f), Stand(x, 66f));
                Assert.That(path.Reachable, Is.True, $"from x = {x}: {path.Reason}");
            }
        }

        [Test]
        public void VehiclesFitDownTheSpine()
        {
            var path = Measurer.Measure(Stand(0f, -68f, 1.8f), Stand(0f, 68f, 1.8f), 1.8f);
            Assert.That(path.Reachable, Is.True, path.Reason);
        }

        [Test]
        public void TheClutterGivesCover()
        {
            var objects = Object.FindObjectsByType<TerrainObject>(FindObjectsSortMode.None);
            void Check(string name, string cover, bool mantle, bool climb)
            {
                var found = objects.Where(o => o.DisplayName == name).ToList();
                Assert.That(found, Is.Not.Empty, name);
                foreach (var o in found)
                {
                    Assert.That(o.HasKeyword(cover), Is.True, $"{name}: {cover}");
                    Assert.That((o.CanMantle, o.CanClimbOnto), Is.EqualTo((mantle, climb)), name);
                }
            }
            Check("Supply Crate", "Half-cover", true, true);
            Check("Coolant Drum", "Half-cover", true, true);
            Check("Debris", "Half-cover", true, false);
            Check("Console", "Half-cover", true, false);
            Check("Bunk", "Half-cover", true, true);
            Check("Cargo Container", "Full-cover", false, false);
            Check("Locker", "Full-cover", false, false);
            Check("Reactor Housing", "Full-cover", false, false);
        }

        [UnityTest]
        public IEnumerator BulkheadsBlockSightAndTheHoldContainerBlocksShots()
        {
            var one = TestArmies.FieldCommander;   // any unit of one soldier
            Unit Place(float x, float z) => UnitFactory.Spawn(one, Side.Player, new Vector3(x, 0f, z), Quaternion.identity);
            var units = new[]
            {
                Place(0f, 4f), Place(0f, -4f),       // either side of the cargo container in the middle of the hold
                Place(0f, 41f), Place(12f, 44f),             // the spine and a compartment, through a bulkhead
                Place(0f, 20f), Place(0f, 55f),      // down the spine
            };
            yield return null;
            try
            {
                Assert.That(Attack.CanTarget(units[0].Leader, units[0].Leader.Weapons[0], units[1].Leader), Is.False, "the container (Full-cover) is in the way");
                Assert.That(LineOfSight.CanSee(units[2].Leader, units[3].Leader), Is.False, "the spine's wall");
                Assert.That(LineOfSight.CanSee(units[4].Leader, units[5].Leader), Is.True, "straight down the corridor");
            }
            finally
            {
                foreach (var u in units) Object.Destroy(u.gameObject);
            }
        }

        [Test]
        public void TheArmiesStartInTheirZonesOnStandableGround()
        {
            var zones = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None);
            var units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            Assert.That(units, Is.Not.Empty);
            foreach (var unit in units)
            foreach (var s in unit.Soldiers)
            {
                Assert.That(zones.Single(z => z.Side == unit.Side).Contains(s.Position), Is.True, s.name);
                Assert.That(Measurer.TryGetStandPoint(s.Position, s.Radius, s.Height, out _, out var why), Is.True, $"{s.name}: {why}");
            }
        }

        [UnityTest]
        public IEnumerator TheAIPlaysOnTheShip()
        {
            var controller = Object.FindFirstObjectByType<MovementController>();
            for (int turn = 0; turn < 3 && !Victory.IsOver; turn++)
            {
                controller.EndTurn(instant: true);
                yield return null;
            }
            Assert.That(Turns.Current, Is.GreaterThanOrEqualTo(3));
        }
    }
}
