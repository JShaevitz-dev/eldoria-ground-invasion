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
    /// The Draconis Imperialis's Dragon Hierarch (Commander): one Dragon Hierarch (Health 15, Armor 2, Accuracy 50%, Move
    /// 30 m; Biological, Large) with a Dragon Claw (Damage 5, Shots 1, Penetration 1, Range 2 m; Melee, Natural).
    /// </summary>
    public class DragonHierarchTests
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
        private static UnitTemplate Hierarch => Catalog.Available.Single(u => u.displayName == "Dragon Hierarch");

        private Unit Spawn(UnitTemplate template, Side side, Vector3 at)
        {
            var unit = UnitFactory.Spawn(template, side, at, side == Side.Player ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f));
            created.Add(unit);
            return unit;
        }

        [Test]
        public void TheDragonHierarchIsTheDraconisCommander()
        {
            var unit = Hierarch;
            Assert.That(Draconis.displayName, Is.EqualTo("Imperial Army of The Draconis Imperialis"));
            Assert.That(Draconis.Has(unit), Is.True);
            Assert.That(Catalog.Factions.First().Has(unit), Is.False, "not the ISF's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Commander));
            Assert.That(unit.Size, Is.EqualTo(3));
            Assert.That(ArmyRules.Composition(unit), Is.EqualTo("Dragon Hierarch + 2\u00d7 Dragonborn Honorguard"));
            Assert.That(ArmyBuilderScreen.FactionCanFieldArmy(Catalog, Draconis), Is.True, "a Commander makes the faction playable");
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { unit }, Draconis), Is.True);
        }

        [Test]
        public void TheDragonHierarchIsAsDesigned()
        {
            var d = Hierarch.leader;
            Assert.That(d.displayName, Is.EqualTo("Dragon Hierarch"));
            Assert.That((d.health, d.armorLevel, d.accuracy, d.movementRange), Is.EqualTo((15, 2, 50, 30f)));
            Assert.That(d.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Large" }));
            Assert.That(d.abilities, Is.Empty);
            Assert.That(d.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Dragon Claw" }));
            Assert.That(d.PrimaryWeapon.displayName, Is.EqualTo("Dragon Claw"));
            Assert.That(d.prefab, Is.Not.Null, "the dragon model");
            Assert.That(d.radius, Is.EqualTo(1f), "wider than a person, but close enough in to reach with a 2 m claw");
            Assert.That(d.height, Is.GreaterThan(3f), "a large dragon");
            Assert.That(UnitInfo.SoldierLines(Hierarch).First(),
                Is.EqualTo("Dragon Hierarch — Health 15 · Armor 2 · Accuracy 50% · Move 30 m (sprint 60 m) · Dragon Claw"));
        }

        [Test]
        public void TheDragonClawIsAsDesigned()
        {
            var claw = Hierarch.leader.PrimaryWeapon;
            Assert.That((claw.baseDamage, claw.shots, claw.PenetrationValue, claw.accuracyBonus, claw.range),
                Is.EqualTo((5, 1, (int?)1, 0, 2f)));
            Assert.That(claw.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Melee", "Natural" }));
            Assert.That(UnitInfo.WeaponLines(Hierarch).First(),
                Is.EqualTo("Dragon Claw — Damage 5 · Shots 1 · Penetration 1 · Range 2 m · Melee, Natural"));
        }

        [Test]
        public void LargeAndNaturalAreTraitKeywords()
        {
            var large = KeywordLibrary.Get("Large");
            var natural = KeywordLibrary.Get("Natural");
            Assert.That(large, Is.Not.Null);
            Assert.That(natural, Is.Not.Null);
            Assert.That(large.category, Is.EqualTo(KeywordCategory.Trait));
            Assert.That(natural.category, Is.EqualTo(KeywordCategory.Trait));
            Assert.That(large.appliesTo, Is.EqualTo(KeywordTarget.Soldier));
            Assert.That(natural.appliesTo, Is.EqualTo(KeywordTarget.Weapon));
            Assert.That(natural.description, Does.Contain("Disarm"));
            Assert.That(large.interactions, Is.Empty, "no effect on its own yet");
            Assert.That(natural.interactions, Is.Empty, "no effect on its own yet");
        }

        [Test]
        public void TheClawReachesASoldierStandingNextToTheDragonButNotOneFurtherAway()
        {
            var dragon = Spawn(Hierarch, Side.AI, Vector3.zero).Leader;
            var isfSquad = Catalog.Available.Single(u => u.displayName == "ISF Rifle Squad");
            var squad = Spawn(isfSquad, Side.Player, new Vector3(0f, 0f, -20f));
            var near = squad.Soldiers[0];
            var far = squad.Soldiers[1];

            // Right up against the dragon's body (radius 1 m + 0.35 m): the closest a soldier can stand.
            near.transform.position = new Vector3(0f, 0f, -1.35f);
            far.transform.position = new Vector3(3f, 0f, 0f);
            var positions = new Dictionary<Soldier, Vector3> { [dragon] = dragon.Position, [near] = near.Position };
            Assert.That(MovementRules.IsSpaceFree(near, near.Position, positions), Is.True, "a soldier can stand right next to the dragon");

            var claw = dragon.PrimaryWeapon;
            Assert.That(Attack.InRange(dragon, claw, near), Is.True, "1.35 m: within the claw's 2 m");
            Assert.That(Attack.InRange(dragon, claw, far), Is.False, "3 m: out of reach");
            Assert.That(CloseRange.CanUseWeapon(dragon, claw), Is.True, "a Melee weapon works in close range");
        }

        [Test]
        public void ItsArmor2HalvesRifleFireAndStopsPistolsAndSmgs()
        {
            int armor = Hierarch.leader.armorLevel;
            Assert.That(armor, Is.EqualTo(2));
            Assert.That(TestArmies.Weapon("AssaultRifle").PenetrationValue, Is.EqualTo(armor - 1), "1 lower: half damage");
            Assert.That(TestArmies.Weapon("MachineGun").PenetrationValue, Is.EqualTo(armor - 1), "1 lower: half damage");
            Assert.That(TestArmies.Weapon("Pistol").PenetrationValue, Is.EqualTo(armor - 2), "2 lower: no damage");
            Assert.That(TestArmies.Weapon("SMG").PenetrationValue, Is.EqualTo(armor - 2), "2 lower: no damage");
            Assert.That(TestArmies.Weapon("RocketLauncher").PenetrationValue, Is.GreaterThan(armor), "higher: 150%");
        }
    }
}
