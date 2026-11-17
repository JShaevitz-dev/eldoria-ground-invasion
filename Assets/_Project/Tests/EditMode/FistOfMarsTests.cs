using System.Collections.Generic;
using System.Linq;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;

namespace Eldoria.Tests
{
    /// <summary>
    /// The ISF's Fist of Mars (Specialist): a Sagittarius MBT with Accuracy 125% and the Unique keyword (only one per army),
    /// with a red Symbol of Mars on each side of its turret. Master Sergeant Jakoby is Unique too.
    /// </summary>
    public class FistOfMarsTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.ElementAt(0);
        private static UnitTemplate UnitNamed(string name) => Catalog.Available.Single(u => u.displayName == name);
        private static UnitTemplate Fist => UnitNamed("Fist of Mars");

        [Test]
        public void TheFistOfMarsIsAnIsfSpecialistOfOne()
        {
            var unit = Fist;
            Assert.That(Isf.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Skip(1).Any(f => f.Has(unit)), Is.False, "only the ISF's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.leader.displayName, Is.EqualTo("Fist of Mars"));
            Assert.That(unit.description, Does.Contain("Symbol of Mars").And.Contain("Unique").And.Contain("Specialist"));
        }

        [Test]
        public void ItHasTheSagittariusStatsWith125PercentAccuracyAndUnique()
        {
            var f = Fist.leader;
            var mbt = UnitNamed("Sagittarius MBT").leader;
            Assert.That((f.health, f.armorLevel, f.movementRange, f.radius, f.height), Is.EqualTo((mbt.health, mbt.armorLevel, mbt.movementRange, mbt.radius, mbt.height)));
            Assert.That((f.health, f.armorLevel, f.movementRange), Is.EqualTo((20, 4, 30f)));
            Assert.That(f.accuracy, Is.EqualTo(125));
            Assert.That(mbt.accuracy, Is.EqualTo(75), "the Sagittarius is unchanged");
            Assert.That(f.keywords.Select(k => k.displayName), Is.EqualTo(mbt.keywords.Select(k => k.displayName).Concat(new[] { "Unique" })));
            Assert.That(f.weapons, Is.EqualTo(mbt.weapons));
            Assert.That(f.abilities, Is.EqualTo(mbt.abilities), "the 50 m Frag Grenade every turn");
            Assert.That(mbt.keywords.Any(k => k.displayName == "Unique"), Is.False);
        }

        [Test]
        public void ItsModelIsTheSagittariusWithASymbolOfMarsOnEachSideOfTheTurret()
        {
            var prefab = Fist.leader.prefab;
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab, Is.Not.SameAs(UnitNamed("Sagittarius MBT").leader.prefab));
            var rings = prefab.GetComponentsInChildren<UnityEngine.Transform>().Where(t => t.name == "Mars Ring").ToList();
            Assert.That(rings.Count, Is.EqualTo(2));
            Assert.That(rings.Select(r => System.Math.Sign(r.position.x)), Is.EquivalentTo(new[] { -1, 1 }), "left and right");
            Assert.That(prefab.GetComponentsInChildren<UnityEngine.Transform>().Count(t => t.name == "Mars Arrow"), Is.EqualTo(2));
        }

        [Test]
        public void UniqueIsATraitKeyword()
        {
            var unique = KeywordLibrary.Get("Unique");
            Assert.That(unique, Is.Not.Null);
            Assert.That(unique.appliesTo, Is.EqualTo(KeywordTarget.Soldier));
            Assert.That(unique.category, Is.EqualTo(KeywordCategory.Trait));
            Assert.That(unique.description, Is.EqualTo("Only one of this unit may appear in an army list."));
        }

        [Test]
        public void OnlyOneFistOfMarsPerArmy()
        {
            var fist = Fist;
            var army = new List<UnitTemplate> { UnitNamed("Field Commander") };
            Assert.That(ArmyRules.IsUnique(fist), Is.True);
            Assert.That(ArmyRules.WhyCannotAdd(army, fist, Isf), Is.Null);
            army.Add(fist);
            Assert.That(ArmyRules.WhyCannotAdd(army, fist, Isf), Is.EqualTo("Already in the army (Unique: only one per army)"));
            Assert.That(ArmyRules.WhyCannotAdd(army, UnitNamed("Sagittarius MBT"), Isf), Is.Null, "plain Sagittariuses can still be added");
            army.Add(fist);
            Assert.That(ArmyRules.Problems(army, Isf), Does.Contain("Only one Fist of Mars allowed (Unique)."));
        }

        [Test]
        public void JakobyIsUniqueToo()
        {
            var jakoby = UnitNamed("Master Sergeant Jakoby");
            Assert.That(jakoby.leader.keywords.Any(k => k.displayName == "Unique"), Is.True);
            Assert.That(ArmyRules.HasUniqueKeyword(jakoby), Is.True);
            Assert.That(ArmyRules.HasUniqueKeyword(UnitNamed("Field Commander")), Is.False, "a Commander, unique by role only");
            Assert.That(ArmyRules.IsUnique(UnitNamed("ISF Rifle Squad")), Is.False);
        }

        [Test]
        public void RandomArmiesNeverTakeTwo()
        {
            var pool = Catalog.UnitsFor(Isf).ToList();
            for (int seed = 0; seed < 60; seed++)
            {
                var army = ArmyRules.RandomArmy(pool, new System.Random(seed));
                Assert.That(army.Count(u => u == Fist), Is.LessThanOrEqualTo(1), $"seed {seed}");
                Assert.That(ArmyRules.IsValid(army, Isf), Is.True, $"seed {seed}");
            }
        }
    }
}
