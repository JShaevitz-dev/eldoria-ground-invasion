using System.Collections.Generic;
using System.Linq;
using Eldoria.AI;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// Tank Hunter Team (Heavy Support, every faction): Sergeant (Assault Rifle, Pistol), 3 Troopers (Assault Rifle),
    /// Rocket Trooper (Assault Rifle + the Rocket Launcher as an active ability: an aimed shot at one enemy soldier).
    /// </summary>
    public class TankHunterTeamTests
    {
        private const string Data = "Assets/_Project/Data";
        private readonly List<Object> created = new List<Object>();

        private static UnitTemplate U(string file) => TestArmies.ByFile(file);   // test-made stand-ins
        private static Ability Rocket() => AssetDatabase.LoadAssetAtPath<Ability>($"{Data}/Abilities/RocketLauncher.asset");

        [SetUp]
        public void SetUp()
        {
            Turns.ResetCounter();
            Turns.StartNextTurn();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o is Component c ? c.gameObject : o);
            created.Clear();
            Turns.ResetCounter();
        }

        private Unit Spawn(string file, Side side, Vector3 at)
        {
            var unit = UnitFactory.Spawn(U(file), side, at, side == Side.Player ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f));
            created.Add(unit);
            return unit;
        }

        private static Soldier RocketTrooper(Unit unit) => unit.Soldiers.Single(s => s.Template.displayName == "Rocket Trooper");

        [Test]
        public void TheRocketLauncherAbilityIsAnAimedShotWithTheRocketLauncher()
        {
            var a = Rocket();
            Assert.That((a.IsActive, a.targets, a.affects, a.usesPerTurn, a.usesPerBattle), Is.EqualTo((true, AbilityTarget.Soldier, AuraAffects.Enemies, 1, 0)));
            Assert.That(a.IsAimedAttack, Is.True);
            Assert.That(a.attackWeapon.displayName, Is.EqualTo("Rocket Launcher"));
            Assert.That(a.range, Is.EqualTo(50f));
        }

        [Test]
        public void ARocketHitsATankForSevenDamage()
        {
            var us = Spawn("TankHunterTeam", Side.Player, Vector3.zero);
            var tank = Spawn("MediumTank", Side.AI, new Vector3(0f, 0f, 30f)).Leader;
            var user = RocketTrooper(us);

            var r = ActiveAbilities.Use(user, Rocket(), targetSoldier: tank);
            Assert.That(r.Used, Is.True, r.Reason);
            Assert.That(r.Target, Is.SameAs(tank));
            var shot = r.Attacks.Single();
            Assert.That(shot.Accuracy.Final, Is.EqualTo(75), "a hit roll at the Rocket Trooper's 75%");
            Assert.That(tank.Health, Is.EqualTo(shot.Hit ? 20 - 7 : 20), "penetration 5 vs armor 4: 150% of 5");
        }

        [Test]
        public void TheRocketOnlyTargetsEnemiesInRangeAndInSight()
        {
            var us = Spawn("TankHunterTeam", Side.Player, Vector3.zero);
            var user = RocketTrooper(us);
            var friend = us.Leader;
            var far = Spawn("MediumTank", Side.AI, new Vector3(0f, 0f, 60f)).Leader;
            Assert.That(ActiveAbilities.WhyCannotUse(user, Rocket(), friend), Does.Contain("isn't a valid target"));
            Assert.That(ActiveAbilities.WhyCannotUse(user, Rocket(), far), Does.Contain("Out of range (50 m)"));
        }

        [Test]
        public void ManualLoadOnceATurn()
        {
            var us = Spawn("TankHunterTeam", Side.Player, Vector3.zero);
            var tank = Spawn("MediumTank", Side.AI, new Vector3(0f, 0f, 30f)).Leader;
            var user = RocketTrooper(us);
            Assert.That(ActiveAbilities.Use(user, Rocket(), targetSoldier: tank).Used, Is.True);
            Assert.That(WeaponUse.WhyCannotUse(user, user.AbilityWeapon(Rocket().attackWeapon)), Does.Contain("Manual Load"));
            Assert.That(ActiveAbilities.Use(user, Rocket(), targetSoldier: tank).Used, Is.False, "not twice in a turn");
        }

        [Test]
        public void TheAISavesItsRocketForArmorAndShootsInfantry()
        {
            var us = Spawn("TankHunterTeam", Side.AI, new Vector3(0f, 0f, 14f));   // within rifle range (20 m) of the infantry
            var infantry = Spawn("InfantrySquad", Side.Player, new Vector3(10f, 0f, 0f));
            var vsInfantry = AIPlanner.ChooseAction(us);
            Assert.That(vsInfantry.Kind, Is.EqualTo(AIActionKind.Shoot), "rifles for infantry");

            var tank = Spawn("MediumTank", Side.Player, new Vector3(-10f, 0f, 0f)).Leader;
            var vsTank = AIPlanner.ChooseAction(us);
            Assert.That((vsTank.Kind, vsTank.Ability, vsTank.TargetSoldier), Is.EqualTo((AIActionKind.Ability, Rocket(), tank)), "the rocket for the tank");
        }
    }
}
