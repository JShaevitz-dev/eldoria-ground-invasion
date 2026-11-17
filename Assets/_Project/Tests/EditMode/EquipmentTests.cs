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
    /// The game's equipment (Data/Equipment): the Combat Shield (+1 Armor Level) and the Frag Grenade (gives the active
    /// ability Frag Grenade). No soldier type offers them yet; a soldier given one in a loadout gets its effect.
    /// </summary>
    public class EquipmentTests
    {
        private static Equipment Shield => AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/CombatShield.asset");
        private static Equipment Grenade => AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/FragGrenade.asset");
        private static Equipment RepairKitItem => AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/RepairKit.asset");
        private static Equipment MedkitItem => AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/Medkit.asset");
        private static Ability GrenadeAbility => AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/FragGrenade.asset");

        [Test]
        public void TheCombatShieldAdds1ToArmorAndNothingElse()
        {
            var e = Shield;
            Assert.That(e, Is.Not.Null);
            Assert.That(e.displayName, Is.EqualTo("Combat Shield"));
            Assert.That((e.armorLevel, e.health, e.accuracy, e.movement), Is.EqualTo((1, 0, 0, 0f)));
            Assert.That(e.keywords.Count + e.abilities.Count + e.weapons.Count, Is.EqualTo(0));
        }

        [Test]
        public void TheFragGrenadeGivesTheFragGrenadeAbility()
        {
            var e = Grenade;
            Assert.That(e, Is.Not.Null);
            Assert.That(e.displayName, Is.EqualTo("Frag Grenade"));
            Assert.That(e.abilities, Is.EqualTo(new[] { GrenadeAbility }));
            Assert.That(GrenadeAbility.kind, Is.EqualTo(AbilityKind.Active));
            Assert.That((e.armorLevel, e.health, e.accuracy, e.movement), Is.EqualTo((0, 0, 0, 0f)));
            Assert.That(e.keywords.Count + e.weapons.Count, Is.EqualTo(0));
        }

        [Test]
        public void NoSoldierTypeOffersThemYet()
        {
            var soldiers = AssetDatabase.FindAssets("t:SoldierTemplate", new[] { "Assets/_Project/Data/Soldiers" })
                .Select(g => AssetDatabase.LoadAssetAtPath<SoldierTemplate>(AssetDatabase.GUIDToAssetPath(g)));
            Assert.That(soldiers.Any(s => s.equipmentOptions.Contains(Shield) || s.equipmentOptions.Contains(Grenade) || s.equipmentOptions.Contains(MedkitItem) || s.equipmentOptions.Contains(RepairKitItem)), Is.False);
            Assert.That(soldiers.Where(s => s.equipment.Contains(MedkitItem)).Select(s => s.displayName), Is.EquivalentTo(new[] { "Rask Redhand", "ISF Combat Medic" }), "the Redhand and the Combat Medic carry Medkits");
            Assert.That(soldiers.Where(s => s.equipment.Contains(RepairKitItem)).Select(s => s.displayName), Is.EquivalentTo(new[] { "Kobold Mechanist", "Rask Blackhand", "ISF Combat Engineer" }), "the three engineers carry Repair Kits");
        }

        [Test]
        public void ASoldierGivenThemHas1MoreArmorAndTheFragGrenade()
        {
            var squad = TestArmies.Sniper;
            var trooper = squad.leader;
            Assert.That(trooper.abilities.Contains(GrenadeAbility), Is.False, "a soldier type without the grenade");
            var loadout = new UnitLoadout();
            loadout.For(trooper).Equipment.Add(Shield);
            loadout.For(trooper).Equipment.Add(Grenade);
            var unit = ArmyDeployment.Spawn(squad, Side.Player, Vector3.zero, Quaternion.identity, "Test Sniper", null, null, loadout);
            try
            {
                var s = unit.Soldiers.First(x => x.Template == trooper);
                Assert.That(s.ArmorLevel, Is.EqualTo(trooper.armorLevel + 1));
                Assert.That(s.Abilities, Does.Contain(GrenadeAbility));
                Assert.That(loadout.Describe(), Does.Contain("Combat Shield").And.Contain("Frag Grenade"));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }
    }
}
