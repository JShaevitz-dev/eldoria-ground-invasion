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
    /// The Rask's Commander, the Rask Ultima: one Rask Ultima with the Rask Rifleman's stats but Health 10 and Armor 2
    /// (Accuracy 50%, Move 20 m; Biological, Solo, Officer) and a Pistol. It makes the Rask playable.
    /// </summary>
    public class RaskUltimaTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Rask => Catalog.Factions.ElementAt(2);
        private static UnitTemplate Ultima => Catalog.Available.Single(u => u.displayName == "Rask Ultima");
        private static SoldierTemplate Rifleman => Catalog.Available.Single(u => u.displayName == "Rask Rifle Team").members.Single().soldier;

        [Test]
        public void TheRaskUltimaIsTheRaskCommander()
        {
            var unit = Ultima;
            Assert.That(Rask.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Take(2).Any(f => f.Has(unit)), Is.False, "only the Rask's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Commander));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(ArmyRules.Composition(unit), Is.EqualTo("Rask Ultima"));
            Assert.That(ArmyBuilderScreen.FactionCanFieldArmy(Catalog, Rask), Is.True, "a Commander makes the Rask playable");
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { unit }, Rask), Is.True);
        }

        [Test]
        public void ItHasTheRiflemansStatsBut10HealthAnd2Armor()
        {
            var u = Ultima.leader;
            var r = Rifleman;
            Assert.That(u.displayName, Is.EqualTo("Rask Ultima"));
            Assert.That((u.health, u.armorLevel), Is.EqualTo((10, 2)));
            Assert.That((u.accuracy, u.movementRange, u.radius, u.height), Is.EqualTo((r.accuracy, r.movementRange, r.radius, r.height)));
            Assert.That((u.accuracy, u.movementRange), Is.EqualTo((75, 20f)));
            Assert.That((r.health, r.armorLevel), Is.EqualTo((5, 1)), "the Rifleman is unchanged");
            Assert.That(u.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Solo", "Officer" }));
            Assert.That(u.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Pistol" }));
            Assert.That(u.abilities, Is.Empty);
            Assert.That(u.prefab, Is.Not.Null);
            Assert.That(u.prefab.transform.Find("Model/Pistol"), Is.Not.Null, "holding a pistol");
            Assert.That(u.prefab.transform.Find("Model/Rifle"), Is.Null, "not a rifle");
        }

        [Test]
        public void ADeployedRaskUltimaCarriesAPistol()
        {
            var unit = ArmyDeployment.Spawn(Ultima, Side.AI, Vector3.zero, Quaternion.identity, "Rask Ultima", null, Rask);
            try
            {
                Assert.That(unit.Soldiers.Count, Is.EqualTo(1));
                var s = unit.Leader;
                Assert.That((s.MaxHealth, s.ArmorLevel), Is.EqualTo((10, 2)));
                Assert.That(s.PrimaryWeapon.Name, Is.EqualTo("Pistol"), "the Rask don't rename the Pistol");
                Assert.That(s.HasKeyword("Officer") && s.HasKeyword("Solo"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
