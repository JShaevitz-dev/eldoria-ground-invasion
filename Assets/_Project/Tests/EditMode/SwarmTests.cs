using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Rules;
using Eldoria.Units;
using NUnit.Framework;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>Swarm (soldier keyword): a unit whose leader has it keeps cohesion out to 10 m instead of 5 m.</summary>
    public class SwarmTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        /// <summary>A leader and three followers 8 m behind it, with or without Swarm.</summary>
        private Unit Squad(bool swarm, out Soldier leader, out List<Soldier> followers)
        {
            var profile = ScriptableObject.CreateInstance<SoldierTemplate>();   // move 10, sprint 20, radius 0.35
            created.Add(profile);
            if (swarm) profile.keywords.Add(KeywordLibrary.Get(MovementRules.SwarmKeyword));
            var go = new GameObject(swarm ? "Swarm Unit" : "Unit");
            created.Add(go);
            var unit = go.AddComponent<Unit>();
            Soldier Spawn(bool isLeader, Vector3 at)
            {
                var s = new GameObject(isLeader ? "Leader" : "Follower");
                s.transform.SetParent(go.transform);
                s.transform.position = at;
                var soldier = s.AddComponent<Soldier>();
                soldier.Configure(profile, isLeader);
                return soldier;
            }
            leader = Spawn(true, Vector3.zero);
            followers = Enumerable.Range(0, 3).Select(i => Spawn(false, new Vector3((i - 1) * 1.5f, 0f, -8f))).ToList();
            unit.RefreshSoldiers();
            return unit;
        }

        [Test]
        public void SwarmIsATraitKeyword()
        {
            var swarm = KeywordLibrary.Get("Swarm");
            Assert.That(swarm, Is.Not.Null);
            Assert.That(swarm.category, Is.EqualTo(KeywordCategory.Trait));
            Assert.That(swarm.appliesTo, Is.EqualTo(KeywordTarget.Soldier));
            Assert.That(swarm.description, Does.Contain("10 m"));
            Assert.That((MovementRules.CohesionRange, MovementRules.SwarmCohesionRange), Is.EqualTo((5f, 10f)));
        }

        [Test]
        public void ASwarmUnitsCohesionRadiusIs10Metres()
        {
            var normal = Squad(false, out _, out _);
            var swarm = Squad(true, out _, out _);
            Assert.That(MovementRules.CohesionRangeFor(normal), Is.EqualTo(5f));
            Assert.That(MovementRules.CohesionRangeFor(swarm), Is.EqualTo(10f));
        }

        [Test]
        public void SoldiersUpTo10MetresFromASwarmLeaderAreInCohesion()
        {
            var normal = Squad(false, out _, out _);
            var swarm = Squad(true, out _, out _);
            Assert.That(normal.OutOfCohesion().Count, Is.EqualTo(3), "8 m from the leader: out of a normal unit's 5 m");
            Assert.That(swarm.OutOfCohesion(), Is.Empty, "8 m is inside the Swarm unit's 10 m");
        }

        [Test]
        public void FollowersOfASwarmUnitCanEndUpTo10MetresFromTheLeader()
        {
            var normal = Squad(false, out var nLeader, out var nFollowers);
            var swarm = Squad(true, out var sLeader, out var sFollowers);
            var na = new UnitMoveActivation(normal, normal.Soldiers);
            var sa = new UnitMoveActivation(swarm, swarm.Soldiers);
            Assert.That(na.TryMoveLeader(new Vector3(0f, 0f, 1f), out _), Is.True);
            Assert.That(sa.TryMoveLeader(new Vector3(0f, 0f, 1f), out _), Is.True);

            var nine = new Vector3(0f, 0f, -8f);    // 9 m from the leader's new spot
            var eleven = new Vector3(0f, 0f, -10f); // 11 m
            Assert.That(sa.CheckFollowerMove(sFollowers[1], nine).Ok, Is.True, "9 m: within 10 m");
            var tooFar = sa.CheckFollowerMove(sFollowers[1], eleven);
            Assert.That(tooFar.Ok, Is.False, "11 m: beyond 10 m");
            Assert.That(tooFar.Reason, Does.Contain("10 m"));
            var normalCheck = na.CheckFollowerMove(nFollowers[1], nine);
            Assert.That(normalCheck.Ok, Is.False, "9 m is out of a normal unit's 5 m");
            Assert.That(normalCheck.Reason, Does.Contain("5 m"));
        }

        [Test]
        public void GatheringASwarmUnitOnlyPullsInSoldiersBeyond10Metres()
        {
            var swarm = Squad(true, out _, out var followers);
            var a = new UnitMoveActivation(swarm, swarm.Soldiers);
            Assert.That(a.TryMoveLeader(new Vector3(0f, 0f, 6f), out _), Is.True);   // now 14 m from the followers
            Assert.That(a.OutOfCohesion().Count, Is.EqualTo(3));
            a.GatherIntoCohesion();
            Assert.That(a.OutOfCohesion(), Is.Empty);
            foreach (var f in followers)
                Assert.That(Measure.Horizontal(a.LeaderPosition, a.PositionOf(f)), Is.GreaterThan(5f).And.LessThanOrEqualTo(10.01f),
                    "pulled just inside 10 m, not 5 m");
        }

        [Test]
        public void OnlyTheKoboldMilitiaHasSwarmSoFar()
        {
            var swarm = UnitCatalog.Load().Available.Where(u => u.Roster().Any(s => s.HasKeyword("Swarm"))).Select(u => u.displayName);
            Assert.That(swarm, Is.EqualTo(new[] { "Kobold Militia" }));
        }
    }
}
