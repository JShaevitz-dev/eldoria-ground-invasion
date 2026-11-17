using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>
    /// The Draconis Imperialis's Tyrant Hover-tank (Heavy Support): one Tyrant Hover-tank (Health 15, Armor 4, Accuracy 100%,
    /// Move 30 m; Mechanical, Unstable, Hover) with a Tyrant Railgun (Damage 5, Shots 1, Penetration 7, +0%, 50 m; Manual
    /// Load). Hover: terrain can't reduce its movement.
    /// </summary>
    public class TyrantHoverTankTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static UnitTemplate Tyrant => Catalog.Available.Single(u => u.displayName == "Tyrant Hover-tank");
        private static UnitTemplate Mbt => Catalog.Available.Single(u => u.displayName == "Sagittarius MBT");

        /// <summary>A measurer that returns one fixed route, to test what a route costs.</summary>
        private class FixedRoute : IMoveMeasurer
        {
            private readonly MovePath path;
            public FixedRoute(MovePath path) { this.path = path; }

            public bool TryGetStandPoint(Vector3 point, float radius, float height, out Vector3 stand, out string reason)
            {
                stand = point;
                reason = null;
                return true;
            }

            public MovePath Measure(Vector3 from, Vector3 to, float radius = 0f) => path;
        }

        [Test]
        public void TheTyrantIsADraconisHeavySupportUnitOfOne()
        {
            var unit = Tyrant;
            Assert.That(Draconis.Has(unit), Is.True);
            Assert.That(Catalog.Factions.First().Has(unit) || Catalog.Factions.ElementAt(2).Has(unit), Is.False, "only the Draconis's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.HeavySupport));
            Assert.That(unit.Size, Is.EqualTo(1));
            Assert.That(unit.description, Does.Contain("Hover").And.Contain("Kobold"));
        }

        [Test]
        public void ItHasTheGivenStatsAndKeywords()
        {
            var s = Tyrant.leader;
            Assert.That(s.displayName, Is.EqualTo("Tyrant Hover-tank"));
            Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((15, 4, 100, 30f)));
            Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Mechanical", "Unstable", "Hover", "Large" }));
            Assert.That(s.abilities, Is.Empty);
            Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Tyrant Railgun" }));
            Assert.That(s.radius, Is.EqualTo(Mbt.leader.radius), "as wide as the Sagittarius MBT");
            Assert.That(s.prefab, Is.Not.Null);
            Assert.That(s.prefab.transform.Find("Model/Pilot Head"), Is.Not.Null, "a Kobold pilot in the cockpit");
        }

        [Test]
        public void TheTyrantRailgunIsAsDesigned()
        {
            var w = Tyrant.leader.PrimaryWeapon;
            Assert.That(w.displayName, Is.EqualTo("Tyrant Railgun"));
            Assert.That((w.baseDamage, w.shots, w.PenetrationValue, w.accuracyBonus, w.range), Is.EqualTo((5, 1, (int?)7, 0, 50f)));
            Assert.That(w.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Manual Load" }));
            Assert.That(UnitInfo.WeaponLines(Tyrant).Single(), Is.EqualTo("Tyrant Railgun \u2014 Damage 5 \u00b7 Shots 1 \u00b7 Penetration 7 \u00b7 Range 50 m \u00b7 Manual Load"));
            // Penetration 7 is higher than any armor in the game: 150% damage (7) even against the Stinger's Armor 5.
            Assert.That(ArmorPenetration.Resolve(5, 7, 5).Damage, Is.EqualTo(7));
        }

        [Test]
        public void HoverIsATraitKeyword()
        {
            var hover = KeywordLibrary.Get("Hover");
            Assert.That(hover, Is.Not.Null);
            Assert.That(hover.appliesTo, Is.EqualTo(KeywordTarget.Soldier));
            Assert.That(hover.category, Is.EqualTo(KeywordCategory.Trait));
            Assert.That(MovementRules.HoverKeyword, Is.EqualTo("Hover"));
        }

        [Test]
        public void TerrainCantReduceItsMovement()
        {
            var tyrant = ArmyDeployment.Spawn(Tyrant, Side.AI, Vector3.zero, Quaternion.identity, "Tyrant", null, Draconis);
            var mbt = ArmyDeployment.Spawn(Mbt, Side.Player, new Vector3(20f, 0f, 0f), Quaternion.identity, "MBT", null, Catalog.Factions.First());
            try
            {
                Assert.That(tyrant.Leader.CanSprint, Is.False, "Mechanical");
                // 20 m flat with 5 m of it through water (counted twice), then a climb onto an object (10 m): 35 m for most.
                var wet = new MovePath
                {
                    Reachable = true, Length = 35f, Climbs = 1, WadingMeters = 5f,
                    Points = new List<Vector3> { Vector3.zero, new Vector3(0f, 0f, 20f), new Vector3(0f, 1.5f, 20f) }
                };
                var positions = new Dictionary<Soldier, Vector3>();
                var tyrantCheck = MovementRules.CheckLeaderMove(tyrant.Leader, Vector3.zero, new Vector3(0f, 1.5f, 20f), positions, new FixedRoute(wet));
                Assert.That(tyrantCheck.Ok, Is.True, tyrantCheck.Reason);
                Assert.That(tyrantCheck.Distance, Is.EqualTo(20f).Within(0.01f), "only the 20 m it covers");
                var mbtCheck = MovementRules.CheckLeaderMove(mbt.Leader, Vector3.zero, new Vector3(0f, 1.5f, 20f), positions, new FixedRoute(wet));
                Assert.That(mbtCheck.Ok, Is.False, "35 m is beyond the MBT's 30 m (and it can't sprint)");

                // Up and over a hill: the route is longer along the slope, but the Tyrant pays only the flat distance.
                var hill = new MovePath
                {
                    Reachable = true, Length = 26f,
                    Points = new List<Vector3> { Vector3.zero, new Vector3(0f, 6f, 12f), new Vector3(0f, 0f, 24f) }
                };
                Assert.That(MovementRules.CostFor(tyrant.Leader, hill), Is.EqualTo(24f).Within(0.01f));
                Assert.That(MovementRules.CostFor(mbt.Leader, hill), Is.EqualTo(26f));
            }
            finally
            {
                Object.DestroyImmediate(tyrant.gameObject);
                Object.DestroyImmediate(mbt.gameObject);
            }
        }
    }
}
