using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// Units that only a particular Commander brings (UnitTemplate.onlyWithCommanders): hidden in the army builder and not
    /// allowed in an army until one of their Commanders is in the list.
    /// </summary>
    public class CommanderGatedUnitTests
    {
        private readonly List<Object> created = new List<Object>();
        private UnitTemplate commander, otherCommander, infantry, guard;

        [SetUp]
        public void SetUp()
        {
            commander = TestArmies.FieldCommander;
            infantry = TestArmies.InfantrySquad;
            otherCommander = TestArmies.Unit("Gate Test Commander", UnitRole.Commander,
                TestArmies.Soldier("Gate Officer", 10, 1, 75, 6f, new[] { "Biological", "Officer", "Solo" }, null, new[] { TestArmies.Weapon("Pistol") }));
            guard = TestArmies.Unit("Honour Guard", UnitRole.Specialist,
                TestArmies.Soldier("Guard Sergeant", 5, 1, 75, 6f, new[] { "Biological" }, null, new[] { TestArmies.Weapon("AssaultRifle") }),
                TestArmies.Soldier("Guard", 5, 1, 75, 6f, new[] { "Biological" }, null, new[] { TestArmies.Weapon("AssaultRifle") }), 4);
            guard.onlyWithCommanders = new List<UnitTemplate> { commander };
            created.Add(otherCommander);
            created.Add(guard);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        private ArmyBuilderScreen Builder()
        {
            var catalog = ScriptableObject.CreateInstance<UnitCatalog>();
            var faction = TestArmies.Faction("Gate Test", Color.grey, commander, otherCommander, infantry, guard);
            created.Add(catalog);
            created.Add(faction);
            catalog.units = new List<UnitTemplate> { commander, otherCommander, infantry, guard };
            catalog.factions = new List<Faction> { faction };
            catalog.defaultArmy = new List<UnitTemplate>();
            var builder = new ArmyBuilderScreen(catalog);
            builder.ChooseFaction(faction);
            builder.Clear();
            return builder;
        }

        [Test]
        public void AUnitWithoutRequiredCommandersIsAlwaysUnlocked()
        {
            Assert.That(infantry.NeedsCommander, Is.False);
            Assert.That(ArmyRules.IsUnlocked(new List<UnitTemplate>(), infantry), Is.True);
            Assert.That(guard.NeedsCommander, Is.True);
        }

        [Test]
        public void TheUnitCanOnlyBeAddedWithItsCommander()
        {
            var army = new List<UnitTemplate>();
            Assert.That(ArmyRules.WhyCannotAdd(army, guard), Is.EqualTo("Needs Field Commander in the army"));
            army.Add(otherCommander);
            Assert.That(ArmyRules.WhyCannotAdd(army, guard), Is.EqualTo("Needs Field Commander in the army"), "another Commander doesn't unlock it");
            army[0] = commander;
            Assert.That(ArmyRules.WhyCannotAdd(army, guard), Is.Null);
        }

        [Test]
        public void AnyOfSeveralCommandersUnlocksIt()
        {
            guard.onlyWithCommanders.Add(otherCommander);
            Assert.That(ArmyRules.RequiredCommandersText(guard), Is.EqualTo("Field Commander or Gate Test Commander"));
            Assert.That(ArmyRules.WhyCannotAdd(new List<UnitTemplate> { otherCommander }, guard), Is.Null);
        }

        [Test]
        public void AListWithTheUnitButNotItsCommanderIsInvalid()
        {
            var problems = ArmyRules.Problems(new List<UnitTemplate> { otherCommander, guard });
            Assert.That(problems, Does.Contain("Honour Guard can only be taken with Field Commander in the army."));
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { commander, guard, guard }), Is.True);
        }

        [Test]
        public void TheArmyBuilderHidesTheUnitUntilItsCommanderIsInTheList()
        {
            var builder = Builder();
            Assert.That(builder.Available, Does.Contain(guard));
            Assert.That(builder.Shown.Contains(guard), Is.False, "hidden: no Commander yet");
            Assert.That(builder.Add(guard), Is.EqualTo("Needs Field Commander in the army"));

            Assert.That(builder.Add(otherCommander), Is.Null);
            Assert.That(builder.Shown.Contains(guard), Is.False, "still hidden with another Commander");

            builder.RemoveAt(0);
            Assert.That(builder.Add(commander), Is.Null);
            Assert.That(builder.Shown, Does.Contain(guard), "shown once its Commander is in the list");
            Assert.That(builder.Add(guard), Is.Null);
            Assert.That(builder.Add(infantry), Is.Null);
            Assert.That(builder.Add(guard), Is.Null);
            Assert.That(builder.Confirm(), Is.True);
        }

        [Test]
        public void RemovingTheCommanderRemovesTheUnitsItBrought()
        {
            var builder = Builder();
            builder.Add(commander);
            builder.Add(guard);
            builder.Add(infantry);
            builder.Add(guard);
            builder.RemoveAt(0);
            Assert.That(builder.Army, Is.EqualTo(new[] { infantry }));
            Assert.That(builder.Loadouts.Count, Is.EqualTo(1));
            Assert.That(builder.Shown.Contains(guard), Is.False);
        }

        [Test]
        public void RandomArmiesOnlyTakeTheUnitWithItsCommander()
        {
            var pool = new[] { commander, otherCommander, infantry, guard };
            for (int seed = 0; seed < 40; seed++)
            {
                var army = ArmyRules.RandomArmy(pool, new System.Random(seed));
                Assert.That(ArmyRules.IsValid(army), Is.True, $"seed {seed}: {string.Join("; ", ArmyRules.Problems(army))}");
                if (army.Contains(guard)) Assert.That(army, Does.Contain(commander), $"seed {seed}");
            }
        }
    }
}
