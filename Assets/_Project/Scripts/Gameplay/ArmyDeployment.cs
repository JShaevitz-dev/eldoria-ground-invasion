using System.Collections.Generic;
using System.Linq;
using Eldoria.Rules;
using Eldoria.Terrain;
using Eldoria.Units;
using UnityEngine;

namespace Eldoria.Gameplay
{
    /// <summary>
    /// Puts the chosen armies (<see cref="ArmySelection"/>) on the battlefield, replacing the armies placed in the scene.
    /// Each army deploys in its side's deployment zone on the map being played, one unit per slot in list order, left
    /// to right as its side looks across the map. Units take their faction's colour (see PrimaryColor).
    /// </summary>
    public static class ArmyDeployment
    {
        /// <summary>Units per row across a deployment zone (an army of up to this many deploys in one row).</summary>
        public const int UnitsPerRow = 6;

        /// <summary>Metres between rows of units, for an army too big for one row.</summary>
        public const float RowSpacing = 7f;

        /// <summary>
        /// A deployment slot (unit leader position) in a zone. Units go in rows of <see cref="UnitsPerRow"/> spread evenly
        /// across the zone's width, left to right as the side looks across the map (the player looks north, the AI south).
        /// The first row is a little forward of the zone's middle so the units' back ranks stay inside it; any further rows
        /// (an army of more than six units) are <see cref="RowSpacing"/> behind each other, never off the battlefield: a unit
        /// whose formation is <paramref name="depth"/> metres deep behind its leader keeps its back rank on the board too.
        /// </summary>
        public static Vector3 Slot(Vector3 zoneCentre, Vector2 zoneSize, Side side, int index, float depth = 0f)
        {
            float forward = side == Side.Player ? 1f : -1f;   // towards the enemy
            int row = index / UnitsPerRow, col = index % UnitsPerRow;
            float across = (col + 0.5f) * zoneSize.x / UnitsPerRow - zoneSize.x / 2f;
            var slot = zoneCentre + new Vector3(across * forward, 0f, forward * (zoneSize.y * 0.125f - row * RowSpacing));
            if (row > 0)
            {
                // Back rows stay on the battlefield, but never come in front of the first row.
                float edge = Core.BattlefieldScale.BoardDepth / 2f - 2f - depth;
                float firstRow = zoneCentre.z + forward * zoneSize.y * 0.125f;
                float z = Mathf.Clamp(slot.z, -edge, edge);
                slot.z = forward > 0f ? Mathf.Min(z, firstRow) : Mathf.Max(z, firstRow);
            }
            return slot;
        }

        /// <summary>The slot in the current map's deployment zone for that side (the Boxed in's zones if the map has none).</summary>
        public static Vector3 SlotFor(Side side, int index, float depth = 0f)
        {
            var zone = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None).FirstOrDefault(z => z.Side == side);
            if (zone != null) return Slot(zone.transform.position, zone.Size, side, index, depth);
            return Slot(new Vector3(0f, 0f, side == Side.Player ? -26f : 26f), new Vector2(80f, 8f), side, index, depth);
        }

        /// <summary>The AI's colour when it has no faction (e.g. tests): the old enemy red. The player keeps the model's own blue.</summary>
        public static readonly Color NoFactionAIColor = new Color(0.62f, 0.16f, 0.14f);

        /// <summary>Deploys the selected armies, if any were chosen. Returns true if it did.</summary>
        public static bool DeploySelected()
        {
            if (!ArmySelection.HasArmies) return false;
            Deploy(ArmySelection.PlayerArmy, ArmySelection.AIArmy, ArmySelection.PlayerFaction, ArmySelection.AIFaction,
                ArmySelection.PlayerLoadouts, ArmySelection.AILoadouts);
            return true;
        }

        /// <summary>Removes every unit on the battlefield and deploys these two armies.</summary>
        public static List<Unit> Deploy(IReadOnlyList<UnitTemplate> player, IReadOnlyList<UnitTemplate> ai,
            Faction playerFaction = null, Faction aiFaction = null, IReadOnlyList<UnitLoadout> playerLoadouts = null,
            IReadOnlyList<UnitLoadout> aiLoadouts = null)
        {
            // Out with the armies placed in the scene (deactivated at once so nothing finds them this frame).
            foreach (var old in Object.FindObjectsByType<Unit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                old.gameObject.SetActive(false);
                Object.Destroy(old.gameObject);
            }
            var oldRoot = GameObject.Find("Armies");
            if (oldRoot != null) { oldRoot.SetActive(false); Object.Destroy(oldRoot); }

            var root = new GameObject("Armies").transform;
            var deployed = new List<Unit>();
            occupied.Clear();
            deployed.AddRange(DeployArmy(player, Side.Player, root, playerFaction, playerLoadouts));
            deployed.AddRange(DeployArmy(ai, Side.AI, root, aiFaction, aiLoadouts));
            return deployed;   // nearby keywords (cover, auras) are worked out when the first turn starts
        }

        /// <summary>How far a unit's formation reaches behind its leader: its back rank, plus a soldier's radius.</summary>
        public static float FormationDepth(UnitTemplate unit)
        {
            if (unit == null) return 0f;
            int ranks = Mathf.CeilToInt(unit.Size / (float)UnitFactory.RankWidth);
            float radius = unit.Roster().Where(s => s != null).Select(s => s.radius).DefaultIfEmpty(0f).Max();
            return (ranks - 1) * unit.deploymentSpacing + radius;
        }

        private static IEnumerable<Unit> DeployArmy(IReadOnlyList<UnitTemplate> army, Side side, Transform root, Faction faction,
            IReadOnlyList<UnitLoadout> loadouts)
        {
            var units = army.Where(u => u != null).ToList();
            var names = UnitNames(units, side);
            var facing = Quaternion.Euler(0f, side == Side.Player ? 0f : 180f, 0f);
            var zone = Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None).FirstOrDefault(z => z.Side == side);
            for (int i = 0; i < units.Count; i++)
            {
                var slot = ClearSpotFor(units[i], SlotFor(side, i, FormationDepth(units[i])), facing, zone);
                var unit = Spawn(units[i], side, slot, facing, names[i], root, faction,
                    loadouts != null && i < loadouts.Count ? loadouts[i] : null);
                unit.NetId = NetIdOf(side, i);
                foreach (var s in unit.Soldiers) occupied.Add((s.transform.position, s.Radius));
                yield return unit;
            }
        }

        /// <summary>Where the soldiers deployed so far stand (and how big they are), so later units don't overlap them.</summary>
        private static readonly List<(Vector3 at, float radius)> occupied = new List<(Vector3, float)>();

        /// <summary>
        /// The unit's slot if its whole formation fits there (see <see cref="FormationFits"/>; inside the zone too, for a zone
        /// that keeps its army inside); otherwise the nearest spot in the side's deployment zone where it does (on a 2 m
        /// grid), e.g. when a big army's back row would land on a fortress wall. If nowhere fits, the slot anyway.
        /// </summary>
        public static Vector3 ClearSpotFor(UnitTemplate unit, Vector3 slot, Quaternion facing, DeploymentZone zone)
        {
            if (unit == null || FormationFits(unit, slot, facing, zone != null && zone.KeepsArmyInside ? zone : null)) return slot;
            if (zone == null) return slot;
            const float step = 2f;
            var candidates = new List<Vector3>();
            foreach (var r in zone.Areas)
                for (float x = r.xMin + step / 2f; x < r.xMax; x += step)
                    for (float z = r.yMin + step / 2f; z < r.yMax; z += step)
                        candidates.Add(new Vector3(x, 0f, z));
            foreach (var c in candidates.Distinct().OrderBy(c => (c - slot).sqrMagnitude).ThenBy(c => c.x).ThenBy(c => c.z))
            {
                if (!zone.Contains(c)) continue;
                if (FormationFits(unit, c, facing, zone)) return c;
            }
            return slot;
        }

        /// <summary>
        /// Would every soldier of the unit, in formation with its leader at <paramref name="slot"/>, stand on open ground
        /// (not on or in a building, wall, ramp, water or object), on the battlefield, clear of the soldiers deployed so
        /// far (and inside <paramref name="within"/>, if given)?
        /// </summary>
        public static bool FormationFits(UnitTemplate unit, Vector3 slot, Quaternion facing, DeploymentZone within = null)
        {
            var roster = unit.Roster().ToList();
            var slots = UnitFactory.FormationSlots(roster.Count, unit.deploymentSpacing);
            var placed = new List<(Vector3, float)>();
            for (int i = 0; i < roster.Count; i++)
            {
                float radius = roster[i] != null ? roster[i].radius : 0.35f;
                float height = roster[i] != null ? roster[i].height : 1.8f;
                var p = slot + facing * slots[i];
                if (!MovementRules.IsInsideBattlefield(p, radius)) return false;
                if (within != null && !within.Contains(p)) return false;
                if (!Physics.Raycast(new Vector3(p.x, 50f, p.z), Vector3.down, out var hit, 100f, TerrainLayers.TerrainMask, QueryTriggerInteraction.Ignore))
                    return false;
                if (hit.collider.gameObject.layer != TerrainLayers.Structure) return false;   // on an object
                var st = hit.collider.GetComponentInParent<StructureTerrain>();
                if (st != null && st.Kind != StructureKind.Ground && st.Kind != StructureKind.Hill) return false;
                var ground = hit.point;
                var bottom = ground + Vector3.up * (radius + 0.05f);
                var top = ground + Vector3.up * Mathf.Max(radius + 0.06f, height - radius);
                foreach (var c in Physics.OverlapCapsule(bottom, top, radius * 0.9f, TerrainLayers.TerrainMask, QueryTriggerInteraction.Ignore))
                {
                    if (c == hit.collider) continue;
                    if (c.gameObject.layer == TerrainLayers.Object) return false;
                    var other = c.GetComponentInParent<StructureTerrain>();
                    if (other == null || (other.Kind != StructureKind.Ground && other.Kind != StructureKind.Hill)) return false;
                }
                foreach (var (at, r) in occupied.Concat(placed))
                    if (Measure.Horizontal(at, p) < r + radius) return false;
                placed.Add((p, radius));
            }
            return true;
        }

        /// <summary>A unit's id in a battle, the same in both games of a multiplayer battle: the side, and its slot in the list.</summary>
        public static int NetIdOf(Side side, int slot) => (side == Side.Player ? 0 : 100) + slot;

        /// <summary>
        /// Names for the units of an army: a unit taken more than once is numbered ("Infantry Squad 1", "Infantry Squad 2");
        /// the other side's units (from this game's player's point of view) are marked "(Enemy)".
        /// </summary>
        public static List<string> UnitNames(IReadOnlyList<UnitTemplate> units, Side side)
        {
            string suffix = side != ArmySelection.LocalSide ? " (Enemy)" : "";
            var counts = units.GroupBy(u => u).ToDictionary(g => g.Key, g => g.Count());
            var seen = new Dictionary<UnitTemplate, int>();
            var names = new List<string>();
            foreach (var u in units)
            {
                seen.TryGetValue(u, out int n);
                seen[u] = ++n;
                names.Add((counts[u] > 1 ? $"{u.displayName} {n}" : u.displayName) + suffix);
            }
            return names;
        }

        /// <summary>Gives each soldier of the unit their type's army builder choices (weapon swaps, equipment), if any.</summary>
        public static void ApplyLoadout(Unit unit, UnitLoadout loadout)
        {
            if (unit == null || loadout == null) return;
            foreach (var s in unit.Soldiers)
            {
                var l = loadout.Get(s.Template);
                if (l != null) s.ApplyLoadout(l);
            }
        }

        /// <summary>Spawns one unit: on the ground, in its faction's colour, with a marker over its leader.</summary>
        public static Unit Spawn(UnitTemplate template, Side side, Vector3 position, Quaternion facing, string name, Transform parent,
            Faction faction = null, UnitLoadout loadout = null)
        {
            var unit = UnitFactory.Spawn(template, side, position, facing, parent, name);
            unit.SetFaction(faction);
            ApplyLoadout(unit, loadout);
            var catalog = UnitCatalog.Load();

            if (catalog != null && (faction != null || side == Side.AI))
                PrimaryColor.Apply(unit.gameObject, catalog.primaryColorMaterial, faction != null ? faction.color : NoFactionAIColor);

            // Stand on the ground (or whatever structure is underfoot).
            foreach (var soldier in unit.Soldiers)
            {
                var p = soldier.transform.position;
                if (Physics.Raycast(new Vector3(p.x, 50f, p.z), Vector3.down, out var hit, 100f, TerrainLayers.StructureMask, QueryTriggerInteraction.Ignore))
                    soldier.transform.position = new Vector3(p.x, hit.point.y, p.z);
            }

            if (unit.Soldiers.Count > 1)
                unit.gameObject.AddComponent<LeaderMarker>().SetMaterial(catalog != null ? catalog.leaderMarker : null);
            return unit;
        }
    }
}
