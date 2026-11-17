using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Rask's Intimidator Medium Robot (Heavy Support): one Intimidator Medium Robot (Health 10, Armor 3, Accuracy 75%,
    /// Move 30 m; Mechanical, Robotic; Stabilizers) with Twin Intimidator Machine Guns (the Machine Gun's stats, 10 shots).
    /// </summary>
    public class IntimidatorMediumRobotTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Rask => Catalog.Factions.ElementAt(2);
        private static UnitTemplate Intimidator => Catalog.Available.Single(u => u.displayName == "Intimidator Medium Robot");

        [Test]
        public void TheIntimidatorIsARaskHeavySupportUnitOfOne()
        {
            var unit = Intimidator;
            Assert.That(Rask.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Take(2).Any(f => f.Has(unit)), Is.False, "only the Rask's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.HeavySupport));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.Contain("Twin Intimidator Machine Guns").And.Contain("Stabilizers"));
            Assert.That(ArmyBuilderScreen.FactionCanFieldArmy(Catalog, Rask), Is.True, "the Rask Ultima is their Commander");
        }

        [Test]
        public void ItHasTheGivenStatsKeywordsAndAbility()
        {
            var s = Intimidator.leader;
            Assert.That(s.displayName, Is.EqualTo("Intimidator Medium Robot"));
            Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((10, 3, 75, 30f)));
            Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Mechanical", "Robotic", "Large" }));
            Assert.That(s.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Stabilizers" }));
            Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Twin Intimidator Machine Guns" }));
            Assert.That(s.prefab, Is.Not.Null);
            Assert.That(s.prefab.GetComponentsInChildren<Transform>().Count(t => t.name == "Machine Gun"), Is.EqualTo(2), "a gun on each shoulder");
        }

        [Test]
        public void TheTwinGunsAreTheMachineGunWith10Shots()
        {
            var twin = Intimidator.leader.PrimaryWeapon;
            var mg = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponTemplate>("Assets/_Project/Data/Weapons/MachineGun.asset");
            Assert.That(twin, Is.Not.SameAs(mg));
            Assert.That(twin.shots, Is.EqualTo(10));
            Assert.That(mg.shots, Is.EqualTo(5), "the Machine Gun is unchanged");
            Assert.That((twin.baseDamage, twin.PenetrationValue, twin.accuracyBonus, twin.range), Is.EqualTo((mg.baseDamage, mg.PenetrationValue, mg.accuracyBonus, mg.range)));
            Assert.That((twin.baseDamage, twin.PenetrationValue, twin.accuracyBonus, twin.range), Is.EqualTo((5, (int?)1, 0, 20f)));
            Assert.That(twin.keywords, Is.EqualTo(mg.keywords), "Deployed, like the Machine Gun");
            Assert.That(UnitInfo.WeaponLines(Intimidator).Single(), Is.EqualTo("Twin Intimidator Machine Guns \u2014 Damage 5 \u00b7 Shots 10 \u00b7 Penetration 1 \u00b7 Range 20 m \u00b7 Deployed"));
        }

        [Test]
        public void StabilizersLetItFireAfterMovingButItCantSprint()
        {
            var unit = ArmyDeployment.Spawn(Intimidator, Side.AI, Vector3.zero, Quaternion.identity, "Intimidator", null, Rask);
            try
            {
                var robot = unit.Leader;
                Assert.That(robot.CanSprint, Is.False, "Mechanical");
                robot.GainKeyword(KeywordLibrary.Get(KeywordLibrary.Moved));
                Assert.That(robot.PrimaryWeapon.Template.HasKeyword("Deployed"), Is.True);
                Assert.That(WeaponUse.CanUse(robot, robot.PrimaryWeapon), Is.True, "Stabilizers nullify Deployed");
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
