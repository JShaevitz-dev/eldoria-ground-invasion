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
    /// The Draconis Imperialis's Dragonborn Skymaster Squad (Specialist): 1 Dragonborn Skylord + 4 Dragonborn Skymasters
    /// with the Dragonborn Footman's stats but Move 40 m and Jump Pack (climbing onto objects costs no movement), carrying
    /// Twin Raptor Pistols (Damage 4, Shots 2, Penetration 1, +0%, 20 m; Pistol).
    /// </summary>
    public class DragonbornSkymasterSquadTests
    {
        private static UnitCatalog Catalog => UnitCatalog.Load();
        private static Faction Draconis => Catalog.Factions.ElementAt(1);
        private static UnitTemplate Squad => Catalog.Available.Single(u => u.displayName == "Dragonborn Skymaster Squad");
        private static UnitTemplate Dragonborn => Catalog.Available.Single(u => u.displayName == "Dragonborn Squad");

        /// <summary>A measurer that returns one fixed route, to test what a route costs.</summary>
        private class FixedRoute : IMoveMeasurer
        {
            private readonly float length;
            private readonly int climbs;
            public FixedRoute(float length, int climbs) { this.length = length; this.climbs = climbs; }

            public bool TryGetStandPoint(Vector3 point, float radius, float height, out Vector3 stand, out string reason)
            {
                stand = point;
                reason = null;
                return true;
            }

            public MovePath Measure(Vector3 from, Vector3 to, float radius = 0f) =>
                new MovePath { Reachable = true, Length = length, Climbs = climbs, Points = new List<Vector3> { from, to } };
        }

        [Test]
        public void TheSkymasterSquadIsADraconisSpecialistUnitOfFive()
        {
            var unit = Squad;
            Assert.That(Draconis.Has(unit), Is.True);
            Assert.That(Catalog.Factions.First().Has(unit) || Catalog.Factions.ElementAt(2).Has(unit), Is.False, "only the Draconis's");
            Assert.That(unit.role, Is.EqualTo(UnitRole.Specialist));
            Assert.That(unit.Size, Is.EqualTo(5));
            Assert.That(ArmyRules.Composition(unit), Is.EqualTo("Dragonborn Skylord + 4\u00d7 Dragonborn Skymaster"));
            Assert.That(unit.description, Does.Contain("Jump Pack").And.Contain("Twin Raptor Pistols"));
        }

        [Test]
        public void TheyHaveTheFootmansStatsButMove40AndJumpPack()
        {
            var footman = Dragonborn.members.Single().soldier;
            var skylord = Squad.leader;
            var skymaster = Squad.members.Single().soldier;
            Assert.That(skylord.displayName, Is.EqualTo("Dragonborn Skylord"));
            Assert.That(skymaster.displayName, Is.EqualTo("Dragonborn Skymaster"));
            foreach (var s in new[] { skylord, skymaster })
            {
                Assert.That((s.health, s.armorLevel, s.accuracy, s.radius, s.height), Is.EqualTo((footman.health, footman.armorLevel, footman.accuracy, footman.radius, footman.height)), s.displayName);
                Assert.That((s.health, s.armorLevel, s.accuracy, s.movementRange), Is.EqualTo((10, 2, 75, 40f)), s.displayName);
                Assert.That(s.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Biological", "Jump Pack" }), s.displayName);
                Assert.That(s.weapons.Select(w => w.displayName), Is.EqualTo(new[] { "Twin Raptor Pistols" }), s.displayName);
                Assert.That(s.abilities, Is.Empty);
                Assert.That(s.prefab, Is.Not.Null);
                Assert.That(s.prefab, Is.Not.SameAs(footman.prefab), "jetpacks and pistols, not wings and a glaive");
            }
            Assert.That(skylord.prefab, Is.Not.SameAs(skymaster.prefab), "the Skylord has the gold crest");
        }

        [Test]
        public void TwinRaptorPistolsAreAsDesigned()
        {
            var w = Squad.leader.PrimaryWeapon;
            Assert.That(w.displayName, Is.EqualTo("Twin Raptor Pistols"));
            Assert.That((w.baseDamage, w.shots, w.PenetrationValue, w.accuracyBonus, w.range), Is.EqualTo((4, 2, (int?)1, 0, 20f)));
            Assert.That(w.keywords.Select(k => k.displayName), Is.EqualTo(new[] { "Pistol" }), "usable in close range");
            Assert.That(UnitInfo.WeaponLines(Squad).Single(), Is.EqualTo("Twin Raptor Pistols \u2014 Damage 4 \u00b7 Shots 2 \u00b7 Penetration 1 \u00b7 Range 20 m \u00b7 Pistol"));
        }

        [Test]
        public void JumpPackIsATraitKeyword()
        {
            var jp = KeywordLibrary.Get("Jump Pack");
            Assert.That(jp, Is.Not.Null);
            Assert.That(jp.appliesTo, Is.EqualTo(KeywordTarget.Soldier));
            Assert.That(jp.category, Is.EqualTo(KeywordCategory.Trait));
            Assert.That(MovementRules.JumpPackKeyword, Is.EqualTo("Jump Pack"));
        }

        [Test]
        public void ClimbingCostsAJumpPackSoldierNoMovement()
        {
            var sky = ArmyDeployment.Spawn(Squad, Side.AI, Vector3.zero, Quaternion.identity, "Skymasters", null, Draconis);
            var foot = ArmyDeployment.Spawn(Dragonborn, Side.AI, new Vector3(10f, 0f, 0f), Quaternion.identity, "Dragonborn", null, Draconis);
            try
            {
                var positions = new Dictionary<Soldier, Vector3>();
                // A 45 m route that climbs onto one object: 35 m of walking plus the 10 m climb.
                var route = new FixedRoute(45f, 1);
                var skyCheck = MovementRules.CheckLeaderMove(sky.Leader, Vector3.zero, new Vector3(0f, 0f, 5f), positions, route);
                Assert.That(skyCheck.Ok, Is.True);
                Assert.That(skyCheck.Distance, Is.EqualTo(35f).Within(0.01f), "the climb is free");
                Assert.That(skyCheck.Kind, Is.EqualTo(MoveKind.Normal), "35 m is within their 40 m move");

                var footCheck = MovementRules.CheckLeaderMove(foot.Leader, Vector3.zero, new Vector3(0f, 0f, 5f), positions, new FixedRoute(15f, 1));
                Assert.That(footCheck.Distance, Is.EqualTo(15f).Within(0.01f), "without a Jump Pack the climb costs 10 m");
                Assert.That(footCheck.Kind, Is.EqualTo(MoveKind.Sprint));

                Assert.That(MovementRules.CostFor(sky.Leader, new MovePath { Reachable = true, Length = 12f, Climbs = 0 }), Is.EqualTo(12f), "no climb, no change");
                Assert.That(MovementRules.CostFor(sky.Leader, new MovePath { Reachable = true, Length = 23f, Climbs = 2 }), Is.EqualTo(3f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(sky.gameObject);
                Object.DestroyImmediate(foot.gameObject);
            }
        }
    }
}
