using System;
using System.Collections.Generic;
using System.Linq;
using Eldoria.Units;
using UnityEngine;

namespace Eldoria.Rules
{
    /// <summary>
    /// A unit's Supply Cost:
    ///
    ///   Supply Cost = 3 × Firepower + ½ × Durability + 20 × active abilities + 10 × passive abilities + armor tax
    ///
    /// (rounded to the nearest multiple of 5, halves up). The weights are the constants below.
    /// <list type="bullet">
    /// <item><b>Firepower</b>: the damage the unit can expect to do in a turn. Its action is either a volley or an ability,
    /// so it's the higher of:
    ///   - a volley: for every soldier, their best weapon (counting weapons the army builder can give them) worth
    ///     Damage × Shots × hit chance × penetration factor, added up;
    ///   - its best ability attack, against one target: the ability weapon's Damage × Shots × penetration factor, times the
    ///     user's hit chance for an aimed attack (area and unit attacks don't roll to hit).
    ///   Hit chance = the soldier's Accuracy (with standard equipment) + the weapon's accuracy bonus, capped at 100%.
    ///   Weapons use the faction's stats (e.g. the Rask's +2 Damage). No other bonuses (crits, auras, cover).</item>
    /// <item><b>Penetration factor</b>: the weapon's average damage multiplier against Armor 0 to 4 under the penetration
    /// rules (<see cref="ArmorPenetration"/>): 0.3 for Penetration 0, 0.6 for 1, 0.9 for 2, 1.2 for 3, 1.4 for 4, 1.5 for
    /// 5 or more, 1 for X. So anti-armor weapons are worth more than small arms that can't hurt armor.</item>
    /// <item><b>Durability</b>: every soldier's Health × (1 + Armor ÷ 4), with standard equipment, added up. At half weight:
    /// what a unit can do counts for more than how long it lasts.</item>
    /// <item><b>Abilities</b>: the unit's different abilities (its soldiers' own, and those their standard equipment and
    /// keywords give), each counted once however many soldiers have it.</item>
    /// <item><b>Armor tax</b>, once per unit, by its best-armored soldier: 100 for Armor 4 or higher, 50 for Armor 3.</item>
    /// </list>
    /// </summary>
    public static class SupplyCost
    {
        public const double FirepowerWeight = 3.0;
        public const double DurabilityWeight = 0.5;
        /// <summary>Each point of Armor adds this much to a soldier's Health for Durability (Health × (1 + Armor × this)).</summary>
        public const double ArmorDurabilityPerPoint = 0.25;
        public const int ActiveAbilityCost = 20;
        public const int PassiveAbilityCost = 10;
        /// <summary>A unit with a soldier whose Armor Level is this or higher pays <see cref="HeavyArmorTax"/>.</summary>
        public const int HeavyArmorThreshold = 4;
        public const int HeavyArmorTax = 100;
        /// <summary>A unit whose best-armored soldier has exactly this Armor Level pays <see cref="MediumArmorTax"/>.</summary>
        public const int MediumArmor = 3;
        public const int MediumArmorTax = 50;
        /// <summary>The Armor Levels a weapon's penetration factor is averaged over (0 to this).</summary>
        public const int ReferenceArmorMax = 4;
        /// <summary>Supply Costs are rounded to the nearest multiple of this.</summary>
        public const int RoundTo = 5;

        /// <summary>Rounds a cost to the nearest multiple of <see cref="RoundTo"/> (halves up).</summary>
        public static int Round(double cost) => (int)Math.Floor(cost / RoundTo + 0.5 + 1e-9) * RoundTo;

        /// <summary>The parts of a unit's Supply Cost.</summary>
        public struct Parts
        {
            public double Firepower, Durability;
            public int ActiveAbilities, PassiveAbilities;
            /// <summary>The unit's best Armor Level (armor counts through Durability and the tax).</summary>
            public int BestArmor;
            /// <summary>The armor tax: 100 for a soldier with Armor 4 or higher, 50 for Armor 3, else 0.</summary>
            public int ArmorTax;

            public double Exact => FirepowerWeight * Firepower + DurabilityWeight * Durability
                                   + ActiveAbilities * ActiveAbilityCost + PassiveAbilities * PassiveAbilityCost + ArmorTax;

            /// <summary>The Supply Cost: <see cref="Exact"/> rounded to the nearest multiple of 5 (halves up).</summary>
            public int Total => Round(Exact);

            public override string ToString() =>
                $"{Total} = 3 × {Firepower:0.##} firepower + ½ × {Durability:0.##} durability + {ActiveAbilities}×{ActiveAbilityCost} + {PassiveAbilities}×{PassiveAbilityCost} + {ArmorTax} armor tax (Armor {BestArmor})";
        }

        /// <summary>The armor tax for a unit whose best-armored soldier has this Armor Level.</summary>
        public static int ArmorTaxFor(int armor) =>
            armor >= HeavyArmorThreshold ? HeavyArmorTax : armor == MediumArmor ? MediumArmorTax : 0;

        /// <summary>
        /// A weapon's average damage multiplier against Armor 0 to <see cref="ReferenceArmorMax"/> under the penetration rules
        /// (1 for no penetration value, i.e. Penetration X).
        /// </summary>
        public static double PenetrationFactor(int? penetration)
        {
            if (!penetration.HasValue) return 1.0;
            double sum = 0;
            for (int armor = 0; armor <= ReferenceArmorMax; armor++) sum += ArmorPenetration.Resolve(1, penetration, armor).Multiplier;
            return sum / (ReferenceArmorMax + 1);
        }

        /// <summary>A soldier's chance to hit with a weapon: Accuracy (with standard equipment) + the weapon's bonus, 0 to 1.</summary>
        public static double HitChance(SoldierTemplate soldier, WeaponStats weapon)
        {
            int accuracy = soldier.accuracy + Gear(soldier).Sum(e => e.accuracy) + weapon.AccuracyBonus;
            return Math.Max(0, Math.Min(100, accuracy)) / 100.0;
        }

        /// <summary>What a soldier's shots with a weapon are worth in a turn: Damage × Shots × hit chance × penetration factor.</summary>
        public static double WeaponValue(SoldierTemplate soldier, WeaponTemplate weapon, Faction faction, bool rollsToHit = true)
        {
            var stats = WeaponStats.For(weapon, faction);
            double hit = rollsToHit ? HitChance(soldier, stats) : 1.0;
            return stats.BaseDamage * Math.Max(1, stats.Shots) * hit * PenetrationFactor(stats.PenetrationValue);
        }

        public static int Calculate(UnitTemplate unit, Faction faction) => Breakdown(unit, faction).Total;

        public static Parts Breakdown(UnitTemplate unit, Faction faction)
        {
            var parts = new Parts();
            if (unit == null) return parts;
            var roster = unit.Roster().Where(s => s != null).ToList();
            double volley = 0;
            foreach (var s in roster)
            {
                var gear = Gear(s).ToList();
                int armor = Math.Max(0, s.armorLevel + gear.Sum(e => e.armorLevel));
                parts.BestArmor = Math.Max(parts.BestArmor, armor);
                parts.Durability += (s.health + gear.Sum(e => e.health)) * (1 + armor * ArmorDurabilityPerPoint);
                volley += WeaponsAvailableTo(s, gear).Select(w => WeaponValue(s, w, faction)).DefaultIfEmpty(0).Max();
            }
            parts.ArmorTax = ArmorTaxFor(parts.BestArmor);   // once per unit

            // The best ability attack, against one target, by the soldier who'd use it best.
            double bestAbility = 0;
            foreach (var s in roster.Distinct())
                foreach (var a in s.AllAbilities())
                {
                    if (a.attackWeapon != null) bestAbility = Math.Max(bestAbility, WeaponValue(s, a.attackWeapon, faction, rollsToHit: a.IsAimedAttack));
                    if (a.areaWeapon != null) bestAbility = Math.Max(bestAbility, WeaponValue(s, a.areaWeapon, faction, rollsToHit: false));
                }
            parts.Firepower = Math.Max(volley, bestAbility);

            var abilities = roster.SelectMany(s => s.AllAbilities()).Distinct().ToList();
            parts.ActiveAbilities = abilities.Count(a => a.kind == AbilityKind.Active);
            parts.PassiveAbilities = abilities.Count(a => a.kind == AbilityKind.Passive);
            return parts;
        }

        private static IEnumerable<Equipment> Gear(SoldierTemplate s) =>
            s.equipment != null ? s.equipment.Where(e => e != null) : Enumerable.Empty<Equipment>();

        /// <summary>The soldier's weapons, their standard equipment's, and any they can swap to in the army builder.</summary>
        private static IEnumerable<WeaponTemplate> WeaponsAvailableTo(SoldierTemplate s, List<Equipment> gear) =>
            s.weapons.Concat(gear.SelectMany(e => e.weapons))
                .Concat(s.weaponOptions.Where(o => o != null).SelectMany(o => o.alternatives))
                .Where(w => w != null).Distinct();
    }
}
