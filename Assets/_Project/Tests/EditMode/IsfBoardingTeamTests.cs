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
    /// The ISF Boarding Team (Specialist): 1 Boarding Sergeant + 4 Boarding Specialists with the ISF Rifleman's stats, each
    /// with a Shotgun and a Combat Shield carried as standard equipment (+1 Armor). Also covers standard equipment in
    /// general: a soldier type can carry equipment without any army builder choice.
    /// </summary>
    public class IsfBoardingTeamTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Isf => Catalog.Factions.First();
        private static UnitTemplate Team => Catalog.Available.Single(u => u.displayName == "ISF Boarding Team");
        private static SoldierTemplate Rifleman => Catalog.Available.Single(u => u.displayName == "ISF Rifle Squad").members.Single().soldier;
        private static Equipment Shield => AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/CombatShield.asset");

        [Test]
        public void TheBoardingTeamIsAnIsfSpecialistUnitOfFive()
        {
            var unit = Team;
            Assert.That(Isf.Has(unit), Is.True);
            Assert.That(Catalog.Factions.Skip(1).Any(f => f.Has(unit)), Is.False, "only the ISF's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(5));
            Assert.That(ArmyRules.Composition(unit), Is.EqualTo("Boarding Sergeant + 4\u00d7 Boarding Specialist"));
            Assert.That(unit.description, Does.Contain("Shotgun").And.Contain("Combat Shield"));
        }

        [Test]
        public void TheyHaveTheIsfRiflemansStatsAShotgunAndACombatShield()
        {
            var r = Rifleman;
            Assert.That(r.displayName, Is.EqualTo("ISF Rifleman"));
            foreach (var s in new[] { Team.leader, Team.members.Single().soldier })
            {
                Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange, s.radius, s.height), Is.EqualTo((r.health, r.armorLevel, r.accuracy, r.movementRange, r.radius, r.height)), s.displayName);
                Assert.That(s.keywords, Is.EqualTo(r.keywords), s.displayName);
                Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Shotgun" }), s.displayName);
                Assert.That(s.equipment, Is.EqualTo(new[] { Shield }), "carried as standard");
                Assert.That(s.equipmentOptions, Is.Empty, "nothing to choose in the army builder");
                Assert.That(s.HasOptions, Is.False);
                Assert.That(s.prefab, Is.Not.Null);
                Assert.That(s.prefab.transform.Find("Shield"), Is.Not.Null, "a shield on the arm");
                Assert.That(s.prefab.transform.Find("Weapon"), Is.Null, "the rifle is gone");
            }
            Assert.That(Team.leader.displayName, Is.EqualTo("Boarding Sergeant"));
            Assert.That(Team.members.Single().soldier.displayName, Is.EqualTo("Boarding Specialist"));
            Assert.That(r.equipment, Is.Empty, "the ISF Rifleman carries no shield");
        }

        [Test]
        public void InBattleTheShieldGivesThemArmor3()
        {
            var unit = ArmyDeployment.Spawn(Team, Side.Player, Vector3.zero, Quaternion.identity, "Boarding Team", null, Isf);
            try
            {
                Assert.That(unit.Soldiers.Count, Is.EqualTo(5));
                foreach (var s in unit.Soldiers)
                {
                    Assert.That(s.ArmorLevel, Is.EqualTo(3), "Armor 2 + the Combat Shield's 1, with no army builder choice");
                    Assert.That((s.MaxHealth, s.Accuracy, s.Move), Is.EqualTo((5, 75, 10f)));
                    Assert.That(s.Weapons.Select(w => w.Name), Is.EqualTo(new[] { "Shotgun" }));
                }
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
            }
        }

        [Test]
        public void TheUnitInfoCountsTheShield()
        {
            Assert.That(UnitInfo.SoldierLines(Team).First(),
                Is.EqualTo("Boarding Sergeant \u2014 Health 5 \u00b7 Armor 3 \u00b7 Accuracy 75% \u00b7 Move 10 m (sprint 20 m) \u00b7 Shotgun, Combat Shield"));
        }

        [Test]
        public void StandardEquipmentGivesAbilitiesAndKeywordsWithoutALoadout()
        {
            var grenade = AssetDatabase.LoadAssetAtPath<Equipment>("Assets/_Project/Data/Equipment/FragGrenade.asset");
            var template = ScriptableObject.CreateInstance<SoldierTemplate>();
            var go = new GameObject("Test Soldier");
            try
            {
                template.displayName = "Test Grenadier";
                template.health = 5;
                template.equipment.Add(grenade);
                template.equipment.Add(Shield);
                var soldier = go.AddComponent<Soldier>();
                soldier.Configure(template, false);
                Assert.That(soldier.Abilities, Is.EqualTo(grenade.abilities), "the Frag Grenade ability, from standard equipment");
                Assert.That(soldier.ArmorLevel, Is.EqualTo(1));
                // An army builder choice of the same item doesn't count twice.
                var loadout = new SoldierLoadout();
                loadout.Equipment.Add(Shield);
                soldier.ApplyLoadout(loadout);
                Assert.That(soldier.ArmorLevel, Is.EqualTo(1));
                Assert.That(soldier.Abilities, Is.EqualTo(grenade.abilities));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(template);
            }
        }
    }
}
