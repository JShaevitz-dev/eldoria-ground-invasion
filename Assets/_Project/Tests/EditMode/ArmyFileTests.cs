using System.Collections.Generic;
using System.IO;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>Saving and loading army lists as files (ArmyFiles), and the army builder's Save and Load.</summary>
    public class ArmyFileTests
    {
        private readonly List<Object> created = new List<Object>();
        private string folder;
        private UnitCatalog catalog;
        private Faction faction, other;
        private UnitTemplate custom;
        private WeaponTemplate smg;
        private Equipment plating;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "GroundInvasionArmyTests_" + System.Guid.NewGuid().ToString("N"));
            ArmyFiles.Folder = folder;

            plating = ScriptableObject.CreateInstance<Equipment>();
            plating.name = plating.displayName = "Armor Plating";
            var rifle = TestArmies.Weapon("AssaultRifle");
            smg = TestArmies.Weapon("SMG");
            var trooper = TestArmies.Soldier("File Trooper", 5, 1, 75, 10f, new[] { "Biological" }, null, new[] { rifle });
            trooper.weaponOptions.Add(new WeaponOption { weapon = rifle, alternatives = new List<WeaponTemplate> { smg } });
            trooper.equipmentOptions.Add(plating);
            custom = TestArmies.Unit("File Squad", UnitRole.Infantry, TestArmies.Sergeant, trooper, 4);
            catalog = ScriptableObject.CreateInstance<UnitCatalog>();
            catalog.units = new List<UnitTemplate> { TestArmies.FieldCommander, TestArmies.InfantrySquad, custom };
            faction = TestArmies.Faction("File Faction", Color.gray, catalog.units.ToArray());
            other = TestArmies.Faction("Other Faction", Color.red, TestArmies.FieldCommander, TestArmies.InfantrySquad);
            catalog.factions = new List<Faction> { faction, other };
            catalog.defaultArmy = new List<UnitTemplate>();
            created.AddRange(new Object[] { plating, trooper, custom, catalog, faction, other });
        }

        [TearDown]
        public void TearDown()
        {
            ArmyFiles.Folder = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        private ArmyBuilderScreen Builder(Faction f)
        {
            var builder = new ArmyBuilderScreen(catalog);
            builder.ChooseFaction(f);
            builder.Clear();
            return builder;
        }

        /// <summary>A list with a Commander and the custom squad, its troopers given the SMG and Armor Plating.</summary>
        private ArmyBuilderScreen CustomArmy()
        {
            var builder = Builder(faction);
            Assert.That(builder.Add(TestArmies.FieldCommander), Is.Null);
            Assert.That(builder.Add(custom), Is.Null);
            Assert.That(builder.Add(TestArmies.InfantrySquad), Is.Null);
            var trooper = custom.Roster().First(s => s.displayName == "File Trooper");
            Assert.That(builder.SetWeapon(1, trooper, trooper.weaponOptions[0].weapon, smg), Is.Null);
            Assert.That(builder.SetEquipment(1, trooper, plating, true), Is.Null);
            return builder;
        }

        [Test]
        public void ASavedArmyLoadsBackExactly()
        {
            var saved = CustomArmy();
            Assert.That(saved.SaveArmy("Strike Force"), Is.Null);
            Assert.That(File.Exists(Path.Combine(folder, "Strike Force" + ArmyFiles.Extension)), Is.True);

            var builder = Builder(other);
            Assert.That(builder.LoadArmy(ArmyFiles.PathFor("Strike Force")), Is.Null);
            Assert.That(builder.Faction, Is.SameAs(faction), "loading switches to the army's faction");
            Assert.That(builder.Army, Is.EqualTo(saved.Army));
            var trooper = custom.Roster().First(s => s.displayName == "File Trooper");
            var loadout = builder.Loadouts[1].Get(trooper);
            Assert.That(loadout, Is.Not.Null);
            Assert.That(loadout.Swap(trooper.weaponOptions[0].weapon), Is.SameAs(smg));
            Assert.That(loadout.Equipment, Is.EqualTo(new[] { plating }));
            Assert.That(builder.Problems, Is.Empty);
            Assert.That(builder.SaveName, Is.EqualTo("Strike Force"));
            Assert.That(builder.Confirm(), Is.True);
        }

        [Test]
        public void SavedArmiesAreListedMostRecentFirst()
        {
            var builder = CustomArmy();
            Assert.That(builder.SaveArmy("First"), Is.Null);
            File.SetLastWriteTime(ArmyFiles.PathFor("First"), System.DateTime.Now.AddMinutes(-5));
            builder.RemoveAt(2);
            Assert.That(builder.SaveArmy("Second"), Is.Null);
            var list = ArmyFiles.List(catalog);
            Assert.That(list.Select(a => a.Name), Is.EqualTo(new[] { "Second", "First" }));
            Assert.That(list[0].FactionName, Is.EqualTo("File Faction"));
            Assert.That(list[0].UnitCount, Is.EqualTo(2));
            Assert.That(list[1].UnitCount, Is.EqualTo(3));
            Assert.That(list.All(a => a.Error == null), Is.True);
        }

        [Test]
        public void SavingUnderTheSameNameReplacesTheArmy()
        {
            var builder = CustomArmy();
            builder.SaveArmy("Mine");
            builder.RemoveAt(2);
            builder.SaveArmy("Mine");
            Assert.That(ArmyFiles.List(catalog).Count, Is.EqualTo(1));
            Assert.That(ArmyFiles.List(catalog)[0].UnitCount, Is.EqualTo(2));
        }

        [Test]
        public void NamesAreCleanedForTheFileSystem()
        {
            Assert.That(ArmyFiles.CleanName("  My: army/<1>?  "), Is.EqualTo("My army1"));
            Assert.That(ArmyFiles.CleanName("..."), Is.EqualTo(""));
            Assert.That(ArmyFiles.CleanName(new string('x', 80)).Length, Is.EqualTo(ArmyFiles.MaxNameLength));
            var builder = CustomArmy();
            Assert.That(builder.SaveArmy("   "), Is.EqualTo("Give the army a name"));
            Assert.That(builder.SaveArmy("a/b"), Is.Null);
            Assert.That(ArmyFiles.Exists("ab"), Is.True);
        }

        [Test]
        public void AnEmptyArmyIsNotSaved()
        {
            Assert.That(Builder(faction).SaveArmy("Nothing"), Is.EqualTo("The army is empty"));
            Assert.That(ArmyFiles.List().Count, Is.EqualTo(0));
        }

        [Test]
        public void ABrokenFileIsListedWithItsProblemAndNotLoaded()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "Junk" + ArmyFiles.Extension), "this is not json");
            File.WriteAllText(Path.Combine(folder, "Ghost" + ArmyFiles.Extension),
                JsonUtility.ToJson(new ArmyFile { name = "Ghost", army = new Network.ArmyMsg { faction = "File Faction", units = new List<string> { "No Such Unit" } } }));
            var list = ArmyFiles.List(catalog);
            Assert.That(list.First(a => a.Name == "Junk").Error, Is.EqualTo("Not an army file"));
            Assert.That(list.First(a => a.Name == "Ghost").Error, Is.Null, "readable; the unit is only checked on loading");

            var builder = CustomArmy();
            Assert.That(builder.LoadArmy(ArmyFiles.PathFor("Junk")), Is.EqualTo("Not an army file"));
            Assert.That(builder.LoadArmy(ArmyFiles.PathFor("Ghost")), Is.EqualTo("Unknown unit 'No Such Unit'"));
            Assert.That(builder.Army.Count, Is.EqualTo(3), "a failed load leaves the list as it was");
        }

        [Test]
        public void AnArmyWithUnitsItsFactionCannotTakeIsRefused()
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(ArmyFiles.PathFor("Smuggled"), JsonUtility.ToJson(new ArmyFile
            {
                name = "Smuggled",
                army = new Network.ArmyMsg { faction = "Other Faction", units = new List<string> { "Field Commander", "File Squad" } },
            }));
            Assert.That(Builder(faction).LoadArmy(ArmyFiles.PathFor("Smuggled")), Is.EqualTo("File Squad isn't available to Other Faction"));
        }

        [Test]
        public void DeletingASavedArmyRemovesTheFile()
        {
            var builder = CustomArmy();
            builder.SaveArmy("Gone");
            builder.OpenFilePanel(ArmyBuilderScreen.FilePanel.Load);
            Assert.That(builder.SavedArmies.Count, Is.EqualTo(1));
            Assert.That(builder.DeleteSavedArmy(builder.SavedArmies[0].Path), Is.Null);
            Assert.That(builder.SavedArmies, Is.Empty);
            Assert.That(ArmyFiles.Exists("Gone"), Is.False);
        }

        [Test]
        public void TheSavePanelSuggestsAName()
        {
            var builder = CustomArmy();
            builder.OpenFilePanel(ArmyBuilderScreen.FilePanel.Save);
            Assert.That(builder.OpenPanel, Is.EqualTo(ArmyBuilderScreen.FilePanel.Save));
            Assert.That(builder.SaveName, Is.EqualTo("File Faction army"));
            builder.OpenFilePanel(ArmyBuilderScreen.FilePanel.None);
            Assert.That(builder.OpenPanel, Is.EqualTo(ArmyBuilderScreen.FilePanel.None));
        }
    }
}
