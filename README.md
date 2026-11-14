# Eldoria Ground Invasion

> **A fork of Eldoria with its first three factions. Two are playable:** the International Space Force of Humanity
> (Field Commander, ISF Rifle Squad, Heavy Weapons Team, Mortar Team, Sagittarius MBT, ISF Ranger) and the Imperial Army of The Draconis Imperialis (Dragon
> Hierarch, Dragonborn Squad). The Rask have no units yet. Every game system is here (army lists and factions, movement,
> terrain and cover, shooting, keywords, abilities, the AI, multiplayer, both maps and the menus). The weapons,
> abilities, keywords and placeholder soldier models are kept as a starting kit for new units.

Eldoria Ground Invasion is a space opera turn-based strategy game focused on company-level warfare across a variety of
battlefields. Its tone sits between Star Trek and Warhammer 40k. The Free Nations Alliance (humanity, the Skael, the
Chrome, the Sphinxians, and associates such as the Rask and the Hive) faces Draconis Imperialis, the empire of the
Dragons and their bred Kobold and Dragonborn castes, across an uneasy cease-fire. The setting and its races are
described in [`Docs/Lore.md`](Docs/Lore.md). This fork leaves behind the original Eldoria's WWII-inspired fantasy.

The goal of this project is to offer the tactical freedom of tabletop wargames with the convenience and tactical
minutiae of a computer strategy game.

Like on a tabletop, there's no grid and no hexes: soldiers move, take cover and shoot across an open 3D battlefield,
with every distance measured in meters. The game is built in Unity and is an early proof of concept, focused on
getting the rules right.

## Play it

Build the game (see **Windows build** below) and run `Builds/Eldoria/Eldoria.exe`. The main menu has **Play**,
**Multiplayer**, **Quit** and the turn timer option (off by default, 10 minutes per turn).

After **Play**, you'll:

1. **Choose a battlefield:** the Boxed in or the Open Season.
2. **Choose your faction**, which decides which units and commanders you can take.
3. **Build your army:** up to 6 units, exactly 1 Commander, and no duplicate Specialist units. Units whose soldiers
   have weapon or equipment options can be customized. Then press **Confirm army**.

The two maps:

- **Boxed in:** an 80 × 60 m city block with a central plaza, laid out with 180° symmetry so both sides start out even.
- **Open Season:** 100 × 100 m of open, rolling farmland. Hills block line of sight. A river crosses the map:
  wading it costs double movement, unless you take the stone bridge or the ford. A farmhouse, a barn, a stone shed,
  and hay bales, carts and fences give cover.

The AI picks a random faction and builds its own random army under the same rules. You go first. Wipe out the enemy to
win.

### Multiplayer

Two players can play each other over a network. On the main menu, pick **Multiplayer**:

1. One player presses **Host**. The game shows their IP address and the port (**7777**).
2. The other player presses **Join**, types in the host's IP address, and presses **Connect**.
3. The host chooses the battlefield. Then both players choose a faction and build an army, and the battle starts once
   both have confirmed. Who deploys at the south edge and moves first is decided at random.

Things to check:

- **Firewall:** the first time you host, Windows asks whether to let Eldoria through the firewall. Allow it, at least on
  private networks.
- **Same network (LAN):** use the address the host's game shows (usually `192.168.x.x` or `10.x.x.x`).
- **Over the internet:** the host's router must forward **TCP port 7777** to the host's computer, and the other player
  joins the host's *public* IP address. A virtual LAN tool, such as a VPN shared by both players, also works and avoids
  port forwarding.
- Both players need the same version of the game.

## Controls

**Camera**

| Input | Action |
|---|---|
| W A S D / arrow keys | Pan |
| Middle mouse drag | Pan |
| Right mouse drag | Rotate |
| Q / E | Rotate in steps |
| Mouse wheel | Zoom |

**Commanding a unit**

| Input | Action |
|---|---|
| Click a unit / Tab | Select a unit (Tab cycles through them) |
| Click the ground | Move the unit leader, then the rest of the unit |
| H | Leader holds position |
| G | Gather the rest of the unit around the leader |
| Enter | End the move |
| Backspace | Undo the move |
| F | Shoot: then click an enemy soldier |
| Esc | Back out: stop aiming, or deselect a unit that hasn't moved |
| N | End your turn |
| Space | Skip ahead while the AI is playing |
| R | Play again once the battle is over |

Abilities (grenades, spells and so on) have buttons on the HUD when the selected unit has them.

## The rules

All the game rules live in [`Docs/Rules.md`](Docs/Rules.md): movement and cohesion, terrain and cover, line of sight,
accuracy and crits, armor penetration, keywords, abilities, spells, turns and winning. Each section says whether it's
implemented.

At a glance:

- **Units, not individual soldiers.** A unit's leader moves first (normal move, or sprint for double), and everyone else
  must end within 5 m of them. If the leader dies, another soldier *in the same unit* takes over.
- **Shooting is the unit's action** and ends its turn. Soldiers spread their fire across the target unit: each picks
  an enemy nobody else is aiming at if it can (the closest first), and only doubles up once every target is covered. The dead are removed when the volley ends.
- **Cover matters.** Half-cover costs the shooter 25% accuracy. A Full-cover object between shooter and target blocks the
  shot entirely.
- **Keywords drive the interactions**, such as Biological, Toxic, Incendiary, Deployed, Solo, Spell and Sniper Precision.

### Factions

| Faction | Race | Colour |
|---|---|---|
| **International Space Force of Humanity** | Humanity (Free Nations Alliance) | slate navy blue |
| **Imperial Army of The Draconis Imperialis** | Dragons, Dragonborn and Kobolds | imperial crimson |
| **Most Holy Army of The Rask Divine Collective** | Rask (Alliance associate) | ochre/bronze |

A **faction** (Data/Factions) has a name, a colour, the units it can take and optional weapon changes. The three are
created by **Eldoria > Rebuild Unit Catalog** (`SquadBuilder.FactionNames`), which keeps any edits made to them in the
Inspector.

### Units

| Unit | Faction | Role | Made of |
|---|---|---|---|
| **Field Commander** | International Space Force of Humanity | Commander | Field Commander (Health 10, Armor 2, Pistol, Lead from the front) |
| **Mortar Team** | International Space Force of Humanity | Heavy Support | ISF Mortar Specialist (Mortar Launcher, Assault Rifle) + ISF Rifleman; Armor 2 |
| **Sagittarius MBT** | International Space Force of Humanity | Heavy Support | Sagittarius MBT (the original Eldoria's Medium Tank: Health 20, Armor 4, 100mm Cannon) |
| **ISF Ranger** | International Space Force of Humanity | Specialist | ISF Ranger (the original Eldoria's Sniper with Armor 1: Accuracy 100%, Sniper Rifle, Anti-Materiel Rifle or Shotgun, Sniper Precision; Chromashift Textiles: −25% to anyone aiming at it) |
| **Dragon Hierarch** | Imperial Army of The Draconis Imperialis | Commander | Dragon Hierarch (Health 15, Armor 2, Accuracy 50%, Move 30 m, Large; Dragon Claw, a 2 m melee weapon) |
| **Dragonborn Squad** | Imperial Army of The Draconis Imperialis | Infantry | Dragonborn Sergeant + 4 Dragonborn Footmen (Health 10, Armor 2; Dragon Glaive: a 20 m rifle and a 2 m Melee blade) |
| **ISF Rifle Squad** | International Space Force of Humanity | Infantry | ISF Rifleman Sergeant + 9 ISF Riflemen (Armor 2, Assault Rifles, Frag Grenade) |
| **Heavy Weapons Team** | International Space Force of Humanity | Heavy Support | ISF Rifleman Sergeant (Pistol) + 3 ISF Riflemen (Assault Rifles) + ISF Heavy Weapons (Machine Gun, Rocket Launcher or Flamethrower); Armor 2 |

A **unit** (Data/Units) is a leader soldier plus other soldiers; each **soldier** (Data/Soldiers) has stats, keywords,
weapons, abilities and a model. After adding them, run **Eldoria > Rebuild Unit Catalog** so the army builder lists
them (and add each unit to its faction's unit list). They're created in code
(`SquadBuilder.EnsureFieldCommander`, `EnsureIsfRifleSquad`, `EnsureHeavyWeaponsTeam`, `EnsureIsfRanger`, `EnsureMortarTeam`, `EnsureSagittariusMbt`, `EnsureDragonHierarch`,
`EnsureDragonbornSquad`; the dragon and Dragonborn placeholder models are built from primitives the first time), keeping any Inspector edits. Every unit has a role: Commander, Infantry, Heavy Support or
Specialist.

Kept as a starting kit: weapons (Assault Rifle, Pistol, SMG, Machine Gun, Flamethrower, Sniper Rifle, 100mm Cannon,
Rocket Launcher, Broadsword, Iron Claw, the new Anti-Materiel Rifle: Damage 10, Penetration X, 50 m, Deployed; and the new Mortar Launcher:
Damage 5, Penetration 3, 50 m, Deployed, Manual Load, Explosive; and the new Shotgun: Damage 10, Penetration 1, 10 m), abilities (Frag Grenade, Lead from the front, Divine Healing, Divine Sanctuary,
Fireball, Firestorm, Rocket Launcher, Iron Claw, Stabilizers), every keyword, and the placeholder soldier models in
Prefabs/Units.

## Working on the project

**Requirements:** Unity **6000.3.1f1** (Unity 6.3) with the Universal Render Pipeline and the Input System package. Open
the repo folder in Unity Hub. (The local folder may be named `Eldoira`; the project is Eldoria.)

**Layout**

```
Assets/_Project/
  Scripts/
    Rules/        the rules engine: shooting, attacks, keywords, abilities, turns, victory
    Units/        templates (units, soldiers, weapons, keywords, abilities) and runtime soldiers/units
    Terrain/      terrain objects, cover, line of sight, movement measuring
    AI/           the AI planner
    Gameplay/     player input, HUD, AI turns, audio, menus
    CameraControl/
    Core/         battlefield scale and the shared random number generator
  Data/           ScriptableObject assets: Weapons, Keywords, Abilities, Factions, Units, Soldiers
  Editor/         builders for data, maps, scenes and the Windows build
  Scenes/         MainMenu, UrbanDemo, Countryside, CubeRoom (test room)
  Tests/          EditMode and PlayMode tests
Docs/Rules.md     the rules
```

**Editor menu (Eldoria):** Build Boxed in, Build Open Season, Build Main Menu, Build Cube Room, Rebuild Unit Catalog, Build Game (Windows).
`SquadBuilder` keeps the keyword library and the unit catalog (Resources/UnitCatalog) in step with the assets in Data.

**Tests** build their units from test-made ScriptableObjects (`Tests/PlayMode/TestArmies.cs`: stand-ins for an infantry
squad, a commander, a priest and a few others, using the kept weapons, abilities and models), and place them on the maps
where the tests need armies.

**Tests:** run them in Unity's Test Runner, or from the command line:

```bash
Unity.exe -batchmode -projectPath . -runTests -testPlatform EditMode -testResults edit.xml
```

```bash
Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults play.xml
```

**Windows build:** use **Eldoria > Build Game (Windows)**, or run it in batch mode:

```bash
Unity.exe -batchmode -quit -projectPath . -executeMethod Eldoria.EditorTools.GameBuilder.BuildWindows
```

The game is written to `Builds/Eldoria/`. Run `Eldoria.exe -smokeTest` to have it build an ISF army, start a battle on the
Open Season, play a few AI turns on its own and report whether everything loaded.

To test multiplayer, run two copies on one computer, starting the host first. They connect over `127.0.0.1` and the
host's map choice reaches the other game; they play three turns against each other and check that both games agree on
the battle after every order:

```bash
Eldoria.exe -batchmode -nographics -mpSmoke host -logFile host.log
```

```bash
Eldoria.exe -batchmode -nographics -mpSmoke join -logFile join.log
```

## Status

A fork of the Eldoria proof of concept with its factions and units removed. Its first three factions are in; next up: their units.

## Audience and business model

Eldoria is aimed at players aged 16 to 40, mostly male, who play strategy games competitively. The pitch to them:
*"With this game you can play more matches of our strategy game in a night than you could play physically in a year."*

There are three planned revenue streams:

1. **New commanders and factions.** Commanders bring exclusive units, and new factions are released periodically to
   refresh the competitive environment.
2. **Cosmetic items**, so players can customize their army to their liking.
3. **Seasonal battle passes** containing customization items.
