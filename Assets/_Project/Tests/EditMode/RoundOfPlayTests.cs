using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;

namespace Eldoria.Tests
{
    /// <summary>A round of play starts at the start of the first player's turn and lasts through the other player's turn.</summary>
    public class RoundOfPlayTests
    {
        [SetUp]
        public void SetUp() => Turns.ResetCounter();

        [TearDown]
        public void TearDown() => Turns.ResetCounter();

        [Test]
        public void ARoundStartsWithTheFirstPlayersTurn()
        {
            Assert.That(Turns.FirstSide, Is.EqualTo(Side.Player), "the player (or the south player in multiplayer) moves first");
            Assert.That(Turns.Round, Is.EqualTo(0), "before the battle");

            Turns.StartNextTurn();
            Assert.That((Turns.Round, Turns.ActiveSide), Is.EqualTo((1, Side.Player)), "round 1 starts with the first player's turn");

            Turns.StartSideTurn(Side.AI);
            Assert.That((Turns.Round, Turns.ActiveSide), Is.EqualTo((1, Side.AI)), "the second player's turn is still round 1");

            Turns.StartNextTurn();
            Assert.That((Turns.Round, Turns.ActiveSide), Is.EqualTo((2, Side.Player)), "round 2 starts with the first player's next turn");
            Assert.That(Turns.Round, Is.EqualTo(Turns.Current));
        }
    }
}
