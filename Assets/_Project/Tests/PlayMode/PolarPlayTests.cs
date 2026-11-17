using System.Collections;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using L = Eldoria.Terrain.PolarLayout;

namespace Eldoria.Tests
{
    /// <summary>The Polar Nonsense map: 100 × 100 m of snow with 180° symmetry, hills that block sight, a polar station, cover.</summary>
    public class PolarPlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadMap()
        {
            ArmySelection.Clear();
            TestArmies.LoadScene(Battlefields.PolarNonsense.Scene);
            yield return null;
            yield return null;
        }

        private static IMoveMeasurer Measurer => BattlefieldNavigation.Measurer;

        private static Vector3 Stand(float x, float z)
        {
            Assert.That(Measurer.TryGetStandPoint(new Vector3(x, 0f, z), 0.35f, 1.8f, out var p, out var why), Is.True, $"({x}, {z}): {why}");
            return p;
        }

        [Test]
        public void TheMapIs100By100WithAZoneAlongEachEdge()
        {
            Assert.That(Battlefields.All, Does.Contain(Battlefields.PolarNonsense));
            Assert.That((Core.BattlefieldScale.BoardWidth, Core.BattlefieldScale.BoardDepth), Is.EqualTo((100f, 100f)));
            var zones = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None);
            Assert.That(zones.Single(z => z.Side == Side.Player).transform.position.z, Is.EqualTo(-45f));
            Assert.That(zones.Single(z => z.Side == Side.AI).transform.position.z, Is.EqualTo(45f));
            Assert.That(zones.All(z => z.Size == new Vector2(100f, 10f)), Is.True);
        }

        [Test]
        public void TheMapIsSymmetrical()
        {
            // The ground: the same height at (x, z) and (−x, −z).
            for (float x = -48f; x <= 48f; x += 6f)
            for (float z = -48f; z <= 48f; z += 6f)
                Assert.That(L.GroundHeight(x, z), Is.EqualTo(L.GroundHeight(-x, -z)).Within(1e-4f), $"({x}, {z})");

            // Every prop and building has a twin, of the same kind, at the mirrored point.
            var things = Object.FindObjectsByType<TerrainObject>(FindObjectsSortMode.None).Select(o => (name: o.DisplayName, at: o.transform.position))
                .Concat(Object.FindObjectsByType<StructureTerrain>(FindObjectsSortMode.None).Where(s => s.Kind == StructureKind.Building)
                    .Select(s => (name: s.name, at: s.transform.position)))
                .ToList();
            Assert.That(things.Count, Is.GreaterThan(30));
            foreach (var (name, at) in things)
                Assert.That(things.Any(t => t.name == name && Measure.Horizontal(t.at, new Vector3(-at.x, 0f, -at.z)) < 0.05f), Is.True, $"{name} at {at}");
        }

        [Test]
        public void TheStationIsThere()
        {
            var buildings = Object.FindObjectsByType<StructureTerrain>(FindObjectsSortMode.None)
                .Where(s => s.Kind == StructureKind.Building).Select(s => s.name).ToList();
            Assert.That(buildings.Count(n => n == "Relay Hut"), Is.EqualTo(1));
            Assert.That(buildings.Count(n => n == "Weather Station"), Is.EqualTo(2));
            Assert.That(buildings.Count(n => n == "Supply Shed"), Is.EqualTo(2));
            // Into the Relay Hut through a door.
            var path = Measurer.Measure(Stand(0f, -8f), Stand(0f, 1.5f));
            Assert.That(path.Reachable, Is.True, path.Reason);
        }

        [Test]
        public void ThePropsGiveCover()
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
            Check("Snowbank", "Half-cover", true, false);
            Check("Fuel Drum", "Half-cover", true, true);
            Check("Supply Crate", "Half-cover", true, true);
            Check("Ice Boulder", "Full-cover", false, false);
            Check("Fuel Tank", "Full-cover", false, false);
        }

        [Test]
        public void NoPropIsClippedByTheSlope()
        {
            // Under every prop's footprint the ground stays well below its top (over half the prop shows, so no hill
            // pokes through it) and reaches its bottom (no corner floats above the snow).
            var props = Object.FindObjectsByType<TerrainObject>(FindObjectsSortMode.None).Where(o => o.DisplayName != "Radio Mast").ToList();
            Assert.That(props.Count, Is.GreaterThan(30));
            foreach (var prop in props)
            foreach (var c in prop.GetComponentsInChildren<Collider>())
            {
                var b = c.bounds;
                float height = b.size.y;
                for (float x = b.min.x; x <= b.max.x; x += 0.2f)
                for (float z = b.min.z; z <= b.max.z; z += 0.2f)
                {
                    // Only points inside the prop's own footprint (its bounds are wider when it's turned).
                    if (!c.Raycast(new Ray(new Vector3(x, b.max.y + 1f, z), Vector3.down), out var hit, height + 2f)) continue;
                    float ground = L.GroundHeight(x, z);
                    Assert.That(b.max.y - ground, Is.GreaterThan(0.55f * height), $"{prop.DisplayName} at {prop.transform.position}: buried at ({x:0.0}, {z:0.0})");
                    Assert.That(ground - b.min.y, Is.GreaterThan(-0.02f), $"{prop.DisplayName} at {prop.transform.position}: floating at ({x:0.0}, {z:0.0})");
                }
            }
        }

        [UnityTest]
        public IEnumerator TheKnollsBlockLineOfSight()
        {
            var one = TestArmies.FieldCommander;   // any unit of one soldier
            Unit Place(float x, float z) => UnitFactory.Spawn(one, Side.Player, new Vector3(x, L.GroundHeight(x, z), z), Quaternion.identity);
            var c = L.KnollCentre;
            var units = new[]
            {
                Place(c.x - L.KnollRadius, c.y), Place(c.x + L.KnollRadius, c.y),       // either side of the south-west knoll
                Place(-c.x - L.KnollRadius, -c.y), Place(-c.x + L.KnollRadius, -c.y),   // and of its twin
                Place(-20f, -44f), Place(20f, -44f),
            };
            yield return null;
            try
            {
                Assert.That(LineOfSight.CanSee(units[0].Leader, units[1].Leader), Is.False, "the knoll's crest is in the way");
                Assert.That(LineOfSight.CanSee(units[2].Leader, units[3].Leader), Is.False, "and its twin's");
                Assert.That(LineOfSight.CanSee(units[4].Leader, units[5].Leader), Is.True, "the flat deployment strip");
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
        public IEnumerator TheAIPlaysOnThePolarMap()
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
