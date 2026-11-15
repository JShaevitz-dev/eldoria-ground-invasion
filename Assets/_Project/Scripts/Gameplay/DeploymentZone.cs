using System.Collections.Generic;
using System.Linq;
using Eldoria.Units;
using UnityEngine;

namespace Eldoria.Gameplay
{
    /// <summary>
    /// The area of the map where one side sets up its army before the battle: a rectangle on the ground, centred on
    /// this object and measured in meters (the army's default slots are laid out in it), plus any further rectangles
    /// the side may deploy in during the deployment phase (e.g. the whole of a fortress).
    /// </summary>
    public class DeploymentZone : MonoBehaviour
    {
        [SerializeField] private Side side = Side.Player;
        [SerializeField] private Vector2 size = new Vector2(80f, 8f);
        [Tooltip("Further areas of the zone, in world meters (x, z of the south-west corner; width, depth).")]
        [SerializeField] private List<Rect> extraAreas = new List<Rect>();
        [Tooltip("The army's default slots must be inside the zone: a unit whose slot isn't (e.g. a back row beyond a " +
                 "fortress wall) takes the nearest spot in the zone where it fits. Otherwise back rows may be behind the zone.")]
        [SerializeField] private bool keepArmyInside;

        public Side Side => side;
        /// <summary>The main rectangle's size (the army's default slots are laid out in it).</summary>
        public Vector2 Size => size;
        /// <summary>The army's default slots must be inside the zone (see the field's tooltip).</summary>
        public bool KeepsArmyInside => keepArmyInside;
        public void SetKeepArmyInside(bool keep) => keepArmyInside = keep;

        public void Configure(Side newSide, Vector2 newSize)
        {
            side = newSide;
            size = newSize;
            extraAreas.Clear();
            keepArmyInside = false;
        }

        /// <summary>Adds a further rectangle (world meters, centred on <paramref name="centre"/>) to the zone.</summary>
        public void AddArea(Vector3 centre, Vector2 areaSize) =>
            extraAreas.Add(new Rect(centre.x - areaSize.x / 2f, centre.z - areaSize.y / 2f, areaSize.x, areaSize.y));

        /// <summary>The main rectangle, in world meters (x, z of the south-west corner; width, depth).</summary>
        public Rect MainArea
        {
            get
            {
                Vector3 c = transform.position;
                return new Rect(c.x - size.x / 2f, c.z - size.y / 2f, size.x, size.y);
            }
        }

        /// <summary>Every rectangle of the zone: the main one, then the extra ones.</summary>
        public IEnumerable<Rect> Areas => new[] { MainArea }.Concat(extraAreas);

        /// <summary>True if a point lies inside the zone (horizontally), in any of its rectangles.</summary>
        public bool Contains(Vector3 point) => Areas.Any(r => Inside(r, point));

        public static bool Inside(Rect r, Vector3 p) => p.x >= r.xMin && p.x <= r.xMax && p.z >= r.yMin && p.z <= r.yMax;

        private void OnDrawGizmos()
        {
            Gizmos.color = side == Side.Player ? new Color(0.3f, 0.6f, 1f, 0.8f) : new Color(1f, 0.3f, 0.25f, 0.8f);
            foreach (var r in Areas)
                Gizmos.DrawWireCube(new Vector3(r.center.x, 0.05f, r.center.y), new Vector3(r.width, 0.1f, r.height));
        }
    }
}
