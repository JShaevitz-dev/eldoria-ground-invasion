using System.Linq;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Mortar Launcher: the Frag Grenade weapon's stats (Damage 5, Shots 1, +0%, Explosive), but Penetration 3, Range
    /// 50 m, and Deployed and Manual Load too. Carried by the ISF Mortar Team's Mortar Specialist.
    /// </summary>
    public class MortarLauncherTests
    {
        private static WeaponTemplate Load(string file) =>
            AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"Assets/_Project/Data/Weapons/{file}.asset");

        [Test]
        public void ItIsTheFragGrenadeWithPenetration3Range50DeployedManualLoadAndExplosive()
        {
            var mortar = Load("MortarLauncher");
            var grenade = Load("FragGrenade");
            Assert.That(mortar, Is.Not.Null);
            Assert.That(mortar.displayName, Is.EqualTo("Mortar Launcher"));
            Assert.That((mortar.baseDamage, mortar.shots, mortar.accuracyBonus), Is.EqualTo((grenade.baseDamage, grenade.shots, grenade.accuracyBonus)),
                "the Frag Grenade's damage, shots and accuracy bonus");
            Assert.That((mortar.baseDamage, mortar.shots, mortar.accuracyBonus), Is.EqualTo((5, 1, 0)));
            Assert.That(mortar.PenetrationValue, Is.EqualTo(3));
            Assert.That(mortar.range, Is.EqualTo(50f));
            Assert.That(mortar.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Deployed", "Manual Load", "Explosive" }));
            Assert.That(grenade.HasKeyword("Explosive"), Is.True, "Explosive, like the Frag Grenade");
        }

        [Test]
        public void Penetration3AgainstArmor()
        {
            var mortar = Load("MortarLauncher");
            int Damage(int armor) => ArmorPenetration.Resolve(mortar.baseDamage, mortar.PenetrationValue, armor).Damage;
            Assert.That(Damage(1), Is.EqualTo(7), "higher: 150%, rounded down");
            Assert.That(Damage(2), Is.EqualTo(7), "Armor 2 (ISF Riflemen, Dragonborn, Dragon Hierarch): 150%");
            Assert.That(Damage(3), Is.EqualTo(5), "equal: 100%");
            Assert.That(Damage(4), Is.EqualTo(2), "1 lower: 50%");
            Assert.That(Damage(5), Is.EqualTo(0), "2 or more lower: nothing");
        }

        [Test]
        public void OnlyTheIsfMortarSpecialistCarriesIt()
        {
            var mortar = Load("MortarLauncher");
            var carriers = UnitCatalog.Load().Available.SelectMany(u => u.Roster()).Distinct()
                .Where(s => s.weapons.Contains(mortar) || s.weaponOptions.Any(o => o.alternatives.Contains(mortar)))
                .Select(s => s.displayName);
            Assert.That(carriers, Is.EqualTo(new[] { "ISF Mortar Specialist" }));
        }
    }
}
