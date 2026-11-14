using System.Collections;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eldoria.Tests
{
    /// <summary>
    /// The main menu: the first scene; Play chooses a map then opens the army builder; a valid army starts a game against
    /// an AI army; back to the menu. So far the ISF and the Draconis Imperialis can field armies (the Rask have no units),
    /// so the AI fields one of those two.
    /// </summary>
    public class MainMenuPlayTests
    {
        [UnitySetUp]
        public IEnumerator LoadMenu()
        {
            ArmySelection.Clear();
            MainMenu.ForgetLastArmy();
            SceneManager.LoadScene(MainMenu.MenuScene);
            yield return null;
            yield return null;
        }

        [TearDown]
        public void ForgetArmies() => ArmySelection.Clear();

        private static Faction Isf => UnitCatalog.Load().Factions.First();

        [Test]
        public void TheIsfAndTheDraconisImperialisCanFieldArmiesSoFar()
        {
            var catalog = UnitCatalog.Load();
            Assert.That(catalog.Factions.Select(f => f.displayName), Is.EqualTo(new[] { "International Space Force of Humanity", "Imperial Army of The Draconis Imperialis", "Most Holy Army of The Rask Divine Collective" }));
            Assert.That(ArmyBuilderScreen.CatalogCanFieldArmies(catalog), Is.True);
            Assert.That(catalog.Factions.Select(f => ArmyBuilderScreen.FactionCanFieldArmy(catalog, f)), Is.EqualTo(new[] { true, true, false }));
        }

        [UnityTest]
        public IEnumerator PlayOpensTheArmyBuilderOnTheFactionChoice()
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.Builder, Is.Null);

            menu.OpenArmyBuilder();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"), "still on the menu, building");
            Assert.That(menu.Builder.CanFieldArmies, Is.True);
            Assert.That(menu.Builder.Faction, Is.Null, "a faction is chosen first");
            Assert.That(menu.Builder.Available, Is.Empty, "no units until a faction is chosen");
            Assert.That(menu.Builder.FactionStatus(menu.Builder.Factions[0]), Is.EqualTo("7 units available"));
            Assert.That(menu.Builder.FactionStatus(menu.Builder.Factions[1]), Is.EqualTo("3 units available"));
            Assert.That(menu.Builder.FactionStatus(menu.Builder.Factions[2]), Is.EqualTo("no units yet"));

            menu.Builder.ChooseFaction(Isf);
            yield return null;   // draws the unit list
            Assert.That(menu.Builder.Available.Select(u => u.displayName), Is.EqualTo(new[] { "Field Commander", "ISF Rifle Squad", "Heavy Weapons Team", "Mortar Team", "Olympian Mech", "Sagittarius MBT", "ISF Ranger" }),
                "Commanders first, then Infantry, then Heavy Support");
            Assert.That(menu.Builder.Army, Is.Empty, "no demo army set yet: the list starts empty");
        }

        [UnityTest]
        public IEnumerator AnInvalidArmyIsRefusedAndStaysOnTheMenu()
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            menu.OpenArmyBuilder();
            var builder = menu.Builder;
            builder.ChooseFaction(Isf);
            Assert.That(builder.Add(builder.Available.First(u => u.displayName == "ISF Rifle Squad")), Is.Null);

            Assert.That(builder.Confirm(), Is.False);
            Assert.That(builder.Problems.Single(), Does.Contain("Commander"));
            Assert.That(MainMenu.StartBattle(builder.Faction, builder.Army), Is.Not.Empty);
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(ArmySelection.HasArmies, Is.False);
        }

        [UnityTest]
        public IEnumerator AValidArmyIsTakenToTheBattlefieldAgainstAnAIArmy()
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            menu.OpenArmyBuilder();
            var builder = menu.Builder;
            builder.ChooseFaction(Isf);
            UnitTemplate Unit(string name) => builder.Available.First(u => u.displayName == name);
            foreach (var name in new[] { "Field Commander", "ISF Rifle Squad", "Heavy Weapons Team", "Heavy Weapons Team" })
                Assert.That(builder.Add(Unit(name)), Is.Null, name);
            Assert.That(builder.Add(Unit("Field Commander")), Does.Contain("Already in the army"));
            Assert.That(builder.Confirm(), Is.True);
            var army = builder.Army.ToList();

            Assert.That(MainMenu.StartBattle(builder.Faction, army), Is.Empty);
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("UrbanDemo"));
            var controller = Object.FindFirstObjectByType<MovementController>();
            Assert.That(controller.PlayerUnits.Select(u => u.DisplayName), Is.EqualTo(new[]
                { "Field Commander", "ISF Rifle Squad", "Heavy Weapons Team 1", "Heavy Weapons Team 2" }));
            Assert.That(ArmySelection.PlayerFaction, Is.SameAs(Isf));
            var catalog = UnitCatalog.Load();
            Assert.That(ArmyBuilderScreen.FactionCanFieldArmy(catalog, ArmySelection.AIFaction), Is.True, "the AI picks a faction that can field an army");
            Assert.That(ArmyRules.IsValid(ArmySelection.AIArmy, ArmySelection.AIFaction), Is.True, "the AI's army follows the same rules");
            Assert.That(ArmySelection.AIArmy.Count, Is.EqualTo(6), "the AI fills every slot");
            var aiUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None).Where(u => u.Side == Side.AI).ToList();
            Assert.That(aiUnits.Count, Is.EqualTo(ArmySelection.AIArmy.Count));
            Assert.That(Turns.Current, Is.EqualTo(1), "a fresh battle on turn 1");
            Assert.That(Victory.IsOver, Is.False);
        }

        [UnityTest]
        public IEnumerator PlayChoosesAMapThenTheArmyFightsThere()
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            menu.OpenMapChoice();
            Assert.That((menu.ChoosingMap, menu.Builder), Is.EqualTo((true, (ArmyBuilderScreen)null)), "the map comes first");
            Assert.That(Battlefields.All.Select(m => m.Name), Is.EqualTo(new[] { "Boxed in", "Open Season", "Siege of Ft Solstice" }));

            menu.ChooseMap(Battlefields.EnglishCountryside);
            Assert.That(menu.ChoosingMap, Is.False);
            Assert.That(menu.Builder, Is.Not.Null, "then the army builder");
            menu.Builder.ChooseFaction(Isf);
            menu.Builder.Randomize(new System.Random(7));
            Assert.That(menu.Builder.Confirm(), Is.True, "a random army is valid");
            Assert.That(MainMenu.StartBattle(menu.Builder.Faction, menu.Builder.Army.ToList()), Is.Empty);
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Countryside"));
            var zones = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None);
            foreach (var unit in Object.FindObjectsByType<Unit>(FindObjectsSortMode.None))
            foreach (var s in unit.Soldiers)
                Assert.That(zones.Single(z => z.Side == unit.Side).Contains(s.Position), Is.True, $"{unit.DisplayName}: {s.name}");
            Assert.That(Object.FindFirstObjectByType<MovementController>().PlayerUnits.Count, Is.EqualTo(ArmySelection.PlayerArmy.Count));
        }

        [UnityTest]
        public IEnumerator TheGameCanReturnToTheMenu()
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            menu.OpenArmyBuilder();
            menu.Builder.ChooseFaction(Isf);
            menu.Builder.Randomize(new System.Random(3));
            Assert.That(MainMenu.StartBattle(menu.Builder.Faction, menu.Builder.Army.ToList()), Is.Empty);
            yield return null;
            MainMenu.BackToMenu();
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }
    }
}
