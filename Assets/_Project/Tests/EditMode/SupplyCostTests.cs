using System.Linq;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;

namespace Eldoria.Tests
{
    /// <summary>
    /// Supply Cost = total Health + highest potential damage per turn (no bonuses) + sum of Armor + 20 × active abilities
    /// + 10 × passive abilities. Every unit carries its cost, worked out by that formula.
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
                var parts = SupplyCost.Breakdown(u, FactionOf(u));
                TestContext.Out.WriteLine($"SUPPLY | {FactionOf(u).displayName} | {u.displayName} | {u.role} | {parts.Health} | {parts.Damage} | {parts.Armor} | {parts.ActiveAbilities} | {parts.PassiveAbilities} | {parts.ArmorTax} | {parts.Total}");
                Assert.That(u.supplyCost, Is.EqualTo(parts.Total), u.displayName);
                Assert.That(u.supplyCost, Is.GreaterThan(0), u.displayName);
            }
        }

        [Test]
        public void TheFieldCommander()
        {
            // 10 health + a Pistol (2 × 1) + Armor 2 + Lead from the front (passive).
            var p = Parts("Field Commander");
            Assert.That((p.Health, p.Damage, p.Armor, p.ActiveAbilities, p.PassiveAbilities, p.Total), Is.EqualTo((10, 2, 2, 0, 1, 24)));
        }

        [Test]
        public void TheIsfRifleSquad()
        {
            // 10 × 5 health; 10 Assault Rifles (4 each: the Sergeant's is better than his Pistol) = 40, more than a grenade's 5;
            // 10 × Armor 2; the Frag Grenade (one active ability for the squad). The Medkit/Repair Kit swaps don't count.
            var p = Parts("ISF Rifle Squad");
            Assert.That((p.Health, p.Damage, p.Armor, p.ActiveAbilities, p.PassiveAbilities, p.Total), Is.EqualTo((50, 40, 20, 1, 0, 130)));
        }

        [Test]
        public void TheHeavyWeaponsTeamCountsItsMostDamagingWeaponOption()
        {
            // 5 × 5 health; Sergeant's Pistol 2 + 3 Assault Rifles 12 + the Flamethrower option (10 × 3 = 30, more than the
            // Machine Gun's 5 × 5 = 25) = 44; 5 × Armor 2.
            var p = Parts("Heavy Weapons Team");
            Assert.That((p.Health, p.Damage, p.Armor, p.ActiveAbilities, p.PassiveAbilities, p.Total), Is.EqualTo((25, 44, 10, 0, 0, 79)));
        }

        [Test]
        public void TheRaskRifleTeamCountsTheRasksBetterRifles()
        {
            // 20 × 5 health; 20 Solntse Laser Rifles at 6 damage (the Rask's +2) = 120; 20 × Armor 1.
            var p = Parts("Rask Rifle Team");
            Assert.That((p.Health, p.Damage, p.Armor, p.Total), Is.EqualTo((100, 120, 20, 240)));
        }

        [Test]
        public void TheSagittariusMbt()
        {
            // 20 health + the 100mm Cannon (10) + Armor 4 + its Frag Grenade (active) + the heavy armor tax (Armor 4).
            var p = Parts("Sagittarius MBT");
            Assert.That((p.Health, p.Damage, p.Armor, p.ActiveAbilities, p.PassiveAbilities, p.ArmorTax, p.Total), Is.EqualTo((20, 10, 4, 1, 0, 200, 254)));
        }

        [Test]
        public void Armor4OrHigherPaysTheHeavyArmorTax()
        {
            // The Stinger (Armor 5): 25 health + the Plasma Mortar (5) + 5 armor + Stabilizers (passive) + 200.
            var stinger = Parts("Stinger Heavy Robot");
            Assert.That((stinger.Health, stinger.Damage, stinger.Armor, stinger.PassiveAbilities, stinger.ArmorTax, stinger.Total), Is.EqualTo((25, 5, 5, 1, 200, 245)));
            // Armor 4 is taxed too; Armor 3 isn't.
            Assert.That(Parts("Tyrant Hover-tank").ArmorTax, Is.EqualTo(200));
            Assert.That(Parts("Olympian Mech").ArmorTax, Is.EqualTo(0), "Armor 3");
            Assert.That(Parts("ISF Boarding Team").ArmorTax, Is.EqualTo(0), "Armor 3 with the Combat Shield");
            var taxed = Catalog.Available.Where(u => SupplyCost.Breakdown(u, FactionOf(u)).ArmorTax > 0).Select(u => u.displayName);
            Assert.That(taxed, Is.EquivalentTo(new[] { "Stinger Heavy Robot", "Sagittarius MBT", "Marshal Command Tank", "Tyrant Hover-tank" }));
        }

        [Test]
        public void AbilitiesAreCountedOncePerUnit()
        {
            // Jakoby's two Frag Grenades give one ability.
            Assert.That(Parts("Master Sergeant Jakoby").ActiveAbilities, Is.EqualTo(1));
            // The Olympian Mech: Iron Claw (active) and Stabilizers (passive).
            var mech = Parts("Olympian Mech");
            Assert.That((mech.ActiveAbilities, mech.PassiveAbilities), Is.EqualTo((1, 1)));
        }
    }
}
