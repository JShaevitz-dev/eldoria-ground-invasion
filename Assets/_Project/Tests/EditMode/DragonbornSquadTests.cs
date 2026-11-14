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
    /// The Draconis Imperialis's Dragonborn Squad (Infantry): 1 Dragonborn Sergeant + 4 Dragonborn Footmen, all Health 10,
    /// Armor 2, Accuracy 75%, Move 10 m, Biological, each with a Dragon Glaive (Range) (Damage 5, Shots 1, Penetration 1,
    /// 20 m) and a Dragon Glaive (melee) (Damage 5, Shots 1, Penetration 2, +25%, 2 m, Melee).
    /// </summary>
    public class DragonbornSquadTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o is Component c ? c.gameObject : o);
            created.Clear();
        }

        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static UnitTemplate Squad => Catalog.Available.Single(u => u.displayName == "Dragonborn Squad");

        private Unit Spawn(UnitTemplate template, Side side, Vector3 at)
        {
            var unit = UnitFactory.Spawn(template, side, at, side == Side.Player ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f));
            created.Add(unit);
            return unit;
        }

        [Test]
        public void TheDragonbornSquadIsADraconisInfantryUnitOfASergeantAndFourFootmen()
        {
            var squad = Squad;
            Assert.That(Draconis.Has(squad), Is.True);
            Assert.That(Catalog.Factions.First().Has(squad), Is.False, "not the ISF's");
            Assert.That(squad.role, Is.EqualTo(UnitRole.Infantry));
            Assert.That(squad.Size, Is.EqualTo(5));
            Assert.That(ArmyRules.Composition(squad), Is.EqualTo("Dragonborn Sergeant + 4× Dragonborn Footman"));
            var hierarch = Catalog.Available.Single(u => u.displayName == "Dragon Hierarch");
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { hierarch, squad, squad, squad }, Draconis), Is.True, "Infantry can be taken more than once");
        }

        [Test]
        public void SergeantAndFootmenShareTheirStatsAndWeapons()
        {
            var sergeant = Squad.leader;
            var footman = Squad.members.Single().soldier;
            Assert.That(sergeant.displayName, Is.EqualTo("Dragonborn Sergeant"));
            Assert.That(footman.displayName, Is.EqualTo("Dragonborn Footman"));
            foreach (var s in new[] { sergeant, footman })
            {
                Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((10, 2, 75, 10f)), s.displayName);
                Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological" }), s.displayName);
                Assert.That(s.abilities, Is.Empty, s.displayName);
                Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Dragon Glaive (Range)", "Dragon Glaive (melee)" }), s.displayName);
                Assert.That(s.PrimaryWeapon.displayName, Is.EqualTo("Dragon Glaive (Range)"), $"{s.displayName} shoots with it normally");
                Assert.That(s.prefab, Is.Not.Null, s.displayName);
                Assert.That(s.height, Is.InRange(1.9f, 2.5f), "slightly taller than a human");
            }
            Assert.That(sergeant.prefab, Is.Not.SameAs(footman.prefab), "the Sergeant has its own model (gold crest)");
            Assert.That(UnitInfo.SoldierLines(Squad), Is.EqualTo(new[]
            {
                "Dragonborn Sergeant — Health 10 · Armor 2 · Accuracy 75% · Move 10 m (sprint 20 m) · Dragon Glaive (Range), Dragon Glaive (melee)",
                "4× Dragonborn Footman — Health 10 · Armor 2 · Accuracy 75% · Move 10 m (sprint 20 m) · Dragon Glaive (Range), Dragon Glaive (melee)",
            }));
        }

        [Test]
        public void TheTwoGlaiveAttacksAreAsDesigned()
        {
            var weapons = Squad.leader.weapons;
            var ranged = weapons[0];
            var melee = weapons[1];
            Assert.That((ranged.baseDamage, ranged.shots, ranged.PenetrationValue, ranged.accuracyBonus, ranged.range),
                Is.EqualTo((5, 1, (int?)1, 0, 20f)));
            Assert.That(ranged.keywords, Is.Empty);
            Assert.That((melee.baseDamage, melee.shots, melee.PenetrationValue, melee.accuracyBonus, melee.range),
                Is.EqualTo((5, 1, (int?)2, 25, 2f)));
            Assert.That(melee.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Melee" }));
            Assert.That(UnitInfo.WeaponLines(Squad), Is.EqualTo(new[]
            {
                "Dragon Glaive (Range) — Damage 5 · Shots 1 · Penetration 1 · Range 20 m",
                "Dragon Glaive (melee) — Damage 5 · Shots 1 · Penetration 2 · +25% accuracy · Range 2 m · Melee",
            }));
        }

        [Test]
        public void TheyShootWithTheGlaivesRifleAndSwitchToItsBladeInCloseRange()
        {
            var dragonborn = Spawn(Squad, Side.AI, Vector3.zero);
            var footman = dragonborn.Soldiers.First(s => s.Template.displayName == "Dragonborn Footman");
            var isf = Spawn(Catalog.Available.Single(u => u.displayName == "ISF Rifle Squad"), Side.Player, new Vector3(0f, 0f, -15f));
            var target = isf.Soldiers[0];

            Assert.That(CloseRange.IsInCloseRange(footman), Is.False);
            Assert.That(footman.WeaponFor(target).Name, Is.EqualTo("Dragon Glaive (Range)"), "at a distance: the rifle");

            // An ISF Rifleman steps up next to the footman: close range (both Biological, under 2 m apart).
            target.transform.position = footman.Position + new Vector3(0f, 0f, -1.2f);
            Assert.That(CloseRange.IsInCloseRange(footman), Is.True);
            var weapon = footman.WeaponFor(target);
            Assert.That(weapon.Name, Is.EqualTo("Dragon Glaive (melee)"), "in close range: the blade (Melee)");
            Assert.That(Attack.InRange(footman, weapon, target), Is.True, "1.2 m: within the blade's 2 m");
        }
    }
}
