using System.Collections;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using L = Eldoria.Terrain.JungleLayout;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Untamed map: 100 × 100 m of dense jungle with 180° symmetry. Thickets block sight and movement; trails and
    /// clearings are the open ground, wide enough for a tank.
    /// </summary>
    public class JunglePlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadMap()
        {
            ArmySelection.Clear();
            TestArmies.LoadScene(Battlefields.Untamed.Scene);
            yield return null;
            yield return null;
        }

        private static IMoveMeasurer Measurer => BattlefieldNavigation.Measurer;

        private static Vector3 Stand(Vector2 p, float radius = 0.35f)
        {
            Assert.That(Measurer.TryGetStandPoint(new Vector3(p.x, 0f, p.y), radius, 1.8f, out var at, out var why), Is.True, $"{p}: {why}");
            return at;
        }

        [Test]
        public void TheMapIs100By100WithAZoneAlongEachEdge()
        {
            Assert.That(Battlefields.All, Does.Contain(Battlefields.Untamed));
            Assert.That(Battlefields.Untamed.Name, Is.EqualTo("Untamed"));
            Assert.That((Core.BattlefieldScale.BoardWidth, Core.BattlefieldScale.BoardDepth), Is.EqualTo((100f, 100f)));
            var zones = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None);
            Assert.That(zones.Single(z => z.Side == Side.Player).transform.position.z, Is.EqualTo(-45f));
            Assert.That(zones.Single(z => z.Side == Side.AI).transform.position.z, Is.EqualTo(45f));
        }

        [Test]
        public void TheJungleIsDenseAndSymmetrical()
        {
            var thickets = Object.FindObjectsByType<StructureTerrain>(FindObjectsSortMode.None).Where(s => s.Kind == StructureKind.Thicket).ToList();
            Assert.That(thickets.Count, Is.EqualTo(L.Thickets.Count));
            Assert.That(thickets.Count, Is.GreaterThan(300), "dense");
            foreach (var t in thickets)
            {
                var p = t.transform.position;
                Assert.That(thickets.Any(o => Measure.Horizontal(o.transform.position, new Vector3(-p.x, 0f, -p.z)) < 0.05f), Is.True, $"twin of {p}");
            }
            for (float x = -48f; x <= 48f; x += 6f)
            for (float z = -48f; z <= 48f; z += 6f)
                Assert.That(L.GroundHeight(x, z), Is.EqualTo(L.GroundHeight(-x, -z)).Within(1e-4f));

            // Roughly half the ground between the deployment strips is undergrowth.
            int covered = 0, total = 0;
            for (float x = -49f; x <= 49f; x += 1f)
            for (float z = -39f; z <= 39f; z += 1f)
            {
                total++;
                if (L.Thickets.Any(t => Vector2.Distance(t.Centre, new Vector2(x, z)) < t.Radius)) covered++;
            }
            Assert.That(covered / (float)total, Is.GreaterThan(0.4f));
        }

        [UnityTest]
        public IEnumerator ThicketsBlockSightButTrailsAndClearingsAreOpen()
        {
            var one = TestArmies.FieldCommander;
            Unit Place(Vector2 p) => UnitFactory.Spawn(one, Side.Player, Stand(p), Quaternion.identity);
            // Find a thicket with open ground on two opposite sides of it.
            bool Open(Vector2 p) => Mathf.Abs(p.x) < 48f && Mathf.Abs(p.y) < 39f && L.Thickets.All(o => Vector2.Distance(o.Centre, p) > o.Radius + 0.8f);
            Vector2 a = default, b = default;
            bool found = false;
            foreach (var k in L.Thickets)
            {
                for (int d = 0; d < 8 && !found; d++)
                {
                    var dir = new Vector2(Mathf.Cos(d * Mathf.PI / 4f), Mathf.Sin(d * Mathf.PI / 4f));
                    for (float gap = 1.2f; gap <= 3f && !found; gap += 0.6f)
                    {
                        a = k.Centre + dir * (k.Radius + gap);
                        b = k.Centre - dir * (k.Radius + gap);
                        found = Open(a) && Open(b);
                    }
                }
                if (found) break;
            }
            Assert.That(found, Is.True, "a thicket with open ground on both sides");
            var units = new[]
            {
                Place(a), Place(b),                                                                                       // either side of a thicket
                Place(new Vector2(-6f, 0f)), Place(new Vector2(6f, 0f)),                                                  // across the central clearing
            };
            yield return null;
            try
            {
                Assert.That(LineOfSight.CanSee(units[0].Leader, units[1].Leader), Is.False, "the thicket is in the way");
                Assert.That(LineOfSight.CanSee(units[2].Leader, units[3].Leader), Is.True, "the clearing is open");
            }
            finally
            {
                foreach (var u in units) Object.Destroy(u.gameObject);
            }
        }

        [Test]
        public void ATankCanFollowEachTrailToTheCentreButNotPushThroughTheUndergrowth()
        {
            foreach (var trail in L.Trails.Take(3))
            {
                var start = trail[0] + new Vector2(0f, 3f);   // just out of the deployment strip
                var path = Measurer.Measure(Stand(start, 1.8f), Stand(Vector2.zero, 1.8f), 1.8f);
                Assert.That(path.Reachable, Is.True, $"from {start}: {path.Reason}");
            }
            var thicket = L.Thickets[0];
            Assert.That(Measurer.TryGetStandPoint(new Vector3(thicket.Centre.x, 0f, thicket.Centre.y), 0.35f, 1.8f, out _, out var why), Is.False);
            Assert.That(why, Is.Not.Empty);
        }

        [Test]
        public void ThePropsGiveCoverAndSitClearOfTheThickets()
        {
            var objects = Object.FindObjectsByType<TerrainObject>(FindObjectsSortMode.None);
            void Check(string name, string cover, int count)
            {
                var found = objects.Where(o => o.DisplayName == name).ToList();
                Assert.That(found.Count, Is.EqualTo(count), name);
                Assert.That(found.All(o => o.HasKeyword(cover)), Is.True, $"{name}: {cover}");
            }
            Check("Ancient Shrine", "Full-cover", 2);
            Check("Boulder", "Full-cover", 6);
            Check("Fallen Tree", "Half-cover", 4);
            Check("Ferns", "Half-cover", 10);
            foreach (var o in objects)
            {
                var p = new Vector2(o.transform.position.x, o.transform.position.z);
                Assert.That(L.Thickets.All(t => Vector2.Distance(t.Centre, p) > t.Radius), Is.True, $"{o.DisplayName} at {p}");
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
        public IEnumerator TheAIPlaysInTheJungle()
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
