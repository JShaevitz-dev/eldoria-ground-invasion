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
    /// The Draconis Imperialis's Crown Princess X'tara (Commander): the Dragon Hierarch's stats with 100% accuracy and Unique;
    /// a Dragon Claw and a Flamethrower the Draconis call the Imperial Igniter (5 shots); Imperial Mandate (+1 penetration for
    /// friendly Biological soldiers within 15 m). A white dragon with red eyes, golden armor and a brace on the right foreleg.
    /// </summary>
    public class CrownPrincessXtaraTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.ElementAt(0);
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static UnitTemplate UnitNamed(string name) => Catalog.Available.Single(u => u.displayName == name);
        private static UnitTemplate Xtara => UnitNamed("Crown Princess X'tara");

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

        [Test]
        public void XtaraIsAUniqueDraconisCommanderOfOne()
        {
            var unit = Xtara;
            Assert.That(Draconis.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Where(f => f != Draconis).Any(f => f.Has(unit)), Is.False);
            Assert.That(unit.role, Is.EqualTo(UnitRole.Commander));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.StartWith("The war crazed 1st daughter of The Emperor of Draconis Imperialis who abdicated her claim on the throne to pursue a military career."));
            Assert.That(ArmyRules.HasUniqueKeyword(unit), Is.True);
        }

        [Test]
        public void SheHasTheHierarchsStatsWith100PercentAccuracy()
        {
            var x = Xtara.leader;
            var h = UnitNamed("Dragon Hierarch").leader;
            Assert.That((x.health, x.armorLevel, x.movementRange, x.radius, x.height), Is.EqualTo((h.health, h.armorLevel, h.movementRange, h.radius, h.height)));
            Assert.That(x.accuracy, Is.EqualTo(100));
            Assert.That(h.accuracy, Is.EqualTo(50), "the Hierarch is unchanged");
            Assert.That(x.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Large", "Unique" }));
            Assert.That(x.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Flamethrower", "Dragon Claw" }));
            Assert.That(x.PrimaryWeapon.displayName, Is.EqualTo("Flamethrower"));
            Assert.That(x.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Imperial Mandate" }));
        }

        [Test]
        public void TheDraconisCallTheFlamethrowerTheImperialIgniterAndFireItFiveTimes()
        {
            var flamer = Xtara.leader.PrimaryWeapon;
            var draconis = WeaponStats.For(flamer, Draconis);
            Assert.That(draconis.Name, Is.EqualTo("Imperial Igniter"));
            Assert.That(draconis.Shots, Is.EqualTo(5));
            Assert.That((draconis.BaseDamage, draconis.PenetrationValue, draconis.Range), Is.EqualTo((10, (int?)null, 10f)), "otherwise the Flamethrower");
            var isf = WeaponStats.For(flamer, Isf);
            Assert.That((isf.Name, isf.Shots), Is.EqualTo(("Flamethrower", 3)), "the ISF's is unchanged");
        }

        [Test]
        public void ImperialMandateGivesNearbyFriendlyBiologicalSoldiersOnePenetration()
        {
            var xtara = Spawn("Crown Princess X'tara", Side.AI, new Vector3(0f, 0f, 0f), Draconis).Leader;
            var squad = Spawn("Dragonborn Squad", Side.AI, new Vector3(-20f, 0f, 0f), Draconis);
            var dragonborn = squad.LivingSoldiers.First();
            var tank = Spawn("Tyrant Hover-tank", Side.AI, new Vector3(20f, 0f, 0f), Draconis).Leader;
            var enemy = Spawn("ISF Rifle Squad", Side.Player, new Vector3(0f, 0f, -18f), Isf).LivingSoldiers.First();

            dragonborn.transform.position = new Vector3(8f, 0f, 0f);
            tank.transform.position = new Vector3(-8f, 0f, 3f);
            foreach (var s in new[] { dragonborn, tank, xtara, enemy }) NearbyKeywords.Refresh(s, s.Position);
            Assert.That(dragonborn.HasKeyword("Under Imperial Mandate"), Is.True, "8 m away");
            Assert.That(tank.HasKeyword("Under Imperial Mandate"), Is.False, "not machines");
            Assert.That(xtara.HasKeyword("Under Imperial Mandate"), Is.False, "not X'tara herself");
            Assert.That(enemy.HasKeyword("Under Imperial Mandate"), Is.False, "not the enemy");

            var weapon = dragonborn.WeaponFor(null);
            var outcome = KeywordRules.Evaluate(dragonborn, weapon, enemy);
            Assert.That(outcome.PenetrationBonus, Is.EqualTo(1));
            Assert.That(Attack.PenetrationFor(weapon.PenetrationValue, outcome), Is.EqualTo(weapon.PenetrationValue + 1));
            Assert.That(Attack.PenetrationFor(null, outcome), Is.Null, "a weapon with no penetration value stays without one");

            dragonborn.transform.position = new Vector3(18f, 0f, 0f);
            NearbyKeywords.Refresh(dragonborn, dragonborn.Position);
            Assert.That(dragonborn.HasKeyword("Under Imperial Mandate"), Is.False, "18 m away");
        }

        [Test]
        public void ThePenetrationBonusTurnsAHalfDamageHitIntoAFullOne()
        {
            var keywords = new KeywordOutcome { PenetrationBonus = 1 };
            Assert.That(ArmorPenetration.Resolve(4, 1, 2).Damage, Is.EqualTo(2), "penetration 1 against Armor 2: half");
            Assert.That(ArmorPenetration.Resolve(4, Attack.PenetrationFor(1, keywords), 2).Damage, Is.EqualTo(4), "with the Mandate: full");
        }

        [Test]
        public void SheIsAWhiteDragonWithRedEyesAndABraceOnTheRightForeleg()
        {
            var prefab = Xtara.leader.prefab;
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab, Is.Not.SameAs(UnitNamed("Dragon Hierarch").leader.prefab));
            var parts = prefab.GetComponentsInChildren<Renderer>();
            Assert.That(parts.Single(r => r.name == "Body").sharedMaterial.name, Is.EqualTo("Dragon_White"), "white, not the faction's colour");
            Assert.That(parts.Where(r => r.name == "Eye").All(r => r.sharedMaterial.name == "Dragon_Red_Eye"), Is.True);
            var brace = parts.Where(r => r.name.StartsWith("Leg Brace")).ToList();
            Assert.That(brace, Is.Not.Empty);
            Assert.That(brace.All(r => r.transform.localPosition.x > 0f && r.transform.localPosition.z > 0f), Is.True, "the right foreleg");
            Assert.That(parts.Count(r => r.sharedMaterial.name == "Dragon_Gold"), Is.GreaterThan(5), "golden armor");
        }
    }
}
