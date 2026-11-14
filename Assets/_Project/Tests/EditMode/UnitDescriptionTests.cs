using System.Linq;
using Eldoria.Units;
using NUnit.Framework;

namespace Eldoria.Tests
{
    /// <summary>Every unit has a description for the army builder, and it names the unit's role.</summary>
    public class UnitDescriptionTests
    {
        [Test]
        public void EveryUnitHasADescriptionThatNamesItsRole()
        {
            var units = UnitCatalog.Load().Available.ToList();
            Assert.That(units.Count, Is.GreaterThanOrEqualTo(19));
            foreach (var u in units)
            {
                Assert.That(u.description, Is.Not.Null.And.Not.Empty, u.displayName);
                Assert.That(u.description.Length, Is.GreaterThan(150), $"{u.displayName}: a real description, not a placeholder");
                string role = u.role == UnitRole.HeavySupport ? "Heavy Support" : u.role.ToString();
                Assert.That(u.description, Does.Contain(role), $"{u.displayName} says it's {role}");
            }
        }

        [Test]
        public void DescriptionsMatchTheUnits()
        {
            string Of(string name) => UnitCatalog.Load().Available.Single(u => u.displayName == name).description;
            Assert.That(Of("Dragon Hierarch"), Does.Contain("Honorguard"));
            Assert.That(Of("Kobold Militia"), Does.Contain("Swarm").And.Contain("Fang Caster"));
            Assert.That(Of("Rask Rifle Team"), Does.Contain("Solntse Laser Rifle").And.Contain("6 damage"));
            Assert.That(Of("Stinger Heavy Robot"), Does.Not.Contain("beetle"), "the model was redesigned");
            Assert.That(Of("Sagittarius MBT"), Does.Not.Contain("only weapon"), "other weapons hurt heavy armor now");
            Assert.That(Of("Master Sergeant Jakoby"), Does.Contain("no Lead from the front"));
            Assert.That(Of("Dragonborn Skymaster Squad"), Does.Not.Contain("rooftop"), "buildings have no roofs");
        }
    }
}
