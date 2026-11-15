using System.Collections;
using System.Linq;
using Eldoria.Core;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Siege of Ft Solstice: 100 m wide × 130 m deep of desert. The south side (attackers) deploys on the open sand
    /// (z -65..-57), 42 m from the fortress; the north side (defenders) inside it. Three lanes lead through the outer wall
    /// (z -15) and the middle wall (z 9) to the keep (x ±12, z 21..45), with the objective in its centre (0, 33). An
    /// attacker's unit leader on the objective wins.
    /// </summary>
    public class FortSolsticePlayTests
    {
        private static readonly Vector3 Objective = new Vector3(0f, 0f, 33f);

        [UnitySetUp]
        public IEnumerator LoadFort()
        {
            Victory.Reset();
            ArmySelection.Set(new[] { TestArmies.FieldCommander, TestArmies.InfantrySquad, TestArmies.InfantrySquad },
                new[] { TestArmies.FieldCommander, TestArmies.InfantrySquad, TestArmies.InfantrySquad });
            SceneManager.LoadScene(Battlefields.FortSolstice.Scene);
            yield return null;
            yield return null;
        }

        [TearDown]
        public void CleanUp()
        {
            ArmySelection.Clear();
            Victory.Reset();
        }

        private static IMoveMeasurer Measurer => BattlefieldNavigation.Measurer;

        [Test]
        public void TheMapIs100By130MetresOfDesert()
        {
            Assert.That((BattlefieldScale.BoardWidth, BattlefieldScale.BoardDepth), Is.EqualTo((100f, 130f)));
            Assert.That(MovementRules.IsInsideBattlefield(new Vector3(48f, 0f, -63f), 0.35f), Is.True, "30 m more to the south");
            Assert.That(MovementRules.IsInsideBattlefield(new Vector3(48f, 0f, 63f), 0.35f), Is.True);
            Assert.That(MovementRules.IsInsideBattlefield(new Vector3(51f, 0f, 0f), 0.35f), Is.False);
            Assert.That(MovementRules.IsInsideBattlefield(new Vector3(0f, 0f, -66f), 0.35f), Is.False);
            Assert.That(Battlefields.All, Does.Contain(Battlefields.FortSolstice));
        }

        [Test]
        public void TheAttackersStartOutsideTheFortressAndTheDefendersInside()
        {
            var zones = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None);
            var attackers = zones.Single(z => z.Side == Side.Player);
            var defenders = zones.Single(z => z.Side == Side.AI);
            Assert.That(attackers.transform.position.z - attackers.Size.y / 2f, Is.EqualTo(-65f).Within(0.01f), "along the south edge");
            Assert.That(-15f - (attackers.transform.position.z + attackers.Size.y / 2f), Is.GreaterThanOrEqualTo(40f),
                "at least 40 m of open sand before the outer wall");
            Assert.That(defenders.transform.position.z - defenders.Size.y / 2f, Is.GreaterThan(45f), "north of the keep");
            Assert.That(defenders.transform.position.z + defenders.Size.y / 2f, Is.LessThan(55f), "inside the middle wall");
            Assert.That(defenders.Size.x / 2f, Is.LessThan(30f), "inside the middle wall");

            foreach (var unit in Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
            foreach (var s in unit.Soldiers)
            {
                var zone = unit.Side == Side.Player ? attackers : defenders;
                Assert.That(zone.Contains(s.Position), Is.True, $"{unit.DisplayName}: {s.name} at {s.Position}");
                Assert.That(Measurer.TryGetStandPoint(s.Position, s.Radius, s.Height, out _, out var why), Is.True, $"{s.name}: {why}");
            }
        }

        [Test]
        public void TheDefendersMayDeployAnywhereInTheFortress()
        {
            var defenders = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None).Single(z => z.Side == Side.AI);
            foreach (var p in new[] { new Vector3(0f, 0f, 33f), new Vector3(-28f, 0f, 0f), new Vector3(0f, 0f, -14f), new Vector3(40f, 0f, -10f),
                                      new Vector3(-25f, 0f, 50f), new Vector3(20f, 0f, 15f) })
                Assert.That(defenders.Contains(p), Is.True, $"inside the fortress: {p}");
            foreach (var p in new[] { new Vector3(0f, 0f, -20f), new Vector3(36f, 0f, 20f), new Vector3(-36f, 0f, 40f), new Vector3(0f, 0f, 58f) })
                Assert.That(defenders.Contains(p), Is.False, $"outside the fortress: {p}");
        }

        private static Vector3 Stand(float x, float z)
        {
            Assert.That(Measurer.TryGetStandPoint(new Vector3(x, 0f, z), 0.35f, 1.8f, out var stand, out var why), Is.True, $"({x}, {z}): {why}");
            return stand;
        }

        [Test]
        public void SoldiersCanManTheWallsFromTheWallWalks()
        {
            // Behind the outer wall (5 m) the walk is 4.2 m up; behind the middle wall (4 m), 3.2 m.
            Assert.That(Stand(-20f, -13.2f).y, Is.EqualTo(4.2f).Within(0.05f), "outer south");
            Assert.That(Stand(-40.15f, -8f).y, Is.EqualTo(4.2f).Within(0.05f), "outer west");
            Assert.That(Stand(-36f, 7.15f).y, Is.EqualTo(4.2f).Within(0.05f), "outer north-west");
            Assert.That(Stand(15f, 10.75f).y, Is.EqualTo(3.2f).Within(0.05f), "middle south");
            Assert.That(Stand(-28.25f, 20f).y, Is.EqualTo(3.2f).Within(0.05f), "middle west");
            Assert.That(Stand(28.25f, 40f).y, Is.EqualTo(3.2f).Within(0.05f), "middle east");
            Assert.That(Measurer.TryGetStandPoint(new Vector3(-20f, 0f, -15f), 0.35f, 1.8f, out _, out _), Is.False, "not on the wall itself");

            // Ramps lead up from the ground.
            foreach (var (from, to) in new[]
            {
                (new Vector3(-20f, 0f, -3f), Stand(-20f, -13.2f)),
                (new Vector3(-28f, 0f, -8f), Stand(-40.15f, -8f)),
                (new Vector3(-28f, 0f, -8f), Stand(-36f, 7.15f)),
                (new Vector3(0f, 0f, 15f), Stand(15f, 10.75f)),
                (new Vector3(-20f, 0f, 30f), Stand(-28.25f, 20f)),
                (new Vector3(20f, 0f, 30f), Stand(28.25f, 40f)),
            })
            {
                var path = Measurer.Measure(Stand(from.x, from.z), to);
                Assert.That(path.Reachable, Is.True, $"{from} to {to}: {path.Reason}");
            }
        }

        [Test]
        public void FromTheWallWalkADefenderSeesOverTheWallAndTheBattlementsCoverThem()
        {
            var defender = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).First(u => u.Side == Side.AI && u.Soldiers.Count > 1).Soldiers[1];
            var attacker = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).First(u => u.Side == Side.Player && u.Soldiers.Count > 1).Soldiers[1];
            defender.transform.position = Stand(-20f, -13.2f);
            attacker.transform.position = new Vector3(-20f, 0f, -35f);
            Assert.That(ShotGeometry.CanSee(defender, attacker), Is.True, "the defender sees over the wall");
            Assert.That(ShotGeometry.CanSee(attacker, defender), Is.True, "and can be shot at");

            NearbyKeywords.RefreshAll();
            Assert.That(defender.HasKeyword("In half-cover"), Is.True, "behind the battlements");
            var outcome = KeywordRules.Evaluate(attacker, attacker.WeaponFor(defender), defender);
            Assert.That(outcome.AccuracyModifiers.Any(m => m.Type == AccuracyModifierType.Penalty && m.Value == 25), Is.True, "−25% to hit them");

            // On the ground behind the wall, nobody sees anyone.
            defender.transform.position = new Vector3(-20f, 0f, -11f);
            Assert.That(ShotGeometry.CanSee(defender, attacker), Is.False);
            Assert.That(ShotGeometry.CanSee(attacker, defender), Is.False);
        }

        [UnityTest]
        public IEnumerator ABigDefendingArmyDeploysInsideTheFortressNotOnTheWalls()
        {
            // 13 units: two rows of six and one more. The rows behind the courtyard would land on or beyond the north wall;
            // those units take the nearest clear spots inside the fortress instead.
            var big = new[] { TestArmies.FieldCommander }.Concat(Enumerable.Repeat(TestArmies.InfantrySquad, 12)).ToArray();
            ArmySelection.Set(new[] { TestArmies.FieldCommander, TestArmies.InfantrySquad }, big);
            SceneManager.LoadScene(Battlefields.FortSolstice.Scene);
            yield return null;
            yield return null;
            var defenders = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None).Single(z => z.Side == Side.AI);
            var soldiers = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).Where(u => u.Side == Side.AI).SelectMany(u => u.Soldiers).ToList();
            Assert.That(soldiers.Count, Is.EqualTo(1 + 12 * TestArmies.InfantrySquad.Size));
            foreach (var s in soldiers)
            {
                Assert.That(defenders.Contains(s.Position), Is.True, $"{s.Unit.DisplayName}: {s.name} at {s.Position}");
                Assert.That(s.Position.y, Is.LessThan(0.1f), $"{s.Unit.DisplayName}: {s.name} on the ground at {s.Position}");
                Assert.That(Measurer.TryGetStandPoint(s.Position, s.Radius, s.Height, out _, out var why), Is.True, $"{s.Unit.DisplayName}: {s.name}: {why}");
                foreach (var o in soldiers)
                    if (o != s) Assert.That(Measure.Horizontal(o.Position, s.Position), Is.GreaterThanOrEqualTo(o.Radius + s.Radius - 0.01f), $"{s.name} and {o.name}");
            }
        }

        [Test]
        public void ThereIsAnObjectiveInTheCentreOfTheKeepForTheAttackers()
        {
            var marker = Object.FindObjectsByType<ObjectiveMarker>(FindObjectsSortMode.None).Single();
            Assert.That(Measure.Horizontal(marker.transform.position, Objective), Is.LessThan(0.01f));
            Assert.That(marker.CapturingSide, Is.EqualTo(Side.Player), "the side that starts outside");
            Assert.That(marker.Radius, Is.EqualTo(2.5f));
            Assert.That(Measurer.TryGetStandPoint(Objective, 0.35f, 1.8f, out _, out var why), Is.True, why);
        }

        [Test]
        public void EachOfTheThreeLanesLeadsToTheObjective()
        {
            foreach (float gate in new[] { -28f, 0f, 28f })
            {
                var start = new Vector3(gate, 0f, -30f);
                var path = Measurer.Measure(start, Objective, 0.35f);
                Assert.That(path.Reachable, Is.True, $"from the {gate} gate: {path.Reason}");
                Assert.That(path.Length, Is.GreaterThan(Measure.Horizontal(start, Objective) - 0.01f));
            }
            var tank = Measurer.Measure(new Vector3(0f, 0f, -55f), Objective, 1.8f);
            Assert.That(tank.Reachable, Is.True, $"the gates fit a tank: {tank.Reason}");
            var fromDefenders = Measurer.Measure(new Vector3(0f, 0f, 50f), Objective, 0.35f);
            Assert.That(fromDefenders.Reachable, Is.True, $"the defenders can reach the keep: {fromDefenders.Reason}");
        }

        [Test]
        public void TheWallsBlockTheWayExceptAtTheGates()
        {
            // Straight through the outer wall between the gates: no way in there, so the route goes round by a gate.
            var start = new Vector3(-14f, 0f, -19f);
            var inside = new Vector3(-10f, 0f, -11f);   // just inside the outer wall, in the centre lane
            // The walls are thick enough to stand on, but nobody can get up there: no route climbs a 5 m wall.
            if (Measurer.TryGetStandPoint(new Vector3(-14f, 0f, -15f), 0.35f, 1.8f, out var wallTop, out _))
            {
                Assert.That(wallTop.y, Is.GreaterThan(4f), "the top of the outer wall");
                Assert.That(Measurer.Measure(start, wallTop, 0.35f).Reachable, Is.False, "the wall top can't be reached");
            }
            var path = Measurer.Measure(start, inside, 0.35f);
            Assert.That(path.Reachable && path.Length > 12f, Is.True, "round through a gate, not through the wall");
        }

        [Test]
        public void AnAttackersUnitLeaderOnTheObjectiveWinsTheBattle()
        {
            var attacker = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).First(u => u.Side == Side.Player && u.Soldiers.Count > 1);
            var defender = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).First(u => u.Side == Side.AI);

            // A defender's leader on the objective doesn't win (or lose) anything.
            defender.Leader.transform.position = Objective;
            Assert.That(Victory.Check(), Is.EqualTo(BattleOutcome.InProgress));
            defender.Leader.transform.position = Objective + new Vector3(0f, 0f, 8f);

            // An attacker who isn't the unit leader doesn't count either.
            var follower = attacker.Soldiers.First(s => s != attacker.Leader);
            follower.transform.position = Objective + new Vector3(1f, 0f, 0f);
            Assert.That(Victory.Check(), Is.EqualTo(BattleOutcome.InProgress));

            attacker.Leader.transform.position = Objective + new Vector3(0f, 0f, -2f);   // on the 2.5 m marker
            Assert.That(Victory.Check(), Is.EqualTo(BattleOutcome.PlayerWins));
            Assert.That(Victory.ObjectiveTaken, Is.True);
            Assert.That(Victory.Describe(BattleOutcome.PlayerWins), Does.Contain("objective"));
        }

        [Test]
        public void WipingOutTheDefendersStillWinsTheBattle()
        {
            foreach (var unit in Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
                if (unit.Side == Side.AI)
                    foreach (var s in unit.Soldiers) s.Kill();
            Assert.That(Victory.Outcome, Is.EqualTo(BattleOutcome.PlayerWins), "the attackers win without reaching the objective");
            Assert.That(Victory.ObjectiveTaken, Is.False);
            Assert.That(Victory.Describe(BattleOutcome.PlayerWins), Does.Contain("no soldiers left"));
        }

        [Test]
        public void WipingOutTheAttackersStillWinsTheBattle()
        {
            foreach (var unit in Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
                if (unit.Side == Side.Player)
                    foreach (var s in unit.Soldiers) s.Kill();
            Assert.That(Victory.Outcome, Is.EqualTo(BattleOutcome.AIWins), "the defenders win by wiping out the attackers");
            Assert.That(Victory.ObjectiveTaken, Is.False);
            Assert.That(Victory.Describe(BattleOutcome.AIWins), Does.Contain("no soldiers left"));
        }

        [Test]
        public void ConcedingWorksOnTheFortToo()
        {
            var controller = Object.FindFirstObjectByType<MovementController>();
            controller.RequestConcede();
            controller.Concede();
            Assert.That(Victory.Outcome, Is.EqualTo(BattleOutcome.AIWins));
        }

        [UnityTest]
        public IEnumerator TheAIPlaysOnTheFort()
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
