using Eldoria.Units;
using UnityEditor;
using UnityEngine;
using static Eldoria.EditorTools.SquadBuilder;

namespace Eldoria.EditorTools
{
    /// <summary>
    /// Placeholder soldier models for Alliance races from Docs/Lore.md that have no units yet: the Skael and the Sphinxians.
    /// Each is a soldier prefab (Prefabs/Units) with a Soldier component, a body collider and the primary-colour material on
    /// its gear (the faction's colour once deployed), but no soldier type: no unit uses them yet.
    /// Menu: Eldoria > Build Alliance Models, or -executeMethod Eldoria.EditorTools.AllianceModels.Build. The unit catalog
    /// rebuild makes them if they're missing.
    /// </summary>
    public static class AllianceModels
    {
        private const string Root = "Assets/_Project";
        public const string SkaelPrefab = Root + "/Prefabs/Units/SkaelSoldier.prefab";
        public const string SphinxianPrefab = Root + "/Prefabs/Units/SphinxianSoldier.prefab";

        /// <summary>Skael: the model is the size of a Kobold (1.2 m, radius 0.33), for gameplay (the lore's are smaller).</summary>
        public const float SkaelHeight = 1.2f, SkaelRadius = 0.33f;
        /// <summary>Sphinxian: human height (1.85 m).</summary>
        public const float SphinxianHeight = 1.85f, SphinxianRadius = 0.35f;

        [MenuItem("Eldoria/Build Alliance Models")]
        public static void Build()
        {
            BuildSkael();
            BuildSphinxian();
            AssetDatabase.SaveAssets();
            Debug.Log("[Eldoria] Alliance models built (Skael, Sphinxian).");
        }

        /// <summary>Builds the models that don't exist yet.</summary>
        public static void EnsureBuilt()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SkaelPrefab) == null) BuildSkael();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SphinxianPrefab) == null) BuildSphinxian();
        }

        private static (GameObject root, GameObject model) Begin(string name, float height, float radius, float laidOutFor)
        {
            EnsureFolder(Root, "Prefabs");
            EnsureFolder($"{Root}/Prefabs", "Units");
            var root = new GameObject(name);
            root.AddComponent<Soldier>();   // no soldier type yet
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = radius;
            col.height = height;
            col.center = new Vector3(0f, height / 2f, 0f);
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * (height / laidOutFor);
            return (root, model);
        }

        /// <summary>A capsule from <paramref name="a"/> to <paramref name="b"/> (a limb or tail segment).</summary>
        private static void Segment(GameObject model, string name, Material mat, Vector3 a, Vector3 b, float thickness)
        {
            Vector3 d = b - a;
            Part(model, name, PrimitiveType.Capsule, mat, (a + b) / 2f, new Vector3(thickness, d.magnitude / 2f + thickness / 4f, thickness),
                Quaternion.FromToRotation(Vector3.up, d).eulerAngles);
        }

        private static GameObject Save(GameObject root, string path)
        {
            Eldoria.Terrain.TerrainLayers.SetLayerRecursively(root, Eldoria.Terrain.TerrainLayers.Soldier);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// A Skael soldier (1.2 m, a Kobold's size; the parts are laid out for 0.9 m and scaled up): a small rodent-primate built for the trees. Big round head with large forward-facing
        /// eyes and round ears, a short muzzle, long arms, a long prehensile tail curling up behind, and hand-like feet
        /// with an opposable toe. Skael are nudists whose jewellery shows gender and rank, so in place of a uniform: a
        /// combat harness, belt and small pack (primary colour) and a Silver's jewellery (the defenders' gender): a torc,
        /// armbands and an earring. A compact carbine held across the body.
        /// </summary>
        public static GameObject BuildSkael()
        {
            var armor = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            var fur = GetOrCreateMaterial("Skael_Fur", new Color(0.55f, 0.44f, 0.33f));
            var belly = GetOrCreateMaterial("Skael_Belly", new Color(0.84f, 0.75f, 0.6f));
            var skin = GetOrCreateMaterial("Skael_Skin", new Color(0.86f, 0.62f, 0.56f));
            var eye = GetOrCreateMaterial("Skael_Eye", new Color(0.06f, 0.05f, 0.05f));
            var shine = GetOrCreateMaterial("Eye_Shine", new Color(0.95f, 0.95f, 0.95f));
            var silver = GetOrCreateMaterial("Skael_Silver", new Color(0.82f, 0.84f, 0.88f));
            var metal = GetOrCreateMaterial("Weapon", new Color(0.22f, 0.22f, 0.24f));

            var (root, model) = Begin("SkaelSoldier", SkaelHeight, SkaelRadius, 0.9f);
            foreach (float side in new[] { -1f, 1f })
            {
                // Crouched legs and grasping, hand-like feet: three toes forward, an opposable toe to the inside.
                Part(model, "Thigh", PrimitiveType.Capsule, fur, new Vector3(side * 0.08f, 0.24f, 0.03f), new Vector3(0.07f, 0.09f, 0.07f), new Vector3(-35f, 0f, 0f));
                Part(model, "Shin", PrimitiveType.Capsule, fur, new Vector3(side * 0.08f, 0.11f, 0f), new Vector3(0.05f, 0.08f, 0.05f), new Vector3(15f, 0f, 0f));
                Part(model, "Foot", PrimitiveType.Cube, skin, new Vector3(side * 0.08f, 0.015f, 0.04f), new Vector3(0.07f, 0.03f, 0.09f));
                for (int t = -1; t <= 1; t++)
                    Part(model, "Toe", PrimitiveType.Capsule, skin, new Vector3(side * 0.08f + t * 0.023f, 0.012f, 0.1f), new Vector3(0.016f, 0.028f, 0.016f), new Vector3(90f, 0f, 0f));
                Part(model, "Grasping Toe", PrimitiveType.Capsule, skin, new Vector3(side * 0.08f - side * 0.045f, 0.012f, 0.06f), new Vector3(0.016f, 0.028f, 0.016f), new Vector3(90f, side * -50f, 0f));

                // Long arms reaching forward to the carbine.
                Part(model, "Upper Arm", PrimitiveType.Capsule, fur, new Vector3(side * 0.12f, 0.49f, 0.03f), new Vector3(0.045f, 0.08f, 0.045f), new Vector3(-25f, 0f, 0f));
                Part(model, "Armband", PrimitiveType.Cylinder, silver, new Vector3(side * 0.12f, 0.5f, 0.03f), new Vector3(0.055f, 0.012f, 0.055f), new Vector3(-25f, 0f, 0f));
                Part(model, "Forearm", PrimitiveType.Capsule, fur, new Vector3(side * 0.1f, 0.41f, 0.12f), new Vector3(0.04f, 0.08f, 0.04f), new Vector3(70f, 0f, 0f));
                Part(model, "Hand", PrimitiveType.Sphere, skin, new Vector3(side * 0.07f, 0.4f, 0.2f), Vector3.one * 0.05f);

                // Big forward-facing eyes (with a glint) and round ears.
                Part(model, "Eye", PrimitiveType.Sphere, eye, new Vector3(side * 0.052f, 0.745f, 0.088f), Vector3.one * 0.062f);
                Part(model, "Eye Shine", PrimitiveType.Sphere, shine, new Vector3(side * 0.052f + 0.012f, 0.757f, 0.117f), Vector3.one * 0.014f);
                Part(model, "Ear", PrimitiveType.Cylinder, fur, new Vector3(side * 0.1f, 0.82f, -0.01f), new Vector3(0.1f, 0.01f, 0.1f), new Vector3(90f, 0f, side * 30f));
                Part(model, "Inner Ear", PrimitiveType.Cylinder, skin, new Vector3(side * 0.098f, 0.82f, -0.004f), new Vector3(0.07f, 0.01f, 0.07f), new Vector3(90f, 0f, side * 30f));
            }
            Part(model, "Earring", PrimitiveType.Sphere, silver, new Vector3(0.135f, 0.775f, 0f), Vector3.one * 0.02f);

            Part(model, "Body", PrimitiveType.Capsule, fur, new Vector3(0f, 0.45f, 0f), new Vector3(0.2f, 0.17f, 0.16f), new Vector3(10f, 0f, 0f));
            Part(model, "Belly", PrimitiveType.Sphere, belly, new Vector3(0f, 0.43f, 0.055f), new Vector3(0.14f, 0.2f, 0.08f));
            // The harness: belt, two straps over the chest, pouches and a small pack.
            Part(model, "Belt", PrimitiveType.Cube, armor, new Vector3(0f, 0.33f, 0f), new Vector3(0.21f, 0.035f, 0.17f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(model, "Harness Strap", PrimitiveType.Cube, armor, new Vector3(side * 0.04f, 0.47f, 0.088f), new Vector3(0.028f, 0.25f, 0.012f), new Vector3(10f, 0f, side * 22f));
                Part(model, "Pouch", PrimitiveType.Cube, armor, new Vector3(side * 0.08f, 0.32f, 0.07f), new Vector3(0.05f, 0.05f, 0.04f));
            }
            Part(model, "Pack", PrimitiveType.Cube, armor, new Vector3(0f, 0.48f, -0.1f), new Vector3(0.12f, 0.13f, 0.06f));
            Part(model, "Torc", PrimitiveType.Cylinder, silver, new Vector3(0f, 0.6f, 0.01f), new Vector3(0.13f, 0.012f, 0.12f));

            Part(model, "Head", PrimitiveType.Sphere, fur, new Vector3(0f, 0.72f, 0.02f), new Vector3(0.22f, 0.2f, 0.2f));
            Part(model, "Muzzle", PrimitiveType.Sphere, belly, new Vector3(0f, 0.69f, 0.1f), new Vector3(0.1f, 0.07f, 0.07f));
            Part(model, "Nose", PrimitiveType.Sphere, skin, new Vector3(0f, 0.7f, 0.135f), new Vector3(0.03f, 0.022f, 0.02f));

            // A long prehensile tail, curling up behind.
            Part(model, "Tail", PrimitiveType.Capsule, fur, new Vector3(0f, 0.33f, -0.13f), new Vector3(0.026f, 0.08f, 0.026f), new Vector3(-55f, 0f, 0f));
            Part(model, "Tail", PrimitiveType.Capsule, fur, new Vector3(0f, 0.42f, -0.22f), new Vector3(0.024f, 0.08f, 0.024f), new Vector3(-10f, 0f, 0f));
            Part(model, "Tail", PrimitiveType.Capsule, fur, new Vector3(0f, 0.56f, -0.23f), new Vector3(0.022f, 0.08f, 0.022f), new Vector3(15f, 0f, 0f));
            Part(model, "Tail", PrimitiveType.Capsule, fur, new Vector3(0f, 0.67f, -0.19f), new Vector3(0.02f, 0.06f, 0.02f), new Vector3(60f, 0f, 0f));
            Part(model, "Tail Tip", PrimitiveType.Sphere, fur, new Vector3(0f, 0.7f, -0.13f), Vector3.one * 0.035f);

            // A compact carbine, held across the body.
            Part(model, "Carbine", PrimitiveType.Cube, metal, new Vector3(0.02f, 0.42f, 0.21f), new Vector3(0.035f, 0.045f, 0.32f), new Vector3(0f, -10f, 0f));
            Part(model, "Carbine Magazine", PrimitiveType.Cube, metal, new Vector3(0.025f, 0.38f, 0.2f), new Vector3(0.025f, 0.05f, 0.03f), new Vector3(10f, -10f, 0f));
            return Save(root, SkaelPrefab);
        }

        /// <summary>
        /// A Sphinxian soldier (1.85 m): a hairless, feline-like woman. Bare, wrinkled-pink skin, a cat's head with large
        /// upright ears and almond eyes, digitigrade legs on padded feet, and a long bare tail. Fitted armor (primary colour):
        /// breastplate and backplate, pauldrons, faulds over the hips and plates on the thighs, with a tabard hanging from a
        /// gold belt. All Sphinxians are psionic: a gold circlet with a glowing violet gem on the brow. A rifle held across
        /// the body. The tail hangs to the ankles, its tip curling forward.
        /// </summary>
        public static GameObject BuildSphinxian()
        {
            var armor = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            var skin = GetOrCreateMaterial("Sphinxian_Skin", new Color(0.8f, 0.64f, 0.6f));
            var inner = GetOrCreateMaterial("Sphinxian_Inner", new Color(0.88f, 0.55f, 0.55f));
            var eye = GetOrCreateMaterial("Sphinxian_Eye", new Color(0.72f, 0.82f, 0.22f));
            var pupil = GetOrCreateMaterial("Skael_Eye", new Color(0.06f, 0.05f, 0.05f));
            var gold = GetOrCreateMaterial("Dragon_Gold", new Color(0.93f, 0.71f, 0.22f));
            var psionic = GetOrCreateMaterial("Psionic_Glow", new Color(0.78f, 0.45f, 1f));
            var metal = GetOrCreateMaterial("Weapon", new Color(0.22f, 0.22f, 0.24f));

            var (root, model) = Begin("SphinxianSoldier", SphinxianHeight, SphinxianRadius, 1.85f);
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * 0.12f;
                // Digitigrade legs: padded foot, the long foot bone up to a raised heel, the shin forward to the knee, the thigh.
                Part(model, "Paw", PrimitiveType.Cube, skin, new Vector3(x, 0.035f, 0.1f), new Vector3(0.1f, 0.07f, 0.17f));
                Part(model, "Toes", PrimitiveType.Sphere, skin, new Vector3(x, 0.035f, 0.19f), new Vector3(0.1f, 0.06f, 0.06f));
                Part(model, "Foot Bone", PrimitiveType.Capsule, skin, new Vector3(x, 0.19f, 0.02f), new Vector3(0.09f, 0.16f, 0.09f), new Vector3(-30f, 0f, 0f));
                Part(model, "Shin", PrimitiveType.Capsule, skin, new Vector3(x, 0.47f, 0f), new Vector3(0.12f, 0.17f, 0.12f), new Vector3(22f, 0f, 0f));
                Part(model, "Thigh", PrimitiveType.Capsule, skin, new Vector3(x, 0.79f, 0.03f), new Vector3(0.16f, 0.19f, 0.16f), new Vector3(-14f, 0f, 0f));
                Part(model, "Thigh Plate", PrimitiveType.Cube, armor, new Vector3(x, 0.8f, 0.1f), new Vector3(0.14f, 0.22f, 0.03f), new Vector3(-14f, 0f, 0f));
                Part(model, "Greave", PrimitiveType.Cube, armor, new Vector3(x, 0.47f, 0.065f), new Vector3(0.1f, 0.22f, 0.03f), new Vector3(22f, 0f, 0f));

                // Arms: shoulders, upper arms, forearms forward to the rifle.
                Part(model, "Pauldron", PrimitiveType.Sphere, armor, new Vector3(side * 0.22f, 1.47f, 0f), new Vector3(0.16f, 0.1f, 0.16f));
                Part(model, "Upper Arm", PrimitiveType.Capsule, skin, new Vector3(side * 0.23f, 1.3f, 0.02f), new Vector3(0.1f, 0.15f, 0.1f));
                Part(model, "Forearm", PrimitiveType.Capsule, skin, new Vector3(side * 0.2f, 1.12f, 0.14f), new Vector3(0.09f, 0.14f, 0.09f), new Vector3(72f, 0f, 0f));
                Part(model, "Bracer", PrimitiveType.Cylinder, armor, new Vector3(side * 0.2f, 1.12f, 0.15f), new Vector3(0.1f, 0.06f, 0.1f), new Vector3(72f, 0f, 0f));
                Part(model, "Hand", PrimitiveType.Sphere, skin, new Vector3(side * 0.16f, 1.09f, 0.28f), Vector3.one * 0.08f);

                // A cat's head: large upright ears (pink inside) and almond eyes with slit pupils.
                Part(model, "Ear", PrimitiveType.Cube, skin, new Vector3(side * 0.095f, 1.83f, -0.01f), new Vector3(0.11f, 0.11f, 0.02f), new Vector3(0f, side * -15f, side * 45f));
                Part(model, "Inner Ear", PrimitiveType.Cube, inner, new Vector3(side * 0.095f, 1.83f, 0.002f), new Vector3(0.07f, 0.07f, 0.01f), new Vector3(0f, side * -15f, side * 45f));
                Part(model, "Eye", PrimitiveType.Sphere, eye, new Vector3(side * 0.05f, 1.73f, 0.105f), new Vector3(0.055f, 0.03f, 0.03f), new Vector3(0f, 0f, side * -12f));
                Part(model, "Pupil", PrimitiveType.Cube, pupil, new Vector3(side * 0.05f, 1.73f, 0.12f), new Vector3(0.008f, 0.026f, 0.006f));
            }
            // Hips, waist and chest: a woman's figure under fitted armor.
            Part(model, "Hips", PrimitiveType.Sphere, skin, new Vector3(0f, 0.98f, 0f), new Vector3(0.36f, 0.2f, 0.24f));
            Part(model, "Faulds", PrimitiveType.Cube, armor, new Vector3(0f, 0.95f, 0f), new Vector3(0.38f, 0.16f, 0.26f));
            Part(model, "Belt", PrimitiveType.Cube, gold, new Vector3(0f, 1.04f, 0f), new Vector3(0.36f, 0.04f, 0.25f));
            Part(model, "Tabard", PrimitiveType.Cube, armor, new Vector3(0f, 0.86f, 0.135f), new Vector3(0.15f, 0.3f, 0.015f), new Vector3(-6f, 0f, 0f));
            Part(model, "Tabard Sign", PrimitiveType.Sphere, gold, new Vector3(0f, 0.9f, 0.146f), new Vector3(0.05f, 0.05f, 0.01f));
            Part(model, "Waist", PrimitiveType.Capsule, skin, new Vector3(0f, 1.13f, 0f), new Vector3(0.27f, 0.12f, 0.2f));
            Part(model, "Torso", PrimitiveType.Capsule, skin, new Vector3(0f, 1.32f, 0f), new Vector3(0.32f, 0.2f, 0.22f));
            Part(model, "Breastplate", PrimitiveType.Sphere, armor, new Vector3(0f, 1.33f, 0.04f), new Vector3(0.34f, 0.3f, 0.24f));
            Part(model, "Backplate", PrimitiveType.Cube, armor, new Vector3(0f, 1.34f, -0.08f), new Vector3(0.32f, 0.3f, 0.06f));
            Part(model, "Neck", PrimitiveType.Cylinder, skin, new Vector3(0f, 1.56f, 0.01f), new Vector3(0.11f, 0.06f, 0.11f));

            Part(model, "Head", PrimitiveType.Sphere, skin, new Vector3(0f, 1.71f, 0.02f), new Vector3(0.22f, 0.21f, 0.22f));
            Part(model, "Muzzle", PrimitiveType.Sphere, skin, new Vector3(0f, 1.66f, 0.1f), new Vector3(0.11f, 0.08f, 0.09f));
            Part(model, "Nose", PrimitiveType.Sphere, inner, new Vector3(0f, 1.685f, 0.145f), new Vector3(0.035f, 0.025f, 0.02f));
            // The psionic's circlet: a gold band around the head with a glowing violet gem on the brow.
            Part(model, "Circlet", PrimitiveType.Cylinder, gold, new Vector3(0f, 1.77f, 0.02f), new Vector3(0.205f, 0.012f, 0.215f));
            Part(model, "Psionic Gem", PrimitiveType.Sphere, psionic, new Vector3(0f, 1.78f, 0.125f), Vector3.one * 0.035f);

            // A long, bare tail, out from the base of the spine and hanging to the ankles, the tip curling forward.
            var tail = new[]
            {
                new Vector3(0f, 0.97f, -0.12f), new Vector3(0f, 0.9f, -0.3f), new Vector3(0f, 0.66f, -0.4f),
                new Vector3(0f, 0.38f, -0.43f), new Vector3(0f, 0.14f, -0.4f), new Vector3(0f, 0.07f, -0.3f),
            };
            for (int i = 0; i < tail.Length - 1; i++)
                Segment(model, i == tail.Length - 2 ? "Tail Tip" : "Tail", skin, tail[i], tail[i + 1], Mathf.Lerp(0.045f, 0.028f, i / (tail.Length - 2f)));

            // A rifle held across the body.
            Part(model, "Rifle", PrimitiveType.Cube, metal, new Vector3(0.05f, 1.1f, 0.33f), new Vector3(0.06f, 0.08f, 0.72f), new Vector3(0f, -8f, 0f));
            Part(model, "Rifle Stock", PrimitiveType.Cube, metal, new Vector3(0.1f, 1.08f, 0.0f), new Vector3(0.05f, 0.1f, 0.16f), new Vector3(0f, -8f, 0f));
            Part(model, "Rifle Sight", PrimitiveType.Cylinder, psionic, new Vector3(0.05f, 1.17f, 0.32f), new Vector3(0.04f, 0.06f, 0.04f), new Vector3(90f, 0f, 0f));
            return Save(root, SphinxianPrefab);
        }
    }
}
