using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Dragon Hierarch's escort: 2 Dragonborn Honorguard, with the Dragonborn Footman's stats but 95% accuracy.
    /// </summary>
    public class DragonbornHonorguardTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static UnitTemplate Hierarch => Catalog.Available.Single(u => u.displayName == "Dragon Hierarch");
        private static SoldierTemplate Footman => Catalog.Available.Single(u => u.displayName == "Dragonborn Squad").members.Single().soldier;

        [Test]
        public void TheDragonHierarchLeadsTwoHonorguard()
        {
            var member = Hierarch.members.Single();
            Assert.That(Hierarch.leader.displayName, Is.EqualTo("Dragon Hierarch"), "the dragon still leads");
            Assert.That(member.soldier.displayName, Is.EqualTo("Dragonborn Honorguard"));
            Assert.That(member.count, Is.EqualTo(2));
            Assert.That(Hierarch.role, Is.EqualTo(UnitRole.Commander));
        }

        [Test]
        public void TheyHaveTheFootmansStatsBut95PercentAccuracy()
        {
            var h = Hierarch.members.Single().soldier;
            var f = Footman;
            Assert.That(f.displayName, Is.EqualTo("Dragonborn Footman"));
            Assert.That(h, Is.Not.SameAs(f));
            Assert.That(h.accuracy, Is.EqualTo(95));
            Assert.That(f.accuracy, Is.EqualTo(75), "the Footman is unchanged");
            Assert.That((h.health, h.armorLevel, h.movementRange, h.radius, h.height), Is.EqualTo((f.health, f.armorLevel, f.movementRange, f.radius, f.height)));
            Assert.That((h.health, h.armorLevel, h.movementRange), Is.EqualTo((10, 2, 10f)));
            Assert.That(h.keywords, Is.EqualTo(f.keywords));
            Assert.That(h.weapons, Is.EqualTo(f.weapons), "both Dragon Glaives");
            Assert.That(h.primaryWeapon, Is.SameAs(f.primaryWeapon));
            Assert.That(h.abilities, Is.Empty);
            Assert.That(h.prefab, Is.Not.Null);
            Assert.That(h.prefab, Is.Not.SameAs(f.prefab), "their own model (gold armor, crimson cape)");
        }

        [Test]
        public void ADeployedDragonHierarchHasItsHonorguard()
        {
            var unit = ArmyDeployment.Spawn(Hierarch, Side.AI, Vector3.zero, Quaternion.identity, "Dragon Hierarch", null, Draconis);
            try
            {
                Assert.That(unit.Soldiers.Count, Is.EqualTo(3));
                Assert.That(unit.Leader.Template.displayName, Is.EqualTo("Dragon Hierarch"));
                var guards = unit.Soldiers.Where(s => s != unit.Leader).ToList();
                Assert.That(guards.Count, Is.EqualTo(2));
                Assert.That(guards.All(g => g.Accuracy == 95 && g.PrimaryWeapon.Name == "Dragon Glaive (Range)"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
