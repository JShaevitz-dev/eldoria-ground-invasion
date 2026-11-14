using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>Unit roles: Infantry, Commander, Heavy Support, Specialist (Docs/Rules.md, Roles).</summary>
    public class UnitRoleTests
    {
        [Test]
        public void RolesAreWrittenAsInTheRules()
        {
            Assert.That(UnitRole.HeavySupport.DisplayName(), Is.EqualTo("Heavy Support"));
            Assert.That(UnitRole.Infantry.DisplayName(), Is.EqualTo("Infantry"));
        }

        [Test]
        public void CommandersAreListedFirstThenInfantryHeavySupportAndSpecialists()
        {
            Assert.That(UnitRole.Commander.ListOrder(), Is.LessThan(UnitRole.Infantry.ListOrder()));
            Assert.That(UnitRole.Infantry.ListOrder(), Is.LessThan(UnitRole.HeavySupport.ListOrder()));
            Assert.That(UnitRole.HeavySupport.ListOrder(), Is.LessThan(UnitRole.Specialist.ListOrder()));
        }

        [Test]
        public void ADeployedUnitHasItsTemplatesRole()
        {
            var template = TestArmies.Unit("Test Heavy Team", UnitRole.HeavySupport, TestArmies.Trooper, TestArmies.Trooper, 2);
            var unit = UnitFactory.Spawn(template, Side.Player, Vector3.zero, Quaternion.identity);
            try
            {
                Assert.That(unit.Role, Is.EqualTo(UnitRole.HeavySupport));
            }
            finally
            {
                Object.DestroyImmediate(unit.gameObject);
                Object.DestroyImmediate(template);
            }
        }
    }
}
