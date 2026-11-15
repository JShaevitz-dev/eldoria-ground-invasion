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
    /// The Rask's Rask Blackhand (Specialist): the Rask Ultima's stats but Armor 1 and no Officer, a Solntse Laser Rifle and
    /// a Repair Kit.
    /// </summary>
    public class RaskBlackhandTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Rask => Catalog.Factions.ElementAt(2);
        private static UnitTemplate Blackhand => Catalog.Available.Single(u => u.displayName == "Rask Blackhand");
        private static SoldierTemplate Ultima => Catalog.Available.Single(u => u.displayName == "Rask Ultima").leader;
        private static Ability Repair => AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/RepairKit.asset");

        [Test]
        public void TheBlackhandIsARaskSpecialistOfOne()
        {
            var unit = Blackhand;
            Assert.That(Rask.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Take(2).Any(f => f.Has(unit)), Is.False, "only the Rask's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.Contain("Repair Kit").And.Contain("Specialist"));
        }

        [Test]
        public void ItHasTheRaskUltimasStatsButArmor1AndNoOfficerALaserRifleAndARepairKit()
        {
            var b = Blackhand.leader;
            var u = Ultima;
            Assert.That(b.displayName, Is.EqualTo("Rask Blackhand"));
            Assert.That((b.health, b.accuracy, b.movementRange, b.radius, b.height), Is.EqualTo((u.health, u.accuracy, u.movementRange, u.radius, u.height)));
            Assert.That((b.health, b.armorLevel, u.armorLevel), Is.EqualTo((10, 1, 2)), "Armor 1; the Ultima keeps 2");
            Assert.That(b.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Solo" }), "no Officer");
            Assert.That(u.keywords.Select(k => k.displayName), Does.Contain("Officer"), "the Ultima keeps it");
            Assert.That(b.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Assault Rifle" }));
            Assert.That(WeaponStats.For(b.PrimaryWeapon, Rask).Name, Is.EqualTo("Solntse Laser Rifle"));
            Assert.That(WeaponStats.For(b.PrimaryWeapon, Rask).BaseDamage, Is.EqualTo(6));
            Assert.That(b.equipment.Select(e => e.displayName), Is.EqualTo(new[] { "Repair Kit" }));
            Assert.That(b.prefab, Is.Not.Null);
            Assert.That(b.prefab.transform.Find("Model/Black Hand"), Is.Not.Null);
            Assert.That(b.prefab.transform.Find("Model/Rifle"), Is.Not.Null);
        }

        [Test]
        public void InBattleItCanRepairAStinger()
        {
            var unit = ArmyDeployment.Spawn(Blackhand, Side.AI, Vector3.zero, Quaternion.identity, "Blackhand", null, Rask);
            var stinger = ArmyDeployment.Spawn(Catalog.Available.Single(x => x.displayName == "Stinger Heavy Robot"), Side.AI, new Vector3(4f, 0f, 0f), Quaternion.identity, "Stinger", null, Rask);
            try
            {
                var b = unit.Leader;
                b.transform.position = Vector3.zero;
                stinger.Leader.transform.position = new Vector3(4f, 0f, 0f);
                Assert.That(b.Abilities, Is.EqualTo(new[] { Repair }));
                Assert.That(b.PrimaryWeapon.Name, Is.EqualTo("Solntse Laser Rifle"));
                stinger.Leader.TakeDamage(15);
                Assert.That(ActiveAbilities.Use(b, Repair, stinger.Leader).Used, Is.True);
                Assert.That(stinger.Leader.Health, Is.EqualTo(20));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
                Object.DestroyImmediate(stinger.gameObject);
            }
        }
    }
}
