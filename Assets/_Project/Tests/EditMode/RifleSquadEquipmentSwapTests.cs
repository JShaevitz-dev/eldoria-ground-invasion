using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Network;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// Equipment swaps: the ISF Rifle Squad's soldiers carry a Frag Grenade as standard equipment, and the army builder lets
    /// each soldier type swap it for a Medkit or a Repair Kit.
    /// </summary>
    public class RifleSquadEquipmentSwapTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.First();
        private static UnitTemplate Squad => Catalog.Available.Single(u => u.displayName == "ISF Rifle Squad");
        private static Equipment Item(string file) => AssetDatabase.LoadAssetAtPath<Equipment>($"Assets/_Project/Data/Equipment/{file}.asset");
        private static Ability Ability(string file) => AssetDatabase.LoadAssetAtPath<Ability>($"Assets/_Project/Data/Abilities/{file}.asset");

        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private ArmyBuilderScreen BuilderWithSquad()
        {
            var builder = new ArmyBuilderScreen(Catalog);
            builder.ChooseFaction(Isf);
            builder.Clear();
            builder.Add(Catalog.Available.Single(u => u.displayName == "Field Commander"));
            Assert.That(builder.Add(Squad), Is.Null);
            return builder;
        }

        [Test]
        public void EveryRiflemanCarriesAFragGrenadeThatCanBeSwappedForAMedkitOrRepairKit()
        {
            foreach (var s in new[] { Squad.leader, Squad.members.Single().soldier })
            {
                Assert.That(s.equipment, Is.EqualTo(new[] { Item("FragGrenade") }), s.displayName);
                var option = s.equipmentSwaps.Single();
                Assert.That(option.equipment, Is.SameAs(Item("FragGrenade")));
                Assert.That(option.alternatives, Is.EqualTo(new[] { Item("Medkit"), Item("RepairKit") }));
                Assert.That(s.HasOptions, Is.True, s.displayName);
            }
            Assert.That(UnitInfo.AbilityLines(Squad), Is.EqualTo(new[] { "Frag Grenade (active)" }), "no limit per battle");
        }

        [Test]
        public void TheArmyBuilderSwapsItForTheWholeSquadAtOnce()
        {
            var builder = BuilderWithSquad();
            var rifleman = Squad.members.Single().soldier;
            var sergeant = Squad.leader;
            Assert.That(Squad.equipmentSwapsForWholeUnit, Is.True);
            Assert.That(builder.CanCustomize(1), Is.True);
            Assert.That(builder.CustomizableSoldiers(1), Is.Empty, "no choices per soldier type");
            Assert.That(builder.UnitEquipmentSwaps(1).Select(o => o.equipment), Is.EqualTo(new[] { Item("FragGrenade") }), "one choice for the squad");

            Assert.That(builder.SetUnitEquipmentSwap(1, Item("FragGrenade"), Item("Medkit")), Is.Null);
            Assert.That(builder.ChosenEquipment(1, Item("FragGrenade")), Is.SameAs(Item("Medkit")));
            Assert.That(builder.Loadouts[1].Get(rifleman).SwapEquipment(Item("FragGrenade")), Is.SameAs(Item("Medkit")));
            Assert.That(builder.Loadouts[1].Get(sergeant).SwapEquipment(Item("FragGrenade")), Is.SameAs(Item("Medkit")), "the Sergeant too");

            // Choosing for one soldier type changes the whole squad as well.
            Assert.That(builder.SetEquipmentSwap(1, sergeant, Item("FragGrenade"), Item("RepairKit")), Is.Null);
            Assert.That(builder.Loadouts[1].Get(rifleman).SwapEquipment(Item("FragGrenade")), Is.SameAs(Item("RepairKit")));

            Assert.That(builder.SetUnitEquipmentSwap(1, Item("FragGrenade"), Item("CombatShield")), Does.Contain("can't be swapped for Combat Shield"), "only the listed choices");
            Assert.That(builder.SetUnitEquipmentSwap(1, Item("Medkit"), Item("RepairKit")), Does.Contain("can't be swapped"), "only standard equipment");
            Assert.That(builder.SetUnitEquipmentSwap(1, Item("FragGrenade"), Item("FragGrenade")), Is.Null, "choosing the grenade undoes the swap");
            Assert.That(builder.Loadouts[1].Get(rifleman), Is.Null);
            Assert.That(builder.Loadouts[1].Get(sergeant), Is.Null);
            Assert.That(builder.Confirm(), Is.True);
        }

        [Test]
        public void TheMortarTeamStillChoosesForItsRifleman()
        {
            var builder = BuilderWithSquad();
            var team = Catalog.Available.Single(u => u.displayName == "Mortar Team");
            Assert.That(builder.Add(team), Is.Null);
            var rifleman = Squad.members.Single().soldier;
            Assert.That(team.equipmentSwapsForWholeUnit, Is.False);
            Assert.That(builder.CustomizableSoldiers(2), Is.EqualTo(new[] { rifleman }));
            Assert.That(builder.UnitEquipmentSwaps(2), Is.Empty);
        }

        [Test]
        public void ADeployedSquadCarriesWhatWasChosen()
        {
            // (The army builder swaps the whole squad; a loadout can still give one soldier type its own choice.)
            var rifleman = Squad.members.Single().soldier;
            var loadout = new UnitLoadout();
            loadout.For(rifleman).EquipmentSwaps[Item("FragGrenade")] = Item("Medkit");
            var unit = ArmyDeployment.Spawn(Squad, Side.Player, Vector3.zero, Quaternion.identity, "Rifles", null, Isf, loadout);
            spawned.Add(unit.gameObject);

            var riflemen = unit.Soldiers.Where(s => s.Template == rifleman).ToList();
            Assert.That(riflemen.Count, Is.EqualTo(9));
            Assert.That(riflemen.All(s => s.Abilities.Contains(Ability("Medkit")) && !s.Abilities.Contains(Ability("FragGrenade"))), Is.True,
                "every Rifleman has a Medkit instead of a grenade");
            Assert.That(unit.Leader.Abilities, Is.EqualTo(new[] { Ability("FragGrenade") }), "the Sergeant kept his grenade");
            Assert.That(unit.UsesLeftThisBattle(Ability("FragGrenade")), Is.EqualTo(int.MaxValue), "the Sergeant's grenade, with no limit per battle");
        }

        [Test]
        public void WithRepairKitsTheyCanRepairATank()
        {
            var rifleman = Squad.members.Single().soldier;
            var loadout = new UnitLoadout();
            loadout.For(rifleman).EquipmentSwaps[Item("FragGrenade")] = Item("RepairKit");
            var unit = ArmyDeployment.Spawn(Squad, Side.Player, Vector3.zero, Quaternion.identity, "Rifles", null, Isf, loadout);
            spawned.Add(unit.gameObject);
            var mechanic = unit.Soldiers.First(s => s.Template == rifleman);
            mechanic.transform.position = Vector3.zero;
            var tank = ArmyDeployment.Spawn(Catalog.Available.Single(u => u.displayName == "Sagittarius MBT"), Side.Player, new Vector3(4f, 0f, 0f), Quaternion.identity, "MBT", null, Isf);
            spawned.Add(tank.gameObject);
            tank.Leader.transform.position = new Vector3(4f, 0f, 0f);
            tank.Leader.TakeDamage(12);
            Assert.That(ActiveAbilities.Use(mechanic, Ability("RepairKit"), tank.Leader).Used, Is.True);
            Assert.That(tank.Leader.Health, Is.EqualTo(18));
        }

        [Test]
        public void EquipmentSwapsTravelToTheOtherPlayer()
        {
            var rifleman = Squad.members.Single().soldier;
            var army = new List<UnitTemplate> { Catalog.Available.Single(u => u.displayName == "Field Commander"), Squad };
            var loadouts = new List<UnitLoadout> { new UnitLoadout(), new UnitLoadout() };
            loadouts[1].For(rifleman).EquipmentSwaps[Item("FragGrenade")] = Item("RepairKit");
            var msg = ArmyMsg.From(Isf, army, loadouts);
            var copy = JsonUtility.FromJson<ArmyMsg>(JsonUtility.ToJson(msg));
            Assert.That(copy.TryResolve(Catalog, out _, out _, out var resolved, out var error), Is.True, error);
            Assert.That(resolved[1].Get(rifleman).SwapEquipment(Item("FragGrenade")), Is.SameAs(Item("RepairKit")));
        }
    }
}
