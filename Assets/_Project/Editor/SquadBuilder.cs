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

        /// <summary>
        /// Mortar Launcher: the Frag Grenade weapon's stats (Damage 5, Shots 1, +0%, Explosive), but Penetration 3, Range
        /// 50 m, and Deployed and Manual Load too. A weapon fired at a soldier like any other: the grenade's 2.5 m blast comes
        /// from the Frag Grenade ability, not its weapon. Carried by the ISF Mortar Team's Mortar Specialist.
        /// </summary>
        public static WeaponTemplate EnsureMortarLauncher(Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Weapons");
            return GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/MortarLauncher.asset", w =>
            {
                w.displayName = "Mortar Launcher";
                w.baseDamage = 5; w.shots = 1;
                w.hasPenetration = true; w.penetration = 3;
                w.accuracyBonus = 0;
                w.range = 50f;
                w.keywords = new List<Keyword> { keywords["Deployed"], keywords["Manual Load"], keywords["Explosive"] };
            });
        }

        /// <summary>
        /// Mortar Team (Heavy Support, ISF): 1 ISF Mortar Specialist (leader; the ISF Rifleman's stats: Health 5, Armor 2,
        /// Accuracy 75%, Move 10 m, Biological; Mortar Launcher (primary) and Assault Rifle) + 1 ISF Rifleman (the ISF Rifle
        /// Squad's: Assault Rifle, Frag Grenade). Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureMortarTeam(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/AssaultRifle.asset");
            var mortar = EnsureMortarLauncher(keywords);
            var rifleman = AssetDatabase.LoadAssetAtPath<SoldierTemplate>($"{DataDir}/Soldiers/IsfRifleman.asset");
            var specialist = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/IsfMortarSpecialist.asset", s =>
            {
                s.displayName = "ISF Mortar Specialist";
                s.health = 5; s.armorLevel = 2; s.accuracy = 75; s.movementRange = 10f;
                s.keywords = new List<Keyword> { keywords["Biological"] };
                s.abilities = new List<Ability>();
                s.weapons = new List<WeaponTemplate> { mortar, rifle };
                s.primaryWeapon = mortar;   // the Assault Rifle when the mortar can't fire (after moving, or already fired)
                s.prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Units/RocketTrooper.prefab");
            });
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/MortarTeam.asset", u =>
            {
                u.displayName = "Mortar Team";
                u.role = UnitRole.HeavySupport;
                u.leader = specialist;
                u.members = new List<UnitTemplate.Member> { new UnitTemplate.Member { soldier = rifleman, count = 1 } };
                u.deploymentSpacing = 2f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>
        /// Anti-Materiel Rifle: the Sniper Rifle's stats (Damage 10), but no penetration value ("X": always full damage,
        /// whatever the armor) and no accuracy bonus. Damage 10, Shots 1, Penetration X, +0%, Range 50 m; Deployed. The ISF
        /// Ranger can swap its Sniper Rifle for it.
        /// </summary>
        /// <summary>Shotgun: Damage 10, Shots 1, Penetration 1, Accuracy Bonus 0, Range 10 m; no keywords. The ISF Ranger can
        /// swap its Sniper Rifle for it.</summary>
        public static WeaponTemplate EnsureShotgun()
        {
            EnsureFolder(DataDir, "Weapons");
            return GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/Shotgun.asset", w =>
            {
                w.displayName = "Shotgun";
                w.baseDamage = 10; w.shots = 1;
                w.hasPenetration = true; w.penetration = 1;
                w.accuracyBonus = 0;
                w.range = 10f;
                w.keywords = new List<Keyword>();
            });
        }

        public static WeaponTemplate EnsureAntiMaterielRifle(Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Weapons");
            return GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/AntiMaterielRifle.asset", w =>
            {
                w.displayName = "Anti-Materiel Rifle";
                w.baseDamage = 10; w.shots = 1;
                w.hasPenetration = false; w.penetration = 0;
                w.accuracyBonus = 0;
                w.range = 50f;
                w.keywords = new List<Keyword> { keywords["Deployed"] };
            });
        }

        /// <summary>
        /// ISF Ranger (Specialist, ISF), the original Eldoria's Sniper renamed, with Armor 1: 1 ISF Ranger (Health 5, Armor 1, Accuracy
        /// 100%, Move 15 m; Biological, Sniper Precision, Solo; Sniper Rifle: Damage 10, Shots 1, Penetration 1, +50%
        /// accuracy, 50 m, Deployed; can swap it for the Anti-Materiel Rifle or the Shotgun); Chromashift Textiles (passive: attacks against
        /// it take -25% accuracy). Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureIsfRanger(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/SniperRifle.asset");
            var amr = EnsureAntiMaterielRifle(keywords);
            var shotgun = EnsureShotgun();
            EnsureFolder(DataDir, "Abilities");
            var chromashift = GetOrCreate<Ability>($"{DataDir}/Abilities/ChromashiftTextiles.asset", a =>
            {
                a.displayName = "Chromashift Textiles";
                a.description = "Passive. Attacks against this soldier take −25% accuracy.";
                a.kind = AbilityKind.Passive;
                a.grantsNearby = keywords["Chromashift"];
                a.radius = 0f;               // the wearer only
                a.affects = AuraAffects.Allies;
                a.includeSelf = true;
            });
            var ranger = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/IsfRanger.asset", s =>
            {
                s.displayName = "ISF Ranger";
                s.health = 5; s.armorLevel = 1; s.accuracy = 100; s.movementRange = 15f;
                s.keywords = new List<Keyword> { keywords["Biological"], keywords["Sniper Precision"], keywords["Solo"] };
                s.abilities = new List<Ability> { chromashift };
                s.weapons = new List<WeaponTemplate> { rifle };
                s.primaryWeapon = rifle;
                s.weaponOptions = new List<WeaponOption> { new WeaponOption { weapon = rifle, alternatives = new List<WeaponTemplate> { amr, shotgun } } };
                s.prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Units/Sniper.prefab");
            }, s =>
            {
                // Added later: an existing Ranger gets Chromashift Textiles too (once).
                if (!s.abilities.Contains(chromashift)) s.abilities.Add(chromashift);
                // Added later: the Shotgun as another choice for the Sniper Rifle (once).
                var option = s.weaponOptions.FirstOrDefault(o => o != null && o.weapon == rifle);
                if (option != null && !option.alternatives.Contains(shotgun)) option.alternatives.Add(shotgun);
            });
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/IsfRanger.asset", u =>
            {
                u.displayName = "ISF Ranger";
                u.role = UnitRole.Specialist;
                u.description = "A lone marksman who picks off key enemies from up to 50 m. At 150% accuracy every shot hits, with a 50% chance to crit, and you choose exactly which enemy soldier to shoot: ideal for removing sergeants and officers. Or swap the Sniper Rifle for an Anti-Materiel Rifle (10 damage that ignores armor, but without the +50% aim) or a Shotgun (10 damage up close, out to 10 m, and it can fire after moving). The two rifles can't fire after moving, and with 5 health and only light armor the Ranger must stay out of sight, though its Chromashift Textiles give anyone aiming at it -25% accuracy; moving 15 m, they can reposition quickly between shots. A Specialist and a Solo unit: one per army.";
                u.leader = ranger;
                u.members = new List<UnitTemplate.Member>();
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>
        /// Sagittarius MBT (Heavy Support, ISF), the original Eldoria's Medium Tank renamed: 1 Sagittarius MBT (Health 20,
        /// Armor 4, Accuracy 75%, Move 30 m; Mechanical (can't sprint), Tank, Unstable; Frag Grenade usable every turn;
        /// 100mm Cannon: Damage 10, Shots 1, Penetration 4, 50 m, Manual Load). A wide body (radius 1.8 m). Existing assets
        /// keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureSagittariusMbt(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var cannon = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/Cannon100mm.asset");
            var grenade = AssetDatabase.LoadAssetAtPath<Ability>($"{DataDir}/Abilities/TankFragGrenade.asset");
            var tank = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/SagittariusMbt.asset", s =>
            {
                s.displayName = "Sagittarius MBT";
                s.health = 20; s.armorLevel = 4; s.accuracy = 75; s.movementRange = 30f;
                s.keywords = new List<Keyword> { keywords["Mechanical"], keywords["Tank"], keywords["Unstable"] };
                s.abilities = new List<Ability> { grenade };
                s.weapons = new List<WeaponTemplate> { cannon };
                s.primaryWeapon = cannon;
                s.radius = 1.8f;
                s.height = 2.3f;
                s.prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Units/MediumTank.prefab");
            });
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/SagittariusMbt.asset", u =>
            {
                u.displayName = "Sagittarius MBT";
                u.role = UnitRole.HeavySupport;
                u.description = "A lone armored vehicle and the hardest target on the field: 20 health and Armor Level 4 shrug off rifles, machine guns, grenades and flamethrowers alike. Its 100mm Cannon (10 damage, penetration 4, 50 m) is the only weapon that hurts heavy armor, but it's Manual Load: one shot a turn. It moves a long way (30 m) but, being Mechanical, can't sprint. When destroyed it has a 10% chance to explode (Unstable), so keep your own soldiers clear of it. Heavy Support, so you can field more than one.";
                u.leader = tank;
                u.members = new List<UnitTemplate.Member>();
                u.deploymentSpacing = 2f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>
        /// Olympian Mech (Heavy Support, ISF), the original Eldoria's Stahl Colossus renamed: 1 Olympian Mech (Health 15,
        /// Armor 3, Accuracy 75%, Move 15 m; Solo, Mechanical; Iron Claw (active: an aimed melee strike) and Stabilizers
        /// (passive: nullifies Deployed); Machine Gun (primary) and Iron Claw). Radius 0.7 m, 3.6 m tall. Existing assets keep
        /// their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureOlympianMech(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var mg = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/MachineGun.asset");
            var claw = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/IronClaw.asset");
            var clawStrike = AssetDatabase.LoadAssetAtPath<Ability>($"{DataDir}/Abilities/IronClaw.asset");
            var stabilizers = AssetDatabase.LoadAssetAtPath<Ability>($"{DataDir}/Abilities/Stabilizers.asset");
            var mech = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/OlympianMech.asset", s =>
            {
                s.displayName = "Olympian Mech";
                s.health = 15; s.armorLevel = 3; s.accuracy = 75; s.movementRange = 15f;
                s.keywords = new List<Keyword> { keywords["Solo"], keywords["Mechanical"] };
                s.abilities = new List<Ability> { clawStrike, stabilizers };
                s.weapons = new List<WeaponTemplate> { mg, claw };
                s.primaryWeapon = mg;
                s.radius = 0.7f;
                s.height = 3.6f;
                s.prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Units/StahlColossus.prefab");
            });
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/OlympianMech.asset", u =>
            {
                u.displayName = "Olympian Mech";
                u.role = UnitRole.HeavySupport;
                u.description = "The ISF's walking war machine: a humanoid mech twice a soldier's height, with 15 health and Armor Level 3 that rifles, pistols and grenades can't dent. Its Stabilizers let its Machine Gun (5 shots) fire even after moving, and when an enemy is within 2 m it can strike with its Iron Claw instead (Damage 10, +25% accuracy). Mechanical: it moves 15 m but can't sprint. Rockets and tank cannons are its bane. Heavy Support, so you can field more than one.";
                u.leader = mech;
                u.members = new List<UnitTemplate.Member>();
                u.deploymentSpacing = 2f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        // ------------------------------------------------------------------ Imperial Army of The Draconis Imperialis

        /// <summary>Dragon Claw: Damage 5, Shots 1, Penetration 1, Accuracy Bonus 0, Range 2 m; Melee, Natural.</summary>
        public static WeaponTemplate EnsureDragonClaw(Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Weapons");
            return GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/DragonClaw.asset", w =>
            {
                w.displayName = "Dragon Claw";
                w.baseDamage = 5; w.shots = 1;
                w.hasPenetration = true; w.penetration = 1;
                w.accuracyBonus = 0;
                w.range = 2f;   // like the game's other melee weapons
                w.keywords = new List<Keyword> { keywords["Melee"], keywords["Natural"] };
            });
        }

        /// <summary>
        /// Dragon Hierarch (Commander, Draconis Imperialis): 1 Dragon Hierarch (Health 15, Armor 2, Accuracy 50%, Move 30 m;
        /// Biological, Large; Dragon Claw). A large quadrupedal dragon with wings and golden armor. Its footprint (radius 1 m)
        /// is smaller than the model, so its 2 m claw can reach a soldier standing next to it (reach is measured between
        /// soldiers' centres). Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureDragonHierarch(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var claw = EnsureDragonClaw(keywords);
            var dragon = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/DragonHierarch.asset", s =>
            {
                s.displayName = "Dragon Hierarch";
                s.health = 15; s.armorLevel = 2; s.accuracy = 50; s.movementRange = 30f;
                s.keywords = new List<Keyword> { keywords["Biological"], keywords["Large"] };
                s.abilities = new List<Ability>();
                s.weapons = new List<WeaponTemplate> { claw };
                s.primaryWeapon = claw;
                s.radius = 1.0f;
                s.height = 3.6f;
            });
            if (dragon.prefab == null)
            {
                dragon.prefab = BuildDragonPrefab("DragonHierarch", dragon);
                EditorUtility.SetDirty(dragon);
            }
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/DragonHierarch.asset", u =>
            {
                u.displayName = "Dragon Hierarch";
                u.role = UnitRole.Commander;
                u.leader = dragon;
                u.members = new List<UnitTemplate.Member>();
                u.deploymentSpacing = 4f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>
        /// A placeholder dragon built from primitives, facing +z: a quadrupedal body with a long neck and tail, four legs,
        /// two raised wings and golden armor plates. Body and wings use the primary colour material (tinted with the
        /// faction's colour when deployed); the armor is gold.
        /// </summary>
        private static GameObject BuildDragonPrefab(string name, SoldierTemplate template)
        {
            EnsureFolder(Root, "Prefabs");
            EnsureFolder($"{Root}/Prefabs", "Units");
            var hide = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            var gold = GetOrCreateMaterial("Dragon_Gold", new Color(0.93f, 0.71f, 0.22f));
            var horn = GetOrCreateMaterial("Dragon_Horn", new Color(0.86f, 0.82f, 0.7f));
            var eye = GetOrCreateMaterial("Dragon_Eye", new Color(1f, 0.6f, 0.05f));
            float h = template.height;

            var root = new GameObject(name);
            var soldier = root.AddComponent<Soldier>();
            var sso = new SerializedObject(soldier);
            sso.FindProperty("template").objectReferenceValue = template;
            sso.FindProperty("isSergeant").boolValue = false;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = template.radius;
            col.height = h;
            col.center = new Vector3(0f, h / 2f, 0f);

            // Body: a long barrel on four legs, belly about 1.2 m up.
            Part(root, "Body", PrimitiveType.Capsule, hide, new Vector3(0f, 1.75f, 0f), new Vector3(1.5f, 1.6f, 1.5f), new Vector3(90f, 0f, 0f));
            Part(root, "Chest Plate", PrimitiveType.Cube, gold, new Vector3(0f, 1.6f, 1.1f), new Vector3(1.2f, 1.0f, 0.5f), new Vector3(-20f, 0f, 0f));
            Part(root, "Back Plate", PrimitiveType.Cube, gold, new Vector3(0f, 2.45f, -0.1f), new Vector3(1.1f, 0.18f, 2.0f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root, "Flank Plate", PrimitiveType.Cube, gold, new Vector3(side * 0.74f, 1.85f, 0f), new Vector3(0.08f, 0.7f, 1.8f));
                foreach (float z in new[] { 0.95f, -0.95f })
                {
                    Part(root, "Leg", PrimitiveType.Cube, hide, new Vector3(side * 0.55f, 0.75f, z), new Vector3(0.4f, 1.5f, 0.45f));
                    Part(root, "Greave", PrimitiveType.Cube, gold, new Vector3(side * 0.55f, 0.95f, z + 0.24f), new Vector3(0.44f, 0.6f, 0.06f));
                    Part(root, "Foot", PrimitiveType.Cube, hide, new Vector3(side * 0.55f, 0.1f, z + 0.15f), new Vector3(0.5f, 0.2f, 0.7f));
                    foreach (float x in new[] { -0.15f, 0f, 0.15f })
                        Part(root, "Claw", PrimitiveType.Cube, horn, new Vector3(side * 0.55f + x, 0.06f, z + 0.58f), new Vector3(0.07f, 0.08f, 0.2f));
                }
                // Wings: raised and swept back from the shoulders.
                Part(root, "Wing Arm", PrimitiveType.Cube, hide, new Vector3(side * 1.1f, 2.9f, 0.3f), new Vector3(1.8f, 0.14f, 0.18f), new Vector3(0f, side * 20f, side * 35f));
                Part(root, "Wing", PrimitiveType.Cube, hide, new Vector3(side * 1.25f, 2.75f, -0.35f), new Vector3(2.0f, 0.04f, 1.5f), new Vector3(0f, side * 20f, side * 35f));
            }
            // Neck and head, with a golden crown and horns.
            Part(root, "Neck", PrimitiveType.Capsule, hide, new Vector3(0f, 2.55f, 1.7f), new Vector3(0.6f, 0.75f, 0.6f), new Vector3(35f, 0f, 0f));
            Part(root, "Neck Plate", PrimitiveType.Cube, gold, new Vector3(0f, 2.75f, 1.55f), new Vector3(0.5f, 0.1f, 0.8f), new Vector3(-55f, 0f, 0f));
            Part(root, "Head", PrimitiveType.Cube, hide, new Vector3(0f, 3.15f, 2.2f), new Vector3(0.6f, 0.5f, 0.9f));
            Part(root, "Snout", PrimitiveType.Cube, hide, new Vector3(0f, 3.02f, 2.75f), new Vector3(0.42f, 0.3f, 0.5f));
            Part(root, "Crown", PrimitiveType.Cube, gold, new Vector3(0f, 3.43f, 2.1f), new Vector3(0.66f, 0.1f, 0.5f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root, "Horn", PrimitiveType.Cube, horn, new Vector3(side * 0.22f, 3.55f, 1.85f), new Vector3(0.08f, 0.45f, 0.08f), new Vector3(-40f, 0f, side * 12f));
                Part(root, "Eye", PrimitiveType.Cube, eye, new Vector3(side * 0.31f, 3.2f, 2.45f), new Vector3(0.03f, 0.08f, 0.14f));
            }
            // Tail, tapering behind.
            Part(root, "Tail", PrimitiveType.Capsule, hide, new Vector3(0f, 1.5f, -2.0f), new Vector3(0.45f, 0.9f, 0.45f), new Vector3(-70f, 0f, 0f));
            Part(root, "Tail Tip", PrimitiveType.Capsule, hide, new Vector3(0f, 1.0f, -3.0f), new Vector3(0.25f, 0.6f, 0.25f), new Vector3(-80f, 0f, 0f));

            Eldoria.Terrain.TerrainLayers.SetLayerRecursively(root, Eldoria.Terrain.TerrainLayers.Soldier);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Units/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Kobold Militia (Infantry, Draconis Imperialis): 1 Kobold Sergeant (leader) + 19 Kobold Militiamen. All Health 5,
        /// Armor 0, Accuracy 50%, Move 20 m; Biological, Swarm (cohesion 10 m); Assault Rifle, which the Draconis Imperialis
        /// calls the "Fang Caster" (see <see cref="EnsureDraconisWeaponNames"/>). Kobolds are two-thirds of a human's height (1.2 m).
        /// Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureKoboldMilitia(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/AssaultRifle.asset");

            SoldierTemplate Kobold(string file, string name, bool sergeant)
            {
                var s = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/{file}.asset", t =>
                {
                    t.displayName = name;
                    t.health = 5; t.armorLevel = 0; t.accuracy = 50; t.movementRange = 20f;
                    t.keywords = new List<Keyword> { keywords["Biological"], keywords["Swarm"] };
                    t.abilities = new List<Ability>();
                    t.weapons = new List<WeaponTemplate> { rifle };
                    t.primaryWeapon = rifle;
                    t.radius = 0.33f;
                    t.height = 1.2f;   // two-thirds of a human's height
                });
                // (Re)build the model if it's missing or was built for another size (it's saved over the same prefab, keeping links).
                var built = s.prefab != null ? s.prefab.GetComponent<CapsuleCollider>() : null;
                if (s.prefab == null || built == null || !Mathf.Approximately(built.height, s.height))
                {
                    s.prefab = BuildKoboldPrefab(file, s, sergeant);
                    EditorUtility.SetDirty(s);
                }
                return s;
            }

            var sergeantTemplate = Kobold("KoboldSergeant", "Kobold Sergeant", true);
            var militiaman = Kobold("KoboldMilitiaman", "Kobold Militiaman", false);
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/KoboldMilitia.asset", u =>
            {
                u.displayName = "Kobold Militia";
                u.role = UnitRole.Infantry;
                u.description = "Wingless anthropomorphic dragons standing at two-thirds the height of a human with fabric uniform and holding rifles";
                u.leader = sergeantTemplate;
                u.members = new List<UnitTemplate.Member> { new UnitTemplate.Member { soldier = militiaman, count = 19 } };
                u.deploymentSpacing = 1.2f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>The Draconis Imperialis's names for weapons: the Assault Rifle is the "Fang Caster" (same weapon and stats).</summary>
        public static void EnsureDraconisWeaponNames(Faction draconis)
        {
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/AssaultRifle.asset");
            if (draconis.weaponNames.Any(n => n != null && n.weapon == rifle)) return;
            draconis.weaponNames.Add(new WeaponName { weapon = rifle, name = "Fang Caster" });
            EditorUtility.SetDirty(draconis);
        }

        /// <summary>
        /// A placeholder Kobold built from primitives, facing +z: a small (1.2 m) wingless, upright dragon with a snout and a
        /// tail, in a fabric uniform (tunic and cap in the primary colour, so the faction's colour when deployed) holding a
        /// rifle. Scales are a muted olive. The Sergeant has a gold armband and cap badge.
        /// </summary>
        private static GameObject BuildKoboldPrefab(string name, SoldierTemplate template, bool sergeant)
        {
            EnsureFolder(Root, "Prefabs");
            EnsureFolder($"{Root}/Prefabs", "Units");
            var uniform = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            var scales = GetOrCreateMaterial("Kobold_Scales", new Color(0.47f, 0.5f, 0.3f));
            var eye = GetOrCreateMaterial("Dragon_Eye", new Color(1f, 0.6f, 0.05f));
            var gold = GetOrCreateMaterial("Dragon_Gold", new Color(0.93f, 0.71f, 0.22f));
            var metal = GetOrCreateMaterial("Weapon", new Color(0.22f, 0.22f, 0.24f));

            var root = new GameObject(name);
            var soldier = root.AddComponent<Soldier>();
            var sso = new SerializedObject(soldier);
            sso.FindProperty("template").objectReferenceValue = template;
            sso.FindProperty("isSergeant").boolValue = sergeant;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = template.radius;
            col.height = template.height;
            col.center = new Vector3(0f, template.height / 2f, 0f);
            // The parts below are laid out for a 0.9 m kobold; the model is scaled to the soldier's height.
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * (template.height / 0.9f);

            foreach (float side in new[] { -1f, 1f })
            {
                Part(model, "Leg", PrimitiveType.Capsule, uniform, new Vector3(side * 0.07f, 0.17f, 0f), new Vector3(0.09f, 0.16f, 0.1f));
                Part(model, "Foot", PrimitiveType.Cube, scales, new Vector3(side * 0.07f, 0.02f, 0.05f), new Vector3(0.08f, 0.04f, 0.16f));
                Part(model, "Arm", PrimitiveType.Capsule, uniform, new Vector3(side * 0.15f, 0.46f, 0.05f), new Vector3(0.06f, 0.12f, 0.06f), new Vector3(-50f, 0f, 0f));
                Part(model, "Eye", PrimitiveType.Cube, eye, new Vector3(side * 0.05f, 0.74f, 0.08f), new Vector3(0.025f, 0.02f, 0.02f));
            }
            Part(model, "Tunic", PrimitiveType.Capsule, uniform, new Vector3(0f, 0.45f, 0f), new Vector3(0.24f, 0.18f, 0.18f));
            Part(model, "Belt", PrimitiveType.Cube, metal, new Vector3(0f, 0.36f, 0f), new Vector3(0.22f, 0.03f, 0.17f));
            Part(model, "Neck", PrimitiveType.Capsule, scales, new Vector3(0f, 0.62f, 0.01f), new Vector3(0.08f, 0.06f, 0.08f));
            Part(model, "Head", PrimitiveType.Cube, scales, new Vector3(0f, 0.72f, 0.01f), new Vector3(0.13f, 0.11f, 0.14f));
            Part(model, "Snout", PrimitiveType.Cube, scales, new Vector3(0f, 0.69f, 0.11f), new Vector3(0.08f, 0.06f, 0.1f));
            Part(model, "Cap", PrimitiveType.Cylinder, uniform, new Vector3(0f, 0.8f, 0f), new Vector3(0.14f, 0.025f, 0.14f));
            Part(model, "Tail", PrimitiveType.Capsule, scales, new Vector3(0f, 0.22f, -0.2f), new Vector3(0.07f, 0.16f, 0.07f), new Vector3(-60f, 0f, 0f));
            if (sergeant)
            {
                Part(model, "Cap Badge", PrimitiveType.Cube, gold, new Vector3(0f, 0.81f, 0.07f), new Vector3(0.04f, 0.03f, 0.01f));
                Part(model, "Armband", PrimitiveType.Cylinder, gold, new Vector3(-0.15f, 0.5f, 0.03f), new Vector3(0.07f, 0.015f, 0.07f), new Vector3(-50f, 0f, 0f));
            }
            // The rifle, held across the body.
            Part(model, "Rifle", PrimitiveType.Cube, metal, new Vector3(0.06f, 0.44f, 0.18f), new Vector3(0.04f, 0.05f, 0.42f), new Vector3(0f, -12f, 0f));

            Eldoria.Terrain.TerrainLayers.SetLayerRecursively(root, Eldoria.Terrain.TerrainLayers.Soldier);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Units/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// The Dragonborn's glaive, as two weapons:
        ///   Dragon Glaive (Range): the rifle built into its head. Damage 5, Shots 1, Penetration 1, +0, Range 20 m.
        ///   Dragon Glaive (melee): the blade. Damage 5, Shots 1, Penetration 2, +25% accuracy, Range 2 m; Melee.
        /// </summary>
        public static (WeaponTemplate ranged, WeaponTemplate melee) EnsureDragonGlaive(Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Weapons");
            var ranged = GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/DragonGlaiveRanged.asset", w =>
            {
                w.displayName = "Dragon Glaive (Range)";
                w.baseDamage = 5; w.shots = 1;
                w.hasPenetration = true; w.penetration = 1;
                w.accuracyBonus = 0;
                w.range = 20f;   // like the Assault Rifle
                w.keywords = new List<Keyword>();
            });
            var melee = GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/DragonGlaiveMelee.asset", w =>
            {
                w.displayName = "Dragon Glaive (melee)";
                w.baseDamage = 5; w.shots = 1;
                w.hasPenetration = true; w.penetration = 2;
                w.accuracyBonus = 25;
                w.range = 2f;    // like the game's other melee weapons
                w.keywords = new List<Keyword> { keywords["Melee"] };
            });
            return (ranged, melee);
        }

        /// <summary>
        /// Dragonborn Squad (Infantry, Draconis Imperialis): 1 Dragonborn Sergeant (leader) + 4 Dragonborn Footmen. All
        /// Health 10, Armor 2, Accuracy 75%, Move 10 m; Biological; Dragon Glaive (Range) (primary) and Dragon Glaive (melee),
        /// which they switch to in close range. Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureDragonbornSquad(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var (ranged, melee) = EnsureDragonGlaive(keywords);

            SoldierTemplate Dragonborn(string file, string name, bool sergeant)
            {
                var s = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/{file}.asset", t =>
                {
                    t.displayName = name;
                    t.health = 10; t.armorLevel = 2; t.accuracy = 75; t.movementRange = 10f;
                    t.keywords = new List<Keyword> { keywords["Biological"] };
                    t.abilities = new List<Ability>();
                    t.weapons = new List<WeaponTemplate> { ranged, melee };
                    t.primaryWeapon = ranged;
                    t.radius = 0.4f;
                    t.height = 2.2f;   // slightly taller than a human
                });
                if (s.prefab == null)
                {
                    s.prefab = BuildDragonbornPrefab(file, s, sergeant);
                    EditorUtility.SetDirty(s);
                }
                return s;
            }

            var sergeantTemplate = Dragonborn("DragonbornSergeant", "Dragonborn Sergeant", true);
            var footman = Dragonborn("DragonbornFootman", "Dragonborn Footman", false);
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/DragonbornSquad.asset", u =>
            {
                u.displayName = "Dragonborn Squad";
                u.role = UnitRole.Infantry;
                u.leader = sergeantTemplate;
                u.members = new List<UnitTemplate.Member> { new UnitTemplate.Member { soldier = footman, count = 4 } };
                u.deploymentSpacing = 2f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>
        /// A placeholder Dragonborn built from primitives, facing +z: an upright, anthropomorphic dragon about 2.2 m tall with
        /// a snouted head and horns, folded wings and a tail, holding a glaive whose head carries a rifle's receiver and barrel
        /// below the blade. Scales use the primary colour material (the faction's colour when deployed); the Sergeant has a
        /// gold crest and shoulder plates.
        /// </summary>
        private static GameObject BuildDragonbornPrefab(string name, SoldierTemplate template, bool sergeant)
        {
            EnsureFolder(Root, "Prefabs");
            EnsureFolder($"{Root}/Prefabs", "Units");
            var scales = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            var horn = GetOrCreateMaterial("Dragon_Horn", new Color(0.86f, 0.82f, 0.7f));
            var eye = GetOrCreateMaterial("Dragon_Eye", new Color(1f, 0.6f, 0.05f));
            var gold = GetOrCreateMaterial("Dragon_Gold", new Color(0.93f, 0.71f, 0.22f));
            var metal = GetOrCreateMaterial("Weapon", new Color(0.22f, 0.22f, 0.24f));
            var blade = GetOrCreateMaterial("Sword_Blade", new Color(0.8f, 0.82f, 0.85f));
            var shaft = GetOrCreateMaterial("Wood", new Color(0.45f, 0.3f, 0.18f));

            var root = new GameObject(name);
            var soldier = root.AddComponent<Soldier>();
            var sso = new SerializedObject(soldier);
            sso.FindProperty("template").objectReferenceValue = template;
            sso.FindProperty("isSergeant").boolValue = sergeant;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = template.radius;
            col.height = template.height;
            col.center = new Vector3(0f, template.height / 2f, 0f);

            // Legs (digitigrade feet with claws), body, head with a snout, horns and eyes.
            foreach (float side in new[] { -1f, 1f })
            {
                Part(root, "Thigh", PrimitiveType.Capsule, scales, new Vector3(side * 0.16f, 0.72f, 0f), new Vector3(0.22f, 0.3f, 0.24f));
                Part(root, "Shin", PrimitiveType.Capsule, scales, new Vector3(side * 0.16f, 0.3f, -0.05f), new Vector3(0.16f, 0.26f, 0.16f), new Vector3(-12f, 0f, 0f));
                Part(root, "Foot", PrimitiveType.Cube, scales, new Vector3(side * 0.16f, 0.05f, 0.1f), new Vector3(0.18f, 0.1f, 0.34f));
                Part(root, "Toe Claw", PrimitiveType.Cube, horn, new Vector3(side * 0.16f, 0.04f, 0.3f), new Vector3(0.12f, 0.05f, 0.08f));
                Part(root, "Arm", PrimitiveType.Capsule, scales, new Vector3(side * 0.34f, 1.42f, 0.08f), new Vector3(0.14f, 0.3f, 0.14f), new Vector3(-30f, 0f, 0f));
                Part(root, "Horn", PrimitiveType.Cube, horn, new Vector3(side * 0.09f, 2.12f, -0.08f), new Vector3(0.05f, 0.24f, 0.05f), new Vector3(-35f, 0f, side * 10f));
                Part(root, "Eye", PrimitiveType.Cube, eye, new Vector3(side * 0.1f, 1.98f, 0.13f), new Vector3(0.05f, 0.04f, 0.04f));
                // Wings, folded against the back, tips up.
                Part(root, "Wing", PrimitiveType.Cube, scales, new Vector3(side * 0.22f, 1.65f, -0.24f), new Vector3(0.04f, 0.95f, 0.5f), new Vector3(-10f, side * -25f, 0f));
            }
            Part(root, "Torso", PrimitiveType.Capsule, scales, new Vector3(0f, 1.32f, 0f), new Vector3(0.48f, 0.42f, 0.36f));
            Part(root, "Belly Plates", PrimitiveType.Cube, horn, new Vector3(0f, 1.25f, 0.16f), new Vector3(0.26f, 0.5f, 0.06f));
            Part(root, "Neck", PrimitiveType.Capsule, scales, new Vector3(0f, 1.78f, 0.02f), new Vector3(0.16f, 0.14f, 0.16f));
            Part(root, "Head", PrimitiveType.Cube, scales, new Vector3(0f, 1.97f, 0.02f), new Vector3(0.24f, 0.22f, 0.26f));
            Part(root, "Snout", PrimitiveType.Cube, scales, new Vector3(0f, 1.92f, 0.2f), new Vector3(0.16f, 0.12f, 0.22f));
            // Tail, curving down behind.
            Part(root, "Tail", PrimitiveType.Capsule, scales, new Vector3(0f, 0.8f, -0.42f), new Vector3(0.16f, 0.36f, 0.16f), new Vector3(-50f, 0f, 0f));
            Part(root, "Tail Tip", PrimitiveType.Capsule, scales, new Vector3(0f, 0.35f, -0.78f), new Vector3(0.1f, 0.24f, 0.1f), new Vector3(-75f, 0f, 0f));
            if (sergeant)
            {
                Part(root, "Crest", PrimitiveType.Cube, gold, new Vector3(0f, 2.12f, 0f), new Vector3(0.05f, 0.14f, 0.3f));
                foreach (float side in new[] { -1f, 1f })
                    Part(root, "Shoulder Plate", PrimitiveType.Cube, gold, new Vector3(side * 0.3f, 1.62f, 0f), new Vector3(0.2f, 0.08f, 0.26f), new Vector3(0f, 0f, side * -18f));
            }

            // The glaive, held upright in the right hand: a long shaft, a rifle receiver and barrel below the blade.
            Part(root, "Glaive Shaft", PrimitiveType.Cylinder, shaft, new Vector3(0.42f, 1.25f, 0.28f), new Vector3(0.05f, 1.05f, 0.05f));
            Part(root, "Glaive Receiver", PrimitiveType.Cube, metal, new Vector3(0.42f, 2.36f, 0.3f), new Vector3(0.09f, 0.2f, 0.12f));
            Part(root, "Glaive Barrel", PrimitiveType.Cylinder, metal, new Vector3(0.42f, 2.36f, 0.5f), new Vector3(0.035f, 0.16f, 0.035f), new Vector3(90f, 0f, 0f));
            Part(root, "Glaive Blade", PrimitiveType.Cube, blade, new Vector3(0.42f, 2.62f, 0.34f), new Vector3(0.02f, 0.38f, 0.14f), new Vector3(-8f, 0f, 0f));

            Eldoria.Terrain.TerrainLayers.SetLayerRecursively(root, Eldoria.Terrain.TerrainLayers.Soldier);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Units/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------ Most Holy Army of The Rask Divine Collective

        /// <summary>
        /// The Rask's weapons: the Assault Rifle is the "Solntse Laser Rifle" (an alias of the same stat block) with Damage +2
        /// (4 to 6) for Rask soldiers. Added once; edits made in the Inspector are kept.
        /// </summary>
        public static void EnsureRaskWeapons(Faction rask)
        {
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/AssaultRifle.asset");
            if (!rask.weaponNames.Any(n => n != null && n.weapon == rifle))
            {
                rask.weaponNames.Add(new WeaponName { weapon = rifle, name = "Solntse Laser Rifle" });
                EditorUtility.SetDirty(rask);
            }
            if (!rask.weaponAdjustments.Any(a => a != null && a.weapon == rifle))
            {
                rask.weaponAdjustments.Add(new WeaponAdjustment { weapon = rifle, damage = 2 });
                EditorUtility.SetDirty(rask);
            }
        }

        /// <summary>
        /// Rask Rifle Team (Infantry, Rask): 1 Rask Superior (leader) + 19 Rask Riflemen, with the Kobold Militiaman's stats
        /// (Health 5, Armor 0, Accuracy 50%, Move 20 m; Biological, Swarm). Each carries an Assault Rifle, which the Rask call
        /// the "Solntse Laser Rifle" and which does 6 damage for them (see <see cref="EnsureRaskWeapons"/>). Anthropomorphic
        /// rats, two-thirds of a human's height, in armor. Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureRaskRifleTeam(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var rifle = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/AssaultRifle.asset");

            SoldierTemplate Rask(string file, string name, bool superior)
            {
                var s = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/{file}.asset", t =>
                {
                    t.displayName = name;
                    t.health = 5; t.armorLevel = 0; t.accuracy = 50; t.movementRange = 20f;
                    t.keywords = new List<Keyword> { keywords["Biological"], keywords["Swarm"] };
                    t.abilities = new List<Ability>();
                    t.weapons = new List<WeaponTemplate> { rifle };
                    t.primaryWeapon = rifle;
                    t.radius = 0.33f;
                    t.height = 1.2f;   // two-thirds of a human's height
                });
                var built = s.prefab != null ? s.prefab.GetComponent<CapsuleCollider>() : null;
                if (s.prefab == null || built == null || !Mathf.Approximately(built.height, s.height))
                {
                    s.prefab = BuildRaskPrefab(file, s, superior);
                    EditorUtility.SetDirty(s);
                }
                return s;
            }

            var superiorTemplate = Rask("RaskSuperior", "Rask Superior", true);
            var rifleman = Rask("RaskRifleman", "Rask Rifleman", false);
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/RaskRifleTeam.asset", u =>
            {
                u.displayName = "Rask Rifle Team";
                u.role = UnitRole.Infantry;
                u.leader = superiorTemplate;
                u.members = new List<UnitTemplate.Member> { new UnitTemplate.Member { soldier = rifleman, count = 19 } };
                u.deploymentSpacing = 1.2f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>
        /// A placeholder Rask built from primitives, facing +z: an upright, anthropomorphic rat (1.2 m) with a long pointed
        /// snout, round ears and a long thin tail, in armor (breastplate and helmet in the primary colour, so the faction's
        /// colour when deployed; grey metal pauldrons and greaves), holding a rifle. Fur is grey-brown. The Superior has a gold
        /// helmet crest and gorget.
        /// </summary>
        private static GameObject BuildRaskPrefab(string name, SoldierTemplate template, bool superior)
        {
            EnsureFolder(Root, "Prefabs");
            EnsureFolder($"{Root}/Prefabs", "Units");
            var armor = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            var fur = GetOrCreateMaterial("Rask_Fur", new Color(0.45f, 0.4f, 0.36f));
            var skin = GetOrCreateMaterial("Rask_Skin", new Color(0.86f, 0.62f, 0.6f));
            var steel = GetOrCreateMaterial("Rask_Steel", new Color(0.52f, 0.53f, 0.56f));
            var eye = GetOrCreateMaterial("Rask_Eye", new Color(0.08f, 0.06f, 0.06f));
            var gold = GetOrCreateMaterial("Dragon_Gold", new Color(0.93f, 0.71f, 0.22f));
            var metal = GetOrCreateMaterial("Weapon", new Color(0.22f, 0.22f, 0.24f));

            var root = new GameObject(name);
            var soldier = root.AddComponent<Soldier>();
            var sso = new SerializedObject(soldier);
            sso.FindProperty("template").objectReferenceValue = template;
            sso.FindProperty("isSergeant").boolValue = superior;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = template.radius;
            col.height = template.height;
            col.center = new Vector3(0f, template.height / 2f, 0f);
            // The parts below are laid out for a 1.2 m rat; the model is scaled to the soldier's height.
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * (template.height / 1.2f);

            foreach (float side in new[] { -1f, 1f })
            {
                Part(model, "Leg", PrimitiveType.Capsule, fur, new Vector3(side * 0.09f, 0.24f, 0f), new Vector3(0.12f, 0.22f, 0.13f));
                Part(model, "Greave", PrimitiveType.Cube, steel, new Vector3(side * 0.09f, 0.2f, 0.06f), new Vector3(0.12f, 0.2f, 0.03f));
                Part(model, "Foot", PrimitiveType.Cube, skin, new Vector3(side * 0.09f, 0.03f, 0.06f), new Vector3(0.1f, 0.05f, 0.2f));
                Part(model, "Arm", PrimitiveType.Capsule, fur, new Vector3(side * 0.2f, 0.62f, 0.07f), new Vector3(0.08f, 0.16f, 0.08f), new Vector3(-50f, 0f, 0f));
                Part(model, "Pauldron", PrimitiveType.Sphere, steel, new Vector3(side * 0.19f, 0.77f, 0f), new Vector3(0.14f, 0.09f, 0.16f));
                Part(model, "Ear", PrimitiveType.Cylinder, skin, new Vector3(side * 0.09f, 1.12f, -0.01f), new Vector3(0.09f, 0.01f, 0.09f), new Vector3(90f, 0f, side * 20f));
                Part(model, "Eye", PrimitiveType.Sphere, eye, new Vector3(side * 0.06f, 1.0f, 0.1f), Vector3.one * 0.035f);
            }
            Part(model, "Body", PrimitiveType.Capsule, fur, new Vector3(0f, 0.6f, 0f), new Vector3(0.3f, 0.24f, 0.24f));
            Part(model, "Breastplate", PrimitiveType.Cube, armor, new Vector3(0f, 0.64f, 0.09f), new Vector3(0.28f, 0.3f, 0.08f));
            Part(model, "Backplate", PrimitiveType.Cube, armor, new Vector3(0f, 0.64f, -0.09f), new Vector3(0.26f, 0.28f, 0.06f));
            Part(model, "Belt", PrimitiveType.Cube, metal, new Vector3(0f, 0.47f, 0f), new Vector3(0.28f, 0.04f, 0.22f));
            Part(model, "Head", PrimitiveType.Sphere, fur, new Vector3(0f, 0.98f, 0.02f), new Vector3(0.18f, 0.17f, 0.2f));
            Part(model, "Snout", PrimitiveType.Capsule, fur, new Vector3(0f, 0.95f, 0.15f), new Vector3(0.08f, 0.08f, 0.08f), new Vector3(90f, 0f, 0f));
            Part(model, "Nose", PrimitiveType.Sphere, skin, new Vector3(0f, 0.95f, 0.23f), Vector3.one * 0.03f);
            Part(model, "Helmet", PrimitiveType.Sphere, armor, new Vector3(0f, 1.05f, -0.01f), new Vector3(0.2f, 0.1f, 0.2f));
            // A long, thin tail curling behind.
            Part(model, "Tail", PrimitiveType.Capsule, skin, new Vector3(0f, 0.3f, -0.3f), new Vector3(0.035f, 0.25f, 0.035f), new Vector3(-65f, 0f, 0f));
            Part(model, "Tail Tip", PrimitiveType.Capsule, skin, new Vector3(0f, 0.1f, -0.58f), new Vector3(0.025f, 0.12f, 0.025f), new Vector3(-85f, 0f, 0f));
            if (superior)
            {
                Part(model, "Helmet Crest", PrimitiveType.Cube, gold, new Vector3(0f, 1.12f, 0f), new Vector3(0.03f, 0.06f, 0.2f));
                Part(model, "Gorget", PrimitiveType.Cube, gold, new Vector3(0f, 0.82f, 0.08f), new Vector3(0.16f, 0.05f, 0.04f));
            }
            // The rifle, held across the body.
            Part(model, "Rifle", PrimitiveType.Cube, metal, new Vector3(0.08f, 0.58f, 0.24f), new Vector3(0.05f, 0.06f, 0.56f), new Vector3(0f, -12f, 0f));
            Part(model, "Rifle Emitter", PrimitiveType.Cylinder, gold, new Vector3(0.14f, 0.58f, 0.52f), new Vector3(0.04f, 0.02f, 0.04f), new Vector3(90f, 0f, 0f));

            Eldoria.Terrain.TerrainLayers.SetLayerRecursively(root, Eldoria.Terrain.TerrainLayers.Soldier);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Units/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Part(GameObject parent, string name, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale,
            Vector3? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            if (rotation.HasValue) go.transform.localEulerAngles = rotation.Value;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        [MenuItem("Eldoria/Rebuild Unit Catalog")]
        public static void RebuildUnitCatalog()
        {
            var keywords = EnsureKeywords();
            var factions = EnsureFactions();
            EnsureFieldCommander(factions[0], keywords);
            EnsureDragonHierarch(factions[1], keywords);
            EnsureDragonbornSquad(factions[1], keywords);
            EnsureKoboldMilitia(factions[1], keywords);
            EnsureDraconisWeaponNames(factions[1]);
            EnsureRaskRifleTeam(factions[2], keywords);
            EnsureRaskWeapons(factions[2]);
            EnsureIsfRifleSquad(factions[0], keywords);
            EnsureHeavyWeaponsTeam(factions[0], keywords);
            EnsureIsfRanger(factions[0], keywords);
            EnsureMortarTeam(factions[0], keywords);   // after the ISF Rifle Squad: it uses its ISF Rifleman
            EnsureSagittariusMbt(factions[0], keywords);
            EnsureOlympianMech(factions[0], keywords);
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
            Kw("Swarm", "Swarm", KeywordTarget.Soldier,
                "Expands the unit's cohesion radius to 10 m (normally 5 m): if the unit's leader has it, the rest of the unit may end a move up to 10 m from the leader.");
            Kw("Large", "Large", KeywordTarget.Soldier, "A very large creature. No effect on its own yet; other rules can check for it.");
            Kw("Natural", "Natural", KeywordTarget.Weapon, "A natural weapon (claws, teeth...): can't be disabled by Disarm.");
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
            GetOrCreate<Keyword>($"{DataDir}/Keywords/Chromashift.asset", k =>
            {
                k.displayName = "Chromashift";
                k.appliesTo = KeywordTarget.Soldier;
                k.description = "Wearing chromashift textiles (the ISF Ranger's Chromashift Textiles): attacks against this soldier take −25% accuracy.";
                k.interactions = new List<KeywordInteraction>
                {
                    new KeywordInteraction
                    {
                        role = KeywordRole.WhenDefending, party = KeywordParty.Target, condition = KeywordCondition.Always,
                        effect = KeywordEffect.AccuracyPenalty, amount = 25
                    }
                };
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
                ["Mechanical"] = KeywordCategory.Trait, ["Tank"] = KeywordCategory.Trait, ["Manual Load"] = KeywordCategory.Trait, ["Melee"] = KeywordCategory.Trait, ["Sniper Precision"] = KeywordCategory.Trait,
                ["Large"] = KeywordCategory.Trait, ["Natural"] = KeywordCategory.Trait, ["Swarm"] = KeywordCategory.Trait, ["In close range"] = KeywordCategory.Status,
                ["Divine Sanctuary"] = KeywordCategory.Status,
                ["Moved"] = KeywordCategory.Status, ["In half-cover"] = KeywordCategory.Status,
                ["In full cover"] = KeywordCategory.Status, ["Led from the front"] = KeywordCategory.Status,
                ["Chromashift"] = KeywordCategory.Status,
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
