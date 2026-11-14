using System.Collections.Generic;
using System.Linq;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// Paladin Squad (United Kingdom of Aelfland only, Heavy Support): a Sergeant and 4 Troopers in Powered Armor, each with a
    /// Broadsword (Damage 5, Penetration 1, Range 2 m, +0, Melee).
    /// </summary>
    public class PaladinSquadTests
    {
        private const string Data = "Assets/_Project/Data";
        private static UnitTemplate U(string file) => TestArmies.ByFile(file);   // test-made stand-ins

        [Test]
        public void TheBroadswordIsAsDesigned()
        {
            var sword = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{Data}/Weapons/Broadsword.asset");
            Assert.That((sword.displayName, sword.baseDamage, sword.PenetrationValue, sword.range, sword.accuracyBonus, sword.shots),
                Is.EqualTo(("Broadsword", 5, (int?)1, 2f, 0, 1)));
            Assert.That(sword.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Melee" }));
        }

        [Test]
        public void PaladinsFightInCloseRangeWithinTwoMetres()
        {
            var paladins = UnitFactory.Spawn(U("PaladinSquad"), Side.Player, Vector3.zero, Quaternion.identity);
            var near = UnitFactory.Spawn(U("InfantrySquad"), Side.AI, new Vector3(0f, 0f, 1.5f), Quaternion.Euler(0f, 180f, 0f));
            try
            {
                var leader = paladins.Leader;
                Assert.That(CloseRange.IsInCloseRange(leader), Is.True, "Biological, an enemy 1.5 m away");
                Assert.That(leader.WeaponFor(null)?.Name, Is.EqualTo("Broadsword"), "Melee: usable in close range, unlike a rifle");

                var r = UnitShooting.Resolve(paladins, near, false, new System.Random(1));
                var shots = r.Shots.Where(s => s.Target != null).ToList();
                Assert.That(shots, Is.Not.Empty);
                Assert.That(shots.All(s => Measure.Between(s.Shooter, s.Target) <= 2f + 0.01f), Is.True, "only within the sword's 2 m");
                foreach (var hit in shots.Where(s => s.Attack.Hit))
                    Assert.That(hit.Attack.DamageDealt, Is.EqualTo(5), "Damage 5, penetration 1 vs Armor 1: 100%");
            }
            finally
            {
                Object.DestroyImmediate(paladins.gameObject);
                Object.DestroyImmediate(near.gameObject);
            }
        }

        [Test]
        public void TheArmyBuilderShowsTheBroadsword()
        {
            Assert.That(UnitInfo.WeaponLines(U("PaladinSquad")).Single(), Is.EqualTo("Broadsword — Damage 5 · Shots 1 · Penetration 1 · Range 2 m · Melee"));
        }
    }
}
