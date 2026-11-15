using System.Linq;
using Eldoria.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Eldoria.Tests
{
    /// <summary>Models for the Skael and the Sphinxians (Docs/Lore.md): soldier prefabs that no unit uses yet.</summary>
    public class AllianceModelTests
    {
        private static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Units/{name}.prefab");

        [TestCase("SkaelSoldier", 1.2f, 0.33f)]   // a Kobold's size
        [TestCase("SphinxianSoldier", 1.85f, 0.35f)]
        public void TheModelIsASoldierPrefabOfItsRacesSize(string name, float height, float radius)
        {
            var prefab = Load(name);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<Soldier>(), Is.Not.Null);
            var col = prefab.GetComponent<CapsuleCollider>();
            Assert.That((col.height, col.radius), Is.EqualTo((height, radius)));
            Assert.That(prefab.transform.Find("Model").childCount, Is.GreaterThan(20));
            Assert.That(prefab.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterial != null && r.sharedMaterial.name == "Player_Armor"),
                Is.True, "gear in the primary colour (the faction's colour once deployed)");
        }

        [Test]
        public void TheSkaelHaveTailsGraspingFeetAndSilverJewelleryAndTheSphinxiansAPsionicGem()
        {
            var skael = Load("SkaelSoldier").transform.Find("Model");
            Assert.That(skael.Cast<Transform>().Count(t => t.name == "Grasping Toe"), Is.EqualTo(2));
            Assert.That(skael.Cast<Transform>().Count(t => t.name.StartsWith("Tail")), Is.GreaterThanOrEqualTo(4));
            Assert.That(skael.Find("Torc"), Is.Not.Null);
            var sphinxian = Load("SphinxianSoldier").transform.Find("Model");
            Assert.That(sphinxian.Find("Psionic Gem"), Is.Not.Null);
            Assert.That(sphinxian.Cast<Transform>().Count(t => t.name == "Foot Bone"), Is.EqualTo(2), "digitigrade legs");
            var tail = Load("SphinxianSoldier").GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Tail"));
            Assert.That(tail.Min(r => r.bounds.min.y), Is.LessThan(0.15f), "the tail hangs to the ankles");
        }

        [Test]
        public void NoSoldierTypeUsesThemYet()
        {
            var models = new[] { Load("SkaelSoldier"), Load("SphinxianSoldier") };
            foreach (var guid in AssetDatabase.FindAssets("t:SoldierTemplate", new[] { "Assets/_Project/Data" }))
            {
                var s = AssetDatabase.LoadAssetAtPath<SoldierTemplate>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(models.Contains(s.prefab), Is.False, s.name);
            }
        }
    }
}
