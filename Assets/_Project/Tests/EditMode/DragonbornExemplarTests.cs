using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Draconis Imperialis's Dragonborn Exemplar (Commander): the Dragonborn Footman's stats with double the health,
    /// Solo and Officer; a Raptor Pistol and a Ceremonial Blade (the Draconis name for the Broadsword); and Empowering
    /// Presence (other friendly soldiers within 15 m get +2 damage).
    /// </summary>
    public class DragonbornExemplarTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static UnitTemplate Exemplar => Catalog.Available.Single(u => u.displayName == "Dragonborn Exemplar");
        private static UnitTemplate Unit(string name) => Catalog.Available.Single(u => u.displayName == name);
        private static WeaponTemplate Weapon(string file) => AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"Assets/_Project/Data/Weapons/{file}.asset");

        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
            NearbyKeywords.RefreshAll();
        }

        private Unit Spawn(string name, Side side, Vector3 at, Faction faction)
        {
            var unit = ArmyDeployment.Spawn(Unit(name), side, at, Quaternion.identity, name, null, faction);
            spawned.Add(unit.gameObject);
            unit.Leader.transform.position = at;
            return unit;
        }

        [Test]
        public void TheExemplarIsADraconisCommander()
        {
            var unit = Exemplar;
            Assert.That(Draconis.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Where((f, i) => i != 1).Any(f => f.Has(unit)), Is.False, "only the Draconis's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Commander));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { unit }, Draconis), Is.True);
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { unit, Unit("Dragon Hierarch") }, Draconis), Is.False, "only 1 Commander");
        }

        [Test]
        public void ItHasTheFootmansStatsWithDoubleHealthSoloAndOfficer()
        {
            var e = Exemplar.leader;
            var f = Unit("Dragonborn Squad").members.Single().soldier;
            Assert.That(e.displayName, Is.EqualTo("Dragonborn Exemplar"));
            Assert.That(e.health, Is.EqualTo(f.health * 2));
            Assert.That((e.health, e.armorLevel, e.accuracy, e.movementRange), Is.EqualTo((20, 2, 75, 10f)));
            Assert.That((e.armorLevel, e.accuracy, e.movementRange, e.radius, e.height), Is.EqualTo((f.armorLevel, f.accuracy, f.movementRange, f.radius, f.height)));
            Assert.That(e.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Solo", "Officer" }));
            Assert.That(e.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Empowering Presence" }));
            Assert.That(e.weapons, Is.EqualTo(new[] { Weapon("RaptorPistol"), Weapon("Broadsword") }));
            Assert.That(e.PrimaryWeapon, Is.SameAs(Weapon("RaptorPistol")));
            Assert.That(e.prefab, Is.Not.Null);
            Assert.That(e.prefab.transform.Find("Blade"), Is.Not.Null);
        }

        [Test]
        public void TheRaptorPistolIsTheTwinRaptorPistolsWithOneShot()
        {
            var p = Weapon("RaptorPistol");
            var twin = Weapon("TwinRaptorPistols");
            Assert.That(p.displayName, Is.EqualTo("Raptor Pistol"));
            Assert.That((p.baseDamage, p.PenetrationValue, p.accuracyBonus, p.range), Is.EqualTo((twin.baseDamage, twin.PenetrationValue, twin.accuracyBonus, twin.range)));
            Assert.That((p.shots, twin.shots), Is.EqualTo((1, 2)));
            Assert.That(p.keywords.Select(k => k.displayName), Is.EqualTo(twin.keywords.Select(k => k.displayName)), "Pistol");
        }

        [Test]
        public void TheDraconisCallTheBroadswordTheCeremonialBlade()
        {
            var sword = Weapon("Broadsword");
            Assert.That(WeaponStats.For(sword, Draconis).Name, Is.EqualTo("Ceremonial Blade"));
            Assert.That(WeaponStats.For(sword, Catalog.Factions.First()).Name, Is.EqualTo("Broadsword"), "only the Draconis's name");
            Assert.That(WeaponStats.For(sword, Draconis).BaseDamage, Is.EqualTo(5), "the same stats");
            Assert.That(WeaponStats.For(Weapon("AssaultRifle"), Draconis).Name, Is.EqualTo("Fang Caster"), "still");
            Assert.That(UnitInfo.WeaponLines(Exemplar, Draconis).Select(l => l.Split(' ')[0] + " " + l.Split(' ')[1]), Is.EqualTo(new[] { "Raptor Pistol", "Ceremonial Blade" }));
        }

        [Test]
        public void EmpoweringPresenceHasLeadFromTheFrontsRangeAndGivesPlus2Damage()
        {
            var presence = Exemplar.leader.abilities.Single();
            var lead = AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/LeadFromTheFront.asset");
            Assert.That(presence.kind, Is.EqualTo(AbilityKind.Passive));
            Assert.That(presence.IsAura, Is.True);
            Assert.That((presence.radius, presence.affects, presence.includeSelf), Is.EqualTo((lead.radius, lead.affects, lead.includeSelf)), "the same range and targets as Lead from the front");
            Assert.That(presence.radius, Is.EqualTo(15f));
            var empowered = presence.grantsNearby;
            Assert.That(empowered.displayName, Is.EqualTo("Empowered"));
            Assert.That(empowered.category, Is.EqualTo(KeywordCategory.Status));
            var i = empowered.interactions.Single();
            Assert.That((i.role, i.effect, i.amount), Is.EqualTo((KeywordRole.WhenAttacking, KeywordEffect.DamageBonus, 2)));
        }

        [Test]
        public void FriendlySoldiersWithin15mDoPlus2Damage()
        {
            var exemplar = Spawn("Dragonborn Exemplar", Side.AI, Vector3.zero, Draconis).Leader;
            var near = Spawn("Kobold Militia", Side.AI, new Vector3(10f, 0f, 0f), Draconis).Leader;
            var far = Spawn("Dragonborn Squad", Side.AI, new Vector3(0f, 0f, 25f), Draconis).Leader;
            var enemy = Spawn("ISF Rifle Squad", Side.Player, new Vector3(-12f, 0f, 0f), Catalog.Factions.First()).Leader;
            NearbyKeywords.RefreshAll();

            Assert.That(near.HasKeyword("Empowered"), Is.True, "10 m away");
            Assert.That(far.HasKeyword("Empowered"), Is.False, "25 m away");
            Assert.That(enemy.HasKeyword("Empowered"), Is.False, "enemies aren't empowered");
            Assert.That(exemplar.HasKeyword("Empowered"), Is.False, "like Lead from the front, not the Exemplar itself");

            var rifle = near.WeaponFor(enemy);
            var outcome = KeywordRules.Evaluate(near, rifle, enemy);
            Assert.That(outcome.DamageBonus, Is.EqualTo(2));
            Assert.That(Attack.FinalDamage(rifle.BaseDamage, outcome), Is.EqualTo(rifle.BaseDamage + 2), "the Fang Caster does 6");
            Assert.That(KeywordRules.Evaluate(far, far.WeaponFor(enemy), enemy).DamageBonus, Is.EqualTo(0));
        }

        [Test]
        public void ItUsesTheBladeInCloseRangeAndThePistolOtherwise()
        {
            var exemplar = Spawn("Dragonborn Exemplar", Side.AI, Vector3.zero, Draconis).Leader;
            var enemy = Spawn("ISF Rifle Squad", Side.Player, new Vector3(0f, 0f, 12f), Catalog.Factions.First()).Leader;
            Assert.That(exemplar.WeaponFor(enemy).Name, Is.EqualTo("Raptor Pistol"), "12 m away");
            enemy.transform.position = new Vector3(0f, 0f, 1.2f);
            Assert.That(CloseRange.IsInCloseRange(exemplar), Is.True);
            Assert.That(exemplar.WeaponFor(enemy).Name, Is.EqualTo("Ceremonial Blade"), "in close range the blade, not the pistol");
        }
    }
}
