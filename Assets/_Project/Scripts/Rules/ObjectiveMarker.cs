using System.Linq;
using Eldoria.Units;
using UnityEngine;

namespace Eldoria.Rules
{
    /// <summary>
    /// An objective on the map (e.g. the centre of Ft Solstice): if a unit leader of the capturing side ends a move standing
    /// on it (within <see cref="Radius"/> of its centre, measured horizontally), that side wins the battle. See
    /// <see cref="Victory"/>. The capturing side is the side that deploys outside (south, <see cref="Side.Player"/>).
    /// </summary>
    public class ObjectiveMarker : MonoBehaviour
    {
        [SerializeField] private string objectiveName = "Objective";
        [SerializeField] private Side capturingSide = Side.Player;
        [SerializeField, Min(0.5f)] private float radius = 2.5f;

        public string ObjectiveName => objectiveName;
        public Side CapturingSide => capturingSide;
        public float Radius => radius;

        public void Configure(string name, Side capturing, float markerRadius)
        {
            objectiveName = name;
            capturingSide = capturing;
            radius = markerRadius;
        }

        /// <summary>True if the point is on the marker (horizontally within its radius).</summary>
        public bool Covers(Vector3 point) => Measure.Horizontal(transform.position, point) <= radius + 0.01f;

        /// <summary>A living unit leader of the capturing side standing on the marker, if any.</summary>
        public Soldier CapturingLeader(System.Collections.Generic.IEnumerable<Unit> units) =>
            units.Where(u => u != null && u.Side == capturingSide && !u.IsDestroyed)
                 .Select(u => u.Leader)
                 .FirstOrDefault(l => l != null && l.IsAlive && Covers(l.Position));

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.05f, radius);
        }
    }
}
