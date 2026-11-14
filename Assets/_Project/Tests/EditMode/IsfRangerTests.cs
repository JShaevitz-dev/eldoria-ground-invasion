using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The ISF Ranger (Specialist): the original Eldoria's Sniper, renamed, with Armor 1. One ISF Ranger (Health 5, Armor 1, Accuracy
    /// 100%, Move 15 m; Biological, Sniper Precision, Solo) with a Sniper Rifle (Damage 10, Shots 1, Penetration 1, +50%
    /// accuracy, 50 m, Deployed).
    /// </summary>
    public class IsfRangerTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.First();
        private static UnitTemplate Ranger => Catalog.Available.Single(u => u.displayName == "ISF Ranger");

        [Test]
        public void TheIsfRangerIsAnIsfSpecialist()
        {
            var unit = Ranger;
            Assert.That(Isf.Has(unit), Is.True);
            Assert.That(Catalog.Factions.ElementAt(1).Has(unit), Is.False, "not the Draconis Imperialis's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.Contain("Ranger").And.Contain("50 m").And.Contain("light armor").And.Contain("Anti-Materiel Rifle"));
            var commander = Catalog.Available.Single(u => u.displayName == "Field Commander");
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { commander, unit }, Isf), Is.True);
            Assert.That(ArmyRules.WhyCannotAdd(new List<UnitTemplate> { commander, unit }, unit, Isf), Does.Contain("Already in the army"),
                "a Specialist: one per army");
        }

        [Test]
        public void TheIsfRangerHasTheOldSnipersStatsWithArmor1()
        {
            var r = Ranger.leader;
            Assert.That(r.displayName, Is.EqualTo("ISF Ranger"));
            Assert.That((r.health, r.armorLevel, r.accuracy, r.movementRange), Is.EqualTo((5, 1, 100, 15f)));
            Assert.That(r.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Sniper Precision", "Solo" }));
            Assert.That(r.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Chromashift Textiles" }));
            Assert.That(r.HasOptions, Is.True, "it can swap its rifle");
            Assert.That(r.prefab, Is.Not.Null, "the old Sniper model");
            var rifle = r.weapons.Single();
            Assert.That(r.PrimaryWeapon, Is.SameAs(rifle));
            Assert.That(rifle.displayName, Is.EqualTo("Sniper Rifle"));
            Assert.That((rifle.baseDamage, rifle.shots, rifle.PenetrationValue, rifle.accuracyBonus, rifle.range), Is.EqualTo((10, 1, (int?)1, 50, 50f)));
            Assert.That(rifle.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Deployed" }));
            Assert.That(UnitInfo.SoldierLines(Ranger).Single(),
                Is.EqualTo("ISF Ranger \u2014 Health 5 \u00b7 Armor 1 \u00b7 Accuracy 100% \u00b7 Move 15 m (sprint 30 m) \u00b7 Sniper Rifle"));
            Assert.That(UnitInfo.SharedKeywords(Ranger), Does.Contain("Sniper Precision"));
        }

        [Test]
        public void ChromashiftTextilesIsAPassiveAbilityThatOnlyCoversTheRanger()
        {
            var ability = Ranger.leader.abilities.Single();
            Assert.That(ability.kind, Is.EqualTo(AbilityKind.Passive));
            Assert.That(ability.IsAura, Is.True, "it grants its keyword like Lead from the front");
            Assert.That(ability.includeSelf, Is.True);
            Assert.That(ability.radius, Is.EqualTo(0f), "the wearer only");
            Assert.That(ability.description, Does.Contain("25%"));
            var keyword = ability.grantsNearby;
            Assert.That(keyword.displayName, Is.EqualTo("Chromashift"));
            var i = keyword.interactions.Single();
            Assert.That((i.role, i.party, i.condition, i.effect, i.amount),
                Is.EqualTo((KeywordRole.WhenDefending, KeywordParty.Target, KeywordCondition.Always, KeywordEffect.AccuracyPenalty, 25)));
            Assert.That(UnitInfo.AbilityLines(Ranger), Is.EqualTo(new[] { "Chromashift Textiles (passive)" }));
            Assert.That(Ranger.description, Does.Contain("Chromashift").And.Contain("Shotgun"));
        }

        [Test]
        public void SoldiersAimingAtTheRangerTakeMinus25PercentAccuracy()
        {
            var ranger = UnitFactory.Spawn(Ranger, Side.Player, Vector3.zero, Quaternion.identity);
            var buddy = UnitFactory.Spawn(Catalog.Available.Single(u => u.displayName == "Field Commander"), Side.Player,
                new Vector3(1f, 0f, 0f), Quaternion.identity);
            var enemy = UnitFactory.Spawn(Catalog.Available.Single(u => u.displayName == "Dragonborn Squad"), Side.AI,
                new Vector3(0f, 0f, 15f), Quaternion.Euler(0f, 180f, 0f));
            try
            {
                Eldoria.Terrain.NearbyKeywords.RefreshAll();
                var r = ranger.Leader;
                var shooter = enemy.Soldiers[0];
                Assert.That(r.HasKeyword("Chromashift"), Is.True, "the Ranger wears it");
                Assert.That(buddy.Leader.HasKeyword("Chromashift"), Is.False, "a friend standing 1 m away doesn't");
                Assert.That(shooter.HasKeyword("Chromashift"), Is.False);

                var atRanger = KeywordRules.Evaluate(shooter, shooter.PrimaryWeapon, r).AccuracyModifiers;
                Assert.That(atRanger.Count(m => m.Type == AccuracyModifierType.Penalty && m.Value == 25), Is.EqualTo(1),
                    "-25% for shooting at the Ranger");
                var atBuddy = KeywordRules.Evaluate(shooter, shooter.PrimaryWeapon, buddy.Leader).AccuracyModifiers;
                Assert.That(atBuddy.Any(m => m.Type == AccuracyModifierType.Penalty), Is.False, "no penalty against anyone else");
                var byRanger = KeywordRules.Evaluate(r, r.PrimaryWeapon, shooter).AccuracyModifiers;
                Assert.That(byRanger.Any(m => m.Type == AccuracyModifierType.Penalty), Is.False, "the Ranger's own shots aren't affected");

                int dragonborn = shooter.Accuracy;
                var result = Accuracy.ForAttack(shooter, shooter.PrimaryWeapon, false, atRanger);
                Assert.That(result.Final, Is.EqualTo(dragonborn - 25), "75% becomes 50%");
            }
            finally
            {
                Object.DestroyImmediate(ranger.gameObject);
                Object.DestroyImmediate(buddy.gameObject);
                Object.DestroyImmediate(enemy.gameObject);
            }
        }

        [Test]
        public void TheRangerCanSwapTheSniperRifleForTheAntiMaterielRifleOrAShotgun()
        {
            var r = Ranger.leader;
            var option = r.weaponOptions.Single();
            Assert.That(option.weapon.displayName, Is.EqualTo("Sniper Rifle"));
            Assert.That(option.alternatives.Select(w => w.displayName), Is.EqualTo(new[] { "Anti-Materiel Rifle", "Shotgun" }));
            var sniper = option.weapon;
            var amr = option.alternatives[0];
            var shotgun = option.alternatives[1];

            var builder = new ArmyBuilderScreen(Catalog);
            builder.ChooseFaction(Isf);
            builder.Clear();
            Assert.That(builder.Add(Ranger), Is.Null);
            Assert.That(builder.CanCustomize(0), Is.True);
            Assert.That(builder.SetWeapon(0, r, sniper, amr), Is.Null);
            Assert.That(builder.Loadouts[0].Describe(), Is.EqualTo("ISF Ranger: Anti-Materiel Rifle"));
            Assert.That(builder.SetWeapon(0, r, sniper, TestArmies.Weapon("RocketLauncher")), Does.Contain("can't be swapped"), "only the listed weapons");
            Assert.That(builder.Add(Catalog.Available.Single(u => u.displayName == "Field Commander")), Is.Null);

            var unit = UnitFactory.Spawn(Ranger, Side.Player, Vector3.zero, Quaternion.identity);
            try
            {
                ArmyDeployment.ApplyLoadout(unit, builder.Loadouts[0]);
                var soldier = unit.Leader;
                Assert.That(soldier.Weapons.Select(w => w.Name), Is.EqualTo(new[] { "Anti-Materiel Rifle" }), "replaces the Sniper Rifle");
                Assert.That(soldier.PrimaryWeapon.Name, Is.EqualTo("Anti-Materiel Rifle"));
                Assert.That(soldier.HasKeyword("Sniper Precision"), Is.True, "still picks its target");
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }

            // Or the Shotgun.
            Assert.That(builder.SetWeapon(0, r, sniper, shotgun), Is.Null);
            Assert.That(builder.Loadouts[0].Describe(), Is.EqualTo("ISF Ranger: Shotgun"));
            var shotgunUnit = UnitFactory.Spawn(Ranger, Side.Player, Vector3.zero, Quaternion.identity);
            try
            {
                ArmyDeployment.ApplyLoadout(shotgunUnit, builder.Loadouts[0]);
                Assert.That(shotgunUnit.Leader.Weapons.Select(w => w.Name), Is.EqualTo(new[] { "Shotgun" }), "replaces the Sniper Rifle");
            }
            finally
            {
                Object.DestroyImmediate(shotgunUnit.gameObject);
            }
        }
    }
}
