using System.Linq;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Anti-Materiel Rifle: the Sniper Rifle's stats, but Damage 10, no penetration value (X: full damage whatever the
    /// armor) and no accuracy bonus. Damage 10, Shots 1, Penetration X, +0%, Range 50 m; Deployed. The ISF Ranger can swap
    /// its Sniper Rifle for it.
    /// </summary>
    public class AntiMaterielRifleTests
    {
        private static WeaponTemplate Load(string file) =>
            AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"Assets/_Project/Data/Weapons/{file}.asset");

        [Test]
        public void ItIsTheSniperRifleWithDamage10PenetrationXAndNoAccuracyBonus()
        {
            var amr = Load("AntiMaterielRifle");
            var sniper = Load("SniperRifle");
            Assert.That(amr, Is.Not.Null);
            Assert.That(amr.displayName, Is.EqualTo("Anti-Materiel Rifle"));
            Assert.That((amr.baseDamage, amr.shots, amr.range), Is.EqualTo((sniper.baseDamage, sniper.shots, sniper.range)), "the Sniper Rifle's damage, shots and range");
            Assert.That((amr.baseDamage, amr.shots, amr.range), Is.EqualTo((10, 1, 50f)));
            Assert.That(amr.PenetrationValue, Is.Null, "Penetration X");
            Assert.That(amr.accuracyBonus, Is.EqualTo(0), "no accuracy bonus (the Sniper Rifle has +50%)");
            Assert.That(amr.keywords.Select(k => k.displayName), Is.EqualTo(sniper.keywords.Select(k => k.displayName)), "Deployed, like the Sniper Rifle");
        }

        [Test]
        public void ItDoesFullDamageWhateverTheArmor()
        {
            var amr = Load("AntiMaterielRifle");
            foreach (int armor in new[] { 0, 2, 4 })
            {
                var outcome = ArmorPenetration.Resolve(amr.baseDamage, amr.PenetrationValue, armor);
                Assert.That(outcome.Damage, Is.EqualTo(10), $"Armor {armor}: 100%");
                Assert.That(outcome.BonusEffectsApply, Is.True, $"Armor {armor}");
            }
            var sniper = Load("SniperRifle");
            Assert.That(ArmorPenetration.Resolve(sniper.baseDamage, sniper.PenetrationValue, 4).Damage, Is.EqualTo(0),
                "the Sniper Rifle (Penetration 1) does nothing to Armor 4");
        }

        [Test]
        public void OnlyTheIsfRangerCanTakeIt()
        {
            var amr = Load("AntiMaterielRifle");
            var carriers = UnitCatalog.Load().Available
                .SelectMany(u => u.Roster().Distinct())
                .Where(s => s.weapons.Contains(amr) || s.weaponOptions.Any(o => o.alternatives.Contains(amr)))
                .Select(s => s.displayName).Distinct();
            Assert.That(carriers, Is.EqualTo(new[] { "ISF Ranger" }));
        }
    }
}
