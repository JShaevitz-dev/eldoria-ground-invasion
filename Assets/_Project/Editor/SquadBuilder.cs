using System.Collections.Generic;
using System.Linq;
using Eldoria.Units;
using UnityEditor;
using UnityEngine;

namespace Eldoria.EditorTools
{
    /// <summary>
    /// Editor helpers for the game's data:
    ///   Keywords: every keyword asset (Data/Keywords) + the keyword library (Resources/KeywordLibrary)
    ///   Unit catalog: Resources/UnitCatalog, listing every unit template in Data/Units and every faction in Data/Factions
    ///   SpawnUnit: places a unit from a template in the open scene
    /// This fork has no factions or units yet: add Faction assets to Data/Factions and UnitTemplate assets (with their
    /// SoldierTemplates in Data/Soldiers) to Data/Units, then run Eldoria > Rebuild Unit Catalog so the army builder lists them.
    /// </summary>
    public static class SquadBuilder
    {
        private const string Root = "Assets/_Project";
        private const string DataDir = Root + "/Data";
        private const string MaterialDir = Root + "/Materials";

        [MenuItem("Eldoria/Rebuild Unit Catalog")]
        public static void RebuildUnitCatalog()
        {
            EnsureKeywords();
            var catalog = EnsureUnitCatalog();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Eldoria] Unit catalog: {catalog.units.Count} unit(s), {catalog.factions.Count} faction(s).");
        }

        /// <summary>
        /// The unit catalog for army lists (Resources/UnitCatalog): every unit in Data/Units, Commanders first, then
        /// Infantry, Heavy Support and Specialists; every faction in Data/Factions (by name); and the materials for
        /// deploying units at runtime. The demo army (the builder's starting list) is left as it is.
        /// </summary>
        public static UnitCatalog EnsureUnitCatalog()
        {
            EnsureFolder(Root, "Resources");
            var catalog = GetOrCreate<UnitCatalog>($"{Root}/Resources/{UnitCatalog.ResourcePath}.asset", c => { });
            catalog.units = AssetDatabase.FindAssets("t:UnitTemplate", new[] { $"{DataDir}/Units" })
                .Select(g => AssetDatabase.LoadAssetAtPath<UnitTemplate>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(u => u != null)
                .OrderBy(u => u.role.ListOrder()).ThenBy(u => u.displayName).ToList();
            catalog.defaultArmy = catalog.defaultArmy.Where(u => u != null && catalog.units.Contains(u)).ToList();
            catalog.factions = AssetDatabase.IsValidFolder($"{DataDir}/Factions")
                ? AssetDatabase.FindAssets("t:Faction", new[] { $"{DataDir}/Factions" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<Faction>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(f => f != null).OrderBy(f => f.displayName).ToList()
                : new List<Faction>();

            catalog.primaryColorMaterial = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            catalog.leaderMarker = GetOrCreateMaterial("Sergeant_Gold", new Color(0.95f, 0.72f, 0.2f));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>
        /// Spawns a unit from an existing template asset at any spot in the open scene (keeping prefab links).
        /// AI units are recoloured red; units of more than one soldier get the floating leader marker.
        /// Soldiers are placed on the ground below them.
        /// </summary>
        public static Unit SpawnUnit(UnitTemplate template, Side side, Vector3 position, Quaternion facing, string unitName)
        {
            var unit = UnitFactory.Spawn(template, side, position, facing, unitName: unitName,
                instantiate: (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent));

            if (side == Side.AI)
            {
                var blue = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
                var red = GetOrCreateMaterial("Enemy_Armor", new Color(0.62f, 0.16f, 0.14f));
                foreach (var r in unit.GetComponentsInChildren<Renderer>(true))
                    if (r.sharedMaterial == blue) r.sharedMaterial = red;
            }

            foreach (var soldier in unit.Soldiers)
            {
                var p = soldier.transform.position;
                if (Physics.Raycast(new Vector3(p.x, 50f, p.z), Vector3.down, out var hit, 100f,
                        Eldoria.Terrain.TerrainLayers.StructureMask, QueryTriggerInteraction.Ignore))
                    soldier.transform.position = new Vector3(p.x, hit.point.y, p.z);
            }

            if (unit.Soldiers.Count > 1)
            {
                var marker = unit.gameObject.AddComponent<LeaderMarker>();
                var markerSo = new SerializedObject(marker);
                markerSo.FindProperty("material").objectReferenceValue = GetOrCreateMaterial("Sergeant_Gold", new Color(0.95f, 0.72f, 0.2f));
                markerSo.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(unit);
            return unit;
        }

        /// <summary>Creates (or loads) every keyword asset and the keyword library. Returns them by name.</summary>
        public static Dictionary<string, Keyword> EnsureKeywords()
        {
            EnsureFolder(Root, "Data");
            EnsureFolder(DataDir, "Keywords");

            Keyword Kw(string file, string name, KeywordTarget target, string description) =>
                GetOrCreate<Keyword>($"{DataDir}/Keywords/{file}.asset", k =>
                {
                    k.displayName = name;
                    k.appliesTo = target;
                    k.description = description;
                });
            Kw("Explosive", "Explosive", KeywordTarget.Weapon, "An explosive weapon. A trait for other rules to check.");
            Kw("Incendiary", "Incendiary", KeywordTarget.Weapon, "Only damages targets with the Biological keyword.");
            Kw("Robotic", "Robotic", KeywordTarget.Soldier, "A machine rather than a person. A trait for other rules to check.");
            Kw("Biological", "Biological", KeywordTarget.Soldier, "A living being. Every living soldier has this keyword.");
            GetOrCreate<Keyword>($"{DataDir}/Keywords/Toxic.asset", k =>
            {
                k.displayName = "Toxic";
                k.appliesTo = KeywordTarget.Weapon;
                k.description = "Only harms living beings: no effect on a target that isn't Biological.";
                k.interactions = new List<KeywordInteraction>
                {
                    new KeywordInteraction
                    {
                        party = KeywordParty.Target, condition = KeywordCondition.Lacks, keyword = AssetDatabase.LoadAssetAtPath<Keyword>($"{DataDir}/Keywords/Biological.asset"),
                        effect = KeywordEffect.NoEffect
                    }
                };
            });

            Kw("Moved", "Moved", KeywordTarget.Soldier, "Gained by a soldier who moves; removed at the start of the next turn.");

            // Cover: a Half-cover object grants In half-cover to soldiers within 5 m; attacks on them take −25%
            // accuracy if that object is between attacker and target.
            var inHalfCover = GetOrCreate<Keyword>($"{DataDir}/Keywords/InHalfCover.asset", k =>
            {
                k.displayName = "In half-cover";
                k.appliesTo = KeywordTarget.Soldier;
                k.description = "Within 5 m of half-cover. Attacks against this soldier take −25% accuracy if the cover is between them.";
                k.interactions = new List<KeywordInteraction>
                {
                    new KeywordInteraction
                    {
                        role = KeywordRole.WhenDefending, condition = KeywordCondition.Always,
                        effect = KeywordEffect.AccuracyPenalty, amount = 25, onlyIfSourceIsBetween = true
                    }
                };
            });
            // Full cover: a soldier fully concealed behind the cover can't be targeted by a shooting attack.
            var inFullCover = GetOrCreate<Keyword>($"{DataDir}/Keywords/InFullCover.asset", k =>
            {
                k.displayName = "In full cover";
                k.appliesTo = KeywordTarget.Soldier;
                k.description = "Fully concealed behind cover: can't be targeted by a shooting attack while the cover is between them.";
                k.interactions = new List<KeywordInteraction>
                {
                    new KeywordInteraction
                    {
                        role = KeywordRole.WhenDefending, condition = KeywordCondition.Always,
                        effect = KeywordEffect.CannotBeTargeted, onlyIfSourceIsBetween = true
                    }
                };
            });
            var fullCover = GetOrCreate<Keyword>($"{DataDir}/Keywords/FullCover.asset", k =>
            {
                k.displayName = "Full-cover";
                k.appliesTo = KeywordTarget.TerrainObject;
                k.grantsNearby = inFullCover;
            });
            // Full cover works at any distance: it only has to be between the shooter and the target.
            fullCover.description = "A soldier is In full cover against any attacker this object is between, however far either is from it.";
            fullCover.grantsWhenBetween = true;
            EditorUtility.SetDirty(fullCover);
            inFullCover.description = "Fully concealed behind cover: can't be targeted by a shooting attack. Applies whenever a Full-cover object is between the attacker and the soldier.";
            EditorUtility.SetDirty(inFullCover);
            GetOrCreate<Keyword>($"{DataDir}/Keywords/IndirectFire.asset", k =>
            {
                k.displayName = "Indirect fire";
                k.appliesTo = KeywordTarget.Weapon;
                k.description = "Fires over obstacles: ignores line of sight (including cover) and all accuracy modifiers.";
                k.interactions = new List<KeywordInteraction>
                {
                    new KeywordInteraction { condition = KeywordCondition.Always, effect = KeywordEffect.IgnoresLineOfSight },
                    new KeywordInteraction { condition = KeywordCondition.Always, effect = KeywordEffect.IgnoresAccuracyModifiers },
                };
            });
            GetOrCreate<Keyword>($"{DataDir}/Keywords/HalfCover.asset", k =>
            {
                k.displayName = "Half-cover";
                k.appliesTo = KeywordTarget.TerrainObject;
                k.description = "Soldiers within 5 m gain In half-cover.";
                k.grantsNearby = inHalfCover;
                k.grantRadius = 5f;
            });

            Kw("Officer", "Officer", KeywordTarget.Soldier, "A commissioned officer. (Effect not designed yet.)");
            Kw("Solo", "Solo", KeywordTarget.Soldier, "Fights as a unit of one. No effect on its own; some weapons and abilities check for it.");
            Kw("Magic", "Magic", KeywordTarget.Soldier, "Wields magic. No effect on its own; some weapons and abilities check for it.");
            Kw("Spell", "Spell", KeywordTarget.Ability, "A magical ability. Can't be cast by a soldier whose unit sprinted this turn.");
            Kw("Pistol", "Pistol", KeywordTarget.Weapon, "A sidearm: the only kind of weapon a soldier in close range can use.");
            Kw("SniperPrecision", "Sniper Precision", KeywordTarget.Soldier,
                "When this soldier shoots, the player picks the specific enemy soldier they target instead of the closest one.");
            Kw("Deployed", "Deployed", KeywordTarget.Weapon, "This weapon can't be used in a volley if the soldier holding it has moved this turn.");
            Kw("InCloseRange", "In close range", KeywordTarget.Soldier,
                "A Biological enemy is less than 2 m away: can only use weapons with the Pistol keyword (spells and other abilities still work).");
            GetOrCreate<Keyword>($"{DataDir}/Keywords/DivineSanctuary.asset", k =>
            {
                k.displayName = "Divine Sanctuary";
                k.appliesTo = KeywordTarget.Soldier;
                k.description = "Blessed by a Battle Priest: +1 Armor Level.";
                k.interactions = new List<KeywordInteraction>
                {
                    new KeywordInteraction
                    {
                        role = KeywordRole.WhenDefending, condition = KeywordCondition.Always,
                        effect = KeywordEffect.ArmorBonus, amount = 1
                    }
                };
            }, k =>
            {
                // Blessed soldiers glitter gold while the spell lasts.
                k.sparkles = true;
                k.sparkleColor = new Color(1f, 0.82f, 0.25f, 1f);
            });
            GetOrCreate<Keyword>($"{DataDir}/Keywords/LedFromTheFront.asset", k =>
            {
                k.displayName = "Led from the front";
                k.appliesTo = KeywordTarget.Soldier;
                k.description = "Within 15 m of a friendly Field Commander (not the commander themself): +15% accuracy.";
                k.interactions = new List<KeywordInteraction>
                {
                    new KeywordInteraction
                    {
                        role = KeywordRole.WhenAttacking, condition = KeywordCondition.Always,
                        effect = KeywordEffect.AccuracyBonus, amount = 15
                    }
                };
            });

            // Categories: Traits are what a soldier is; Statuses come and go during play.
            var categories = new Dictionary<string, KeywordCategory>
            {
                ["Biological"] = KeywordCategory.Trait, ["Robotic"] = KeywordCategory.Trait,
                ["Officer"] = KeywordCategory.Trait, ["Solo"] = KeywordCategory.Trait, ["Magic"] = KeywordCategory.Trait,
                ["Spell"] = KeywordCategory.Trait, ["Pistol"] = KeywordCategory.Trait, ["Deployed"] = KeywordCategory.Trait,
                ["Mechanical"] = KeywordCategory.Trait, ["Tank"] = KeywordCategory.Trait, ["Manual Load"] = KeywordCategory.Trait, ["Melee"] = KeywordCategory.Trait, ["Sniper Precision"] = KeywordCategory.Trait, ["In close range"] = KeywordCategory.Status,
                ["Divine Sanctuary"] = KeywordCategory.Status,
                ["Moved"] = KeywordCategory.Status, ["In half-cover"] = KeywordCategory.Status,
                ["In full cover"] = KeywordCategory.Status, ["Led from the front"] = KeywordCategory.Status,
            };
            foreach (var guid in AssetDatabase.FindAssets("t:Keyword", new[] { $"{DataDir}/Keywords" }))
            {
                var k = AssetDatabase.LoadAssetAtPath<Keyword>(AssetDatabase.GUIDToAssetPath(guid));
                if (k != null && k.category == KeywordCategory.None && categories.TryGetValue(k.displayName, out var cat))
                {
                    k.category = cat;
                    EditorUtility.SetDirty(k);
                }
            }

            // --- Keyword library (lets rules find keywords like "Moved" by name at runtime) ---
            EnsureFolder(Root, "Resources");
            var library = GetOrCreate<KeywordLibrary>($"{Root}/Resources/{KeywordLibrary.ResourcePath}.asset", l => { });
            library.keywords = AssetDatabase.FindAssets("t:Keyword", new[] { $"{DataDir}/Keywords" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Keyword>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(k => k != null).OrderBy(k => k.displayName).ToList();
            EditorUtility.SetDirty(library);

            return library.keywords.ToDictionary(k => k.displayName);
        }


        /// <summary>Loads an asset, or creates it with <paramref name="init"/>. <paramref name="upgrade"/> runs on existing assets.</summary>
        private static T GetOrCreate<T>(string path, System.Action<T> init, System.Action<T> upgrade = null) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                if (upgrade != null)
                {
                    upgrade(asset);
                    EditorUtility.SetDirty(asset);
                }
                return asset; // keep any tweaks made in the inspector
            }
            asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            upgrade?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.35f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
