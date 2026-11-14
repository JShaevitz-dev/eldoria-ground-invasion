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
    /// The Rask's Rask Rifle Team (Infantry): 1 Rask Superior + 19 Rask Riflemen with the Kobold Militiaman's stats but Armor 1
    /// (Health 5, Armor 1, Accuracy 50%, Move 20 m; Biological, Swarm), carrying Assault Rifles the Rask call "Solntse Laser Rifles",
    /// which do 6 damage for them.
    /// </summary>
    public class RaskRifleTeamTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Rask => Catalog.Factions.ElementAt(2);
        private static UnitTemplate Team => Catalog.Available.Single(u => u.displayName == "Rask Rifle Team");

        [Test]
        public void TheRaskRifleTeamIsARaskInfantryUnitOfTwenty()
        {
            var unit = Team;
            Assert.That(Rask.displayName, Is.EqualTo("Most Holy Army of The Rask Divine Collective"));
            Assert.That(Rask.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Take(2).Any(f => f.Has(unit)), Is.False, "only the Rask's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Infantry));
            Assert.That(unit.Size, Is.EqualTo(20));
            Assert.That(ArmyRules.Composition(unit), Is.EqualTo("Rask Superior + 19\u00d7 Rask Rifleman"));
            Assert.That(ArmyBuilderScreen.FactionCanFieldArmy(Catalog, Rask), Is.True, "the Rask Ultima is their Commander");
        }

        [Test]
        public void TheyHaveTheKoboldMilitiamansStatsButArmor1()
        {
            var kobold = Catalog.Available.Single(u => u.displayName == "Kobold Militia").members.Single().soldier;
            var superior = Team.leader;
            var rifleman = Team.members.Single().soldier;
            Assert.That(superior.displayName, Is.EqualTo("Rask Superior"));
            Assert.That(rifleman.displayName, Is.EqualTo("Rask Rifleman"));
            foreach (var s in new[] { superior, rifleman })
            {
                Assert.That((s.health, s.accuracy, s.movementRange), Is.EqualTo((kobold.health, kobold.accuracy, kobold.movementRange)), s.displayName);
                Assert.That(kobold.armorLevel, Is.EqualTo(0), "the kobolds stay at Armor 0");
                Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((5, 1, 50, 20f)), s.displayName);
                Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Swarm" }), s.displayName);
                Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Assault Rifle" }), s.displayName);
                Assert.That(s.height, Is.EqualTo(1.2f), "two-thirds of a human's height");
                Assert.That(s.prefab, Is.Not.Null, s.displayName);
                Assert.That(s.prefab.GetComponent<CapsuleCollider>().height, Is.EqualTo(1.2f));
            }
            Assert.That(superior.prefab, Is.Not.SameAs(rifleman.prefab), "the Superior has its own model (gold crest)");
        }

        [Test]
        public void TheRaskCallTheAssaultRifleTheSolntseLaserRifleAndItDoes6Damage()
        {
            var rifle = Team.leader.weapons.Single();
            var stats = WeaponStats.For(rifle, Rask);
            Assert.That(stats.Name, Is.EqualTo("Solntse Laser Rifle"));
            Assert.That((stats.BaseDamage, stats.Shots, stats.PenetrationValue, stats.Range), Is.EqualTo((6, 1, (int?)1, 20f)), "Damage 6; the rest unchanged");
            Assert.That(rifle.baseDamage, Is.EqualTo(4), "the Assault Rifle itself is unchanged");
            Assert.That(WeaponStats.For(rifle, Catalog.Factions.First()).BaseDamage, Is.EqualTo(4), "the ISF's still do 4");
            Assert.That(WeaponStats.For(rifle, Catalog.Factions.ElementAt(1)).Name, Is.EqualTo("Fang Caster"), "the Draconis name is theirs alone");
            Assert.That(UnitInfo.WeaponLines(Team, Rask).Single(), Is.EqualTo("Solntse Laser Rifle \u2014 Damage 6 \u00b7 Shots 1 \u00b7 Penetration 1 \u00b7 Range 20 m"));
        }

        [Test]
        public void ADeployedRaskRifleTeamFiresSolntseLaserRifles()
        {
            var unit = ArmyDeployment.Spawn(Team, Side.AI, Vector3.zero, Quaternion.identity, "Rask Rifle Team", null, Rask);
            try
            {
                Assert.That(unit.Soldiers.Count, Is.EqualTo(20));
                Assert.That(MovementRules.CohesionRangeFor(unit), Is.EqualTo(10f), "Swarm");
                Assert.That(unit.Soldiers.All(s => s.PrimaryWeapon.Name == "Solntse Laser Rifle" && s.PrimaryWeapon.BaseDamage == 6), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
