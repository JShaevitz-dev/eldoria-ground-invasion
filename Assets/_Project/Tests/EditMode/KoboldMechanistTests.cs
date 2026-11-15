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
    /// The Draconis Imperialis's Kobold Mechanist (Specialist): the Kobold Militiaman's stats but Armor 1 and Solo, a Fang
    /// Caster and a Repair Kit.
    /// </summary>
    public class KoboldMechanistTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static UnitTemplate Mechanist => Catalog.Available.Single(u => u.displayName == "Kobold Mechanist");
        private static SoldierTemplate Militiaman => Catalog.Available.Single(u => u.displayName == "Kobold Militia").members.Single().soldier;
        private static Ability Repair => AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/RepairKit.asset");

        [Test]
        public void TheMechanistIsADraconisSpecialistOfOne()
        {
            var unit = Mechanist;
            Assert.That(Draconis.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Where((f, i) => i != 1).Any(f => f.Has(unit)), Is.False, "only the Draconis's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.Contain("Repair Kit").And.Contain("Specialist"));
        }

        [Test]
        public void ItHasTheMilitiamansStatsButArmor1AndSolo()
        {
            var m = Mechanist.leader;
            var k = Militiaman;
            Assert.That(m.displayName, Is.EqualTo("Kobold Mechanist"));
            Assert.That((m.health, m.accuracy, m.movementRange, m.radius, m.height), Is.EqualTo((k.health, k.accuracy, k.movementRange, k.radius, k.height)));
            Assert.That((m.armorLevel, k.armorLevel), Is.EqualTo((1, 0)));
            Assert.That(m.keywords.Select(x => x.displayName), Is.EqualTo(k.keywords.Select(x => x.displayName).Concat(new[] { "Solo" })));
            Assert.That(m.weapons, Is.EqualTo(k.weapons), "the Assault Rifle");
            Assert.That(m.equipment.Select(e => e.displayName), Is.EqualTo(new[] { "Repair Kit" }));
            Assert.That(WeaponStats.For(m.PrimaryWeapon, Draconis).Name, Is.EqualTo("Fang Caster"));
            Assert.That(m.prefab, Is.Not.Null);
            Assert.That(m.prefab.transform.Find("Model/Wrench"), Is.Not.Null);
        }

        [Test]
        public void InBattleItCanRepairATyrant()
        {
            var unit = ArmyDeployment.Spawn(Mechanist, Side.AI, Vector3.zero, Quaternion.identity, "Mechanist", null, Draconis);
            var tyrant = ArmyDeployment.Spawn(Catalog.Available.Single(u => u.displayName == "Tyrant Hover-tank"), Side.AI, new Vector3(4f, 0f, 0f), Quaternion.identity, "Tyrant", null, Draconis);
            try
            {
                var m = unit.Leader;
                m.transform.position = Vector3.zero;
                tyrant.Leader.transform.position = new Vector3(4f, 0f, 0f);
                Assert.That((m.ArmorLevel, m.PrimaryWeapon.Name), Is.EqualTo((1, "Fang Caster")));
                Assert.That(m.Abilities, Is.EqualTo(new[] { Repair }));
                Assert.That(ActiveAbilities.WhyCannotUse(m, Repair, tyrant.Leader), Does.Contain("full health"));
                tyrant.Leader.TakeDamage(12);
                Assert.That(ActiveAbilities.Use(m, Repair, tyrant.Leader).Used, Is.True);
                Assert.That(tyrant.Leader.Health, Is.EqualTo(13));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
                Object.DestroyImmediate(tyrant.gameObject);
            }
        }
    }
}
