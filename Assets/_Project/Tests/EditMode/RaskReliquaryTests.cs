using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Rask Reliquary (Specialist): a shrine on tracks with no weapon, whose Sanctifying Presence gives other friendly
    /// Biological soldiers within 20 m +2 damage and +20% accuracy. Its Supply Cost is the formula's + 100.
    /// </summary>
    public class RaskReliquaryTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Rask => Catalog.Factions.ElementAt(2);
        private static UnitTemplate Unit(string name) => Catalog.Available.Single(u => u.displayName == name);
        private static UnitTemplate Reliquary => Unit("Rask Reliquary");
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
            NearbyKeywords.RefreshAll();
        }

        private Unit Spawn(string name, Side side, Vector3 at, Faction faction)
        {
            var unit = ArmyDeployment.Spawn(Unit(name), side, at, Quaternion.identity, name, null, faction);
            spawned.Add(unit.gameObject);
            var shift = at - unit.Leader.Position;
            foreach (var s in unit.Soldiers) s.transform.position += shift;
            return unit;
        }

        [Test]
        public void TheReliquaryIsARaskSpecialistShrineWithNoWeapon()
        {
            var unit = Reliquary;
            Assert.That(Rask.Has(unit), Is.True);
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(1));
            var s = unit.leader;
            Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((10, 2, 75, 20f)));
            Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Mechanical" }));
            Assert.That(s.weapons, Is.Empty, "no weapon of its own");
            Assert.That(s.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Sanctifying Presence" }));
            Assert.That(s.prefab, Is.Not.Null);
            Assert.That(s.prefab.transform.Find("Model/Relic"), Is.Not.Null);
            Assert.That(s.prefab.GetComponentsInChildren<Transform>().Count(t => t.name == "Track"), Is.EqualTo(2), "on tracks");
        }

        [Test]
        public void SanctifyingPresenceIsA20mAuraOfPlus2DamageAndPlus20Accuracy()
        {
            var aura = Reliquary.leader.abilities.Single();
            Assert.That((aura.kind, aura.IsAura, aura.radius, aura.affects, aura.includeSelf),
                Is.EqualTo((AbilityKind.Passive, true, 20f, AuraAffects.Allies, false)));
            Assert.That(aura.auraMustHave.displayName, Is.EqualTo("Biological"));
            var sanctified = aura.grantsNearby;
            Assert.That((sanctified.displayName, sanctified.category), Is.EqualTo(("Sanctified", KeywordCategory.Status)));
            Assert.That(sanctified.interactions.Select(i => (i.role, i.effect, i.amount)), Is.EquivalentTo(new[]
            {
                (KeywordRole.WhenAttacking, KeywordEffect.DamageBonus, 2),
                (KeywordRole.WhenAttacking, KeywordEffect.AccuracyBonus, 20),
            }));
        }

        [Test]
        public void FriendlySoldiersWithin20mHitHarderAndMoreOften()
        {
            var shrine = Spawn("Rask Reliquary", Side.AI, Vector3.zero, Rask).Leader;
            var near = Spawn("Rask Ultima", Side.AI, new Vector3(18f, 0f, 0f), Rask).Leader;
            var far = Spawn("Rask Blackhand", Side.AI, new Vector3(0f, 0f, 30f), Rask).Leader;
            var enemy = Spawn("Field Commander", Side.Player, new Vector3(-15f, 0f, 0f), Catalog.Factions.First()).Leader;
            var robot = Spawn("Intimidator Medium Robot", Side.AI, new Vector3(0f, 0f, -12f), Rask).Leader;
            NearbyKeywords.RefreshAll();

            Assert.That(robot.HasKeyword("Mechanical"), Is.True);
            Assert.That(robot.HasKeyword("Sanctified"), Is.False, "Biological soldiers only: not the robots");

            Assert.That(near.HasKeyword("Sanctified"), Is.True, "18 m away");
            Assert.That(far.HasKeyword("Sanctified"), Is.False, "30 m away");
            Assert.That(enemy.HasKeyword("Sanctified"), Is.False, "enemies aren't blessed");
            Assert.That(shrine.HasKeyword("Sanctified"), Is.False, "not the Reliquary itself");

            var outcome = KeywordRules.Evaluate(near, near.WeaponFor(enemy), enemy);
            Assert.That(outcome.DamageBonus, Is.EqualTo(2));
            Assert.That(outcome.AccuracyModifiers.Where(m => m.Type == AccuracyModifierType.Bonus).Sum(m => m.Value), Is.EqualTo(20));
            Assert.That(KeywordRules.Evaluate(far, far.WeaponFor(enemy), enemy).DamageBonus, Is.EqualTo(0));
        }

        [Test]
        public void ItsSupplyCostIsTheFormulaPlus100()
        {
            // No weapon: no firepower. 10 health × 1.5 (Armor 2) = 15, at half weight; Sanctifying Presence (passive) 10.
            // The formula gives 18; the Reliquary adds 100 for its aura, which the formula doesn't count.
            var p = SupplyCost.Breakdown(Reliquary, Rask);
            Assert.That((p.Firepower, p.Durability, p.PassiveAbilities, p.ArmorTax, p.Total), Is.EqualTo((0.0, 15.0, 1, 0, 18)));
            Assert.That((Reliquary.supplyCostAdjustment, Reliquary.supplyCost), Is.EqualTo((100, 118)));
        }
    }
}
