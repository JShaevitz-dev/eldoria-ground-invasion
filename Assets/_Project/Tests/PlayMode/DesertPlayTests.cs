using System.Collections;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using L = Eldoria.Terrain.DesertLayout;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Desert Surprise map: 140 × 140 m of open salt flat with 180° symmetry, long sightlines, low dunes, sparse cover,
    /// and a sunken wadi on each side that hides what moves along it.
    /// </summary>
    public class DesertPlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadMap()
        {
            ArmySelection.Clear();
            TestArmies.LoadScene(Battlefields.DesertSurprise.Scene);
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
        public void TheMapIs140By140WithAZoneAlongEachEdge()
        {
            Assert.That(Battlefields.All, Does.Contain(Battlefields.DesertSurprise));
            Assert.That(Battlefields.DesertSurprise.Name, Is.EqualTo("Desert Surprise"));
            Assert.That((Core.BattlefieldScale.BoardWidth, Core.BattlefieldScale.BoardDepth), Is.EqualTo((140f, 140f)));
            var zones = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None);
            Assert.That(zones.Single(z => z.Side == Side.Player).transform.position.z, Is.EqualTo(-65f));
            Assert.That(zones.Single(z => z.Side == Side.AI).transform.position.z, Is.EqualTo(65f));
            Assert.That(zones.All(z => z.Size == new Vector2(140f, 10f)), Is.True);
        }

        [Test]
        public void TheMapIsSymmetrical()
        {
            for (float x = -66f; x <= 66f; x += 6f)
            for (float z = -66f; z <= 66f; z += 6f)
                Assert.That(L.GroundHeight(x, z), Is.EqualTo(L.GroundHeight(-x, -z)).Within(1e-4f), $"({x}, {z})");
            var props = Object.FindObjectsByType<TerrainObject>(FindObjectsSortMode.None).Select(o => (name: o.DisplayName, at: o.transform.position)).ToList();
            Assert.That(props.Count, Is.EqualTo(DesertMapBuilderProps * 2));
            foreach (var (name, at) in props)
                Assert.That(props.Any(t => t.name == name && Measure.Horizontal(t.at, new Vector3(-at.x, 0f, -at.z)) < 0.05f), Is.True, $"{name} at {at}");
        }

        /// <summary>How many props the builder lists (each is placed twice).</summary>
        private const int DesertMapBuilderProps = 13;

        [Test]
        public void CoverIsSparse()
        {
            var objects = Object.FindObjectsByType<TerrainObject>(FindObjectsSortMode.None);
            void Check(string name, string cover, int count)
            {
                var found = objects.Where(o => o.DisplayName == name).ToList();
                Assert.That(found.Count, Is.EqualTo(count), name);
                Assert.That(found.All(o => o.HasKeyword(cover)), Is.True, $"{name}: {cover}");
            }
            Check("Rock Outcrop", "Full-cover", 6);
            Check("Freighter Wreck", "Full-cover", 2);
            Check("Tank Wreck", "Full-cover", 4);
            Check("Scrub", "Half-cover", 8);
            Check("Cairn", "Half-cover", 6);
        }

        [UnityTest]
        public IEnumerator TheFlatHasLongSightlinesButTheWadiHidesWhatIsInIt()
        {
            var one = TestArmies.FieldCommander;   // any unit of one soldier
            Unit Place(Vector2 p) => UnitFactory.Spawn(one, Side.Player, new Vector3(p.x, L.GroundHeight(p.x, p.y), p.y), Quaternion.identity);
            var wadi = L.WadiPoint(2);                      // the bottom of the south-west wadi, south-west of the centre
            var units = new[]
            {
                Place(new Vector2(62f, -50f)), Place(new Vector2(62f, 50f)),   // 100 m apart down the open east flank
                Place(wadi), Place(wadi + new Vector2(35f, 10f)),              // in the wadi, and someone out on the flat
                Place(wadi + new Vector2(5.5f, 3f)),                           // on the wadi's bank
            };
            yield return null;
            try
            {
                Assert.That(L.WadiDepthAt(wadi), Is.GreaterThan(2f), "the wadi is deep");
                Assert.That(LineOfSight.CanSee(units[0].Leader, units[1].Leader), Is.True, "100 m of open flat");
                Assert.That(LineOfSight.CanSee(units[3].Leader, units[2].Leader), Is.False, "the banks hide the wadi from the flat");
                Assert.That(LineOfSight.CanSee(units[4].Leader, units[2].Leader), Is.True, "but not from the lip");
            }
            finally
            {
                foreach (var u in units) Object.Destroy(u.gameObject);
            }
        }

        [Test]
        public void TheWadiCanBeFollowedFromNearTheDeploymentStripToTheCentre()
        {
            var start = L.WadiPath[0] + new Vector2(2f, 6f);
            var end = L.WadiPath[L.WadiPath.Length - 1];
            var path = Measurer.Measure(Stand(start.x, start.y), Stand(end.x, end.y));
            Assert.That(path.Reachable, Is.True, path.Reason);
            Assert.That(L.WadiDepthAt(L.WadiPoint(1)), Is.GreaterThan(2f));
            Assert.That(L.WadiDepthAt(-L.WadiPoint(1), -1f), Is.GreaterThan(2f), "its twin on the north side");
        }

        [Test]
        public void NoPropIsClippedByTheGround()
        {
            foreach (var prop in Object.FindObjectsByType<TerrainObject>(FindObjectsSortMode.None))
            foreach (var c in prop.GetComponentsInChildren<Collider>())
            {
                var b = c.bounds;
                for (float x = b.min.x; x <= b.max.x; x += 0.25f)
                for (float z = b.min.z; z <= b.max.z; z += 0.25f)
                {
                    if (!c.Raycast(new Ray(new Vector3(x, b.max.y + 1f, z), Vector3.down), out _, b.size.y + 2f)) continue;
                    float ground = L.GroundHeight(x, z);
                    Assert.That(b.max.y - ground, Is.GreaterThan(0.8f * b.size.y), $"{prop.DisplayName} at {prop.transform.position}: buried");
                    Assert.That(ground - b.min.y, Is.GreaterThan(-0.02f), $"{prop.DisplayName} at {prop.transform.position}: floating");
                }
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
        public IEnumerator TheAIPlaysOnTheDesertMap()
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
