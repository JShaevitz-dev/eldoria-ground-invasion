using System.Collections.Generic;
using System.Linq;
using Eldoria.Network;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eldoria.Gameplay
{
    /// <summary>
    /// The deployment phase, before the first turn of a battle started from the menu (or a multiplayer lobby). Each
    /// army starts in its default slots in its side's deployment zone; the player can move any of their units elsewhere
    /// in their zone, then presses Ready. The other side's units are hidden until the battle begins, so neither player
    /// sees the other's deployment. In multiplayer both players deploy at the same time; the battle begins once both
    /// are ready (each game then moves the other's units to where that player put them). The turn timer, if it's on,
    /// gives the same length of time to deploy; when it runs out, the units stay where they are and the player is ready.
    /// </summary>
    public partial class MovementController
    {
        private static readonly Color ZoneColor = new Color(0.3f, 0.75f, 1f, 0.8f);

        private bool deploying;
        private bool deployReady;
        private OrderMsg remoteDeployment;
        private bool deployTimerRunning;
        private float deployTimeLeft;
        private Unit deployUnit;
        private DeploymentZone deployZone;
        private RangeRing zoneOutline;
        private readonly List<RangeRing> deployRings = new List<RangeRing>();

        /// <summary>The battle hasn't begun yet: the players are placing their units.</summary>
        public bool IsDeploying => deploying;
        /// <summary>This player has finished deploying (in multiplayer, they may be waiting for their opponent).</summary>
        public bool DeploymentReady => deployReady;
        /// <summary>Seconds left to deploy, or null if the turn timer is off (or this player is ready).</summary>
        public float? DeploymentTimeLeft => deploying && deployTimerRunning ? deployTimeLeft : (float?)null;
        /// <summary>The unit the player has picked to place.</summary>
        public Unit DeployingUnit => deployUnit;
        /// <summary>This player's deployment zone (null if the map has none).</summary>
        public DeploymentZone DeploymentArea => deployZone;

        private static DeploymentZone ZoneFor(Side side) =>
            FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None).FirstOrDefault(z => z.Side == side);

        /// <summary>A battle started from the menu or a lobby, on a map with deployment zones, starts with deployment.</summary>
        private bool ShouldDeploy() => ArmySelection.DeploymentPhase && ZoneFor(playerSide) != null;

        private void BeginDeployment()
        {
            deploying = true;
            deployReady = false;
            remoteDeployment = null;
            deployUnit = null;
            deployZone = ZoneFor(playerSide);
            foreach (var u in remoteUnits) if (u != null) u.SetHidden(true);
            deployTimerRunning = TurnTimerSettings.Enabled;
            deployTimeLeft = TurnTimerSettings.Seconds;
            ShowZoneOutline();
            ForceFlash("Deployment: click one of your units, then click where in your deployment zone to place it. Press Ready when you're done.");
        }

        private void UpdateDeployment(Mouse mouse, Keyboard kb)
        {
            if (deployReady)
            {
                HideDeployPreview();
                return;
            }
            if (deployTimerRunning)
            {
                deployTimeLeft -= Time.deltaTime;
                if (deployTimeLeft <= 0f)
                {
                    deployTimeLeft = 0f;
                    ForceFlash("Time's up! Your units deploy where they stand.");
                    CombatAudio.PlayError();
                    ReadyDeployment();
                    return;
                }
            }

            UpdateHover(mouse);
            if (kb != null)
            {
                if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { ReadyDeployment(); return; }
                if (kb.escapeKey.wasPressedThisFrame) deployUnit = null;
                if (kb.tabKey.wasPressedThisFrame && playerUnits.Count > 0)
                {
                    int i = deployUnit != null ? playerUnits.IndexOf(deployUnit) : -1;
                    PickForDeployment(playerUnits[(i + 1) % playerUnits.Count]);
                }
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !PointerOverHud(mouse))
            {
                if (hoverSoldier != null && hoverSoldier.Unit != null && playerUnits.Contains(hoverSoldier.Unit) && hoverSoldier.Unit != deployUnit)
                    PickForDeployment(hoverSoldier.Unit);
                else if (deployUnit != null && hasHover)
                {
                    if (DeployUnitAt(deployUnit, hoverPoint, out string reason)) Flash($"{deployUnit.DisplayName} deployed. Click another unit, or press Ready.");
                    else { Flash($"Can't deploy there: {reason}."); CombatAudio.PlayError(); }
                }
                else Flash("Click one of your units (or press Tab) to place it.");
            }
            UpdateDeployPreview();
        }

        private void PickForDeployment(Unit unit)
        {
            deployUnit = unit;
            Flash($"{unit.DisplayName}: click where its leader should stand. Its soldiers form up behind.");
        }

        /// <summary>Where each living soldier of <paramref name="unit"/> would stand with its leader at <paramref name="leaderPoint"/>: the leader there, the rest in ranks behind, facing the enemy.</summary>
        private List<(Soldier soldier, Vector3 point)> FormationAt(Unit unit, Vector3 leaderPoint)
        {
            var soldiers = unit.LivingSoldiers.OrderBy(s => s == unit.Leader ? 0 : 1).ToList();
            float spacing = unit.Template != null ? unit.Template.deploymentSpacing : 2f;
            var slots = UnitFactory.FormationSlots(soldiers.Count, spacing);
            var facing = Facing(unit.Side);
            return soldiers.Select((s, i) => (s, leaderPoint + facing * slots[i])).ToList();
        }

        private static Quaternion Facing(Side side) => Quaternion.Euler(0f, side == Side.Player ? 0f : 180f, 0f);

        /// <summary>
        /// Checks a unit could deploy with its leader at the point: every soldier on ground they can stand on, inside this
        /// player's deployment zone and the battlefield, and not on top of another of this player's soldiers.
        /// </summary>
        public bool CanDeployAt(Unit unit, Vector3 leaderPoint, out List<(Soldier soldier, Vector3 point)> placed, out string reason)
        {
            placed = new List<(Soldier, Vector3)>();
            reason = null;
            if (!deploying || deployReady) { reason = "deployment is over"; return false; }
            if (unit == null || !playerUnits.Contains(unit)) { reason = "that isn't one of your units"; return false; }
            var zone = deployZone;
            var measurer = BattlefieldNavigation.Measurer;
            var others = new Dictionary<Soldier, Vector3>();
            foreach (var u in playerUnits)
                if (u != unit)
                    foreach (var s in u.LivingSoldiers) others[s] = s.Position;

            foreach (var (s, p) in FormationAt(unit, leaderPoint))
            {
                Vector3 stand = p;
                if (measurer != null)
                {
                    if (!measurer.TryGetStandPoint(p, s.Radius, s.Height, out stand, out string why))
                    {
                        reason = $"{s.name}: {why}";
                        return false;
                    }
                }
                else stand.y = TerrainMeasurer.SurfaceHeight(p);
                if (zone != null && !zone.Contains(stand)) { reason = "outside your deployment zone"; return false; }
                if (!MovementRules.IsInsideBattlefield(stand, s.Radius)) { reason = "off the battlefield"; return false; }
                if (!MovementRules.IsSpaceFree(s, stand, others)) { reason = "another unit is in the way"; return false; }
                others[s] = stand;
                placed.Add((s, stand));
            }
            return true;
        }

        /// <summary>Deploys one of this player's units with its leader at the point, if it can go there (see <see cref="CanDeployAt"/>).</summary>
        public bool DeployUnitAt(Unit unit, Vector3 leaderPoint, out string reason)
        {
            if (!CanDeployAt(unit, leaderPoint, out var placed, out reason)) return false;
            var facing = Facing(unit.Side);
            foreach (var (s, p) in placed) s.transform.SetPositionAndRotation(p, facing);
            NearbyKeywords.RefreshAll();
            return true;
        }

        /// <summary>
        /// This player has finished deploying. Single player: the battle begins. Multiplayer: their deployment is sent to
        /// the other player, and the battle begins once theirs has arrived too.
        /// </summary>
        public void ReadyDeployment()
        {
            if (!deploying || deployReady || Victory.IsOver) return;
            deployReady = true;
            deployTimerRunning = false;
            deployUnit = null;
            HideDeployPreview();
            if (IsMultiplayer)
            {
                session.SendOrder(DeploymentOrder());
                if (remoteDeployment == null)
                {
                    ForceFlash("Waiting for your opponent to finish deploying…");
                    return;
                }
                ApplyDeployment(remoteDeployment);
            }
            FinishDeployment();
        }

        /// <summary>This player's deployment: every one of their soldiers' exact positions.</summary>
        private OrderMsg DeploymentOrder()
        {
            var order = new OrderMsg { kind = OrderKinds.Deploy };
            foreach (var u in playerUnits)
                for (int i = 0; i < u.Soldiers.Count; i++)
                {
                    var s = u.Soldiers[i];
                    if (s == null) continue;
                    var p = s.transform.position;
                    order.placements.AddRange(new[] { u.NetId, i,
                        System.BitConverter.SingleToInt32Bits(p.x), System.BitConverter.SingleToInt32Bits(p.y), System.BitConverter.SingleToInt32Bits(p.z) });
                }
            return order;
        }

        /// <summary>The other player's deployment arrived (it stays hidden until the battle begins).</summary>
        private bool ReceiveDeployment(OrderMsg order)
        {
            if (!deploying || remoteDeployment != null) return false;
            remoteDeployment = order;
            if (deployReady)
            {
                ApplyDeployment(order);
                FinishDeployment();
            }
            return true;
        }

        /// <summary>Moves the other player's units to where they deployed them.</summary>
        private void ApplyDeployment(OrderMsg order)
        {
            var list = order.placements;
            for (int i = 0; i + 4 < list.Count; i += 5)
            {
                var unit = UnitById(list[i]);
                if (unit == null || unit.Side == playerSide) continue;   // only the other player's own units
                var s = SoldierAt(unit, list[i + 1]);
                if (s == null) continue;
                var p = new Vector3(System.BitConverter.Int32BitsToSingle(list[i + 2]), System.BitConverter.Int32BitsToSingle(list[i + 3]),
                    System.BitConverter.Int32BitsToSingle(list[i + 4]));
                s.transform.SetPositionAndRotation(p, Facing(unit.Side));
            }
        }

        /// <summary>Both sides are deployed: reveal everyone and start the first turn.</summary>
        private void FinishDeployment()
        {
            EndDeploymentState();
            NearbyKeywords.RefreshAll();
            NextTurn();
        }

        private void EndDeploymentState()
        {
            deploying = false;
            deployReady = false;
            deployTimerRunning = false;
            remoteDeployment = null;
            deployUnit = null;
            HideDeployPreview();
            if (zoneOutline != null) zoneOutline.Hide();
            foreach (var u in remoteUnits) if (u != null) u.SetHidden(false);
        }

        // ------------------------------------------------------------------ Visuals

        private void ShowZoneOutline()
        {
            if (deployZone == null) return;
            if (zoneOutline == null) zoneOutline = RangeRing.Create("Ring_DeploymentZone", transform, 0.15f);
            Vector3 c = deployZone.transform.position;
            Vector2 half = deployZone.Size / 2f;
            var corners = new[]
            {
                new Vector3(c.x - half.x, 0f, c.z - half.y), new Vector3(c.x + half.x, 0f, c.z - half.y),
                new Vector3(c.x + half.x, 0f, c.z + half.y), new Vector3(c.x - half.x, 0f, c.z + half.y),
            };
            var points = new List<Vector3>();
            for (int k = 0; k < 4; k++)
            {
                Vector3 a = corners[k], b = corners[(k + 1) % 4];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 2f));
                for (int j = 0; j < steps; j++)
                {
                    var p = Vector3.Lerp(a, b, j / (float)steps);
                    p.y = TerrainMeasurer.SurfaceHeight(p);
                    points.Add(p);
                }
            }
            points.Add(points[0]);
            zoneOutline.ShowPath(points, ZoneColor);
        }

        private void UpdateDeployPreview()
        {
            int n = 0;
            if (deployUnit != null)
            {
                foreach (var s in deployUnit.LivingSoldiers)
                {
                    while (deployRings.Count <= n) deployRings.Add(RangeRing.Create("Ring_Deploy", transform, 0.06f));
                    deployRings[n++].ShowCircle(s.Position, s.Radius + 0.3f, SelectColor);
                }
                if (hasHover && (hoverSoldier == null || hoverSoldier.Unit == deployUnit))
                {
                    bool ok = CanDeployAt(deployUnit, hoverPoint, out _, out _);
                    foreach (var (s, p) in FormationAt(deployUnit, hoverPoint))
                    {
                        while (deployRings.Count <= n) deployRings.Add(RangeRing.Create("Ring_Deploy", transform, 0.06f));
                        var at = p;
                        at.y = TerrainMeasurer.SurfaceHeight(p);
                        deployRings[n++].ShowCircle(at, s.Radius + 0.2f, ok ? FollowColor : InvalidColor);
                    }
                }
            }
            for (int i = n; i < deployRings.Count; i++) deployRings[i].Hide();
        }

        private void HideDeployPreview()
        {
            foreach (var r in deployRings) if (r != null) r.Hide();
        }

        private void DrawDeploymentHud(GUIStyle wrap, GUIStyle title)
        {
            GUILayout.Label("DEPLOYMENT", title);
            DrawFactions(wrap);
            if (deployTimerRunning)
            {
                var timerStyle = new GUIStyle(title);
                timerStyle.normal.textColor = deployTimeLeft <= 60f ? new Color(1f, 0.35f, 0.3f) : Color.white;
                GUILayout.Label($"Time left: {TurnTimerSettings.Format(deployTimeLeft)}", timerStyle);
            }
            if (deployReady)
            {
                GUILayout.Label("You're ready. Waiting for your opponent to finish deploying…", wrap);
            }
            else
            {
                GUILayout.Label("Place your units in your deployment zone (outlined in blue). Click a unit (or press Tab), then " +
                                "click where its leader should stand; its soldiers form up behind. Your opponent can't see your " +
                                "units, and you can't see theirs, until the battle begins.", wrap);
                GUILayout.Label(deployUnit != null ? $"Placing: {deployUnit.DisplayName} (Esc to put it down)" : "No unit picked.", wrap);
                if (GUILayout.Button("Ready (Enter)")) ReadyDeployment();
            }
            if (!string.IsNullOrEmpty(message) && Time.time < messageUntil) GUILayout.Label(message, wrap);
        }
    }
}
