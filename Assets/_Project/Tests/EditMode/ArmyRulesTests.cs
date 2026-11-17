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
    /// Army lists: up to 6 units, at least 1 Commander, no duplicate Commander or Specialist units. The game has no units
    /// of its own yet, so these use test-made units and a test-made catalog with two factions.
    /// </summary>
    public class ArmyRulesTests
    {
        private static UnitTemplate Infantry => TestArmies.InfantrySquad;
        private static UnitTemplate Commander => TestArmies.FieldCommander;
        private static UnitTemplate Priest => TestArmies.BattlePriest;
        private static UnitTemplate Sniper => TestArmies.Sniper;

        private static UnitTemplate mg, pa, commanderPA;
        private static UnitTemplate MG => mg != null ? mg : mg = TestArmies.Unit("Machine Gun Team", UnitRole.HeavySupport,
            TestArmies.Soldier("Sergeant", 5, 1, 75, 10f, new[] { "Biological" }, null, new[] { TestArmies.Weapon("Pistol") }),
            TestArmies.Soldier("Machine Gunner", 5, 1, 75, 10f, new[] { "Biological" }, null, new[] { TestArmies.Weapon("MachineGun") }), 4);
        private static UnitTemplate PA => pa != null ? pa : pa = TestArmies.Unit("Powered Armor Squad", UnitRole.Specialist,
            TestArmies.Soldier("Sergeant in Powered Armor", 5, 2, 75, 6f, new[] { "Biological" }, null, new[] { TestArmies.Weapon("AssaultRifle") }),
            TestArmies.Soldier("Trooper in Powered Armor", 5, 2, 75, 6f, new[] { "Biological" }, null, new[] { TestArmies.Weapon("AssaultRifle") }), 4);
        private static UnitTemplate CommanderPA => commanderPA != null ? commanderPA : commanderPA = TestArmies.Unit("Field Commander in Powered Armor",
            UnitRole.Commander, TestArmies.Soldier("Field Commander", 10, 2, 75, 6f, new[] { "Biological", "Officer", "Solo" }, null, new[] { TestArmies.Weapon("Pistol") }));

        private static List<UnitTemplate> List(params UnitTemplate[] units) => units.ToList();

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        /// <summary>A test catalog: two factions, Blue (everything but the Powered Armor Squad) and Grey (everything but the Battle Priest).</summary>
        private UnitCatalog TestCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<UnitCatalog>();
            created.Add(catalog);
            catalog.units = List(Commander, CommanderPA, Infantry, MG, Priest, PA, Sniper).OrderBy(u => u.role.ListOrder()).ToList();
            var blue = TestArmies.Faction("Test Blue", new Color(0.2f, 0.3f, 0.7f), Commander, CommanderPA, Infantry, MG, Priest, Sniper);
            var grey = TestArmies.Faction("Test Grey", new Color(0.46f, 0.47f, 0.49f), Commander, CommanderPA, Infantry, MG, PA, Sniper);
            created.Add(blue);
            created.Add(grey);
            catalog.factions = new List<Faction> { blue, grey };
            catalog.defaultArmy = List(Infantry, Commander, Infantry, Priest, Infantry);
            catalog.primaryColorMaterial = UnitCatalog.Load().primaryColorMaterial;
            catalog.leaderMarker = UnitCatalog.Load().leaderMarker;
            return catalog;
        }

        [Test]
        public void TheGameHasItsThreeFactionsAndTheirUnitsSoFar()
        {
            var catalog = UnitCatalog.Load();
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Available.Select(u => u.displayName), Is.EqualTo(new[] { "Crown Princess X'tara", "Dragon Hierarch", "Dragonborn Exemplar", "Field Commander", "Marshal Command Tank", "Master Sergeant Jakoby", "Rask Ultima", "Dragonborn Squad", "ISF Rifle Squad", "Kobold Militia", "Rask Rifle Team", "Heavy Weapons Team", "Intimidator Medium Robot", "Mortar Team", "Olympian Mech", "Sagittarius MBT", "Stinger Heavy Robot", "Tyrant Hover-tank", "Beasthunter Cavalry", "Dragonborn Skymaster Squad", "Fist of Mars", "ISF Boarding Team", "ISF Combat Engineer", "ISF Combat Medic", "ISF Ranger", "Kobold Heartstrike Spear Team", "Kobold Mechanist", "Rask Blackhand", "Rask Redhand", "Rask Reliquary", "Workhorse Transport" }), "Commanders, Infantry, Heavy Support, then Specialists");
            Assert.That(catalog.Factions.Select(f => f.displayName), Is.EqualTo(new[] { "International Space Force of Humanity", "Imperial Army of The Draconis Imperialis", "Most Holy Army of The Rask Divine Collective" }));
            var factions = catalog.Factions.ToList();
            Assert.That(factions[0].Units.Select(u => u.displayName), Is.EquivalentTo(new[] { "Field Commander", "Master Sergeant Jakoby", "Marshal Command Tank", "ISF Rifle Squad", "Heavy Weapons Team", "Mortar Team", "Sagittarius MBT", "Olympian Mech", "ISF Ranger", "ISF Boarding Team", "ISF Combat Engineer", "ISF Combat Medic", "Workhorse Transport", "Fist of Mars" }));
            Assert.That(factions[1].Units.Select(u => u.displayName), Is.EquivalentTo(new[] { "Dragon Hierarch", "Dragonborn Squad", "Kobold Militia", "Dragonborn Skymaster Squad", "Tyrant Hover-tank", "Kobold Mechanist", "Dragonborn Exemplar", "Kobold Heartstrike Spear Team", "Crown Princess X'tara" }), "Draconis Imperialis");
            Assert.That(factions[2].Units.Select(u => u.displayName), Is.EquivalentTo(new[] { "Rask Ultima", "Rask Rifle Team", "Stinger Heavy Robot", "Intimidator Medium Robot", "Rask Blackhand", "Rask Redhand", "Rask Reliquary", "Beasthunter Cavalry" }), "Rask");
            Assert.That(catalog.defaultArmy, Is.Empty);
            Assert.That(catalog.primaryColorMaterial, Is.Not.Null, "still there for deploying units");
            Assert.That(catalog.leaderMarker, Is.Not.Null);
            Assert.That(AssetDatabase.FindAssets("t:UnitTemplate", new[] { "Assets/_Project/Data/Units" }).Length, Is.EqualTo(31));
            Assert.That(AssetDatabase.FindAssets("t:SoldierTemplate", new[] { "Assets/_Project/Data/Soldiers" }).Length, Is.EqualTo(43));
            Assert.That(AssetDatabase.FindAssets("t:Faction", new[] { "Assets/_Project/Data/Factions" }).Length, Is.EqualTo(3));
        }

        [Test]
        public void EachFactionHasItsOwnFlag()
        {
            var factions = UnitCatalog.Load().Factions.ToList();
            foreach (var faction in factions)
            {
                Assert.That(faction.flag, Is.Not.Null, faction.displayName);
                Assert.That(faction.flag.name, Is.EqualTo(faction.name), "Textures/Flags/<faction asset>.png");
                Assert.That(faction.flag.width, Is.EqualTo(600), $"{faction.displayName}: kept at its own size");
                Assert.That(faction.flag.height, Is.EqualTo(400), $"{faction.displayName}: 3:2");
            }
            Assert.That(factions.Select(f => f.flag).Distinct().Count(), Is.EqualTo(factions.Count));
        }

        [Test]
        public void EachFactionHasItsOwnColour()
        {
            var factions = UnitCatalog.Load().Factions.ToList();
            Assert.That(ColorUtility.ToHtmlStringRGB(factions[0].color), Is.EqualTo("AAAAAA"), "Humanity: grey (#aaaaaa)");
            Assert.That(factions[1].color, Is.EqualTo(new Color(0.55f, 0.09f, 0.11f)), "Draconis Imperialis: imperial crimson");
            Assert.That(factions[2].color, Is.EqualTo(new Color(0.66f, 0.48f, 0.18f)), "Rask: ochre/bronze");
            Assert.That(factions.Select(f => f.color).Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public void EveryFactionCanFieldAnArmyAndTheirRandomArmiesAreValid()
        {
            var catalog = UnitCatalog.Load();
            var builder = new ArmyBuilderScreen(catalog);
            Assert.That(builder.CanFieldArmies, Is.True);
            Assert.That(builder.FactionStatus(builder.Factions[0]), Is.EqualTo("14 units available"));
            Assert.That(builder.FactionStatus(builder.Factions[1]), Is.EqualTo("9 units available"));
            Assert.That(builder.FactionStatus(builder.Factions[2]), Is.EqualTo("8 units available"));
            Assert.That(catalog.Factions.Select(f => ArmyBuilderScreen.FactionCanFieldArmy(catalog, f)), Is.EqualTo(new[] { true, true, true }));
            Assert.That(new ArmyBuilderScreen(null).CanFieldArmies, Is.False);
            var isf = catalog.Factions.First();
            for (int seed = 0; seed < 50; seed++)
            {
                var army = ArmyRules.RandomArmy(catalog.UnitsFor(isf), new System.Random(seed));
                Assert.That(ArmyRules.Problems(army, isf), Is.Empty, $"seed {seed}");
                Assert.That(ArmyRules.SupplyOf(army), Is.LessThanOrEqualTo(ArmyRules.SupplyCap));
                Assert.That(catalog.UnitsFor(isf).Where(u => u.role != UnitRole.Commander).All(u => ArmyRules.WhyCannotAdd(army, u, isf) != null), Is.True, "nothing more fits");
                Assert.That(new[] { "Field Commander", "Master Sergeant Jakoby", "Marshal Command Tank" }, Does.Contain(army.First().displayName));
                Assert.That(army.Count(u => u.role == UnitRole.Commander), Is.EqualTo(1));
            }
            var draconis = catalog.Factions.ElementAt(1);
            for (int seed = 0; seed < 20; seed++)
            {
                var army = ArmyRules.RandomArmy(catalog.UnitsFor(draconis), new System.Random(seed));
                Assert.That(ArmyRules.Problems(army, draconis), Is.Empty, $"Draconis seed {seed}");
                Assert.That(ArmyRules.SupplyOf(army), Is.LessThanOrEqualTo(ArmyRules.SupplyCap));
                Assert.That(catalog.UnitsFor(draconis).Where(u => u.role != UnitRole.Commander).All(u => ArmyRules.WhyCannotAdd(army, u, draconis) != null), Is.True, "nothing more fits");
                Assert.That(new[] { "Dragon Hierarch", "Dragonborn Exemplar", "Crown Princess X'tara" }, Does.Contain(army.First().displayName));
                Assert.That(army.Count(u => u.role == UnitRole.Commander), Is.EqualTo(1));
                Assert.That(army.Skip(1).All(u => u.displayName == "Dragonborn Squad" || u.displayName == "Kobold Militia" || u.displayName == "Dragonborn Skymaster Squad" || u.displayName == "Tyrant Hover-tank" || u.displayName == "Kobold Mechanist" || u.displayName == "Kobold Heartstrike Spear Team"), Is.True,
                    "then Infantry (Dragonborn Squads and Kobold Militia), the Specialist Skymaster Squad and Heavy Support");
            }
            var rask = catalog.Factions.Last();
            for (int seed = 0; seed < 20; seed++)
            {
                var army = ArmyRules.RandomArmy(catalog.UnitsFor(rask), new System.Random(seed));
                Assert.That(ArmyRules.Problems(army, rask), Is.Empty, $"Rask seed {seed}");
                Assert.That(ArmyRules.SupplyOf(army), Is.LessThanOrEqualTo(ArmyRules.SupplyCap));
                Assert.That(catalog.UnitsFor(rask).Where(u => u.role != UnitRole.Commander).All(u => ArmyRules.WhyCannotAdd(army, u, rask) != null), Is.True, "nothing more fits");
                Assert.That(army.First().displayName, Is.EqualTo("Rask Ultima"));
                Assert.That(army.Count(u => u.role == UnitRole.Commander), Is.EqualTo(1));
            }
        }

        [Test]
        public void WithNoCommandersAnywhereTheBuilderSaysSo()
        {
            var catalog = TestCatalog();
            catalog.units = List(Infantry, MG);
            var noCommander = TestArmies.Faction("Test No Commander", Color.white, Infantry, MG);
            created.Add(noCommander);
            catalog.factions = new System.Collections.Generic.List<Faction> { noCommander };
            var builder = new ArmyBuilderScreen(catalog);
            Assert.That(builder.CanFieldArmies, Is.False);
            Assert.That(builder.FactionStatus(noCommander), Is.EqualTo("2 units \u00b7 no Commander yet"));
            Assert.That(ArmyBuilderScreen.NoArmiesMessage, Does.Contain("No faction has a Commander yet"));
        }

        // ---- The ISF's Field Commander (the original Eldoria's, with Armor 2) ----

        [Test]
        public void TheFieldCommanderIsTheIsfsCommanderWithArmor2()
        {
            var catalog = UnitCatalog.Load();
            var unit = catalog.Available.Single(u => u.displayName == "Field Commander");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Commander));
            Assert.That(catalog.Factions.First().Has(unit), Is.True, "the ISF's");
            Assert.That(unit.Size, Is.EqualTo(1));
            var c = unit.leader;
            Assert.That(c.displayName, Is.EqualTo("Field Commander"));
            Assert.That((c.health, c.armorLevel, c.accuracy, c.movementRange), Is.EqualTo((10, 2, 75, 10f)));
            Assert.That(c.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Officer", "Solo" }));
            Assert.That(c.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Lead from the front" }));
            Assert.That(c.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Pistol" }));
            Assert.That(c.prefab, Is.Not.Null);
            Assert.That(unit.description, Does.Contain("Lead from the front"));
            Assert.That(UnitInfo.SoldierLines(unit).Single(),
                Is.EqualTo("Field Commander \u2014 Health 10 \u00b7 Armor 2 \u00b7 Accuracy 75% \u00b7 Move 10 m (sprint 20 m) \u00b7 Pistol"));
        }

        [Test]
        public void OnlyAFactionWithACommanderCanFieldAnArmy()
        {
            var catalog = TestCatalog();
            var noCommander = TestArmies.Faction("Test No Commander", Color.white, Infantry, Priest);
            created.Add(noCommander);
            Assert.That(ArmyBuilderScreen.FactionCanFieldArmy(catalog, catalog.Factions.First()), Is.True);
            Assert.That(ArmyBuilderScreen.FactionCanFieldArmy(catalog, noCommander), Is.False);
            Assert.That(new ArmyBuilderScreen(catalog).FactionStatus(catalog.Factions.First()), Is.EqualTo("6 units available"));
        }

        // ---- The ISF's Heavy Weapons Team (the original Eldoria's Machine Gun Team) ----

        [Test]
        public void TheHeavyWeaponsTeamIsAnIsfSergeantThreeRiflemenAndAHeavyWeaponsSoldier()
        {
            var team = UnitCatalog.Load().Available.Single(u => u.displayName == "Heavy Weapons Team");
            Assert.That(team.role, Is.EqualTo(UnitRole.HeavySupport));
            Assert.That(UnitCatalog.Load().Factions.First().Has(team), Is.True, "the ISF's");
            Assert.That(ArmyRules.Composition(team), Is.EqualTo("ISF Rifleman Sergeant + 3\u00d7 ISF Rifleman + ISF Heavy Weapons"));
            Assert.That(team.Size, Is.EqualTo(5));
            Assert.That(team.description, Does.Contain("ISF Heavy Weapons").And.Contain("Rocket Launcher").And.Contain("Flamethrower").And.Contain("Assault Rifles").And.Not.Contain("SMG"));
            Assert.That(UnitInfo.SoldierLines(team), Is.EqualTo(new[]
            {
                "ISF Rifleman Sergeant \u2014 Health 5 \u00b7 Armor 2 \u00b7 Accuracy 75% \u00b7 Move 10 m (sprint 20 m) \u00b7 Pistol",
                "3\u00d7 ISF Rifleman \u2014 Health 5 \u00b7 Armor 2 \u00b7 Accuracy 75% \u00b7 Move 10 m (sprint 20 m) \u00b7 Assault Rifle, Pistol",
                "ISF Heavy Weapons \u2014 Health 5 \u00b7 Armor 2 \u00b7 Accuracy 75% \u00b7 Move 10 m (sprint 20 m) \u00b7 Machine Gun",
            }));
            Assert.That(UnitInfo.WeaponLines(team).Last(), Is.EqualTo("Machine Gun \u2014 Damage 5 \u00b7 Shots 5 \u00b7 Penetration 1 \u00b7 Range 20 m \u00b7 Deployed"));
            foreach (var s in team.Roster().Distinct())
            {
                Assert.That(s.HasKeyword("Biological"), Is.True, s.displayName);
                Assert.That(s.abilities, Is.Empty, $"{s.displayName}: no Frag Grenade");
                Assert.That(s.prefab, Is.Not.Null, s.displayName);
            }
        }

        [Test]
        public void TheHeavyWeaponsSoldierCanSwapTheMachineGunForARocketLauncherOrAFlamethrower()
        {
            var catalog = UnitCatalog.Load();
            var isf = catalog.Factions.First();
            var team = catalog.Available.Single(u => u.displayName == "Heavy Weapons Team");
            var heavy = team.Roster().Single(s => s.displayName == "ISF Heavy Weapons");
            var option = heavy.weaponOptions.Single();
            Assert.That(option.weapon.displayName, Is.EqualTo("Machine Gun"));
            Assert.That(option.alternatives.Select(w => w.displayName), Is.EqualTo(new[] { "Rocket Launcher", "Flamethrower" }));
            var rockets = option.alternatives[0];
            var flamer = option.alternatives[1];
            Assert.That((rockets.baseDamage, rockets.shots, rockets.PenetrationValue, rockets.range), Is.EqualTo((5, 1, (int?)5, 50f)));
            Assert.That(rockets.HasKeyword("Manual Load"), Is.True, "once a turn");
            Assert.That((flamer.baseDamage, flamer.shots, flamer.PenetrationValue, flamer.range), Is.EqualTo((10, 3, (int?)null, 10f)));
            Assert.That(flamer.HasKeyword("Incendiary") && flamer.HasKeyword("Unstable"), Is.True);
            Assert.That(team.Roster().Where(s => s != heavy).All(s => !s.HasOptions), Is.True, "only the heavy weapons soldier has a choice");

            // In the army builder: only that soldier type can be customized, and the swap is kept per slot.
            var builder = new ArmyBuilderScreen(catalog);
            builder.ChooseFaction(isf);
            builder.Clear();
            Assert.That(builder.Add(team), Is.Null);
            Assert.That(builder.CustomizableSoldiers(0), Is.EqualTo(new[] { heavy }));
            Assert.That(builder.SetWeapon(0, heavy, option.weapon, rockets), Is.Null);
            Assert.That(builder.Loadouts[0].Describe(), Is.EqualTo("ISF Heavy Weapons: Rocket Launcher"));
            Assert.That(builder.Add(team), Is.Null, "a second team, with its own choice");
            Assert.That(builder.SetWeapon(1, heavy, option.weapon, flamer), Is.Null);
            Assert.That(builder.Loadouts[1].Describe(), Is.EqualTo("ISF Heavy Weapons: Flamethrower"));
            Assert.That(builder.SetWeapon(1, heavy, option.weapon, TestArmies.Weapon("SniperRifle")), Does.Contain("can't be swapped"), "only the listed weapons");

            // Deployed: the heavy weapons soldier carries the Rocket Launcher instead of the Machine Gun; the others are unchanged.
            var unit = UnitFactory.Spawn(team, Side.Player, Vector3.zero, Quaternion.identity);
            try
            {
                ArmyDeployment.ApplyLoadout(unit, builder.Loadouts[0]);
                var soldier = unit.Soldiers.Single(s => s.Template == heavy);
                Assert.That(soldier.Weapons.Select(w => w.Name), Is.EqualTo(new[] { "Rocket Launcher" }));
                Assert.That(soldier.PrimaryWeapon.Name, Is.EqualTo("Rocket Launcher"));
                Assert.That(unit.Soldiers.Where(s => s != soldier).All(s => s.Weapons.All(w => w.Name != "Rocket Launcher")), Is.True);
                var riflemen = unit.Soldiers.Where(s => s.Template.displayName == "ISF Rifleman").ToList();
                Assert.That(riflemen.Count, Is.EqualTo(3));
                Assert.That(riflemen.All(s => s.PrimaryWeapon.Name == "Assault Rifle"), Is.True, "Assault Rifles, not SMGs");
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }

            var flameUnit = UnitFactory.Spawn(team, Side.Player, Vector3.zero, Quaternion.identity);
            try
            {
                ArmyDeployment.ApplyLoadout(flameUnit, builder.Loadouts[1]);
                Assert.That(flameUnit.Soldiers.Single(s => s.Template == heavy).PrimaryWeapon.Name, Is.EqualTo("Flamethrower"));
            }
            finally
            {
                Object.DestroyImmediate(flameUnit.gameObject);
            }
        }

        [Test]
        public void HeavySupportCanBeTakenMoreThanOnceAlongsideRifleSquads()
        {
            var catalog = UnitCatalog.Load();
            var team = catalog.Available.Single(u => u.displayName == "Heavy Weapons Team");
            var squad = catalog.Available.Single(u => u.displayName == "ISF Rifle Squad");
            Assert.That(ArmyRules.WhyCannotAdd(List(squad, team), team, catalog.Factions.First()), Is.Null);
            Assert.That(ArmyRules.Problems(List(squad, team, team), catalog.Factions.First()).Single(), Does.Contain("Commander"),
                "only the missing Commander is wrong");
        }

        // ---- The ISF Rifle Squad ----

        [Test]
        public void TheIsfRifleSquadIsASergeantAndNineRiflemen()
        {
            var squad = UnitCatalog.Load().Available.Single(u => u.displayName == "ISF Rifle Squad");
            Assert.That(squad.role, Is.EqualTo(UnitRole.Infantry));
            Assert.That(ArmyRules.Composition(squad), Is.EqualTo("ISF Rifleman Sergeant + 9\u00d7 ISF Rifleman"));
            Assert.That(squad.Size, Is.EqualTo(10));
        }

        [Test]
        public void IsfRiflemenHaveArmor2()
        {
            var squad = UnitCatalog.Load().Available.Single(u => u.displayName == "ISF Rifle Squad");
            var sergeant = squad.leader;
            var rifleman = squad.members.Single().soldier;
            foreach (var s in new[] { sergeant, rifleman })
            {
                Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((5, 2, 75, 10f)), s.displayName);
                Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological" }), s.displayName);
                Assert.That(s.abilities, Is.Empty, $"{s.displayName}: the Frag Grenade is equipment now");
                Assert.That(s.equipment.Select(e => e.displayName), Is.EqualTo(new[] { "Frag Grenade" }), s.displayName);
                Assert.That(s.PrimaryWeapon.displayName, Is.EqualTo("Assault Rifle"), s.displayName);
                Assert.That(s.prefab, Is.Not.Null, s.displayName);
            }
            Assert.That(rifleman.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Assault Rifle" }));
            Assert.That(sergeant.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Assault Rifle", "Pistol" }));
            Assert.That(UnitInfo.SoldierLines(squad), Is.EqualTo(new[]
            {
                "ISF Rifleman Sergeant \u2014 Health 5 \u00b7 Armor 2 \u00b7 Accuracy 75% \u00b7 Move 10 m (sprint 20 m) \u00b7 Assault Rifle, Pistol, Frag Grenade",
                "9\u00d7 ISF Rifleman \u2014 Health 5 \u00b7 Armor 2 \u00b7 Accuracy 75% \u00b7 Move 10 m (sprint 20 m) \u00b7 Assault Rifle, Frag Grenade",
            }));
        }

        [Test]
        public void TheDemoArmyIsValid()
        {
            Assert.That(ArmyRules.Problems(TestArmies.DemoArmy), Is.Empty);
            Assert.That(ArmyRules.Problems(TestCatalog().defaultArmy), Is.Empty);
        }

        [Test]
        public void AnArmyNeedsAtLeastOneCommander()
        {
            Assert.That(ArmyRules.Problems(List()).Single(), Does.Contain("at least 1 Commander"));
            Assert.That(ArmyRules.Problems(List(Infantry, MG, Sniper)).Single(), Does.Contain("at least 1 Commander"));
            Assert.That(ArmyRules.IsValid(List(Commander)), Is.True, "a single Commander is a valid army");
            Assert.That(ArmyRules.Problems(List(Priest, Infantry)).Single(), Does.Contain("at least 1 Commander"),
                "the Battle Priest is a Specialist, not a Commander");
        }

        [Test]
        public void AnArmysSupplyCostsCantAddUpToMoreThanTheSupplyCap()
        {
            var catalog = UnitCatalog.Load();
            UnitTemplate Real(string name) => catalog.Available.Single(u => u.displayName == name);
            var ultima = Real("Rask Ultima");          // 12
            var rifles = Real("Rask Rifle Team");      // 220
            Assert.That(ArmyRules.SupplyCap, Is.EqualTo(750));
            var army = new List<UnitTemplate> { ultima, rifles, rifles, rifles };   // 672
            Assert.That(ArmyRules.SupplyOf(army), Is.EqualTo(ultima.supplyCost + 3 * rifles.supplyCost));
            Assert.That(ArmyRules.IsValid(army), Is.True, "under the cap");
            Assert.That(ArmyRules.WhyCannotAdd(army, rifles), Does.Contain("Over the Supply Cap"));
            Assert.That(ArmyRules.WhyCannotAdd(army, ultima), Does.Contain("Commander"), "and still one Commander");
            army.Add(rifles);
            Assert.That(ArmyRules.Problems(army).Single(), Does.Contain($"Over the Supply Cap: {ultima.supplyCost + 4 * rifles.supplyCost} of 750"));
        }

        [Test]
        public void ThereIsNoLimitOnHowManyUnits()
        {
            var catalog = UnitCatalog.Load();
            var commander = catalog.Available.Single(u => u.displayName == "Field Commander");
            var ranger = catalog.Available.Single(u => u.displayName == "ISF Ranger");
            var army = new List<UnitTemplate> { commander };
            while (ArmyRules.WhyCannotAdd(army, ranger) == null) army.Add(ranger);
            Assert.That(army.Count, Is.EqualTo(1 + (750 - commander.supplyCost) / ranger.supplyCost), "as many as the Supply Cap allows");
            Assert.That(army.Count, Is.GreaterThan(6));
            Assert.That(ArmyRules.IsValid(army), Is.True);
        }

        [Test]
        public void OnlyCommandersAreUnique()
        {
            Assert.That(ArmyRules.Problems(List(Commander, Commander)), Has.Some.Contains("Only one Field Commander"));
            Assert.That(ArmyRules.IsValid(List(Commander, Sniper, Sniper)), Is.True, "Specialists can be taken more than once");
            Assert.That(ArmyRules.IsValid(List(Commander, PA, PA)), Is.True);
            Assert.That(ArmyRules.IsValid(List(Commander, Priest, Priest)), Is.True);
            Assert.That(ArmyRules.IsUnique(UnitRole.Specialist), Is.False);
            Assert.That(ArmyRules.IsUnique(UnitRole.Commander), Is.True);
        }

        [Test]
        public void OnlyOneCommanderIsAllowedInAList()
        {
            var commanderPA = CommanderPA;
            Assert.That(ArmyRules.Problems(List(Commander, commanderPA)).Single(), Does.Contain("Only 1 Commander unit is allowed"));
            Assert.That(ArmyRules.WhyCannotAdd(List(Commander, Infantry), commanderPA), Does.Contain("already has its Commander"));
            Assert.That(ArmyRules.WhyCannotAdd(List(Infantry), commanderPA), Is.Null, "the first Commander is fine");
            Assert.That(ArmyRules.IsValid(List(commanderPA, Infantry, Priest)), Is.True);
        }

        [Test]
        public void InfantryAndHeavySupportCanBeTakenMoreThanOnce()
        {
            Assert.That(ArmyRules.IsValid(List(Commander, Infantry, Infantry, Infantry, MG, MG)), Is.True);
        }

        [Test]
        public void WhyAUnitCantBeAdded()
        {
            Assert.That(ArmyRules.WhyCannotAdd(List(Commander), Infantry), Is.Null);
            Assert.That(ArmyRules.WhyCannotAdd(List(Commander), Commander), Does.Contain("Already in the army"));
            Assert.That(ArmyRules.WhyCannotAdd(List(Sniper), Sniper), Is.Null, "Specialists can be duplicated");
            Assert.That(ArmyRules.WhyCannotAdd(List(Commander, Infantry), Infantry), Is.Null);
            Assert.That(ArmyRules.WhyCannotAdd(List(Commander, Sniper, Sniper, Sniper, Sniper, Sniper, Sniper), Sniper), Is.Null, "no slot limit");
        }

        [Test]
        public void TheAIsRandomArmiesAreAlwaysValidAndFull()
        {
            var available = TestCatalog().Available.ToList();
            var used = new HashSet<UnitTemplate>();
            for (int seed = 0; seed < 300; seed++)
            {
                var army = ArmyRules.RandomArmy(available, new System.Random(seed));
                Assert.That(ArmyRules.Problems(army), Is.Empty, $"seed {seed}");
                Assert.That(ArmyRules.SupplyOf(army), Is.LessThanOrEqualTo(ArmyRules.SupplyCap));
                Assert.That(available.Where(u => u.role != UnitRole.Commander).All(u => ArmyRules.WhyCannotAdd(army, u) != null), Is.True, "nothing more fits");
                Assert.That(army.Count(u => u.role == UnitRole.Commander), Is.EqualTo(1), "exactly one Commander");
                used.UnionWith(army);
            }
            Assert.That(used, Is.EquivalentTo(available), "every unit turns up sometimes");
        }

        [Test]
        public void TheCatalogListsEveryUnitCommandersFirst()
        {
            var catalog = TestCatalog();
            Assert.That(catalog.Available.Count(), Is.EqualTo(7));
            Assert.That(catalog.Available.Select(u => u.role.ListOrder()), Is.Ordered);
            Assert.That(catalog.Available.First().role, Is.EqualTo(UnitRole.Commander));
        }

        [Test]
        public void UnitsTakenMoreThanOnceAreNumbered()
        {
            var names = ArmyDeployment.UnitNames(List(Infantry, Commander, Infantry), Side.Player);
            Assert.That(names, Is.EqualTo(new[] { "Infantry Squad 1", "Field Commander", "Infantry Squad 2" }));
            Assert.That(ArmyDeployment.UnitNames(List(Commander), Side.AI).Single(), Is.EqualTo("Field Commander (Enemy)"));
        }

        [Test]
        public void TheBuilderShowsWhatAUnitIsMadeOf()
        {
            Assert.That(ArmyRules.Composition(Infantry), Is.EqualTo("Sergeant + 9× Trooper"));
            Assert.That(ArmyRules.Composition(Sniper), Is.EqualTo("Sniper"));
        }

        // ---- Unit stats and descriptions in the army builder ----

        [Test]
        public void TheBuilderListsEachSoldierTypesStats()
        {
            Assert.That(UnitInfo.SoldierLines(Infantry), Is.EqualTo(new[]
            {
                "Sergeant — Health 5 · Armor 1 · Accuracy 75% · Move 10 m (sprint 20 m) · Assault Rifle, Pistol",
                "9× Trooper — Health 5 · Armor 1 · Accuracy 75% · Move 10 m (sprint 20 m) · Assault Rifle",
            }));
            Assert.That(UnitInfo.SoldierLines(PA).Last(), Does.StartWith("4× Trooper in Powered Armor — Health 5 · Armor 2 · Accuracy 75% · Move 6 m (sprint 12 m)"));
            Assert.That(UnitInfo.SoldierLines(Sniper).Single(), Does.Contain("Accuracy 100% · Move 15 m (sprint 30 m) · Sniper Rifle"));
        }

        [Test]
        public void TheBuilderListsEachWeaponOnce()
        {
            Assert.That(UnitInfo.WeaponLines(Infantry), Is.EqualTo(new[]
            {
                "Assault Rifle — Damage 4 · Shots 1 · Penetration 1 · Range 20 m",
                "Pistol — Damage 2 · Shots 1 · Penetration 0 · Range 10 m · Pistol",
            }));
            Assert.That(UnitInfo.WeaponLines(Sniper).Single(), Is.EqualTo("Sniper Rifle — Damage 10 · Shots 1 · Penetration 1 · +50% accuracy · Range 50 m"));
            Assert.That(UnitInfo.WeaponLines(MG).Select(l => l.Split(' ')[0]), Is.EqualTo(new[] { "Pistol", "Machine" }));
            Assert.That(UnitInfo.WeaponLines(MG).Last(), Is.EqualTo("Machine Gun — Damage 5 · Shots 5 · Penetration 1 · Range 20 m · Deployed"));
        }

        [Test]
        public void TheBuilderListsAbilitiesAndKeywords()
        {
            Assert.That(UnitInfo.AbilityLines(Infantry).Single(), Is.EqualTo("Frag Grenade (active)"), "no limit per battle");
            Assert.That(UnitInfo.AbilityLines(Priest), Has.Some.EqualTo("Divine Healing (active, spell)"));
            Assert.That(UnitInfo.AbilityLines(Commander).Single(), Does.Contain("(passive)"));
            Assert.That(UnitInfo.SharedKeywords(Commander), Does.Contain("Officer").And.Contain("Solo"));
            Assert.That(UnitInfo.SharedKeywords(Sniper), Does.Contain("Sniper Precision"));
        }

        [Test]
        public void TheArmyBuilderAddsRemovesAndChecks()
        {
            var catalog = TestCatalog();
            var builder = new ArmyBuilderScreen(catalog);
            builder.ChooseFaction(catalog.Factions.First());
            builder.Clear();
            Assert.That(builder.Army, Is.Empty);
            Assert.That(builder.Confirm(), Is.False, "an empty army has no Commander");
            Assert.That(builder.Add(Commander), Is.Null);
            int added = 0;
            string why;
            while ((why = builder.Add(Infantry)) == null && added < 100) added++;
            Assert.That(why, Does.Contain("Over the Supply Cap"), "squads can be added until the Supply Cap");
            Assert.That(added, Is.EqualTo((ArmyRules.SupplyCap - Commander.supplyCost) / Infantry.supplyCost));
            Assert.That(builder.SupplyTotal, Is.LessThanOrEqualTo(ArmyRules.SupplyCap));
            Assert.That(builder.Confirm(), Is.True);
            builder.RemoveAt(0);
            Assert.That(builder.Army.Count, Is.EqualTo(added));
            Assert.That(builder.Confirm(), Is.False);
            Assert.That(builder.Problems.Single(), Does.Contain("Commander"));
        }

        // ---- Factions ----

        [Test]
        public void EachFactionOffersOnlyItsOwnUnitsInCatalogOrder()
        {
            var catalog = TestCatalog();
            var blue = catalog.Factions.First();
            var grey = catalog.Factions.Last();
            Assert.That(catalog.UnitsFor(blue), Is.EqualTo(catalog.Available.Where(u => u != PA)));
            Assert.That(catalog.UnitsFor(grey), Is.EqualTo(catalog.Available.Where(u => u != Priest)));
            Assert.That(catalog.UnitsFor(null), Is.EqualTo(catalog.Available), "no faction: every unit");
        }

        [Test]
        public void AFactionsArmyCanOnlyHaveItsOwnUnits()
        {
            var limited = ScriptableObject.CreateInstance<Faction>();
            try
            {
                limited.displayName = "Test Faction";
                limited.units = List(Commander, Infantry);
                Assert.That(ArmyRules.IsValid(List(Commander, Infantry, Infantry), limited), Is.True);
                Assert.That(ArmyRules.Problems(List(Commander, Sniper), limited).Single(), Does.Contain("Sniper isn't available to Test Faction"));
                Assert.That(ArmyRules.WhyCannotAdd(List(Commander), Sniper, limited), Does.Contain("Not available to Test Faction"));
                Assert.That(ArmyRules.Problems(List(CommanderPA), limited), Has.Some.Contains("Field Commander in Powered Armor isn't available"),
                    "the faction decides the commanders too");
            }
            finally
            {
                Object.DestroyImmediate(limited);
            }
        }

        [Test]
        public void ThePrimaryColourTintsOnlyTheUnitsPrimaryParts()
        {
            var catalog = UnitCatalog.Load();
            var unit = UnitFactory.Spawn(Infantry, Side.Player, Vector3.zero, Quaternion.identity);
            try
            {
                var gray = new Color(0.46f, 0.47f, 0.49f);
                int tinted = PrimaryColor.Apply(unit.gameObject, catalog.primaryColorMaterial, gray);
                Assert.That(tinted, Is.GreaterThanOrEqualTo(10), "every soldier's body");
                foreach (var r in unit.GetComponentsInChildren<Renderer>())
                {
                    bool isGray = Vector4.Distance(PrimaryColor.Of(r), gray) < 0.002f;
                    if (r.sharedMaterial == catalog.primaryColorMaterial) Assert.That(isGray, Is.True, r.name);
                    else Assert.That(isGray, Is.False, $"{r.name} keeps its own colour");
                }
                Assert.That(unit.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterial).Distinct().Count(),
                    Is.LessThan(8), "no new materials were made");
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        [Test]
        public void TheBuilderShowsOnlyTheFactionsUnits()
        {
            var catalog = TestCatalog();
            var faction = catalog.Factions.Last();
            var builder = new ArmyBuilderScreen(catalog);
            Assert.That(builder.Faction, Is.Null);
            builder.ChooseFaction(faction);
            Assert.That(builder.Available, Is.EquivalentTo(catalog.UnitsFor(faction)));
            Assert.That(builder.Confirm(), Is.True, "starts from the demo army");
            builder.ChangeFaction();
            Assert.That((builder.Faction, builder.Army.Count, builder.Available.Count), Is.EqualTo(((Faction)null, 0, 0)));
        }

        [Test]
        public void TheBuilderRemembersTheLastArmyForItsFaction()
        {
            var catalog = TestCatalog();
            var usc = catalog.Factions.First();
            var builder = new ArmyBuilderScreen(catalog, usc, List(Commander, Sniper));
            builder.ChooseFaction(usc);
            Assert.That(builder.Army, Is.EqualTo(List(Commander, Sniper)));
            builder.ChooseFaction(catalog.Factions.Last());
            var grey = catalog.Factions.Last();
            Assert.That(builder.Army, Is.EqualTo(catalog.defaultArmy.Where(grey.Has)), "another faction starts from the demo army (its own units)");
        }

        [Test]
        public void SixSlotsAreSpreadAcrossEachDeploymentZone()
        {
            // The Boxed in's zones: 80 × 8 m at z = ∓26.
            var player = Enumerable.Range(0, ArmyDeployment.UnitsPerRow).Select(i => ArmyDeployment.Slot(new Vector3(0f, 0f, -26f), new Vector2(80f, 8f), Side.Player, i)).ToList();
            var ai = Enumerable.Range(0, ArmyDeployment.UnitsPerRow).Select(i => ArmyDeployment.Slot(new Vector3(0f, 0f, 26f), new Vector2(80f, 8f), Side.AI, i)).ToList();
            Assert.That(player.Select(p => p.x), Is.Ordered, "west to east for the player");
            Assert.That(player.All(p => Mathf.Abs(p.x) < 40f && Mathf.Approximately(p.z, -25f)), Is.True, "inside the zone, a little forward");
            for (int i = 0; i < ArmyDeployment.UnitsPerRow; i++)
                Assert.That(Vector3.Distance(ai[i], -player[i]), Is.LessThan(0.001f), "the AI's slots are the player's turned 180°");
            Assert.That(player[1].x - player[0].x, Is.EqualTo(80f / 6f).Within(0.001f), "evenly spaced");
        }

        [Test]
        public void MoreThanSixUnitsDeployInFurtherRowsBehind()
        {
            Eldoria.Core.BattlefieldScale.SetBoard(100f, 100f);
            try { RowsBehind(); }
            finally { Eldoria.Core.BattlefieldScale.ResetBoard(); }
        }

        private static void RowsBehind()
        {
            var zone = new Vector3(0f, 0f, -26f);
            var size = new Vector2(80f, 8f);
            var first = ArmyDeployment.Slot(zone, size, Side.Player, 0);
            var seventh = ArmyDeployment.Slot(zone, size, Side.Player, 6);
            var thirteenth = ArmyDeployment.Slot(zone, size, Side.Player, 12);
            Assert.That(seventh.x, Is.EqualTo(first.x).Within(0.001f), "the next row starts on the left again");
            Assert.That(first.z - seventh.z, Is.EqualTo(ArmyDeployment.RowSpacing).Within(0.001f), "a row behind");
            Assert.That(thirteenth.z, Is.LessThan(seventh.z));
            var ai = ArmyDeployment.Slot(-zone, size, Side.AI, 6);
            Assert.That(ai.z, Is.GreaterThan(ArmyDeployment.Slot(-zone, size, Side.AI, 0).z), "the AI's rows go back north");
            var far = ArmyDeployment.Slot(zone, size, Side.Player, 59);
            Assert.That(Mathf.Abs(far.z), Is.LessThanOrEqualTo(Eldoria.Core.BattlefieldScale.BoardDepth / 2f), "never off the battlefield");
            // A deep unit (the Rask Rifle Team: 4 ranks) keeps its back rank on the board as well.
            var rifles = UnitCatalog.Load().Available.Single(u => u.displayName == "Rask Rifle Team");
            float depth = ArmyDeployment.FormationDepth(rifles);
            Assert.That(depth, Is.EqualTo(3 * rifles.deploymentSpacing + rifles.leader.radius).Within(0.001f));
            var deep = ArmyDeployment.Slot(-zone, size, Side.AI, 59, depth);
            Assert.That(deep.z + depth, Is.LessThanOrEqualTo(Eldoria.Core.BattlefieldScale.BoardDepth / 2f - 2f + 0.001f), "its back rank too");
        }
    }
}
