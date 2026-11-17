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
    /// The ISF's Sagittarius MBT (Heavy Support): the original Eldoria's Medium Tank, renamed. One Sagittarius MBT (Health
    /// 20, Armor 4, Accuracy 75%, Move 30 m; Mechanical, Tank, Unstable; a Frag Grenade usable every turn; 100mm Cannon:
    /// Damage 10, Shots 1, Penetration 4, 50 m, Manual Load).
    /// </summary>
    public class SagittariusMbtTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.First();
        private static UnitTemplate Mbt => Catalog.Available.Single(u => u.displayName == "Sagittarius MBT");

        [Test]
        public void TheSagittariusIsAnIsfHeavySupportTank()
        {
            var unit = Mbt;
            Assert.That(Isf.Has(unit), Is.True);
            Assert.That(Catalog.Factions.ElementAt(1).Has(unit), Is.False, "not the Draconis Imperialis's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.HeavySupport));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.Contain("100mm Cannon"));
            var commander = Catalog.Available.Single(u => u.displayName == "Field Commander");
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { commander, unit, unit }, Isf), Is.True, "more than one tank is fine (well within the Supply Cap)");
        }

        [Test]
        public void ItHasTheOldMediumTanksStats()
        {
            var t = Mbt.leader;
            Assert.That(t.displayName, Is.EqualTo("Sagittarius MBT"));
            Assert.That((t.health, t.armorLevel, t.accuracy, t.movementRange), Is.EqualTo((20, 4, 75, 30f)));
            Assert.That(t.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Mechanical", "Tank", "Unstable", "Large" }));
            Assert.That(t.HasKeyword("Biological"), Is.False, "a machine");
            Assert.That(t.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Frag Grenade" }));
            Assert.That(t.abilities.Single().usesPerBattle, Is.EqualTo(0), "no once-per-battle limit");
            var cannon = t.weapons.Single();
            Assert.That(t.PrimaryWeapon, Is.SameAs(cannon));
            Assert.That(cannon.displayName, Is.EqualTo("100mm Cannon"));
            Assert.That((cannon.baseDamage, cannon.shots, cannon.PenetrationValue, cannon.range), Is.EqualTo((10, 1, (int?)4, 50f)));
            Assert.That(cannon.HasKeyword("Manual Load"), Is.True);
            Assert.That((t.radius, t.height), Is.EqualTo((1.8f, 2.3f)), "a wide hull");
            Assert.That(t.prefab, Is.Not.Null, "the old Medium Tank model");
            Assert.That(UnitInfo.SoldierLines(Mbt).Single(),
                Is.EqualTo("Sagittarius MBT \u2014 Health 20 \u00b7 Armor 4 \u00b7 Accuracy 75% \u00b7 Move 30 m (can't sprint) \u00b7 100mm Cannon"));
        }

        [Test]
        public void ItCantSprintAndIsNeverInCloseRange()
        {
            var unit = UnitFactory.Spawn(Mbt, Side.Player, Vector3.zero, Quaternion.identity);
            var enemy = UnitFactory.Spawn(Catalog.Available.Single(u => u.displayName == "Dragonborn Squad"), Side.AI,
                new Vector3(0f, 0f, 2.2f), Quaternion.Euler(0f, 180f, 0f));
            try
            {
                var tank = unit.Leader;
                Assert.That(tank.CanSprint, Is.False, "Mechanical");
                Assert.That(CloseRange.IsInCloseRange(tank), Is.False, "Mechanical soldiers are never in close range");
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
                Object.DestroyImmediate(enemy.gameObject);
            }
        }
    }
}
