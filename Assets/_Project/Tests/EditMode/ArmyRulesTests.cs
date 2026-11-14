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
        public void TheGameHasNoFactionsOrUnitsYet()
        {
            var catalog = UnitCatalog.Load();
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Available, Is.Empty);
            Assert.That(catalog.Factions, Is.Empty);
            Assert.That(catalog.defaultArmy, Is.Empty);
            Assert.That(catalog.primaryColorMaterial, Is.Not.Null, "still there for deploying units");
            Assert.That(catalog.leaderMarker, Is.Not.Null);
            Assert.That(AssetDatabase.FindAssets("t:UnitTemplate", new[] { "Assets/_Project/Data/Units" }), Is.Empty);
            Assert.That(AssetDatabase.FindAssets("t:SoldierTemplate", new[] { "Assets/_Project/Data/Soldiers" }), Is.Empty);
            Assert.That(AssetDatabase.FindAssets("t:Faction", new[] { "Assets/_Project/Data/Factions" }), Is.Empty);
        }

        [Test]
        public void WithNoFactionsOrUnitsTheBuilderSaysSoAndTheAIHasNoArmy()
        {
            var builder = new ArmyBuilderScreen(UnitCatalog.Load());
            Assert.That(builder.HasUnits, Is.False);
            Assert.That(builder.Factions, Is.Empty);
            Assert.That(builder.Confirm(), Is.False);
            Assert.That(new ArmyBuilderScreen(null).HasUnits, Is.False);
            Assert.That(new ArmyBuilderScreen(TestCatalog()).HasUnits, Is.True);
            Assert.That(ArmyRules.RandomArmy(UnitCatalog.Load().Available, new System.Random(1)), Is.Empty, "nothing to field");
            Assert.That(MainMenu.StartBattle(null, List()), Is.EqualTo(new[] { ArmyBuilderScreen.NoUnitsMessage }));
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
        public void AnArmyHasSixSlots()
        {
            Assert.That(ArmyRules.IsValid(List(Commander, Infantry, Infantry, Infantry, Infantry, Infantry)), Is.True);
            Assert.That(ArmyRules.Problems(List(Commander, Infantry, Infantry, Infantry, Infantry, Infantry, Infantry)).Single(),
                Does.Contain("Too many units"));
        }

        [Test]
        public void CommandersAndSpecialistsCantBeDuplicated()
        {
            Assert.That(ArmyRules.Problems(List(Commander, Commander)), Has.Some.Contains("Only one Field Commander"));
            Assert.That(ArmyRules.Problems(List(Commander, Sniper, Sniper)).Single(), Does.Contain("Only one Sniper"));
            Assert.That(ArmyRules.Problems(List(Commander, PA, PA)).Single(), Does.Contain("Only one Powered Armor Squad"));
            Assert.That(ArmyRules.IsValid(List(Commander, Priest, Sniper, PA)), Is.True, "different ones are fine");
            Assert.That(ArmyRules.Problems(List(Commander, Priest, Priest)).Single(), Does.Contain("Only one Battle Priest"));
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
            Assert.That(ArmyRules.WhyCannotAdd(List(Sniper), Sniper), Does.Contain("Already in the army"));
            Assert.That(ArmyRules.WhyCannotAdd(List(Commander, Infantry), Infantry), Is.Null);
            Assert.That(ArmyRules.WhyCannotAdd(List(Commander, Infantry, Infantry, Infantry, Infantry, Infantry), MG), Does.Contain("slots are full"));
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
                Assert.That(army.Count, Is.EqualTo(6), "every slot filled");
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
            Assert.That(UnitInfo.WeaponLines(Sniper).Single(), Is.EqualTo("Sniper Rifle — Damage 5 · Shots 1 · Penetration 1 · +50% accuracy · Range 50 m · Deployed"));
            Assert.That(UnitInfo.WeaponLines(MG).Select(l => l.Split(' ')[0]), Is.EqualTo(new[] { "Pistol", "Machine" }));
            Assert.That(UnitInfo.WeaponLines(MG).Last(), Is.EqualTo("Machine Gun — Damage 5 · Shots 5 · Penetration 1 · Range 20 m · Deployed"));
        }

        [Test]
        public void TheBuilderListsAbilitiesAndKeywords()
        {
            Assert.That(UnitInfo.AbilityLines(Infantry).Single(), Is.EqualTo("Frag Grenade (active, once per battle)"));
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
            for (int i = 0; i < 5; i++) Assert.That(builder.Add(Infantry), Is.Null);
            Assert.That(builder.Add(Infantry), Does.Contain("slots are full"));
            Assert.That(builder.Confirm(), Is.True);
            builder.RemoveAt(0);
            Assert.That(builder.Army.Count, Is.EqualTo(5));
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
            var player = Enumerable.Range(0, ArmyRules.MaxUnits).Select(i => ArmyDeployment.Slot(new Vector3(0f, 0f, -26f), new Vector2(80f, 8f), Side.Player, i)).ToList();
            var ai = Enumerable.Range(0, ArmyRules.MaxUnits).Select(i => ArmyDeployment.Slot(new Vector3(0f, 0f, 26f), new Vector2(80f, 8f), Side.AI, i)).ToList();
            Assert.That(player.Select(p => p.x), Is.Ordered, "west to east for the player");
            Assert.That(player.All(p => Mathf.Abs(p.x) < 40f && Mathf.Approximately(p.z, -25f)), Is.True, "inside the zone, a little forward");
            for (int i = 0; i < ArmyRules.MaxUnits; i++)
                Assert.That(Vector3.Distance(ai[i], -player[i]), Is.LessThan(0.001f), "the AI's slots are the player's turned 180°");
            Assert.That(player[1].x - player[0].x, Is.EqualTo(80f / 6f).Within(0.001f), "evenly spaced");
        }
    }
}
