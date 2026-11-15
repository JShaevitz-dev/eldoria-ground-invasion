using System.Collections;
using System.Linq;
using Eldoria.Core;
using Eldoria.Gameplay;
using Eldoria.Network;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eldoria.Tests
{
    /// <summary>
    /// A battle from the menu or a lobby starts with deployment: each player places their units in their deployment zone
    /// without seeing the other's, then presses Ready. The turn timer applies to deployment too.
    /// </summary>
    public class DeploymentPlayTests
    {
        private bool savedEnabled;
        private float savedMinutes;

        [SetUp]
        public void SaveTimer()
        {
            savedEnabled = TurnTimerSettings.Enabled;
            savedMinutes = TurnTimerSettings.Minutes;
            TurnTimerSettings.Enabled = false;
        }

        [TearDown]
        public void CleanUp()
        {
            TurnTimerSettings.Enabled = savedEnabled;
            TurnTimerSettings.Minutes = savedMinutes;
            MultiplayerSession.Leave();
            ArmySelection.Clear();
            Victory.Reset();
        }

        private static MovementController Controller => Object.FindFirstObjectByType<MovementController>();
        private static Unit[] Units(System.Func<Unit, bool> which) => Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).Where(which).ToArray();

        private static IEnumerator StartSinglePlayer()
        {
            Victory.Reset();
            var army = new[] { TestArmies.FieldCommander, TestArmies.InfantrySquad };
            ArmySelection.Set(army, army);
            ArmySelection.DeploymentPhase = true;   // as the menu's Play does
            SceneManager.LoadScene("Countryside");
            yield return null;
            yield return null;
        }

        private static IEnumerator StartMultiplayer(Side localSide)
        {
            MultiplayerSession.Leave();
            Victory.Reset();
            var army = new[] { TestArmies.FieldCommander, TestArmies.InfantrySquad };
            ArmySelection.Set(army, army);
            ArmySelection.LocalSide = localSide;
            ArmySelection.DeploymentPhase = true;   // as a lobby's start does
            GameRandom.Seed(99);
            MultiplayerSession.Offline(localSide);
            SceneManager.LoadScene("Countryside");
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheBattleStartsWithDeploymentAndTheEnemyHidden()
        {
            yield return StartSinglePlayer();
            var c = Controller;
            Assert.That(c.IsDeploying, Is.True);
            var enemy = Units(u => u.Side == Side.AI);
            Assert.That(enemy, Is.Not.Empty);
            foreach (var u in enemy)
            {
                Assert.That(u.Hidden, Is.True, u.DisplayName);
                Assert.That(u.GetComponentsInChildren<Renderer>().Any(r => r.enabled), Is.False, $"{u.DisplayName} isn't drawn");
                Assert.That(u.GetComponentsInChildren<Collider>().Any(col => col.enabled), Is.False, $"{u.DisplayName} can't be clicked");
            }
            Assert.That(Units(u => u.Side == Side.Player).All(u => !u.Hidden), Is.True, "the player's own units are shown");

            // No battle orders yet.
            Assert.That(c.SelectUnit(c.PlayerUnits[0]), Is.False);
            c.EndTurn(instant: true);
            Assert.That(c.IsDeploying, Is.True);

            c.ReadyDeployment();
            Assert.That(c.IsDeploying, Is.False);
            Assert.That(Turns.Current, Is.EqualTo(1));
            Assert.That(Units(u => true).All(u => !u.Hidden), Is.True, "everyone is revealed");
            Assert.That(Units(u => u.Side == Side.AI).All(u => u.GetComponentsInChildren<Renderer>().Any(r => r.enabled)), Is.True);
            Assert.That(c.SelectUnit(c.PlayerUnits[0]), Is.True, "the first turn has begun");
        }

        [UnityTest]
        public IEnumerator UnitsArePlacedOnlyInsideTheDeploymentZone()
        {
            yield return StartSinglePlayer();
            var c = Controller;
            var zone = c.DeploymentArea;
            Assert.That(zone, Is.Not.Null);
            var squad = c.PlayerUnits.First(u => u.Soldiers.Count > 1);
            var zc = zone.transform.position;

            // Its leader on the empty right of the zone: the whole squad forms up there, inside the zone, facing the enemy.
            var spot = new Vector3(zc.x + 25f, 0f, zc.z + 1f);
            Assert.That(c.DeployUnitAt(squad, spot, out string why), Is.True, why);
            Assert.That(Measure.Horizontal(squad.Leader.Position, spot), Is.LessThan(0.01f));
            foreach (var s in squad.Soldiers) Assert.That(zone.Contains(s.Position), Is.True, s.name);

            // Out of the zone: refused, and the squad stays put.
            var before = squad.Soldiers.Select(s => s.Position).ToList();
            Assert.That(c.DeployUnitAt(squad, zc + new Vector3(0f, 0f, 20f), out why), Is.False);
            Assert.That(why, Does.Contain("deployment zone"));
            Assert.That(squad.Soldiers.Select(s => s.Position), Is.EqualTo(before));

            // On top of another of the player's units: refused.
            var other = c.PlayerUnits.First(u => u != squad);
            Assert.That(c.DeployUnitAt(other, squad.Leader.Position, out why), Is.False);
            Assert.That(why, Does.Contain("in the way"));

            // Not the enemy's units.
            Assert.That(c.DeployUnitAt(Units(u => u.Side == Side.AI).First(), spot, out _), Is.False);
        }

        [UnityTest]
        public IEnumerator TheTurnTimerAppliesToDeployment()
        {
            TurnTimerSettings.Enabled = true;
            TurnTimerSettings.Minutes = 0.01f;   // 0.6 s
            yield return StartSinglePlayer();
            var c = Controller;
            Assert.That(c.IsDeploying, Is.True);
            Assert.That(c.DeploymentTimeLeft, Is.Not.Null.And.GreaterThan(0f));
            float until = Time.realtimeSinceStartup + 5f;
            while (c.IsDeploying && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(c.IsDeploying, Is.False, "time ran out: the units deploy where they stand");
            Assert.That(Turns.Current, Is.EqualTo(1));
            Assert.That(c.TurnTimeLeft, Is.Not.Null, "and the first turn has its own timer");
        }

        [UnityTest]
        public IEnumerator MultiplayerWaitsForBothPlayersAndThenShowsWhereTheOpponentDeployed()
        {
            yield return StartMultiplayer(Side.Player);
            var c = Controller;
            var session = MultiplayerSession.Active;
            Assert.That(c.IsDeploying, Is.True);
            var theirs = Units(u => u.Side == Side.AI);
            Assert.That(theirs.All(u => u.Hidden), Is.True, "the opponent's army is hidden");

            c.ReadyDeployment();
            Assert.That((c.IsDeploying, c.DeploymentReady), Is.EqualTo((true, true)), "waiting for the opponent");
            var sent = session.Sent.Last();
            Assert.That(sent.kind, Is.EqualTo(OrderKinds.Deploy));
            Assert.That(sent.placements.Count, Is.EqualTo(5 * c.PlayerUnits.Sum(u => u.Soldiers.Count)), "every soldier's position");
            Assert.That(theirs.All(u => u.Hidden), Is.True, "still hidden while waiting");

            // The opponent deployed 2 m further back than their default slots.
            var order = new OrderMsg { kind = OrderKinds.Deploy, checksum = 12345 };
            foreach (var u in theirs)
                for (int i = 0; i < u.Soldiers.Count; i++)
                {
                    var p = u.Soldiers[i].Position + new Vector3(0f, 0f, 2f);
                    order.placements.AddRange(new[] { u.NetId, i, System.BitConverter.SingleToInt32Bits(p.x),
                        System.BitConverter.SingleToInt32Bits(p.y), System.BitConverter.SingleToInt32Bits(p.z) });
                }
            var expected = theirs.SelectMany(u => u.Soldiers).ToDictionary(s => s, s => s.Position + new Vector3(0f, 0f, 2f));
            session.Inject(order);
            yield return null;
            yield return null;

            Assert.That(c.IsDeploying, Is.False, "both ready: the battle begins");
            Assert.That(theirs.All(u => !u.Hidden), Is.True, "and the opponent's army is revealed");
            foreach (var kv in expected) Assert.That(Measure.Horizontal(kv.Key.Position, kv.Value), Is.LessThan(0.001f), kv.Key.name);
            Assert.That(session.Desyncs, Is.EqualTo(0));
            Assert.That(c.IsRemoteTurn, Is.False, "south moves first");
        }

        [UnityTest]
        public IEnumerator AnOpponentWhoIsReadyFirstStaysHiddenUntilThisPlayerIsReady()
        {
            yield return StartMultiplayer(Side.AI);
            var c = Controller;
            var session = MultiplayerSession.Active;
            var theirs = Units(u => u.Side == Side.Player);
            session.Inject(new OrderMsg { kind = OrderKinds.Deploy });   // ready, with their default slots
            yield return null;
            yield return null;
            Assert.That(c.IsDeploying, Is.True, "this player is still deploying");
            Assert.That(theirs.All(u => u.Hidden), Is.True);

            c.ReadyDeployment();
            Assert.That(c.IsDeploying, Is.False);
            Assert.That(theirs.All(u => !u.Hidden), Is.True);
            Assert.That(c.IsRemoteTurn, Is.True, "north: the opponent (south) moves first");
            Assert.That(session.Desyncs, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator OnTheFortTheDefendersDeployAnywhereInsideIncludingOnTheWalls()
        {
            MultiplayerSession.Leave();
            Victory.Reset();
            var army = new[] { TestArmies.FieldCommander, TestArmies.InfantrySquad };
            ArmySelection.Set(army, army);
            ArmySelection.LocalSide = Side.AI;   // the defenders
            ArmySelection.DeploymentPhase = true;
            MultiplayerSession.Offline(Side.AI);
            SceneManager.LoadScene(Battlefields.FortSolstice.Scene);
            yield return null;
            yield return null;
            var c = Controller;
            Assert.That(c.IsDeploying, Is.True);
            var commander = c.PlayerUnits.First(u => u.Soldiers.Count == 1);
            Assert.That(c.DeployUnitAt(commander, new Vector3(-20f, 0f, -13.2f), out string why), Is.True, "on the outer wall walk: " + why);
            Assert.That(commander.Leader.Position.y, Is.EqualTo(4.2f).Within(0.05f));
            Assert.That(c.DeployUnitAt(commander, new Vector3(-28f, 0f, 0f), out why), Is.True, "in a lane: " + why);
            Assert.That(c.DeployUnitAt(commander, new Vector3(0f, 0f, 38f), out why), Is.True, "in the keep: " + why);
            Assert.That(c.DeployUnitAt(commander, new Vector3(0f, 0f, -25f), out why), Is.False, "not outside the walls");
        }

        [UnityTest]
        public IEnumerator ConcedingDuringDeploymentEndsTheBattleAndRevealsEveryone()
        {
            yield return StartSinglePlayer();
            var c = Controller;
            c.Concede();
            Assert.That(Victory.IsOver, Is.True);
            Assert.That(c.IsDeploying, Is.False);
            Assert.That(Units(u => true).All(u => !u.Hidden), Is.True);
        }
    }
}
