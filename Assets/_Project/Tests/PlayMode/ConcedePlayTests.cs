using System.Collections;
using Eldoria.Gameplay;
using Eldoria.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Eldoria.Tests
{
    /// <summary>The HUD's Concede button asks first; conceding ends the battle with the player's defeat.</summary>
    public class ConcedePlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Victory.Reset();
            TestArmies.LoadScene("CubeRoom");
            yield return null;
            yield return null;
        }

        [TearDown]
        public void TearDown() => Victory.Reset();

        [Test]
        public void ConcedingAsksFirstAndKeepPlayingGoesBack()
        {
            var controller = Object.FindFirstObjectByType<MovementController>();
            controller.RequestConcede();
            Assert.That(controller.ConfirmingConcede, Is.True, "a confirmation box");
            Assert.That(Victory.IsOver, Is.False, "nothing has happened yet");
            controller.CancelConcede();
            Assert.That(controller.ConfirmingConcede, Is.False);
            Assert.That(Victory.IsOver, Is.False);
        }

        [Test]
        public void ConfirmingTheConcessionLosesTheBattle()
        {
            var controller = Object.FindFirstObjectByType<MovementController>();
            controller.RequestConcede();
            controller.Concede();
            Assert.That(Victory.Outcome, Is.EqualTo(BattleOutcome.AIWins), "the enemy wins");
            Assert.That(Victory.Conceded, Is.True);
            Assert.That(controller.ConfirmingConcede, Is.False);
            Assert.That(controller.BattleOver, Is.True);
            controller.RequestConcede();
            Assert.That(controller.ConfirmingConcede, Is.False, "no conceding once the battle is decided");
        }

        [UnityTest]
        public IEnumerator ConcedingDuringTheAIsTurnEndsItToo()
        {
            var controller = Object.FindFirstObjectByType<MovementController>();
            controller.EndTurn();   // the AI starts its (watchable) turn
            yield return null;
            controller.RequestConcede();
            controller.Concede();
            Assert.That(Victory.Outcome, Is.EqualTo(BattleOutcome.AIWins));
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(Victory.Outcome, Is.EqualTo(BattleOutcome.AIWins), "still decided");
        }
    }
}
