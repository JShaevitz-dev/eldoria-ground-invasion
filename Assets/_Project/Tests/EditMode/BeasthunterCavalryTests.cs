using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Rask's Beasthunter Cavalry (Specialist): a Rask Superior Rider and 2 Rask Riders on motorcycles, with the Rask
    /// Rifleman's stats but Move 40 m and Cavalry (can't sprint), each with a wrist-mounted Mag-grenade Launcher (Damage 5,
    /// Penetration 5, Range 20 m, Shots 1, +0%).
    /// </summary>
    public class BeasthunterCavalryTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Rask => Catalog.Factions.ElementAt(2);
        private static UnitTemplate UnitNamed(string name) => Catalog.Available.Single(u => u.displayName == name);
        private static UnitTemplate Cavalry => UnitNamed("Beasthunter Cavalry");

        [Test]
        public void TheCavalryIsARaskSpecialistOfThree()
        {
            var unit = Cavalry;
            Assert.That(Rask.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Take(2).Any(f => f.Has(unit)), Is.False, "only the Rask's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(3));
            Assert.That(unit.leader.displayName, Is.EqualTo("Rask Superior Rider"));
            Assert.That(unit.members.Single().soldier.displayName, Is.EqualTo("Rask Rider"));
            Assert.That(unit.members.Single().count, Is.EqualTo(2));
            Assert.That(unit.description, Does.StartWith("Using techniques originally developed for hunting megafauna this anti-tank cavalry has the firepower to crack armor and the mobility to stay alive."));
            Assert.That(unit.description, Does.Contain("Specialist"));
        }

        [Test]
        public void RidersHaveTheRaskRiflemansStatsButMove40AndCavalry()
        {
            var rifleman = UnitNamed("Rask Rifle Team").members.Single().soldier;
            foreach (var s in Cavalry.Roster().Distinct())
            {
                Assert.That((s.health, s.armorLevel, s.accuracy), Is.EqualTo((rifleman.health, rifleman.armorLevel, rifleman.accuracy)), s.displayName);
                Assert.That(s.movementRange, Is.EqualTo(40f));
                Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(rifleman.keywords.Select(k => k.displayName).Concat(new[] { "Cavalry" })));
                Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Mag-grenade Launcher" }));
                Assert.That(s.prefab, Is.Not.Null);
                Assert.That(s.prefab.GetComponentsInChildren<Transform>().Count(t => t.name == "Wheel"), Is.EqualTo(2), "on a motorcycle");
                Assert.That(s.prefab.GetComponentsInChildren<Transform>().Any(t => t.name == "Launcher Barrel"), Is.True, "a wrist-mounted launcher");
            }
        }

        [Test]
        public void TheMagGrenadeLauncherHasTheGivenStats()
        {
            var w = Cavalry.leader.PrimaryWeapon;
            Assert.That((w.baseDamage, w.shots, w.PenetrationValue, w.accuracyBonus, w.range), Is.EqualTo((5, 1, (int?)5, 0, 20f)));
            Assert.That(WeaponStats.For(w, Rask).Name, Is.EqualTo("Mag-grenade Launcher"), "no Rask name for it");
        }

        [Test]
        public void CavalryIsATraitAndCavalryCantSprint()
        {
            var cavalry = KeywordLibrary.Get("Cavalry");
            Assert.That(cavalry, Is.Not.Null);
            Assert.That(cavalry.category, Is.EqualTo(KeywordCategory.Trait));
            Assert.That(cavalry.description, Does.Contain("cannot sprint"));

            var unit = ArmyDeployment.Spawn(Cavalry, Side.AI, Vector3.zero, Quaternion.identity, "Beasthunter Cavalry", null, Rask);
            try
            {
                foreach (var s in unit.LivingSoldiers)
                {
                    Assert.That(s.Move, Is.EqualTo(40f));
                    Assert.That(s.CanSprint, Is.False, s.name);
                    Assert.That(s.Sprint, Is.EqualTo(40f), "no further than a move");
                }
                Assert.That(UnitInfo.SoldierLines(Cavalry)[0], Does.Contain("Move 40 m (can't sprint)"));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
