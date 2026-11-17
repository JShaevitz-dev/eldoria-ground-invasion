using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The ISF's second Commander, Master Sergeant Jakoby: the Field Commander's stats without Lead from the front, a
    /// Shotgun and two Frag Grenades (standard equipment), which give the Frag Grenade ability (no limit per battle).
    /// </summary>
    public class MasterSergeantJakobyTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.First();
        private static UnitTemplate Jakoby => Catalog.Available.Single(u => u.displayName == "Master Sergeant Jakoby");
        private static SoldierTemplate FieldCommander => Catalog.Available.Single(u => u.displayName == "Field Commander").leader;
        private static Equipment GrenadeItem => AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/FragGrenade.asset");
        private static Ability Grenade => AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/FragGrenade.asset");

        [Test]
        public void JakobyIsAnIsfCommander()
        {
            var unit = Jakoby;
            Assert.That(Isf.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Skip(1).Any(f => f.Has(unit)), Is.False, "only the ISF's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Commander));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(ArmyRules.IsValid(new List<UnitTemplate> { unit }, Isf), Is.True);
            var both = new List<UnitTemplate> { unit, Catalog.Available.Single(u => u.displayName == "Field Commander") };
            Assert.That(ArmyRules.IsValid(both, Isf), Is.False, "only 1 Commander in an army");
        }

        [Test]
        public void HeHasTheFieldCommandersStatsButNotLeadFromTheFront()
        {
            var j = Jakoby.leader;
            var f = FieldCommander;
            Assert.That(j.displayName, Is.EqualTo("Master Sergeant Jakoby"));
            Assert.That((j.health, j.armorLevel, j.accuracy, j.movementRange, j.radius, j.height), Is.EqualTo((f.health, f.armorLevel, f.accuracy, f.movementRange, f.radius, f.height)));
            Assert.That(j.keywords.Where(k => k.displayName != "Unique"), Is.EqualTo(f.keywords), "the Field Commander's keywords, plus Unique");
            Assert.That(j.keywords.Select(k => k.displayName), Does.Contain("Unique"));
            Assert.That(f.abilities.Select(a => a.displayName), Is.EqualTo(new[] { "Lead from the front" }), "the Field Commander keeps it");
            Assert.That(j.abilities, Is.Empty, "Jakoby doesn't have it");
            Assert.That(j.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Shotgun" }));
            Assert.That(j.equipment, Is.EqualTo(new[] { GrenadeItem, GrenadeItem }), "two Frag Grenades");
        }

        [Test]
        public void HeWearsABeret()
        {
            var prefab = Jakoby.leader.prefab;
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab, Is.Not.SameAs(FieldCommander.prefab));
            Assert.That(prefab.transform.Find("Beret"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Cap"), Is.Null, "the beret replaces the cap");
            Assert.That(prefab.transform.Find("Shotgun"), Is.Not.Null);
        }

        [Test]
        public void HisFragGrenadesHaveNoLimitPerBattle()
        {
            Assert.That(UnitInfo.AbilityLines(Jakoby), Is.EqualTo(new[] { "Frag Grenade (active)" }));
            var unit = ArmyDeployment.Spawn(Jakoby, Side.Player, Vector3.zero, Quaternion.identity, "Jakoby", null, Isf);
            try
            {
                var j = unit.Leader;
                Assert.That(j.Abilities, Is.EqualTo(new[] { Grenade }), "the ability once");
                Assert.That(j.AbilitySources(Grenade), Is.EqualTo(2));
                for (int i = 0; i < 3; i++) unit.RecordBattleUse(Grenade);
                Assert.That(unit.UsesLeftThisBattle(Grenade), Is.EqualTo(int.MaxValue), "no limit per battle");
                Assert.That(ActiveAbilities.WhyCannotCast(j, Grenade), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        [Test]
        public void AnOrdinaryGrenadeCarrierHasNoLimitEither()
        {
            var squad = Catalog.Available.Single(u => u.displayName == "ISF Rifle Squad");
            Assert.That(UnitInfo.AbilityLines(squad), Is.EqualTo(new[] { "Frag Grenade (active)" }));
            var unit = ArmyDeployment.Spawn(squad, Side.Player, Vector3.zero, Quaternion.identity, "Rifles", null, Isf);
            try
            {
                Assert.That(unit.UsesLeftThisBattle(Grenade), Is.EqualTo(int.MaxValue), "no limit per battle");
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
