using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Eldoria.Tests
{
    /// <summary>
    /// The main menu: the first scene; Play chooses a map then opens the army builder; back to the menu. This fork has no
    /// Commanders yet (only its three factions and the ISF's Rifle Squad and Heavy Weapons Team), so the army builder says so and a battle can't be started.
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

        [Test]
        public void TheGameHasItsThreeFactionsButNoCommandersYet()
        {
            var catalog = UnitCatalog.Load();
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Factions.Select(f => f.displayName), Is.EqualTo(new[] { "International Space Force of Humanity", "Imperial Army of The Draconis Imperialis", "Most Holy Army of The Rask Divine Collective" }));
            Assert.That(catalog.Available.Select(u => u.displayName), Is.EqualTo(new[] { "ISF Rifle Squad", "Heavy Weapons Team" }));
            Assert.That(ArmyBuilderScreen.CatalogCanFieldArmies(catalog), Is.False, "no Commanders yet");
            Assert.That(catalog.primaryColorMaterial, Is.Not.Null);
            Assert.That(catalog.leaderMarker, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator PlayOpensTheArmyBuilderWhichSaysThereAreNoCommanders()
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.Builder, Is.Null);

            menu.OpenArmyBuilder();
            yield return null;   // draws the builder: no Commanders, so the message (and the factions, greyed out)
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"), "still on the menu");
            Assert.That(menu.Builder.CanFieldArmies, Is.False);
            Assert.That(menu.Builder.Factions.Count, Is.EqualTo(3));
            Assert.That(menu.Builder.Faction, Is.Null);
            Assert.That(menu.Builder.Available, Is.Empty);
            Assert.That(menu.Builder.Confirm(), Is.False);
            Assert.That(ArmyBuilderScreen.NoArmiesMessage, Does.Contain("No faction has a Commander yet"));
        }

        [UnityTest]
        public IEnumerator ABattleCantBeStartedAndTheGameStaysOnTheMenu()
        {
            var problems = MainMenu.StartBattle(null, new List<UnitTemplate>());
            Assert.That(problems, Is.EqualTo(new[] { ArmyBuilderScreen.NoArmiesMessage }));
            // Even a made-up army with a Commander is refused: no faction in the game can field an army for the AI.
            var squad = TestArmies.InfantrySquad;
            var faction = TestArmies.Faction("Test", Color.gray, squad, TestArmies.FieldCommander);
            Assert.That(MainMenu.StartBattle(faction, new[] { TestArmies.FieldCommander, squad }), Is.EqualTo(new[] { ArmyBuilderScreen.NoArmiesMessage }));
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(ArmySelection.HasArmies, Is.False);
        }

        [UnityTest]
        public IEnumerator PlayChoosesAMapThenOpensTheArmyBuilder()
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            menu.OpenMapChoice();
            Assert.That((menu.ChoosingMap, menu.Builder), Is.EqualTo((true, (ArmyBuilderScreen)null)), "the map comes first");
            Assert.That(Battlefields.All.Select(m => m.Name), Is.EqualTo(new[] { "Boxed in", "Open Season" }));
            yield return null;

            menu.ChooseMap(Battlefields.EnglishCountryside);
            Assert.That(menu.ChoosingMap, Is.False);
            Assert.That(menu.Builder, Is.Not.Null, "then the army builder");
            Assert.That(MainMenu.ChosenMap, Is.SameAs(Battlefields.EnglishCountryside));
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }

        [UnityTest]
        public IEnumerator TheGameCanReturnToTheMenu()
        {
            ArmySelection.Set(new[] { TestArmies.FieldCommander }, new[] { TestArmies.FieldCommander });
            SceneManager.LoadScene(MainMenu.GameScene);
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainMenu.GameScene));
            MainMenu.BackToMenu();
            yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }
    }
}
