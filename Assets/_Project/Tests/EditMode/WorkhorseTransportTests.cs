using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The ISF's Workhorse Transport (Transport): Health 10, Armor 3, Accuracy 75%, Move 40 m; Transport, Mechanical;
    /// Stabilizers; Machine Gun. A six-wheeled armored truck that carries one unit of Biological soldiers.
    /// </summary>
    public class WorkhorseTransportTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.First();
        private static UnitTemplate Workhorse => Catalog.Available.Single(u => u.displayName == "Workhorse Transport");
        private static Ability Ability(string file) => AssetDatabase.LoadAssetAtPath<Ability>($"Assets/_Project/Data/Abilities/{file}.asset");

        [Test]
        public void TheWorkhorseIsAnIsfTransportOfOne()
        {
            var unit = Workhorse;
            Assert.That(Isf.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Skip(1).Any(f => f.Has(unit)), Is.False, "only the ISF's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Transport));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.Contain("Transport").And.Contain("Embark"));
        }

        [Test]
        public void ItHasTheGivenStatsKeywordsAndAbilities()
        {
            var s = Workhorse.leader;
            Assert.That(s.displayName, Is.EqualTo("Workhorse Transport"));
            Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((10, 3, 75, 40f)));
            Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Transport", "Mechanical" }));
            Assert.That(s.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Stabilizers" }));
            Assert.That(s.AllAbilities(), Is.EquivalentTo(new[] { Ability("Stabilizers"), Ability("Embark"), Ability("Disembark") }), "Embark and Disembark from Transport");
            Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Machine Gun" }));
            Assert.That(s.prefab, Is.Not.Null);
            Assert.That(s.prefab.GetComponentsInChildren<Transform>().Count(t => t.name == "Wheel"), Is.EqualTo(6), "six wheels");
        }

        [Test]
        public void ItsSupplyCost()
        {
            // 10 health + Machine Gun (5 × 5) + Armor 3 + Embark and Disembark (2 active) + Stabilizers (passive).
            var p = SupplyCost.Breakdown(Workhorse, Isf);
            Assert.That((p.Health, p.Damage, p.Armor, p.ActiveAbilities, p.PassiveAbilities, p.ArmorTax, p.Total), Is.EqualTo((10, 25, 3, 2, 1, 0, 88)));
            Assert.That(Workhorse.supplyCost, Is.EqualTo(88));
        }

        [Test]
        public void ItCarriesARifleSquadAndFiresOnTheMove()
        {
            var truck = ArmyDeployment.Spawn(Workhorse, Side.Player, Vector3.zero, Quaternion.identity, "Workhorse", null, Isf);
            var squad = ArmyDeployment.Spawn(Catalog.Available.Single(u => u.displayName == "ISF Rifle Squad"), Side.Player, new Vector3(0f, 0f, -4f), Quaternion.identity, "Rifles", null, Isf);
            try
            {
                var driver = truck.Leader;
                driver.transform.position = Vector3.zero;
                var shift = new Vector3(0f, 0f, -3.5f) - squad.Leader.Position;
                foreach (var s in squad.Soldiers) s.transform.position += shift;
                Assert.That(driver.CanSprint, Is.False, "Mechanical");
                Assert.That(ActiveAbilities.Use(driver, Ability("Embark"), null, squad).Used, Is.True);
                Assert.That(truck.Passenger, Is.SameAs(squad));
                driver.GainKeyword(KeywordLibrary.Get(KeywordLibrary.Moved));
                Assert.That(WeaponUse.CanUse(driver, driver.PrimaryWeapon), Is.True, "Stabilizers: the Machine Gun fires after moving");
            }
            finally
            {
                Object.DestroyImmediate(squad.gameObject);
                Object.DestroyImmediate(truck.gameObject);
            }
        }
    }
}
