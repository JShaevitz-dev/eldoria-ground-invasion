using System;
using System.Linq;
using Eldoria.Units;
using UnityEngine;

namespace Eldoria.Rules
{
    public enum BattleOutcome
    {
        InProgress,
        PlayerWins,
        AIWins,
        /// <summary>Both sides lost their last soldiers at the same moment (e.g. one grenade).</summary>
        Draw
    }

    /// <summary>
    /// The win conditions:
    ///  - A side with no soldiers left loses, and the other side wins. If both sides lose their last soldiers at the same
    ///    moment, it's a draw. Only sides that have units on the battlefield take part.
    ///  - On a map with an <see cref="ObjectiveMarker"/> (e.g. Ft Solstice): if a unit leader of the marker's capturing
    ///    side stands on it when a unit finishes its move, that side wins (<see cref="ObjectiveTaken"/>).
    /// Checked whenever soldiers are removed from the battlefield (the end of a volley or blast, or a death outside combat)
    /// and whenever a unit finishes its move.
    /// </summary>
    public static class Victory
    {
        public static BattleOutcome Outcome { get; private set; } = BattleOutcome.InProgress;
        public static bool IsOver => Outcome != BattleOutcome.InProgress;

        /// <summary>True if the battle was won by taking an objective (rather than by wiping out the enemy).</summary>
        public static bool ObjectiveTaken { get; private set; }

        /// <summary>Raised once, when the battle is decided.</summary>
        public static event Action<BattleOutcome> BattleEnded;

        /// <summary>Living soldiers the side has on the battlefield.</summary>
        public static int SoldiersLeft(Side side) =>
            AllUnits().Where(u => u.Side == side).Sum(u => u.LivingSoldiers.Count());

        /// <summary>Checks whether a side has been wiped out. Returns the outcome (unchanged once decided).</summary>
        public static BattleOutcome Check()
        {
            if (IsOver) return Outcome;
            var units = AllUnits();

            // An objective taken: a unit leader of the capturing side stands on the marker.
            foreach (var marker in UnityEngine.Object.FindObjectsByType<ObjectiveMarker>(FindObjectsSortMode.None))
            {
                var leader = marker.CapturingLeader(units);
                if (leader == null) continue;
                ObjectiveTaken = true;
                Outcome = marker.CapturingSide == Side.Player ? BattleOutcome.PlayerWins : BattleOutcome.AIWins;
                Debug.Log($"[Eldoria] Battle over: {leader.Unit.DisplayName} reached {marker.ObjectiveName}. {Describe(Outcome)}");
                BattleEnded?.Invoke(Outcome);
                return Outcome;
            }

            bool hasPlayer = units.Any(u => u.Side == Side.Player), hasAI = units.Any(u => u.Side == Side.AI);
            bool playerOut = hasPlayer && SoldiersLeft(Side.Player) == 0;
            bool aiOut = hasAI && SoldiersLeft(Side.AI) == 0;

            if (playerOut && aiOut) Outcome = BattleOutcome.Draw;
            else if (aiOut) Outcome = BattleOutcome.PlayerWins;
            else if (playerOut) Outcome = BattleOutcome.AIWins;
            else return Outcome;

            Debug.Log($"[Eldoria] Battle over: {Describe(Outcome)}");
            BattleEnded?.Invoke(Outcome);
            return Outcome;
        }

        public static string Describe(BattleOutcome outcome) => ObjectiveTaken ? outcome switch
        {
            BattleOutcome.PlayerWins => "Victory! Your unit leader reached the objective.",
            BattleOutcome.AIWins => "Defeat. The enemy reached the objective.",
            _ => "The battle goes on."
        } : outcome switch
        {
            BattleOutcome.PlayerWins => "Victory! The enemy has no soldiers left.",
            BattleOutcome.AIWins => "Defeat. You have no soldiers left.",
            BattleOutcome.Draw => "Draw. Neither side has any soldiers left.",
            _ => "The battle goes on."
        };

        /// <summary>A new battle (new game, tests).</summary>
        public static void Reset()
        {
            Outcome = BattleOutcome.InProgress;
            ObjectiveTaken = false;
        }

        private static Unit[] AllUnits() =>
            UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }
}
