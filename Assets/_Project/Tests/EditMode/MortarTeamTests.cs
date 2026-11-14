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
    /// The ISF Mortar Team (Heavy Support): 1 ISF Mortar Specialist (the ISF Rifleman's stats; Mortar Launcher and Assault
    /// Rifle) + 1 ISF Rifleman (the ISF Rifle Squad's: Assault Rifle, Frag Grenade).
    /// </summary>
    public class MortarTeamTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.First();
        private static UnitTemplate Team => Catalog.Available.Single(u => u.displayName == "Mortar Team");

        [SetUp]
        public void SetUp()
        {
            Turns.ResetCounter();
            Turns.StartNextTurn();
        }

        [TearDown]
        public void TearDown() => Turns.ResetCounter();

        [Test]
        public void TheMortarTeamIsAnIsfHeavySupportUnitOfASpecialistAndARifleman()
        {
            var team = Team;
            Assert.That(Isf.Has(team), Is.True);
            Assert.That(Catalog.Factions.ElementAt(1).Has(team), Is.False, "not the Draconis Imperialis's");
            Assert.That(team.role, Is.EqualTo(UnitRole.HeavySupport));
            Assert.That(team.Size, Is.EqualTo(2));
            Assert.That(ArmyRules.Composition(team), Is.EqualTo("ISF Mortar Specialist + ISF Rifleman"));
            var commander = Catalog.Available.Single(u => u.displayName == "Field Commander");
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { commander, team, team }, Isf), Is.True, "Heavy Support can be taken more than once");
        }

        [Test]
        public void TheMortarSpecialistHasTheIsfRiflemansStatsAndCarriesTheMortarAndARifle()
        {
            var specialist = Team.leader;
            var rifleman = Team.members.Single().soldier;
            var squadRifleman = Catalog.Available.Single(u => u.displayName == "ISF Rifle Squad").members.Single().soldier;
            Assert.That(specialist.displayName, Is.EqualTo("ISF Mortar Specialist"));
            Assert.That((specialist.health, specialist.armorLevel, specialist.accuracy, specialist.movementRange),
                Is.EqualTo((rifleman.health, rifleman.armorLevel, rifleman.accuracy, rifleman.movementRange)), "the ISF Rifleman's stats");
            Assert.That((specialist.health, specialist.armorLevel, specialist.accuracy, specialist.movementRange), Is.EqualTo((5, 2, 75, 10f)));
            Assert.That(specialist.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological" }));
            Assert.That(specialist.weapons.Select(w => w.displayName), Is.EquivalentTo(new[] { "Mortar Launcher", "Assault Rifle" }));
            Assert.That(specialist.PrimaryWeapon.displayName, Is.EqualTo("Mortar Launcher"));
            Assert.That(specialist.prefab, Is.Not.Null);

            Assert.That(rifleman, Is.SameAs(squadRifleman), "the ISF Rifle Squad's ISF Rifleman");
            Assert.That(rifleman.displayName, Is.EqualTo("ISF Rifleman"));
            Assert.That(rifleman.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Assault Rifle" }));
            Assert.That(UnitInfo.SoldierLines(Team), Is.EqualTo(new[]
            {
                "ISF Mortar Specialist \u2014 Health 5 \u00b7 Armor 2 \u00b7 Accuracy 75% \u00b7 Move 10 m (sprint 20 m) \u00b7 Mortar Launcher, Assault Rifle",
                "ISF Rifleman \u2014 Health 5 \u00b7 Armor 2 \u00b7 Accuracy 75% \u00b7 Move 10 m (sprint 20 m) \u00b7 Assault Rifle",
            }));
        }

        [Test]
        public void AfterMovingTheSpecialistFallsBackOnTheAssaultRifle()
        {
            var unit = UnitFactory.Spawn(Team, Side.Player, Vector3.zero, Quaternion.identity);
            var enemyUnit = UnitFactory.Spawn(Catalog.Available.Single(u => u.displayName == "ISF Rifle Squad"), Side.AI,
                new Vector3(0f, 0f, 15f), Quaternion.Euler(0f, 180f, 0f));
            try
            {
                var specialist = unit.Leader;
                var target = enemyUnit.Soldiers[0];
                Assert.That(specialist.WeaponFor(target).Name, Is.EqualTo("Mortar Launcher"), "standing still: the mortar");
                specialist.GainKeyword(KeywordLibrary.Get(KeywordLibrary.Moved), Turns.NextTurn);
                Assert.That(specialist.WeaponFor(target).Name, Is.EqualTo("Assault Rifle"), "after moving: the mortar is Deployed, so the rifle");
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
                Object.DestroyImmediate(enemyUnit.gameObject);
            }
        }
    }
}
