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
    /// factions or units yet, so the army builder says so and a battle can't be started.
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
        public void TheGameHasNoFactionsOrUnitsYet()
        {
            var catalog = UnitCatalog.Load();
            Assert.That(catalog, Is.Not.Null, "the (empty) catalog is still there for runtime materials");
            Assert.That(catalog.Factions, Is.Empty);
            Assert.That(catalog.Available, Is.Empty);
            Assert.That(ArmyBuilderScreen.CatalogHasUnits(catalog), Is.False);
            Assert.That(catalog.primaryColorMaterial, Is.Not.Null);
            Assert.That(catalog.leaderMarker, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator PlayOpensTheArmyBuilderWhichSaysThereAreNoUnits()
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            Assert.That(menu, Is.Not.Null);
            Assert.That(menu.Builder, Is.Null);

            menu.OpenArmyBuilder();
            yield return null;   // draws the builder: no factions, so the no-units message instead of a crash
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"), "still on the menu");
            Assert.That(menu.Builder.HasUnits, Is.False);
            Assert.That(menu.Builder.Factions, Is.Empty);
            Assert.That(menu.Builder.Faction, Is.Null);
            Assert.That(menu.Builder.Available, Is.Empty);
            Assert.That(menu.Builder.Confirm(), Is.False);
            Assert.That(ArmyBuilderScreen.NoUnitsMessage, Does.Contain("no factions or units"));
        }

        [UnityTest]
        public IEnumerator ABattleCantBeStartedAndTheGameStaysOnTheMenu()
        {
            var problems = MainMenu.StartBattle(null, new List<UnitTemplate>());
            Assert.That(problems, Is.EqualTo(new[] { ArmyBuilderScreen.NoUnitsMessage }));
            // Even a made-up army is refused: there is nothing for the AI to field.
            var squad = TestArmies.InfantrySquad;
            var faction = TestArmies.Faction("Test", Color.gray, squad);
            Assert.That(MainMenu.StartBattle(faction, new[] { squad }), Is.EqualTo(new[] { ArmyBuilderScreen.NoUnitsMessage }));
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
