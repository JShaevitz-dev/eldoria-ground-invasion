using System;
using System.Collections.Generic;
using System.Linq;
using Eldoria.Gameplay;
using Eldoria.Units;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Eldoria.Tests
{
    /// <summary>
    /// Test-made units. The game has no units of its own yet, so tests that need soldiers build them here from
    /// ScriptableObjects made in code, using the game's weapons, abilities, keywords and placeholder models.
    /// The stand-ins match the old demo units the maps used to have (Infantry Squad, Field Commander, Battle Priest), and
    /// <see cref="LoadScene"/> places them on a map where those units used to stand, so the map and movement tests keep
    /// working.
    /// </summary>
    public static class TestArmies
    {
        private const string Data = "Assets/_Project/Data";

        private static T Load<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }

        public static WeaponTemplate Weapon(string file) => Load<WeaponTemplate>($"{Data}/Weapons/{file}.asset");
        public static Ability Ability(string file) => Load<Ability>($"{Data}/Abilities/{file}.asset");
        public static GameObject Prefab(string name) => Load<GameObject>($"Assets/_Project/Prefabs/Units/{name}.prefab");
        public static Keyword Keyword(string name) => KeywordLibrary.Get(name);

        private static T Make<T>() where T : ScriptableObject
        {
            var t = ScriptableObject.CreateInstance<T>();
            t.hideFlags = HideFlags.HideAndDontSave;   // shared by every test: survives scene loads and unloading
            return t;
        }

        /// <summary>A test-made soldier type.</summary>
        public static SoldierTemplate Soldier(string name, int health, int armor, int accuracy, float move,
            IEnumerable<string> keywords, IEnumerable<Ability> abilities, IEnumerable<WeaponTemplate> weapons, GameObject prefab = null)
        {
            var s = Make<SoldierTemplate>();
            s.name = s.displayName = name;
            s.health = health; s.armorLevel = armor; s.accuracy = accuracy; s.movementRange = move;
            s.keywords = (keywords ?? Enumerable.Empty<string>()).Select(Keyword).Where(k => k != null).ToList();
            s.abilities = (abilities ?? Enumerable.Empty<Ability>()).Where(a => a != null).ToList();
            s.weapons = (weapons ?? Enumerable.Empty<WeaponTemplate>()).Where(w => w != null).ToList();
            s.primaryWeapon = s.weapons.FirstOrDefault();
            s.prefab = prefab;
            return s;
        }

        /// <summary>A test-made unit: a leader plus <paramref name="count"/> of <paramref name="member"/>.</summary>
        public static UnitTemplate Unit(string name, UnitRole role, SoldierTemplate leader, SoldierTemplate member = null, int count = 0)
        {
            var u = Make<UnitTemplate>();
            u.name = u.displayName = name;
            u.role = role;
            u.leader = leader;
            u.members = member != null && count > 0
                ? new List<UnitTemplate.Member> { new UnitTemplate.Member { soldier = member, count = count } }
                : new List<UnitTemplate.Member>();
            u.supplyCost = Eldoria.Rules.SupplyCost.Calculate(u, null);   // like the real units' (worked out by the catalog rebuild)
            return u;
        }

        /// <summary>A test-made faction that can take these units.</summary>
        public static Faction Faction(string name, Color color, params UnitTemplate[] units)
        {
            var f = Make<Faction>();
            f.name = f.displayName = name;
            f.color = color;
            f.units = units.ToList();
            return f;
        }

        // ------------------------------------------------------------------ The stand-ins for the old demo units

        private static SoldierTemplate trooper, sergeant, commander, priest;
        private static UnitTemplate infantry, fieldCommander, battlePriest;

        /// <summary>Trooper: Health 5, Armor 1, Accuracy 75%, Move 10 m; Biological; Frag Grenade; Assault Rifle.</summary>
        public static SoldierTemplate Trooper => trooper != null ? trooper : trooper =
            Soldier("Trooper", 5, 1, 75, 10f, new[] { "Biological" }, new[] { Ability("FragGrenade") },
                new[] { Weapon("AssaultRifle") }, Prefab("Trooper"));

        /// <summary>Sergeant: as the Trooper, with a Pistol too.</summary>
        public static SoldierTemplate Sergeant => sergeant != null ? sergeant : sergeant =
            Soldier("Sergeant", 5, 1, 75, 10f, new[] { "Biological" }, new[] { Ability("FragGrenade") },
                new[] { Weapon("AssaultRifle"), Weapon("Pistol") }, Prefab("Sergeant"));

        /// <summary>Field Commander: Health 10, Armor 1, Accuracy 75%, Move 10 m; Biological, Officer, Solo; Lead from the front; Pistol.</summary>
        public static SoldierTemplate Commander => commander != null ? commander : commander =
            Soldier("Field Commander", 10, 1, 75, 10f, new[] { "Biological", "Officer", "Solo" }, new[] { Ability("LeadFromTheFront") },
                new[] { Weapon("Pistol") }, Prefab("FieldCommander"));

        /// <summary>Battle Priest: Health 5, Armor 0, Accuracy 50%, Move 10 m; Biological, Officer, Solo, Magic; Divine Healing and Sanctuary; Pistol.</summary>
        public static SoldierTemplate Priest => priest != null ? priest : priest =
            Soldier("Battle Priest", 5, 0, 50, 10f, new[] { "Biological", "Officer", "Solo", "Magic" },
                new[] { Ability("DivineHealing"), Ability("DivineSanctuary") }, new[] { Weapon("Pistol") }, Prefab("BattlefieldPriest"));

        /// <summary>Infantry Squad (Infantry): a Sergeant and 9 Troopers.</summary>
        public static UnitTemplate InfantrySquad => infantry != null ? infantry : infantry =
            Unit("Infantry Squad", UnitRole.Infantry, Sergeant, Trooper, 9);

        /// <summary>Field Commander (Commander): one Field Commander.</summary>
        public static UnitTemplate FieldCommander => fieldCommander != null ? fieldCommander : fieldCommander =
            Unit("Field Commander", UnitRole.Commander, Commander);

        /// <summary>Battle Priest (Specialist): one Battle Priest.</summary>
        public static UnitTemplate BattlePriest => battlePriest != null ? battlePriest : battlePriest =
            Unit("Battle Priest", UnitRole.Specialist, Priest);

        private static readonly Dictionary<string, UnitTemplate> more = new Dictionary<string, UnitTemplate>();

        private static UnitTemplate Cached(string key, Func<UnitTemplate> make)
        {
            if (!more.TryGetValue(key, out var u) || u == null) more[key] = u = make();
            return u;
        }

        private static SoldierTemplate Sized(SoldierTemplate s, float radius, float height)
        {
            s.radius = radius; s.height = height;
            return s;
        }

        /// <summary>A Mechanical stand-in (the old Medium Tank): Health 20, Armor 4, Accuracy 75%, Move 30 m; Mechanical, Tank, Unstable; a cannon with Manual Load; a frag grenade usable every turn.</summary>
        /// <summary>A transport stand-in: the Medium Tank with the Transport keyword (which gives Embark and Disembark).</summary>
        public static UnitTemplate Transport => Cached("Transport", () => Unit("Transport", UnitRole.HeavySupport,
            Sized(Soldier("Transport", 20, 4, 75, 30f, new[] { "Mechanical", "Tank", "Transport" }, null,
                new[] { Weapon("Cannon100mm") }, Prefab("MediumTank")), 1.8f, 2.3f)));

        public static UnitTemplate MediumTank => Cached("MediumTank", () => Unit("Medium Tank", UnitRole.HeavySupport,
            Sized(Soldier("Medium Tank", 20, 4, 75, 30f, new[] { "Mechanical", "Tank", "Unstable" }, new[] { Ability("TankFragGrenade") },
                new[] { Weapon("Cannon100mm") }, Prefab("MediumTank")), 1.8f, 2.3f)));

        /// <summary>A walker stand-in (the old Stahl Colossus): Health 15, Armor 3, Move 15 m; Solo, Mechanical; Iron Claw and Stabilizers; Machine Gun and Iron Claw.</summary>
        public static UnitTemplate StahlColossus => Cached("StahlColossus", () => Unit("Stahl Colossus", UnitRole.HeavySupport,
            Sized(Soldier("Stahl Colossus", 15, 3, 75, 15f, new[] { "Solo", "Mechanical" }, new[] { Ability("IronClaw"), Ability("Stabilizers") },
                new[] { Weapon("MachineGun"), Weapon("IronClaw") }, Prefab("StahlColossus")), 0.7f, 3.6f)));

        /// <summary>A caster stand-in (the old Battle Mage): Health 5, Armor 0, Move 20 m; Biological, Solo, Officer, Magic; Firestorm and Fireball; Pistol.</summary>
        public static UnitTemplate BattleMage => Cached("BattleMage", () => Unit("Battle Mage", UnitRole.Specialist,
            Soldier("Battle Mage", 5, 0, 75, 20f, new[] { "Biological", "Solo", "Officer", "Magic" }, new[] { Ability("Firestorm"), Ability("Fireball") },
                new[] { Weapon("Pistol") }, Prefab("BattleMage"))));

        /// <summary>A rocket team stand-in (the old Tank Hunter Team): a Sergeant, 3 Troopers and a Rocket Trooper (Rocket Launcher ability).</summary>
        public static UnitTemplate TankHunterTeam => Cached("TankHunterTeam", () =>
        {
            var u = Unit("Tank Hunter Team", UnitRole.HeavySupport,
                Soldier("Sergeant", 5, 1, 75, 10f, new[] { "Biological" }, null, new[] { Weapon("AssaultRifle"), Weapon("Pistol") }, Prefab("Sergeant")),
                Soldier("Trooper", 5, 1, 75, 10f, new[] { "Biological" }, null, new[] { Weapon("AssaultRifle") }, Prefab("Trooper")), 3);
            u.members.Add(new UnitTemplate.Member { count = 1, soldier =
                Soldier("Rocket Trooper", 5, 1, 75, 10f, new[] { "Biological" }, new[] { Ability("RocketLauncher") }, new[] { Weapon("AssaultRifle") }, Prefab("RocketTrooper")) });
            return u;
        });

        /// <summary>A melee squad stand-in (the old Paladin Squad): a Sergeant (Health 10) and 4 Troopers in Powered Armor (Armor 2, Move 6 m) with Broadswords.</summary>
        public static UnitTemplate PaladinSquad => Cached("PaladinSquad", () => Unit("Paladin Squad", UnitRole.HeavySupport,
            Soldier("Sergeant in Powered Armor", 10, 2, 75, 6f, new[] { "Biological" }, null, new[] { Weapon("Broadsword") }, Prefab("PaladinSergeant")),
            Soldier("Trooper in Powered Armor", 5, 2, 75, 6f, new[] { "Biological" }, null, new[] { Weapon("Broadsword") }, Prefab("PaladinTrooper")), 4));

        /// <summary>A sniper stand-in (the old Sniper): Health 5, Armor 0, Accuracy 100%, Move 15 m; Biological, Sniper Precision, Solo; Sniper Rifle.</summary>
        public static UnitTemplate Sniper => Cached("Sniper", () => Unit("Sniper", UnitRole.Specialist,
            Soldier("Sniper", 5, 0, 100, 15f, new[] { "Biological", "Sniper Precision", "Solo" }, null, new[] { Weapon("SniperRifle") }, Prefab("Sniper"))));

        /// <summary>A machine gun team stand-in (the old Machine Gun Team): a Sergeant (Pistol), 3 Troopers (SMG, Pistol) and a Machine Gunner.</summary>
        public static UnitTemplate MachineGunTeam => Cached("MachineGunTeam", () =>
        {
            var u = Unit("Machine Gun Team", UnitRole.HeavySupport,
                Soldier("Sergeant", 5, 1, 75, 10f, new[] { "Biological" }, null, new[] { Weapon("Pistol") }, Prefab("Sergeant")),
                Soldier("Trooper", 5, 1, 75, 10f, new[] { "Biological" }, null, new[] { Weapon("SMG"), Weapon("Pistol") }, Prefab("Trooper")), 3);
            u.members.Add(new UnitTemplate.Member { count = 1, soldier =
                Soldier("Machine Gunner", 5, 1, 75, 10f, new[] { "Biological" }, null, new[] { Weapon("MachineGun") }, Prefab("MachineGunner")) });
            return u;
        });

        /// <summary>A powered armor stand-in (the old Powered Armor Squad): a Sergeant (Health 10) and 4 Troopers, Armor 2, Move 6 m, Assault Rifles.</summary>
        public static UnitTemplate PoweredArmorSquad => Cached("PoweredArmorSquad", () => Unit("Powered Armor Squad", UnitRole.Specialist,
            Soldier("Sergeant in Powered Armor", 10, 2, 75, 6f, new[] { "Biological" }, null, new[] { Weapon("AssaultRifle") }, Prefab("PoweredArmorSergeant")),
            Soldier("Trooper in Powered Armor", 5, 2, 75, 6f, new[] { "Biological" }, null, new[] { Weapon("AssaultRifle") }, Prefab("PoweredArmorTrooper")), 4));

        /// <summary>The stand-in for an old Data/Units file name (throws if there is none).</summary>
        public static UnitTemplate ByFile(string file) => file switch
        {
            "InfantrySquad" => InfantrySquad,
            "FieldCommander" => FieldCommander,
            "BattlefieldPriest" => BattlePriest,
            "MediumTank" => MediumTank,
            "StahlColossus" => StahlColossus,
            "BattleMage" => BattleMage,
            "TankHunterTeam" => TankHunterTeam,
            "PaladinSquad" => PaladinSquad,
            "Sniper" => Sniper,
            "MachineGunTeam" => MachineGunTeam,
            "PoweredArmorSquad" => PoweredArmorSquad,
            _ => throw new ArgumentException($"No test stand-in for the unit '{file}'"),
        };

        /// <summary>The old demo army: three Infantry Squads, a Field Commander and a Battle Priest.</summary>
        public static List<UnitTemplate> DemoArmy => new List<UnitTemplate>
            { InfantrySquad, FieldCommander, InfantrySquad, BattlePriest, InfantrySquad };

        // ------------------------------------------------------------------ Maps

        /// <summary>
        /// Loads a map with the test armies where the old demo units stood. They're placed as the scene finishes loading
        /// (after Awake, before Start), so the game controller finds them as if they had been in the scene.
        /// Nothing is placed if armies were chosen (<see cref="ArmySelection"/>): those are deployed instead.
        /// </summary>
        public static void LoadScene(string scene)
        {
            void Place(Scene loaded, LoadSceneMode mode)
            {
                SceneManager.sceneLoaded -= Place;
                if (ArmySelection.HasArmies) return;
                if (loaded.name == "CubeRoom") PlaceCubeRoomArmies();
                else if (loaded.name == "UrbanDemo") PlaceUrbanArmies();
                else if (loaded.name == Battlefields.EnglishCountryside.Scene || loaded.name == Battlefields.PolarNonsense.Scene) PlaceDemoArmyInZones();
            }
            SceneManager.sceneLoaded += Place;
            SceneManager.LoadScene(scene);
        }

        private static Transform Root()
        {
            var root = GameObject.Find("Armies");
            return root != null ? root.transform : new GameObject("Armies").transform;
        }

        private static Unit Spawn(UnitTemplate template, Side side, Vector3 at, string name, bool leaderMarker = true)
        {
            var facing = Quaternion.Euler(0f, side == Side.Player ? 0f : 180f, 0f);
            var unit = ArmyDeployment.Spawn(template, side, at, facing, name, Root());
            if (!leaderMarker && unit.TryGetComponent<LeaderMarker>(out var marker)) Object.Destroy(marker);
            return unit;
        }

        /// <summary>The cube room: the player's squad, Field Commander and Battle Priest in the south, an enemy squad north of the container.</summary>
        public static void PlaceCubeRoomArmies()
        {
            Spawn(InfantrySquad, Side.Player, new Vector3(0f, 0f, -16f), "Infantry Squad");
            Spawn(InfantrySquad, Side.AI, new Vector3(-4f, 0f, 6.5f), "Infantry Squad (Enemy)");
            Spawn(FieldCommander, Side.Player, new Vector3(0f, 0f, -20.5f), "Field Commander");
            Spawn(BattlePriest, Side.Player, new Vector3(3f, 0f, -20.5f), "Battle Priest");
        }

        /// <summary>The Boxed in: both sides field three Infantry Squads, a Field Commander and a Battle Priest, 180° apart.</summary>
        public static void PlaceUrbanArmies()
        {
            foreach (var side in new[] { Side.Player, Side.AI })
            {
                float s = side == Side.Player ? 1f : -1f;
                string suffix = side == Side.Player ? "" : " (Enemy)";
                Spawn(InfantrySquad, side, new Vector3(-22f, 0f, -24f) * s, "Infantry Squad 1" + suffix);
                Spawn(InfantrySquad, side, new Vector3(0f, 0f, -24f) * s, "Infantry Squad 2" + suffix);
                Spawn(InfantrySquad, side, new Vector3(22f, 0f, -24f) * s, "Infantry Squad 3" + suffix);
                Spawn(FieldCommander, side, new Vector3(-11f, 0f, -27f) * s, "Field Commander" + suffix);
                Spawn(BattlePriest, side, new Vector3(11f, 0f, -27f) * s, "Battle Priest" + suffix);
            }
        }

        /// <summary>Any map with deployment zones: the demo army in each side's zone, one unit per slot.</summary>
        public static void PlaceDemoArmyInZones()
        {
            var army = DemoArmy;
            foreach (var zone in Object.FindObjectsByType<DeploymentZone>(FindObjectsSortMode.None))
            {
                var names = ArmyDeployment.UnitNames(army, zone.Side);
                for (int i = 0; i < army.Count; i++)
                    Spawn(army[i], zone.Side, ArmyDeployment.Slot(zone.transform.position, zone.Size, zone.Side, i), names[i]);
            }
        }
    }
}
