using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Rask's Stinger Heavy Robot (Heavy Support): one Stinger Heavy Robot (Health 25, Armor 5, Accuracy 75%, Move 30 m;
    /// Mechanical, Heavy, Robotic, Large; Stabilizers) whose Plasma Mortar (the Mortar Launcher's stats) shells an area: an
    /// active ability, like the Frag Grenade, at a spot within 50 m (a 2.5 m blast).
    /// </summary>
    public class StingerHeavyRobotTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Rask => Catalog.Factions.ElementAt(2);
        private static UnitTemplate Stinger => Catalog.Available.Single(u => u.displayName == "Stinger Heavy Robot");

        [Test]
        public void TheStingerIsARaskHeavySupportUnitOfOne()
        {
            var unit = Stinger;
            Assert.That(Rask.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Take(2).Any(f => f.Has(unit)), Is.False, "only the Rask's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.HeavySupport));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.leader.displayName, Is.EqualTo("Stinger Heavy Robot"));
            Assert.That(unit.description, Does.Contain("Plasma Mortar").And.Contain("Stabilizers"));
            Assert.That(ArmyBuilderScreen.FactionCanFieldArmy(Catalog, Rask), Is.True, "the Rask Ultima is their Commander");
        }

        [Test]
        public void ItHasTheGivenStatsKeywordsAndAbility()
        {
            var s = Stinger.leader;
            Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((25, 5, 75, 30f)));
            Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Mechanical", "Heavy", "Robotic", "Large" }));
            Assert.That(s.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Stabilizers", "Plasma Mortar" }));
            Assert.That(s.weapons, Is.Empty, "the Plasma Mortar shells an area (an ability), it isn't aimed at a soldier");
            Assert.That(s.prefab, Is.Not.Null);
            Assert.That(s.prefab.GetComponent<CapsuleCollider>().radius, Is.EqualTo(1.6f));
            Assert.That(UnitInfo.AbilityLines(Stinger).Count, Is.EqualTo(2));
            Assert.That(UnitInfo.AbilityLines(Stinger)[0], Is.EqualTo("Stabilizers (passive)"));
            Assert.That(UnitInfo.AbilityLines(Stinger)[1], Does.StartWith("Plasma Mortar"));
        }

        [Test]
        public void HeavyIsANewTraitKeyword()
        {
            var heavy = KeywordLibrary.Get("Heavy");
            Assert.That(heavy, Is.Not.Null);
            Assert.That(heavy.appliesTo, Is.EqualTo(KeywordTarget.Soldier));
            Assert.That(heavy.category, Is.EqualTo(KeywordCategory.Trait));
        }

        [Test]
        public void ThePlasmaMortarIsAnAreaAttackLikeTheFragGrenade()
        {
            var blast = PlasmaMortar;
            Assert.That(blast.IsActive && blast.IsAreaAttack, Is.True);
            Assert.That((blast.range, blast.areaRadius, blast.usesPerTurn, blast.usesPerBattle), Is.EqualTo((50f, 2.5f, 1, 0)));
            var grenade = UnityEditor.AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/FragGrenade.asset");
            Assert.That(blast.areaRadius, Is.EqualTo(grenade.areaRadius), "the same blast as a Frag Grenade");
            Assert.That(blast.areaWeapon.displayName, Is.EqualTo("Plasma Mortar"));
        }

        private static Ability PlasmaMortar => Stinger.leader.abilities.Single(a => a.displayName == "Plasma Mortar");

        [Test]
        public void ThePlasmaMortarHasTheMortarLaunchersStats()
        {
            var plasma = PlasmaMortar.areaWeapon;
            var mortar = Catalog.Available.Single(u => u.displayName == "Mortar Team").leader.PrimaryWeapon;
            Assert.That(mortar.displayName, Is.EqualTo("Mortar Launcher"));
            Assert.That(plasma, Is.Not.SameAs(mortar), "a weapon of its own");
            Assert.That((plasma.baseDamage, plasma.shots, plasma.PenetrationValue, plasma.accuracyBonus, plasma.range),
                Is.EqualTo((mortar.baseDamage, mortar.shots, mortar.PenetrationValue, mortar.accuracyBonus, mortar.range)));
            Assert.That((plasma.baseDamage, plasma.shots, plasma.PenetrationValue, plasma.accuracyBonus, plasma.range), Is.EqualTo((5, 1, (int?)3, 0, 50f)));
            Assert.That(plasma.keywords.Select(k => k.displayName), Is.EqualTo(mortar.keywords.Select(k => k.displayName)));
            Assert.That(plasma.keywords.Select(k => k.displayName), Is.EquivalentTo(new[] { "Deployed", "Manual Load", "Explosive" }));
        }

        [Test]
        public void StabilizersLetThePlasmaMortarFireAfterMovingButItCantSprint()
        {
            var unit = ArmyDeployment.Spawn(Stinger, Side.AI, new Vector3(-25f, 0f, 0f), Quaternion.identity, "Stinger Heavy Robot", null, Rask);
            try
            {
                var robot = unit.Leader;
                Assert.That(robot.CanSprint, Is.False, "Mechanical");
                Assert.That(robot.Move, Is.EqualTo(30f));
                robot.GainKeyword(KeywordLibrary.Get(KeywordLibrary.Moved));
                Assert.That(ActiveAbilities.WhyCannotCast(robot, PlasmaMortar), Is.Null, "Stabilizers nullify Deployed");
                Assert.That(ActiveAbilities.WhyCannotUse(robot, PlasmaMortar, targetPoint: new Vector3(20f, 0f, 0f)), Is.Null, "45 m away");
                Assert.That(ActiveAbilities.WhyCannotUse(robot, PlasmaMortar, targetPoint: new Vector3(29f, 0f, 0f)), Does.StartWith("Out of range"), "54 m away");
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
