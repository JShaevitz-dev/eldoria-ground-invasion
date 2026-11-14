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
    /// The ISF Ranger (Specialist): the original Eldoria's Sniper, renamed. One ISF Ranger (Health 5, Armor 0, Accuracy
    /// 100%, Move 15 m; Biological, Sniper Precision, Solo) with a Sniper Rifle (Damage 5, Shots 1, Penetration 1, +50%
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
            Assert.That(unit.description, Does.Contain("Ranger").And.Contain("50 m"));
            var commander = Catalog.Available.Single(u => u.displayName == "Field Commander");
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { commander, unit }, Isf), Is.True);
            Assert.That(ArmyRules.WhyCannotAdd(new List<UnitTemplate> { commander, unit }, unit, Isf), Does.Contain("Already in the army"),
                "a Specialist: one per army");
        }

        [Test]
        public void TheIsfRangerHasTheOldSnipersStats()
        {
            var r = Ranger.leader;
            Assert.That(r.displayName, Is.EqualTo("ISF Ranger"));
            Assert.That((r.health, r.armorLevel, r.accuracy, r.movementRange), Is.EqualTo((5, 0, 100, 15f)));
            Assert.That(r.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Sniper Precision", "Solo" }));
            Assert.That(r.abilities, Is.Empty);
            Assert.That(r.prefab, Is.Not.Null, "the old Sniper model");
            var rifle = r.weapons.Single();
            Assert.That(r.PrimaryWeapon, Is.SameAs(rifle));
            Assert.That(rifle.displayName, Is.EqualTo("Sniper Rifle"));
            Assert.That((rifle.baseDamage, rifle.shots, rifle.PenetrationValue, rifle.accuracyBonus, rifle.range), Is.EqualTo((5, 1, (int?)1, 50, 50f)));
            Assert.That(rifle.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Deployed" }));
            Assert.That(UnitInfo.SoldierLines(Ranger).Single(),
                Is.EqualTo("ISF Ranger \u2014 Health 5 \u00b7 Armor 0 \u00b7 Accuracy 100% \u00b7 Move 15 m (sprint 30 m) \u00b7 Sniper Rifle"));
            Assert.That(UnitInfo.SharedKeywords(Ranger), Does.Contain("Sniper Precision"));
        }
    }
}
