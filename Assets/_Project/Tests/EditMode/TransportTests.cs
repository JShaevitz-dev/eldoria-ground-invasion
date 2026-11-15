using System.Collections.Generic;
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
    /// Transports: Embark takes a friendly unit within 2.5 m of the transport aboard (off the battlefield, still in the
    /// battle); Disembark puts it back inside the transport, ready to move out.
    /// </summary>
    public class TransportTests
    {
        private static Ability Embark => AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/Embark.asset");
        private static Ability Disembark => AssetDatabase.LoadAssetAtPath<Ability>("Assets/_Project/Data/Abilities/Disembark.asset");
        private readonly List<GameObject> spawned = new List<GameObject>();

        [SetUp]
        public void SetUp() { Victory.Reset(); Turns.ResetCounter(); }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
            Victory.Reset();
            Turns.ResetCounter();
        }

        private Unit Spawn(UnitTemplate template, Side side, Vector3 at)
        {
            var unit = UnitFactory.Spawn(template, side, at, Quaternion.identity);
            spawned.Add(unit.gameObject);
            return unit;
        }

        /// <summary>Moves the whole unit so its leader stands at <paramref name="at"/>.</summary>
        private static void Place(Unit unit, Vector3 at)
        {
            var shift = at - unit.Leader.Position;
            foreach (var s in unit.Soldiers) s.transform.position += shift;
        }

        [Test]
        public void TheTransportAbilities()
        {
            Assert.That((Embark.kind, Embark.targets, Embark.affects, Embark.range, Embark.transport),
                Is.EqualTo((AbilityKind.Active, AbilityTarget.Unit, AuraAffects.Allies, 2.5f, TransportAction.Embark)));
            Assert.That((Disembark.kind, Disembark.targets, Disembark.transport),
                Is.EqualTo((AbilityKind.Active, AbilityTarget.Self, TransportAction.Disembark)));
            var withThem = UnitCatalog.Load().Available.Where(u => u.Roster().Any(s => s.abilities.Contains(Embark)));
            Assert.That(withThem, Is.Empty, "no unit is a transport yet");
        }

        [Test]
        public void AUnitWithin2Point5mOfTheHullCanBoard()
        {
            var transport = Spawn(TestArmies.Transport, Side.Player, Vector3.zero);
            var squad = Spawn(TestArmies.InfantrySquad, Side.Player, new Vector3(0f, 0f, -10f));
            var driver = transport.Leader;
            Assert.That(ActiveAbilities.WhyCannotUse(driver, Embark, targetUnit: squad), Does.Contain("Out of range"));
            // The closest soldier 2.4 m from the 1.8 m hull (centre to centre: 1.8 + 2.4 + the soldier's radius).
            var closest = squad.Leader;
            Place(squad, new Vector3(0f, 0f, -(1.8f + 2.4f + closest.Radius)));
            Assert.That(ActiveAbilities.WhyCannotUse(driver, Embark, targetUnit: squad), Is.Null);

            var r = ActiveAbilities.Use(driver, Embark, null, squad);
            Assert.That(r.Used, Is.True, r.Reason);
            Assert.That(transport.Passenger, Is.SameAs(squad));
            Assert.That((squad.IsEmbarked, squad.Carrier), Is.EqualTo((true, transport)));
            Assert.That(squad.gameObject.activeInHierarchy, Is.False, "off the battlefield");
            Assert.That(Victory.SoldiersLeft(Side.Player), Is.EqualTo(1 + squad.LivingSoldiers.Count()), "still part of the battle");
        }

        [Test]
        public void WhoCantBoard()
        {
            var transport = Spawn(TestArmies.Transport, Side.Player, Vector3.zero);
            var driver = transport.Leader;
            var enemy = Spawn(TestArmies.InfantrySquad, Side.AI, new Vector3(0f, 0f, 3f));
            Assert.That(ActiveAbilities.WhyCannotUse(driver, Embark, targetUnit: enemy), Does.Contain("isn't a friendly unit"));
            var tank = Spawn(TestArmies.MediumTank, Side.Player, new Vector3(5f, 0f, 0f));
            Assert.That(ActiveAbilities.WhyCannotUse(driver, Embark, targetUnit: tank), Does.Contain("can't board a transport"));
            Assert.That(ActiveAbilities.WhyCannotUse(driver, Embark, targetUnit: transport), Does.Contain("can't board itself"));
            Assert.That(ActiveAbilities.WhyCannotUse(driver, Disembark, targetUnit: transport), Does.Contain("Nobody is aboard"));

            var first = Spawn(TestArmies.FieldCommander, Side.Player, new Vector3(-3f, 0f, 0f));
            var second = Spawn(TestArmies.FieldCommander, Side.Player, new Vector3(0f, 0f, -3f));
            Assert.That(ActiveAbilities.Use(driver, Embark, null, first).Used, Is.True);
            Turns.StartNextTurn();   // (once a turn)
            Assert.That(ActiveAbilities.WhyCannotUse(driver, Embark, targetUnit: second), Does.Contain("already carrying"), "one unit at a time");
        }

        [Test]
        public void DisembarkingPutsTheUnitBackInsideTheTransport()
        {
            var transport = Spawn(TestArmies.Transport, Side.Player, new Vector3(4f, 0f, 2f));
            var squad = Spawn(TestArmies.InfantrySquad, Side.Player, new Vector3(4f, 0f, -2f));
            var driver = transport.Leader;
            Assert.That(ActiveAbilities.Use(driver, Embark, null, squad).Used, Is.True);
            Turns.StartNextTurn();
            var r = ActiveAbilities.Use(driver, Disembark, null, transport);
            Assert.That(r.Used, Is.True, r.Reason);
            Assert.That(r.TargetUnit, Is.SameAs(squad));
            Assert.That((transport.Passenger, squad.Carrier, squad.IsEmbarked), Is.EqualTo(((Unit)null, (Unit)null, false)));
            Assert.That(squad.gameObject.activeInHierarchy, Is.True);
            Assert.That(squad.LivingSoldiers.All(s => Vector3.Distance(s.Position, driver.Position) < 0.01f), Is.True, "every soldier inside the transport");
            Assert.That(squad.DisembarkingFrom, Is.SameAs(transport), "it has to move out");
        }

        [Test]
        public void AUnitAboardIsLostWithItsTransport()
        {
            var transport = Spawn(TestArmies.Transport, Side.Player, Vector3.zero);
            var squad = Spawn(TestArmies.InfantrySquad, Side.Player, new Vector3(0f, 0f, -3f));
            Spawn(TestArmies.InfantrySquad, Side.AI, new Vector3(0f, 0f, 30f));
            ActiveAbilities.Use(transport.Leader, Embark, null, squad);
            transport.Leader.Kill();
            Assert.That(squad.IsDestroyed, Is.True, "lost with the transport");
            Assert.That(Victory.Outcome, Is.EqualTo(BattleOutcome.AIWins), "nothing left on the player's side");
        }

        [Test]
        public void ASideWithOnlyAUnitAboardHasntLost()
        {
            var transport = Spawn(TestArmies.Transport, Side.Player, Vector3.zero);
            var squad = Spawn(TestArmies.InfantrySquad, Side.Player, new Vector3(0f, 0f, -3f));
            Spawn(TestArmies.InfantrySquad, Side.AI, new Vector3(0f, 0f, 30f));
            ActiveAbilities.Use(transport.Leader, Embark, null, squad);
            Assert.That(Victory.Check(), Is.EqualTo(BattleOutcome.InProgress));
        }
    }
}
