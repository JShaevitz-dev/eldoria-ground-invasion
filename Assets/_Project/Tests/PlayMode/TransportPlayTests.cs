using System.Collections;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eldoria.Tests
{
    /// <summary>In battle: a squad boards a transport, then gets out inside it and moves out at once, leader first.</summary>
    public class TransportPlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadBattle()
        {
            Victory.Reset();
            ArmySelection.Set(new[] { TestArmies.FieldCommander, TestArmies.Transport, TestArmies.InfantrySquad },
                new[] { TestArmies.FieldCommander, TestArmies.InfantrySquad });
            SceneManager.LoadScene(Battlefields.EnglishCountryside.Scene);
            yield return null;
            yield return null;
        }

        [TearDown]
        public void CleanUp()
        {
            ArmySelection.Clear();
            Victory.Reset();
        }

        [UnityTest]
        public IEnumerator ASquadBoardsAndGetsOutLeaderFirst()
        {
            var c = Object.FindFirstObjectByType<MovementController>();
            var transport = c.PlayerUnits.Single(u => u.DisplayName == "Transport");
            var squad = c.PlayerUnits.Single(u => u.DisplayName == "Infantry Squad");
            var driver = transport.Leader;
            var embark = driver.Abilities.Single(a => a.transport == TransportAction.Embark);
            var disembark = driver.Abilities.Single(a => a.transport == TransportAction.Disembark);

            // Bring the squad next to the transport.
            var shift = driver.Position + new Vector3(0f, 0f, -3f) - squad.Leader.Position;
            foreach (var s in squad.Soldiers) s.transform.position += shift;

            Assert.That(c.SelectUnit(transport), Is.True);
            c.BeginAbility(driver, embark);
            var r = c.UseAbility(embark, driver, squad.Leader, squad);
            Assert.That(r.Used, Is.True, r.Reason);
            Assert.That(squad.IsEmbarked, Is.True);
            Assert.That(c.SelectUnit(squad), Is.False, "a unit aboard can't be commanded");
            Assert.That(c.UnitsThatCanStillMove().Contains(squad), Is.False, "a unit aboard has nothing to move");

            c.EndTurn(instant: true);
            yield return null;
            Assert.That(Victory.IsOver, Is.False);
            Assert.That(squad.IsEmbarked, Is.True, "still aboard on the next turn");

            Assert.That(c.SelectUnit(transport), Is.True);
            c.BeginAbility(driver, disembark);   // no target: used at once
            Assert.That(squad.IsEmbarked, Is.False);
            Assert.That(c.HasTakenAction, Is.False, "the squad is now the unit being commanded");
            Assert.That(c.OrderHold(), Is.False, "it can't stay inside");
            Assert.That(c.CancelSelection(), Is.False);

            Assert.That(c.Activation, Is.Not.Null);
            Assert.That(c.Activation.Unit, Is.SameAs(squad), "the squad that got out is being commanded");
            Assert.That(c.Activation.Phase, Is.EqualTo(MovePhase.MoveLeader));
            var leaderTarget = driver.Position + new Vector3(6f, 0f, 0f);
            var check = c.Activation.CheckLeaderMove(leaderTarget);
            Assert.That(check.Ok, Is.True, check.Reason);
            Assert.That(c.OrderLeaderMove(leaderTarget), Is.True, "the leader moves out first");
            yield return null;
            Assert.That(c.OrderEndMove(), Is.False, "everyone else is still inside");
            c.OrderGather();
            Assert.That(c.OrderEndMove(), Is.True, "everyone has got out");
            foreach (var s in squad.LivingSoldiers)
                Assert.That(Measure.Between(s, driver), Is.GreaterThanOrEqualTo(driver.Radius + s.Radius - 0.05f), $"{s.name} is out of the transport");
            Assert.That(squad.DisembarkingFrom, Is.Null);
        }
    }
}
