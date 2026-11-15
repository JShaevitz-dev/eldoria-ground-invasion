using System.Linq;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;

namespace Eldoria.Tests
{
    /// <summary>
    /// Supply Cost = 3 × Firepower + ½ × Durability + 20 × active abilities + 10 × passive abilities + armor tax, rounded to a multiple of 5, where
    /// Firepower counts hit chance and penetration, and Durability is Health × (1 + Armor ÷ 4). Every unit carries its cost.
    /// </summary>
    public class SupplyCostTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static UnitTemplate Unit(string name) => Catalog.Available.Single(u => u.displayName == name);
        private static Faction FactionOf(UnitTemplate u) => Catalog.Factions.First(f => f.Has(u));
        private static SupplyCost.Parts Parts(string name) => SupplyCost.Breakdown(Unit(name), FactionOf(Unit(name)));

        [Test]
        public void EveryUnitHasItsSupplyCost()
        {
            foreach (var u in Catalog.Available)
            {
                var p = SupplyCost.Breakdown(u, FactionOf(u));
                TestContext.Out.WriteLine($"SUPPLY | {FactionOf(u).displayName} | {u.displayName} | {u.role} | {u.Size} | {p.Firepower:0.##} | {p.Durability:0.##} | {p.ActiveAbilities} | {p.PassiveAbilities} | {p.BestArmor} | {p.ArmorTax} | {u.supplyCostAdjustment} | {u.supplyCost} | {(double)u.supplyCost / u.Size:0.#}");
                Assert.That(u.supplyCost, Is.EqualTo(SupplyCost.Round(p.Total + u.supplyCostAdjustment)), u.displayName);
                Assert.That(u.supplyCost % 5, Is.EqualTo(0), $"{u.displayName}: a multiple of 5");
                if (u.displayName != "Rask Reliquary") Assert.That(u.supplyCostAdjustment, Is.EqualTo(0), u.displayName);
                Assert.That(u.supplyCost, Is.GreaterThan(0), u.displayName);
            }
        }

        [Test]
        public void CostsRoundToTheNearestMultipleOf5()
        {
            Assert.That(new[] { 0.0, 2.4, 2.5, 7.4, 7.5, 111.5, 112.5, 118.0 }.Select(SupplyCost.Round),
                Is.EqualTo(new[] { 0, 0, 5, 5, 10, 110, 115, 120 }));
        }

        [Test]
        public void PenetrationFactorsAverageTheRulesOverArmor0To4()
        {
            Assert.That(SupplyCost.PenetrationFactor(0), Is.EqualTo(0.3).Within(1e-6));
            Assert.That(SupplyCost.PenetrationFactor(1), Is.EqualTo(0.6).Within(1e-6));
            Assert.That(SupplyCost.PenetrationFactor(2), Is.EqualTo(0.9).Within(1e-6));
            Assert.That(SupplyCost.PenetrationFactor(3), Is.EqualTo(1.2).Within(1e-6));
            Assert.That(SupplyCost.PenetrationFactor(4), Is.EqualTo(1.4).Within(1e-6));
            Assert.That(SupplyCost.PenetrationFactor(5), Is.EqualTo(1.5).Within(1e-6));
            Assert.That(SupplyCost.PenetrationFactor(7), Is.EqualTo(1.5).Within(1e-6));
            Assert.That(SupplyCost.PenetrationFactor(null), Is.EqualTo(1.0), "Penetration X");
        }

        [Test]
        public void ArmorTaxes()
        {
            Assert.That(new[] { 0, 2, 3, 4, 5 }.Select(SupplyCost.ArmorTaxFor), Is.EqualTo(new[] { 0, 0, 50, 100, 100 }));
            string Taxed(int tax) => string.Join(", ", Catalog.Available.Where(u => SupplyCost.Breakdown(u, FactionOf(u)).ArmorTax == tax)
                .Select(u => u.displayName).OrderBy(n => n, System.StringComparer.Ordinal));
            Assert.That(Taxed(100), Is.EqualTo("Marshal Command Tank, Sagittarius MBT, Stinger Heavy Robot, Tyrant Hover-tank"));
            Assert.That(Taxed(50), Is.EqualTo("ISF Boarding Team, Intimidator Medium Robot, Olympian Mech, Workhorse Transport"));
        }

        [Test]
        public void TheIsfRifleSquad()
        {
            // Firepower: 10 Assault Rifles × (4 damage × 75% × 0.6 for Penetration 1) = 18 (the grenade: 5 × 0.3 = 1.5).
            // Durability: 10 × 5 health × 1.5 (Armor 2) = 75. Cost: 3 × 18 + ½ × 75 + the Frag Grenade 20 = 111.5 → 110.
            var p = Parts("ISF Rifle Squad");
            Assert.That(p.Firepower, Is.EqualTo(18).Within(1e-6));
            Assert.That(p.Durability, Is.EqualTo(75).Within(1e-6));
            Assert.That((p.ActiveAbilities, p.PassiveAbilities, p.ArmorTax, p.Total), Is.EqualTo((1, 0, 0, 110)));
        }

        [Test]
        public void TheFieldCommander()
        {
            // Pistol: 2 × 75% × 0.3 = 0.45; 10 health × 1.5 = 15; Lead from the front (passive). 1.35 + 7.5 + 10 = 18.85 → 20.
            var p = Parts("Field Commander");
            Assert.That(p.Firepower, Is.EqualTo(0.45).Within(1e-6));
            Assert.That(p.Total, Is.EqualTo(20));
        }

        [Test]
        public void TheSagittariusMbt()
        {
            // 100mm Cannon: 10 × 75% × 1.4 (Penetration 4) = 10.5; 20 health × 2 (Armor 4) = 40; Frag Grenade; tax 100.
            // 31.5 + 20 + 20 + 100 = 171.5 → 170.
            var p = Parts("Sagittarius MBT");
            Assert.That(p.Firepower, Is.EqualTo(10.5).Within(1e-6));
            Assert.That(p.Durability, Is.EqualTo(40).Within(1e-6));
            Assert.That((p.ActiveAbilities, p.ArmorTax, p.Total), Is.EqualTo((1, 100, 170)));
        }

        [Test]
        public void TheRaskRifleTeamAt75PercentAccuracy()
        {
            // 20 Solntse Laser Rifles × (6 × 75% × 0.6) = 54; 20 × 5 health × 1.25 (Armor 1) = 125. 162 + 62.5 = 224.5 → 225.
            var p = Parts("Rask Rifle Team");
            Assert.That(p.Firepower, Is.EqualTo(54).Within(1e-6));
            Assert.That(p.Durability, Is.EqualTo(125).Within(1e-6));
            Assert.That(p.Total, Is.EqualTo(225));
        }

        [Test]
        public void TheKoboldMilitia()
        {
            // 20 Fang Casters × (4 × 50% × 0.6) = 24; 20 × 5 health (Armor 0) = 100. 72 + 50 = 122 → 120.
            var p = Parts("Kobold Militia");
            Assert.That(p.Firepower, Is.EqualTo(24).Within(1e-6));
            Assert.That(p.Durability, Is.EqualTo(100).Within(1e-6));
            Assert.That(p.Total, Is.EqualTo(120));
        }

        [Test]
        public void TheHeavyWeaponsTeamCountsItsBestWeaponOption()
        {
            // The Flamethrower option (10 × 3 shots × 75% × 1 for Penetration X = 22.5) beats the Machine Gun (5 × 5 × 75% ×
            // 0.6 = 11.25) and the Rocket Launcher (5 × 75% × 1.5 = 5.625). With the Sergeant's Pistol (0.45) and 3 Assault
            // Rifles (5.4): 28.35. Durability 25 × 1.5 = 37.5. 85.05 + 18.75 = 103.8 → 105.
            var p = Parts("Heavy Weapons Team");
            Assert.That(p.Firepower, Is.EqualTo(28.35).Within(1e-6));
            Assert.That(p.Total, Is.EqualTo(105));
        }

        [Test]
        public void HitChanceIsCappedAt100Percent()
        {
            // The ISF Ranger (100%) with the Sniper Rifle (+50%) hits 100%, no more: 10 × 1.0 × 0.6 = 6. Its Anti-Materiel
            // Rifle option (10 × 100% × 1 for Penetration X = 10) is worth more.
            var ranger = Unit("ISF Ranger").leader;
            var sniper = ranger.weapons.Single();
            Assert.That(SupplyCost.HitChance(ranger, WeaponStats.For(sniper, FactionOf(Unit("ISF Ranger")))), Is.EqualTo(1.0));
            Assert.That(Parts("ISF Ranger").Firepower, Is.EqualTo(10).Within(1e-6), "the Anti-Materiel Rifle: 10 × 100% × 1");
        }

        [Test]
        public void AbilitiesAreCountedOncePerUnit()
        {
            Assert.That(Parts("Master Sergeant Jakoby").ActiveAbilities, Is.EqualTo(1), "two Frag Grenades, one ability");
            var mech = Parts("Olympian Mech");
            Assert.That((mech.ActiveAbilities, mech.PassiveAbilities), Is.EqualTo((1, 1)), "Iron Claw and Stabilizers");
        }
    }
}
