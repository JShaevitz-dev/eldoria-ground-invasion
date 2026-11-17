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
    /// The ISF's Marshal Command Tank (Commander): the Sagittarius MBT's stats plus the Officer keyword, with a 100mm Cannon;
    /// the Sagittarius model with antennae and a satellite dish.
    /// </summary>
    public class MarshalCommandTankTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.First();
        private static UnitTemplate Marshal => Catalog.Available.Single(u => u.displayName == "Marshal Command Tank");
        private static SoldierTemplate Mbt => Catalog.Available.Single(u => u.displayName == "Sagittarius MBT").leader;

        [Test]
        public void TheMarshalIsAnIsfCommander()
        {
            var unit = Marshal;
            Assert.That(Isf.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Skip(1).Any(f => f.Has(unit)), Is.False, "only the ISF's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Commander));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { unit }, Isf), Is.True);
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { unit, Catalog.Available.Single(u => u.displayName == "Field Commander") }, Isf), Is.False, "only 1 Commander");
        }

        [Test]
        public void ItHasTheSagittariusStatsPlusOfficer()
        {
            var m = Marshal.leader;
            var t = Mbt;
            Assert.That(m.displayName, Is.EqualTo("Marshal Command Tank"));
            Assert.That((m.health, m.armorLevel, m.accuracy, m.movementRange, m.radius, m.height), Is.EqualTo((t.health, t.armorLevel, t.accuracy, t.movementRange, t.radius, t.height)));
            Assert.That((m.health, m.armorLevel, m.accuracy, m.movementRange), Is.EqualTo((20, 4, 75, 30f)));
            Assert.That(m.keywords.Select(k => k.displayName), Is.EquivalentTo(t.keywords.Select(k => k.displayName).Concat(new[] { "Officer" })));
            Assert.That(m.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Mechanical", "Tank", "Unstable", "Officer", "Large" }));
            Assert.That(t.keywords.Select(k => k.displayName), Does.Not.Contain("Officer"), "the Sagittarius is unchanged");
            Assert.That(m.abilities, Is.EqualTo(t.abilities), "the tank's every-turn Frag Grenade");
            Assert.That(m.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "100mm Cannon" }));
        }

        [Test]
        public void ItIsTheSagittariusModelWithAntennaeAndADish()
        {
            var prefab = Marshal.leader.prefab;
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab, Is.Not.SameAs(Mbt.prefab));
            foreach (var part in new[] { "Hull", "Turret", "Track_L", "Track_R", "Weapon" })
                Assert.That(prefab.transform.Find(part), Is.Not.Null, $"the Sagittarius's {part}");
            Assert.That(prefab.GetComponentsInChildren<Transform>().Count(t => t.name == "Antenna"), Is.EqualTo(2));
            Assert.That(prefab.transform.Find("Satellite Dish"), Is.Not.Null);
        }

        [Test]
        public void ADeployedMarshalIsAnOfficerThatCantSprint()
        {
            var unit = ArmyDeployment.Spawn(Marshal, Side.Player, Vector3.zero, Quaternion.identity, "Marshal", null, Isf);
            try
            {
                var tank = unit.Leader;
                Assert.That(tank.HasKeyword("Officer"), Is.True);
                Assert.That(tank.CanSprint, Is.False, "Mechanical");
                Assert.That(tank.PrimaryWeapon.Name, Is.EqualTo("100mm Cannon"));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
