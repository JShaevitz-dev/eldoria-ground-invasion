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
    /// The Rask's Rask Redhand (Specialist): the Rask Ultima's stats but Armor 1 and no Officer, a Solntse Laser Rifle and
    /// a Repair Kit.
    /// </summary>
    public class RaskRedhandTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Rask => Catalog.Factions.ElementAt(2);
        private static UnitTemplate Redhand => Catalog.Available.Single(u => u.displayName == "Rask Redhand");
        private static SoldierTemplate Ultima => Catalog.Available.Single(u => u.displayName == "Rask Ultima").leader;
        private static Ability Repair => AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/Medkit.asset");

        [Test]
        public void TheRedhandIsARaskSpecialistOfOne()
        {
            var unit = Redhand;
            Assert.That(Rask.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Take(2).Any(f => f.Has(unit)), Is.False, "only the Rask's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.Contain("Medkit").And.Contain("Specialist"));
        }

        [Test]
        public void ItHasTheRaskUltimasStatsButArmor1AndNoOfficerALaserRifleAndAMedkit()
        {
            var b = Redhand.leader;
            var u = Ultima;
            Assert.That(b.displayName, Is.EqualTo("Rask Redhand"));
            Assert.That((b.health, b.accuracy, b.movementRange, b.radius, b.height), Is.EqualTo((u.health, u.accuracy, u.movementRange, u.radius, u.height)));
            Assert.That((b.health, b.armorLevel, u.armorLevel), Is.EqualTo((10, 1, 2)), "Armor 1; the Ultima keeps 2");
            Assert.That(b.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Solo" }), "no Officer");
            Assert.That(u.keywords.Select(k => k.displayName), Does.Contain("Officer"), "the Ultima keeps it");
            Assert.That(b.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Assault Rifle" }));
            Assert.That(WeaponStats.For(b.PrimaryWeapon, Rask).Name, Is.EqualTo("Solntse Laser Rifle"));
            Assert.That(WeaponStats.For(b.PrimaryWeapon, Rask).BaseDamage, Is.EqualTo(6));
            Assert.That(b.equipment.Select(e => e.displayName), Is.EqualTo(new[] { "Medkit" }));
            Assert.That(b.prefab, Is.Not.Null);
            Assert.That(b.prefab.transform.Find("Model/Red Hand"), Is.Not.Null);
            Assert.That(b.prefab.transform.Find("Model/Rifle"), Is.Not.Null);
        }

        [Test]
        public void InBattleItCanHealAWoundedRifleman()
        {
            var unit = ArmyDeployment.Spawn(Redhand, Side.AI, Vector3.zero, Quaternion.identity, "Redhand", null, Rask);
            var team = ArmyDeployment.Spawn(Catalog.Available.Single(x => x.displayName == "Rask Rifle Team"), Side.AI, new Vector3(3f, 0f, 0f), Quaternion.identity, "Rifles", null, Rask);
            try
            {
                var r = unit.Leader;
                r.transform.position = Vector3.zero;
                var patient = team.Leader;
                patient.transform.position = new Vector3(3f, 0f, 0f);
                Assert.That(r.Abilities, Is.EqualTo(new[] { Repair }));
                Assert.That(r.PrimaryWeapon.Name, Is.EqualTo("Solntse Laser Rifle"));
                Assert.That(ActiveAbilities.WhyCannotUse(r, Repair, patient), Does.Contain("full health"));
                patient.TakeDamage(4);
                Assert.That(ActiveAbilities.Use(r, Repair, patient).Used, Is.True);
                Assert.That(patient.Health, Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
                Object.DestroyImmediate(team.gameObject);
            }
        }
    }
}
