using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;

namespace Eldoria.Tests
{
    /// <summary>Conceding: the side that concedes loses at once, and the other side wins.</summary>
    public class ConcedeTests
    {
        [SetUp]
        public void SetUp() => Victory.Reset();

        [TearDown]
        public void TearDown() => Victory.Reset();

        [Test]
        public void ThePlayerConcedingLosesTheBattle()
        {
            Assert.That(Victory.Concede(Side.Player), Is.EqualTo(BattleOutcome.AIWins));
            Assert.That((Victory.IsOver, Victory.Conceded, Victory.ObjectiveTaken), Is.EqualTo((true, true, false)));
            Assert.That(Victory.Describe(BattleOutcome.AIWins), Is.EqualTo("Defeat. You conceded."));
            Assert.That(Victory.Describe(BattleOutcome.PlayerWins), Is.EqualTo("Victory! The enemy conceded."), "what the other player sees");
        }

        [Test]
        public void TheOtherSideConcedingWinsTheBattle()
        {
            Assert.That(Victory.Concede(Side.AI), Is.EqualTo(BattleOutcome.PlayerWins));
        }

        [Test]
        public void ADecidedBattleCantBeConceded()
        {
            Victory.Concede(Side.AI);
            Assert.That(Victory.Concede(Side.Player), Is.EqualTo(BattleOutcome.PlayerWins), "already won");
            Victory.Reset();
            Assert.That((Victory.IsOver, Victory.Conceded), Is.EqualTo((false, false)), "a new battle");
        }
    }
}
