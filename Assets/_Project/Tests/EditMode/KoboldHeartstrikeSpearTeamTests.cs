using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Draconis Imperialis's Kobold Heartstrike Spear Team (Specialist): a Kobold Sergeant and 4 Kobold Riflemen with the
    /// Kobold Militiaman's stats, each with a Heartstrike Spear (an anti-tank mine on a rod: Damage 5, Shots 1, Penetration 6,
    /// +0%, Range 30 m).
    /// </summary>
    public class KoboldHeartstrikeSpearTeamTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static UnitTemplate Team => Catalog.Available.Single(u => u.displayName == "Kobold Heartstrike Spear Team");
        private static UnitTemplate UnitNamed(string name) => Catalog.Available.Single(u => u.displayName == name);

        [Test]
        public void TheTeamIsADraconisSpecialistOfFiveKobolds()
        {
            var team = Team;
            Assert.That(Draconis.Has(team), Is.True);
            Assert.That(Catalog.Factions.Where(f => f != Draconis).Any(f => f.Has(team)), Is.False, "only the Draconis'");
            Assert.That(team.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(team.Size, Is.EqualTo(5));
            Assert.That(team.leader.displayName, Is.EqualTo("Kobold Sergeant"));
            Assert.That(team.members.Single().soldier.displayName, Is.EqualTo("Kobold Rifleman"));
            Assert.That(team.members.Single().count, Is.EqualTo(4));
            Assert.That(team.description, Does.StartWith("An anti-tank squad of kobolds conditioned for suicidal bravery."));
            Assert.That(team.supplyCost, Is.GreaterThan(0));
        }

        [Test]
        public void EveryKoboldHasTheMilitiamansStatsAndAHeartstrikeSpear()
        {
            var militiaman = UnitNamed("Kobold Militia").members.Single().soldier;
            foreach (var s in Team.Roster().Distinct())
            {
                Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange, s.radius, s.height),
                    Is.EqualTo((militiaman.health, militiaman.armorLevel, militiaman.accuracy, militiaman.movementRange, militiaman.radius, militiaman.height)), s.displayName);
                Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(militiaman.keywords.Select(k => k.displayName)), s.displayName);
                Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Heartstrike Spear" }), s.displayName);
                Assert.That(s.PrimaryWeapon.displayName, Is.EqualTo("Heartstrike Spear"));
                Assert.That(s.prefab, Is.Not.Null);
                Assert.That(s.prefab.transform.Find("Model/Anti-tank Mine"), Is.Not.Null, "a mine on a rod");
                Assert.That(s.prefab.transform.Find("Model/Rifle"), Is.Null, "no rifle");
            }
        }

        [Test]
        public void TheHeartstrikeSpearHasTheGivenStats()
        {
            var spear = Team.leader.PrimaryWeapon;
            Assert.That((spear.baseDamage, spear.shots, spear.PenetrationValue, spear.accuracyBonus, spear.range), Is.EqualTo((5, 1, (int?)6, 0, 30f)));
            Assert.That(spear.keywords, Is.Empty);
        }

        [Test]
        public void ItCanHurtAnyTankButFeelsTheRifles()
        {
            var spear = Team.leader.PrimaryWeapon;
            // Penetration 6 beats every tank's armor so far (the Stinger's 5 included): 150% damage.
            foreach (var tank in new[] { "Sagittarius MBT", "Marshal Command Tank", "Stinger Heavy Robot", "Tyrant Hover-tank" })
            {
                int armor = UnitNamed(tank).leader.armorLevel;
                Assert.That(ArmorPenetration.Resolve(spear.baseDamage, spear.PenetrationValue, armor).Damage, Is.EqualTo(7), tank);
            }
            var kobold = Team.leader;
            Assert.That((kobold.health, kobold.armorLevel), Is.EqualTo((5, 0)), "frail: rifles hurt them fully");
        }
    }
}
