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
    /// The ISF's Olympian Mech (Heavy Support): the original Eldoria's Stahl Colossus, renamed. One Olympian Mech (Health 15,
    /// Armor 3, Accuracy 75%, Move 15 m; Solo, Mechanical; Iron Claw and Stabilizers; Machine Gun and Iron Claw).
    /// </summary>
    public class OlympianMechTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.First();
        private static UnitTemplate Mech => Catalog.Available.Single(u => u.displayName == "Olympian Mech");

        [Test]
        public void TheOlympianMechIsAnIsfHeavySupportUnit()
        {
            var unit = Mech;
            Assert.That(Isf.Has(unit), Is.True);
            Assert.That(Catalog.Factions.ElementAt(1).Has(unit), Is.False, "not the Draconis Imperialis's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.HeavySupport));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.Contain("Stabilizers").And.Not.Contain("Draksa").And.Not.Contain("Stahl"));
            var commander = Catalog.Available.Single(u => u.displayName == "Field Commander");
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { commander, unit, unit }, Isf), Is.True, "more than one is fine");
        }

        [Test]
        public void ItHasTheOldStahlColossusStats()
        {
            var m = Mech.leader;
            Assert.That(m.displayName, Is.EqualTo("Olympian Mech"));
            Assert.That((m.health, m.armorLevel, m.accuracy, m.movementRange), Is.EqualTo((15, 3, 75, 15f)));
            Assert.That(m.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Solo", "Mechanical" }));
            Assert.That(m.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Iron Claw", "Stabilizers" }));
            Assert.That(m.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Machine Gun", "Iron Claw" }));
            Assert.That(m.PrimaryWeapon.displayName, Is.EqualTo("Machine Gun"));
            Assert.That((m.radius, m.height), Is.EqualTo((0.7f, 3.6f)));
            Assert.That(m.prefab, Is.Not.Null, "the old Stahl Colossus model");
            Assert.That(m.abilities.Single(a => a.displayName == "Stabilizers").description, Does.Not.Contain("Stahl"));
            Assert.That(UnitInfo.AbilityLines(Mech), Is.EqualTo(new[] { "Iron Claw (active)", "Stabilizers (passive)" }));
        }

        [Test]
        public void StabilizersLetItsMachineGunFireAfterMoving()
        {
            var unit = UnitFactory.Spawn(Mech, Side.Player, Vector3.zero, Quaternion.identity);
            try
            {
                var mech = unit.Leader;
                Assert.That(mech.CanSprint, Is.False, "Mechanical");
                mech.GainKeyword(KeywordLibrary.Get(KeywordLibrary.Moved));
                Assert.That(mech.PrimaryWeapon.Template.HasKeyword("Deployed"), Is.True);
                Assert.That(WeaponUse.CanUse(mech, mech.PrimaryWeapon), Is.True, "Stabilizers nullify Deployed");
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
