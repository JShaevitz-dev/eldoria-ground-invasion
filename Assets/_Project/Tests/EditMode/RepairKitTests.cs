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
    /// The Repair Kit equipment gives the active ability Repair Kit: choose one friendly Mechanical soldier within 5 m and
    /// they restore 10 health. A soldier at full health is refused (so the action isn't wasted).
    /// </summary>
    public class RepairKitTests
    {
        private static Equipment Item => AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/RepairKit.asset");
        private static Ability Repair => AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/RepairKit.asset");
        private static UnitTemplate Unit(string name) => UnitCatalog.Load().Available.Single(u => u.displayName == name);

        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private Soldier Spawn(string name, Side side, Vector3 at)
        {
            var unit = ArmyDeployment.Spawn(Unit(name), side, at, Quaternion.identity, name, null, null);
            spawned.Add(unit.gameObject);
            unit.Leader.transform.position = at;
            return unit.Leader;
        }

        /// <summary>An ISF Rifle Squad's leader, given a Repair Kit, standing at the origin.</summary>
        private Soldier Mechanic()
        {
            var mechanic = Spawn("ISF Rifle Squad", Side.Player, Vector3.zero);
            var loadout = new SoldierLoadout();
            loadout.Equipment.Add(Item);
            mechanic.ApplyLoadout(loadout);
            mechanic.transform.position = Vector3.zero;
            return mechanic;
        }

        [Test]
        public void TheRepairKitIsEquipmentThatGivesTheRepairKitAbility()
        {
            Assert.That(Item, Is.Not.Null);
            Assert.That(Item.displayName, Is.EqualTo("Repair Kit"));
            Assert.That(Item.abilities, Is.EqualTo(new[] { Repair }));
            Assert.That((Item.health, Item.armorLevel, Item.accuracy, Item.movement), Is.EqualTo((0, 0, 0, 0f)));
            var a = Repair;
            Assert.That(a.kind, Is.EqualTo(AbilityKind.Active));
            Assert.That(a.targets, Is.EqualTo(AbilityTarget.Soldier), "one soldier");
            Assert.That((a.range, a.heal), Is.EqualTo((5f, 10)));
            Assert.That(a.targetMustHave.displayName, Is.EqualTo("Mechanical"));
            Assert.That(a.affects, Is.EqualTo(AuraAffects.Allies));
            Assert.That(a.IsSpell, Is.False);
            Assert.That(ActiveAbilities.IsHealOnly(a), Is.True);
        }

        [Test]
        public void ItRestores10HealthToADamagedMachineWithin5m()
        {
            var mechanic = Mechanic();
            Assert.That(mechanic.Abilities.Select(a => a.displayName), Does.Contain("Repair Kit"));
            // The Sagittarius MBT (radius 1.8 m) parked beside the mechanic.
            var tank = Spawn("Sagittarius MBT", Side.Player, new Vector3(4f, 0f, 0f));
            tank.TakeDamage(15);
            Assert.That(tank.Health, Is.EqualTo(5));
            var r = ActiveAbilities.Use(mechanic, Repair, tank);
            Assert.That(r.Used, Is.True, r.Reason);
            Assert.That(tank.Health, Is.EqualTo(15), "10 health back");
            Assert.That(ActiveAbilities.Use(mechanic, Repair, tank).Used, Is.False, "once a turn");
        }

        [Test]
        public void ItCantRepairBeyond5mAFullHealthMachineASoldierOrAnEnemy()
        {
            var mechanic = Mechanic();
            var far = Spawn("Intimidator Medium Robot", Side.Player, new Vector3(0f, 0f, 9f));
            far.TakeDamage(3);
            Assert.That(ActiveAbilities.WhyCannotUse(mechanic, Repair, far), Does.Contain("Out of range"));

            var intact = Spawn("Intimidator Medium Robot", Side.Player, new Vector3(3f, 0f, 0f));
            Assert.That(ActiveAbilities.WhyCannotUse(mechanic, Repair, intact), Does.Contain("already at full health"));
            Assert.That(ActiveAbilities.Use(mechanic, Repair, intact).Used, Is.False, "refused, so the action isn't spent");

            var person = Spawn("ISF Rifle Squad", Side.Player, new Vector3(-2f, 0f, 0f));
            person.TakeDamage(2);
            Assert.That(ActiveAbilities.WhyCannotUse(mechanic, Repair, person), Does.Contain("Mechanical"), "people need a Medkit");

            var enemy = Spawn("Intimidator Medium Robot", Side.AI, new Vector3(0f, 0f, -3f));
            enemy.TakeDamage(3);
            Assert.That(ActiveAbilities.WhyCannotUse(mechanic, Repair, enemy), Does.Contain("isn't a valid target"), "friendly machines only");
        }

        [Test]
        public void AMachineWithARepairKitCanRepairItself()
        {
            var robot = Spawn("Intimidator Medium Robot", Side.Player, Vector3.zero);
            var loadout = new SoldierLoadout();
            loadout.Equipment.Add(Item);
            robot.ApplyLoadout(loadout);
            robot.TakeDamage(6);
            Assert.That(ActiveAbilities.Use(robot, Repair, robot).Used, Is.True);
            Assert.That(robot.Health, Is.EqualTo(robot.MaxHealth), "4 health back of 10, up to its maximum");
        }
    }
}
