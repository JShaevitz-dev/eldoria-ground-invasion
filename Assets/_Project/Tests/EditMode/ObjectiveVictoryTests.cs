using System.Collections.Generic;
using System.Linq;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>The objective win condition: a unit leader of the capturing side standing on an <see cref="ObjectiveMarker"/>.</summary>
    public class ObjectiveVictoryTests
    {
        private readonly List<Object> created = new List<Object>();
        private ObjectiveMarker marker;

        [SetUp]
        public void SetUp()
        {
            Victory.Reset();
            var go = new GameObject("Objective");
            created.Add(go);
            marker = go.AddComponent<ObjectiveMarker>();
            marker.Configure("the test objective", Side.Player, 2.5f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
            Victory.Reset();
        }

        private Unit Spawn(UnitTemplate template, Side side, Vector3 at)
        {
            var unit = UnitFactory.Spawn(template, side, at, Quaternion.identity);
            created.Add(unit.gameObject);
            return unit;
        }

        [Test]
        public void ACapturingLeaderOnTheMarkerWins()
        {
            var attackers = Spawn(TestArmies.InfantrySquad, Side.Player, new Vector3(0f, 0f, -20f));
            Spawn(TestArmies.InfantrySquad, Side.AI, new Vector3(0f, 0f, 20f));
            Assert.That(Victory.Check(), Is.EqualTo(BattleOutcome.InProgress));
            attackers.Leader.transform.position = new Vector3(2.4f, 0f, 0f);   // just inside 2.5 m
            Assert.That(Victory.Check(), Is.EqualTo(BattleOutcome.PlayerWins));
            Assert.That(Victory.ObjectiveTaken, Is.True);
            Assert.That(Victory.Describe(BattleOutcome.PlayerWins), Is.EqualTo("Victory! Your unit leader reached the objective."));
            Assert.That(Victory.Describe(BattleOutcome.AIWins), Is.EqualTo("Defeat. The enemy reached the objective."));
        }

        [Test]
        public void JustOutsideTheMarkerDoesntCount()
        {
            var attackers = Spawn(TestArmies.InfantrySquad, Side.Player, new Vector3(0f, 0f, -20f));
            Spawn(TestArmies.InfantrySquad, Side.AI, new Vector3(0f, 0f, 20f));
            attackers.Leader.transform.position = new Vector3(2.6f, 0f, 0f);
            Assert.That(Victory.Check(), Is.EqualTo(BattleOutcome.InProgress));
        }

        [Test]
        public void TheOtherSidesLeaderAndOrdinarySoldiersDontCount()
        {
            var attackers = Spawn(TestArmies.InfantrySquad, Side.Player, new Vector3(0f, 0f, -20f));
            var defenders = Spawn(TestArmies.InfantrySquad, Side.AI, new Vector3(0f, 0f, 20f));
            defenders.Leader.transform.position = Vector3.zero;
            attackers.Soldiers.First(s => s != attackers.Leader).transform.position = new Vector3(1f, 0f, 0f);
            Assert.That(Victory.Check(), Is.EqualTo(BattleOutcome.InProgress));
        }

        [Test]
        public void ADeadLeaderDoesntCount()
        {
            var attackers = Spawn(TestArmies.FieldCommander, Side.Player, Vector3.zero);
            Spawn(TestArmies.InfantrySquad, Side.AI, new Vector3(0f, 0f, 20f));
            Spawn(TestArmies.InfantrySquad, Side.Player, new Vector3(0f, 0f, -20f));   // the attackers aren't wiped out
            attackers.Leader.Kill();
            Assert.That(Victory.ObjectiveTaken, Is.False);
            Assert.That(Victory.Outcome, Is.EqualTo(BattleOutcome.InProgress));
        }

        [Test]
        public void WithoutAMarkerOnlyWipingOutTheEnemyWins()
        {
            Object.DestroyImmediate(marker.gameObject);
            var attackers = Spawn(TestArmies.InfantrySquad, Side.Player, Vector3.zero);
            Spawn(TestArmies.InfantrySquad, Side.AI, new Vector3(0f, 0f, 20f));
            Assert.That(Victory.Check(), Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(Victory.Describe(BattleOutcome.PlayerWins), Is.EqualTo("Victory! The enemy has no soldiers left."));
        }
    }
}
