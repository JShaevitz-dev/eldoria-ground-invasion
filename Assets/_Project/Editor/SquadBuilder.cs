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
        /// accuracy, 50 m; can swap it for the Anti-Materiel Rifle or the Shotgun); Chromashift Textiles (passive: attacks against
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
                u.description = "A lone marksman who picks off key enemies from up to 50 m. At 150% accuracy every shot hits, with a 50% chance to crit, and you choose exactly which enemy soldier to shoot: ideal for removing sergeants and officers. Or swap the Sniper Rifle for an Anti-Materiel Rifle (10 damage that ignores armor, but without the +50% aim) or a Shotgun (10 damage up close, out to 10 m, and it can fire after moving). The Anti-Materiel Rifle can't fire after moving, and with 5 health and only light armor the Ranger must stay out of sight, though its Chromashift Textiles give anyone aiming at it -25% accuracy; moving 15 m, they can reposition quickly between shots. A Specialist and a Solo unit: one per army.";
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
        /// Biological, Large; Dragon Claw) + 2 Dragonborn Honorguard (the Dragonborn Footman's stats but Accuracy 95%: Health
        /// 10, Armor 2, Move 10 m; Biological; Dragon Glaive (Range) and Dragon Glaive (melee)). A large quadrupedal dragon with
        /// wings and golden armor. Its footprint (radius 1 m) is smaller than the model, so its 2 m claw can reach a soldier
        /// standing next to it (reach is measured between soldiers' centres). Existing assets keep their Inspector edits; the
        /// Honorguard are added to an existing unit that lacks them.
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
            var (ranged, melee) = EnsureDragonGlaive(keywords);
            var honorguard = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/DragonbornHonorguard.asset", t =>
            {
                t.displayName = "Dragonborn Honorguard";
                t.health = 10; t.armorLevel = 2; t.accuracy = 95; t.movementRange = 10f;   // the Footman's, but 95%
                t.keywords = new List<Keyword> { keywords["Biological"] };
                t.abilities = new List<Ability>();
                t.weapons = new List<WeaponTemplate> { ranged, melee };
                t.primaryWeapon = ranged;
                t.radius = 0.4f;
                t.height = 2.2f;
            });
            if (honorguard.prefab == null)
            {
                honorguard.prefab = BuildDragonbornPrefab("DragonbornHonorguard", honorguard, false, honorguard: true);
                EditorUtility.SetDirty(honorguard);
            }
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/DragonHierarch.asset", u =>
            {
                u.displayName = "Dragon Hierarch";
                u.role = UnitRole.Commander;
                u.leader = dragon;
                u.members = new List<UnitTemplate.Member>();
                u.deploymentSpacing = 4f;
            }, u =>
            {
                if (!u.members.Any(m => m != null && m.soldier == honorguard))
                    u.members.Add(new UnitTemplate.Member { soldier = honorguard, count = 2 });
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
        /// Twin Raptor Pistols: a pair of pistols fired together. Damage 4, Shots 2, Penetration 1, Accuracy Bonus 0, Range
        /// 20 m; Pistol (usable in close range). Carried by the Dragonborn Skymaster Squad.
        /// </summary>
        public static WeaponTemplate EnsureTwinRaptorPistols(Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Weapons");
            return GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/TwinRaptorPistols.asset", w =>
            {
                w.displayName = "Twin Raptor Pistols";
                w.baseDamage = 4; w.shots = 2;
                w.hasPenetration = true; w.penetration = 1;
                w.accuracyBonus = 0;
                w.range = 20f;
                w.keywords = new List<Keyword> { keywords["Pistol"] };
            });
        }

        /// <summary>
        /// Dragonborn Skymaster Squad (Specialist, Draconis Imperialis): 1 Dragonborn Skylord (leader) + 4 Dragonborn
        /// Skymasters, with the Dragonborn Footman's stats but Move 40 m and the Jump Pack keyword (climbing onto objects
        /// costs no movement): Health 10, Armor 2, Accuracy 75%; Biological, Jump Pack; Twin Raptor Pistols. Dragonborn with
        /// jetpacks in place of wings, holding a pistol in each hand. Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureDragonbornSkymasterSquad(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var pistols = EnsureTwinRaptorPistols(keywords);

            SoldierTemplate Skymaster(string file, string name, bool leader)
            {
                var s = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/{file}.asset", t =>
                {
                    t.displayName = name;
                    t.health = 10; t.armorLevel = 2; t.accuracy = 75; t.movementRange = 40f;   // the Footman's, but Move 40
                    t.keywords = new List<Keyword> { keywords["Biological"], keywords["Jump Pack"] };
                    t.abilities = new List<Ability>();
                    t.weapons = new List<WeaponTemplate> { pistols };
                    t.primaryWeapon = pistols;
                    t.radius = 0.4f;
                    t.height = 2.2f;
                });
                if (s.prefab == null)
                {
                    s.prefab = BuildDragonbornPrefab(file, s, leader, skymaster: true);
                    EditorUtility.SetDirty(s);
                }
                return s;
            }

            var skylord = Skymaster("DragonbornSkylord", "Dragonborn Skylord", true);
            var skymaster = Skymaster("DragonbornSkymaster", "Dragonborn Skymaster", false);
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/DragonbornSkymasterSquad.asset", u =>
            {
                u.displayName = "Dragonborn Skymaster Squad";
                u.role = UnitRole.Specialist;
                u.description = "Dragonborn shock troops on jetpacks. They move 40 m (80 m sprinting) and their Jump Packs carry them onto rooftops, walls and crates at no extra cost, so they can strike anywhere on the field. Each fires Twin Raptor Pistols (2 shots, 4 damage, penetration 1, 20 m), pistols they can still use in close range. A Specialist: one per army.";
                u.leader = skylord;
                u.members = new List<UnitTemplate.Member> { new UnitTemplate.Member { soldier = skymaster, count = 4 } };
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
        /// Tyrant Railgun: Damage 5, Shots 1, Penetration 7, Accuracy Bonus 0, Range 50 m; Manual Load (fires once a turn).
        /// Mounted on the Draconis Tyrant Hover-tank.
        /// </summary>
        public static WeaponTemplate EnsureTyrantRailgun(Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Weapons");
            return GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/TyrantRailgun.asset", w =>
            {
                w.displayName = "Tyrant Railgun";
                w.baseDamage = 5; w.shots = 1;
                w.hasPenetration = true; w.penetration = 7;
                w.accuracyBonus = 0;
                w.range = 50f;
                w.keywords = new List<Keyword> { keywords["Manual Load"] };
            });
        }

        /// <summary>
        /// Tyrant Hover-tank (Heavy Support, Draconis Imperialis): 1 Tyrant Hover-tank (Health 15, Armor 4, Accuracy 100%, Move
        /// 30 m; Mechanical, Unstable, Hover (terrain can't reduce its movement); Tyrant Railgun). A dagger-shaped tank
        /// hovering just off the ground, a railgun turret on top and a Kobold pilot in the cockpit at the front. A wide body
        /// (radius 1.8 m, like the Sagittarius MBT). Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureTyrantHoverTank(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var railgun = EnsureTyrantRailgun(keywords);
            var tank = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/TyrantHoverTank.asset", s =>
            {
                s.displayName = "Tyrant Hover-tank";
                s.health = 15; s.armorLevel = 4; s.accuracy = 100; s.movementRange = 30f;
                s.keywords = new List<Keyword> { keywords["Mechanical"], keywords["Unstable"], keywords["Hover"] };
                s.abilities = new List<Ability>();
                s.weapons = new List<WeaponTemplate> { railgun };
                s.primaryWeapon = railgun;
                s.radius = 1.8f;
                s.height = 2.0f;
            });
            if (tank.prefab == null)
            {
                tank.prefab = BuildTyrantPrefab("TyrantHoverTank", tank);
                EditorUtility.SetDirty(tank);
            }
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/TyrantHoverTank.asset", u =>
            {
                u.displayName = "Tyrant Hover-tank";
                u.role = UnitRole.HeavySupport;
                u.description = "A dagger-shaped Draconis hover-tank flown by a Kobold pilot. Its Tyrant Railgun (5 damage, penetration 7, 50 m, once a turn) punches through any armor in the game, and at 100% accuracy it rarely misses. It glides 30 m over hills, rivers and obstacles alike, as terrain can't slow it (Hover), but being Mechanical it can't sprint. With 15 health and Armor Level 4 it shrugs off small arms, and when destroyed it has a 10% chance to explode (Unstable). Heavy Support, so you can field more than one.";
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
        /// A placeholder Tyrant Hover-tank built from primitives, facing +z: a dagger-shaped hull (a long blade body coming to
        /// a point at the front, a raised spine, a crossguard of side fins and an engine "pommel" at the back) hovering
        /// 0.75 m off the ground on glowing lift pads, a railgun turret (twin rails with glowing coils) set back on top, and
        /// an open cockpit on the point at the front with a Kobold pilot. The hull uses the primary colour (the faction's colour when deployed)
        /// with gold trim.
        /// </summary>
        private static GameObject BuildTyrantPrefab(string name, SoldierTemplate template)
        {
            EnsureFolder(Root, "Prefabs");
            EnsureFolder($"{Root}/Prefabs", "Units");
            var hull = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            var dark = GetOrCreateMaterial("Weapon", new Color(0.22f, 0.22f, 0.24f));
            var gold = GetOrCreateMaterial("Dragon_Gold", new Color(0.93f, 0.71f, 0.22f));
            var glow = GetOrCreateMaterial("Plasma_Glow", new Color(0.45f, 0.9f, 1f));
            var flame = GetOrCreateMaterial("Jet_Flame", new Color(1f, 0.55f, 0.12f));
            var scales = GetOrCreateMaterial("Kobold_Scales", new Color(0.47f, 0.5f, 0.3f));
            var eye = GetOrCreateMaterial("Dragon_Eye", new Color(1f, 0.6f, 0.05f));
            var glass = GetOrCreateMaterial("Cockpit_Glass", new Color(0.3f, 0.55f, 0.6f));

            var root = new GameObject(name);
            var soldier = root.AddComponent<Soldier>();
            var sso = new SerializedObject(soldier);
            sso.FindProperty("template").objectReferenceValue = template;
            sso.FindProperty("isSergeant").boolValue = true;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = template.radius;
            col.height = template.height;
            col.center = new Vector3(0f, template.height / 2f, 0f);
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);

            // The blade: a flat hull narrowing to a point at the front, with a raised spine down the middle.
            Part(model, "Hull", PrimitiveType.Cube, hull, new Vector3(0f, 1.0f, -0.5f), new Vector3(2.4f, 0.45f, 3.8f));
            Part(model, "Blade Tip", PrimitiveType.Cube, hull, new Vector3(0f, 1.0f, 1.35f), new Vector3(1.7f, 0.42f, 1.7f), new Vector3(0f, 45f, 0f));
            Part(model, "Underside", PrimitiveType.Cube, dark, new Vector3(0f, 0.8f, -0.6f), new Vector3(1.8f, 0.1f, 3.2f));
            Part(model, "Spine", PrimitiveType.Cube, gold, new Vector3(0f, 1.27f, -1.1f), new Vector3(0.14f, 0.1f, 2.6f));
            // The crossguard: swept side fins near the back.
            Part(model, "Crossguard", PrimitiveType.Cube, gold, new Vector3(0f, 1.05f, -1.5f), new Vector3(3.6f, 0.25f, 0.45f));
            foreach (float side in new[] { -1f, 1f })
                Part(model, "Fin", PrimitiveType.Cube, hull, new Vector3(side * 1.75f, 1.2f, -1.65f), new Vector3(0.18f, 0.55f, 0.8f), new Vector3(0f, side * -15f, 0f));
            // The pommel: the engine block at the back, its thrusters glowing.
            Part(model, "Engine", PrimitiveType.Cube, dark, new Vector3(0f, 1.0f, -2.6f), new Vector3(1.4f, 0.5f, 0.6f));
            foreach (float x in new[] { -0.4f, 0.4f })
                Part(model, "Thruster", PrimitiveType.Cylinder, flame, new Vector3(x, 1.0f, -2.92f), new Vector3(0.34f, 0.04f, 0.34f), new Vector3(90f, 0f, 0f));

            // Hovering: the hull floats about 0.75 m up on glowing lift pads; nothing touches the ground.
            foreach (float x in new[] { -0.75f, 0.75f })
                foreach (float z in new[] { 0.6f, -1.9f })
                    Part(model, "Lift Pad", PrimitiveType.Cylinder, glow, new Vector3(x, 0.72f, z), new Vector3(0.7f, 0.03f, 0.7f));

            // The turret, set back on top, with the railgun: twin rails and glowing coils, pointing forward over the bow.
            Part(model, "Turret Ring", PrimitiveType.Cylinder, dark, new Vector3(0f, 1.26f, -1.0f), new Vector3(1.3f, 0.05f, 1.3f));
            Part(model, "Turret", PrimitiveType.Cube, hull, new Vector3(0f, 1.52f, -1.0f), new Vector3(1.1f, 0.45f, 1.3f));
            Part(model, "Turret Trim", PrimitiveType.Cube, gold, new Vector3(0f, 1.76f, -1.0f), new Vector3(0.9f, 0.05f, 1.1f));
            foreach (float x in new[] { -0.13f, 0.13f })
                Part(model, "Rail", PrimitiveType.Cube, dark, new Vector3(x, 1.58f, 0.0f), new Vector3(0.09f, 0.14f, 2.0f));
            foreach (float z in new[] { -0.2f, 0.3f, 0.8f })
                Part(model, "Coil", PrimitiveType.Cube, glow, new Vector3(0f, 1.58f, z), new Vector3(0.42f, 0.2f, 0.06f));

            // The cockpit at the front, on the blade's point: an open tub behind a windscreen, with the Kobold pilot.
            Part(model, "Cockpit", PrimitiveType.Cube, dark, new Vector3(0f, 1.28f, 1.55f), new Vector3(0.7f, 0.16f, 0.8f));
            Part(model, "Windscreen", PrimitiveType.Cube, glass, new Vector3(0f, 1.42f, 1.98f), new Vector3(0.62f, 0.28f, 0.04f), new Vector3(-35f, 0f, 0f));
            Part(model, "Pilot", PrimitiveType.Capsule, scales, new Vector3(0f, 1.38f, 1.5f), new Vector3(0.3f, 0.18f, 0.24f));
            Part(model, "Pilot Head", PrimitiveType.Sphere, scales, new Vector3(0f, 1.62f, 1.55f), new Vector3(0.24f, 0.22f, 0.26f));
            Part(model, "Pilot Snout", PrimitiveType.Cube, scales, new Vector3(0f, 1.59f, 1.71f), new Vector3(0.12f, 0.09f, 0.16f));
            foreach (float side in new[] { -1f, 1f })
            {
                Part(model, "Pilot Eye", PrimitiveType.Sphere, eye, new Vector3(side * 0.07f, 1.66f, 1.66f), Vector3.one * 0.05f);
                Part(model, "Pilot Horn", PrimitiveType.Cube, gold, new Vector3(side * 0.07f, 1.76f, 1.49f), new Vector3(0.03f, 0.1f, 0.03f), new Vector3(-35f, 0f, 0f));
            }

            Eldoria.Terrain.TerrainLayers.SetLayerRecursively(root, Eldoria.Terrain.TerrainLayers.Soldier);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Units/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// A placeholder Dragonborn built from primitives, facing +z: an upright, anthropomorphic dragon about 2.2 m tall with
        /// a snouted head and horns, folded wings and a tail, holding a glaive whose head carries a rifle's receiver and barrel
        /// below the blade. Scales use the primary colour material (the faction's colour when deployed); the Sergeant has a
        /// gold crest and shoulder plates. The Honorguard (the Dragon Hierarch's escort) wear a gold breastplate, helm and
        /// shoulder plates, and a crimson cape. Skymasters have a jetpack (twin thrusters with glowing nozzles) in place of wings
        /// and hold a pistol in each hand instead of the glaive.
        /// </summary>
        private static GameObject BuildDragonbornPrefab(string name, SoldierTemplate template, bool sergeant, bool honorguard = false, bool skymaster = false)
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
                // Wings, folded against the back, tips up (Skymasters wear a jetpack instead).
                if (!skymaster) Part(root, "Wing", PrimitiveType.Cube, scales, new Vector3(side * 0.22f, 1.65f, -0.24f), new Vector3(0.04f, 0.95f, 0.5f), new Vector3(-10f, side * -25f, 0f));
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
            if (honorguard)
            {
                var cape = GetOrCreateMaterial("Dragon_Cape", new Color(0.55f, 0.07f, 0.09f));
                Part(root, "Breastplate", PrimitiveType.Cube, gold, new Vector3(0f, 1.36f, 0.17f), new Vector3(0.42f, 0.5f, 0.08f));
                Part(root, "Helm", PrimitiveType.Cube, gold, new Vector3(0f, 2.06f, 0.0f), new Vector3(0.27f, 0.08f, 0.3f));
                Part(root, "Helm Crest", PrimitiveType.Cube, cape, new Vector3(0f, 2.16f, -0.04f), new Vector3(0.05f, 0.12f, 0.34f));
                foreach (float side in new[] { -1f, 1f })
                    Part(root, "Shoulder Plate", PrimitiveType.Cube, gold, new Vector3(side * 0.3f, 1.62f, 0f), new Vector3(0.22f, 0.09f, 0.28f), new Vector3(0f, 0f, side * -18f));
                Part(root, "Cape", PrimitiveType.Cube, cape, new Vector3(0f, 1.12f, -0.27f), new Vector3(0.5f, 1.05f, 0.04f), new Vector3(8f, 0f, 0f));
            }

            if (skymaster)
            {
                var flame = GetOrCreateMaterial("Jet_Flame", new Color(1f, 0.55f, 0.12f));
                // The jetpack: a pack on the back with a thruster on each side, nozzles glowing.
                Part(root, "Jetpack", PrimitiveType.Cube, metal, new Vector3(0f, 1.48f, -0.3f), new Vector3(0.5f, 0.56f, 0.2f));
                foreach (float side in new[] { -1f, 1f })
                {
                    Part(root, "Thruster", PrimitiveType.Cylinder, metal, new Vector3(side * 0.36f, 1.46f, -0.34f), new Vector3(0.2f, 0.38f, 0.2f));
                    Part(root, "Thruster Fin", PrimitiveType.Cube, gold, new Vector3(side * 0.48f, 1.7f, -0.34f), new Vector3(0.04f, 0.24f, 0.16f));
                    Part(root, "Nozzle", PrimitiveType.Cylinder, flame, new Vector3(side * 0.36f, 1.06f, -0.34f), new Vector3(0.15f, 0.04f, 0.15f));
                    // A pistol in each hand, aimed forward.
                    Part(root, "Pistol", PrimitiveType.Cube, metal, new Vector3(side * 0.36f, 1.2f, 0.34f), new Vector3(0.07f, 0.1f, 0.24f));
                    Part(root, "Pistol Grip", PrimitiveType.Cube, metal, new Vector3(side * 0.36f, 1.11f, 0.27f), new Vector3(0.06f, 0.12f, 0.06f), new Vector3(15f, 0f, 0f));
                    Part(root, "Pistol Muzzle", PrimitiveType.Cylinder, gold, new Vector3(side * 0.36f, 1.22f, 0.48f), new Vector3(0.04f, 0.02f, 0.04f), new Vector3(90f, 0f, 0f));
                }
            }
            else
            {
                // The glaive, held upright in the right hand: a long shaft, a rifle receiver and barrel below the blade.
                Part(root, "Glaive Shaft", PrimitiveType.Cylinder, shaft, new Vector3(0.42f, 1.25f, 0.28f), new Vector3(0.05f, 1.05f, 0.05f));
                Part(root, "Glaive Receiver", PrimitiveType.Cube, metal, new Vector3(0.42f, 2.36f, 0.3f), new Vector3(0.09f, 0.2f, 0.12f));
                Part(root, "Glaive Barrel", PrimitiveType.Cylinder, metal, new Vector3(0.42f, 2.36f, 0.5f), new Vector3(0.035f, 0.16f, 0.035f), new Vector3(90f, 0f, 0f));
                Part(root, "Glaive Blade", PrimitiveType.Cube, blade, new Vector3(0.42f, 2.62f, 0.34f), new Vector3(0.02f, 0.38f, 0.14f), new Vector3(-8f, 0f, 0f));
            }

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
        /// but Armor 1 (Health 5, Armor 1, Accuracy 50%, Move 20 m; Biological, Swarm). Each carries an Assault Rifle, which the Rask call
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
                    t.health = 5; t.armorLevel = 1; t.accuracy = 50; t.movementRange = 20f;
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
        /// Rask Ultima (Commander, Rask): 1 Rask Ultima with the Rask Rifleman's stats but Health 10 and Armor 2 (Accuracy
        /// 50%, Move 20 m; Biological, Solo, Officer) and a Pistol. The Rask model with the Superior's gold crest and gorget, a
        /// purple cape and a pistol in place of the rifle. Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureRaskUltima(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/Pistol.asset");
            var ultima = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/RaskUltima.asset", t =>
            {
                t.displayName = "Rask Ultima";
                t.health = 10; t.armorLevel = 2; t.accuracy = 50; t.movementRange = 20f;   // the Rifleman's, but Health 10, Armor 2
                t.keywords = new List<Keyword> { keywords["Biological"], keywords["Solo"], keywords["Officer"] };
                t.abilities = new List<Ability>();
                t.weapons = new List<WeaponTemplate> { pistol };
                t.primaryWeapon = pistol;
                t.radius = 0.33f;
                t.height = 1.2f;
            });
            if (ultima.prefab == null)
            {
                ultima.prefab = BuildRaskPrefab("RaskUltima", ultima, true, ultima: true);
                EditorUtility.SetDirty(ultima);
            }
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/RaskUltima.asset", u =>
            {
                u.displayName = "Rask Ultima";
                u.role = UnitRole.Commander;
                u.leader = ultima;
                u.members = new List<UnitTemplate.Member>();
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
        private static GameObject BuildRaskPrefab(string name, SoldierTemplate template, bool superior, bool ultima = false)
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
            if (ultima)
            {
                // The Ultima: a purple cape, gold epaulettes and a pistol in the right hand.
                var cape = GetOrCreateMaterial("Rask_Cape", new Color(0.36f, 0.12f, 0.46f));
                Part(model, "Cape", PrimitiveType.Cube, cape, new Vector3(0f, 0.55f, -0.15f), new Vector3(0.34f, 0.55f, 0.03f), new Vector3(10f, 0f, 0f));
                foreach (float side in new[] { -1f, 1f })
                    Part(model, "Epaulette", PrimitiveType.Cube, gold, new Vector3(side * 0.19f, 0.83f, 0f), new Vector3(0.14f, 0.03f, 0.14f));
                Part(model, "Pistol", PrimitiveType.Cube, metal, new Vector3(0.2f, 0.52f, 0.22f), new Vector3(0.04f, 0.06f, 0.16f));
                Part(model, "Pistol Grip", PrimitiveType.Cube, metal, new Vector3(0.2f, 0.47f, 0.17f), new Vector3(0.035f, 0.07f, 0.04f), new Vector3(15f, 0f, 0f));
            }
            else
            {
                // The rifle, held across the body.
                Part(model, "Rifle", PrimitiveType.Cube, metal, new Vector3(0.08f, 0.58f, 0.24f), new Vector3(0.05f, 0.06f, 0.56f), new Vector3(0f, -12f, 0f));
                Part(model, "Rifle Emitter", PrimitiveType.Cylinder, gold, new Vector3(0.14f, 0.58f, 0.52f), new Vector3(0.04f, 0.02f, 0.04f), new Vector3(90f, 0f, 0f));
            }

            Eldoria.Terrain.TerrainLayers.SetLayerRecursively(root, Eldoria.Terrain.TerrainLayers.Soldier);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Units/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Plasma Mortar: the Mortar Launcher's stats (Damage 5, Shots 1, Penetration 3, +0%, Range 50 m; Deployed, Manual
        /// Load, Explosive) as a weapon of its own. Carried by the Rask's Stinger Heavy Robot.
        /// </summary>
        public static WeaponTemplate EnsurePlasmaMortar(Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Weapons");
            return GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/PlasmaMortar.asset", w =>
            {
                w.displayName = "Plasma Mortar";
                w.baseDamage = 5; w.shots = 1;
                w.hasPenetration = true; w.penetration = 3;
                w.accuracyBonus = 0;
                w.range = 50f;
                w.keywords = new List<Keyword> { keywords["Deployed"], keywords["Manual Load"], keywords["Explosive"] };
            });
        }

        /// <summary>
        /// Stinger Heavy Robot (Heavy Support, Rask): 1 Stinger Heavy Robot (Health 25, Armor 5, Accuracy 75%, Move 30 m;
        /// Mechanical, Heavy, Robotic; Stabilizers (passive: nullifies Deployed); Plasma Mortar). A six-legged, beetle-like
        /// war machine (armored hull, angular legs) with a large cannon on its back angled up at 45 degrees. Footprint radius 1.6 m (smaller than the legs'
        /// spread). Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureStingerHeavyRobot(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var mortar = EnsurePlasmaMortar(keywords);
            var stabilizers = AssetDatabase.LoadAssetAtPath<Ability>($"{DataDir}/Abilities/Stabilizers.asset");
            var robot = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/StingerHeavyRobot.asset", s =>
            {
                s.displayName = "Stinger Heavy Robot";
                s.health = 25; s.armorLevel = 5; s.accuracy = 75; s.movementRange = 30f;
                s.keywords = new List<Keyword> { keywords["Mechanical"], keywords["Heavy"], keywords["Robotic"] };
                s.abilities = new List<Ability> { stabilizers };
                s.weapons = new List<WeaponTemplate> { mortar };
                s.primaryWeapon = mortar;
                s.radius = 1.6f;
                s.height = 3f;
            });
            // Rebuilt if missing, or if it's the first (beetle) version of the model.
            if (robot.prefab == null || robot.prefab.transform.Find("Model/Hull") == null)
            {
                robot.prefab = BuildStingerPrefab("StingerHeavyRobot", robot);
                EditorUtility.SetDirty(robot);
            }
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/StingerHeavyRobot.asset", u =>
            {
                u.displayName = "Stinger Heavy Robot";
                u.role = UnitRole.HeavySupport;
                u.description = "The Rask's six-legged siege engine: a beetle-shaped war robot with 25 health and Armor Level 5, proof against almost everything but the heaviest guns. The Plasma Mortar on its back (5 damage, penetration 3, 50 m, once a turn) cracks armored targets at long range, and its Stabilizers let it fire even after moving. Mechanical: it strides 30 m but can't sprint. Heavy Support, so you can field more than one.";
                u.leader = robot;
                u.members = new List<UnitTemplate.Member>();
                u.deploymentSpacing = 5f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>
        /// A placeholder Stinger built from primitives, facing +z: a hard-edged, six-legged war machine rather than an
        /// animal. A boxy armored hull with a ridged, sloped front shield; round sensor pods on its flanks (one with a red
        /// lens); six angular legs (three a side) of broad armor plates on dark joints, ending in hooked tips; and on its
        /// back a turret carrying a large cannon angled up at 45 degrees, with a glowing plasma muzzle. Armor plates use the
        /// primary colour (the faction's colour when deployed).
        /// </summary>
        private static GameObject BuildStingerPrefab(string name, SoldierTemplate template)
        {
            EnsureFolder(Root, "Prefabs");
            EnsureFolder($"{Root}/Prefabs", "Units");
            var shell = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            var steel = GetOrCreateMaterial("Rask_Steel", new Color(0.52f, 0.53f, 0.56f));
            var dark = GetOrCreateMaterial("Weapon", new Color(0.22f, 0.22f, 0.24f));
            var eye = GetOrCreateMaterial("Stinger_Eye", new Color(0.95f, 0.12f, 0.08f));
            var plasma = GetOrCreateMaterial("Plasma_Glow", new Color(0.45f, 0.9f, 1f));

            var root = new GameObject(name);
            var soldier = root.AddComponent<Soldier>();
            var sso = new SerializedObject(soldier);
            sso.FindProperty("template").objectReferenceValue = template;
            sso.FindProperty("isSergeant").boolValue = true;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = template.radius;
            col.height = template.height;
            col.center = new Vector3(0f, template.height / 2f, 0f);
            // Laid out for a 3 m tall robot; scaled to the soldier's height.
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * (template.height / 3f);

            // Hull: an armored box with a sloped, ridged front shield and a rear plate, on a dark under-frame.
            Part(model, "Frame", PrimitiveType.Cube, dark, new Vector3(0f, 1.35f, 0f), new Vector3(1.3f, 0.5f, 2.2f));
            Part(model, "Hull", PrimitiveType.Cube, shell, new Vector3(0f, 1.75f, -0.05f), new Vector3(1.5f, 0.9f, 1.9f));
            Part(model, "Hull Top", PrimitiveType.Cube, shell, new Vector3(0f, 2.22f, -0.15f), new Vector3(1.2f, 0.12f, 1.5f));
            Part(model, "Front Shield", PrimitiveType.Cube, shell, new Vector3(0f, 1.6f, 1.05f), new Vector3(1.25f, 1.5f, 0.22f), new Vector3(-18f, 0f, 0f));
            foreach (float x in new[] { -0.35f, 0f, 0.35f })
                Part(model, "Shield Ridge", PrimitiveType.Cube, shell, new Vector3(x, 1.62f, 1.2f), new Vector3(0.12f, 1.3f, 0.12f), new Vector3(-18f, 0f, 0f));
            Part(model, "Shield Trim", PrimitiveType.Cube, dark, new Vector3(0f, 0.92f, 0.85f), new Vector3(1.0f, 0.12f, 0.3f));
            Part(model, "Rear Plate", PrimitiveType.Cube, shell, new Vector3(0f, 1.65f, -1.05f), new Vector3(1.2f, 1.0f, 0.2f), new Vector3(12f, 0f, 0f));

            // Sensor pods on the flanks; the right one carries the red lens.
            foreach (float side in new[] { -1f, 1f })
            {
                Part(model, "Pod", PrimitiveType.Sphere, shell, new Vector3(side * 0.95f, 1.85f, 0.35f), Vector3.one * 0.75f);
                Part(model, "Pod Ring", PrimitiveType.Cylinder, dark, new Vector3(side * 1.08f, 1.85f, 0.62f), new Vector3(0.42f, 0.05f, 0.42f), new Vector3(90f, 0f, 0f));
            }
            Part(model, "Lens", PrimitiveType.Sphere, eye, new Vector3(1.08f, 1.85f, 0.68f), new Vector3(0.24f, 0.24f, 0.12f));
            Part(model, "Lens", PrimitiveType.Sphere, plasma, new Vector3(-1.08f, 1.85f, 0.68f), new Vector3(0.18f, 0.18f, 0.08f));

            // Six angular legs, three a side: a broad armor plate rising out to a dark knee joint, a tapering lower plate
            // down to the ground, and a hooked tip turned inward.
            foreach (float side in new[] { -1f, 1f })
                foreach (float z in new[] { 1.05f, 0f, -1.05f })
                {
                    Part(model, "Hip", PrimitiveType.Cylinder, dark, new Vector3(side * 0.8f, 1.4f, z), new Vector3(0.38f, 0.12f, 0.38f), new Vector3(0f, 0f, 90f));
                    Part(model, "Upper Leg", PrimitiveType.Cube, shell, new Vector3(side * 1.3f, 1.72f, z), new Vector3(0.24f, 1.15f, 0.42f), new Vector3(0f, 0f, side * -56f));
                    Part(model, "Knee", PrimitiveType.Cylinder, dark, new Vector3(side * 1.78f, 2.02f, z), new Vector3(0.32f, 0.16f, 0.32f), new Vector3(0f, 0f, 90f));
                    Part(model, "Lower Leg", PrimitiveType.Cube, steel, new Vector3(side * 1.96f, 1.1f, z), new Vector3(0.2f, 1.85f, 0.3f), new Vector3(0f, 0f, side * 12f));
                    Part(model, "Shin Plate", PrimitiveType.Cube, shell, new Vector3(side * 2.02f, 1.35f, z), new Vector3(0.12f, 0.9f, 0.36f), new Vector3(0f, 0f, side * 12f));
                    Part(model, "Hook", PrimitiveType.Cube, dark, new Vector3(side * 2.08f, 0.14f, z), new Vector3(0.14f, 0.36f, 0.22f), new Vector3(0f, 0f, side * -45f));
                }

            // The cannon: a turret on the back and a long barrel pointing forward and up at 45 degrees.
            Part(model, "Turret", PrimitiveType.Cube, dark, new Vector3(0f, 2.45f, -0.4f), new Vector3(0.9f, 0.35f, 1.0f));
            Part(model, "Breech", PrimitiveType.Cube, steel, new Vector3(0f, 2.75f, -0.5f), new Vector3(0.6f, 0.5f, 0.7f), new Vector3(-45f, 0f, 0f));
            Part(model, "Barrel", PrimitiveType.Cylinder, dark, new Vector3(0f, 3.2f, -0.05f), new Vector3(0.34f, 0.85f, 0.34f), new Vector3(45f, 0f, 0f));
            foreach (float t in new[] { 0.35f, 0.75f })
                Part(model, "Coil", PrimitiveType.Cylinder, plasma, new Vector3(0f, 2.85f + t * 0.9f, -0.4f + t * 0.9f), new Vector3(0.42f, 0.04f, 0.42f), new Vector3(45f, 0f, 0f));
            Part(model, "Muzzle", PrimitiveType.Cylinder, plasma, new Vector3(0f, 3.8f, 0.55f), new Vector3(0.4f, 0.06f, 0.4f), new Vector3(45f, 0f, 0f));

            Eldoria.Terrain.TerrainLayers.SetLayerRecursively(root, Eldoria.Terrain.TerrainLayers.Soldier);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Units/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Twin Intimidator Machine Guns: the Machine Gun's stats (Damage 5, Penetration 1, +0%, Range 20 m; Deployed), but
        /// 10 shots. Copied from the Machine Gun when first created. Carried by the Rask's Intimidator Medium Robot.
        /// </summary>
        public static WeaponTemplate EnsureTwinIntimidatorMachineGuns()
        {
            EnsureFolder(DataDir, "Weapons");
            var mg = AssetDatabase.LoadAssetAtPath<WeaponTemplate>($"{DataDir}/Weapons/MachineGun.asset");
            return GetOrCreate<WeaponTemplate>($"{DataDir}/Weapons/TwinIntimidatorMachineGuns.asset", w =>
            {
                w.displayName = "Twin Intimidator Machine Guns";
                w.baseDamage = mg.baseDamage;
                w.hasPenetration = mg.hasPenetration; w.penetration = mg.penetration;
                w.accuracyBonus = mg.accuracyBonus;
                w.range = mg.range;
                w.keywords = new List<Keyword>(mg.keywords);
                w.shots = 10;
            });
        }

        /// <summary>
        /// Intimidator Medium Robot (Heavy Support, Rask): 1 Intimidator Medium Robot (Health 10, Armor 3, Accuracy 75%, Move
        /// 30 m; Mechanical, Robotic; Stabilizers (passive: nullifies Deployed); Twin Intimidator Machine Guns). A gorilla-like,
        /// four-legged robot that walks on its knuckles, with a machine gun on each shoulder. Radius 1 m. Existing assets keep
        /// their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureIntimidatorMediumRobot(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var guns = EnsureTwinIntimidatorMachineGuns();
            var stabilizers = AssetDatabase.LoadAssetAtPath<Ability>($"{DataDir}/Abilities/Stabilizers.asset");
            var robot = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/IntimidatorMediumRobot.asset", s =>
            {
                s.displayName = "Intimidator Medium Robot";
                s.health = 10; s.armorLevel = 3; s.accuracy = 75; s.movementRange = 30f;
                s.keywords = new List<Keyword> { keywords["Mechanical"], keywords["Robotic"] };
                s.abilities = new List<Ability> { stabilizers };
                s.weapons = new List<WeaponTemplate> { guns };
                s.primaryWeapon = guns;
                s.radius = 1.0f;
                s.height = 2.6f;
            });
            if (robot.prefab == null)
            {
                robot.prefab = BuildIntimidatorPrefab("IntimidatorMediumRobot", robot);
                EditorUtility.SetDirty(robot);
            }
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/IntimidatorMediumRobot.asset", u =>
            {
                u.displayName = "Intimidator Medium Robot";
                u.role = UnitRole.HeavySupport;
                u.description = "A gorilla-like Rask war robot that knuckle-walks into battle with a machine gun on each shoulder. Its Twin Intimidator Machine Guns pour out 10 shots a volley (5 damage, penetration 1, 20 m), and its Stabilizers let it fire them even after moving 30 m. With 10 health and Armor Level 3 it shrugs off rifles and pistols, but anti-tank weapons will bring it down. Mechanical: it can't sprint. Heavy Support, so you can field more than one.";
                u.leader = robot;
                u.members = new List<UnitTemplate.Member>();
                u.deploymentSpacing = 3f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>
        /// A placeholder Intimidator built from primitives, facing +z: a gorilla-like robot on all fours. A massive armored
        /// chest and shoulders sloping down to small hips, long thick arms reaching down to the ground on big knuckle-fists,
        /// short back legs, a small head set low between the shoulders with a red visor, and a machine gun on a mount on each
        /// shoulder. Armor plates use the primary colour (the faction's colour when deployed).
        /// </summary>
        private static GameObject BuildIntimidatorPrefab(string name, SoldierTemplate template)
        {
            EnsureFolder(Root, "Prefabs");
            EnsureFolder($"{Root}/Prefabs", "Units");
            var armor = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));
            var steel = GetOrCreateMaterial("Rask_Steel", new Color(0.52f, 0.53f, 0.56f));
            var dark = GetOrCreateMaterial("Weapon", new Color(0.22f, 0.22f, 0.24f));
            var eye = GetOrCreateMaterial("Stinger_Eye", new Color(0.95f, 0.12f, 0.08f));
            var gold = GetOrCreateMaterial("Dragon_Gold", new Color(0.93f, 0.71f, 0.22f));

            var root = new GameObject(name);
            var soldier = root.AddComponent<Soldier>();
            var sso = new SerializedObject(soldier);
            sso.FindProperty("template").objectReferenceValue = template;
            sso.FindProperty("isSergeant").boolValue = true;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var col = root.AddComponent<CapsuleCollider>();
            col.radius = template.radius;
            col.height = template.height;
            col.center = new Vector3(0f, template.height / 2f, 0f);
            // Laid out for a 2.6 m tall robot; scaled to the soldier's height.
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * (template.height / 2.6f);

            // Body: a huge chest and shoulders, sloping back and down to small hips.
            Part(model, "Chest", PrimitiveType.Sphere, dark, new Vector3(0f, 1.6f, 0.25f), new Vector3(1.5f, 1.15f, 1.15f));
            Part(model, "Chest Plate", PrimitiveType.Cube, armor, new Vector3(0f, 1.55f, 0.72f), new Vector3(0.95f, 0.75f, 0.2f), new Vector3(-15f, 0f, 0f));
            Part(model, "Back Plate", PrimitiveType.Cube, armor, new Vector3(0f, 2.05f, 0.05f), new Vector3(1.2f, 0.25f, 1.0f), new Vector3(-12f, 0f, 0f));
            Part(model, "Waist", PrimitiveType.Cylinder, steel, new Vector3(0f, 1.3f, -0.35f), new Vector3(0.55f, 0.3f, 0.55f), new Vector3(-60f, 0f, 0f));
            Part(model, "Hips", PrimitiveType.Sphere, dark, new Vector3(0f, 1.05f, -0.7f), new Vector3(0.9f, 0.6f, 0.65f));
            Part(model, "Hip Plate", PrimitiveType.Cube, armor, new Vector3(0f, 1.3f, -0.75f), new Vector3(0.75f, 0.12f, 0.55f));

            // Head: small, set low and forward between the shoulders, with a red visor.
            Part(model, "Head", PrimitiveType.Cube, armor, new Vector3(0f, 1.78f, 0.95f), new Vector3(0.45f, 0.38f, 0.42f));
            Part(model, "Brow", PrimitiveType.Cube, dark, new Vector3(0f, 1.94f, 1.12f), new Vector3(0.48f, 0.08f, 0.12f));
            Part(model, "Visor", PrimitiveType.Cube, eye, new Vector3(0f, 1.82f, 1.17f), new Vector3(0.34f, 0.07f, 0.04f));
            Part(model, "Jaw", PrimitiveType.Cube, steel, new Vector3(0f, 1.62f, 1.08f), new Vector3(0.38f, 0.12f, 0.24f));

            foreach (float side in new[] { -1f, 1f })
            {
                // Long, thick arms down to big knuckle-fists on the ground.
                Part(model, "Shoulder", PrimitiveType.Sphere, armor, new Vector3(side * 0.82f, 1.9f, 0.35f), Vector3.one * 0.62f);
                Part(model, "Upper Arm", PrimitiveType.Capsule, steel, new Vector3(side * 0.98f, 1.45f, 0.5f), new Vector3(0.36f, 0.45f, 0.36f), new Vector3(-18f, 0f, side * 12f));
                Part(model, "Elbow", PrimitiveType.Sphere, dark, new Vector3(side * 1.07f, 1.05f, 0.64f), Vector3.one * 0.32f);
                Part(model, "Forearm", PrimitiveType.Capsule, armor, new Vector3(side * 1.07f, 0.62f, 0.72f), new Vector3(0.44f, 0.42f, 0.44f), new Vector3(-8f, 0f, 0f));
                Part(model, "Fist", PrimitiveType.Cube, dark, new Vector3(side * 1.07f, 0.17f, 0.8f), new Vector3(0.48f, 0.34f, 0.5f));
                Part(model, "Knuckles", PrimitiveType.Cube, steel, new Vector3(side * 1.07f, 0.1f, 1.04f), new Vector3(0.44f, 0.14f, 0.1f));

                // Short back legs.
                Part(model, "Thigh", PrimitiveType.Capsule, steel, new Vector3(side * 0.5f, 0.78f, -0.78f), new Vector3(0.34f, 0.32f, 0.34f), new Vector3(25f, 0f, 0f));
                Part(model, "Shin", PrimitiveType.Capsule, armor, new Vector3(side * 0.5f, 0.34f, -0.9f), new Vector3(0.28f, 0.28f, 0.28f), new Vector3(-10f, 0f, 0f));
                Part(model, "Foot", PrimitiveType.Cube, dark, new Vector3(side * 0.5f, 0.06f, -0.78f), new Vector3(0.36f, 0.12f, 0.52f));

                // A machine gun on a mount on each shoulder, aimed forward, with an ammo box behind.
                Part(model, "Gun Mount", PrimitiveType.Cube, dark, new Vector3(side * 0.82f, 2.28f, 0.3f), new Vector3(0.3f, 0.22f, 0.4f));
                Part(model, "Machine Gun", PrimitiveType.Cube, dark, new Vector3(side * 0.82f, 2.46f, 0.55f), new Vector3(0.22f, 0.24f, 0.85f));
                Part(model, "Barrel Shroud", PrimitiveType.Cylinder, steel, new Vector3(side * 0.82f, 2.46f, 1.12f), new Vector3(0.14f, 0.2f, 0.14f), new Vector3(90f, 0f, 0f));
                Part(model, "Barrel", PrimitiveType.Cylinder, dark, new Vector3(side * 0.82f, 2.46f, 1.42f), new Vector3(0.07f, 0.16f, 0.07f), new Vector3(90f, 0f, 0f));
                Part(model, "Ammo Box", PrimitiveType.Cube, gold, new Vector3(side * 0.82f, 2.42f, 0.0f), new Vector3(0.3f, 0.28f, 0.3f));
            }

            Eldoria.Terrain.TerrainLayers.SetLayerRecursively(root, Eldoria.Terrain.TerrainLayers.Soldier);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Units/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// ISF Boarding Team (Specialist, ISF): 1 Boarding Sergeant (leader) + 4 Boarding Specialists, with the ISF Rifleman's
        /// stats (Health 5, Armor 2, Accuracy 75%, Move 10 m; Biological), each carrying a Shotgun and, as standard equipment,
        /// a Combat Shield (+1 Armor, so Armor 3 in battle). Models: the ISF Trooper and Sergeant with a shotgun in place of
        /// the rifle and a shield on the left arm. Existing assets keep their Inspector edits.
        /// </summary>
        public static UnitTemplate EnsureIsfBoardingTeam(Faction faction, Dictionary<string, Keyword> keywords)
        {
            EnsureFolder(DataDir, "Soldiers");
            EnsureFolder(DataDir, "Units");
            var shotgun = EnsureShotgun();
            EnsureEquipment();
            var shield = AssetDatabase.LoadAssetAtPath<Equipment>($"{DataDir}/Equipment/CombatShield.asset");

            SoldierTemplate Boarder(string file, string name, bool sergeant)
            {
                var s = GetOrCreate<SoldierTemplate>($"{DataDir}/Soldiers/{file}.asset", t =>
                {
                    t.displayName = name;
                    t.health = 5; t.armorLevel = 2; t.accuracy = 75; t.movementRange = 10f;   // the ISF Rifleman's
                    t.keywords = new List<Keyword> { keywords["Biological"] };
                    t.abilities = new List<Ability>();
                    t.weapons = new List<WeaponTemplate> { shotgun };
                    t.primaryWeapon = shotgun;
                    t.equipment = new List<Equipment> { shield };
                });
                if (s.prefab == null)
                {
                    s.prefab = BuildBoarderPrefab(file, s, sergeant);
                    EditorUtility.SetDirty(s);
                }
                return s;
            }

            var sergeantTemplate = Boarder("BoardingSergeant", "Boarding Sergeant", true);
            var specialist = Boarder("BoardingSpecialist", "Boarding Specialist", false);
            var unit = GetOrCreate<UnitTemplate>($"{DataDir}/Units/IsfBoardingTeam.asset", u =>
            {
                u.displayName = "ISF Boarding Team";
                u.role = UnitRole.Specialist;
                u.description = "Close-quarters assault troops trained to storm ships and strongpoints. Each carries a Shotgun (10 damage, penetration 1, 10 m) and a Combat Shield that raises their Armor Level to 3, so they can close the distance under fire and clear rooms and trenches at point-blank range. A Specialist: one per army.";
                u.leader = sergeantTemplate;
                u.members = new List<UnitTemplate.Member> { new UnitTemplate.Member { soldier = specialist, count = 4 } };
                u.deploymentSpacing = 1.5f;
            });
            if (!faction.units.Contains(unit))
            {
                faction.units.Add(unit);
                EditorUtility.SetDirty(faction);
            }
            return unit;
        }

        /// <summary>
        /// A Boarding Team model: a copy of the ISF Trooper (or, for the Sergeant, the Sergeant) model with its rifle replaced
        /// by a short shotgun and a tall shield carried on the left arm.
        /// </summary>
        private static GameObject BuildBoarderPrefab(string name, SoldierTemplate template, bool sergeant)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Units/{(sergeant ? "Sergeant" : "Trooper")}.prefab");
            var metal = GetOrCreateMaterial("Weapon", new Color(0.22f, 0.22f, 0.24f));
            var shieldMat = GetOrCreateMaterial("Shield_Steel", new Color(0.6f, 0.62f, 0.66f));
            var armor = GetOrCreateMaterial("Player_Armor", new Color(0.18f, 0.32f, 0.62f));

            var root = Object.Instantiate(source);
            root.name = name;
            var sso = new SerializedObject(root.GetComponent<Soldier>());
            sso.FindProperty("template").objectReferenceValue = template;
            sso.FindProperty("isSergeant").boolValue = sergeant;
            sso.ApplyModifiedPropertiesWithoutUndo();
            var rifle = root.transform.Find("Weapon");
            if (rifle != null) Object.DestroyImmediate(rifle.gameObject);

            // The shotgun, held at the right hip: a short, wide body with a pump and a thick barrel.
            Part(root, "Shotgun", PrimitiveType.Cube, metal, new Vector3(0.18f, 1.2f, 0.24f), new Vector3(0.08f, 0.11f, 0.5f));
            Part(root, "Shotgun Barrel", PrimitiveType.Cylinder, metal, new Vector3(0.18f, 1.23f, 0.58f), new Vector3(0.06f, 0.14f, 0.06f), new Vector3(90f, 0f, 0f));
            Part(root, "Shotgun Pump", PrimitiveType.Cube, shieldMat, new Vector3(0.18f, 1.16f, 0.46f), new Vector3(0.07f, 0.06f, 0.16f));
            // The shield on the left arm, angled slightly outward, with a stripe in the faction's colour and a view slit.
            Part(root, "Shield", PrimitiveType.Cube, shieldMat, new Vector3(-0.34f, 1.05f, 0.24f), new Vector3(0.05f, 0.95f, 0.5f), new Vector3(0f, -15f, 0f));
            Part(root, "Shield Stripe", PrimitiveType.Cube, armor, new Vector3(-0.37f, 1.05f, 0.25f), new Vector3(0.02f, 0.9f, 0.12f), new Vector3(0f, -15f, 0f));
            Part(root, "Shield Slit", PrimitiveType.Cube, metal, new Vector3(-0.36f, 1.4f, 0.25f), new Vector3(0.03f, 0.04f, 0.3f), new Vector3(0f, -15f, 0f));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/Units/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------ Equipment

        /// <summary>
        /// The equipment a soldier can be given in the army builder (if their soldier type lists it in Equipment Options):
        /// <list type="bullet">
        /// <item>Combat Shield: +1 Armor Level.</item>
        /// <item>Frag Grenade: gives the active ability Frag Grenade (once per battle; 2.5 m blast).</item>
        /// </list>
        /// No soldier type offers them yet. Existing assets keep their Inspector edits.
        /// </summary>
        public static void EnsureEquipment()
        {
            EnsureFolder(DataDir, "Equipment");
            GetOrCreate<Equipment>($"{DataDir}/Equipment/CombatShield.asset", e =>
            {
                e.displayName = "Combat Shield";
                e.description = "A shield carried on the arm. Adds 1 to the soldier's Armor Level.";
                e.armorLevel = 1;
            });
            var grenade = AssetDatabase.LoadAssetAtPath<Ability>($"{DataDir}/Abilities/FragGrenade.asset");
            GetOrCreate<Equipment>($"{DataDir}/Equipment/FragGrenade.asset", e =>
            {
                e.displayName = "Frag Grenade";
                e.description = "Gives the soldier the active ability Frag Grenade: thrown once per battle, a 2.5 m blast.";
                e.abilities = new List<Ability> { grenade };
            });
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
            EnsureDragonbornSkymasterSquad(factions[1], keywords);
            EnsureTyrantHoverTank(factions[1], keywords);
            EnsureKoboldMilitia(factions[1], keywords);
            EnsureDraconisWeaponNames(factions[1]);
            EnsureRaskUltima(factions[2], keywords);
            EnsureRaskRifleTeam(factions[2], keywords);
            EnsureRaskWeapons(factions[2]);
            EnsureStingerHeavyRobot(factions[2], keywords);
            EnsureIntimidatorMediumRobot(factions[2], keywords);
            EnsureIsfRifleSquad(factions[0], keywords);
            EnsureHeavyWeaponsTeam(factions[0], keywords);
            EnsureIsfRanger(factions[0], keywords);
            EnsureIsfBoardingTeam(factions[0], keywords);
            EnsureMortarTeam(factions[0], keywords);   // after the ISF Rifle Squad: it uses its ISF Rifleman
            EnsureSagittariusMbt(factions[0], keywords);
            EnsureOlympianMech(factions[0], keywords);
            EnsureEquipment();
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
            Kw("JumpPack", "Jump Pack", KeywordTarget.Soldier,
                "Able to climb up objects without movement cost: climbing onto an object costs this soldier 0 m (normally 10 m).");
            Kw("Hover", "Hover", KeywordTarget.Soldier,
                "Terrain cannot reduce the movement of this soldier: a move costs only its flat distance (no climbing cost, no wading penalty, slopes cost no extra).");
            Kw("Heavy", "Heavy", KeywordTarget.Soldier, "A heavy war machine. No effect on its own yet; other rules can check for it.");
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
                ["Large"] = KeywordCategory.Trait, ["Heavy"] = KeywordCategory.Trait, ["Jump Pack"] = KeywordCategory.Trait, ["Hover"] = KeywordCategory.Trait, ["Natural"] = KeywordCategory.Trait, ["Swarm"] = KeywordCategory.Trait, ["In close range"] = KeywordCategory.Status,
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
