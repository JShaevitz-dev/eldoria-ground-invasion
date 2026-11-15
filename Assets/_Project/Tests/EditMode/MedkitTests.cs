using System.Collections.Generic;
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
    /// The Medkit equipment gives the active ability Medkit: choose one friendly Biological soldier within 5 m and they
    /// restore 5 health. A soldier at full health is refused (so the action isn't wasted).
    /// </summary>
    public class MedkitTests
    {
        private static Equipment Item => AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/Medkit.asset");
        private static Ability Medkit => AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/Medkit.asset");
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static UnitTemplate Unit(string name) => Catalog.Available.Single(u => u.displayName == name);

        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private Unit Spawn(string name, Side side, Vector3 at)
        {
            var unit = ArmyDeployment.Spawn(Unit(name), side, at, Quaternion.identity, name, null, null);
            spawned.Add(unit.gameObject);
            return unit;
        }

        /// <summary>The ISF Rifle Squad's leader, given a Medkit, standing at the origin.</summary>
        private Soldier Medic(out Unit squad)
        {
            squad = Spawn("ISF Rifle Squad", Side.Player, Vector3.zero);
            var medic = squad.Leader;
            var loadout = new SoldierLoadout();
            loadout.Equipment.Add(Item);
            medic.ApplyLoadout(loadout);
            medic.transform.position = Vector3.zero;
            return medic;
        }

        [Test]
        public void TheMedkitIsEquipmentThatGivesTheMedkitAbility()
        {
            Assert.That(Item, Is.Not.Null);
            Assert.That(Item.displayName, Is.EqualTo("Medkit"));
            Assert.That(Item.abilities, Is.EqualTo(new[] { Medkit }));
            Assert.That((Item.health, Item.armorLevel, Item.accuracy, Item.movement), Is.EqualTo((0, 0, 0, 0f)));
            var a = Medkit;
            Assert.That(a.displayName, Is.EqualTo("Medkit"));
            Assert.That(a.kind, Is.EqualTo(AbilityKind.Active));
            Assert.That(a.targets, Is.EqualTo(AbilityTarget.Soldier), "one soldier");
            Assert.That((a.range, a.heal), Is.EqualTo((5f, 5)));
            Assert.That(a.targetMustHave.displayName, Is.EqualTo("Biological"));
            Assert.That(a.affects, Is.EqualTo(AuraAffects.Allies));
            Assert.That(a.IsSpell, Is.False, "a medkit isn't magic: it works after sprinting");
            Assert.That(ActiveAbilities.IsHealOnly(a), Is.True);
        }

        [Test]
        public void ItRestores5HealthToAWoundedSoldierWithin5m()
        {
            var medic = Medic(out var squad);
            Assert.That(medic.Abilities, Does.Contain(Medkit));
            var patient = squad.Soldiers.Last();
            patient.transform.position = new Vector3(4.5f, 0f, 0f);
            patient.TakeDamage(4);
            Assert.That(patient.Health, Is.EqualTo(1));
            var r = ActiveAbilities.Use(medic, Medkit, patient);
            Assert.That(r.Used, Is.True, r.Reason);
            Assert.That(patient.Health, Is.EqualTo(5), "5 health back, but not above the maximum");

        }

        [Test]
        public void ItCantReachBeyond5mOrHealAFullHealthNonBiologicalOrEnemySoldier()
        {
            var medic = Medic(out var squad);
            var far = squad.Soldiers.Last();
            far.transform.position = new Vector3(6f, 0f, 0f);
            far.TakeDamage(2);
            Assert.That(ActiveAbilities.WhyCannotUse(medic, Medkit, far), Does.Contain("Out of range"));

            var healthy = squad.Soldiers[1];
            healthy.transform.position = new Vector3(2f, 0f, 0f);
            Assert.That(ActiveAbilities.WhyCannotUse(medic, Medkit, healthy), Does.Contain("already at full health"));
            var refused = ActiveAbilities.Use(medic, Medkit, healthy);
            Assert.That(refused.Used, Is.False, "refused, so the action isn't spent");

            var robot = Spawn("Intimidator Medium Robot", Side.Player, new Vector3(0f, 0f, 3f)).Leader;
            robot.transform.position = new Vector3(0f, 0f, 3f);
            robot.TakeDamage(3);
            Assert.That(ActiveAbilities.WhyCannotUse(medic, Medkit, robot), Does.Contain("Biological"), "machines can't be patched up");

            var enemy = Spawn("ISF Rifle Squad", Side.AI, new Vector3(-3f, 0f, 0f)).Leader;
            enemy.transform.position = new Vector3(-3f, 0f, 0f);
            enemy.TakeDamage(3);
            Assert.That(ActiveAbilities.WhyCannotUse(medic, Medkit, enemy), Does.Contain("isn't a valid target"), "friendly soldiers only");

            medic.TakeDamage(2);
            Assert.That(ActiveAbilities.Use(medic, Medkit, medic).Used, Is.True, "the medic can treat themself");
        }
    }
}
