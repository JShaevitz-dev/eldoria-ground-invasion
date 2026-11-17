# Ground Invasion — Game Rules

Living record of the rules decided so far. **Status** shows what the code does today.

> **This fork (Ground Invasion, of Eldoria) has its first three factions, and all three can field armies:** the ISF (Field
> Commander, Master Sergeant Jakoby, Marshal Command Tank, ISF Rifle Squad, Heavy Weapons Team, Mortar Team, Sagittarius MBT, Olympian Mech, ISF Ranger, ISF Boarding Team, ISF Combat Engineer, ISF Combat Medic, Fist of Mars, Workhorse Transport), the Draconis Imperialis (Dragon Hierarch, Dragonborn Exemplar, Dragonborn Squad, Kobold Militia, Dragonborn Skymaster Squad, Tyrant Hover-tank, Kobold Mechanist, Kobold Heartstrike Spear Team) and the Rask (Rask Ultima, Rask Rifle Team, Stinger Heavy Robot, Intimidator Medium Robot, Rask Blackhand, Rask Redhand, Rask Reliquary). The game
> systems below (army lists, factions, movement, terrain, shooting, keywords, abilities, turns, the AI, multiplayer) are
> all in place. The weapons,
> abilities, keywords and placeholder soldier models are kept as a starting kit for new units.
>
> **Setting:** a space opera (see [Lore.md](Lore.md)), replacing the original WWII-inspired fantasy. Some names below
> still come from the old setting (e.g. **Magic** and **Spell** keywords, the Fireball and Divine Healing abilities);
> they're kept until the new factions are designed.

## Scale

- All distances are in **meters** (1 Unity unit = 1 m). Soldiers are human-sized (~1.8 m tall) and have no bases.
- Distances are measured horizontally between soldiers' positions.

## Squads

- A squad is a group of soldiers that moves together, e.g. a sergeant and nine troopers.
- One living soldier holds the **squad leader** role. At the start of a battle it is the sergeant.

### Leader succession — *implemented*

- If the squad leader is killed, the living squad member **physically closest** to where the leader fell becomes the new squad leader. Only soldiers **of that same squad** can inherit command — never a soldier of another unit, however close.
- If two or more soldiers are equally close (within 1 cm), a **random roll** picks one of them.
- This repeats every time a leader falls, until the squad is wiped out.

## Units, soldiers and weapons — *implemented*

- A **unit** is made of **soldiers**, who carry **weapons**. Each is defined by a template asset (Assets/_Project/Data):
  - **Unit template:** the soldier class that leads the unit, plus which other soldier classes it contains and how many.
  - **Soldier template:** the soldier's stats, keywords and the weapon classes they carry.
  - **Weapon template:** the weapon's stats and keywords.

| Soldier stat | Meaning |
|---|---|
| Health | Damage the soldier can take. |
| Armor Level | Protection, compared against a weapon's Penetration. |
| Accuracy | Base chance to hit, in percent. |
| Movement range | Normal move in meters. Sprinting doubles it. |
| Keywords | Extra properties, e.g. Biological, Robotic. |

| Weapon stat | Meaning |
|---|---|
| Base Damage | Damage dealt by a hit. |
| Penetration | How well it gets through Armor Level. |
| Accuracy Bonus | Added to the soldier's Accuracy. |
| Range | The farthest a target can be (meters, horizontal). None defined = unlimited. |
| Keywords | Extra properties, e.g. Explosive, Incendiary. |

## Units

### Roles — *implemented*

Every unit has a **role**: **Infantry**, **Commander**, **Heavy Support**, **Specialist** (any unit not given another
role) or **Transport**. It's shown in the HUD when the unit is selected, and the army builder lists units Commanders first, then Infantry, Heavy Support, Specialists and Transports.

| Role | Units |
|---|---|
| **Infantry** | ISF Rifle Squad, Dragonborn Squad, Kobold Militia, Rask Rifle Team |
| **Commander** | Field Commander, Master Sergeant Jakoby, Marshal Command Tank, Dragon Hierarch, Dragonborn Exemplar, Rask Ultima |
| **Heavy Support** | Heavy Weapons Team, Mortar Team, Sagittarius MBT, Olympian Mech, Stinger Heavy Robot, Tyrant Hover-tank, Intimidator Medium Robot |
| **Specialist** | ISF Ranger, ISF Boarding Team, ISF Combat Engineer, ISF Combat Medic, Fist of Mars, Dragonborn Skymaster Squad, Kobold Mechanist, Kobold Heartstrike Spear Team, Rask Blackhand, Rask Redhand, Rask Reliquary |
| **Transport** | Workhorse Transport. Like every role but Commander, an army can take more than one. |

**The role ecosystem** (design intent, used when designing and balancing units; not a rule in itself):

- **Heavy Support beats Infantry and armor:** it brings maximum firepower to bear on large infantry units or armored Heavy Support.
- **Specialists beat Heavy Support:** either by fortifying infantry against it, or as a direct answer to Heavy Support (or to other Specialists).
- **Infantry beats unsupported Specialists and Heavy Support, and is best at objectives.** Infantry get **no objective-specific rules**: when objectives arrive that need a unit to interact with them or hold them over several rounds, infantry's staying power is what holds the line.

### Factions — *implemented*

Every army belongs to a **faction**. The faction a player chooses at the start of building their list decides **which units and commanders** they can take.

| Faction | Race | Colour |
|---|---|---|
| **International Space Force of Humanity** | Humanity (Free Nations Alliance) | grey (#aaaaaa) |
| **Imperial Army of The Draconis Imperialis** | Dragons, Dragonborn and Kobolds | imperial crimson |
| **Most Holy Army of The Rask Divine Collective** | Rask (Alliance associate) | ochre/bronze |

So far the ISF has the Field Commander, Master Sergeant Jakoby, the Marshal Command Tank, the ISF Rifle Squad, the Heavy Weapons Team, the Mortar Team, the Sagittarius MBT, the Olympian Mech, the ISF Ranger, the ISF Boarding Team, the ISF Combat Engineer, the ISF Combat Medic, the Fist of Mars and the Workhorse Transport, and the Draconis Imperialis has the Dragon Hierarch, the Dragonborn Exemplar, the Dragonborn Squad, the Kobold Militia, the Dragonborn Skymaster Squad, the Tyrant Hover-tank, the Kobold Mechanist and the Kobold Heartstrike Spear Team, and the Rask have the Rask Ultima, the Rask Rifle Team, the Stinger Heavy Robot, the Intimidator Medium Robot, the Rask Blackhand, the Rask Redhand and the Rask Reliquary, so all three can field armies (the AI picks one of them).
A faction is an asset (Data/Factions) with a name, a colour, the units
it can take, and any weapon changes and names (below). The army builder lists them in the order above. See
[Lore.md](Lore.md) for each race.

**Faction colours:** a unit's **primary colour** (its soldiers' bodies and caps) is its faction's colour. Every faction uses the same models and materials: the primary-colour parts are tinted per unit when it deploys, so a new faction only needs a colour, not new textures. Other parts keep their own colours (skin, weapons, gold trim, the officer's cape, the priest's white robe, powered armor plates). Both sides can choose the same faction; the soldier labels (blue for yours, red for the enemy's) still tell them apart. The army builder shows each faction's **flag** (Textures/Flags; a square of its colour for a faction without one): ISF navy and steel with the Shield Bearer's shield, Draconis crimson and gold with a crown over dragon wings, Rask ochre and brown with the Craftsman's halo, gear, hammer and chisel. Flags for the planned Chrome (a white equilateral triangle on a teal roundel, on polished chrome) and Hive (a honeycomb cluster around the brain caste's eye) are drawn and waiting for their factions.

**Faction weapons:** a faction can change its soldiers' weapons, without new weapons being made:

- **Stat changes:** each faction can list changes to a weapon's stats: Damage, Shots, Penetration, Accuracy Bonus and Range. They are **added to the weapon's own stats** (negative values subtract), and several changes to the same weapon add up. Example: *Assault Rifle: Damage +1, Range −5 m* makes that faction's Assault Rifles 5 damage, 15 m.
- **Names (aliases):** each faction can give a weapon its **own name**. Example: *Assault Rifle → "Sturmgewehr"*. It is the **same weapon and stat block** underneath (with that faction's stat changes, if any): only the name shown changes, in the army builder, the HUD and the combat log.
- The changes apply to whichever soldier holds the weapon, by **their unit's faction**, including a thrown grenade or an explosion (the faction of the soldier who used it). Armies placed directly in a scene have no faction, so their weapons are unchanged.
- **Limits:** Damage never goes below 0, Shots below 1, Penetration below 0 or Range below 1 m. A weapon with **no penetration value** keeps none, and **unlimited range** stays unlimited.
- The army builder shows each unit's weapons **as the chosen faction has them**.
- **So far:** the Draconis Imperialis calls the Assault Rifle the **Fang Caster** and the Broadsword the **Ceremonial Blade**; the Rask call it the **Solntse Laser Rifle** and add **+2 Damage** to it (6).

### Army lists — *implemented*

Before a battle, the player chooses the **map** (Boxed in, Open Season, the Siege of Ft Solstice, Polar Nonsense or Leisure Cruise), then each side chooses a **faction** and builds an **army list** from that faction's units:

- Every unit must be one the army's **faction** has access to.
- **Supply Cap:** the Supply Costs of an army's units can add up to **no more than 750**. There's **no limit on how many units** an army has. The army builder shows the list's total against the cap ("Supply 520 / 750") and won't add a unit that would go over it.
- It must contain **exactly 1 Commander** unit: at least one, and **only 1 Commander is allowed** in a list at a time.
- **Only the Commander is unique** (an army has exactly one), **and units with the Unique keyword**: only one of each may appear in an army list (the **Fist of Mars**; Master Sergeant Jakoby, a Commander, has it too). Other **Infantry**, **Heavy Support** and **Specialist** units can all be taken more than once. The army builder greys out a second one (*"Already in the army (Unique: only one per army)"*).
- **Units only a Commander brings:** a unit can be set to need a particular Commander (or one of several: the unit's *Only With Commanders* list). It's **hidden in the army builder** until one of those Commanders is in the list, then shown with "Only with ..." under its role; it can't be added (and a list with it isn't valid) without one. **Removing the Commander also removes those units** from the list. Random armies only take them with their Commander. *(No unit uses this yet.)*

**Map choice, then army builder:** pressing **Play** first asks which **battlefield** to fight on (each with a short description); **Back** in the army builder returns to it. Choosing one opens the army builder on the **faction choice**; **Change faction** goes back to it (and empties the list). It then lists every unit the faction can take with an **Add** button and its details: role and size; **stats** for each soldier type (health, armor, accuracy, move, weapons), each **weapon** (damage, penetration, accuracy bonus, range, keywords), its **abilities** and **keywords**, all read from the game data; and a short **description** of how the unit plays and what it does in an army. The Add button is greyed out with the reason when the unit can't be added; your list shows the 6 slots with **Remove** buttons. It starts with the catalog's demo army (keeping only units the faction has; none set yet), or your last confirmed army if you choose the same faction again. **Random** builds a random valid army; **Clear** empties the list. **Save army** saves the list (its faction, units and each unit's weapon and equipment choices) as a file under a name you give it (saving under a name that's taken replaces that army); **Load army**, in the builder or on the faction choice, lists the saved armies (most recent first, with faction, unit count and date) to **Load** or **Delete** (click twice). Loading replaces the list and switches to the army's faction; a file the game can't read, or one naming units, factions or loadout choices it doesn't have (or units its faction can't take), is refused with the reason and the list is left as it was. A loaded army is checked against the rules, so one that no longer fits (e.g. after a Supply Cost changed) says why. Army files are JSON (`<name>.army.json`) in the Armies folder of the game's save data, and can be copied between players. **Confirm army** checks the list against the rules: if it isn't valid, it says why and you stay in the builder; if it is, you go to the battlefield.

**Supply Cost:** every unit has a **Supply Cost**, shown in the army builder next to its role and size, with the army list's total by "Your army". It's worked out from the unit's stats:

> **Supply Cost = 3 × Firepower + ½ × Durability + 20 × active abilities + 10 × passive abilities + armor tax**, **rounded to the nearest multiple of 5** (halves up: 112.5 → 115)

- **Firepower:** the damage the unit can expect to do in a turn. Its action is either a **volley** or an **ability**, so it's the higher of:
  - a volley: every soldier's best weapon (counting weapons the army builder can give them), each worth **Damage × Shots × hit chance × penetration factor**, added up;
  - its best ability attack against one target: the ability weapon's Damage × Shots × penetration factor, × the user's hit chance for an aimed attack (area and unit attacks don't roll to hit).
  - **Hit chance:** the soldier's Accuracy (with standard equipment) + the weapon's accuracy bonus, capped at 100%. Weapons use the faction's stats (e.g. the Rask's +2 Damage). No other bonuses (crits, auras, cover).
  - **Penetration factor:** the weapon's average damage multiplier against Armor 0 to 4 under the penetration rules, so anti-armor weapons count for more than small arms that can't hurt armor:

    | Penetration | 0 | 1 | 2 | 3 | 4 | 5+ | X |
    |---|---|---|---|---|---|---|---|
    | Factor | 0.3 | 0.6 | 0.9 | 1.2 | 1.4 | 1.5 | 1.0 |
- **Durability:** every soldier's Health × (1 + Armor ÷ 4), with standard equipment (e.g. a Combat Shield), added up. It counts at **half weight**: what a unit can do counts for more than how long it lasts.
- **Abilities:** each different ability in the unit (its soldiers' own, and those their standard equipment and keywords give, e.g. a Transport's Embark and Disembark) counts once, however many soldiers have it. Equipment the army builder can swap in (e.g. a Medkit for a Frag Grenade) doesn't change the cost.
- **Armor tax**, once per unit, by its best-armored soldier: **+100** for **Armor 4 or higher** (Sagittarius MBT, Marshal Command Tank, Tyrant Hover-tank, Stinger Heavy Robot); **+50** for **Armor 3** (Olympian Mech, Intimidator Medium Robot, ISF Boarding Team, Workhorse Transport). It discourages tank spam without limiting how many tanks an army takes.
- The weights are constants in one place (`Rules.SupplyCost`), so they can be tuned.
- **Adjustments:** a unit can carry a fixed surcharge on top of the formula (`UnitTemplate.supplyCostAdjustment`) for value the formula doesn't capture. Only the Rask Reliquary has one: +100 for its aura.
- It's recalculated whenever the unit catalog is rebuilt (**Ground Invasion > Rebuild Unit Catalog**), so it follows any change to a unit.

**A faction can only field an army once it has a Commander unit.** While **no faction** has one, the army builder says so (*"No faction has a Commander yet, so a battle can't be started"*) instead of the faction choice, listing the factions greyed out with how far along each is (*"2 units · no Commander yet"*, *"no units yet"*), with a **Back** button, and the main menu notes it under the title (*"No Commanders yet"*). Starting a battle (or sending a multiplayer army) is refused with the same message. Once some faction has a Commander, the faction choice works as usual and each faction shows its units (*"3 units available"*, or *"· no Commander yet"* for one that can't field an army). Add Unit and Soldier template assets under Data, put each unit in its faction's unit list, then run **Ground Invasion > Rebuild Unit Catalog** so the army builder lists them.

**Weapons and equipment (customizing a unit):** a soldier type can offer **weapon swaps** (a weapon it carries, and what it can be swapped for, e.g. *Assault Rifle → SMG*) and **equipment** it can be given. A unit in your list whose soldiers have any options gets a **Customize** button, which opens a panel listing each of those soldier types with its weapon choices and equipment.
- Choices apply to **every soldier of that type in the unit** (e.g. all the Troopers); the Sergeant is set separately. Each unit in the list has its own choices, even two of the same unit.
- **A swapped weapon replaces the original**; if it was the primary weapon, the new one is the primary.
- **Equipment swaps:** a soldier type can also offer to swap a piece of its **standard equipment** for another (e.g. the ISF Riflemen's *Frag Grenade → Medkit or Repair Kit*). The new item replaces the old one, with everything it gives. Swaps are sent to the other player in multiplayer. A unit can make its equipment swaps **one choice for the whole unit** (every soldier type with that swap takes it together, e.g. the ISF Rifle Squad); otherwise each soldier type chooses for itself.
- **Equipment** can give **keywords**, **abilities**, **extra weapons** (carried as well, not as the primary) and **stat changes** (Health, Armor Level, Accuracy, Movement; added to the soldier's own). The list shows a short summary of a unit's choices.
- Choices are kept with your last army, and applied when your army deploys. The AI's armies don't customize.
- *So far only the ISF Heavy Weapons (Machine Gun → Rocket Launcher or Flamethrower), the ISF Ranger (Sniper Rifle → Anti-Materiel Rifle or Shotgun) and the ISF Rifleman and ISF Rifleman Sergeant (Frag Grenade → Medkit or Repair Kit; in the ISF Rifle Squad, and the Rifleman in the Mortar Team) have options; options are set on each soldier's data (Weapon Options, Equipment Options).*
- **Standard equipment:** a soldier type can also carry equipment **as standard** (Equipment, on the soldier's data), always applied, without any army builder choice. Its stat changes are included in the unit's stats in the army builder.
- **Equipment in the game** (Data/Equipment):
  - **Combat Shield:** adds **1** to the soldier's Armor Level. Carried as standard by the ISF Boarding Team.
  - **Frag Grenade:** gives the active ability **Frag Grenade** (a 2.5 m blast, thrown as the unit's action; no limit per battle). Carried twice as standard by Master Sergeant Jakoby.
  - **Medkit:** gives the active ability **Medkit**: choose **one friendly Biological soldier within 5 m** (the medic too) and they restore **5 health** (not above their maximum). Like any healing ability, it **can't be used on a soldier at full health**: the game plays the error sound and the unit keeps its action, so you can pick another target. Not a Spell, so it works after sprinting. Carried as standard by the Rask Redhand.
  - **Repair Kit:** gives the active ability **Repair Kit**: choose **one friendly Mechanical soldier within 5 m** (the user too, if Mechanical) and they restore **10 health** (not above their maximum). Like the Medkit, it **can't be used on a soldier at full health** (error sound; the unit keeps its action) and works after sprinting. Carried as standard by the Kobold Mechanist and the Rask Blackhand.
- **Duplicate equipment stacks:** each copy a soldier carries counts (stat changes add up). For an ability limited per battle, each copy (and the soldier type's own ability, if it has it) would add that many uses; no ability is limited per battle at the moment.

**The AI's army:** the AI picks a **random faction** (of those that have a Commander, so it can field an army) and builds its own **random valid army** from that faction's units each battle under the same rules, always filling all 6 slots: 1 Commander, then random units.

**Deployment:** each army starts in its zone, **one unit per slot in list order**, left to right as that side looks across the map (the AI's slots are the player's turned 180°). Units go in **rows of 6** across the zone; an army of more than 6 units fills further rows **7 m behind** each other (never off the battlefield, back ranks included). A unit whose slot is blocked (its formation would stand on a building, wall, ramp, water or object, or on soldiers already deployed) takes the **nearest clear spot in its zone** instead. The players can then move their units during the deployment phase. A unit taken more than once is numbered ("Rifle Squad 1", "Rifle Squad 2"). Restarting a battle replays the same armies.

### Mortar Team

*International Space Force of Humanity · Heavy Support.* 1 **ISF Mortar Specialist** (leader) + 1 **ISF Rifleman**.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapons | Ability |
|---|---|---|---|---|---|---|---|
| ISF Mortar Specialist | 5 | 2 | 75% | 10 m | Biological | Mortar Launcher (primary), Assault Rifle | — |
| ISF Rifleman | 5 | 2 | 75% | 10 m | Biological | Assault Rifle | Anti-infantry Mortar, **50 m** (equipment; *or* Medkit, *or* Repair Kit) |

- **Mortar Launcher:** Damage 5, Shots 1, Penetration 3, +0% accuracy, Range 50 m; **Deployed** (can't fire after moving that turn), **Manual Load** (fires once a turn), **Explosive**. It fires at one soldier, like a rifle (no blast).
- After the Specialist moves, the mortar can't fire, so they shoot with the **Assault Rifle** instead.
- The ISF Rifleman has the ISF Rifle Squad's Rifleman's stats, but the team's Frag Grenade, which the team calls the **Anti-infantry Mortar**, can be fired **up to 50 m** (the Rifle Squad's Frag Grenade: 10 m); otherwise the same grenade (2.5 m blast, 5 damage, penetration 0). It can be swapped for a Medkit or a Repair Kit in the same way.
- Heavy Support, so an army can take more than one.

### Fist of Mars

*International Space Force of Humanity · Specialist.* One soldier: a Sagittarius MBT crewed by tank aces, with a red **Symbol of Mars** (a ring with an arrow pointing up and out of it) painted on the left and right sides of its turret. The Sagittarius MBT's stats, but **Accuracy 125%** and the **Unique** keyword.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Ability |
|---|---|---|---|---|---|---|---|
| Fist of Mars | 20 | 4 | **125%** | 30 m (can't sprint) | Mechanical, Tank, Unstable, Large, **Unique** | 100mm Cannon | Frag Grenade (every turn, 50 m) |

- **Accuracy 125%:** its shots always hit (before any penalties) and have a **25% chance to crit** (a crit adds 100% of the damage after penetration).
- **Unique:** only one Fist of Mars per army.
- Otherwise exactly the Sagittarius: 100mm Cannon (Damage 10, Penetration 4, 50 m, Manual Load), the 50 m Frag Grenade, Mechanical, Unstable, Large, radius 1.8 m.
- A Specialist.

### Sagittarius MBT

*International Space Force of Humanity · Heavy Support.* The original Eldoria's Medium Tank, renamed. One vehicle.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Ability |
|---|---|---|---|---|---|---|---|
| Sagittarius MBT | 20 | 4 | 75% | 30 m (can't sprint) | Mechanical, Tank, Unstable, Large | 100mm Cannon | Frag Grenade (every turn, 50 m) |

- **100mm Cannon:** Damage 10, Shots 1, Penetration 4, Range 50 m, **Manual Load** (fires once a turn).
- **Mechanical:** can't sprint and is never in close range. **Unstable:** when destroyed, a 10% chance to explode (5 damage to every soldier within 5 m).
- **Frag Grenade:** the Rifle Squad's grenade (2.5 m blast, 5 damage, penetration 0), but thrown **up to 50 m** (the Rifle Squad's: 10 m); once a turn, no limit per battle.
- **Large:** friendly Biological soldiers within 5 m of it can shelter behind it (half-cover; see Keywords).
- **Wide body:** it stands on a circle of 1.8 m radius, so its route needs room for the hull the whole way.
- Heavy Support, so an army can take more than one.

### Olympian Mech

*International Space Force of Humanity · Heavy Support.* The original Eldoria's Stahl Colossus, renamed. One walking war machine, about twice a soldier's height.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapons | Abilities |
|---|---|---|---|---|---|---|---|
| Olympian Mech | 15 | 3 | 75% | 15 m (can't sprint) | Solo, Mechanical | Machine Gun (primary), Iron Claw | Iron Claw, Stabilizers |

- **Machine Gun:** Damage 5, Shots 5, Penetration 1, Range 20 m, Deployed, but **Stabilizers** (passive) nullify Deployed, so it can fire after moving.
- **Iron Claw** (active): an aimed strike at one enemy within 2 m: Damage 10, Penetration 0, +25% accuracy (Melee, so it works in close range). Using it is the unit's action.
- **Wide body:** it stands on a circle of 0.7 m radius.
- Heavy Support, so an army can take more than one.

### ISF Boarding Team

*International Space Force of Humanity · Specialist.* 1 **Boarding Sergeant** (leader) + 4 **Boarding Specialists**: ISF troopers with a shotgun and a tall shield on the arm. The ISF Rifleman's stats, with a **Combat Shield** carried as standard.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Equipment |
|---|---|---|---|---|---|---|---|
| Boarding Sergeant | 5 | 2 (**3** with the shield) | 75% | 10 m (sprint 20 m) | Biological | Shotgun | Combat Shield |
| Boarding Specialist (×4) | 5 | 2 (**3** with the shield) | 75% | 10 m (sprint 20 m) | Biological | Shotgun | Combat Shield |

- **Shotgun:** Damage 10, Shots 1, Penetration 1, +0% accuracy, Range 10 m.
- **Combat Shield:** +1 Armor Level, so they fight at **Armor 3**.
- They have the ISF Rifleman's stats, not its Frag Grenade.
- A Specialist (an army can take more than one).

### ISF Ranger

*International Space Force of Humanity · Specialist.* The original Eldoria's Sniper, renamed, with Armor 1. One soldier.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon |
|---|---|---|---|---|---|---|
| ISF Ranger | 5 | 1 | 100% | 15 m | Biological, Sniper Precision, Solo | Sniper Rifle, *or* Anti-Materiel Rifle, *or* Shotgun |

- **Chromashift Textiles** (passive ability): **attacks against the Ranger take −25% accuracy.** It gives the Ranger the **Chromashift** status (a keyword whose effect, when defending, is −25% to the attacker's accuracy), the same way cover works; it adds to any cover penalty, and Indirect fire ignores it like every accuracy modifier. Only the Ranger itself has it, not soldiers near it.

- **Sniper Rifle:** **Damage 10** (raised from the original Eldoria's 5), Shots 1, Penetration 1, **+50% accuracy**, Range 50 m, no keywords (it can fire after moving).
- **Weapon options:** in the army builder (**Customize**), the ISF Ranger can swap the Sniper Rifle for either:
  - an **Anti-Materiel Rifle**: Damage 10, Shots 1, Penetration X (full damage whatever the armor), +0% accuracy, Range 50 m, **Deployed** (can't fire after moving that turn). It trades the Sniper Rifle's +50% aim for damage that armor can't stop;
  - a **Shotgun**: Damage 10, Shots 1, Penetration 1, +0% accuracy, Range 10 m, no keywords (so it can fire after moving, but not in close range).
- **Sniper Precision:** when the Ranger shoots, the player picks the exact enemy soldier to target (the AI prefers Solo soldiers and sergeants).
- A Specialist and a Solo unit (an army can take more than one).

### ISF Combat Engineer

*International Space Force of Humanity · Specialist.* One soldier who keeps the army's machines fighting.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Equipment |
|---|---|---|---|---|---|---|---|
| ISF Combat Engineer | 5 | 2 | 75% | 10 m (sprint 20 m) | Biological, Solo | Assault Rifle | Repair Kit |

- The ISF Rifleman's stats, with a **Repair Kit** (standard equipment) instead of a Frag Grenade: as its action, once a turn, a friendly **Mechanical** soldier within 5 m restores **10 health** (refused, with no action spent, on one at full health).
- A Specialist and a Solo unit (an army can take more than one).

### ISF Combat Medic

*International Space Force of Humanity · Specialist.* One soldier who keeps the infantry on its feet.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Equipment |
|---|---|---|---|---|---|---|---|
| ISF Combat Medic | 5 | 2 | 75% | 10 m (sprint 20 m) | Biological, Solo | Assault Rifle | Medkit |

- The ISF Rifleman's stats, with a **Medkit** (standard equipment) instead of a Frag Grenade: as its action, once a turn, a friendly **Biological** soldier within 5 m (the medic too) restores **5 health** (refused, with no action spent, on one at full health).
- A Specialist and a Solo unit (an army can take more than one).

### Dragon Hierarch

*Imperial Army of The Draconis Imperialis · Commander.* 1 **Dragon Hierarch** (leader): a large quadrupedal dragon with wings and golden armor, of the Dragons, the Empire's ruling caste (see [Lore.md](Lore.md)); escorted by 2 **Dragonborn Honorguard** in gold armor and crimson capes.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon |
|---|---|---|---|---|---|---|
| Dragon Hierarch | 15 | 2 | 50% | 30 m (sprint 60 m) | Biological, Large | Dragon Claw |
| Dragonborn Honorguard (×2) | 10 | 2 | **95%** | 10 m (sprint 20 m) | Biological | Dragon Glaive (Range), Dragon Glaive (melee) |

- **Dragon Claw:** Damage 5, Shots 1, Penetration 1, Accuracy Bonus 0, Range **2 m**; **Melee** (usable in close range) and **Natural** (can't be disabled by Disarm).
- **Footprint:** the model is about 6 m long and 3.6 m tall, but the dragon stands on a circle of **1 m radius** (it needs room for that along its route, like other wide soldiers). Reach is measured between soldiers' centres, so with a 2 m claw it can hit a soldier standing right next to it (1.35 m apart for a person); a wider footprint couldn't reach anyone.
- Against its Armor 2, Assault Rifles and the Machine Gun (Penetration 1) do half damage, Pistols and SMGs (Penetration 0) do none, and the Rocket Launcher (Penetration 5) does 150%; the Flamethrower (no penetration value) does full damage.
- **Dragonborn Honorguard:** the Dragonborn Footman's stats and Dragon Glaives (see the Dragonborn Squad), but **95% accuracy**.
- The dragon leads the unit's move; the Honorguard then follow it, ending within 5 m of it (cohesion), however far it went.
- The Draconis Imperialis's Commander, so every Draconis army has one.

### Dragonborn Exemplar

*Imperial Army of The Draconis Imperialis · Commander.* One soldier: a Dragonborn in gold armor and a crimson cape, with a tall gold crest, holding a ceremonial blade, a pistol at the hip. The Dragonborn Footman's stats with **double the health** and the **Solo** and **Officer** keywords.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapons | Ability |
|---|---|---|---|---|---|---|---|
| Dragonborn Exemplar | 20 | 2 | 75% | 10 m (sprint 20 m) | Biological, Solo, Officer | Raptor Pistol (primary), Ceremonial Blade | Empowering Presence (passive) |

- **Raptor Pistol:** the Twin Raptor Pistols' stats with **1 shot**: Damage 4, Shots 1, Penetration 1, +0% accuracy, Range 20 m; Pistol.
- **Ceremonial Blade:** the Draconis name for the **Broadsword**: Damage 5, Shots 1, Penetration 1, Range 2 m; Melee.
- **In close range** it fights with the blade: a soldier with both a Melee weapon and a pistol uses the Melee weapon when an enemy is within 2 m.
- **Empowering Presence** (passive aura): **other friendly soldiers within 15 m** (the range of Lead from the front) gain the status **Empowered**: **+2 damage** with their weapons. Like Lead from the front, the Exemplar itself doesn't get it, and each soldier is checked on its own.
- The Draconis Imperialis's second Commander: an army takes it **or** the Dragon Hierarch.

### Dragonborn Squad

*Imperial Army of The Draconis Imperialis · Infantry.* 1 **Dragonborn Sergeant** (leader) + 4 **Dragonborn Footmen**: anthropomorphic dragons with wings and a tail, standing slightly taller than a human (2.2 m), each holding a glaive with a rifle's receiver and barrel built into its head (see [Lore.md](Lore.md), the Dragonborn).

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapons |
|---|---|---|---|---|---|---|
| Dragonborn Sergeant | 10 | 2 | 75% | 10 m | Biological | Dragon Glaive (Range), Dragon Glaive (melee) |
| Dragonborn Footman (×4) | 10 | 2 | 75% | 10 m | Biological | Dragon Glaive (Range), Dragon Glaive (melee) |

| Weapon | Damage | Shots | Penetration | Accuracy bonus | Range | Keywords |
|---|---|---|---|---|---|---|
| Dragon Glaive (Range) | 5 | 1 | 1 | +0% | 20 m | — |
| Dragon Glaive (melee) | 5 | 1 | 2 | +25% | 2 m | Melee |

- The ranged glaive is their **primary weapon**. In **close range** (a Biological enemy under 2 m away) they can't use it, so they switch to the **melee glaive** (Melee weapons work in close range).
- Infantry, so an army can take more than one.

### Rask Ultima

*Most Holy Army of The Rask Divine Collective · Commander.* One soldier: a Rask officer with a gold crest and gorget, gold epaulettes and a purple cape, carrying a pistol. The Rask Rifleman's stats, but **Health 10** and **Armor 2**.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon |
|---|---|---|---|---|---|---|
| Rask Ultima | 10 | 2 | 75% | 20 m (sprint 40 m) | Biological, Solo, Officer | Pistol |

- **Pistol:** Damage 2, Shots 1, Penetration 0, Range 10 m; Pistol (usable in close range).
- The Rask's Commander, so every Rask army has one, and the Rask can now field armies.

### Rask Blackhand

*Most Holy Army of The Rask Divine Collective · Specialist.* One soldier: a Rask combat engineer with soot-black gauntlets, goggles, a tool pack and a wrench. The Rask Ultima's stats, but **Armor 1** and **no Officer** keyword.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Equipment |
|---|---|---|---|---|---|---|---|
| Rask Blackhand | 10 | 1 | 75% | 20 m (sprint 40 m) | Biological, Solo | Solntse Laser Rifle | Repair Kit |

- **Solntse Laser Rifle:** the Rask's Assault Rifle, Damage 6 for them: Damage 6, Shots 1, Penetration 1, Range 20 m.
- **Repair Kit** (standard equipment): its action can restore **10 health** to a friendly **Mechanical** soldier within 5 m (e.g. the Stinger Heavy Robot or the Intimidator Medium Robot). Refused, with no action spent, on a soldier at full health.
- A Specialist (an army can take more than one).

### Rask Redhand

*Most Holy Army of The Rask Divine Collective · Specialist.* One soldier: a Rask field medic with red gauntlets and a white medic's pack marked with a red cross. The Rask Ultima's stats, but **Armor 1** and **no Officer** keyword.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Equipment |
|---|---|---|---|---|---|---|---|
| Rask Redhand | 10 | 1 | 75% | 20 m (sprint 40 m) | Biological, Solo | Solntse Laser Rifle | Medkit |

- **Solntse Laser Rifle:** Damage 6, Shots 1, Penetration 1, Range 20 m.
- **Medkit** (standard equipment): its action can restore **5 health** to a friendly **Biological** soldier within 5 m (itself included). Refused, with no action spent, on a soldier at full health.
- A Specialist (an army can take more than one).

### Rask Reliquary

*Most Holy Army of The Rask Divine Collective · Specialist.* One soldier: a shrine on tracks. A low tracked chassis carries a stone altar under a gilded canopy on four gold pillars, with a glowing relic on the altar, candles at the front and banners at the back.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Ability |
|---|---|---|---|---|---|---|---|
| Rask Reliquary | 10 | 2 | 75% | 20 m (can't sprint) | Mechanical, Large | — | Sanctifying Presence (passive) |

- **No weapon:** it can't shoot; its action is to move.
- **Sanctifying Presence** (passive aura): **other friendly Biological soldiers within 20 m** gain the status **Sanctified**: **+2 damage** and **+20% accuracy** with their weapons. Mechanical soldiers (vehicles and robots) and the Reliquary itself don't get it, and each soldier is checked on its own.
- **Supply Cost 120:** the formula gives 20 (it doesn't count what an aura is worth), plus a **+100 surcharge** set on this unit.
- **Mechanical:** can't sprint and is never in close range. Radius 1.4 m.
- A Specialist (an army can take more than one).

### Rask Rifle Team

*Most Holy Army of The Rask Divine Collective · Infantry.* 1 **Rask Superior** (leader) + 19 **Rask Riflemen**: anthropomorphic rats standing at two-thirds the height of a human (1.2 m), holding rifles and wearing armor (see [Lore.md](Lore.md), the Rask). Same stats as the Kobold Militiaman, but Armor 1 and 75% accuracy.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon |
|---|---|---|---|---|---|---|
| Rask Superior | 5 | 1 | 75% | 20 m (sprint 40 m) | Biological, Swarm | Solntse Laser Rifle |
| Rask Rifleman (×19) | 5 | 1 | 75% | 20 m (sprint 40 m) | Biological, Swarm | Solntse Laser Rifle |

- **Solntse Laser Rifle:** the Rask's name for the **Assault Rifle**, with **Damage +2** for Rask soldiers: Damage 6, Shots 1, Penetration 1, Range 20 m.
- **Swarm:** the unit keeps cohesion out to **10 m** from its leader.
- **Armor 1** (raised from the Kobold Militiaman's 0).
- Led into battle by the Rask Ultima, the Rask's Commander.

### Intimidator Medium Robot

*Most Holy Army of The Rask Divine Collective · Heavy Support.* One soldier: a gorilla-like robot that walks on all fours (on its knuckles), with a machine gun mounted on each shoulder.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Abilities |
|---|---|---|---|---|---|---|---|
| Intimidator Medium Robot | 10 | 3 | 75% | 30 m (can't sprint) | Mechanical, Robotic, Large | Twin Intimidator Machine Guns | Stabilizers (passive) |

- **Twin Intimidator Machine Guns:** the Machine Gun's stats but **10 shots**: Damage 5, Shots 10, Penetration 1, +0% accuracy, Range 20 m; **Deployed**.
- **Stabilizers** (passive) nullify Deployed, so it can fire after moving.
- **Mechanical:** can't sprint and is never in close range. Radius 1 m.

### Stinger Heavy Robot

*Most Holy Army of The Rask Divine Collective · Heavy Support.* One soldier: a six-legged, beetle-like war robot with a large cannon on its back angled up at about 45 degrees.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Abilities |
|---|---|---|---|---|---|---|---|
| Stinger Heavy Robot | 25 | 5 | 75% | 30 m (can't sprint) | Mechanical, Heavy, Robotic, Large | — | Plasma Mortar (every turn, 50 m), Stabilizers (passive) |

- **Plasma Mortar** (active, once a turn, no limit per battle): an **area attack** like the Frag Grenade. It shells a spot **within 50 m**: everyone within **2.5 m** is hit with the Plasma Mortar's stats (Damage 5, Penetration 3: the Mortar Launcher's), unless terrain shields them. No line of sight is needed. It has no aimed weapon.
- **Stabilizers** (passive) nullify Deployed, so it can fire the Plasma Mortar after moving.
- **Large:** friendly Biological soldiers within 5 m of it can shelter behind it (half-cover).
- **Mechanical:** can't sprint and is never in close range. **Heavy** is a new trait with no effect of its own yet.
- Its footprint (radius 1.6 m) is smaller than the spread of its legs.

### Dragonborn Skymaster Squad

*Imperial Army of The Draconis Imperialis · Specialist.* 1 **Dragonborn Skylord** (leader) + 4 **Dragonborn Skymasters**: Dragonborn with jetpacks in place of their wings, each holding a pistol in either hand. The Dragonborn Footman's stats, but **Move 40 m** and **Jump Pack**.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon |
|---|---|---|---|---|---|---|
| Dragonborn Skylord | 10 | 2 | 75% | 40 m (sprint 80 m) | Biological, Jump Pack | Twin Raptor Pistols |
| Dragonborn Skymaster (×4) | 10 | 2 | 75% | 40 m (sprint 80 m) | Biological, Jump Pack | Twin Raptor Pistols |

- **Twin Raptor Pistols:** Damage 4, **Shots 2**, Penetration 1, +0% accuracy, Range 20 m; **Pistol** (usable in close range).
- **Jump Pack:** climbing onto an object costs them **no movement** (normally 10 m).
- A Specialist (an army can take more than one).

### Tyrant Hover-tank

*Imperial Army of The Draconis Imperialis · Heavy Support.* One soldier: a dagger-shaped tank hovering just off the ground, with a railgun turret on top and a cockpit at the front flown by a Kobold pilot.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon |
|---|---|---|---|---|---|---|
| Tyrant Hover-tank | 15 | 4 | 100% | 30 m (can't sprint) | Mechanical, Unstable, Hover, Large | Tyrant Railgun |

- **Tyrant Railgun:** Damage 5, Shots 1, **Penetration 7**, +0% accuracy, Range 50 m; **Manual Load** (fires once a turn). Penetration 7 is higher than any Armor Level in the game, so it always does 150% damage (7).
- **Hover:** terrain can't reduce its movement: a move costs only its flat distance (no 10 m climbing cost, no wading penalty, and going up and over a hill costs no more than crossing flat ground). It still has to find a route around anything that blocks movement.
- **Mechanical:** can't sprint and is never in close range. **Unstable:** when destroyed, a 10% chance to explode (5 damage to every soldier within 5 m).
- A wide body (radius 1.8 m, like the Sagittarius MBT).

### Kobold Mechanist

*Imperial Army of The Draconis Imperialis · Specialist.* One soldier: a Kobold engineer in goggles with a tool pack and a wrench. The Kobold Militiaman's stats, but **Armor 1** and the **Solo** keyword.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Equipment |
|---|---|---|---|---|---|---|---|
| Kobold Mechanist | 5 | 1 | 50% | 20 m (sprint 40 m) | Biological, Swarm, Solo | Fang Caster | Repair Kit |

- **Fang Caster:** the Draconis name for the Assault Rifle: Damage 4, Shots 1, Penetration 1, Range 20 m.
- **Repair Kit** (standard equipment): its action can restore **10 health** to a friendly **Mechanical** soldier within 5 m (e.g. the Tyrant Hover-tank). Refused, with no action spent, on a soldier at full health.
- Swarm comes with the Militiaman's stats; on a unit of one it has no effect.
- A Specialist (an army can take more than one).

### Kobold Heartstrike Spear Team

*Imperial Army of The Draconis Imperialis · Specialist.* 1 **Kobold Sergeant** (leader) + 4 **Kobold Riflemen**: kobolds armed with anti-tank mines grafted onto rods to form spears. *An anti-tank squad of kobolds conditioned for suicidal bravery. While this team can clear almost any tank in the galaxy their frail nature makes them vulnerable to any infantry coordinating with armor.*

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon |
|---|---|---|---|---|---|---|
| Kobold Sergeant | 5 | 0 | 50% | 20 m (sprint 40 m) | Biological, Swarm | Heartstrike Spear |
| Kobold Rifleman (×4) | 5 | 0 | 50% | 20 m (sprint 40 m) | Biological, Swarm | Heartstrike Spear |

- **Heartstrike Spear:** Damage 5, Shots 1, **Penetration 6**, +0% accuracy, Range 30 m, no keywords. Penetration 6 is higher than any armor so far, so it does **150% damage (7)** to every tank, the Stinger's Armor 5 included.
- The Kobold Militiaman's stats: no armor and 5 health each, so rifles cut them down; Swarm lets them spread up to 10 m from the Sergeant.
- A Specialist (an army can take more than one).

### Kobold Militia

*Imperial Army of The Draconis Imperialis · Infantry.* 1 **Kobold Sergeant** (leader) + 19 **Kobold Militiamen**: wingless anthropomorphic dragons standing at two-thirds the height of a human (1.2 m), their scales in the faction's colour (like the Dragons' and Dragonborn's), in drab khaki fabric uniforms and holding rifles (see [Lore.md](Lore.md), the Kobolds).

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon |
|---|---|---|---|---|---|---|
| Kobold Sergeant | 5 | 0 | 50% | 20 m (sprint 40 m) | Biological, Swarm | Fang Caster |
| Kobold Militiaman (×19) | 5 | 0 | 50% | 20 m (sprint 40 m) | Biological, Swarm | Fang Caster |

- **Fang Caster:** the Draconis Imperialis's name for the **Assault Rifle** (same weapon and stats: Damage 4, Shots 1, Penetration 1, Range 20 m).
- **Swarm:** the unit keeps cohesion out to **10 m** from its leader (normally 5 m).
- Infantry, so an army can take more than one.

### Field Commander

*International Space Force of Humanity · Commander.* The original Eldoria's Field Commander, with Armor 2. One soldier.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapons | Ability |
|---|---|---|---|---|---|---|---|
| Field Commander | 10 | 2 | 75% | 10 m | Biological, Officer, Solo | Pistol | Lead from the front |

- **Lead from the front** (passive aura): other friendly soldiers within 15 m gain **Led from the front**, +15% accuracy when attacking. The Field Commander doesn't benefit.
- The ISF's only Commander so far, and what lets the ISF field an army.

### Master Sergeant Jakoby

*International Space Force of Humanity · Commander.* One soldier: a veteran ISF Master Sergeant in a beret, with a shotgun and two frag grenades on his belt. The Field Commander's stats, but **without Lead from the front**.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Equipment | Abilities |
|---|---|---|---|---|---|---|---|---|
| Master Sergeant Jakoby | 10 | 2 | 75% | 10 m (sprint 20 m) | Biological, Officer, Solo, Unique | Shotgun | Frag Grenade ×2 | Frag Grenade |

- **Shotgun:** Damage 10, Shots 1, Penetration 1, +0% accuracy, Range 10 m.
- **Two Frag Grenades:** they give him the Frag Grenade ability, which has no limit per battle (once a turn, as his action).
- One of the ISF's three Commanders: an ISF army takes him, the Field Commander or the Marshal Command Tank.

### Marshal Command Tank

*International Space Force of Humanity · Commander.* One soldier: a Sagittarius MBT refitted as a mobile headquarters, with two antennae and a satellite dish on the turret. The Sagittarius MBT's stats plus the **Officer** keyword.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Abilities |
|---|---|---|---|---|---|---|---|
| Marshal Command Tank | 20 | 4 | 75% | 30 m (can't sprint) | Mechanical, Tank, Unstable, Officer, Large | 100mm Cannon | Frag Grenade (every turn, 50 m) |

- **100mm Cannon:** Damage 10, Shots 1, Penetration 4, Range 50 m; Manual Load.
- Like the Sagittarius: **Mechanical** (can't sprint, never in close range), **Unstable** (a 10% chance to explode when destroyed), **Large** (shelters friendly Biological soldiers within 5 m), radius 1.8 m, and a **Frag Grenade thrown up to 50 m** every turn.
- The ISF's third Commander: an ISF army takes one of the Field Commander, Master Sergeant Jakoby or the Marshal Command Tank.

### Workhorse Transport

*International Space Force of Humanity · Transport.* One soldier: a six-wheeled armored truck with a gunmetal cab, a heavy grille and clustered headlights, an armored cargo body with side and roof panels in the faction's colour, and a machine gun on the cab roof.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapon | Abilities |
|---|---|---|---|---|---|---|---|
| Workhorse Transport | 10 | 3 | 75% | 40 m (can't sprint) | Transport, Mechanical | Machine Gun | Embark, Disembark (from Transport), Stabilizers (passive) |

- **Transport:** carries **one unit of Biological soldiers**, however many (see Transports): **Embark** takes a friendly unit within 2.5 m of the truck aboard; **Disembark** puts it out at once to make its move. A unit aboard is lost if the truck is destroyed.
- **Machine Gun:** Damage 5, Shots 5, Penetration 1, Range 20 m; Deployed, but **Stabilizers** nullify Deployed, so it fires after moving.
- **Mechanical:** can't sprint and is never in close range. Radius 1.6 m.

### ISF Rifle Squad

*International Space Force of Humanity · Infantry.* 1 **ISF Rifleman Sergeant** (leader) + 9 **ISF Riflemen**.

| Soldier | Health | Armor | Accuracy | Move | Keywords | Weapons | Equipment |
|---|---|---|---|---|---|---|---|
| ISF Rifleman Sergeant | 5 | 2 | 75% | 10 m | Biological | Assault Rifle, Pistol | Frag Grenade (*or* Medkit, *or* Repair Kit) |
| ISF Rifleman | 5 | 2 | 75% | 10 m | Biological | Assault Rifle | Frag Grenade (*or* Medkit, *or* Repair Kit) |

- **Frag Grenade** (active, **no limit per battle**; once a turn, as the unit's action): pick a spot on the ground within 10 m of the unit leader (no line of sight needed). Every soldier within 2.5 m of it, friend or foe, takes 5 damage, penetration 0 (armor counts). Object and structure terrain between the blast and a soldier shields them. Throwing it is the unit's action.
- **Equipment option:** each soldier carries the Frag Grenade as standard **equipment**. In the army builder (**Customize**), the **whole squad** can swap its grenades, as **one choice for every soldier** (Sergeant included), for **Medkits** (heal a friendly Biological soldier within 5 m for 5) or **Repair Kits** (repair a friendly Mechanical soldier within 5 m for 10).
- Built from the original Eldoria's Trooper and Sergeant, with Armor 2 (up from 1). Placeholder models: the old Trooper and Sergeant.

### Heavy Weapons Team

*International Space Force of Humanity · Heavy Support.* Based on the original Eldoria's Machine Gun Team, with ISF names, Armor 2 and Assault Rifles in place of SMGs. 1 **ISF Rifleman Sergeant** (leader) + 3 **ISF Riflemen** + 1 **ISF Heavy Weapons**. All are **Biological**; no Frag Grenade.

| Soldier | Health | Armor | Accuracy | Move | Weapons |
|---|---|---|---|---|---|
| ISF Rifleman Sergeant | 5 | 2 | 75% | 10 m | Pistol |
| ISF Rifleman (×3) | 5 | 2 | 75% | 10 m | Assault Rifle (primary), Pistol |
| ISF Heavy Weapons | 5 | 2 | 75% | 10 m | Machine Gun, *or* Rocket Launcher, *or* Flamethrower |

- **Machine Gun:** Damage 5, Shots 5, Penetration 1, Range 20 m, **Deployed** (can't fire after moving that turn).
- **Weapon options:** in the army builder (**Customize**), the ISF Heavy Weapons can swap the Machine Gun for either:
  - a **Rocket Launcher**: Damage 5, Shots 1, Penetration 5, Range 50 m, **Manual Load** (fires once a turn);
  - a **Flamethrower**: Damage 10, Shots 3, no penetration value (always full damage), Range 10 m, **Incendiary** (only harms Biological targets) and **Unstable** (when its carrier dies: 10% chance to explode, 5 damage to every soldier within 5 m).

  The chosen weapon replaces the Machine Gun as the soldier's primary weapon. Each Heavy Weapons Team in an army has its own choice.
- **Assault Rifle:** Damage 4, Shots 1, Penetration 1, Range 20 m.
- Heavy Support, so an army can take more than one.

### Starting kit

Each unit is a Unit template (Data/Units) made of Soldier templates (Data/Soldiers). Kept from the original Eldoria:

- **Weapons** (Data/Weapons): Assault Rifle, Pistol, SMG, Machine Gun, Flamethrower, Sniper Rifle, 100mm Cannon, Rocket Launcher, Broadsword, Iron Claw.
- **Mortar Launcher** (carried by the ISF Mortar Team): the Frag Grenade weapon's stats with **Penetration 3**, **Range 50 m** and the keywords **Deployed** (can't fire after moving that turn), **Manual Load** (fires once a turn) and **Explosive** (like the grenade): Damage 5, Shots 1, +0% accuracy. It's a weapon fired at a soldier like any other: it doesn't have the Frag Grenade ability's 2.5 m blast.
- **Plasma Mortar** (the Rask's Stinger Heavy Robot's blast; see its entry): the Mortar Launcher's stats: Damage 5, Shots 1, Penetration 3, +0% accuracy, Range 50 m; Deployed, Manual Load, Explosive.
- **Twin Intimidator Machine Guns** (on the Rask's Intimidator Medium Robot): the Machine Gun's stats with 10 shots: Damage 5, Shots 10, Penetration 1, +0% accuracy, Range 20 m; Deployed.
- **Raptor Pistol** (the Dragonborn Exemplar's): the Twin Raptor Pistols' stats with 1 shot: Damage 4, Shots 1, Penetration 1, +0% accuracy, Range 20 m; Pistol.
- **Tyrant Railgun** (on the Tyrant Hover-tank): Damage 5, Shots 1, Penetration 7, +0% accuracy, Range 50 m; Manual Load.
- **Twin Raptor Pistols** (carried by the Dragonborn Skymaster Squad): Damage 4, Shots 2, Penetration 1, +0% accuracy, Range 20 m; Pistol.
- **Heartstrike Spear** (the Kobold Heartstrike Spear Team's): an anti-tank mine on a rod: Damage 5, Shots 1, Penetration 6, +0% accuracy, Range 30 m, no keywords.
- **Shotgun** (an option for the ISF Ranger): Damage 10, Shots 1, Penetration 1, +0% accuracy, Range 10 m, no keywords.
- **Anti-Materiel Rifle** (an option for the ISF Ranger): the Sniper Rifle's stats but **Damage 10**, **no penetration value** and **no accuracy bonus**: Damage 10, Shots 1, Penetration X (full damage whatever the armor), +0% accuracy, Range 50 m, **Deployed**.
- **Abilities** (Data/Abilities): Sanctifying Presence (aura: +2 damage and +20% accuracy to Biological soldiers within 20 m), Empowering Presence (aura: +2 damage), Medkit (heals a friendly Biological soldier within 5 m for 5), Repair Kit (repairs a friendly Mechanical soldier within 5 m for 10), Frag Grenade (no limit per battle), a tank's Frag Grenade (the same, kept as its own ability) and the Mortar Team's Frag Grenade (the same, but 50 m), Lead from the front (aura), Divine Healing and Divine Sanctuary (spells), Fireball and Firestorm (spells), Rocket Launcher (aimed attack), Iron Claw (aimed strike), Stabilizers (passive).
- **Placeholder models** (Prefabs/Units) for soldiers, officers, powered armor, a tank and a walker.

### Keywords — *implemented*

Keywords are properties on weapons and soldiers that **interact with each other** in unique ways. Each keyword is an asset (Data/Keywords) listing its interactions. An interaction says:

> **If** the *attacker / weapon / target* **has / lacks** keyword *X* → *effect*

Effects: **no effect** (the attack does nothing — no damage, no bonus effects), **damage bonus/penalty**, **accuracy bonus/penalty**, **always hits**, **always misses**, **can't be targeted**, **ignores line of sight**, **ignores accuracy modifiers**.

- Weapon keywords apply whenever the weapon attacks. A soldier's keyword interactions are marked as applying **when attacking** or **when defending**.
- Example: every living soldier has **Biological**. **Toxic** (weapon): *if the target lacks Biological → no effect*. So a Toxic weapon harms soldiers but does nothing to a tank.
- **Explosive** (weapon) and **Robotic** (soldier) are **trait keywords**: they do nothing by themselves and exist for other rules and keywords to check (e.g. Toxic and Incendiary check for Biological the same way).

#### Keyword categories

- **Swarm** (soldier, Trait): **expands the unit's cohesion radius to 10 m** (normally 5 m). If the unit's leader has it, the rest of the unit may end a move up to 10 m from the leader; the green cohesion ring and the gather order use 10 m. The Kobold Militia and the Rask Rifle Team have it.
- **Unique** (soldier, Trait): **only one of this unit may appear in an army list** (see Army lists). The Fist of Mars and Master Sergeant Jakoby have it.
- **Large** (soldier, Trait): a very large creature or machine: the Dragon Hierarch, the Sagittarius MBT, the Marshal Command Tank, the Tyrant Hover-tank, the Stinger Heavy Robot, the Intimidator Medium Robot and the Rask Reliquary. **It gives half-cover to friendly Biological soldiers hiding behind it:** a friendly Biological soldier within **5 m** of its edge is **In half-cover**, exactly as near a Half-cover object: attacks against them take **−25% accuracy** when the Large soldier is between them and the attacker (the line of fire passes through its footprint, seen from above). Enemies, and machines, don't shelter behind it.
- **Jump Pack** (soldier, Trait): **able to climb up objects without movement cost**: climbing onto an object costs the soldier 0 m instead of 10 m. The Dragonborn Skymaster Squad has it.
- **Hover** (soldier, Trait): **terrain cannot reduce the movement of this soldier**: a move costs only its flat (horizontal) distance, with no climbing cost, no wading penalty and nothing extra for slopes. The Tyrant Hover-tank has it.
- **Transport** (soldier, Trait): a transport vehicle: gives the abilities **Embark** and **Disembark** (see Transports). It carries one unit of Biological soldiers, however many. The Workhorse Transport has it.
- **Heavy** (soldier, Trait): a heavy war machine, e.g. the Stinger Heavy Robot. No effect on its own yet; other rules can check for it.
- **Natural** (weapon, Trait): a natural weapon such as claws or teeth, which **can't be disabled by Disarm**. *Disarm isn't in the game yet, so for now it has no effect.*

Keywords are marked **Trait** (what a soldier or ability is: Biological, Robotic, Officer, Solo, Sniper Precision, Spell, Large, Heavy, Jump Pack, Hover, Transport, Natural, Swarm) or **Status** (a temporary state: Moved, In half-cover, In full cover, Led from the front, Chromashift).

- Some traits **do nothing on their own**: **Officer** and **Solo** only matter because other weapons and abilities check for them (e.g. something that only affects Officers, or has no effect on Solo soldiers) — done with keyword interactions such as *"if the target has / lacks Officer → …"*.

#### Abilities — *implemented (passive auras)*

- Soldier templates can have **abilities**. A **passive** ability is always in effect.
- A passive **aura** grants a keyword to soldiers within a radius of the soldier who has it (measured horizontally), and that keyword carries the effect. It can affect allies, enemies or everyone, and can include the soldier themself. It's updated whenever anyone moves, a move is undone, and at the start of each turn, and ends when the soldier dies.
- Example: **Lead from the front** → other friendly soldiers within 15 m gain **Led from the front** (+15% accuracy when attacking).

#### Active abilities — *implemented*

- **Abilities can target their user** (or the user's own unit) **unless they say otherwise** — this applies to passive and active abilities. (Lead from the front says otherwise.)
- See **Unit actions** below for when an active ability can be used.
- The target must be the right side (e.g. friendly), within range (horizontal; for a unit, to its closest soldier) and have any required keyword (e.g. Biological).
- Effects so far: **restore health** (up to starting health) and **give a keyword** (to the soldier, or every soldier of the unit), optionally until the next turn; an **area attack** at a spot (Frag Grenade); an **aimed attack** on one soldier (Rocket Launcher, Fireball); and a **unit attack** on every soldier of a unit (Firestorm).
- **Passive abilities can nullify a keyword** for the holder's weapons (e.g. **Stabilizers** nullify **Deployed**).
- **Unit attack** (e.g. Firestorm): the user picks an **enemy unit** within the ability's range (measured to its closest soldier; **no line of sight needed**). Every soldier in it with the required keyword (Firestorm: Biological) is hit by the ability's weapon: it **always hits** (no hit roll) and does its damage after keywords and penetration vs armor (a weapon with no penetration value, like Firestorm's, ignores armor). "When targeted" and "when damaged" triggers fire for each soldier.
- **Spells** (magic) aren't weapons: close range and Manual Load don't apply to them, even when they attack.
- **Aimed attack** (e.g. the Rocket Launcher ability): the user shoots one **enemy soldier** with the ability's weapon, going through the shooting steps: the target must be within the **weapon's range**, in **line of sight** and not in **full cover**, and keywords mustn't prevent it; "when targeted" triggers fire; then damage, penetration, the **hit roll** at the user's accuracy (with the sprint penalty if the unit sprinted), a crit roll, damage, "when damaged" triggers. It shoots once per point of the weapon's Shots. The weapon is the user's own (their faction's changes apply), it isn't used in volleys, and **Manual Load** applies to it (once a turn).
- Each soldier can use each active ability once per turn (and in practice a unit uses at most one ability per turn — see Unit actions).
- *Display:* ability buttons don't show uses per turn. A use limit is only shown to the player for abilities limited to a number of uses **per battle** (no ability in the game has such a limit at the moment).
- **Uses per battle:** an ability can be limited to a number of uses per battle **by each unit**, the count shared by all the unit's soldiers. *No ability has such a limit at the moment: the Frag Grenade's once-per-battle limit was removed.*
- **Duplicate abilities are condensed:** when several soldiers in a unit have the same ability, the interface shows **one button**, and pressing it uses the ability **once**, as the unit's action. It's used by the unit leader if they have it (ranges are measured from them), otherwise by another soldier who does.
- **Area attacks** (target: a spot on the ground): every soldier within the radius is attacked with the ability's weapon, friend or foe. There's no hit roll; for each soldier, keywords, damage and penetration vs armor apply as for shooting, "when damaged" triggers fire, and anyone killed is removed when the blast is over.

#### Explosions — *implemented*

- **Under normal circumstances an explosion can't go through object terrain or structure terrain.** A soldier in the blast radius is only hit if the blast can reach them:
  - **Object terrain** (crates, low walls, cars, sandbags, window sills...) shields a soldier if the line from the blast to them, **seen from above**, crosses it — whatever its height. The object the blast lands on, and one the soldier stands on, don't count.
  - **Structure terrain** (building walls, hill crests, platforms) shields a soldier if a straight line just above the ground (0.3 m) from the blast to them hits it.
- Cover keywords (In half-cover, In full cover) don't matter for explosions; only the terrain in between does.
- Keywords can now change **Armor Level** for an attack (armor bonus / penalty), which counts in the penetration check.

#### Spells — *implemented*

- Abilities can have keywords that classify them. **Spell** (ability keyword, Trait) marks an ability as a spell.
- **A Spell can't be cast if the soldier sprinted** — meaning the soldier's **unit sprinted this turn** (its leader sprinted), the same test as the sprint accuracy penalty. So every soldier in a sprinting unit is affected.
- Casting a Spell after a normal move, after holding position, or instead of moving is allowed. Undoing a sprint lifts the restriction; it ends at the start of the next turn.
- Abilities that aren't Spells can still be used after sprinting.
- Spells so far: **Divine Healing**, **Divine Sanctuary**. (Lead from the front is not a Spell.)
- *Sound:* when a spell from the Divine Healing or Divine Sanctuary abilities is successfully cast, a **hallelujah** plays once (not when a target is refused).

#### Gaining and losing keywords — *implemented*

- Besides their **inherent** keywords (from their template), soldiers and weapons can **gain and lose keywords** during the game. Losing an inherent keyword only affects that one soldier/weapon.
- A gained keyword can last until removed, or **wear off at the start of a given turn**.
- Gained keywords take part in keyword interactions exactly like inherent ones.
- **A soldier, weapon or object can only have each keyword once.** Gaining a keyword it already has adds nothing (it can only make it last longer). Keywords with the same name count as the same keyword, and repeats are removed from templates. So a soldier near two Half-cover objects is In half-cover once, and takes the −25% once.
- **Moved:** a soldier who moves (including a 0 m move such as climbing down) gains **Moved**; it is removed at the **start of the next turn**. Undoing a move removes it. A leader who holds position doesn't gain it.

#### Terrain keywords and cover — *implemented*

- **Object terrain can have keywords** too, and can gain and lose them like soldiers and weapons.
- A keyword on an object can **grant a keyword to soldiers nearby**: every soldier within its radius (measured **horizontally from the nearest part of the object**, ignoring height) has the granted keyword. This is updated whenever a soldier moves, a move is undone, and at the start of every turn.
- **Half-cover** (object keyword): soldiers within **5 m** gain **In half-cover**.
- **In half-cover** (soldier keyword): an attack against this soldier takes **−25% accuracy** — **only if the object granting it is between the attacker and the target** (see "Between" under line of sight).
- **Full-cover** (object keyword): a soldier is **In full cover** against an attacker whenever a Full-cover object is **between the attacker and them**. **Distance doesn't matter**: how far the cover is from the shooter or from the target is irrelevant. (So it isn't a standing status on the soldier; it's worked out for each attacker.)
- **In full cover**: the soldier is fully concealed and **cannot be targeted by a shooting attack**.
- **Indirect fire** (weapon keyword): the attack **ignores line of sight** — so it can target soldiers **in full cover**, and cover being "between" doesn't matter — and **ignores accuracy modifiers**: it uses the soldier's Accuracy stat with no penalties or bonuses (no sprint penalty, no cover penalty, not even its own Accuracy Bonus). **Accuracy overrides (always hits / always misses) still apply.**
- Interactions can now use the condition **Always** (apply whenever the keyword is involved), e.g. In half-cover's penalty.
- Test room: a **Barricade** (wood, mantleable) with Half-cover sits near the squad's right flank.

## Movement — *implemented*

1. **The squad leader moves first.**
   - **Normal move:** up to their **movement range** (e.g. 10 m for a typical infantry soldier).
   - **Sprint:** up to **double** their movement range (20 m). **Mechanical** soldiers **can't sprint**: they only make a normal move (no sprint ring is shown).
   - Or the leader can **hold position**.
2. **Then the rest of the squad moves.** Each soldier may move **any distance**, but must **end in cohesion**: within **5 m** of the squad leader (**10 m** for a unit whose leader has the **Swarm** keyword).
   - Cohesion is measured from whoever was squad leader **when the movement began**.
3. The squad's move can only end once every soldier is in cohesion.

### Measuring a move — *implemented*

- A move is measured along the **route** the soldier walks: the shortest way around anything that blocks movement.
- The route is measured **along the surface of structure terrain**: going over a hill costs the distance up and down it; crossing a ditch costs the distance down and back up.
- The route is measured **straight through object terrain**: objects never add distance.
- **Cohesion** (the 5 m, or 10 m with Swarm) is measured as a **straight line** from the leader, **not following terrain and ignoring elevation**.
- **Climbing onto an object of any height costs 10 m** of movement (on top of walking to it); **0 m** for a soldier with **Jump Pack** or **Hover**. Soldiers climb on and off from right beside the object.
- **Climbing down costs 0 m**, but the soldier **still counts as having moved** this turn.
- Any soldier given a move counts as having moved, even if it costs 0 m. A leader who holds position has not moved.
- Walking up a **slope** is not climbing: it's measured along the slope.
- **Maps don't use steps.** Every elevation change is a slope, a climbable object, or purely visual.

### Wading — *implemented*

- **Water** (a riverbed below the water surface) can be walked through, but **every meter moved through water costs double** (2 m of movement per meter).
- Soldiers can stand in water. A **bridge** or a **shallow ford** crosses at normal cost.
- Routes take this into account: when going round by a bridge or ford is cheaper than wading, the route goes that way.
- While placing a move, the readout shows how much of it is wading (e.g. "14.2 m (wading 5.6 m: counts double)").

### Movement restrictions — *implemented*

- Soldiers **may walk through each other**, but may **not end a move overlapping another soldier**.
- Soldiers cannot end a move outside the battlefield.
- **Terrain that cannot be mantled over blocks movement**: building walls, and objects marked as not mantleable. Routes go around them.
- Soldiers can only end a move on walkable ground or **on top of an object that can be climbed onto**.
- **Room for the body:** a soldier wider than a person (e.g. a tank) moves on a walkable-area map built for its size. Its **route must have room for its hull the whole way**: it can't squeeze through a doorway, between buildings or between blocking objects that a soldier could pass, and goes around instead if there's a wide enough way; if there isn't, it can't get there. It can also only stop where its hull fits (not right up against a wall).

## Terrain — *implemented*

Terrain comes in two categories:

**Structure** — large, static landforms and buildings: ground, hills, ditches, slopes, buildings. Movement is measured along its surface. Building walls block movement; doorways let soldiers through.

**Object** — modeled props with some interaction. Each object says whether it:

| Property | Meaning |
|---|---|
| **Cover** | Can be used as cover (used by the shooting rules later). |
| **Mantle** | Soldiers can move over it. If not, it blocks movement. |
| **Climb onto** | Soldiers can end a move standing on top of it (costs 10 m to climb up). |

Test room terrain:

| Terrain | Category | Cover | Mantle | Climb onto |
|---|---|---|---|---|
| Hill (3 m high) | Structure | | | |
| Ditch (1.2 m deep) | Structure | | | |
| Building (roofless, one doorway) | Structure | | | |
| Platform (2 m high) with a slope up to it | Structure | | | |
| Low wall | Object | Yes | Yes | No |
| Crate | Object | Yes | Yes | Yes |
| Shipping container (keyword: Full-cover) | Object | Yes | No | Yes |
| Rock pillar (keyword: Full-cover) | Object | Yes | No | No |
| Barricade (keyword: Half-cover) | Object | Yes | Yes | No |

## Deployment phase — *implemented*

A battle started from the menu (or a multiplayer lobby) begins with a **deployment phase**, before round 1.

- Each army starts in its default slots in its side's **deployment zone** (see Army lists: rows of 6). The player's zone is **outlined in blue**.
- **Placing a unit:** click one of your units (or press **Tab** to pick the next one), then click where its **leader** should stand. Its other soldiers form up in ranks behind, facing the enemy, as they do when they first deploy. Green rings preview where they'll stand; red means they can't go there. **Esc** puts the unit down. A unit can be placed as often as you like.
- **Where a unit can go:** every soldier must be **inside your deployment zone**, on ground they can stand on, on the battlefield, and **not on top of another of your soldiers**.
- **Hidden armies:** you **can't see your opponent's units** (they aren't drawn, labelled or clickable), and they can't see yours, until the battle begins.
- **Ready (Enter)** ends your deployment. Against the AI (which deploys in its default slots), the battle begins at once. In **multiplayer**, both players deploy at the same time; the first to be ready waits ("Waiting for your opponent to finish deploying…"), and the battle begins when both are. Each game then puts the opponent's units where they placed them and reveals them.
- **Turn timer:** if it's on, deployment gets the same length of time as a turn (the HUD shows it). When it runs out, your units stay where they are and you're ready.
- No battle orders (selecting a unit to move, End turn) during deployment. You can **concede**; the battle ends and both armies are shown.

## Turns and the AI opponent — *implemented*

- **Turn order:** the **player** activates their units, then presses **End turn**; then the **AI** activates all of its units; then the player's next turn starts.
- **Rounds of play:** a **round** starts at the start of the turn of the player who moves **first** (against the AI, the player; in multiplayer, the south player), and lasts through the other player's turn. So round 1 is the first player's first turn followed by the second player's first turn; round 2 starts when the first player's second turn does. The HUD shows the round number ("Round 3"). Each player has one turn per round.
- **Concede:** the HUD has a **Concede** button (no hotkey), on either side's turn. It asks first (**Concede the battle?** with **Concede** and **Keep playing**; no hotkeys); confirming ends the battle at once in the other side's favour (**Defeat. You conceded.**). In multiplayer the other player is told and sees **Victory! The enemy conceded.**
- **End turn asks first** if any of the player's units **haven't moved yet** this turn (not destroyed, and not the selected unit once it has finished its move): a box lists them, with **End turn (N or Enter)** and **Keep playing (Esc)**. If every unit has moved, the turn ends straight away. Running out of time on the turn timer doesn't ask.
- **"Until the start of the next turn" means the caster's own next turn.** Something the player gains on their turn (Divine Sanctuary, Moved...) lasts through the AI's turn and wears off when the player's next turn starts; something the AI gains on its turn lasts through the player's turn and wears off when the AI's next turn starts. For a keyword trigger, it's the next turn of the soldier whose keyword fired.
- The AI's turn is **watchable**, unit by unit: the camera pans to each unit, it moves, then takes its action. **Skip (Space)** resolves the rest at once. The player can't give orders during it.
- **How the AI plays** (it uses the same rules as the player):
  - **Order:** units with auras first (e.g. a commander, so its aura is in place), then the infantry squads, then the other support units.
  - **Moving:** squads push toward the enemy, preferring spots in cover from which they can see enemies; they avoid close range, and avoid sprinting when it isn't worth the accuracy penalty. Support units (single officers and casters) stay about 5 m behind their squads, in cover where they can; casters never sprint, so they can still cast. The rest of a squad gathers into cohesion around its leader.
  - **Acting:** heal a friendly soldier missing 2+ health if it can; otherwise cast Divine Sanctuary on the friendly unit the most enemies can see; otherwise shoot the enemy unit the most of its soldiers can target (a sniper picks a soldier: Solo soldiers and sergeants first, otherwise the closest); otherwise throw a grenade at the spot hitting the most enemies — never if it would hit its own soldiers.

### Turn timer — *implemented (option)*

- An optional **turn timer** limits how long the player has for each of their turns. It's set on the main menu: **off by default**, **10 minutes** by default (1–30 minutes), and remembered between sessions.
- It counts down only during the player's turn (it pauses while the AI plays) and starts again from the full length at the start of each of their turns. The HUD shows the time left (red in the last minute).
- It applies to the **deployment phase** too: the same length of time to deploy; when it runs out, the units deploy where they stand.
- **When time runs out, the player's turn ends** as if they'd pressed End turn. A unit half-way through its move gathers its stragglers into cohesion and finishes the move; if it can't, its move is undone.

## Multiplayer — *implemented*

Two players can fight each other over the network instead of against the AI.

- **Connecting:** on the main menu, **Multiplayer**, then:
  - **Host:** the game waits for the other player and shows this computer's IP address(es) and the port (**7777**). Tell the other player the address.
  - **Join:** type the host's IP address and press **Connect**.
- **Setting up:** the **host chooses the battlefield**. Then **both players choose a faction and build an army** under the usual army list rules (each player sees only their own builder). When both have confirmed, the battle starts.
- **Sides:** who deploys at the **south** edge and **moves first** is **decided at random** when the battle starts. The other player deploys at the north edge, and their view of the battlefield is turned around so their own army is at the bottom of the screen.
- **Deployment:** both players deploy at the same time, without seeing each other's armies; the battle begins once both have pressed Ready (see Deployment phase).
- **Turns:** the same as against the AI: the south player activates all their units and ends their turn, then the north player does, then the next round starts (with the south player's turn). The rules (including "until the start of the next turn") are unchanged.
- **The other player's turn:** you watch their orders play out (the camera follows the unit they're commanding, and the log shows their volleys and abilities). You can't give orders until it's your turn.
- **Each player's own turn timer** (the main menu option) applies to their own turns.
- **Winning:** as usual (including conceding: the other player is told at once, even on your own turn). Each player sees **Victory** or **Defeat** from their own point of view. There's no Restart in multiplayer: **Main menu** leaves the game.
- **Leaving:** if either player leaves or the connection drops, the other player is told (**Opponent disconnected**) and returns to the main menu.
- Debug orders (kill the squad leader) aren't available in multiplayer.

How it works (for development): both games run the battle themselves. The host rolls the dice seed and who goes south, and both games use that seed. Each player's deployment (every soldier's exact position, sent when they press Ready) and each order the active player gives (select a unit, move, gather, end move, shoot, use an ability, end turn) is sent to the other game, which gives the same order through the same rules and dice. After every order the sending game includes a fingerprint of the battle (turn, every soldier's health and position), and the other game checks its own matches. If they ever disagree, the HUD shows a warning. While a walk animation is playing, the rules (and the fingerprint) use where the soldier's move ends, not how far the animation has got, and every animation ends exactly on that spot, so both games measure from the same place however fast each one draws.

## Transports — *implemented*

A soldier with the **Transport** keyword (soldier, Trait) is a transport vehicle: the keyword gives it two active abilities (Data/Abilities), **Embark** and **Disembark**. Its **capacity is one unit of Biological soldiers, however many soldiers that unit has.**

- **Embark** (target: a unit): choose a **friendly unit** whose closest soldier is **within 2.5 m of the transport's hull** (measured edge to edge; the green ring shows it). It **boards the transport**: it's taken off the battlefield (it can't be seen, targeted, or commanded) but **stays in the battle** (a side with only units aboard transports hasn't lost). It's the transport's action.
  - A transport carries **one unit at a time**, of any size.
  - **Every soldier of the unit must be Biological** (so no robots, tanks or other machines). It also can't board if it's carrying a unit itself or is aboard another transport.
- **Disembark** (no target; used as soon as its button is pressed): the unit aboard gets out **inside the transport** (every soldier at the transport's spot) and **starts its move at once, leader first**: it's selected straight away. It's the transport's action; the unit that got out then has its own move and action.
  - The **leader must move out** (no holding position, and the unit can't be deselected), then **every other soldier must move out too** (Gather does it), before the move can end.
  - A unit that has already acted this turn (e.g. it acted, then boarded) can't get out until the next turn.
  - If the turn ends before its leader has moved, the unit goes back aboard.
- **Destroyed with a unit aboard:** the unit aboard is lost with the transport.
- Works in multiplayer: getting out (and the selection that follows) happens in both games as part of the Disembark order.
- The AI doesn't use transports yet.

## Winning — *implemented*

- **A side with no soldiers left loses**, and the other side wins. If both sides lose their last soldiers at the same moment (e.g. one grenade), it's a **draw**.
- **Objectives:** on a map with an **objective marker** (so far the Siege of Ft Solstice), the side it belongs to (the attackers) **wins as soon as one of its unit leaders stands on the marker** (within its radius, measured horizontally) when a unit finishes its move or its action. Only a living **unit leader** counts, not other soldiers, and the other side's leaders standing on it do nothing. The banner then says the objective was reached.
- **Wiping out the enemy still wins on a map with an objective**: on the Siege of Ft Solstice the attackers also win if the defenders have no soldiers left, and the defenders win if the attackers have none left.
- **Conceding:** a side that concedes (the HUD's **Concede** button, after confirming) loses at once, and the other side wins.
- It's checked whenever soldiers are removed from the battlefield (at the end of a volley or blast, or when a soldier dies outside combat) and whenever a unit finishes its move or action.
- When the battle is decided, a **Victory / Defeat / Draw** banner appears, no more orders can be given, and **Restart (R)** starts the battle again (single player only). **After 10 seconds the game returns to the main menu by itself** (the banner counts down: *"Returning to the main menu in 10 s"*); **Main menu** goes straight away.

## Maps

### Open Season — *built (Scenes/Countryside, Ground Invasion > Build Open Season)*

- **100 × 100 m**, **not symmetrical**: open, gently rolling English farmland. Its shape is defined in `CountrysideLayout`.
- **Deployment zones:** the player sets up in the southern **10 m** of the map, the AI in the northern 10 m. Both strips are flat.
- **Hills and ridges** (structure terrain; slopes are gradual, never steeper than about 30°). Their crests **block line of sight**, so the high ground matters:
  - a **knoll** in the north-west (about 4 m high);
  - a **long ridge** running across the south-west (about 3 m);
  - a **low rise** in the south field (about 1.5 m).
- **The river** meanders west to east across the middle of the map, about 6 m of water between low banks. **Wading costs double** (see Wading). Two dry crossings:
  - a **stone bridge** (4 m wide) in the east, with low **parapets** (Half-cover; they block movement, so you can't step off the bridge into the water);
  - a **shallow ford** in the west, a gravel bed just above the water.
- **Farm buildings** (roofless, with doorways and climbable windows like the Boxed in's): a stone **farmhouse** in the north-east, a large wooden **barn** with wide doors in the south-east, and a small **stone shed** in the south field.
- **Farm props**, all **Half-cover**:

| Prop | Mantle | Climb onto | Notes |
|---|---|---|---|
| Hay bale | Yes | Yes | 1.2 m tall, in small groups |
| Haystack | No | No | 2.8 m tall |
| Cart | No | Yes | |
| Fence | Yes | No | 1 m tall, along field edges |
| Well | No | No | In the farmyard |
| Bridge parapet | No | No | Along both sides of the bridge |

- When the scene is opened directly in the editor, there are no armies on it.

### Boxed in — *built (Scenes/UrbanDemo, Ground Invasion > Build Boxed in)*

- **80 × 60 m**, with **180° rotational symmetry**: every feature in the south half has a twin at the opposite point, turned round, so both sides face the same choices.
- **Deployment zones:** the player sets up in the southern **8 m** of the map, the AI in the northern 8 m.
- **Armies:** chosen in the army builder (see Army lists). With no list chosen (the scene opened directly in the editor), there are no armies on it.
- **Centre:** an open plaza with a fountain (half-cover, blocks movement) and a statue (full cover), ringed by planters and low walls.
- **Each half:** an apartment block (two rooms), a corner shop with a raised loading dock (reached by a slope), a garage and a ruined house, plus streets with cars, sandbags, crates and a shipping container.
- **Buildings** are roofless, with doorways (2 m) and **windows**. A window can be **seen and shot through**, its sill gives **Half-cover**, and soldiers can **climb through** it (mantle: no extra movement cost).
- *Proposed:* cars, sandbags, crates, planters, low walls, rubble and furniture have **Half-cover**. Cars block movement but can be climbed onto. The container and the statue have **Full-cover**.

### Siege of Ft Solstice — *built (Scenes/FortSolstice, Ground Invasion > Build Ft Solstice)*

- **100 m wide × 130 m deep** of flat **desert**, **asymmetrical**: one side attacks, the other defends a massive fortress in the north.
- **The attackers** are the **south** side (in single player, you): they deploy on the open sand along the south edge (90 × 8 m), **42 m** from the fortress's outer wall. Two bands of sandstone rocks (Half-cover) and five boulders (Full-cover) dot the approach.
- **The defenders** are the **north** side (in single player, the AI): they start in the courtyard between the keep and the middle wall's north face (56 × 8 m), but their **deployment zone is the whole fortress**: anywhere inside the outer wall (the lanes) or the middle wall (including the keep and the wall walks). In the deployment phase they can move their units anywhere in it; the AI keeps its units where they start. An army too big for one row in the courtyard (more than 6 units) doesn't put its back rows on or beyond the north wall: those units take the nearest clear spots inside the fortress.
- **The fortress** (stone walls are structure terrain: they block movement and line of sight; gates are 6 m wide, room for a tank):
  - **Outer wall** (5 m high) across the south at z −15, with **three gates**, one per lane.
  - **Three lanes** running north from the outer wall to the middle wall, split by two walls, each with sandbag barricades and a crate (Half-cover).
  - **Middle wall** (4 m) enclosing the inner fortress (x ±30, z 9 to 55), with a **gate at the end of each lane** in its south face.
  - **The keep** (3.5 m; x ±12, z 21 to 45) with a **gate in each face**, low walls inside, and the **objective** in its centre (0, 33).
  - Sandbags and crates cover the ground between the middle wall and the keep.
  - **Wall walks** (manning the walls): platforms **2.5 m deep** behind the outer wall (its south, west and east faces and the short north stretches) and the middle wall (its south, west and east faces), **0.8 m below the wall top** (4.2 m up behind the outer wall, 3.2 m behind the middle wall), so a soldier on them sees, and shoots, over the wall. **Battlements** (0.5 m) along those wall tops are **Half-cover**: −25% to hit a soldier within 5 m of them when they're between shooter and target. **Ramps** lead up from the ground: two in each lane behind the outer wall (the lane walls are taller than the walk and split it) and one from each side of the outer wall; two from behind the middle wall's south face and one from each of its sides. Each stretch of wall between gates has its own walk (no bridges over the gates); walks meet at the corners. Vehicles and other large soldiers don't fit. On the ground right behind a wall, nobody sees over it.
- **Objective:** a gold marker (2.5 m radius) in the centre of the keep. **The attackers win by getting a unit leader onto it** (see Winning). The defenders win by wiping out the attackers.

### Polar Nonsense — *built (Scenes/Polar, Ground Invasion > Build Polar Nonsense)*

- **100 m × 100 m** of snow, **symmetrical**: laid out with **180° symmetry** (everything at (x, z) is also at (−x, −z), turned round), so neither side has the better half. The shape is in `Terrain.PolarLayout`.
- **Deployment zones:** the player sets up in the southern **10 m** of the map, the AI in the northern 10 m. Both strips are flat.
- **Hills** (structure terrain: their crests block line of sight): two **knolls** (4.5 m high, 13 m radius; south-west and north-east of the centre), two **ridges** (3 m high, along the west and east edges) and two **low rises** (2 m). Gentle snow drifts roll across the rest.
- **The station** (roofless buildings with doors and windows, on flat pads): the **Relay Hut** in the centre (6 × 6 m, doors north and south), and on each side a **Weather Station** (7 × 5 m) and a **Supply Shed** (5 × 4 m), their doors facing the centre.
- **Cover:**
  - **Half-cover:** **snowbanks** (1 m high; step over them), **fuel drums** and **supply crates** (step over or climb on top).
  - **Full-cover:** **ice boulders** (2.6 m) and the station's two **fuel tanks** (2.5 m, 3 m across); both block movement.
  - The Relay Hut's two **radio masts** are thin poles: no cover.
  - Every prop sits on gentle ground and is set into the snow under its whole footprint, so no hill pokes through it and no corner floats (checked by a test).

### Leisure Cruise — *built (Scenes/Ship, Ground Invasion > Build Leisure Cruise)*

- **50 m × 150 m**: the cluttered inside of a ship, laid out with **180° symmetry** (everything at (x, z) is also at (−x, −z), turned round). The shape is in `Terrain.ShipLayout`.
- **Deployment zones:** a **hangar bay** at each end; the player sets up in the southern **10 m**, the AI in the northern 10 m.
- **Corridors:** a **6 m spine corridor** down the middle (wide enough for vehicles), a **4 m corridor** along each side of the hull, and a **cross corridor** in each half joining them. Long and straight: they're firing lanes, broken up by clutter.
- **Compartments** (18 × 12 m, between the corridors, with doorways and a few viewing windows): in each half an Armory, Med Bay, Machine Shop, Galley, Comms Room, Reactor Control and two Crew Quarters. Doorways are 2 m: infantry only.
- **The cargo hold** in the middle (42 × 20 m): open, with cargo containers; one sits in the very middle, so no one can see down the spine from end to end.
- **Bulkheads** are 3 m high and the hull 4 m; both are structure terrain (they block line of sight and movement).
- **Clutter:**
  - **Half-cover:** **supply crates**, **coolant drums**, **bunks**, **med beds**, **tables**, **counters** and **workbenches** (step over or climb on top); **debris** and **consoles** (step over).
  - **Full-cover:** **cargo containers** (2.6 m), **lockers** (2.2 m) and **reactor housings** (2.6 m); all block movement.

## Shooting — *partly implemented*

### Penetration vs armor — *implemented*

When shooting, the weapon's **Penetration** is compared with the target's **Armor Level**, and the result scales the attack's **final damage calculation**:

| Penetration compared to Armor Level | Damage | Bonus effects |
|---|---|---|
| Higher (by any amount) | 150% | Apply |
| Equal | 100% | Apply |
| 1 lower | 50% | Apply |
| 2 or more lower | 0% | **Do not apply** |
| **No penetration value** (not a number, or none listed) | **100%, regardless of armor** | Apply |

- Fractions are **rounded down** (50% of 5 = 2; 150% of 5 = 7).

### Accuracy — *implemented*

All hit-chance changes **add or subtract percentage points**, in this order:

1. Start with the soldier's **Accuracy** stat.
2. Subtract all **penalties**.
3. Add all **bonuses**.

Any modifier that **explicitly overrides** accuracy is used instead, and nothing else can modify it.

Example: Accuracy 100% → sprinted (−25%) → 75% → weapon Accuracy Bonus (+20%) → **95%**.

- **Sprint penalty:** a soldier whose unit sprinted this turn takes **−25%**.
- A weapon's Accuracy Bonus counts as a bonus (a negative one counts as a penalty).
- Accuracy **can't go below 0%** (penalties stop at 0% before bonuses are added).
- Accuracy **can go above 100%**. The excess becomes a **crit chance**: at 105% the hit roll is 100% and a separate crit roll is made at 5%. Above 200% nothing more happens (both rolls are 100%).
- If an attack is under both **"always hits"** and **"always misses"**, **always misses** applies. (In general, when overrides conflict the lowest wins.)

### Hit and crit rolls — *implemented*

1. **Hit roll:** d100 against accuracy (up to 100%).
2. If it hits, **crit roll:** d100 against accuracy over 100%.

A miss can never crit.

### Damage order — *implemented*

1. The **final damage calculation**: the weapon's **Base Damage**, minus keyword damage penalties (not below 0), plus keyword damage bonuses. With no keyword interactions it is just Base Damage. Penalties are applied before bonuses, as with accuracy.
2. **Penetration vs armor** multiplier (below), rounded down.
3. **Crit:** adds a bonus equal to **100% of the damage from step 2**.

Example: 6 damage, penetration beats armor → 9, crit → +9 = **18**. If armor blocks the attack (0 damage), a crit adds nothing.

### Weapon range — *implemented*

- A soldier can only target an enemy **within their weapon's range**, measured horizontally (exactly at the range counts). Assault Rifle **20 m**, Pistol **10 m**, SMG **10 m**, Machine Gun **20 m**, Flamethrower **10 m**, Sniper Rifle **50 m**.
- In step 6 soldiers only pick enemies they can target, so enemies out of range are passed over; a soldier with no enemy in range doesn't shoot.
- **Indirect fire** ignores line of sight but **not** range.
- If **none** of a unit's soldiers can target any soldier of the chosen enemy unit (out of range or out of sight), the shoot order is **refused** (error sound) and the unit's action isn't used.
- **If no range is defined for a weapon, it has unlimited range.** (So far only the blast weapons — the Frag Grenade's and the Unstable explosion — have none; how far the grenade can be thrown is the ability's own 10 m.)
- In shoot mode a red ring shows the unit leader's weapon range.
- **Range preview while moving:** while choosing where the unit leader moves, a red ring shows the range of the leader's **primary weapon**, centred on the spot the leader would end up, so you can see what will be in range before committing. It isn't shown for a weapon with unlimited range, or once the leader has moved.

### Close range — *implemented*

- **Mechanical** soldiers **can't be in close range**, whatever is near them.
- A **Biological** soldier is **in close range** if a **Biological enemy** soldier is **less than 2 m** away (horizontally; exactly 2 m is not close range). The dead don't count.
- A soldier in close range **can't use any weapon unless it has the keyword Pistol or Melee**:
  - **Shooting:** they shoot with a Pistol if they carry one (the Sergeant switches from his Assault Rifle to his Pistol); a soldier with no Pistol (e.g. a Trooper) doesn't shoot.
  - **Abilities that use a weapon** (e.g. Frag Grenade) can't be used by a soldier in close range. For a unit's shared ability, another soldier of the unit who isn't in close range uses it instead (ranges are then measured from them); if everyone is in close range, it can't be used.
- **Magic and non-weapon abilities still work** in close range (e.g. Divine Healing). **Passive abilities are unaffected** (e.g. Lead from the front).
- Soldiers in close range show the status keyword **In close range**, and a **red marker** floats over their head.
- **Pistol** (weapon keyword, Trait): usable in close range. The Pistol has it.

### Unit actions — *implemented*

Each turn, every unit can be activated once. It **may move**, then — **after moving, or instead of moving** — it takes **one action**:

- **Shoot** at an enemy unit (the shooting sequence below), **or**
- **Use one ability** that one soldier in the unit has.

Taking the action **ends that unit's turn**: no more moving, shooting or abilities. (A unit may also simply move and stop.)

### Shooting order — *implemented*

Under normal circumstances, shooting follows this order:

1. The player **selects a unit** (click one of your units, or Tab). Each unit acts once per turn; you can switch units before the current one starts moving, or after it has finished.
2. The player **may move** the unit.
3. The player **declares** the unit will shoot.
4. The player **selects an enemy unit**.
5. **Line of sight:** the attacking unit must be able to see the defending unit (at least one attacking soldier can see at least one defending soldier). Any **objects in the lines of sight** are noted.
   **No wasted volleys:** if none of the unit's soldiers could hurt any soldier of the enemy unit (their armor would stop every shot), the order is refused with a message (*"…'s armor would stop every shot … can make"*) and the unit's action isn't spent. The AI doesn't pick such targets either.

**Shots:** every weapon has a **Shots** value: in a volley, a soldier shoots that many times. **Every shot runs through steps 6–13**, including picking a target afresh. The volley is resolved in **rounds**: first, everyone whose weapon has **Shots 1 or more** fires one shot (steps 6–13 for all of them); then everyone with **Shots 2 or more** fires a shot; then **3 or more**; and so on, until no one has shots left.
- Each round's picks are made after the previous round has been resolved, so soldiers **killed in an earlier round aren't picked again** (they stay on the battlefield until the volley ends, as usual). If no valid target is left, the soldier doesn't take that shot.
- **Spreading fire** (step 6) counts only the soldiers aiming in the current round.
- A soldier killed during the volley (e.g. by an explosion) doesn't take any more shots.
- "When targeted" triggers still fire **only once per volley**, however many shots pick the soldier.
- Every weapon has **Shots 1**, except the **Flamethrower (3)** and the **Machine Gun (5)**.
6. **Targets:** the attacking soldiers pick one at a time, and **spread their fire**: each picks a valid target (one they can target) that **no other soldier of their unit is aiming at yet**. If every valid target is already taken, they pick one that **only 1** soldier is aiming at; if there's none, one with **2**, and so on. Among equally-aimed-at targets they pick the **closest**. So several soldiers only share a target once every valid target has someone on it. If the target has keywords **triggered by being targeted**, shooting pauses, the trigger is resolved, and shooting resumes (so its effects apply to that attack).
7. **Prevented?** If keywords prevent the attack (e.g. a **Toxic** weapon against a target without **Biological**), or **the target's armor would stop it completely** (penetration against armor leaves no damage, e.g. an Assault Rifle, penetration 1, against Armor 4: see Penetration vs armor), the soldier **goes back to step 6** with the next soldier in that order, and repeats steps 6–7 through **every enemy soldier** it can shoot until it finds a valid target. If there is none, it **doesn't shoot this turn**. The target picked at the end of step 7 is final.
8. **Damage** is calculated: Base Damage − keyword penalties (not below 0) + keyword bonuses.
9. **Armor penetration** modifies that damage (×1.5 / ×1 / ×0.5 / ×0, rounded down).
10. **Accuracy** is calculated, then the **hit roll**.
11. **Crit roll** if accuracy is over 100%: a crit adds 100% of the post-penetration damage. Otherwise skipped.
12. **Damage is applied.** At 0 Health a soldier dies (and leadership passes on if they led the unit). A soldier who picked a target that has since died **still shoots at it** (the shot does nothing).
13. Keywords **triggered by receiving damage** activate.

Details:

- **Line of sight:** a soldier can see a target if no **structure** (building walls, hill crests, platforms) blocks the line from their eyes to the target's chest or head. **Objects don't block sight** — they give cover through their keywords.
- **"Between"** for cover: an object is between attacker and target if the line between them, seen from above, passes through the object. (This now drives Half-cover and Full cover.)
- A target **can't be picked** if it's out of sight or a Full-cover object is between it and the shooter (at any distance) — unless the weapon ignores line of sight (Indirect fire).
- **Keyword triggers** (on keyword assets): *when Targeted / when Damaged / when Killed → Self / Attacker gains / loses keyword X* (optionally until the next turn), or *→ explode* (an area attack centred on the soldier). A trigger can have a **chance** (e.g. 10%), rolled each time. The keywords checked are the soldier's own **and those of the weapons they carry**. Coded abilities can hook the same moments.
- **On death:** whenever a soldier is killed — by shooting, a grenade, an explosion or anything else — their "on death" triggers are checked at once. Anyone killed by them (e.g. by an explosion) is removed afterwards, and their own on-death triggers are checked too (chain reactions).
- **Shooting is the unit's action** and ends its turn. A unit may shoot without moving (its leader then counts as holding position).
- **Primary weapon:** every soldier template designates one weapon as its **primary weapon**, used to shoot under normal conditions (e.g. a trooper's Assault Rifle). Circumstances can make a soldier use an alternative weapon: so far, **close range** (a Pistol only).
- **The dead:** a soldier killed during a round of shooting (one attacking unit's volley) stays on the battlefield **until that round of shooting ends** — so soldiers of that unit who already picked them still shoot at them (step 12). When the round ends, the dead are **removed from their unit and the battlefield**: a **second attacking unit can't target them**, even in the same turn. A soldier killed outside shooting is removed at once.
- **"When targeted"** means *when targeted*: a soldier's "when targeted" triggers fire when they are picked in step 6, **even if no attack follows** (e.g. step 7 sends the shooter on to another target). They fire **only once per volley** — one attacking unit's sequence of shooting — however many soldiers from that unit pick them. A **different attacking unit's volley** in the same turn can trigger them again.

## Open questions

- *(None right now.)*
