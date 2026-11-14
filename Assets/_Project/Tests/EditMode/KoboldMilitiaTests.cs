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
    /// The Draconis Imperialis's Kobold Militia (Infantry): 1 Kobold Sergeant + 19 Kobold Militiamen, all Health 5, Armor 0,
    /// Accuracy 50%, Move 20 m, Biological and Swarm, with Assault Rifles the Draconis Imperialis calls "Fang Casters".
    /// </summary>
    public class KoboldMilitiaTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static UnitTemplate Militia => Catalog.Available.Single(u => u.displayName == "Kobold Militia");

        [Test]
        public void TheKoboldMilitiaIsADraconisInfantryUnitOfTwentyKobolds()
        {
            var unit = Militia;
            Assert.That(Draconis.Has(unit), Is.True);
            Assert.That(Catalog.Factions.First().Has(unit), Is.False, "not the ISF's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Infantry));
            Assert.That(unit.Size, Is.EqualTo(20));
            Assert.That(ArmyRules.Composition(unit), Is.EqualTo("Kobold Sergeant + 19\u00d7 Kobold Militiaman"));
            Assert.That(unit.description, Does.Contain("Wingless anthropomorphic dragons").And.Contain("two-thirds the height of a human"));
            var hierarch = Catalog.Available.Single(u => u.displayName == "Dragon Hierarch");
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { hierarch, unit, unit }, Draconis), Is.True, "Infantry can be taken more than once");
        }

        [Test]
        public void SergeantAndMilitiamenShareTheirStats()
        {
            var sergeant = Militia.leader;
            var militiaman = Militia.members.Single().soldier;
            Assert.That(sergeant.displayName, Is.EqualTo("Kobold Sergeant"));
            Assert.That(militiaman.displayName, Is.EqualTo("Kobold Militiaman"));
            foreach (var s in new[] { sergeant, militiaman })
            {
                Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((5, 0, 50, 20f)), s.displayName);
                Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Swarm" }), s.displayName);
                Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Assault Rifle" }), s.displayName);
                Assert.That(s.abilities, Is.Empty, s.displayName);
                Assert.That(s.height, Is.EqualTo(1.2f), "two-thirds of a human's 1.8 m");
                Assert.That(s.prefab.GetComponent<CapsuleCollider>().height, Is.EqualTo(1.2f), "the model was rebuilt at that size");
                Assert.That(s.prefab, Is.Not.Null, s.displayName);
            }
            Assert.That(sergeant.prefab, Is.Not.SameAs(militiaman.prefab), "the Sergeant has its own model (gold insignia)");
        }

        [Test]
        public void TheDraconisImperialisCallsTheAssaultRifleTheFangCaster()
        {
            var rifle = Militia.leader.weapons.Single();
            Assert.That(Draconis.NameFor(rifle), Is.EqualTo("Fang Caster"));
            Assert.That(Catalog.Factions.First().NameFor(rifle), Is.EqualTo("Assault Rifle"), "the ISF still calls it an Assault Rifle");
            var stats = WeaponStats.For(rifle, Draconis);
            Assert.That(stats.Name, Is.EqualTo("Fang Caster"));
            Assert.That((stats.BaseDamage, stats.Shots, stats.PenetrationValue, stats.Range), Is.EqualTo((4, 1, (int?)1, 20f)), "the same stats");
            Assert.That(UnitInfo.SoldierLines(Militia, Draconis), Is.EqualTo(new[]
            {
                "Kobold Sergeant \u2014 Health 5 \u00b7 Armor 0 \u00b7 Accuracy 50% \u00b7 Move 20 m (sprint 40 m) \u00b7 Fang Caster",
                "19\u00d7 Kobold Militiaman \u2014 Health 5 \u00b7 Armor 0 \u00b7 Accuracy 50% \u00b7 Move 20 m (sprint 40 m) \u00b7 Fang Caster",
            }));
        }

        [Test]
        public void ADeployedKoboldMilitiaHasSwarmCohesionAndFangCasters()
        {
            var unit = ArmyDeployment.Spawn(Militia, Side.AI, Vector3.zero, Quaternion.identity, "Kobold Militia", null, Draconis);
            try
            {
                Assert.That(unit.Soldiers.Count, Is.EqualTo(20));
                Assert.That(MovementRules.CohesionRangeFor(unit), Is.EqualTo(10f), "Swarm: 10 m cohesion");
                Assert.That(unit.OutOfCohesion(), Is.Empty, "deploys in cohesion");
                Assert.That(unit.Soldiers.All(s => s.PrimaryWeapon.Name == "Fang Caster"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
