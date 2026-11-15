using System.Collections.Generic;
using System.Linq;
using Eldoria.Units;
using UnityEngine;

namespace Eldoria.Rules
{
    /// <summary>
    /// A unit's Supply Cost:
    ///
    ///   Supply Cost = total Health + highest potential damage per turn (no bonuses) + sum of Armor
    ///                 + 20 × active abilities + 10 × passive abilities
    ///                 + 200 if any of its soldiers has Armor 4 or higher (the heavy armor tax)
    ///
    /// <list type="bullet">
    /// <item><b>Total Health</b>: every soldier's Health, with their standard equipment.</item>
    /// <item><b>Highest potential damage per turn</b>: a unit's action each turn is either a volley or an ability. A volley
    /// is every soldier firing their most damaging weapon (Damage × Shots, with the faction's changes, e.g. the Rask's
    /// +2 Damage), counting weapons they can be given in the army builder; an ability attack is its weapon's Damage × Shots
    /// against one target. The higher of the two. No bonuses: every shot hits, no crits, auras, penetration or armor.</item>
    /// <item><b>Sum of Armor</b>: every soldier's Armor Level, with their standard equipment (e.g. a Combat Shield).</item>
    /// <item><b>Abilities</b>: the unit's different abilities (its soldiers' own, and those their standard equipment and
    /// keywords give, e.g. Transport's Embark and Disembark),
    /// each counted once however many soldiers have it.</item>
    /// <item><b>Heavy armor tax</b>: 200, once per unit, if any of its soldiers has an Armor Level (with standard
    /// equipment) of <see cref="HeavyArmorThreshold"/> (4) or higher.</item>
    /// </list>
    /// </summary>
    public static class SupplyCost
    {
        public const int ActiveAbilityCost = 20;
        public const int PassiveAbilityCost = 10;
        /// <summary>A unit with a soldier whose Armor Level is this or higher pays <see cref="HeavyArmorTax"/>.</summary>
        public const int HeavyArmorThreshold = 4;
        public const int HeavyArmorTax = 200;

        /// <summary>The parts of a unit's Supply Cost.</summary>
        public struct Parts
        {
            public int Health, Damage, Armor, ActiveAbilities, PassiveAbilities;
            /// <summary>The heavy armor tax: <see cref="HeavyArmorTax"/> if a soldier's Armor is above the threshold, else 0.</summary>
            public int ArmorTax;
            public int Total => Health + Damage + Armor + ActiveAbilities * ActiveAbilityCost + PassiveAbilities * PassiveAbilityCost + ArmorTax;

            public override string ToString() =>
                $"{Total} = {Health} health + {Damage} damage + {Armor} armor + {ActiveAbilities}×{ActiveAbilityCost} + {PassiveAbilities}×{PassiveAbilityCost} + {ArmorTax} heavy armor tax";
        }

        public static int Calculate(UnitTemplate unit, Faction faction) => Breakdown(unit, faction).Total;

        public static Parts Breakdown(UnitTemplate unit, Faction faction)
        {
            var parts = new Parts();
            if (unit == null) return parts;
            var roster = unit.Roster().Where(s => s != null).ToList();
            int volley = 0;
            foreach (var s in roster)
            {
                var gear = Gear(s).ToList();
                parts.Health += s.health + gear.Sum(e => e.health);
                int armor = Mathf.Max(0, s.armorLevel + gear.Sum(e => e.armorLevel));
                parts.Armor += armor;
                if (armor >= HeavyArmorThreshold) parts.ArmorTax = HeavyArmorTax;   // once per unit
                volley += WeaponsAvailableTo(s, gear).Select(w => Damage(w, faction)).DefaultIfEmpty(0).Max();
            }

            var abilities = roster.SelectMany(s => s.AllAbilities()).Distinct().ToList();
            int bestAbility = abilities.Select(a => a.attackWeapon != null ? a.attackWeapon : a.areaWeapon)
                .Where(w => w != null).Select(w => Damage(w, faction)).DefaultIfEmpty(0).Max();
            parts.Damage = Mathf.Max(volley, bestAbility);
            parts.ActiveAbilities = abilities.Count(a => a.kind == AbilityKind.Active);
            parts.PassiveAbilities = abilities.Count(a => a.kind == AbilityKind.Passive);
            return parts;
        }

        /// <summary>Damage × Shots, with the faction's changes.</summary>
        private static int Damage(WeaponTemplate weapon, Faction faction)
        {
            var stats = WeaponStats.For(weapon, faction);
            return stats.BaseDamage * Mathf.Max(1, stats.Shots);
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
