# Eldoria Ground Invasion

> **A fork of Eldoria with no factions or units yet.** Every game system is here (army lists and factions, movement,
> terrain and cover, shooting, keywords, abilities, the AI, multiplayer, both maps and the menus), but there are no
> faction, unit or soldier assets, so the army builder is empty and a battle can't be started: the game says so
> instead. The weapons, abilities, keywords and placeholder soldier models are kept as a starting kit for new units.

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

Until factions and units are added, **Play** lets you choose a battlefield and then shows that there are no factions or
units, so no battle can start. Once there are, you'll:

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

### Units and factions

None yet. A **faction** (Data/Factions) has a name, a colour, the units it can take and optional weapon changes. A
**unit** (Data/Units) is a leader soldier plus other soldiers; each **soldier** (Data/Soldiers) has stats, keywords,
weapons, abilities and a model. After adding them, run **Eldoria > Rebuild Unit Catalog** so the army builder lists
them. Every unit has a role: Commander, Infantry, Heavy Support or Specialist.

Kept as a starting kit: weapons (Assault Rifle, Pistol, SMG, Machine Gun, Flamethrower, Sniper Rifle, 100mm Cannon,
Rocket Launcher, Broadsword, Iron Claw), abilities (Frag Grenade, Lead from the front, Divine Healing, Divine Sanctuary,
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
  Data/           ScriptableObject assets: Weapons, Keywords, Abilities (Factions, Units, Soldiers: none yet)
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

The game is written to `Builds/Eldoria/`. Run `Eldoria.exe -smokeTest` to have it open the menus on its own and report
whether everything loaded (with no units, it checks that the army builder says so and no battle starts).

To test multiplayer, run two copies on one computer, starting the host first. They connect over `127.0.0.1` and the
host's map choice reaches the other game; with no units, both check that their army is refused (with units, they play
three turns against each other and check that both games agree on the battle after every order):

```bash
Eldoria.exe -batchmode -nographics -mpSmoke host -logFile host.log
```

```bash
Eldoria.exe -batchmode -nographics -mpSmoke join -logFile join.log
```

## Status

A fork of the Eldoria proof of concept with its factions and units removed. Next up: new factions and units.

## Audience and business model

Eldoria is aimed at players aged 16 to 40, mostly male, who play strategy games competitively. The pitch to them:
*"With this game you can play more matches of our strategy game in a night than you could play physically in a year."*

There are three planned revenue streams:

1. **New commanders and factions.** Commanders bring exclusive units, and new factions are released periodically to
   refresh the competitive environment.
2. **Cosmetic items**, so players can customize their army to their liking.
3. **Seasonal battle passes** containing customization items.
