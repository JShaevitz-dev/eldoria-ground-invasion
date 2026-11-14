using System.Linq;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;

namespace Eldoria.Tests
{
    /// <summary>The Shotgun: Damage 10, Shots 1, Penetration 1, +0%, Range 10 m; no keywords. An option for the ISF Ranger.</summary>
    public class ShotgunTests
    {
        private static WeaponTemplate Shotgun =>
            AssetDatabase.LoadAssetAtPath<WeaponTemplate>("Assets/_Project/Data/Weapons/Shotgun.asset");

        [Test]
        public void TheShotgunIsAsDesigned()
        {
            var s = Shotgun;
            Assert.That(s, Is.Not.Null);
            Assert.That(s.displayName, Is.EqualTo("Shotgun"));
            Assert.That((s.baseDamage, s.shots, s.PenetrationValue, s.accuracyBonus, s.range), Is.EqualTo((10, 1, (int?)1, 0, 10f)));
            Assert.That(s.keywords, Is.Empty, "no keywords: it can fire after moving (not Deployed)");
            Assert.That(UnitInfo.WeaponLines(UnitCatalog.Load().Available.Single(u => u.displayName == "ISF Ranger")), Does.Not.Contain("Shotgun"),
                "an option, not something the Ranger carries by default");
        }

        [Test]
        public void ItCanBeUsedAfterMovingButNotInCloseRange()
        {
            var s = Shotgun;
            Assert.That(s.HasKeyword(WeaponUse.DeployedKeyword), Is.False, "not Deployed: it can fire after moving");
            Assert.That(CloseRange.UsableInCloseRange(s.HasKeyword), Is.False, "not a Pistol or Melee weapon");
        }

        [Test]
        public void OnlyTheIsfRangerCanTakeIt()
        {
            var carriers = UnitCatalog.Load().Available.SelectMany(u => u.Roster()).Distinct()
                .Where(t => t.weapons.Contains(Shotgun) || t.weaponOptions.Any(o => o.alternatives.Contains(Shotgun)))
                .Select(t => t.displayName);
            Assert.That(carriers, Is.EqualTo(new[] { "ISF Ranger" }));
        }
    }
}
