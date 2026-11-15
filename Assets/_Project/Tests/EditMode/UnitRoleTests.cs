using System.Collections.Generic;
using System.Linq;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;

namespace Eldoria.Tests
{
    /// <summary>The unit roles: Infantry, Commander, Heavy Support, Specialist and Transport.</summary>
    public class UnitRoleTests
    {
        [Test]
        public void TransportIsARole()
        {
            Assert.That(UnitRole.Transport.DisplayName(), Is.EqualTo("Transport"));
            Assert.That(ArmyRules.IsUnique(UnitRole.Transport), Is.False, "an army can take more than one");
        }

        [Test]
        public void TransportsAreListedLast()
        {
            var order = new[] { UnitRole.Transport, UnitRole.Specialist, UnitRole.Infantry, UnitRole.Commander, UnitRole.HeavySupport }
                .OrderBy(r => r.ListOrder()).ToArray();
            Assert.That(order, Is.EqualTo(new[] { UnitRole.Commander, UnitRole.Infantry, UnitRole.HeavySupport, UnitRole.Specialist, UnitRole.Transport }));
        }

        [Test]
        public void AnArmyCanTakeTransports()
        {
            var transport = TestArmies.Unit("Test Transport", UnitRole.Transport, TestArmies.Transport.leader);
            var army = new List<UnitTemplate> { TestArmies.FieldCommander, transport, transport };
            Assert.That(ArmyRules.IsValid(army), Is.True, ArmyRules.Problems(army).FirstOrDefault());
            Assert.That(ArmyRules.Problems(new List<UnitTemplate> { transport }).Single(), Does.Contain("Commander"), "not a Commander");
        }

        [Test]
        public void TheWorkhorseIsTheTransportSoFar()
        {
            Assert.That(UnitCatalog.Load().Available.Where(u => u.role == UnitRole.Transport).Select(u => u.displayName), Is.EqualTo(new[] { "Workhorse Transport" }));
        }
    }
}
