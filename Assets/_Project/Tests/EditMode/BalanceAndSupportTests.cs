using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The ISF Combat Engineer and Combat Medic; Large machines sheltering friendly soldiers (half-cover); no volley wasted
    /// on a target armor would make immune; the tanks' 50 m Frag Grenade; the Mortar Team's Anti-infantry Mortar; the Rask
    /// Rifleman's move; the HUD drawn over the soldier labels.
    /// </summary>
    public class BalanceAndSupportTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.ElementAt(0);
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static Faction Rask => Catalog.Factions.ElementAt(2);
        private static UnitTemplate UnitNamed(string name) => Catalog.Available.Single(u => u.displayName == name);

        private readonly List<Unit> spawned = new List<Unit>();

        [TearDown]
        public void TearDown()
        {
            foreach (var u in spawned) if (u != null) Object.DestroyImmediate(u.gameObject);
            spawned.Clear();
        }

        private Unit Spawn(string unit, Side side, Vector3 at, Faction faction)
        {
            var u = ArmyDeployment.Spawn(UnitNamed(unit), side, at, Quaternion.identity, unit, null, faction);
            spawned.Add(u);
            return u;
        }

        // ------------------------------------------------------------------ ISF Combat Engineer and Combat Medic

        [TestCase("ISF Combat Engineer", "Repair Kit")]
        [TestCase("ISF Combat Medic", "Medkit")]
        public void TheIsfHasACombatEngineerAndACombatMedic(string name, string kit)
        {
            var unit = UnitNamed(name);
            Assert.That(Isf.Has(unit), Is.True);
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(1));
            var s = unit.leader;
            Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((5, 2, 75, 10f)), "the ISF Rifleman's stats");
            Assert.That(s.keywords.Select(k => k.displayName), Is.EquivalentTo(new[] { "Biological", "Solo" }));
            Assert.That(s.PrimaryWeapon.displayName, Is.EqualTo("Assault Rifle"));
            Assert.That(s.equipment.Select(e => e.displayName), Is.EqualTo(new[] { kit }));
            Assert.That(s.equipment[0].abilities.Select(a => a.displayName), Is.EqualTo(new[] { kit }));
            Assert.That(unit.supplyCost, Is.GreaterThan(0));
            Assert.That(unit.description, Does.Contain(kit));
        }

        [Test]
        public void TheMedicHealsAndTheEngineerRepairs()
        {
            var medic = Spawn("ISF Combat Medic", Side.Player, new Vector3(0f, 0f, 0f), Isf).Leader;
            var engineer = Spawn("ISF Combat Engineer", Side.Player, new Vector3(3f, 0f, 0f), Isf).Leader;
            var medkit = medic.Abilities.Single(a => a.displayName == "Medkit");
            var repair = engineer.Abilities.Single(a => a.displayName == "Repair Kit");
            medic.TakeDamage(3);
            Assert.That(ActiveAbilities.WhyCannotUse(medic, medkit, targetSoldier: medic), Is.Null, "the medic can patch themself up");
            Assert.That(ActiveAbilities.WhyCannotUse(engineer, repair, targetSoldier: medic), Is.Not.Null, "a Repair Kit is for machines");
        }

        // ------------------------------------------------------------------ Large

        [Test]
        public void TheBigMachinesAreLarge()
        {
            foreach (var name in new[] { "Sagittarius MBT", "Marshal Command Tank", "Tyrant Hover-tank", "Stinger Heavy Robot", "Intimidator Medium Robot", "Rask Reliquary" })
                Assert.That(UnitNamed(name).leader.keywords.Any(k => k.displayName == "Large"), Is.True, name);
            Assert.That(UnitNamed("Olympian Mech").leader.keywords.Any(k => k.displayName == "Large"), Is.False, "not asked for");
            Assert.That(KeywordLibrary.Get("Large").description, Does.Contain("half-cover"));
        }

        [Test]
        public void ALargeMachineShieldsFriendlyBiologicalSoldiersBehindIt()
        {
            var tank = Spawn("Sagittarius MBT", Side.Player, new Vector3(0f, 0f, 0f), Isf).Leader;
            var squad = Spawn("ISF Rifle Squad", Side.Player, new Vector3(-20f, 0f, -15f), Isf);
            var rifleman = squad.LivingSoldiers.First();
            var enemy = Spawn("Rask Rifle Team", Side.AI, new Vector3(0f, 0f, 18f), Rask).Leader;
            var weapon = enemy.WeaponFor(null);

            rifleman.transform.position = new Vector3(0f, 0f, -4f);   // right behind the tank, as seen by the enemy
            NearbyKeywords.Refresh(rifleman, rifleman.Position);
            Assert.That(rifleman.HasKeyword("In half-cover"), Is.True, "within 5 m of a friendly Large machine");
            var penalty = KeywordRules.Evaluate(enemy, weapon, rifleman).AccuracyModifiers;
            Assert.That(penalty.Any(m => m.Type == AccuracyModifierType.Penalty && m.Value == 25), Is.True, "attacks through the tank take −25%");

            // Shot at from the side, the tank isn't between them: no penalty.
            enemy.transform.position = new Vector3(18f, 0f, -4f);
            Assert.That(KeywordRules.Evaluate(enemy, weapon, rifleman).AccuracyModifiers, Is.Empty);

            // Too far from it: no cover.
            rifleman.transform.position = new Vector3(0f, 0f, -12f);
            NearbyKeywords.Refresh(rifleman, rifleman.Position);
            Assert.That(rifleman.HasKeyword("In half-cover"), Is.False);
        }

        [Test]
        public void OnlyFriendlyBiologicalSoldiersShelterBehindALargeOne()
        {
            var enemyTank = Spawn("Tyrant Hover-tank", Side.AI, new Vector3(0f, 0f, 0f), Draconis).Leader;
            var rifleman = Spawn("ISF Rifle Squad", Side.Player, new Vector3(-20f, 0f, -15f), Isf).LivingSoldiers.First();
            rifleman.transform.position = new Vector3(0f, 0f, -4f);
            NearbyKeywords.Refresh(rifleman, rifleman.Position);
            Assert.That(rifleman.HasKeyword("In half-cover"), Is.False, "an enemy machine shelters no one");

            var tank = Spawn("Sagittarius MBT", Side.Player, new Vector3(20f, 0f, 0f), Isf).Leader;
            var transport = Spawn("Workhorse Transport", Side.Player, new Vector3(20f, 0f, -6f), Isf).Leader;
            NearbyKeywords.Refresh(transport, transport.Position);
            Assert.That(transport.HasKeyword("In half-cover"), Is.False, "machines don't hide behind machines");
            Assert.That(enemyTank.IsAlive && tank.IsAlive, Is.True);
        }

        // ------------------------------------------------------------------ No wasted volleys

        [Test]
        public void AUnitWontShootATargetItsWeaponsCantHurt()
        {
            var rifles = Spawn("ISF Rifle Squad", Side.Player, new Vector3(-10f, 0f, 0f), Isf);
            var tank = Spawn("Tyrant Hover-tank", Side.AI, new Vector3(5f, 0f, 0f), Draconis);
            var rats = Spawn("Rask Rifle Team", Side.AI, new Vector3(-10f, 0f, 15f), Rask);
            var rifleman = rifles.LivingSoldiers.First();
            Assert.That(Attack.MaxDamage(rifleman, rifleman.WeaponFor(null), tank.Leader), Is.EqualTo(0), "penetration 1 against Armor 5");
            Assert.That(UnitShooting.SoldiersAbleToShoot(rifles, tank), Is.EqualTo(0));
            Assert.That(UnitShooting.ArmorStopsEveryShot(rifles, tank), Is.True, "in range and sight, but armor stops every shot");
            Assert.That(UnitShooting.SoldiersAbleToShoot(rifles, rats), Is.GreaterThan(0), "lightly armored rats can be hurt");
            Assert.That(UnitShooting.ArmorStopsEveryShot(rifles, rats), Is.False);
        }

        [Test]
        public void AHeavyWeaponCanStillShootTheTank()
        {
            var tank = Spawn("Sagittarius MBT", Side.Player, new Vector3(-15f, 0f, 0f), Isf);
            var target = Spawn("Tyrant Hover-tank", Side.AI, new Vector3(15f, 0f, 0f), Draconis);
            Assert.That(Attack.CanDamage(tank.Leader, tank.Leader.WeaponFor(null), target.Leader), Is.True, "the 100mm Cannon gets through");
            Assert.That(UnitShooting.SoldiersAbleToShoot(tank, target), Is.EqualTo(1));
        }

        // ------------------------------------------------------------------ Grenades and mortars

        [TestCase("Sagittarius MBT")]
        [TestCase("Marshal Command Tank")]
        public void TheTanksFragGrenadeIsThrownUpTo50m(string name)
        {
            var grenade = UnitNamed(name).leader.abilities.Single(a => a.displayName == "Frag Grenade");
            Assert.That(grenade.range, Is.EqualTo(50f));
            Assert.That(grenade.areaRadius, Is.EqualTo(2.5f));
            Assert.That(grenade.description, Does.Contain("50 m"));
        }

        [Test]
        public void TheRiflemensFragGrenadeIsStillThrown10m()
        {
            var squad = UnitNamed("ISF Rifle Squad").leader;
            var grenade = squad.equipment.Single(e => e.displayName == "Frag Grenade").abilities.Single();
            Assert.That(grenade.range, Is.EqualTo(10f));
        }

        [Test]
        public void TheMortarTeamCallsItsGrenadeTheAntiInfantryMortar()
        {
            var team = UnitNamed("Mortar Team");
            var rifleman = team.Roster().Single(s => s != team.leader);
            var item = rifleman.equipment.Single();
            Assert.That(item.displayName, Is.EqualTo("Anti-infantry Mortar"));
            var ability = item.abilities.Single();
            Assert.That(ability.displayName, Is.EqualTo("Anti-infantry Mortar"));
            Assert.That((ability.range, ability.areaRadius), Is.EqualTo((50f, 2.5f)), "the same long-range Frag Grenade");
            Assert.That(team.description, Does.Contain("Anti-infantry Mortar"));
        }

        // ------------------------------------------------------------------ Rask Rifleman, HUD

        [Test]
        public void ARaskRiflemanMoves20mAndSprints40m()
        {
            var team = Spawn("Rask Rifle Team", Side.AI, Vector3.zero, Rask);
            foreach (var s in team.LivingSoldiers)
            {
                Assert.That(s.Move, Is.EqualTo(20f), s.name);
                Assert.That(s.CanSprint, Is.True);
                Assert.That(s.Sprint, Is.EqualTo(40f), s.name);
            }
        }

        [Test]
        public void TheHudIsDrawnOverTheSoldierLabels()
        {
            // Lower GUI depth is drawn on top (and gets clicks first).
            Assert.That(MovementController.HudDepth, Is.LessThan(SoldierLabels.LabelDepth));
        }
    }
}
