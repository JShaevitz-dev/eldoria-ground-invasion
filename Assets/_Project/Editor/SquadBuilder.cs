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
    ///   Factions: the game's factions (Data/Factions), created if missing (see FactionNames)
    ///   SpawnUnit: places a unit from a template in the open scene
    /// There are no units yet: add UnitTemplate assets (with their SoldierTemplates in Data/Soldiers) to Data/Units and
    /// to their factions' unit lists, then run Eldoria > Rebuild Unit Catalog so the army builder lists them.
    /// </summary>
    public static class SquadBuilder
    {
        private const string Root = "Assets/_Project";
        private const string DataDir = Root + "/Data";
        private const string MaterialDir = Root + "/Materials";

        /// <summary>The factions (Data/Factions), in the order the army builder shows them: file name, display name, colour.</summary>
        public static readonly (string file, string name, Color color)[] FactionNames =
        {
            ("Humanity", "International Space Force of Humanity", new Color(0.20f, 0.29f, 0.42f)),                // slate navy blue
            ("DraconisImperialis", "Imperial Army of The Draconis Imperialis", new Color(0.55f, 0.09f, 0.11f)),   // imperial crimson
            ("Rask", "Most Holy Army of The Rask Divine Collective", new Color(0.66f, 0.48f, 0.18f)),             // liturgical ochre/bronze
        };

        /// <summary>
        /// Creates any missing faction from <see cref="FactionNames"/> (existing ones keep their Inspector edits: units,
        /// weapon changes and names). Returns them in table order.
        /// </summary>
        public static List<Faction> EnsureFactions()
        {
            EnsureFolder(Root, "Data");
            EnsureFolder(DataDir, "Factions");
            return FactionNames.Select(f => GetOrCreate<Faction>($"{DataDir}/Factions/{f.file}.asset", a =>
            {
                a.displayName = f.name;
                a.color = f.color;
            })).ToList();
        }

        // ------------------------------------------------------------------ International Space Force of Humanity

        /// <summary>
        /// ISF Rifle Squad (Infantry, ISF): 1 ISF Rifleman Sergeant (leader) + 9 ISF Riflemen.
        ///   ISF Rifleman: Health 5, Armor 2, Accuracy 75%, Move 10 m; Biological; Frag Grenade; Assault Rifle.
        ///   ISF Rifleman Sergeant: the same, plus a Pistol.
        /// (From the original Eldoria's Trooper and Sergeant, with Armor 2.) Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureIsfRifleSquad(Faction isf, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/AssaultRifle.asset");
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/Pistol.asset");
            var grenade = AssetDatabase.LoadAssetAtPath<Ability>($"{DataDir}/Abilities/FragGrenade.asset");
            GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Units/{name}.prefab");

            var rifleman = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/IsfRifleman.asset", s =>
            {
                s.displayName = "ISF Rifleman";
                s.health = 5; s.armorLevel = 2; s.accuracy = 75; s.movementRange = 10f;
                s.keywords = new List<Keyword> { keywords["Biological"] };
                s.abilities = new List<Ability> { grenade };
                s.weapons = new List<WeaponTemplate> { rifle };
                s.primaryWeapon = rifle;
                s.prefab = Model("Trooper");
            });
            var sergeant = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/IsfRiflemanSergeant.asset", s =>
            {
                s.displayName = "ISF Rifleman Sergeant";
                s.health = 5; s.armorLevel = 2; s.accuracy = 75; s.movementRange = 10f;
                s.keywords = new List<Keyword> { keywords["Biological"] };
                s.abilities = new List<Ability> { grenade };
                s.weapons = new List<WeaponTemplate> { rifle, pistol };
                s.primaryWeapon = rifle;
                s.prefab = Model("Sergeant");
            });
            var squad = GetOrCreate<UnitTemplate>($"{DataDir}/Units/IsfRifleSquad.asset", u =>
            {
                u.displayName = "ISF Rifle Squad";
                u.role = UnitRole.Infantry;
                u.leader = sergeant;
                u.members = new List<UnitTemplate.Member> { new UnitTemplate.Member { soldier = rifleman, count = 9 } };
            });
            if (!isf.units.Contains(squad))
            {
                isf.units.Add(squad);
                EditorUtility.SetDirty(isf);
            }
            return squad;
        }

        /// <summary>
        /// Heavy Weapons Team (Heavy Support, ISF), based on the original Eldoria's Machine Gun Team: 1 ISF Rifleman
        /// Sergeant (Pistol) + 3 ISF Riflemen (Assault Rifle, Pistol) + 1 ISF Heavy Weapons (Machine Gun: Damage 5, Shots 5,
        /// Penetration 1, 20 m, Deployed; can swap it for a Rocket Launcher: Damage 5, Shots 1, Penetration 5, 50 m, Manual
        /// Load; or a Flamethrower: Damage 10, Shots 3, no penetration value, 10 m, Incendiary, Unstable). All Health 5,
        /// Armor 2, Accuracy 75%, Move 10 m, Biological; no Frag Grenade. Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureHeavyWeaponsTeam(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/Pistol.asset");
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/AssaultRifle.asset");
            var mg = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/MachineGun.asset");
            var rockets = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/RocketLauncher.asset");
            var flamer = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/Flamethrower.asset");
            GameObject Model(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Units/{name}.prefab");

            void Stats(SoldierTemplate s)
            {
                s.health = 5; s.armorLevel = 2; s.accuracy = 75; s.movementRange = 10f;
                s.keywords = new List<Keyword> { keywords["Biological"] };
                s.abilities = new List<Ability>();
            }

            var sergeant = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/HeavyWeaponsSergeant.asset", s =>
            {
                s.displayName = "ISF Rifleman Sergeant";
                Stats(s);
                s.weapons = new List<WeaponTemplate> { pistol };
                s.primaryWeapon = pistol;
                s.prefab = Model("Sergeant");
            });
            var rifleman = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/HeavyWeaponsRifleman.asset", s =>
            {
                s.displayName = "ISF Rifleman";
                Stats(s);
                s.weapons = new List<WeaponTemplate> { rifle, pistol };
                s.primaryWeapon = rifle;
                s.prefab = Model("Trooper");
            });
            var gunner = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/IsfHeavyWeapons.asset", s =>
            {
                s.displayName = "ISF Heavy Weapons";
                Stats(s);
                s.weapons = new List<WeaponTemplate> { mg };
                s.primaryWeapon = mg;
                s.weaponOptions = new List<WeaponOption> { new WeaponOption { weapon = mg, alternatives = new List<WeaponTemplate> { rockets, flamer } } };
                s.prefab = Model("MachineGunner");
            });
            var team = GetOrCreate<UnitTemplate>($"{DataDir}/Units/HeavyWeaponsTeam.asset", u =>
            {
                u.displayName = "Heavy Weapons Team";
                u.role = UnitRole.HeavySupport;
                u.description = "A heavy fire base built around its ISF Heavy Weapons soldier, who carries one of three weapons. The Machine Gun (5 shots a volley, 5 damage, penetration 1, 20 m) can't fire after moving, so set the team up covering a firing lane and let it hold there. A Rocket Launcher (1 shot, 5 damage, penetration 5, 50 m, once a turn) cracks armor at long range. A Flamethrower (3 shots, 10 damage whatever the armor, 10 m) burns out living soldiers up close, but can't harm machines and may explode when its carrier dies. Three ISF Riflemen with Assault Rifles add firepower out to 20 m and protect the gunner; the ISF Rifleman Sergeant carries only a pistol. With five soldiers and no grenade, it's best covering ground rather than leading an assault. Heavy Support can be taken more than once.";
                u.leader = sergeant;
                u.members = new List<UnitTemplate.Member>
                {
                    new UnitTemplate.Member { soldier = rifleman, count = 3 },
                    new UnitTemplate.Member { soldier = gunner, count = 1 },
                };
                u.deploymentSpacing = 2f;
            });
            if (!faction.units.Contains(team))
            {
                faction.units.Add(team);
                EditorUtility.SetDirty(faction);
            }
            return team;
        }

        /// <summary>
        /// Field Commander (Commander, ISF), recreated from the original Eldoria with Armor 2: 1 Field Commander (Health 10,
        /// Armor 2, Accuracy 75%, Move 10 m; Biological, Officer, Solo; Lead from the front; Pistol). Existing assets keep
        /// their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureFieldCommander(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/Pistol.asset");
            var lead = AssetDatabase.LoadAssetAtPath<Ability>($"{DataDir}/Abilities/LeadFromTheFront.asset");

            var commander = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/FieldCommander.asset", s =>
            {
                s.displayName = "Field Commander";
                s.health = 10; s.armorLevel = 2; s.accuracy = 75; s.movementRange = 10f;
                s.keywords = new List<Keyword> { keywords["Biological"], keywords["Officer"], keywords["Solo"] };
                s.abilities = new List<Ability> { lead };
                s.weapons = new List<WeaponTemplate> { pistol };
                s.primaryWeapon = pistol;
                s.prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Units/FieldCommander.prefab");
            });
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/FieldCommander.asset", u =>
            {
                u.displayName = "Field Commander";
                u.role = UnitRole.Commander;
                u.description = "A lone officer who makes everyone nearby shoot straighter. Lead from the front gives friendly " +
                    "soldiers within 15 m +15% accuracy (the Commander doesn't get it), so keep the Commander just behind your " +
                    "main firing line. With 10 health but only a pistol, the Commander isn't a fighter, and as a Solo unit is a " +
                    "prime target for enemy snipers. A Commander: every army needs exactly one.";
                u.leader = commander;
                u.members = new List<UnitTemplate.Member>();
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        [MenuItem("Eldoria/Rebuild Unit Catalog")]
        public static void RebuildUnitCatalog()
        {
            var keywords = EnsureKeywords();
            var factions = EnsureFactions();
            EnsureFieldCommander(factions[0], keywords);
            EnsureIsfRifleSquad(factions[0], keywords);
            EnsureHeavyWeaponsTeam(factions[0], keywords);
            AssetDatabase.SaveAssets();
            var catalog = EnsureUnitCatalog();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Eldoria] Unit catalog: {catalog.units.Count} unit(s), {catalog.factions.Count} faction(s).");
        }

        /// <summary>
        /// The unit catalog for army lists (Resources/UnitCatalog): every unit in Data/Units, Commanders first, then
        /// Infantry, Heavy Support and Specialists; every faction in Data/Factions (those in FactionNames first, in that
        /// order, then any others by name); and the materials for
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
                    .Where(f => f != null)
                    .OrderBy(f => FactionOrder(f)).ThenBy(f => f.displayName).ToList()
                : new List<Faction>();

            catalog.primaryColorMaterial = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            catalog.leaderMarker = GetOrCreateMaterial("Sergeant_Gold", new Color(0.95f, 0.72f, 0.2f));
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>A faction's place in <see cref="FactionNames"/> (by asset file name); factions not listed come after.</summary>
        private static int FactionOrder(Faction faction)
        {
            int i = System.Array.FindIndex(FactionNames, f => f.file == faction.name);
            return i < 0 ? int.MaxValue : i;
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
