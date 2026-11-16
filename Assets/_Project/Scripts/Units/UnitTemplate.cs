using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Eldoria.Units
{
    /// <summary>A unit's role on the battlefield (e.g. for building army lists).</summary>
    public enum UnitRole
    {
        Infantry,
        Commander,
        HeavySupport,
        Specialist,
        /// <summary>A transport vehicle (e.g. one with the Transport keyword). Can be taken more than once.</summary>
        Transport,
    }

    public static class UnitRoles
    {
        /// <summary>The role as it's written in the rules: "Heavy Support", not "HeavySupport".</summary>
        /// <summary>Order for listing units: Commanders first, then Infantry, Heavy Support, Specialists and Transports.</summary>
        public static int ListOrder(this UnitRole role) => role switch
        {
            UnitRole.Commander => 0,
            UnitRole.Infantry => 1,
            UnitRole.HeavySupport => 2,
            UnitRole.Transport => 4,
            _ => 3,
        };

        public static string DisplayName(this UnitRole role) => role switch
        {
            UnitRole.HeavySupport => "Heavy Support",
            _ => role.ToString(),
        };
    }

    /// <summary>
    /// Template for a class of unit (e.g. "Infantry Squad"): which soldier templates it is made of, and how many.
    /// The leader leads the unit when it is deployed (see leader succession in the rules).
    /// </summary>
    [CreateAssetMenu(menuName = "Ground Invasion/Unit Template", fileName = "NewUnit", order = 0)]
    public class UnitTemplate : ScriptableObject
    {
        [Serializable]
        public class Member
        {
            public SoldierTemplate soldier;
            [Min(1)] public int count = 1;
        }

        public string displayName = "Unit";

        [Tooltip("The unit's role: Infantry, Commander, Heavy Support or Specialist.")]
        public UnitRole role = UnitRole.Specialist;

        [Tooltip("How the unit plays and what it does in an army (shown in the army builder).")]
        [TextArea(3, 8)] public string description;

        [Tooltip("The soldier who leads the unit when deployed (e.g. a Sergeant).")]
        public SoldierTemplate leader;

        [Tooltip("The rest of the unit.")]
        public List<Member> members = new List<Member>();

        [Tooltip("In the army builder, a swap of standard equipment is one choice for the whole unit (every soldier type " +
                 "with that swap takes it together), instead of a choice per soldier type.")]
        public bool equipmentSwapsForWholeUnit;

        [Tooltip("What the unit costs in an army list. Worked out from its stats by the Supply Cost formula " +
                 "(Rules.SupplyCost) when the unit catalog is rebuilt (Ground Invasion > Rebuild Unit Catalog).")]
        [Min(0)] public int supplyCost;

        [Tooltip("Added to the Supply Cost the formula gives, for a unit whose value the formula doesn't capture " +
                 "(e.g. the Rask Reliquary's aura: +100). 0 for most units.")]
        public int supplyCostAdjustment;

        [Tooltip("If any are set: the unit is hidden in the army builder, and can't be in an army, unless one of these " +
                 "Commanders is in the list (e.g. a unit only a particular Commander brings). Empty: always available.")]
        public List<UnitTemplate> onlyWithCommanders = new List<UnitTemplate>();

        /// <summary>The Commanders that unlock this unit (empty if it doesn't need one).</summary>
        public IEnumerable<UnitTemplate> RequiredCommanders => (onlyWithCommanders ?? new List<UnitTemplate>()).Where(c => c != null);

        /// <summary>True if the unit can only be taken with one of <see cref="onlyWithCommanders"/> in the army.</summary>
        public bool NeedsCommander => RequiredCommanders.Any();

        [Tooltip("Meters between soldiers in the formation they deploy in.")]
        [Min(0.5f)] public float deploymentSpacing = 2f;

        /// <summary>Total soldiers including the leader.</summary>
        public int Size => (leader != null ? 1 : 0) + members.Where(m => m.soldier != null).Sum(m => m.count);

        /// <summary>Every soldier in deployment order: the leader first, then the members.</summary>
        public IEnumerable<SoldierTemplate> Roster()
        {
            if (leader != null) yield return leader;
            foreach (var m in members)
            {
                if (m.soldier == null) continue;
                for (int i = 0; i < m.count; i++) yield return m.soldier;
            }
        }
    }
}
