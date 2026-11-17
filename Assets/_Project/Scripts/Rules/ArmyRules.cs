using System.Collections.Generic;
using System.Linq;
using Eldoria.Units;

namespace Eldoria.Rules
{
    /// <summary>
    /// Army list building (Docs/Rules.md, Army lists):
    ///   - the player picks a faction first; every unit must be one that faction has access to;
    ///   - the units' Supply Costs add up to no more than the Supply Cap (<see cref="SupplyCap"/>, 750); there's no
    ///     limit on how many units;
    ///   - exactly 1 Commander unit (at least one, and no more than one);
    ///   - every other unit (Infantry, Heavy Support and Specialists) can be taken more than once;
    ///   - a unit with required Commanders (<see cref="UnitTemplate.onlyWithCommanders"/>) can only be taken when one of
    ///     them is in the army (the army builder hides it until then).
    /// </summary>
    public static class ArmyRules
    {
        /// <summary>The most an army's units' Supply Costs can add up to.</summary>
        public const int SupplyCap = 750;

        /// <summary>The Supply Cost of an army list.</summary>
        public static int SupplyOf(IEnumerable<UnitTemplate> army) =>
            (army ?? Enumerable.Empty<UnitTemplate>()).Where(u => u != null).Sum(u => u.supplyCost);

        /// <summary>Only one of each unit with this role per army (only Commanders: an army has exactly one).</summary>
        public static bool IsUnique(UnitRole role) => role == UnitRole.Commander;

        /// <summary>Soldier keyword: only one of a unit with it may appear in an army list.</summary>
        public const string UniqueKeyword = "Unique";

        /// <summary>A unit with the Unique keyword (on any of its soldiers): only one of it per army, whatever its role.</summary>
        public static bool HasUniqueKeyword(UnitTemplate unit) =>
            unit != null && unit.Roster().Any(s => s != null && s.keywords.Any(k => k != null && k.displayName == UniqueKeyword));

        /// <summary>Only one of this unit per army: a Commander, or a unit with the Unique keyword.</summary>
        public static bool IsUnique(UnitTemplate unit) => unit != null && (IsUnique(unit.role) || HasUniqueKeyword(unit));

        private static string WhyUnique(UnitTemplate unit) =>
            HasUniqueKeyword(unit) ? "Unique" : $"{unit.role.DisplayName()} units can't be duplicated";

        /// <summary>Why the list isn't valid (empty if it is).</summary>
        /// <param name="faction">The army's faction: every unit must be one it has access to (null: not checked).</param>
        public static List<string> Problems(IReadOnlyList<UnitTemplate> army, Faction faction = null)
        {
            var problems = new List<string>();
            var units = (army ?? new List<UnitTemplate>()).Where(u => u != null).ToList();
            if (faction != null)
                foreach (var outsider in units.Where(u => !faction.Has(u)).Distinct())
                    problems.Add($"{outsider.displayName} isn't available to {faction.displayName}.");
            int supply = SupplyOf(units);
            if (supply > SupplyCap) problems.Add($"Over the Supply Cap: {supply} of {SupplyCap}.");
            int commanders = units.Count(u => u.role == UnitRole.Commander);
            if (commanders == 0) problems.Add("An army needs at least 1 Commander unit.");
            if (commanders > 1) problems.Add($"Only 1 Commander unit is allowed in an army ({commanders} chosen).");
            foreach (var dupe in units.Where(IsUnique).GroupBy(u => u).Where(g => g.Count() > 1))
                problems.Add($"Only one {dupe.Key.displayName} allowed ({WhyUnique(dupe.Key)}).");
            foreach (var locked in units.Where(u => !IsUnlocked(units, u)).Distinct())
                problems.Add($"{locked.displayName} can only be taken with {RequiredCommandersText(locked)} in the army.");
            return problems;
        }

        /// <summary>
        /// True if <paramref name="unit"/> can be in <paramref name="army"/> as far as Commanders go: it needs none, or one of
        /// its required Commanders is in the list.
        /// </summary>
        public static bool IsUnlocked(IEnumerable<UnitTemplate> army, UnitTemplate unit)
        {
            if (unit == null || !unit.NeedsCommander) return true;
            var required = unit.RequiredCommanders.ToList();
            return (army ?? Enumerable.Empty<UnitTemplate>()).Any(u => u != null && required.Contains(u));
        }

        /// <summary>The Commanders a unit needs, e.g. "Field Commander" or "Field Commander or Dragon Hierarch".</summary>
        public static string RequiredCommandersText(UnitTemplate unit)
        {
            var names = unit.RequiredCommanders.Select(c => c.displayName).Distinct().ToList();
            return names.Count <= 1 ? names.FirstOrDefault() ?? "" : string.Join(", ", names.Take(names.Count - 1)) + " or " + names.Last();
        }

        public static bool IsValid(IReadOnlyList<UnitTemplate> army, Faction faction = null) => Problems(army, faction).Count == 0;

        /// <summary>Null if <paramref name="unit"/> can be added to the list; otherwise why not.</summary>
        public static string WhyCannotAdd(IReadOnlyList<UnitTemplate> army, UnitTemplate unit, Faction faction = null)
        {
            if (unit == null) return "No unit";
            if (faction != null && !faction.Has(unit)) return $"Not available to {faction.displayName}";
            if (!IsUnlocked(army, unit)) return $"Needs {RequiredCommandersText(unit)} in the army";
            int supply = SupplyOf(army);
            if (supply + unit.supplyCost > SupplyCap)
                return $"Over the Supply Cap ({supply} + {unit.supplyCost} > {SupplyCap})";
            if (IsUnique(unit) && army.Contains(unit))
                return HasUniqueKeyword(unit) ? "Already in the army (Unique: only one per army)" : $"Already in the army ({unit.role.DisplayName()} units are unique)";
            if (unit.role == UnitRole.Commander && army.Any(u => u != null && u.role == UnitRole.Commander))
                return "The army already has its Commander (only 1 allowed)";
            return null;
        }

        /// <summary>
        /// A random valid army from the available units: 1 Commander, then random units (never a second Commander, and only
        /// units that Commander unlocks) for as long as another one fits under the Supply Cap.
        /// </summary>
        public static List<UnitTemplate> RandomArmy(IEnumerable<UnitTemplate> available, System.Random rng)
        {
            var pool = available.Where(u => u != null).Distinct().ToList();
            var army = new List<UnitTemplate>();
            var commanders = pool.Where(u => u.role == UnitRole.Commander).OrderBy(_ => rng.Next()).ToList();
            if (commanders.Count == 0) return army;   // no valid army possible
            army.Add(commanders[0]);

            // (Units that cost nothing would never fill the cap: they're left out, and there's a safety limit.)
            while (army.Count < MaxRandomUnits)
            {
                var options = pool.Where(u => u.role != UnitRole.Commander && u.supplyCost > 0 && WhyCannotAdd(army, u) == null).ToList();
                if (options.Count == 0) break;
                army.Add(options[rng.Next(options.Count)]);
            }
            return army;
        }

        /// <summary>A safety limit on the units in a random army.</summary>
        public const int MaxRandomUnits = 60;

        /// <summary>What the unit is made of, e.g. "Sergeant + 9× Trooper".</summary>
        public static string Composition(UnitTemplate unit)
        {
            var parts = new List<string>();
            if (unit.leader != null) parts.Add(unit.leader.displayName);
            foreach (var m in unit.members.Where(m => m.soldier != null))
                parts.Add(m.count > 1 ? $"{m.count}× {m.soldier.displayName}" : m.soldier.displayName);
            return string.Join(" + ", parts);
        }
    }
}
